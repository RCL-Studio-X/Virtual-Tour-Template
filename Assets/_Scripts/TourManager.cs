using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using XRAccess.Chirp;
using System.Collections.Generic;
using System.Collections;

public class TourManager : MonoBehaviour
{
    public System.Action<int> OnVideoChanged;
    public int VideoCount => tourVideos.Count;
    public int CurrentVideoIndex => currentIndex;

    public GameObject xrOrigin;
    [Tooltip("What the XR Origin's Y rotation will be when switching to a new video.")]
    public float defaultRotation = 80f;

    [Header("Video Tour Settings")]
    public List<VideoClip> tourVideos = new List<VideoClip>();
    public int startIndex = 0;

    [Header("Commentary Audio Settings")]
    public List<AudioClip> commentaryAudio = new List<AudioClip>();
    public bool enableCommentaryAudio = true;
    [Range(0f, 1f)]
    public float commentaryVolume = 1.0f;

    [Header("Captions")]
    [Tooltip("Text assets containing captions for commentary audio tracks, same order")]
    public List<TextAsset> commentaryCaptions = new List<TextAsset>();
    public bool enableCaptions = true;
    private CaptionSource captionSource;
    private readonly List<Coroutine> runningCaptionCoroutines = new List<Coroutine>();

    [Header("Background Audio Settings")]
    public List<AudioClip> backgroundAudio = new List<AudioClip>();
    public bool enableBackgroundAudio = true;
    [Range(0f, 1f)]
    public float backgroundVolume = 0.25f;

    [Header("UI Controls")]
    public Button nextButton;
    public Button prevButton;
    public Button homeButton;
    public Toggle commentaryToggle;
    public Toggle captionsToggle;

    private int currentIndex = 0;
    private VideoPlayer videoPlayer;
    private AudioSource backgroundAudioSource;
    private AudioSource commentaryAudioSource;
    private RenderTexture renderTexture;
    private Material skyboxMaterial;

    [Header("Optional Metadata")]
    [Tooltip("Optional - custom display names for each video")]
    public List<string> videoDisplayNames = new List<string>();
    public TitleCanvasManager titleCanvasManager;
    
    private void Awake()
    {
        InitializeVideoDisplayNames();
        SetupComponents();
        SetupUI();
        PlayVideoAtIndex(startIndex);
    }

    private void InitializeVideoDisplayNames()
    {
        for (int i = 0; i < tourVideos.Count; i++)
        {
            if (i < videoDisplayNames.Count)
            {
                videoDisplayNames[i] = GetVideoDisplayName(i);
            }
            else
            {
                videoDisplayNames.Add(GetVideoDisplayName(i));
            }
        }
    }

    private void SetupComponents()
    {
        // Setup Skybox
        if (RenderSettings.skybox == null)
        {
            skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
            RenderSettings.skybox = skyboxMaterial;
        }
        else
        {
            skyboxMaterial = RenderSettings.skybox;
        }

        // Setup Video Player
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.isLooping = true;
        videoPlayer.playOnAwake = false;

        // Setup Background Audio Source
        backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        backgroundAudioSource.playOnAwake = false;
        backgroundAudioSource.loop = true;
        backgroundAudioSource.spatialBlend = 0f; // 2D audio
        backgroundAudioSource.volume = backgroundVolume;

        // Setup Commentary Audio Source
        commentaryAudioSource = gameObject.AddComponent<AudioSource>();
        commentaryAudioSource.playOnAwake = false;
        commentaryAudioSource.loop = false;
        commentaryAudioSource.spatialBlend = 0f; // 2D audio
        commentaryAudioSource.volume = commentaryVolume;

        // Setup CaptionSource
        captionSource = GetComponent<CaptionSource>();
        if (captionSource == null)
            captionSource = gameObject.AddComponent<CaptionSource>();

        captionSource.audioSource = commentaryAudioSource;
        captionSource.boundingObject = gameObject;

        //defaultRotation = xrOrigin.transform.rotation.y;
    }

    private void SetupUI()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(NextVideo);

        if (prevButton != null)
            prevButton.onClick.AddListener(PreviousVideo);

        if (homeButton != null)
            homeButton.onClick.AddListener(() => PlayVideoAtIndex(0));

        if (commentaryToggle != null)
            commentaryToggle.onValueChanged.AddListener(ToggleCommentaryAudio);

