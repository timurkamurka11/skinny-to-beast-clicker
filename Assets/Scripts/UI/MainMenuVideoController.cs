using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SkinnyToBeast.UI
{
    [RequireComponent(typeof(VideoPlayer))]
    public class MainMenuVideoController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RawImage targetImage;
        [SerializeField] private VideoClip menuLoopClip;
        [SerializeField] private RenderTexture targetTexture;

        [Header("Playback")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool mute = true;

        [Header("Screen Fit")]
        [SerializeField] private bool stretchToPortraitScreen = true;

        private VideoPlayer videoPlayer;

        private void Awake()
        {
            SetupVideoPlayer();
        }

        private void Start()
        {
            if (playOnStart)
            {
                PlayLoop();
            }
        }

        private void OnValidate()
        {
            SetupVideoPlayer();
        }

        public void PlayLoop()
        {
            SetupVideoPlayer();

            if (videoPlayer == null || videoPlayer.clip == null)
            {
                Debug.LogWarning("Main menu video clip is missing. Put the video at Assets/Videos/MainMenuLoop.mp4 and rebuild the menu scene.");
                return;
            }

            videoPlayer.Stop();
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.prepareCompleted += OnPrepared;
        }

        private void SetupVideoPlayer()
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                return;
            }

            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = menuLoopClip;
            videoPlayer.playOnAwake = playOnStart;
            videoPlayer.isLooping = loop;
            videoPlayer.skipOnDrop = true;
            videoPlayer.audioOutputMode = mute ? VideoAudioOutputMode.None : VideoAudioOutputMode.Direct;
            videoPlayer.aspectRatio = stretchToPortraitScreen ? VideoAspectRatio.Stretch : VideoAspectRatio.FitInside;

            if (targetTexture != null)
            {
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = targetTexture;

                if (targetImage != null)
                {
                    targetImage.texture = targetTexture;
                    targetImage.color = Color.white;
                    targetImage.uvRect = new Rect(0f, 0f, 1f, 1f);
                }
            }
            else
            {
                videoPlayer.renderMode = VideoRenderMode.APIOnly;
            }
        }

        private void OnPrepared(VideoPlayer preparedPlayer)
        {
            if (targetImage != null && targetTexture == null)
            {
                targetImage.texture = preparedPlayer.texture;
                targetImage.color = Color.white;
                targetImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            }

            preparedPlayer.Play();
        }
    }
}
