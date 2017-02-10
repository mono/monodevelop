using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal abstract class SimpleTextStorage : ITextStorage
    {
        public static SimpleTextStorage Create(string content, IList<int> lineBreaks)
        {
            if (lineBreaks.Count == 0 || lineBreaks[lineBreaks.Count - 1] <= ushort.MaxValue)
            {
                return new ShortSimpleTextStorage(content, lineBreaks);
            }
            else
            {
                return new LongSimpleTextStorage(content, lineBreaks);
            }
        }

        public static SimpleTextStorage Create(string content)
        {
            return Create(content, CalculateLineBreaks(content));
        }

        protected readonly string _content;

        protected SimpleTextStorage(string content)
        {
            _content = content;
        }

        private static List<int> CalculateLineBreaks(string content)
        {
            List<int> lineBreaks = new List<int>();
            int i = 0;
            while (i < content.Length)
            {
                int breakLength = TextUtilities.LengthOfLineBreak(content, i, content.Length);

                if (breakLength == 0)
                {
                    ++i;
                }
                else
                {
                    lineBreaks.Add(i);
                    i += breakLength;
                }
            }
            return lineBreaks;
        }

        public int Length
        {
            get { return _content.Length; }
        }

        public string GetText(int startIndex, int length)
        {
            return _content.Substring(startIndex, length);
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            _content.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public void Write(TextWriter writer, int startIndex, int length)
        {
            if (startIndex == 0 && length == _content.Length)
            {
                writer.Write(_content);
            }
            else
            {
                writer.Write(_content.Substring(startIndex, length));
            }
        }

        public char this[int index]
        {
            get { return _content[index]; }
        }

        public bool IsNewLine(int index)
        {
            return _content[index] == '\n';
        }

        public bool IsReturn(int index)
        {
            return _content[index] == '\r';
        }

        public abstract ILineBreaks LineBreaks { get; }

        public abstract int StartOfLineBreak(int index);

        public int EndOfLineBreak(int index)
        {
            int pos = StartOfLineBreak(index);
            if (_content[pos] == '\r' && pos + 1 < _content.Length && _content[pos + 1] == '\n')
            {
                return pos + 2;
            }
            else
            {
                return pos + 1;
            }
        }
    }

    internal sealed class ShortSimpleTextStorage : SimpleTextStorage, ILineBreaks
    {
        static readonly ushort[] _emptyLineBreaks = new ushort[0];
        ushort[] _lineBreaks;

        public ShortSimpleTextStorage(string content, IList<int> lineBreaks)
          : base(content)
        {
            if (lineBreaks.Count == 0)
            {
                _lineBreaks = _emptyLineBreaks;
            }
            else
            {
                _lineBreaks = new ushort[lineBreaks.Count];
                for (int l = 0; l < lineBreaks.Count; ++l)
                {
                    _lineBreaks[l] = (ushort)lineBreaks[l];
                }
            }
        }

        public override ILineBreaks LineBreaks
        {
            get { return this; }
        }

        public override int StartOfLineBreak(int index)
        {
            return _lineBreaks[index];
        }

        int ILineBreaks.Length
        {
            get { return _lineBreaks.Length; }
        }
    }

    internal sealed class LongSimpleTextStorage : SimpleTextStorage, ILineBreaks
    {
        static readonly int[] _emptyLineBreaks = new int[0];
        int[] _lineBreaks;

        public LongSimpleTextStorage(string content, IList<int> lineBreaks)
          : base(content)
        {
            if (lineBreaks.Count == 0)
            {
                _lineBreaks = _emptyLineBreaks;
            }
            else
            {
                _lineBreaks = new int[lineBreaks.Count];
                for (int l = 0; l < lineBreaks.Count; ++l)
                {
                    _lineBreaks[l] = lineBreaks[l];
                }
            }
        }

        public LongSimpleTextStorage(string content, List<int> lineBreaks)
            : base(content)
        {
            _lineBreaks = lineBreaks.ToArray();
        }

        public override ILineBreaks LineBreaks
        {
            get { return this; }
        }

        public override int StartOfLineBreak(int index)
        {
            return _lineBreaks[index];
        }

        int ILineBreaks.Length
        {
            get { return _lineBreaks.Length; }
        }
    }
}