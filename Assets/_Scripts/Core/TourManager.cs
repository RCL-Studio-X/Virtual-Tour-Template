using System;
using System.Collections;
using System.Collections.Generic;
using StudioX.VirtualTour.External.SimpleSRT;
using StudioX.VirtualTour.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using XRAccess.Chirp;

namespace StudioX.VirtualTour.Core
{
    /// <summary>
    /// Manages the lifecycle, playback and related audio/caption behavior for a virtual tour composed of 360 videos.
    /// </summary>
    public class TourManager : MonoBehaviour
    {
        /// <summary>
        /// Invoked when the active video index changes. Parameter is the new index.
        /// </summary>
        public Action<int> OnVideoChanged;

        /// <summary>
        /// Number of videos in the tour.
        /// </summary>
        public int VideoCount => tourVideos.Count;

        /// <summary>
        /// Current active video index.
        /// </summary>
        public int CurrentVideoIndex => _currentIndex;
        
        [Header("References")]
        [Tooltip("Optional XR origin whose Y rotation will be set when switching videos.")]
        public GameObject xrOrigin;
        
        [Tooltip("What the XR Origin's Y rotation will be when switching to each video. Each video that has no override will default to 80.")]
        public List<float> defaultRotations = new List<float>();

        [Header("Video Tour Settings")]
        [Tooltip("Video clips that comprise the tour.")]
        public List<VideoClip> tourVideos = new List<VideoClip>();

        [Tooltip("Index of the video to start from (0 = first).")]
        public int startIndex = 0;

        [Tooltip("Once the last video ends, automatically go back to the starting video.")]
        public bool restartTourAfterLastVideo = false;

        [Tooltip("Auto-advance to the next video after the current one finishes.")]
        public bool autoAdvanceVideo = false;

        [Tooltip("Allow the video's audio to play through the device/headset.")]
        public bool enableBackgroundAudio = true;

        [Range(0f, 1f)]
        [Tooltip("Volume used for the video's background audio when enabled.")]
        public float backgroundAudioVolume = 0.25f;

        [Header("Commentary Audio Settings")]
        [Tooltip("Optional commentary audio tracks that correspond to each video.")]
        public List<AudioClip> commentaryAudio = new List<AudioClip>();

        [Tooltip("Allow commentary audio to play.")]
        public bool enableCommentaryAudio = true;

        [Range(0f, 1f)]
        [Tooltip("Commentary audio volume.")]
        public float commentaryVolume = 1.0f;

        [Header("Captions")]
        [Tooltip("SRT/TextAsset captions for commentary audio tracks (same order as commentaryAudio).")]
        public List<TextAsset> commentaryCaptions = new List<TextAsset>();

        [Tooltip("Enable showing captions from loaded SRTs.")]
        public bool enableCaptions = true;

        // Caption handling (internal)
        private CaptionSource _captionSource;
        private readonly List<Coroutine> _runningCaptionCoroutines = new List<Coroutine>();

        [Header("Custom Background Audio Settings")]
        [Tooltip("Optional custom background audio tracks to override video audio.")]
        public List<AudioClip> customBackgroundAudio = new List<AudioClip>();

        [Tooltip("Enable using custom background audio tracks.")]
        public bool enableCustomBackgroundAudio = true;

        [Range(0f, 1f)]
        [Tooltip("Volume for custom background audio.")]
        public float customBackgroundVolume = 0.25f;

        [Header("UI Controls")]
        [Tooltip("Next video button.")]
        public Button nextButton;

        [Tooltip("Previous video button.")]
        public Button prevButton;

        [Tooltip("Home button (go to start).")]
        public Button homeButton;

        [Tooltip("Toggle to enable/disable commentary audio.")]
        public Toggle commentaryToggle;

        [Tooltip("Toggle to enable/disable captions.")]
        public Toggle captionsToggle;

        [Header("Optional Metadata")]
        [Tooltip("Optional custom display names for each video.")]
        public List<string> videoDisplayNames = new List<string>();

        [Tooltip("Optional title canvas manager used to show video titles.")]
        public TitleCanvasManager titleCanvasManager;

        // Private backing fields
        private int _currentIndex = 0;
        private bool _isSpawn = true;
        
        private VideoPlayer _videoPlayer;
        private AudioSource _customBackgroundAudioSource;
        private AudioSource _commentaryAudioSource;
        private RenderTexture _renderTexture;
        private Material _skyboxMaterial;

        /// <summary>
        /// Initialize display names, components and UI wiring.
        /// </summary>
        private void Awake()
        {
            InitializeVideoDisplayNames();
            SetupComponents();
            SetupUI();
        }

