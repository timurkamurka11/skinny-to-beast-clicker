using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Production visual pass for GeneratedFatManRigActor.
    ///
    /// The independent 69-bone actor and all animation logic remain untouched.
    /// This component replaces only the temporary red-striped prototype look:
    /// proportions, palette, clothing, face, hair and surface details are changed
    /// at runtime on the already-skinned meshes. No intact full-body PNG is used.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(13000)]
    public sealed class GeneratedFatManProductionSkin : MonoBehaviour
    {
        private static readonly Color Ink =
            new Color(0.075f, 0.047f, 0.035f, 1f);
        private static readonly Color SkinWarm =
            new Color(0.88f, 0.52f, 0.39f, 1f);
        private static readonly Color SkinLight =
            new Color(0.96f, 0.65f, 0.51f, 1f);
        private static readonly Color SkinShadow =
            new Color(0.67f, 0.31f, 0.24f, 1f);
        private static readonly Color SkinSoftShadow =
            new Color(0.49f, 0.20f, 0.16f, 0.30f);
        private static readonly Color Tank =
            new Color(0.35f, 0.36f, 0.34f, 1f);
        private static readonly Color TankLight =
            new Color(0.43f, 0.44f, 0.41f, 1f);
        private static readonly Color TankShadow =
            new Color(0.19f, 0.20f, 0.19f, 1f);
        private static readonly Color TankFold =
            new Color(0.10f, 0.105f, 0.10f, 0.32f);
        private static readonly Color TankStain =
            new Color(0.12f, 0.11f, 0.085f, 0.23f);
        private static readonly Color Shorts =
            new Color(0.13f, 0.145f, 0.16f, 1f);
        private static readonly Color ShortsLight =
            new Color(0.22f, 0.235f, 0.25f, 1f);
        private static readonly Color ShortsShadow =
            new Color(0.055f, 0.065f, 0.075f, 1f);
        private static readonly Color Shoe =
            new Color(0.095f, 0.14f, 0.16f, 1f);
        private static readonly Color ShoeLight =
            new Color(0.20f, 0.25f, 0.25f, 1f);
        private static readonly Color Sole =
            new Color(0.20f, 0.19f, 0.17f, 1f);
        private static readonly Color Hair =
            new Color(0.105f, 0.063f, 0.043f, 1f);
        private static readonly Color HairLight =
            new Color(0.22f, 0.12f, 0.075f, 1f);
        private static readonly Color FaceDark =
            new Color(0.10f, 0.055f, 0.04f, 1f);
        private static readonly Color EyeWhite =
            new Color(0.92f, 0.90f, 0.82f, 1f);

        private readonly List<EyeDetail> eyeDetails = new();
        private GeneratedFatManRigActor actor;
        private GeneratedFatManAssetScope assets;
        private bool applied;

        public bool IsApplied => applied;

        private void Awake()
        {
            actor = GetComponent<GeneratedFatManRigActor>();
        }

        private void LateUpdate()
        {
            if (!applied)
            {
                actor ??= GetComponent<GeneratedFatManRigActor>();
                if (actor != null && actor.IsReady)
                {
                    ApplyProductionSkin();
                }
                return;
            }

            for (int i = 0; i < eyeDetails.Count; i++)
            {
                EyeDetail detail = eyeDetails[i];
                if (detail.White != null && detail.Pupil != null)
                {
                    detail.White.SetActive(detail.Pupil.activeSelf);
                }
            }
        }

        private void ApplyProductionSkin()
        {
            assets = new GeneratedFatManAssetScope();
            ApplyView(transform.Find("Front.View"), SkinView.Front);
            ApplyView(transform.Find("Side.View"), SkinView.Side);
            ApplyView(transform.Find("Back.View"), SkinView.Back);
            applied = true;

            Debug.Log(
                "Fat Man Production Skin 3.9 active: grey stretched tank, " +
                "worn dark shorts, house shoes, adult face, short neck, " +
                "double chin, folds, stains and shading over the independent " +
                "bone rig.",
                this);
        }

        private void ApplyView(Transform view, SkinView kind)
        {
            if (view == null)
            {
                throw new InvalidOperationException(
                    "Production skin could not find the " + kind + " view rig.");
            }

            SkinnedMeshRenderer[] renderers =
                view.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                string key = BaseName(renderer.gameObject.name);
                if (IsRemovedPrototypeDecoration(key))
                {
                    renderer.gameObject.SetActive(false);
                    continue;
                }

                Recolor(renderer, key, kind);
                Reshape(renderer, key, kind);
            }

            DisablePrototypeHairSpikes(view);
            BuildHair(view, kind);
            BuildClothingDetails(view, kind);
            BuildBodyDetails(view, kind);
            if (kind != SkinView.Back)
            {
                BuildFaceDetails(view, kind);
            }
            else
            {
                BuildBackDetails(view);
            }
        }

        private static bool IsRemovedPrototypeDecoration(string key)
        {
            return key.StartsWith("Shirt.Stripe.", StringComparison.Ordinal) ||
                   key == "Badge";
        }

        private static string BaseName(string value)
        {
            const string suffix = ".Outline";
            return value.EndsWith(suffix, StringComparison.Ordinal)
                ? value.Substring(0, value.Length - suffix.Length)
                : value;
        }

        private static bool IsOutline(string value)
        {
            return value.EndsWith(".Outline", StringComparison.Ordinal);
        }

        private static void Recolor(
            SkinnedMeshRenderer renderer,
            string key,
            SkinView kind)
        {
            if (renderer.sharedMaterial == null)
            {
                return;
            }

            Color color;
            if (IsOutline(renderer.gameObject.name))
            {
                color = Ink;
            }
            else if (key == "Shirt.Main" || key == "Back.Shirt")
            {
                color = kind == SkinView.Side ? TankShadow : Tank;
            }
            else if (key == "Shirt.Hem")
            {
                color = TankShadow;
            }
            else if (key == "Shorts.Pelvis")
            {
                color = Shorts;
            }
            else if (key == "Shorts.Highlight")
            {
                color = ShortsLight;
            }
            else if (key.StartsWith("Foot.", StringComparison.Ordinal))
            {
                color = Shoe;
            }
            else if (key.StartsWith("Sole.", StringComparison.Ordinal))
            {
                color = Sole;
            }
            else if (key.StartsWith("Hair.", StringComparison.Ordinal) ||
                     key.StartsWith("Brow", StringComparison.Ordinal))
            {
                color = Hair;
            }
            else if (key.StartsWith("Eye.", StringComparison.Ordinal) ||
                     key.StartsWith("Mouth.", StringComparison.Ordinal))
            {
                color = FaceDark;
            }
            else if (key.StartsWith("Sweat.", StringComparison.Ordinal))
            {
                color = new Color(0.73f, 0.90f, 1f, 0.72f);
            }
            else if (key == "Neck" ||
                     key.StartsWith("Thigh.L", StringComparison.Ordinal) ||
                     key.StartsWith("Shin.L", StringComparison.Ordinal) ||
                     key.StartsWith("UpperArm.L", StringComparison.Ordinal) ||
                     key.StartsWith("Forearm.L", StringComparison.Ordinal))
            {
                color = kind == SkinView.Side ? SkinShadow : SkinWarm;
            }
            else if (key == "Head" || key.StartsWith("Hand", StringComparison.Ordinal))
            {
                color = SkinLight;
            }
            else
            {
                color = SkinWarm;
            }

            SetMaterialColor(renderer.sharedMaterial, color);
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void Reshape(
            SkinnedMeshRenderer renderer,
            string key,
            SkinView kind)
        {
            Vector2 scale = Vector2.one;
            Vector2 offset = Vector2.zero;

            if (key == "Body.Belly")
            {
                scale = kind == SkinView.Side
                    ? new Vector2(1.16f, 1.10f)
                    : new Vector2(1.20f, 1.11f);
                offset = new Vector2(kind == SkinView.Side ? 0.06f : 0f, -0.10f);
            }
            else if (key == "Shirt.Main" || key == "Back.Shirt")
            {
                scale = kind == SkinView.Side
                    ? new Vector2(1.10f, 1.08f)
                    : new Vector2(1.14f, 1.08f);
                offset = new Vector2(0f, -0.035f);
            }
            else if (key == "Shirt.Hem")
            {
                scale = new Vector2(1.16f, 1.18f);
                offset = new Vector2(0f, -0.075f);
            }
            else if (key == "Shorts.Pelvis")
            {
                scale = new Vector2(1.10f, 1.07f);
                offset = new Vector2(0f, -0.025f);
            }
            else if (key == "Shorts.Highlight")
            {
                scale = new Vector2(1.08f, 0.85f);
            }
            else if (key.StartsWith("UpperArm", StringComparison.Ordinal))
            {
                scale = new Vector2(1.15f, 1.02f);
            }
            else if (key.StartsWith("Forearm", StringComparison.Ordinal))
            {
                scale = new Vector2(1.10f, 1.01f);
            }
            else if (key.StartsWith("Hand", StringComparison.Ordinal))
            {
                scale = new Vector2(0.88f, 0.90f);
            }
            else if (key.StartsWith("Thigh", StringComparison.Ordinal))
            {
                scale = new Vector2(1.16f, 1.02f);
            }
            else if (key.StartsWith("Shin", StringComparison.Ordinal))
            {
                scale = new Vector2(1.09f, 1.00f);
            }
            else if (key.StartsWith("Foot", StringComparison.Ordinal) ||
                     key.StartsWith("Sole", StringComparison.Ordinal))
            {
                scale = new Vector2(1.04f, 0.91f);
            }
            else if (key == "Head")
            {
                scale = new Vector2(0.94f, 0.98f);
                offset = new Vector2(0f, -0.02f);
            }
            else if (key == "Neck")
            {
                scale = new Vector2(1.18f, 0.82f);
                offset = new Vector2(0f, -0.06f);
            }
            else if (key == "Chin")
            {
                scale = new Vector2(1.20f, 1.20f);
                offset = new Vector2(0f, -0.045f);
            }
            else if (key == "Hair.Base")
            {
                scale = new Vector2(0.99f, 0.76f);
                offset = new Vector2(0f, -0.10f);
            }
            else if (key.Contains("Eye.") && key.EndsWith(".Open", StringComparison.Ordinal))
            {
                scale = new Vector2(0.58f, 0.68f);
            }
            else if (key.Contains("Eye.") && key.EndsWith(".Closed", StringComparison.Ordinal))
            {
                scale = new Vector2(0.88f, 0.80f);
            }
            else if (key.StartsWith("Brow", StringComparison.Ordinal))
            {
                scale = new Vector2(0.88f, 0.82f);
                offset = new Vector2(0f, -0.015f);
            }
            else if (key.StartsWith("Mouth.", StringComparison.Ordinal))
            {
                scale = new Vector2(1.10f, 0.92f);
                offset = new Vector2(0f, -0.035f);
            }

            if (scale != Vector2.one || offset != Vector2.zero)
            {
                TransformMesh(renderer, scale, offset);
            }
        }

        private static void TransformMesh(
            SkinnedMeshRenderer renderer,
            Vector2 scale,
            Vector2 offset)
        {
            Mesh mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            Vector3[] vertices = mesh.vertices;
            Vector3 center = mesh.bounds.center;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 delta = vertices[i] - center;
                vertices[i] = new Vector3(
                    center.x + delta.x * scale.x + offset.x,
                    center.y + delta.y * scale.y + offset.y,
                    vertices[i].z);
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        private static void DisablePrototypeHairSpikes(Transform view)
        {
            SkinnedMeshRenderer[] renderers =
                view.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null &&
                    renderers[i].gameObject.name.StartsWith(
                        "Hair.Spike.",
                        StringComparison.Ordinal))
                {
                    renderers[i].gameObject.SetActive(false);
                }
            }
        }

        private void BuildHair(Transform view, SkinView kind)
        {
            Transform head = FindDeep(view, "Head");
            if (head == null)
            {
                return;
            }

            Vector2 point = BonePoint(view, head);
            int tuftCount = kind == SkinView.Side ? 4 : 6;
            for (int i = 0; i < tuftCount; i++)
            {
                float t = tuftCount == 1 ? 0f : i / (float)(tuftCount - 1);
                float x = kind == SkinView.Side
                    ? Mathf.Lerp(-0.48f, 0.52f, t)
                    : Mathf.Lerp(-0.66f, 0.66f, t);
                float rise = 0.70f + Mathf.Sin(t * Mathf.PI) * 0.10f;
                float lean = (i % 2 == 0 ? -1f : 1f) * 0.055f;
                Vector2[] points =
                {
                    point + new Vector2(x - 0.16f, rise - 0.18f),
                    point + new Vector2(x + lean, rise + 0.24f + (i % 3) * 0.035f),
                    point + new Vector2(x + 0.18f, rise - 0.17f)
                };
                CreatePolygon(
                    view,
                    head,
                    "ProductionHair.Tuft." + i,
                    points,
                    i % 2 == 0 ? Hair : HairLight,
                    47 + i % 2);
            }
        }

        private void BuildClothingDetails(Transform view, SkinView kind)
        {
            Transform chest = FindDeep(view, "Chest");
            Transform belly = FindDeep(view, "Belly");
            Transform pelvis = FindDeep(view, "Pelvis");
            if (chest == null || belly == null || pelvis == null)
            {
                return;
            }

            Vector2 chestPoint = BonePoint(view, chest);
            Vector2 bellyPoint = BonePoint(view, belly);
            Vector2 pelvisPoint = BonePoint(view, pelvis);
            float sideX = kind == SkinView.Side ? 0.34f : 0f;

            Vector2 neckCenter = chestPoint + new Vector2(sideX, 0.52f);
            CreateEllipse(view, chest, "Tank.Neckline.Shadow", neckCenter,
                kind == SkinView.Side ? new Vector2(0.74f, 0.56f) : new Vector2(1.08f, 0.58f),
                TankShadow, 27);
            CreateEllipse(view, chest, "Tank.Neckline.Skin",
                neckCenter + new Vector2(kind == SkinView.Side ? 0.08f : 0f, 0.035f),
                kind == SkinView.Side ? new Vector2(0.58f, 0.42f) : new Vector2(0.88f, 0.42f),
                kind == SkinView.Back ? SkinWarm : SkinLight, 28);

            for (int i = 0; i < 3; i++)
            {
                float y = 0.22f - i * 0.34f;
                float width = (kind == SkinView.Side ? 1.38f : 2.10f) - i * 0.10f;
                CreateEllipse(view, belly, "Tank.Fold." + i,
                    bellyPoint + new Vector2(sideX * 0.38f, y),
                    new Vector2(width, 0.075f), TankFold, 29 + i);
            }

            CreateEllipse(view, belly, "Tank.Stain.A",
                bellyPoint + new Vector2(kind == SkinView.Side ? 0.52f : -0.48f, 0.38f),
                new Vector2(0.42f, 0.27f), TankStain, 31);
            CreateEllipse(view, belly, "Tank.Stain.B",
                bellyPoint + new Vector2(kind == SkinView.Side ? 0.72f : 0.56f, -0.18f),
                new Vector2(0.30f, 0.20f), TankStain, 31);

            CreateEllipse(view, pelvis, "Shorts.Waistband",
                pelvisPoint + new Vector2(kind == SkinView.Side ? 0.12f : 0f, 0.24f),
                kind == SkinView.Side ? new Vector2(2.08f, 0.16f) : new Vector2(2.48f, 0.17f),
                ShortsShadow, 32);

            if (kind == SkinView.Front)
            {
                CreateEllipse(view, pelvis, "Shorts.Drawstring.L",
                    pelvisPoint + new Vector2(-0.09f, 0.05f),
                    new Vector2(0.055f, 0.38f), ShortsLight, 33);
                CreateEllipse(view, pelvis, "Shorts.Drawstring.R",
                    pelvisPoint + new Vector2(0.09f, 0.05f),
                    new Vector2(0.055f, 0.38f), ShortsLight, 33);
            }
        }

        private void BuildBodyDetails(Transform view, SkinView kind)
        {
            Transform chin = FindDeep(view, "ChinSoft");
            Transform shinL = FindDeep(view, "Shin.L");
            Transform shinR = FindDeep(view, "Shin.R");
            Transform footL = FindDeep(view, "Foot.L");
            Transform footR = FindDeep(view, "Foot.R");

            if (chin != null)
            {
                Vector2 chinPoint = BonePoint(view, chin);
                CreateEllipse(view, chin, "Chin.Fold",
                    chinPoint + new Vector2(kind == SkinView.Side ? 0.24f : 0f, -0.17f),
                    kind == SkinView.Side ? new Vector2(0.72f, 0.065f) : new Vector2(0.88f, 0.065f),
                    SkinSoftShadow, 45);
                CreateEllipse(view, chin, "Chin.Stubble",
                    chinPoint + new Vector2(kind == SkinView.Side ? 0.25f : 0f, -0.01f),
                    kind == SkinView.Side ? new Vector2(0.65f, 0.24f) : new Vector2(0.82f, 0.24f),
                    new Color(0.16f, 0.10f, 0.075f, 0.12f), 44);
            }

            AddKneeShadow(view, shinL, "Knee.Shadow.L");
            AddKneeShadow(view, shinR, "Knee.Shadow.R");
            AddShoeDetail(view, footL, "Shoe.Detail.L", kind);
            AddShoeDetail(view, footR, "Shoe.Detail.R", kind);
        }

        private void AddKneeShadow(Transform view, Transform shin, string name)
        {
            if (shin == null)
            {
                return;
            }
            Vector2 point = BonePoint(view, shin);
            CreateEllipse(view, shin, name, point + new Vector2(0f, 0.46f),
                new Vector2(0.47f, 0.18f),
                new Color(SkinShadow.r, SkinShadow.g, SkinShadow.b, 0.26f), 39);
        }

        private void AddShoeDetail(
            Transform view,
            Transform foot,
            string name,
            SkinView kind)
        {
            if (foot == null)
            {
                return;
            }
            Vector2 point = BonePoint(view, foot);
            CreateEllipse(view, foot, name,
                point + new Vector2(kind == SkinView.Side ? 0.30f : 0f, -0.22f),
                kind == SkinView.Side ? new Vector2(0.78f, 0.10f) : new Vector2(0.82f, 0.10f),
                ShoeLight, 43);
        }

        private void BuildFaceDetails(Transform view, SkinView kind)
        {
            Transform head = FindDeep(view, "Head");
            if (head == null)
            {
                return;
            }

            Vector2 point = BonePoint(view, head);
            if (kind == SkinView.Side)
            {
                CreateEyeWhite(view, head, point + new Vector2(0.48f, 0.10f),
                    new Vector2(0.27f, 0.20f), "Eye.L.Open");
                CreateEllipse(view, head, "Face.Cheek",
                    point + new Vector2(0.46f, -0.15f),
                    new Vector2(0.38f, 0.27f),
                    new Color(0.78f, 0.28f, 0.20f, 0.15f), 49);
                CreateEllipse(view, head, "Face.NoseShadow",
                    point + new Vector2(0.72f, -0.015f),
                    new Vector2(0.16f, 0.19f), SkinShadow, 53);
            }
            else
            {
                CreateEyeWhite(view, head, point + new Vector2(-0.30f, 0.10f),
                    new Vector2(0.29f, 0.20f), "Eye.L.Open");
                CreateEyeWhite(view, head, point + new Vector2(0.30f, 0.10f),
                    new Vector2(0.29f, 0.20f), "Eye.R.Open");
                CreateEllipse(view, head, "Face.Nose",
                    point + new Vector2(0f, -0.035f),
                    new Vector2(0.20f, 0.26f), SkinShadow, 53);
                CreateEllipse(view, head, "Face.Cheek.L",
                    point + new Vector2(-0.49f, -0.14f),
                    new Vector2(0.38f, 0.25f),
                    new Color(0.78f, 0.28f, 0.20f, 0.13f), 49);
                CreateEllipse(view, head, "Face.Cheek.R",
                    point + new Vector2(0.49f, -0.14f),
                    new Vector2(0.38f, 0.25f),
                    new Color(0.78f, 0.28f, 0.20f, 0.13f), 49);
            }
        }

        private void CreateEyeWhite(
            Transform view,
            Transform head,
            Vector2 center,
            Vector2 size,
            string pupilName)
        {
            SkinnedMeshRenderer renderer = CreateEllipse(
                view, head, "Production." + pupilName + ".White",
                center, size, EyeWhite, 51);
            Transform pupil = FindDeep(view, pupilName);
            if (renderer != null && pupil != null)
            {
                eyeDetails.Add(new EyeDetail(renderer.gameObject, pupil.gameObject));
            }
        }

        private void BuildBackDetails(Transform view)
        {
            Transform chest = FindDeep(view, "Chest");
            Transform belly = FindDeep(view, "Belly");
            if (chest == null || belly == null)
            {
                return;
            }

            Vector2 chestPoint = BonePoint(view, chest);
            Vector2 bellyPoint = BonePoint(view, belly);
            CreateEllipse(view, chest, "Back.Sweat.A",
                chestPoint + new Vector2(-0.38f, 0.05f),
                new Vector2(0.46f, 0.70f),
                new Color(0.09f, 0.10f, 0.09f, 0.14f), 30);
            CreateEllipse(view, belly, "Back.Sweat.B",
                bellyPoint + new Vector2(0.33f, 0.18f),
                new Vector2(0.40f, 0.56f),
                new Color(0.09f, 0.10f, 0.09f, 0.12f), 30);
        }

        private SkinnedMeshRenderer CreateEllipse(
            Transform view,
            Transform bone,
            string name,
            Vector2 center,
            Vector2 size,
            Color color,
            int order)
        {
            SkinnedMeshRenderer renderer =
                GeneratedFatManMeshFactory.CreateEllipse(
                    view,
                    name,
                    center,
                    size,
                    new[] { bone },
                    _ => GeneratedFatManMeshFactory.RigidWeight(),
                    color,
                    order,
                    assets,
                    24,
                    4);
            renderer.gameObject.layer = view.gameObject.layer;
            return renderer;
        }

        private void CreatePolygon(
            Transform view,
            Transform bone,
            string name,
            Vector2[] points,
            Color color,
            int order)
        {
            SkinnedMeshRenderer renderer =
                GeneratedFatManMeshFactory.CreatePolygon(
                    view,
                    name,
                    points,
                    bone,
                    color,
                    order,
                    assets);
            renderer.gameObject.layer = view.gameObject.layer;
        }

        private static Vector2 BonePoint(Transform view, Transform bone)
        {
            Vector3 point = view.InverseTransformPoint(bone.position);
            return new Vector2(point.x, point.y);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }
            if (root.name == name)
            {
                return root;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private void OnDestroy()
        {
            assets?.Dispose();
            assets = null;
            eyeDetails.Clear();
        }

        private enum SkinView
        {
            Front,
            Side,
            Back
        }

        private readonly struct EyeDetail
        {
            public readonly GameObject White;
            public readonly GameObject Pupil;

            public EyeDetail(GameObject white, GameObject pupil)
            {
                White = white;
                Pupil = pupil;
            }
        }
    }

    /// <summary>
    /// Installs the production visual skin after GeneratedFatManRigActor is born.
    /// Kept separate from the bone-rig bootstrap so replacing skins never changes
    /// gameplay, menu, entry flow or animation ownership.
    /// </summary>
    [DefaultExecutionOrder(-31950)]
    internal sealed class GeneratedFatManProductionSkinBootstrap : MonoBehaviour
    {
        private static GeneratedFatManProductionSkinBootstrap instance;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (instance != null)
            {
                return;
            }

            GameObject host =
                new GameObject("FatManProductionSkin3_9.Bootstrap");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<
                GeneratedFatManProductionSkinBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            GeneratedFatManRigActor[] actors =
                Resources.FindObjectsOfTypeAll<GeneratedFatManRigActor>();
            for (int i = 0; i < actors.Length; i++)
            {
                GeneratedFatManRigActor candidate = actors[i];
                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    candidate.GetComponent<GeneratedFatManProductionSkin>() != null)
                {
                    continue;
                }

                candidate.gameObject.AddComponent<
                    GeneratedFatManProductionSkin>();
            }
        }
    }
}
