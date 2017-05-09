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
    using System.Collections.Generic;

    internal partial class TextSnapshot : BaseSnapshot, ITextSnapshot
    {
        #region Private members
        private readonly ITextBuffer textBuffer;
        private readonly StringRebuilder content;

        private Tuple<int, StringRebuilder> cachedLeaf;
        #endregion

        #region Constructors
        public TextSnapshot(ITextBuffer textBuffer, ITextVersion version, StringRebuilder content)
          : base(version)
        {
            System.Diagnostics.Debug.Assert(version.Length == content.Length);
            this.textBuffer = textBuffer;
            this.content = content;
        }
        #endregion

        #region Text fetching
        protected override ITextBuffer TextBufferHelper
        {
            get { return this.textBuffer; }
        }

        public override int Length
        {
            get { return this.content.Length; }
        }

        public override int LineCount
        {
            get { return this.content.LineBreakCount + 1; }
        }

        public override string GetText(Span span)
        {
            Tuple<int, StringRebuilder> cache = this.UpdateCache(span.Start);
           
            int offsetStart = span.Start - cache.Item1;
            if (offsetStart + span.Length < cache.Item2.Length)
            {
                return cache.Item2.GetText(new Span(offsetStart, span.Length));
            }

            return this.content.GetText(span);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.content.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override char[] ToCharArray(int startIndex, int length)
        {
            return this.content.ToCharArray(startIndex, length);
        }

        public override char this[int position]
        {
            get
            {
                Tuple<int, StringRebuilder> cache = this.UpdateCache(position);

                return cache.Item2[position - cache.Item1];
            }
        }
        #endregion

        #region Line Methods
        public override ITextSnapshotLine GetLineFromLineNumber(int lineNumber)
        {
            LineSpan lineSpan = this.content.GetLineFromLineNumber(lineNumber);

            return new TextSnapshotLine(this, lineSpan);
        }

        public override ITextSnapshotLine GetLineFromPosition(int position)
        {
            int lineNumber = this.content.GetLineNumberFromPosition(position);
            return this.GetLineFromLineNumber(lineNumber);
        }

        public override int GetLineNumberFromPosition(int position)
        {
            return this.content.GetLineNumberFromPosition(position);
        }

        public override IEnumerable<ITextSnapshotLine> Lines
        {
            get
            {
                // this is a naive implementation
                for (int line = 0; line < this.LineCount; ++line)
                {
                    yield return GetLineFromLineNumber(line);
                }
            }
        }
        #endregion

        #region Writing
        public override void Write(System.IO.TextWriter writer)
        {
            this.content.Write(writer, new Span(0, this.content.Length));
        }

        public override void Write(System.IO.TextWriter writer, Span span)
        {
            this.content.Write(writer, span);
        }
        #endregion

        public StringRebuilder Content { get { return this.content; } }

        private Tuple<int, StringRebuilder> UpdateCache(int position)
        {
            Tuple<int, StringRebuilder> cache = this.cachedLeaf;
            if ((cache == null) || (position < cache.Item1) || (position >= (cache.Item1 + cache.Item2.Length)))
            {
                int offset;
                StringRebuilder leaf = this.content.GetLeaf(position, out offset);

                cache = new Tuple<int, StringRebuilder>(offset, leaf);

                //Since cache is a class, cachedLeaf should update atomically.
                this.cachedLeaf = cache;
            }

            return cache;
        }
    }
}