        /// <summary>
        /// Starts playback at the configured start index.
        /// </summary>
        private void Start() =>
            PlayVideoAtIndex(startIndex);

        /// <summary>
        /// Ensure <see cref="videoDisplayNames"/> contains sensible values for every video.
        /// </summary>
        private void InitializeVideoDisplayNames()
        {
            int count = tourVideos.Count;
            for (int i = 0; i < count; i++)
            {
                if (i < videoDisplayNames.Count && !string.IsNullOrEmpty(videoDisplayNames[i]))
                    videoDisplayNames[i] = GetVideoDisplayName(i);
                else if (i < videoDisplayNames.Count)
                    videoDisplayNames[i] = GetVideoDisplayName(i);
                else
                    videoDisplayNames.Add(GetVideoDisplayName(i));
            }
        }

        /// <summary>
        /// Set up required components such as VideoPlayer, AudioSources, CaptionSource, and skybox.
        /// </summary>
        private void SetupComponents()
        {
            // Skybox
            if (!RenderSettings.skybox)
            {
                _skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
                RenderSettings.skybox = _skyboxMaterial;
            }
            else
            {
                _skyboxMaterial = RenderSettings.skybox;
            }

            // VideoPlayer
            _videoPlayer = GetComponent<VideoPlayer>();
            if (!_videoPlayer)
                _videoPlayer = gameObject.AddComponent<VideoPlayer>();

            _videoPlayer.isLooping = true;
            _videoPlayer.loopPointReached += OnVideoEnd;
            _videoPlayer.playOnAwake = false;

            if (!enableBackgroundAudio)
                _videoPlayer.SetDirectAudioVolume(0, 0f);
            else
                _videoPlayer.SetDirectAudioVolume(0, backgroundAudioVolume);

            // Custom background audio source
            _customBackgroundAudioSource = gameObject.AddComponent<AudioSource>();
            _customBackgroundAudioSource.playOnAwake = false;
            _customBackgroundAudioSource.loop = true;
            _customBackgroundAudioSource.spatialBlend = 0f;
            _customBackgroundAudioSource.volume = customBackgroundVolume;

            // Commentary audio source
            _commentaryAudioSource = gameObject.AddComponent<AudioSource>();
            _commentaryAudioSource.playOnAwake = false;
            _commentaryAudioSource.loop = false;
            _commentaryAudioSource.spatialBlend = 0f;
            _commentaryAudioSource.volume = commentaryVolume;

            // CaptionSource
            _captionSource = GetComponent<CaptionSource>();
            if (!_captionSource)
                _captionSource = gameObject.AddComponent<CaptionSource>();

            _captionSource.audioSource = _commentaryAudioSource;
            _captionSource.boundingObject = gameObject;

            // Ensure default rotations list covers all videos
            int videoCount = tourVideos.Count;
            for (int i = defaultRotations.Count; i < videoCount; i++)
                defaultRotations.Add(80f);
        }

        /// <summary>
        /// Wire UI events to control methods.
        /// </summary>
        private void SetupUI()
        {
            if (nextButton)
                nextButton.onClick.AddListener(NextVideo);

            if (prevButton)
                prevButton.onClick.AddListener(PreviousVideo);

            if (homeButton)
                homeButton.onClick.AddListener(() => PlayVideoAtIndex(0));

            if (commentaryToggle)
                commentaryToggle.onValueChanged.AddListener(ToggleCommentaryAudio);

            if (captionsToggle)
                captionsToggle.onValueChanged.AddListener(ToggleCaptions);
        }

        /// <summary>
        /// Loads caption SRT and schedules caption display based on current playback position.
        /// </summary>
        /// <param name="srt">SRT file as a TextAsset.</param>
        /// <param name="currentTime">Current audio time (seconds) to offset scheduling.</param>
        private void LoadCaptionsFromSRT(TextAsset srt, double currentTime = 0)
        {
            ClearAllCaptions();

            if (!_captionSource || !srt)
                return;

            List<SubtitleBlock> subtitleBlocks = SRTParser.Load(srt);
            if (subtitleBlocks?.Count is 0)
                return;

            foreach (var subtitle in subtitleBlocks)
            {
                float start = (float)subtitle.From;
                float end = (float)subtitle.To;

                if (!(start >= currentTime))
                    continue;

                float delay = start - (float)currentTime;
                float duration = end - start;
                string text = subtitle.Text;

                Coroutine coroutine = StartCoroutine(PlayCaptionWithDelay(text, duration, delay));
                _runningCaptionCoroutines.Add(coroutine);
            }
        }

