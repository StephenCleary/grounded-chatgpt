using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PewBibleKjv.Text
{
    public static class Data
    {
        private static readonly Lazy<Stream> _verses = new Lazy<Stream>(() => Assembly.GetExecutingAssembly().GetManifestResourceStream("PewBibleKjv.Text.Embedded.verses.dat"));
        private static readonly Lazy<Stream> _verseIndex = new Lazy<Stream>(() => Assembly.GetExecutingAssembly().GetManifestResourceStream("PewBibleKjv.Text.Embedded.verseIndex.dat"));

        public static Stream Verses => _verses.Value;
        public static Stream VerseIndex => _verseIndex.Value;
    }
}
