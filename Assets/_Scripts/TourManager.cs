using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using XRAccess.Chirp;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Controls playback and associated systems for a video tour (background audio, commentary, captions, UI).
/// </summary>
public class TourManager : MonoBehaviour
{
    #region Events & Properties

    /// <summary>
    /// Raised when the active video index changes. Parameter is the new index.
    /// </summary>
    public System.Action<int> OnVideoChanged;

    /// <summary>
    /// Number of videos in the tour.
    /// </summary>
    public int VideoCount => tourVideos.Count;

    /// <summary>
    /// Current active video index.
    /// </summary>
    public int CurrentVideoIndex => _currentIndex;

    #endregion

    #region Inspector Fields

    /// <summary>
    /// Root of the XR origin (used to rotate the user to the video's preferred heading).
    /// </summary>
    [Header("XR")]
    [Tooltip("Root of the XR origin (used to rotate the user to the video's preferred heading).")]
    public GameObject xrOrigin;

    /// <summary>
    /// Per-video default Y-rotation for the XR origin. Unassigned entries default to 80.
    /// </summary>
    [Tooltip("What the XR Origin's Y rotation will be when switching to each video. Each video not assigned a rotation value will default to 80.")]
    public List<float> defaultRotations = new List<float>();

    [Header("Video Tour Settings")]
    /// <summary>
    /// Video clips used by the tour (order matters).
    /// </summary>
    [Tooltip("Video clips used by the tour (order matters).")]
    public List<VideoClip> tourVideos = new List<VideoClip>();

    /// <summary>
    /// Index of the video to play first.
    /// </summary>
    [Tooltip("Specify which video is played first by index number. 0 is the first video, 1 is the second video, etc...")]
    public int startIndex = 0;

    /// <summary>
    /// When true, after the last video ends the tour restarts from <see cref="startIndex"/>.
    /// </summary>
    [Tooltip("Once the last video ends, automatically go back to the starting video.")]
    public bool restartTourAfterLastVideo = false;

    /// <summary>
    /// When true, automatically advance to the next video when the current video finishes.
    /// </summary>
    [Tooltip("Auto Advance to next video once the current video finishes playing.")]
    public bool autoAdvanceVideo = false;

    /// <summary>
    /// When true, allow the video's audio to be played directly to the headset.
    /// </summary>
    [Tooltip("Enabling this will allow the audio in the video to be played directly to the headset.")]
    public bool enableBackgroundAudio = true;

    /// <summary>
    /// Volume used for the video's direct audio output (0..1).
    /// </summary>
    [Range(0f, 1f)]
    [Tooltip("Volume used for the video's direct audio output (0..1).")]
    public float backgroundAudioVolume = 0.25f;

    [Header("Commentary Audio Settings")]
    /// <summary>
    /// Optional commentary audio clips (aligned by index with <see cref="tourVideos"/>).
    /// </summary>
    [Tooltip("Optional commentary audio clips (aligned by index with tourVideos).")]
    public List<AudioClip> commentaryAudio = new List<AudioClip>();

    /// <summary>
    /// When true, commentary audio is allowed to play.
    /// </summary>
    [Tooltip("When true, commentary audio is allowed to play.")]
    public bool enableCommentaryAudio = true;

    /// <summary>
    /// Playback volume for commentary audio (0..1).
    /// </summary>
    [Range(0f, 1f)]
    [Tooltip("Playback volume for commentary audio (0..1).")]
    public float commentaryVolume = 1.0f;

    [Header("Captions")]
    /// <summary>
    /// SRT/Text assets containing captions for commentary audio tracks (same order as commentaryAudio).
    /// </summary>
    [Tooltip("Text assets containing captions for commentary audio tracks, same order")]
    public List<TextAsset> commentaryCaptions = new List<TextAsset>();

    /// <summary>
    /// When true, captions will be displayed if available.
    /// </summary>
    [Tooltip("When true, captions will be displayed if available.")]
    public bool enableCaptions = true;

