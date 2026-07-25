using System.Collections;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// In-scene readiness suite. Run it from CharacterRoot's context menu in
    /// Play Mode; it exercises the same runtime objects used by the player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterRigPlayModeTests : MonoBehaviour
    {
        private CharacterRigController rigController;
        private CharacterSkinController skinController;
        private CharacterRigValidator validator;
        private CharacterVisibilityGate visibilityGate;
        private Coroutine suite;

        public bool IsRunning => suite != null;
        public bool LastRunPassed { get; private set; }
        public string LastResult { get; private set; } =
            "Readiness suite has not run.";

        public void Configure(
            CharacterRigController rig,
            CharacterSkinController skin,
            CharacterRigValidator rigValidator,
            CharacterVisibilityGate gate)
        {
            rigController = rig;
            skinController = skin;
            validator = rigValidator;
            visibilityGate = gate;
        }

        [ContextMenu("Run Full Patch 3 Readiness Suite")]
        private void RunFromContextMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError(
                    "Patch 3 readiness suite must run in Play Mode.",
                    this);
                return;
            }

            if (suite != null)
            {
                return;
            }

            suite = StartCoroutine(RunSuite());
        }

        public IEnumerator RunSuite()
        {
            LastRunPassed = false;
            LastResult = string.Empty;
            if (rigController == null ||
                skinController == null ||
                validator == null ||
                visibilityGate == null)
            {
                Fail("One or more readiness components are missing.");
                yield break;
            }

            if (!validator.ValidateNow(false))
            {
                Fail("Initial validation failed: " + validator.LastError);
                yield break;
            }

            int originalStage =
                Mathf.Max(0, skinController.CurrentArtIndex);
            GameObject rigPrefab =
                Resources.Load<GameObject>(
                    "UI/Gameplay/Living/CharacterRig2D");
            if (rigPrefab == null)
            {
                Fail("CharacterRig2D.prefab could not be loaded.");
                yield break;
            }

            for (int launch = 0; launch < 100; launch++)
            {
                GameObject probe =
                    Instantiate(
                        rigPrefab,
                        transform.parent,
                        false);
                RectTransform probeRoot =
                    probe.GetComponent<RectTransform>();
                probe.name =
                    $"CharacterLaunchProbe.{launch + 1:000}";
                probeRoot.anchorMin = new Vector2(0.5f, 0f);
                probeRoot.anchorMax = new Vector2(0.5f, 0f);
                probeRoot.pivot = new Vector2(0.5f, 0.5f);
                probeRoot.anchoredPosition =
                    (transform as RectTransform).anchoredPosition;
                probeRoot.sizeDelta = new Vector2(720f, 1280f);
                probeRoot.localScale = Vector3.one;

                CanvasGroup probeGroup =
                    GetOrAdd<CanvasGroup>(probe);
                probeGroup.alpha = 1f;
                probeGroup.interactable = false;
                probeGroup.blocksRaycasts = false;

                CharacterFaceController probeFace =
                    GetOrAdd<CharacterFaceController>(probe);
                CharacterRigController probeRig =
                    GetOrAdd<CharacterRigController>(probe);
                probeRig.Build(probeRoot, probeFace);
                CharacterSkinController probeSkin =
                    GetOrAdd<CharacterSkinController>(probe);
                probeSkin.Configure(
                    probeRig,
                    probeGroup,
                    skinController.DefinitionCount);
                probeSkin.ApplySkin(
                    launch % skinController.DefinitionCount,
                    false);
                CharacterRigValidator probeValidator =
                    GetOrAdd<CharacterRigValidator>(probe);
                probeValidator.Configure(probeRig, probeSkin);
                CharacterVisibilityGate probeGate =
                    GetOrAdd<CharacterVisibilityGate>(probe);
                probeGate.Configure(
                    probeRoot,
                    probeRig,
                    probeSkin,
                    probeValidator,
                    0.34f,
                    0.50f);

                yield return null;
                yield return new WaitForEndOfFrame();
                yield return null;
                if (!probeGate.IsReady)
                {
                    Fail(
                        $"Visibility failed on real prefab launch " +
                        $"{launch + 1}: {probeGate.LastError}");
                    Destroy(probe);
                    skinController.ApplySkin(originalStage, false);
                    yield break;
                }

                Destroy(probe);
                yield return null;
            }

            if (!validator.RunSkinSwapStress(50))
            {
                Fail(validator.LastError);
                skinController.ApplySkin(originalStage, false);
                yield break;
            }

            int animatedTarget = originalStage;
            for (int swap = 0; swap < 50; swap++)
            {
                animatedTarget =
                    (originalStage + swap + 1) %
                    skinController.DefinitionCount;
                skinController.ApplySkin(
                    animatedTarget,
                    true);
                yield return null;
                if (skinController.ActiveBaseSkinCount != 1)
                {
                    Fail(
                        $"Animated stage swap created an overlap at " +
                        $"iteration {swap + 1}.");
                    skinController.ApplySkin(originalStage, false);
                    yield break;
                }
            }

            yield return new WaitForSecondsRealtime(0.9f);
            if (skinController.CurrentArtIndex != animatedTarget ||
                !validator.ValidateNow(false))
            {
                Fail(
                    "The final rapid animated stage swap did not settle " +
                    "on one valid body: " + validator.LastError);
                skinController.ApplySkin(originalStage, false);
                yield break;
            }

            if (!validator.RunTapStress(300))
            {
                Fail(validator.LastError);
                skinController.ApplySkin(originalStage, false);
                yield break;
            }

            rigController.CancelAction();
            yield return null;

            CharacterRoutineController routineController =
                GetComponent<CharacterRoutineController>();
            bool routineWasEnabled =
                routineController != null && routineController.enabled;
            if (routineController != null)
            {
                routineController.enabled = false;
            }

            CharacterFacing[] directions =
            {
                CharacterFacing.Front,
                CharacterFacing.SideLeft,
                CharacterFacing.SideRight,
                CharacterFacing.Back
            };
            Vector2[] vectors =
            {
                Vector2.down,
                Vector2.left,
                Vector2.right,
                Vector2.up
            };
            RectTransform liveRoot = transform as RectTransform;
            if (liveRoot == null)
            {
                Fail("CharacterRoot is not a RectTransform.");
                if (routineController != null)
                {
                    routineController.enabled = routineWasEnabled;
                }
                skinController.ApplySkin(originalStage, false);
                yield break;
            }

            Vector2 originalPosition = liveRoot.anchoredPosition;
            rigController.ResetFootPlantDiagnostics();
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2 movementStart = liveRoot.anchoredPosition;
                rigController.SetLocomotion(vectors[i], 1f);
                for (int frame = 0; frame < 6; frame++)
                {
                    liveRoot.anchoredPosition +=
                        vectors[i] * 8f;
                    yield return null;
                }

                if (rigController.Facing != directions[i])
                {
                    Fail(
                        $"Directional travel failed for " +
                        directions[i] + ".");
                    liveRoot.anchoredPosition = originalPosition;
                    if (routineController != null)
                    {
                        routineController.enabled = routineWasEnabled;
                    }
                    skinController.ApplySkin(originalStage, false);
                    yield break;
                }

                Vector2 actualMovement =
                    liveRoot.anchoredPosition - movementStart;
                if (Vector2.Dot(
                        actualMovement.normalized,
                        vectors[i]) < 0.98f ||
                    actualMovement.magnitude < 40f)
                {
                    Fail(
                        $"CharacterRoot did not really cross the room toward " +
                        directions[i] + ".");
                    liveRoot.anchoredPosition = originalPosition;
                    if (routineController != null)
                    {
                        routineController.enabled = routineWasEnabled;
                    }
                    skinController.ApplySkin(originalStage, false);
                    yield break;
                }

                yield return new WaitForEndOfFrame();
                if (rigController.FootPlantError > 2.5f)
                {
                    Fail(
                        $"Foot planting drifted by " +
                        $"{rigController.FootPlantError:F2} units while " +
                        $"travelling {directions[i]}.");
                    liveRoot.anchoredPosition = originalPosition;
                    if (routineController != null)
                    {
                        routineController.enabled = routineWasEnabled;
                    }
                    skinController.ApplySkin(originalStage, false);
                    yield break;
                }
            }

            rigController.StopLocomotion(CharacterFacing.Front);
            liveRoot.anchoredPosition = originalPosition;
            yield return new WaitForSecondsRealtime(0.2f);

            RectTransform rootBone =
                rigController.GetBone("Bone.Root");
            RectTransform pelvis =
                rigController.GetBone("Bone.Pelvis");
            RectTransform spine =
                rigController.GetBone("Bone.Spine");
            RectTransform chest =
                rigController.GetBone("Bone.Chest");
            RectTransform head =
                rigController.GetBone("Bone.Head");
            RectTransform leftArm =
                rigController.GetBone("Bone.UpperArm.L");
            RectTransform rightLeg =
                rigController.GetBone("Bone.Thigh.R");
            Transform leftEyelid =
                rigController.VisualRoot.Find(
                    "Skeleton/Bone.Root/Bone.Pelvis/Bone.Spine/" +
                    "Bone.Chest/Bone.Neck/Bone.Head/FaceRig/Eyelid.L");
            CharacterFaceController faceController =
                GetComponent<CharacterFaceController>();
            if (rootBone == null ||
                pelvis == null ||
                spine == null ||
                chest == null ||
                head == null ||
                leftArm == null ||
                rightLeg == null ||
                leftEyelid == null ||
                faceController == null)
            {
                Fail(
                    "Independent-motion probes could not find every required " +
                    "body and face transform.");
                if (routineController != null)
                {
                    routineController.enabled = routineWasEnabled;
                }
                skinController.ApplySkin(originalStage, false);
                yield break;
            }

            Vector3 chestScaleBefore = chest.localScale;
            Vector2 rootPositionBefore = rootBone.anchoredPosition;
            yield return new WaitForSecondsRealtime(0.34f);
            bool breathingMoved =
                Vector3.Distance(
                    chest.localScale,
                    chestScaleBefore) > 0.002f ||
                Vector2.Distance(
                    rootBone.anchoredPosition,
                    rootPositionBefore) > 0.5f;

            Quaternion spineBefore = spine.localRotation;
            Quaternion armBefore = leftArm.localRotation;
            rigController.PlayAction(
                CharacterRoutineAction.Stretch,
                1.55f);
            yield return new WaitForSecondsRealtime(0.38f);
            bool upperBodyMoved =
                Quaternion.Angle(
                    leftArm.localRotation,
                    armBefore) > 2f &&
                Quaternion.Angle(
                    spine.localRotation,
                    spineBefore) > 0.5f;

            rigController.CancelAction();
            Quaternion pelvisBefore = pelvis.localRotation;
            Quaternion legBefore = rightLeg.localRotation;
            rigController.SetLocomotion(Vector2.right, 1f);
            yield return new WaitForSecondsRealtime(0.24f);
            bool lowerBodyMoved =
                Quaternion.Angle(
                    pelvis.localRotation,
                    pelvisBefore) > 0.4f &&
                Quaternion.Angle(
                    rightLeg.localRotation,
                    legBefore) > 2f;
            rigController.StopLocomotion(CharacterFacing.Front);

            Quaternion headBefore = head.localRotation;
            rigController.PlayAction(
                CharacterRoutineAction.LookAround,
                1.4f);
            yield return new WaitForSecondsRealtime(0.34f);
            bool headMoved =
                Quaternion.Angle(
                    head.localRotation,
                    headBefore) > 2f;

            rigController.CancelAction();
            faceController.ForceBlink(false);
            yield return new WaitForSecondsRealtime(0.18f);
            Vector3 eyelidBefore = leftEyelid.localScale;
            faceController.ForceBlink(false);
            yield return new WaitForSecondsRealtime(0.065f);
            bool faceMoved =
                Vector3.Distance(
                    leftEyelid.localScale,
                    eyelidBefore) > 0.2f;

            if (!breathingMoved ||
                !upperBodyMoved ||
                !lowerBodyMoved ||
                !headMoved ||
                !faceMoved)
            {
                Fail(
                    "Independent motion failed: " +
                    $"breathing={breathingMoved}, " +
                    $"upperBody={upperBodyMoved}, " +
                    $"lowerBody={lowerBodyMoved}, " +
                    $"head={headMoved}, face={faceMoved}.");
                if (routineController != null)
                {
                    routineController.enabled = routineWasEnabled;
                }
                skinController.ApplySkin(originalStage, false);
                yield break;
            }

            rigController.CancelAction();
            rigController.ResetObservedIdleActionHistory();
            if (routineController != null)
            {
                routineController.enabled = routineWasEnabled;
            }

            float idleDeadline = Time.unscaledTime + 60f;
            while (rigController.ObservedIdleActionCount < 3 &&
                   Time.unscaledTime < idleDeadline)
            {
                yield return null;
            }

            if (rigController.ObservedIdleActionCount < 3)
            {
                Fail(
                    "Fewer than three different idle actions appeared " +
                    "within 60 seconds.");
                skinController.ApplySkin(originalStage, false);
                yield break;
            }

            skinController.ApplySkin(originalStage, false);
            LastRunPassed = true;
            LastResult =
                "PASS: 100 visibility starts, 50 immediate + 50 animated " +
                "stage swaps, 300 taps, four-direction travel with planted " +
                "feet, independent bones and three idle actions.";
            Debug.Log(LastResult, this);
            suite = null;
        }

        private void Fail(string message)
        {
            LastRunPassed = false;
            LastResult = "FAIL: " + message;
            Debug.LogError(LastResult, this);
            suite = null;
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : target.AddComponent<T>();
        }
    }
}