        if (captionsToggle != null)
            captionsToggle.onValueChanged.AddListener(ToggleCaptions);
    }


    private void LoadCaptionsFromSRT(TextAsset srt, double currentTime = 0)
    {
        ClearAllCaptions();

        if (captionSource == null || srt == null) return;

        List<SubtitleBlock> subtitleBlocks = SRTParser.Load(srt);
        if (subtitleBlocks == null) return;

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
                runningCaptionCoroutines.Add(coroutine);
            }
        }
    }


    private IEnumerator PlayCaptionWithDelay(string text, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enableCommentaryAudio && captionSource)
            captionSource.ShowTimedCaption(text, duration);
    }


    public void PlayVideoAtIndex(int index)
    {
        bool hasCaptions = index >= 0 && index < commentaryCaptions.Count && commentaryCaptions[index];

        captionsToggle.interactable = hasCaptions;
        
        if (hasCaptions)
        {
            captionsToggle.isOn = enableCaptions;
        }
        
        bool hasCommentary = index >= 0 && index < commentaryAudio.Count && commentaryAudio[index];
        
        commentaryToggle.interactable = hasCommentary;

        if (hasCommentary)
        {
            commentaryToggle.isOn = enableCommentaryAudio;
        }

        if (xrOrigin)
            xrOrigin.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, defaultRotation, transform.rotation.eulerAngles.z);

        ClearAllCaptions();

        titleCanvasManager.SetTitle(GetVideoDisplayName(index));

        if (index < 0 || index >= tourVideos.Count)
        {
            if (index != 0)
                Debug.LogWarning("Video index out of range: " + index);
            else
                Debug.LogWarning("No videos assigned to TourManager!");
            return;
        }

        currentIndex = index;
        VideoClip clip = tourVideos[index];

        if (clip != null)
        {
            if (renderTexture != null)
                renderTexture.Release();

            renderTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            renderTexture.Create();

            videoPlayer.clip = clip;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.Play();

            skyboxMaterial.SetTexture("_MainTex", renderTexture);
        }

        HandleBackgroundAudio(index);
        HandleCommentaryAudio(index);

        UpdateButtonStates();
        OnVideoChanged?.Invoke(currentIndex);
    }

    private void HandleBackgroundAudio(int index)
    {
        backgroundAudioSource.volume = backgroundVolume;

        if (!enableBackgroundAudio)
        {
            backgroundAudioSource.Stop();
            return;
        }

        if (index >= 0 && index < backgroundAudio.Count && backgroundAudio[index])
        {
            backgroundAudioSource.clip = backgroundAudio[index];
            backgroundAudioSource.Play();
        }
        else
        {
            backgroundAudioSource.Stop();
        }
    }

    private void HandleCommentaryAudio(int index)
    {
        commentaryAudioSource.volume = commentaryVolume;

        // Load matching captions if available
        if (index >= 0 && index < commentaryCaptions.Count && commentaryCaptions[index] && enableCaptions)
        {
            LoadCaptionsFromSRT(commentaryCaptions[index]);
        }

        if (!enableCommentaryAudio)
        {
            commentaryAudioSource.Stop();
            return;
        }

        if (index >= 0 && index < commentaryAudio.Count && commentaryAudio[index])
        {
            commentaryAudioSource.clip = commentaryAudio[index];
            commentaryAudioSource.Play();
        }
        else
        {
            commentaryAudioSource.Stop();
        }

    }

    private void ToggleCommentaryAudio(bool value)
    {
        enableCommentaryAudio = value;

        if (!enableCommentaryAudio)
        {
            commentaryAudioSource.Stop();

            ClearAllCaptions();
        }
        else
        {
            HandleCommentaryAudio(currentIndex); // restart from beginning
        }
    }

    private void ToggleCaptions(bool value)
    {
        enableCaptions = value;

        if (!enableCaptions)
        {
            ClearAllCaptions();
        }
        else
        {
            if (commentaryCaptions != null && currentIndex >= 0 && currentIndex < commentaryCaptions.Count)
            {
                TextAsset srt = commentaryCaptions[currentIndex];
                if (srt != null)
                {
                    double currentTime = commentaryAudioSource.time;
                    LoadCaptionsFromSRT(srt, currentTime);
                }
            }
        }
    }


    private void UpdateButtonStates()
    {
        if (prevButton != null)
            prevButton.interactable = currentIndex > 0;

        if (nextButton != null)
            nextButton.interactable = currentIndex < tourVideos.Count - 1;

        if (homeButton != null)
            homeButton.interactable = currentIndex != 0;
    }

    private void NextVideo()
    {
        if (currentIndex + 1 < tourVideos.Count)
            PlayVideoAtIndex(currentIndex + 1);
    }

    private void PreviousVideo()
    {
        if (currentIndex - 1 >= 0)
            PlayVideoAtIndex(currentIndex - 1);
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    private string GetVideoDisplayName(int index)
    {
        if (index >= 0 && index < videoDisplayNames.Count)
            if (videoDisplayNames[index] != null)
                return videoDisplayNames[index];

        if (index >= 0 && index < tourVideos.Count && tourVideos[index] != null)
            return tourVideos[index].name;

        return $"Video {index + 1}";
    }
    private void ClearAllCaptions()
    {
        foreach (var coroutine in runningCaptionCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        runningCaptionCoroutines.Clear();

        if (captionSource != null && CaptionRenderManager.Instance != null && CaptionRenderManager.Instance.currentRenderer != null)
            CaptionRenderManager.Instance.ClearCaptions();
    }
}