    [Header("Custom Background Audio Settings")]
    /// <summary>
    /// Optional custom audio clips to use instead of the video's audio (aligned by index).
    /// </summary>
    [Tooltip("Optional custom audio clips to use instead of the video's audio (aligned by index).")]
    public List<AudioClip> customBackgroundAudio = new List<AudioClip>();

    /// <summary>
    /// When true, customBackgroundAudio will override the video's audio when present.
    /// </summary>
    [Tooltip("Enabling this will override the video's background audio with your own custom audio track(s).")]
    public bool enableCustomBackgroundAudio = true;

    /// <summary>
    /// Volume used for custom background audio (0..1).
    /// </summary>
    [Range(0f, 1f)]
    [Tooltip("Volume used for custom background audio (0..1).")]
    public float customBackgroundVolume = 0.25f;

    [Header("UI Controls")]
    /// <summary>
    /// Next video button.
    /// </summary>
    [Tooltip("Next video button.")]
    public Button nextButton;

    /// <summary>
    /// Previous video button.
    /// </summary>
    [Tooltip("Previous video button.")]
    public Button prevButton;

    /// <summary>
    /// Home button (go to index 0).
    /// </summary>
    [Tooltip("Home button (go to index 0).")]
    public Button homeButton;

    /// <summary>
    /// Toggle to enable/disable commentary audio.
    /// </summary>
    [Tooltip("Toggle to enable/disable commentary audio.")]
    public Toggle commentaryToggle;

    /// <summary>
    /// Toggle to enable/disable captions.
    /// </summary>
    [Tooltip("Toggle to enable/disable captions.")]
    public Toggle captionsToggle;

    [Header("Optional Metadata")]
    /// <summary>
    /// Optional display names used by UI. If empty or element is empty, video clip name or a fallback is used.
    /// </summary>
    [Tooltip("Optional - custom display names for each video")]
    public List<string> videoDisplayNames = new List<string>();

    /// <summary>
    /// Optional title canvas manager used to show the active video's title.
    /// </summary>
    [Tooltip("Optional - TitleCanvasManager used to display the current title.")]
    public TitleCanvasManager titleCanvasManager;

    #endregion

    #region Private Fields

    private int _currentIndex = 0;
    private bool _isSpawn = true;
    private VideoPlayer _videoPlayer;
    private AudioSource _customBackgroundAudioSource;
    private AudioSource _commentaryAudioSource;
    private RenderTexture _renderTexture;
    private Material _skyboxMaterial;
    private CaptionSource _captionSource;
    private readonly List<Coroutine> _runningCaptionCoroutines = new();

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initialize internal data and wire up components and UI.
    /// </summary>
    private void Awake()
    {
        InitializeVideoDisplayNames();
        SetupComponents();
        SetupUI();
    }

    /// <summary>
    /// Starts playback at <see cref="startIndex"/>.
    /// </summary>
    private void Start()
    {
        PlayVideoAtIndex(startIndex);
    }

    #endregion

    #region Initialization Helpers

    /// <summary>
    /// Ensures <see cref="videoDisplayNames"/> contains an entry for each video clip.
    /// </summary>
    private void InitializeVideoDisplayNames()
    {
        for (int i = 0; i < tourVideos.Count; i++)
        {
            if (i < videoDisplayNames.Count)
                videoDisplayNames[i] = GetVideoDisplayName(i);
            else
                videoDisplayNames.Add(GetVideoDisplayName(i));
        }
    }

    /// <summary>
    /// Configures player, audio sources, caption source and skybox material.
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

        // Set direct audio volume using pattern match instead of explicit null comparison
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

        // Caption source
        _captionSource = GetComponent<CaptionSource>();
        if (!_captionSource)
            _captionSource = gameObject.AddComponent<CaptionSource>();

        _captionSource.audioSource = _commentaryAudioSource;
        _captionSource.boundingObject = gameObject;

