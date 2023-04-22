using System;
using System.Collections.Generic;
using System.Text;

namespace PewBibleKjv.Text
{
    public interface IVerseRange
    {
        /// <summary>
        /// The absolute verse number of the first verse in this verse range.
        /// </summary>
        int BeginVerse { get; }

        /// <summary>
        /// The absolute verse number of the last verse in this verse range, plus one.
        /// </summary>
        int EndVerse { get; }
    }
}
