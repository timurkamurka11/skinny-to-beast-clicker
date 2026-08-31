using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Safe state router for the four-layer Animator. It cross-fades directly
    /// to authored bone clips and never queues triggers, so a burst of taps can
    /// restart one action but cannot grow an unbounded Animator queue.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        public const string ControllerResourcePath =
            "UI/Gameplay/Living/Animations/LivingCharacter";

        private const string ExpectedControllerName = "LivingCharacter";

        private static readonly string[] RequiredLayerNames =
        {
            "Base",
            "UpperBody",
            "Face",
            "FullBodyAction"
        };

        private static readonly (string layer, string state)[] RequiredStates =
        {
            ("Base", "Idle_Breathe"),
            ("Base", "Walk_Front"),
            ("Base", "Walk_Side"),
            ("Base", "Walk_Back"),
            ("UpperBody", "Idle_LookAround"),
            ("Face", "Face_Blink"),
            ("FullBodyAction", "TapLift_A"),
            ("FullBodyAction", "TapLift_B"),
            ("FullBodyAction", "TapLift_C")
        };

        private static readonly (string name, AnimatorControllerParameterType type)[]
            RequiredParameters =
            {
                ("Speed", AnimatorControllerParameterType.Float),
                ("Facing", AnimatorControllerParameterType.Int),
                ("Sitting", AnimatorControllerParameterType.Bool)
            };

        private readonly HashSet<string> observedIdleActions = new();
        private readonly Dictionary<string, int> layerByName = new();
        private readonly HashSet<string> parameterNames = new();
        private readonly Dictionary<string, AnimatorControllerParameterType>
            parameterTypes = new();

        private Animator animator;
        private CharacterFacing facing = CharacterFacing.Front;
        private string currentBaseState = string.Empty;
        private string baseOverrideState = string.Empty;
        private float baseOverrideUntil;
        private float upperBodyUntil;
        private float faceUntil;
        private float fullBodyUntil;
        private float nextControllerRetryAt;
        private int tapVariant;
        private int acceptedTapCount;
        private bool moving;
        private float movementSpeed;
        private bool configured;
        private bool missingControllerLogged;
        private RuntimeAnimatorController cachedController;

        public bool IsReady => string.IsNullOrEmpty(ReadinessError);
        public string ReadinessError
        {
            get
            {
                if (!configured) return "Character animation driver is not configured.";
                if (animator == null) return "Character Animator is missing.";
                if (animator.gameObject != gameObject)
                {
                    return "Character Animator is bound to the wrong gameplay object.";
                }
                Animator[] owners = gameObject.GetComponents<Animator>();
                if (owners.Length != 1 || owners[0] != animator)
                {
                    return "Character gameplay owner must have exactly one Animator.";
                }
                if (!animator.enabled) return "Character Animator is disabled.";
                if (animator.applyRootMotion)
                {
                    return "Character Animator must not own gameplay-root travel.";
                }
                if (animator.updateMode != AnimatorUpdateMode.UnscaledTime ||
                    animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                {
                    return "Character Animator has the wrong update or culling mode.";
                }
                if (!animator.gameObject.activeInHierarchy)
                {
                    return "Character Animator hierarchy is inactive.";
                }
                if (!animator.isInitialized) return "Character Animator is not initialized.";
                RuntimeAnimatorController controller = animator.runtimeAnimatorController;
                if (controller == null) return "Character Animator controller is missing.";
                if (!string.Equals(controller.name, ExpectedControllerName,
                        System.StringComparison.Ordinal))
                {
                    return "Character Animator uses the wrong controller: " +
                        controller.name + ".";
                }
                for (int i = 0; i < RequiredLayerNames.Length; i++)
                {
                    if (!layerByName.ContainsKey(RequiredLayerNames[i]))
                    {
                        return "Character Animator is missing required layer " +
                            RequiredLayerNames[i] + ".";
                    }
                }
                for (int i = 0; i < RequiredParameters.Length; i++)
                {
                    var required = RequiredParameters[i];
                    if (!parameterTypes.TryGetValue(required.name, out var actual) ||
                        actual != required.type)
                    {
                        return "Character Animator is missing required parameter " +
                            required.name + " (" + required.type + ").";
                    }
                }
                for (int i = 0; i < RequiredStates.Length; i++)
                {
                    var required = RequiredStates[i];
                    int layer = layerByName[required.layer];
                    int hash = Animator.StringToHash(required.layer + "." + required.state);
                    if (!animator.HasState(layer, hash) &&
                        !animator.HasState(layer, Animator.StringToHash(required.state)))
                    {
                        return "Character Animator is missing required state " +
                            required.layer + "." + required.state + ".";
                    }
                }
                return string.Empty;
            }
        }
        public int AcceptedTapCount => acceptedTapCount;
        public int ObservedIdleActionCount => observedIdleActions.Count;
        public Animator Animator => animator;

        public void ClearObservedIdleActions()
        {
            observedIdleActions.Clear();
        }

        public void Configure(Animator targetAnimator)
        {
            animator = targetAnimator;
            if (animator == null)
            {
                configured = false;
                return;
            }

            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;

            missingControllerLogged = false;
            configured = true;
            TryLoadController();
        }

        private void TryLoadController()
        {
            if (animator == null)
            {
                return;
            }

            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                controller = Resources.Load<RuntimeAnimatorController>(ControllerResourcePath);
                animator.runtimeAnimatorController = controller;
            }
            layerByName.Clear();
            parameterNames.Clear();
            parameterTypes.Clear();
            cachedController = controller;
            if (controller != null)
            {
                missingControllerLogged = false;
                for (int i = 0; i < animator.layerCount; i++)
                {
                    layerByName[animator.GetLayerName(i)] = i;
                }

                AnimatorControllerParameter[] parameters =
                    animator.parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    parameterNames.Add(parameters[i].name);
                    parameterTypes[parameters[i].name] = parameters[i].type;
                }

                animator.Rebind();
                animator.Update(0f);
                SetLayerWeight("Base", 1f);
                SetLayerWeight("UpperBody", 0f);
                SetLayerWeight("Face", 0f);
                SetLayerWeight("FullBodyAction", 0f);
                ResetState();
            }
            else
            {
                if (!missingControllerLogged)
                {
                    missingControllerLogged = true;
                    Debug.LogError(
                        "LivingCharacter.controller is missing because " +
                        "Patch 3 editor asset generation did not finish. " +
                        "Exit Play Mode and inspect the first editor error.",
                        this);
                }

                nextControllerRetryAt =
                    Time.unscaledTime + 1f;
            }
        }

        public void ResetState()
        {
            baseOverrideState = string.Empty;
            baseOverrideUntil = 0f;
            upperBodyUntil = 0f;
            faceUntil = 0f;
            fullBodyUntil = 0f;
            currentBaseState = string.Empty;
            moving = false;
            movementSpeed = 0f;
            facing = CharacterFacing.Front;
            if (animator != null)
            {
                animator.speed = 1f;
            }
            SetParameter("Speed", 0f);
            SetParameter("Facing", (int)facing);
            SetLayerWeight("Base", 1f);
            SetLayerWeight("UpperBody", 0f);
            SetLayerWeight("Face", 0f);
            SetLayerWeight("FullBodyAction", 0f);
            UpdateBaseState(true);
        }

        public void Tick()
        {
            if (animator != null &&
                animator.runtimeAnimatorController != cachedController)
            {
                TryLoadController();
            }
            if (!IsReady)
            {
                if (animator != null &&
                    animator.runtimeAnimatorController == null &&
                    Time.unscaledTime >= nextControllerRetryAt)
                {
                    nextControllerRetryAt =
                        Time.unscaledTime + 1f;
                    TryLoadController();
                }

                return;
            }

            float now = Time.unscaledTime;
            if (!string.IsNullOrEmpty(baseOverrideState) &&
                now >= baseOverrideUntil)
            {
                baseOverrideState = string.Empty;
                currentBaseState = string.Empty;
                UpdateBaseState(true);
            }

            if (upperBodyUntil > 0f && now >= upperBodyUntil)
            {
                upperBodyUntil = 0f;
                SetLayerWeight("UpperBody", 0f);
            }

            if (faceUntil > 0f && now >= faceUntil)
            {
                faceUntil = 0f;
                SetLayerWeight("Face", 0f);
            }

            if (fullBodyUntil > 0f && now >= fullBodyUntil)
            {
                fullBodyUntil = 0f;
                SetLayerWeight("FullBodyAction", 0f);
            }
        }

        public void SetLocomotion(
            CharacterFacing nextFacing,
            float speed,
            bool isMoving)
        {
            facing = nextFacing;
            movementSpeed = Mathf.Max(0f, speed);
            moving = isMoving && movementSpeed > 0.001f;
            if (animator != null)
            {
                animator.speed = moving
                    ? Mathf.Clamp(movementSpeed, 0.65f, 1.75f)
                    : 1f;
            }
            SetParameter("Speed", moving ? movementSpeed : 0f);
            SetParameter("Facing", (int)facing);
            if (moving)
            {
                baseOverrideState = string.Empty;
                baseOverrideUntil = 0f;
            }

            UpdateBaseState(false);
        }

        public void PlayEntryWalk(float speed)
        {
            facing = CharacterFacing.Back;
            movementSpeed =
                Mathf.Clamp(speed, 0.65f, 1.75f);
            moving = true;
            if (animator != null)
            {
                animator.speed = movementSpeed;
            }
            baseOverrideState = "Entry_WalkToDoor";
            baseOverrideUntil = float.PositiveInfinity;
            currentBaseState = string.Empty;
            SetParameter("Speed", movementSpeed);
            SetParameter("Facing", (int)CharacterFacing.Back);
            UpdateBaseState(false);
        }

        public void PlayRoutineAction(
            CharacterRoutineAction action,
            float duration)
        {
            float safeDuration = Mathf.Max(0.12f, duration);
            string state = action switch
            {
                CharacterRoutineAction.ShiftWeight => "Idle_ShiftWeight",
                CharacterRoutineAction.LookAround => "Idle_LookAround",
                CharacterRoutineAction.Scratch => "Idle_Scratch",
                CharacterRoutineAction.Yawn => "Idle_Yawn",
                CharacterRoutineAction.Stretch => "Idle_Stretch",
                CharacterRoutineAction.Flex => "Idle_Flex",
                CharacterRoutineAction.AdjustClothes => "Idle_AdjustClothes",
                CharacterRoutineAction.WarmShoulders => "Idle_WarmShoulders",
                CharacterRoutineAction.SitDown => "SitDown",
                CharacterRoutineAction.SitLoop => "SitLoop",
                CharacterRoutineAction.StandUp => "StandUp",
                CharacterRoutineAction.Sit => "SitLoop",
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(state))
            {
                return;
            }

            if (action != CharacterRoutineAction.SitDown &&
                action != CharacterRoutineAction.SitLoop &&
                action != CharacterRoutineAction.StandUp &&
                action != CharacterRoutineAction.Sit)
            {
                observedIdleActions.Add(state);
            }
            switch (action)
            {
                case CharacterRoutineAction.ShiftWeight:
                case CharacterRoutineAction.SitLoop:
                case CharacterRoutineAction.Sit:
                    baseOverrideState = state;
                    baseOverrideUntil =
                        Time.unscaledTime + safeDuration;
                    currentBaseState = string.Empty;
                    UpdateBaseState(true);
                    break;
                case CharacterRoutineAction.SitDown:
                case CharacterRoutineAction.StandUp:
                    fullBodyUntil =
                        Time.unscaledTime + safeDuration;
                    SetLayerWeight("FullBodyAction", 1f);
                    CrossFade(state, "FullBodyAction", 0.12f, true);
                    break;
                default:
                    upperBodyUntil =
                        Time.unscaledTime + safeDuration;
                    SetLayerWeight("UpperBody", 1f);
                    CrossFade(state, "UpperBody", 0.12f, true);
                    break;
            }
        }

        public void CancelActions()
        {
            baseOverrideState = string.Empty;
            baseOverrideUntil = 0f;
            upperBodyUntil = 0f;
            faceUntil = 0f;
            fullBodyUntil = 0f;
            SetLayerWeight("UpperBody", 0f);
            SetLayerWeight("Face", 0f);
            SetLayerWeight("FullBodyAction", 0f);
            currentBaseState = string.Empty;
            UpdateBaseState(true);
        }

        public int TriggerTap()
        {
            tapVariant = (tapVariant + 1) % 3;
            string state = tapVariant switch
            {
                0 => "TapLift_A",
                1 => "TapLift_B",
                _ => "TapLift_C"
            };
            fullBodyUntil = Time.unscaledTime + 0.54f;
            SetLayerWeight("FullBodyAction", 1f);
            if (CrossFade(
                    state,
                    "FullBodyAction",
                    0.12f,
                    true))
            {
                acceptedTapCount++;
            }

            return tapVariant;
        }

        public void TriggerUpgrade()
        {
            upperBodyUntil = Time.unscaledTime + 0.9f;
            SetLayerWeight("UpperBody", 1f);
            CrossFade("Idle_Flex", "UpperBody", 0.12f, true);
        }

        public void TriggerStageChange()
        {
            fullBodyUntil = Time.unscaledTime + 0.82f;
            SetLayerWeight("FullBodyAction", 1f);
            CrossFade("StageChange", "FullBodyAction", 0.12f, true);
        }

        public void TriggerFaceBlink()
        {
            faceUntil = Time.unscaledTime + 0.13f;
            SetLayerWeight("Face", 1f);
            CrossFade("Face_Blink", "Face", 0.02f, true);
        }

        public bool StressTap(int count)
        {
            if (count < 0)
            {
                return false;
            }

            int before = acceptedTapCount;
            for (int i = 0; i < count; i++)
            {
                TriggerTap();
            }

            if (animator != null &&
                animator.enabled &&
                animator.runtimeAnimatorController != null)
            {
                animator.Update(0f);
            }

            return acceptedTapCount - before == count &&
                   IsReady &&
                   !float.IsNaN(animator.speed) &&
                   !float.IsInfinity(animator.speed);
        }

        private void UpdateBaseState(bool immediate)
        {
            if (!IsReady)
            {
                return;
            }

            string next;
            if (!string.IsNullOrEmpty(baseOverrideState))
            {
                next = baseOverrideState;
            }
            else if (!moving)
            {
                next = "Idle_Breathe";
            }
            else
            {
                next = facing switch
                {
                    CharacterFacing.Back => "Walk_Back",
                    CharacterFacing.SideLeft => "Walk_Side",
                    CharacterFacing.SideRight => "Walk_Side",
                    _ => "Walk_Front"
                };
            }

            if (next == currentBaseState)
            {
                return;
            }

            currentBaseState = next;
            CrossFade(next, "Base", immediate ? 0f : 0.12f);
        }

        private bool CrossFade(
            string stateName,
            string layerName,
            float duration,
            bool restart = false)
        {
            if (animator == null ||
                animator.runtimeAnimatorController == null ||
                !layerByName.TryGetValue(layerName, out int layer))
            {
                return false;
            }

            int fullHash =
                Animator.StringToHash($"{layerName}.{stateName}");
            int shortHash = Animator.StringToHash(stateName);
            int stateHash = animator.HasState(layer, fullHash)
                ? fullHash
                : shortHash;
            if (!animator.HasState(layer, stateHash))
            {
                Debug.LogError(
                    $"Animator state '{layerName}.{stateName}' is missing.",
                    this);
                return false;
            }

            if (restart)
            {
                animator.CrossFade(
                    stateHash,
                    duration,
                    layer,
                    0f);
            }
            else
            {
                animator.CrossFade(stateHash, duration, layer);
            }

            return true;
        }

        private void SetParameter(string name, float value)
        {
            if (animator != null && parameterNames.Contains(name))
            {
                animator.SetFloat(name, value);
            }
        }

        private void SetParameter(string name, int value)
        {
            if (animator != null && parameterNames.Contains(name))
            {
                animator.SetInteger(name, value);
            }
        }

        private void SetLayerWeight(string layerName, float weight)
        {
            if (animator != null &&
                layerByName.TryGetValue(layerName, out int layer))
            {
                animator.SetLayerWeight(
                    layer,
                    Mathf.Clamp01(weight));
            }
        }
    }
}