        // Ensure rotation list covers all videos
        for (int i = 0; i < tourVideos.Count; i++)
            if (i >= defaultRotations.Count)
                defaultRotations.Add(80f);
    }

    /// <summary>
    /// Registers UI callbacks for navigation and toggles.
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

    #endregion

    #region Captions

    /// <summary>
    /// Load SRT captions and schedule them to display starting at <paramref name="currentTime"/>.
    /// </summary>
    /// <param name="srt">SRT/TextAsset to parse.</param>
    /// <param name="currentTime">Optional current time offset (seconds) to start scheduling from.</param>
    private void LoadCaptionsFromSRT(TextAsset srt, double currentTime = 0)
    {
        ClearAllCaptions();

        if (!_captionSource || srt is null)
            return;

        List<SubtitleBlock> subtitleBlocks = SRTParser.Load(srt);
        if (subtitleBlocks is null)
            return;

        foreach (var subtitle in subtitleBlocks)
        {
            float start = (float)subtitle.From;
            float end = (float)subtitle.To;

            if (start >= currentTime)
            {
                float delay = start - (float)currentTime;
                float duration = end - start;
                string text = subtitle.Text;

                Coroutine coroutine = StartCoroutine(PlayCaptionWithDelay(text, duration, delay));
                _runningCaptionCoroutines.Add(coroutine);
            }
        }
    }

    /// <summary>
    /// Coroutine to display a caption after a delay for a set duration.
    /// </summary>
    private IEnumerator PlayCaptionWithDelay(string text, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enableCommentaryAudio && _captionSource)
            _captionSource.ShowTimedCaption(text, duration);
    }

    #endregion

    #region Playback Control

    /// <summary>
    /// Play the video at the specified <paramref name="index"/> and configure audio, captions and UI.
    /// </summary>
    /// <param name="index">Index of the video to play.</param>
    public void PlayVideoAtIndex(int index)
    {
        bool hasCaptions = index >= 0 && index < commentaryCaptions.Count && commentaryCaptions[index] is not null;
        if (captionsToggle)
        {
            captionsToggle.interactable = hasCaptions;
            if (hasCaptions)
                captionsToggle.isOn = enableCaptions;
        }

        bool hasCommentary = index >= 0 && index < commentaryAudio.Count && commentaryAudio[index] is not null;
        if (commentaryToggle)
        {
            commentaryToggle.interactable = hasCommentary;
            if (hasCommentary)
                commentaryToggle.isOn = enableCommentaryAudio;
        }

        if (xrOrigin)
        {
            // Safely get current transform rotation and apply per-video Y rotation
            var currentEuler = transform.rotation.eulerAngles;
            float yRotation = (index >= 0 && index < defaultRotations.Count) ? defaultRotations[index] : 80f;
            xrOrigin.transform.rotation = Quaternion.Euler(currentEuler.x, yRotation, currentEuler.z);
        }

        ClearAllCaptions();

        if (titleCanvasManager)
            titleCanvasManager.SetTitle(GetVideoDisplayName(index));

        if (index < 0 || index >= tourVideos.Count)
        {
            if (index != 0)
                Debug.LogWarning("Video index out of range: " + index);
            else
                Debug.LogWarning("No videos assigned to TourManager!");
            return;
        }

        _currentIndex = index;
        VideoClip clip = tourVideos[index];

        if (clip)
        {
            if (_renderTexture)
                _renderTexture.Release();

            // cast width/height to int for RenderTexture creation
            _renderTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            _renderTexture.Create();

            _videoPlayer.clip = clip;
            _videoPlayer.targetTexture = _renderTexture;
            _videoPlayer.Play();

            if (_skyboxMaterial)
                _skyboxMaterial.SetTexture("_MainTex", _renderTexture);
        }

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
    /// VideoPlayer loop-end callback. Advances or restarts the tour according to settings.
    /// </summary>
    private void OnVideoEnd(VideoPlayer vp)
    {
        // Decide next action: restart to startIndex, advance next, or do nothing.
        if (_currentIndex >= tourVideos.Count - 1 && restartTourAfterLastVideo)
            PlayVideoAtIndex(startIndex);
        else if (autoAdvanceVideo)
            PlayVideoAtIndex(_currentIndex + 1);
    }

    private IEnumerator DelayedStartAudio(int index)
    {
        yield return new WaitForEndOfFrame();
        HandleCustomBackgroundAudio(index);
        HandleCommentaryAudio(index);
    }

    #endregion

    #region Audio Handling

    /// <summary>
    /// Choose and start/stop custom background audio or fall back to the video's direct audio.
    /// </summary>
    /// <param name="index">Current video index.</param>
    private void HandleCustomBackgroundAudio(int index)
    {
        _customBackgroundAudioSource.volume = customBackgroundVolume;

        if (!enableCustomBackgroundAudio)
        {
            _customBackgroundAudioSource.Stop();
            return;
        }

        if (index >= 0 && index < customBackgroundAudio.Count && customBackgroundAudio[index] is not null)
        {
            _customBackgroundAudioSource.clip = customBackgroundAudio[index];
            _customBackgroundAudioSource.Play();
            _videoPlayer.SetDirectAudioVolume(0, 0f);
            return;
        }

        _customBackgroundAudioSource.Stop();

        if (!enableBackgroundAudio)
            _videoPlayer.SetDirectAudioVolume(0, 0f);
        else
            _videoPlayer.SetDirectAudioVolume(0, backgroundAudioVolume);
    }

    /// <summary>
    /// Start or stop commentary audio and load corresponding captions.
    /// </summary>
    /// <param name="index">Current video index.</param>
    private void HandleCommentaryAudio(int index)
    {
        _commentaryAudioSource.volume = commentaryVolume;

        if (index >= 0 && index < commentaryCaptions.Count && commentaryCaptions[index] is not null && enableCaptions)
            LoadCaptionsFromSRT(commentaryCaptions[index]);

        if (!enableCommentaryAudio)
        {
            _commentaryAudioSource.Stop();
            return;
        }

        if (index >= 0 && index < commentaryAudio.Count && commentaryAudio[index] is not null)
        {
            _commentaryAudioSource.clip = commentaryAudio[index];
            _commentaryAudioSource.Play();
            return;
        }

        _commentaryAudioSource.Stop();
    }

    #endregion

    #region UI Helpers

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
    /// Toggle captions on/off from UI.
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

        if (commentaryCaptions is not null && _currentIndex >= 0 && _currentIndex < commentaryCaptions.Count)
        {
            TextAsset srt = commentaryCaptions[_currentIndex];
            if (srt is not null && _commentaryAudioSource.clip is not null)
            {
                double currentTime = _commentaryAudioSource.time;
                LoadCaptionsFromSRT(srt, currentTime);
            }
        }
    }

    /// <summary>
    /// Enable/disable navigation buttons based on the current index.
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
    /// Move to the next video if possible.
    /// </summary>
    private void NextVideo()
    {
        if (_currentIndex + 1 < tourVideos.Count)
            PlayVideoAtIndex(_currentIndex + 1);
    }

    /// <summary>
    /// Move to the previous video if possible.
    /// </summary>
    private void PreviousVideo()
    {
        if (_currentIndex - 1 >= 0)
            PlayVideoAtIndex(_currentIndex - 1);
    }

    /// <summary>
    /// Returns the display name for a video index, falling back to clip name or "Video N".
    /// </summary>
    /// <param name="index">Index to query.</param>
    /// <returns>Appropriate display name.</returns>
    private string GetVideoDisplayName(int index)
    {
        if (index >= 0 && index < videoDisplayNames.Count && !string.IsNullOrEmpty(videoDisplayNames[index]))
            return videoDisplayNames[index];

        if (index >= 0 && index < tourVideos.Count && tourVideos[index] is not null)
            return tourVideos[index].name;

        return $"Video {index + 1}";
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Stops any running caption coroutines and clears rendered captions.
    /// </summary>
    private void ClearAllCaptions()
    {
        foreach (var coroutine in _runningCaptionCoroutines)
            if (coroutine != null)
                StopCoroutine(coroutine);

        _runningCaptionCoroutines.Clear();

        if (_captionSource && CaptionRenderManager.Instance && CaptionRenderManager.Instance.currentRenderer)
            CaptionRenderManager.Instance.ClearCaptions();
    }

    /// <summary>
    /// Unsubscribe delegates to avoid leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (_videoPlayer)
            _videoPlayer.loopPointReached -= OnVideoEnd;
    }

    #endregion
}
