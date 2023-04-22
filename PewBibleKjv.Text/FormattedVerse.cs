using System;
using System.Collections.Generic;
using System.Text;

namespace PewBibleKjv.Text
{
    public sealed class FormattedVerse
    {
        public string Text { get; set; }

        public List<Span> Spans { get; } = new List<Span>();

        public enum SpanType
        {
            Colophon,
            Italics
        }

        public sealed class Span
        {
            public SpanType Type { get; set; }
            public int Begin { get; set; }
            public int End { get; set; }
        }
    }
}
