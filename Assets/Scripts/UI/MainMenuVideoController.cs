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

        [Header("Playback")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool mute = true;

        private VideoPlayer videoPlayer;

        private void Awake()
        {
            videoPlayer = GetComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = loop;
            videoPlayer.renderMode = VideoRenderMode.APIOnly;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.clip = menuLoopClip;

            if (mute)
            {
                videoPlayer.SetDirectAudioMute(0, true);
            }
        }

        private void Start()
        {
            if (!playOnStart)
            {
                return;
            }

            PlayLoop();
        }

        public void PlayLoop()
        {
            if (videoPlayer.clip == null)
            {
                Debug.LogWarning("Main menu video clip is missing. Put the video at Assets/Videos/MainMenuLoop.mp4 and rebuild the menu scene.");
                return;
            }

            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.Prepare();
        }

        private void OnPrepared(VideoPlayer preparedPlayer)
        {
            if (targetImage != null)
            {
                targetImage.texture = preparedPlayer.texture;
            }

            preparedPlayer.Play();
        }
    }
}
