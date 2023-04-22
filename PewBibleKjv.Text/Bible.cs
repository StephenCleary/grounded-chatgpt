using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PewBibleKjv.Text
{
    public static class Bible
    {
        public const int InvalidAbsoluteVerseNumber = -1;
        public const int John_1_1 = 26045;

        public static FormattedVerse FormattedVerse(int verseNumber)
        {
            var result = new FormattedVerse();
            var text = new StringBuilder();
            var preventSpace = false;
            foreach (var word in VerseWords(verseNumber))
            {
                if (!NoPreSpace.Contains(word))
                {
                    if (!preventSpace)
                    {
                        text.Append(' ');
                    }
                    FormatWord(word, text, result);
                }
                else
                {
                    FormatWord(word, text, result);
                }
                preventSpace = NoPostSpace.Contains(word);
            }
            result.Text = text.ToString();
            return result;
        }

        private static void FormatWord(string word, StringBuilder text, FormattedVerse result)
        {
            if (word == "<")
            {
                result.Spans.Add(new FormattedVerse.Span
                {
                    Type = Text.FormattedVerse.SpanType.Colophon,
                    Begin = text.Length
                });
            }
            else if (word == "[")
            {
                result.Spans.Add(new FormattedVerse.Span
                {
                    Type = Text.FormattedVerse.SpanType.Italics,
                    Begin = text.Length
                });
            }
            else if (word == ">")
            {
                result.Spans.Last(x => x.Type == Text.FormattedVerse.SpanType.Colophon).End = text.Length;
            }
            else if (word == "]")
            {
                result.Spans.Last(x => x.Type == Text.FormattedVerse.SpanType.Italics).End = text.Length;
            }
            else if (word == "-")
            {
                text.Append("-\u200B");
            }
            else if (word == "—")
            {
                text.Append("—");
            }
            else
            {
                text.Append(word);
            }
        }

        // Punctuation that prevents a space from being appended after them.
        private static readonly string[] NoPostSpace = {"(", "[", "<", "-", "—"};

        // Punctuation that prevents a space from being prepended before them.
        private static readonly string[] NoPreSpace = {"'", "-", "—", "!", ")", ",", ".", ":", ";", "?", "]", "'s", ">"};

        public static IEnumerable<string> VerseWords(int verseNumber)
        {
            var beginEndBytes = new byte[sizeof(int) * 2];
            Data.VerseIndex.Position = verseNumber * sizeof(int);
            Data.VerseIndex.Read(beginEndBytes, 0, sizeof(int) * 2);
            var begin = BitConverter.ToInt32(beginEndBytes, 0);
            var end = BitConverter.ToInt32(beginEndBytes, sizeof(int));

            var verseDataBytes = new byte[sizeof(ushort) * (end - begin)];
            Data.Verses.Position = sizeof(ushort) * begin;
            Data.Verses.Read(verseDataBytes, 0, verseDataBytes.Length);
            var verseData = new ushort[end - begin];
            Buffer.BlockCopy(verseDataBytes, 0, verseData, 0, verseDataBytes.Length);

            foreach (var value in verseData)
            {
                // Look up the word in either the words or punctuation tables, and adjust casing if necessary.
                var wordIndex = value & 0x3FFF;
                var isPunctuation = wordIndex >= Constants.Words.Length;
                if (isPunctuation)
                {
                    yield return Constants.Punctuation[wordIndex - Constants.Words.Length];
                }
                else
                {
                    var word = Constants.Words[wordIndex];
                    var wordFlags = value >> 14;
                    if (wordFlags == 1)
                        word = word.ToUpperInvariant();
                    else if (wordFlags == 2)
                        word = char.ToUpperInvariant(word[0]) + word.Substring(1);
                    else if (wordFlags == 3)
                        word = char.ToUpperInvariant(word[0]).ToString() + char.ToUpperInvariant(word[1]) + word.Substring(2);
                    yield return word;
                }
            }
        }
    }
}
