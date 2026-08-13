using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Test Runner creates an empty InitTestScene without a camera. The real
    /// gameplay room is a Screen Space Overlay Canvas, but Unity still draws
    /// its "No cameras rendering" diagnostic over that Canvas. A render-empty
    /// camera keeps the review surface clean without touching production scenes.
    /// </summary>
    internal static class Patch4PreviewSceneCamera
    {
        private const string CameraName = "Patch4PreviewSceneCamera";

        public static Camera EnsureActiveCamera()
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (candidate != null &&
                    candidate.enabled &&
                    candidate.gameObject.activeInHierarchy &&
                    candidate.targetDisplay == 0)
                {
                    return candidate;
                }
            }

            GameObject host = new(CameraName, typeof(Camera))
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave
            };
            Object.DontDestroyOnLoad(host);
            Camera camera = host.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            camera.depth = -100f;
            camera.targetDisplay = 0;
            camera.enabled = true;
            return camera;
        }
    }
}
