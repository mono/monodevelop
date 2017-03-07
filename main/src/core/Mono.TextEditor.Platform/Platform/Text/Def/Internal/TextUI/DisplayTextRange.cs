//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Represents a range in the <see cref="TextBuffer"/> that behaves relative to the view in which it lives.
    /// </summary>
    public abstract class DisplayTextRange : TextRange, IEnumerable<DisplayTextPoint>
    {
        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextView"/> of this range.
        /// </summary>
        public abstract TextView TextView { get; }

        /// <summary>
        /// Creates a clone of this text range than can be moved independently of this one.
        /// </summary>
        public new DisplayTextRange Clone()
        {
            return CloneDisplayTextRangeInternal();
        }

        /// <summary>
        /// When implemented in a derived class, gets the start point of this text range.
        /// </summary>
        public abstract DisplayTextPoint GetDisplayStartPoint();

        /// <summary>
        /// When implemented in a derived class, gets the end point of this text range.
        /// </summary>
        public abstract DisplayTextPoint GetDisplayEndPoint();

        /// <summary>
        /// When implemented in a derived class, gets the visibility state of this text range.
        /// </summary>
        public abstract VisibilityState Visibility { get; }

        /// <summary>
        /// Clones this text range.
        /// </summary>
        /// <returns>The cloned <see cref="TextRange"/>.</returns>
        protected override TextRange CloneInternal()
        {
            return CloneDisplayTextRangeInternal();
        }

        /// <summary>
        /// When implemented in a derived class, clones the <see cref="DisplayTextRange"/>.
        /// </summary>
        protected abstract DisplayTextRange CloneDisplayTextRangeInternal();

        /// <summary>
        /// When implemented in a derived class, gets the enumerator of type <see cref="DisplayTextPoint"/>.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerator<DisplayTextPoint> GetDisplayPointEnumeratorInternal();

        #region IEnumerable<DisplayTextPoint> Members

        /// <summary>
        /// Gets an enumerator of type <see cref="DisplayTextPoint"/>.
        /// </summary>
        /// <returns></returns>
        public new IEnumerator<DisplayTextPoint> GetEnumerator()
        {
            return GetDisplayPointEnumeratorInternal();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
