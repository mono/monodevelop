using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Text.Differencing;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    /// <summary>
    /// This is a decomposition of the given string into characters.
    /// Note that this is still a DecomposedStringList, which is an IList&lt;string&gt;,
    /// and so each string will be one character in length.
    /// </summary>
    internal class CharacterDecompositionList : ITokenizedStringListInternal
    {
        string _originalString;
        SnapshotSpan _originalSpan;

        public CharacterDecompositionList(string original)
        {
            _originalString = original;
        }

        public CharacterDecompositionList(SnapshotSpan original)
        {
            _originalSpan = original;
        }

        public string Original
        {
            get
            {
                // A call to GetText() here could be very expensive in memory. Be careful!
                return _originalString ?? _originalSpan.GetText();
            }
        }

        public string OriginalSubstring(int start, int length)
        {
            if (_originalString != null)
                return _originalString.Substring(start, length);
            else
                return _originalSpan.Snapshot.GetText(start + _originalSpan.Start.Position, length);
        }

        public Span GetElementInOriginal(int index)
        {
            //If index == count, return a zero-length span at the end.
            return new Span(index, (index < this.Count) ? 1 : 0);
        }

        public Span GetSpanInOriginal(Span span)
        {
            return span;
        }

        public int IndexOf(string item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, string item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public string this[int index]
        {
            get
            {
                return this.OriginalSubstring(index, 1);
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public void Add(string item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(string item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return _originalString != null ? _originalString.Length : _originalSpan.Length; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(string item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
