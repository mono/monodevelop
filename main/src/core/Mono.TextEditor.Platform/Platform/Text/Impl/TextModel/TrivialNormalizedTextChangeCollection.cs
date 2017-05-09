//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;

    /// <summary>
    /// A compact representation of a normalized text change collection containing a single change that is the
    /// insertion, deletion, or replacement of one character, with no effect on line counts. This object embodies both the
    /// collection and its single member.
    /// </summary>
    internal partial class TrivialNormalizedTextChangeCollection : INormalizedTextChangeCollection, ITextChange3
    {
        char data;
        bool isInsertion;
        int position;

        public TrivialNormalizedTextChangeCollection(char data, bool isInsertion, int position)
        {
            this.data = data;
            this.isInsertion = isInsertion;
            this.position = position;
        }

        public bool IncludesLineChanges
        {
            get { return false; }
        }

        #region IList<ITextChange> implementation
        public ITextChange this[int index]
        {
            get
            {
                if (index != 0)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                return this;
            }
            set
            {
                throw new System.NotSupportedException();
            }
        }

        public void Insert(int index, ITextChange item)
        {
            throw new System.NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new System.NotSupportedException();
        }
        #endregion

        #region ICollection<ITextChange> implementation
        public int Count
        {
            get { return 1; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public void Add(ITextChange item)
        {
            throw new System.NotSupportedException();
        }

        public void Clear()
        {
            throw new System.NotSupportedException();
        }

        public int IndexOf(ITextChange item)
        {
            return item == this ? 0 : -1;
        }

        public bool Contains(ITextChange item)
        {
            return item == this;
        }

        public void CopyTo(ITextChange[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }
            if (array.Rank > 1 || arrayIndex >= array.Length)
            {
                throw new ArgumentException("Bad arguments to CopyTo");
            }
            array[arrayIndex] = this;
        }

        public bool Remove(ITextChange item)
        {
            throw new System.NotSupportedException();
        }
        #endregion

        #region IEnumerable<ITextChange> implementation
        public System.Collections.Generic.IEnumerator<ITextChange> GetEnumerator()
        {
            yield return this;
        }
        #endregion

        #region IEnumerable implementation
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            yield return this;
        }
        #endregion

        #region ITextChange implementation
        Span ITextChange.OldSpan
        {
            get { return new Span(position, isInsertion ? 0 : 1); }
        }

        Span ITextChange.NewSpan
        {
            get { return new Span(position, isInsertion ? 1 : 0); }
        }

        int ITextChange.OldPosition
        {
            get { return position; }
        }

        int ITextChange.NewPosition
        {
            get { return position; }
        }

        int ITextChange.Delta
        {
            get { return isInsertion ? +1 : -1; }
        }

        int ITextChange.OldEnd
        {
            get { return isInsertion ? position : position + 1; }
        }

        int ITextChange.NewEnd
        {
            get { return isInsertion ? position + 1 : position; }
        }

        string ITextChange.OldText
        {
            get { return isInsertion ? "" : new string(data, 1); }
        }

        string ITextChange.NewText
        {
            get { return isInsertion ? new string(data, 1) : ""; }
        }

        int ITextChange.OldLength
        {
            get { return isInsertion ? 0 : 1; }
        }

        int ITextChange.NewLength
        {
            get { return isInsertion ? 1 : 0; }
        }

        int ITextChange.LineCountDelta
        {
            get { return 0; }
        }

        public bool IsOpaque { get; internal set; }

        public string GetOldText(Span span)
        {
            if (span.End > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            return isInsertion ? "" : new string(data, span.Length);
        }

        public string GetNewText(Span span)
        {
            if (span.End > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            return isInsertion ? new string(data, span.Length) : "";
        }

        public char GetOldTextAt(int position)
        {
            if (position > 0 || isInsertion)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return data;
        }

        public char GetNewTextAt(int position)
        {
            if (position > 0 || !isInsertion)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return data;
        }

        #endregion
    }
}