        /// <summary>
        /// Coroutine that waits for a delay then shows a timed caption.
        /// </summary>
        private IEnumerator PlayCaptionWithDelay(string text, float duration, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (enableCommentaryAudio && _captionSource)
                _captionSource.ShowTimedCaption(text, duration);
        }

        /// <summary>
        /// Request playback of a video by index. This sets up audio, captions, skybox and invokes events.
        /// </summary>
        /// <param name="index">Index of the video to play.</param>
        public void PlayVideoAtIndex(int index)
        {
            // Validate index early
            if (index < 0 || index >= tourVideos.Count)
            {
                // If there are no videos and index == 0, still warn accordingly
                if (index != 0)
                    Debug.LogWarning($"Video index out of range: {index}");
                else
                    Debug.LogWarning("No videos assigned to TourManager!");
                return;
            }

            // Configure toggles based on available assets
            bool hasCaptions = (index >= 0 && index < commentaryCaptions.Count) && commentaryCaptions[index];
            if (captionsToggle)
                captionsToggle.interactable = hasCaptions;

            if (hasCaptions && captionsToggle)
                captionsToggle.isOn = enableCaptions;

            bool hasCommentary = (index >= 0 && index < commentaryAudio.Count) && commentaryAudio[index];
            if (commentaryToggle)
                commentaryToggle.interactable = hasCommentary;

            if (hasCommentary && commentaryToggle)
                commentaryToggle.isOn = enableCommentaryAudio;

            // Rotate XR origin if provided (use default rotation fallback)
            if (xrOrigin)
            {
                float yRot = (index >= 0 && index < defaultRotations.Count) ? defaultRotations[index] : 80f;
                xrOrigin.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, yRot, transform.rotation.eulerAngles.z);
            }

            ClearAllCaptions();

            if (titleCanvasManager)
                titleCanvasManager.SetTitle(GetVideoDisplayName(index));

            _currentIndex = index;
            VideoClip clip = tourVideos[index];

            if (clip)
            {
                // Release previous render texture, then allocate new one sized to clip
                if (_renderTexture)
                {
                    _renderTexture.Release();
                    _renderTexture = null;
                }

                // Cast width/height to int once, allocate accordingly
                int width = (int)clip.width;
                int height = (int)clip.height;
                _renderTexture = new RenderTexture(width, height, 0);
                _renderTexture.Create();

                _videoPlayer.clip = clip;
                _videoPlayer.targetTexture = _renderTexture;
                _videoPlayer.Play();

                if (_skyboxMaterial)
                    _skyboxMaterial.SetTexture("_MainTex", _renderTexture);
            }

            // Audio handling: on first spawn we want to delay starting audio until the video is ready
            if (_isSpawn)
                StartCoroutine(DelayedStartAudio(index));
            else
            {
                HandleCustomBackgroundAudio(index);
                HandleCommentaryAudio(index);
            }

            UpdateButtonStates();
            OnVideoChanged?.Invoke(_currentIndex);
            _isSpawn = false;
        }

        /// <summary>
        /// Handler for when the VideoPlayer reaches loop point / end.
        /// </summary>
        private void OnVideoEnd(VideoPlayer vp)
        {
            // Decide what to do next based on current index and settings
            if (_currentIndex >= tourVideos.Count - 1 && restartTourAfterLastVideo)
            {
                PlayVideoAtIndex(startIndex);
                return;
            }

            if (autoAdvanceVideo)
                PlayVideoAtIndex(_currentIndex + 1);
        }

        /// <summary>
        /// Small delay used only to start audio after the first frame has been rendered.
        /// </summary>
        private IEnumerator DelayedStartAudio(int index)
        {
            yield return new WaitForEndOfFrame();
            HandleCustomBackgroundAudio(index);
            HandleCommentaryAudio(index);
        }

        /// <summary>
        /// Apply custom background audio for the provided index, or fallback to video audio based on settings.
        /// </summary>
        private void HandleCustomBackgroundAudio(int index)
        {
            _customBackgroundAudioSource.volume = customBackgroundVolume;

            if (!enableCustomBackgroundAudio)
            {
                _customBackgroundAudioSource.Stop();
                return;
            }

            bool hasCustom = (index >= 0 && index < customBackgroundAudio.Count) && customBackgroundAudio[index];
            if (hasCustom)
            {
                _customBackgroundAudioSource.clip = customBackgroundAudio[index];
                _customBackgroundAudioSource.Play();
                _videoPlayer.SetDirectAudioVolume(0, 0f);
                return;
            }

            _customBackgroundAudioSource.Stop();

            // If no custom audio available, allow the video's audio to play or silence it
            if (!enableBackgroundAudio)
                _videoPlayer.SetDirectAudioVolume(0, 0f);
            else
                _videoPlayer.SetDirectAudioVolume(0, backgroundAudioVolume);
        }

