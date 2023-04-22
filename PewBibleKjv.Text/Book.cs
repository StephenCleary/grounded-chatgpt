using System;
using System.Collections.Generic;

namespace PewBibleKjv.Text
{
    public sealed class Book : IVerseRange
    {
        public Book(int index, string name, int beginVerse, int endVerse, Chapter[] chapters)
        {
            Index = index;
            Name = name;
            BeginVerse = beginVerse;
            EndVerse = endVerse;
            Chapters = chapters;
        }

        /// <summary>
        /// The index of this book in its parent structure's <see cref="Structure.Books"/> array.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The canonical name of the book.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The absolute verse number of the first verse in this book.
        /// </summary>
        public int BeginVerse { get; }

        /// <summary>
        /// The absolute verse number of the last verse in this book, plus one.
        /// </summary>
        public int EndVerse { get; }

        /// <summary>
        /// The chapters of this book.
        /// </summary>
        public Chapter[] Chapters { get; }
    }
}
