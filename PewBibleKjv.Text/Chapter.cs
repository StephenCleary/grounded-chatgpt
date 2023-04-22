using System;
using System.Collections.Generic;
using System.Text;

namespace PewBibleKjv.Text
{
    public sealed class Chapter : IVerseRange
    {
        public Chapter(int index, int beginVerse, int endVerse)
        {
            Index = index;
            BeginVerse = beginVerse;
            EndVerse = endVerse;
        }

        /// <summary>
        /// The index of this chapter in its parent book's <see cref="Book.Chapters"/> array.
        /// This is also the chapter number, minus one.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The absolute verse number of the first verse in this chapter.
        /// </summary>
        public int BeginVerse { get; }

        /// <summary>
        /// The absolute verse number of the last verse in this chapter, plus one.
        /// </summary>
        public int EndVerse { get; }
    }
}
