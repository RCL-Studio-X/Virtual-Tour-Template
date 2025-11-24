# Virtual-Tour-Template

This is Studio X's template Unity project for making your very own interactive 360 Video tour. Complete with support for transitioning between videos, commentary audio and captions, and selecting videos from a menu. 

Examples of tours made with this project:

- Tour of the Genomics Research Center at the University of Rochester's Wilmot Cancer Institute
- Tour of Mary Ann Mavrinac Studio X at the University of Rochester

## Prerequisites

- [Unity Editor 6000.0.58f2](https://unity.com/releases/editor/whats-new/6000.0.58f2#installs) (You can try a more recent version of Unity, but compatibility is not guaranteed)


## Quick Start Guide

The following is a quick start guide for how to use this template to create your own tour. If you'd like a more detailed guide, you can access that here (not yet available).

#### Importing Assets

1. Place your 360 videos (in H.264 or H.265 `.mp4` format) in `Assets/_Resources/360_Videos`. Note: you can use audio that is in the video as background audio.
2. (Optional) Place your custom background audio (in vorbis `.ogg` format or [another audio format compatible with Unity](https://support.unity.com/hc/en-us/articles/206484803-What-are-the-supported-Audio-formats-in-Unity)) in `Assets/_Resources/Audio_Background`.
3. (Optional) Place your Commentary Audio (in vorbis `.ogg` format or [another audio format compatible with Unity](https://support.unity.com/hc/en-us/articles/206484803-What-are-the-supported-Audio-formats-in-Unity)) in `Assets/_Resources/Audio_Commentary`.
4. (Optional) Place the subtitles for your Commentary Audio in `Assets/_Resources/Subtitles_Commentary`.

**Subtitle Format:** your subtitles need to be in SRT format, but with the file extension `.txt`. So, if you have `.srt` files, simply rename them with the file extension `.txt`.

**Video File Size:** 360 videos can get very big in size. We at Studio X recommend recording in 4k30fps, then exporting (encoding) your video file as H.265 `.mp4` with resolution 3840x1920 at ~15 Mbps to keep your video file sizes low.

#### Configuring the Scene

1. Open the scene `Main Tour` in `Assets/_Scenes`.
2. Select the `Virtual Tour (Full Setup)` GameObject.
3. In the `Inspector` Window, add your videos, audio, and subtitles by dragging them  into the appropriate lists in the `Tour Manager` script.
4. (Optional) Manually set the names of each video in the `Tour Manager` script by editing the `Video Display Names` list. If you do not set these names manually, the `Tour Manager` will automatically assign those names as the filenames of the videos (without the file extension).
