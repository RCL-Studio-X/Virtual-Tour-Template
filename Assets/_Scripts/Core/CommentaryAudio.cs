using System.Collections.Generic;
using UnityEngine;

public class CommentaryAudio : MonoBehaviour
{
    public string language;
    [Header("Audio Tracks")]
    [Tooltip("Optional commentary audio tracks that correspond to each video.")]
    public List<AudioClip> audioList = new();

    [Header("Captions")]
    [Tooltip("TextAsset captions in SRT format for commentary audio tracks (same order as audioList).")]
    public List<TextAsset> captionsList = new();
}
