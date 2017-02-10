using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    internal interface ITokenizedStringListInternal : ITokenizedStringList
    {
        string OriginalSubstring(int startIndex, int length);
    }

    /// <summary>
    /// Tokenizes the string into abutting and non-overlapping segments.
    /// </summary>
    /// <remarks>
    /// This class implements IList so that it can be used with 
    /// <see cref="IDifferenceService" />, which finds the differences between two sequences represented
    /// as ILists.
    /// Most of the members of the IList interface are unimplemented. The only
    /// implemented methods are the array accessor getter (operator []), Count,
    /// and IsReadOnly.
    /// </remarks>
    internal abstract class TokenizedStringList : ITokenizedStringListInternal
    {
        protected List<int> Boundaries = new List<int>();
        private string original;
        private SnapshotSpan originalSpan;

        /// <summary>
        /// Creates a tokenized string list from the original string.
        /// Any derived class must initialize the boundaries list in its own constructor.
        /// </summary>
        /// <param name="original">The original string.</param>
        protected TokenizedStringList(string original)
        {
            if (original == null)
                throw new ArgumentNullException("original");

            this.original = original;
        }

        protected TokenizedStringList(SnapshotSpan originalSpan)
        {
            this.originalSpan = originalSpan;
        }

        /// <summary>
        /// The original string that was tokenized.
        /// </summary>
        public string Original
        {
            get
            {
                // A call to GetText() here could be very expensive in memory. Be careful!
                return original ?? originalSpan.GetText();
            }
        }

        internal int OriginalLength   //Internal for unit testing.
        {
            get
            {
                return (original != null) ? original.Length : originalSpan.Length;
            }
        }

        public string OriginalSubstring(int startIndex, int length)
        {
            if (original != null)
            {
                return original.Substring(startIndex, length);
            }
            else
            {
                ITextSnapshot snap = originalSpan.Snapshot;
                return snap.GetText(originalSpan.Start.Position + startIndex, length);
            }
        }

        /// <summary>
        /// Maps the index of an element to its span in the original list.
        /// </summary>
        /// <param name="index">The index of the element in the element list.</param>
        /// <returns>The span of the element.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The specified index is either negative or exceeds the list's Count property.</exception>
        /// <remarks>This method returns a zero-length span at the end of the string if index
        /// is equal to the list's Count property.</remarks>
        public Span GetElementInOriginal(int index)
        {
            if (index < 0 || index > this.Count)
                throw new ArgumentOutOfRangeException("index");

            //Pure support for backward compatibility
            if (index == this.Count)
            {
                return new Span(this.OriginalLength, 0);
            }

            int start = (index == 0) ? 0 : this.Boundaries[index - 1];
            int end = (index == this.Boundaries.Count) ? this.OriginalLength : this.Boundaries[index];

            return Span.FromBounds(start, end);
        }

        /// <summary>
        /// Maps a span of elements in this list to the span in the original list.
        /// </summary>
        /// <param name="span">The span of elements in the elements list.</param>
        /// <returns>The span mapped onto the original list.</returns>
        public Span GetSpanInOriginal(Span span)
        {
            int startIx = GetElementInOriginal(span.Start).Start;
            int endIx = (span.Length == 0) ? startIx : GetElementInOriginal(span.End - 1).End;

            return Span.FromBounds(startIx, endIx);
        }

        /// <summary>
        /// Gets a string of the given element.
        /// </summary>
        /// <param name="index">The index into the list of elements.</param>
        /// <returns>The element, as a string.</returns>
        /// <remarks>The setter of this property throws a NotImplementedException.</remarks>
        public string this[int index]
        {
            get
            {
                // The out of range check will happen in GetElementInOriginal
                Span span = GetElementInOriginal(index);
                return this.OriginalSubstring(span.Start, span.Length);
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// The number of elements in the list.
        /// </summary>
        public int Count
        {
            get 
            { 
                //Boundaries is are separators between tokens, so the number of tokens == boundaries + 1
                return this.Boundaries.Count + 1; 
            }
        }

        /// <summary>
        /// Determines whether this list is read-only. It always returnes <c>true</c>.
        /// </summary>
        public bool IsReadOnly
        {
            get { return true; }
        }

        #region Not Implemented
        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(string item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, string item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="item"></param>
        public void Add(string item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public bool Contains(string item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void CopyTo(string[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public bool Remove(string item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<string> Members

        /// <summary>
        /// Gets the enumerator of type string.
        /// </summary>
        /// <returns>The enumerator of type string.</returns>
        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Gets the untyped enumerator.
        /// </summary>
        /// <returns>The untyped enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
