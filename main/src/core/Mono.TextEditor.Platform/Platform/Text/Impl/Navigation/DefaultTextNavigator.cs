//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    using System;
    using System.Diagnostics;

    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Default Text Navigation helper.
    /// </summary>
    internal class DefaultTextNavigator : ITextStructureNavigator
    {
        #region Private Members

        ITextBuffer _textBuffer;
        IContentTypeRegistryService _contentTypeRegistry;

        #endregion // Private Members

        /// <summary>
        /// Keep the constructor internal so that only the factory can instantiate our class.
        /// </summary>
        /// <param name="textBuffer">
        /// The text buffer that we will navigate on.
        /// </param>
        /// <param name="contentTypeRegistry">
        /// The registry for <see cref="ContentType"/>s.
        /// </param>
        internal DefaultTextNavigator(ITextBuffer textBuffer, IContentTypeRegistryService contentTypeRegistry)
        {
            // Verify
            Debug.Assert(textBuffer != null);
            Debug.Assert(contentTypeRegistry != null);

            _textBuffer = textBuffer;
            _contentTypeRegistry = contentTypeRegistry;
        }

        #region ITextStructureNavigator Members

        /// <summary>
        /// Get the extent of the word at the given position. IsSignificant for the extent should be set to <c>false</c> for words 
        /// consisting of whitespace, unless the whitespace is a significant part of the document. If the 
        /// returned extent is insignificant whitespace, it should include all of the adjacent whitespace, 
        /// including newline characters, spaces, and tabs.
        /// </summary>
        /// <param name="currentPosition">
        /// The text position anywhere in the word whose extents are needed.
        /// </param>
        /// <returns>
        /// A <see cref="TextExtent" /> describing the word. IsSignificant will be set to false for whitespace or other 
        /// insignificant 'words' that should be ignored during navigation.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="currentPosition"/>is less than 0 or greater than the length of the text.</exception>
        public TextExtent GetExtentOfWord(SnapshotPoint currentPosition)
        {
            if (currentPosition.Snapshot.TextBuffer != _textBuffer)
            {
                throw new ArgumentException("currentPosition TextBuffer does not match to the current TextBuffer");
            }

            if (currentPosition.Position >= currentPosition.Snapshot.Length - 1)
            {
                // End of document
                return new TextExtent(new SnapshotSpan(currentPosition, 
                                                       currentPosition.Snapshot.Length - currentPosition),
                                      true);
            }
            else
            {
                return new TextExtent(new SnapshotSpan(currentPosition, 1), true);
            }
        }

        /// <summary>
        /// Get the span of the enclosing syntactic element given the currently active span.
        /// </summary>
        /// <param name="activeSpan">
        /// The active span from where to get the span of the enclosing syntactic element.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan"/> describing the enclosing syntactic element. If the given active
        /// span covers multiple syntactic elements, then the least common ancestor of the elements
        /// will be returned. If it already covers the root element of the document (a.k.a the whole document),
        /// then a <see cref="SnapshotSpan"/> of the same span will be returned.
        /// </returns>
        public SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan)
        {
            if (activeSpan.IsEmpty && (activeSpan.Start != activeSpan.Snapshot.Length))
            {
                return new SnapshotSpan(activeSpan.Start, 1);
            }
            return new SnapshotSpan(activeSpan.Snapshot, 0, activeSpan.Snapshot.Length);
        }

        /// <summary>
        /// Get the span of the first child syntactic element given the currently active span. 
        /// If the active span has zero length, then the default behavior would be the same to 
        /// GetExtentOfEnclosingParent.
        /// </summary>
        /// <param name="activeSpan">
        /// The active span from where to get the span of the first child syntactic element.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan" /> describing the first child syntactic element. If the given active 
        /// span covers multiple syntactic elements, then the span of the least common ancestor of 
        /// the elements will be returned. If it already covers the leaf level element of the document,
        /// then a <see cref="SnapshotSpan" /> of the same span will be returned. 
        /// (such that, when the same size span returned, we will try get the extent of its enclosing
        /// parent, which, for the third case above, will be the whole document or the whole syntactic 
        /// element depend on where the span lies). 
        /// </returns>
        public SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan)
        {
            if (activeSpan.IsEmpty)
            {
                return this.GetSpanOfEnclosing(activeSpan);
            }
            if (activeSpan.Length > 0 && activeSpan.Length < activeSpan.Snapshot.Length)
            {
                return new SnapshotSpan(activeSpan.Snapshot, 0, activeSpan.Snapshot.Length);
            }
            return new SnapshotSpan(activeSpan.Snapshot, 0, 1);
        }

        /// <summary>
        /// Get the span of the next sibling syntactic element given the currently active span. If the
        /// active span has zero length, then the default behavior would be the same to 
        /// GetExtentOfEnclosingParent.
        /// </summary>
        /// <param name="activeSpan">
        /// The active span from where to get the span of the next sibling syntactic element.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan"/> describing the next sibling syntactic element. If the given active
        /// span covers multiple syntactic elements, then the span of the next sibling element will be 
        /// returned. If text covered by the span doesn't followed by a sibling, then the default 
        /// behavior would be the same to GetExtentOfEnclosingParent.
        /// </returns>
        public SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan)
        {
            if (activeSpan.IsEmpty)
            {
                return this.GetSpanOfEnclosing(activeSpan);
            }
            if (activeSpan.End == activeSpan.Snapshot.Length)
            {
                return new SnapshotSpan(activeSpan.Snapshot, 0, activeSpan.Snapshot.Length);
            }

            return new SnapshotSpan(activeSpan.End, 1);
        }

        /// <summary>
        /// Get the span of the previous sibling syntactic element given the currently active span. 
        /// If the active span has zero length, then the default behavior would be the same to 
        /// GetExtentOfEnclosingParent.
        /// </summary>
        /// <param name="activeSpan">
        /// The active span from where to get the span of the previous sibling syntactic element.
        /// </param>
        /// <returns>
        /// A <see cref="SnapshotSpan"/> describing the next sibling syntactic element. If the given active
        /// span covers multiple syntactic elements, then the span of the previous element will be 
        /// returned. If text covered by the span doesn't preceded by a sibling, then the default 
        /// behavior would be the same to GetExtentOfEnclosingParent.
        /// </returns>
        public SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan)
        {
            if (activeSpan.IsEmpty)
            {
                return this.GetSpanOfEnclosing(activeSpan);
            }
            if (activeSpan.Start == 0)
            {
                return new SnapshotSpan(activeSpan.Snapshot, 0, activeSpan.Snapshot.Length);
            }

            return new SnapshotSpan(activeSpan.Start - 1, 1);
        }

        /// <summary>
        /// The content type that this navigator supports.
        /// </summary>
        public IContentType ContentType
        {
            get
            {
                return _contentTypeRegistry.UnknownContentType;
            }
        }

        #endregion // ITextStructureNavigator Members
    }
}