        /// <summary>
        /// Play or stop commentary audio based on current settings and load captions when enabled.
        /// </summary>
        private void HandleCommentaryAudio(int index)
        {
            _commentaryAudioSource.volume = commentaryVolume;

            bool shouldLoadCaptions = (index >= 0 && index < commentaryCaptions.Count) && commentaryCaptions[index] && enableCaptions;
            if (shouldLoadCaptions)
                LoadCaptionsFromSRT(commentaryCaptions[index]);

            if (!enableCommentaryAudio)
            {
                _commentaryAudioSource.Stop();
                return;
            }

            bool hasCommentary = (index >= 0 && index < commentaryAudio.Count) && commentaryAudio[index];
            if (hasCommentary)
            {
                _commentaryAudioSource.clip = commentaryAudio[index];
                _commentaryAudioSource.Play();
                return;
            }

            _commentaryAudioSource.Stop();
        }

        /// <summary>
        /// Toggle commentary audio on/off from UI.
        /// </summary>
        /// <param name="value">New toggle value.</param>
        private void ToggleCommentaryAudio(bool value)
        {
            enableCommentaryAudio = value;

            if (!enableCommentaryAudio)
            {
                _commentaryAudioSource.Stop();
                ClearAllCaptions();
                return;
            }

            HandleCommentaryAudio(_currentIndex);
        }

        /// <summary>
        /// Toggle captions on/off from UI and (re)load captions if necessary.
        /// </summary>
        /// <param name="value">New toggle value.</param>
        private void ToggleCaptions(bool value)
        {
            enableCaptions = value;

            if (!enableCaptions)
            {
                ClearAllCaptions();
                return;
            }

            if (commentaryCaptions?.Count > _currentIndex && commentaryCaptions[_currentIndex] && _commentaryAudioSource.clip)
            {
                double currentTime = _commentaryAudioSource.time;
                LoadCaptionsFromSRT(commentaryCaptions[_currentIndex], currentTime);
            }
        }

        /// <summary>
        /// Enable/disable UI buttons based on current index and content.
        /// </summary>
        private void UpdateButtonStates()
        {
            if (prevButton)
                prevButton.interactable = _currentIndex > 0;

            if (nextButton)
                nextButton.interactable = _currentIndex < tourVideos.Count - 1;

            if (homeButton)
                homeButton.interactable = _currentIndex != 0;
        }

        /// <summary>
        /// Play next video if available.
        /// </summary>
        private void NextVideo()
        {
            if (_currentIndex + 1 < tourVideos.Count)
                PlayVideoAtIndex(_currentIndex + 1);
        }

        /// <summary>
        /// Play previous video if available.
        /// </summary>
        private void PreviousVideo()
        {
            if (_currentIndex - 1 >= 0)
                PlayVideoAtIndex(_currentIndex - 1);
        }

        /// <summary>
        /// Return the current index (same as <see cref="CurrentVideoIndex"/>).
        /// </summary>
        /// <returns>Current video index.</returns>
        public int GetCurrentIndex() => _currentIndex;

        /// <summary>
        /// Returns a display name for the video at index. Prioritizes explicit display names, then clip names, then a fallback label.
        /// </summary>
        /// <param name="index">Index of the video.</param>
        /// <returns>Display name for the video.</returns>
        private string GetVideoDisplayName(int index)
        {
            if (videoDisplayNames?.Count > index && !string.IsNullOrEmpty(videoDisplayNames[index]))
                return videoDisplayNames[index];

            if (tourVideos?.Count > index && tourVideos[index])
                return tourVideos[index].name;

            return $"Video {index + 1}";
        }

        /// <summary>
        /// Stops and clears scheduled caption coroutines and clears caption renderers.
        /// </summary>
        private void ClearAllCaptions()
        {
            for (int i = _runningCaptionCoroutines.Count - 1; i >= 0; i--)
            {
                Coroutine c = _runningCaptionCoroutines[i];
                if (c != null)
                    StopCoroutine(c);
            }
            _runningCaptionCoroutines.Clear();

            if (_captionSource && CaptionRenderManager.Instance && CaptionRenderManager.Instance.currentRenderer)
                CaptionRenderManager.Instance.ClearCaptions();
        }

        /// <summary>
        /// Unsubscribe event handlers when destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (_videoPlayer)
                _videoPlayer.loopPointReached -= OnVideoEnd;
        }
    }
}
