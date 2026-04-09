using System;
using System.Collections.Generic;
using UnityEngine;

/*
MIT License

Copyright (c) 2017 

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

GitHub Repository: https://github.com/roguecode/Unity-Simple-SRT
File Obtained from: https://github.com/roguecode/Unity-Simple-SRT/blob/master/src/Assets/SimpleSRT/SRTParser.cs
Accessed on 6/25/2025 at 01:05 PM EST.
This file has been modified from its original source.
*/

namespace StudioXRCL.VirtualTour.External.SimpleSRT
{
    /// <summary>
    /// Parses SRT subtitle files into <see cref="SubtitleBlock"/> objects.
    /// Supports parsing from a Resources path or directly from a <see cref="TextAsset"/>.
    /// </summary>
    /// <remarks>
    /// Original implementation by roguecode (MIT License).
    /// See file header for more details.
    /// </remarks>
    public class SRTParser
    {
        private List<SubtitleBlock> _subtitles;

        /// <summary>
        /// Creates a new parser and loads an SRT file from a Resources path.
        /// </summary>
        /// <param name="textAssetResourcePath">The path within the Resources folder to a TextAsset containing SRT data.</param>
        public SRTParser(string textAssetResourcePath)
        {
            var text = Resources.Load<TextAsset>(textAssetResourcePath);
            Load(text);
        }

        /// <summary>
        /// Creates a new parser using an existing <see cref="TextAsset"/>.
        /// </summary>
        /// <param name="textAsset">The TextAsset containing SRT-formatted subtitle text.</param>
        public SRTParser(TextAsset textAsset)
        {
            _subtitles = Load(textAsset);
        }

        /// <summary>
        /// Loads and parses subtitle blocks from a <see cref="TextAsset"/> containing SRT data.
        /// </summary>
        /// <param name="textAsset">The TextAsset containing subtitle data.</param>
        /// <returns>A list of parsed <see cref="SubtitleBlock"/> objects, or null if the file is missing.</returns>
        public static List<SubtitleBlock> Load(TextAsset textAsset)
        {
            if (!textAsset)
            {
                Debug.LogError("Subtitle file is null");
                return null;
            }

            var lines = textAsset.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var currentState = eReadState.Index;
            var subs = new List<SubtitleBlock>();

            int currentIndex = 0;
            double currentFrom = 0, currentTo = 0;
            var currentText = string.Empty;

            for (var l = 0; l < lines.Length; l++)
            {
                var line = lines[l];

                switch (currentState)
                {
                    case eReadState.Index:
                        {
                            int index;
                            if (Int32.TryParse(line, out index))
                            {
                                currentIndex = index;
                                currentState = eReadState.Time;
                            }
                        }
                        break;

                    case eReadState.Time:
                        {
                            line = line.Replace(',', '.');
                            var parts = line.Split(new[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length == 2)
                            {
                                if (TimeSpan.TryParse(parts[0], out TimeSpan fromTime) &&
                                    TimeSpan.TryParse(parts[1], out TimeSpan toTime))
                                {
                                    currentFrom = fromTime.TotalSeconds;
                                    currentTo = toTime.TotalSeconds;
                                    currentState = eReadState.Text;
                                }
                            }
                        }
                        break;

                    case eReadState.Text:
                        {
                            if (currentText != string.Empty)
                                currentText += "\r\n";

                            currentText += line;

                            // Empty line = end of block
                            if (string.IsNullOrEmpty(line) || l == lines.Length - 1)
                            {
                                subs.Add(new SubtitleBlock(currentIndex, currentFrom, currentTo, currentText));

                                currentText = string.Empty;
                                currentState = eReadState.Index;
                            }
                        }
                        break;
                }
            }

            return subs;
        }

        /// <summary>
        /// Returns the subtitle block that should be displayed at a given time.
        /// Automatically advances to the next block as subtitles expire.
        /// </summary>
        /// <param name="time">The current playback time in seconds.</param>
        /// <returns>A matching <see cref="SubtitleBlock"/>, <see cref="SubtitleBlock.Blank"/>, or null if no subtitles remain.</returns>
        public SubtitleBlock GetForTime(float time)
        {
            if (_subtitles.Count > 0)
            {
                var subtitle = _subtitles[0];

                if (time >= subtitle.To)
                {
                    _subtitles.RemoveAt(0);

                    if (_subtitles.Count == 0)
                        return null;

                    subtitle = _subtitles[0];
                }

                if (subtitle.From > time)
                    return SubtitleBlock.Blank;

                return subtitle;
            }
            return null;
        }

        /// <summary>
        /// Indicates the current parsing stage as the SRT file is processed.
        /// </summary>
        private enum eReadState
        {
            Index,
            Time,
            Text
        }
    }

    /// <summary>
    /// Represents a single subtitle entry containing timing and text information.
    /// </summary>
    public class SubtitleBlock
    {
        private static SubtitleBlock _blank;

        /// <summary>
        /// Gets a blank subtitle block (represents "no subtitle").
        /// </summary>
        public static SubtitleBlock Blank => _blank ?? (_blank = new SubtitleBlock(0, 0, 0, string.Empty));

        /// <summary>
        /// The numeric index of the subtitle block as defined in the SRT file.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The length of the subtitle display time in seconds.
        /// </summary>
        public double Length { get; }

        /// <summary>
        /// The starting time (in seconds) when the subtitle appears.
        /// </summary>
        public double From { get; }

        /// <summary>
        /// The ending time (in seconds) when the subtitle disappears.
        /// </summary>
        public double To { get; }

        /// <summary>
        /// The subtitle text content.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Creates a new subtitle block.
        /// </summary>
        /// <param name="index">Numeric index of the block.</param>
        /// <param name="from">Start time in seconds.</param>
        /// <param name="to">End time in seconds.</param>
        /// <param name="text">The subtitle text.</param>
        public SubtitleBlock(int index, double from, double to, string text)
        {
            Index = index;
            From = from;
            To = to;
            Length = to - from;
            Text = text;
        }
    }
}
