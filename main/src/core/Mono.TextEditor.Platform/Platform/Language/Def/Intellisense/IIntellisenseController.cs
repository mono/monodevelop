////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Controls the IntelliSense process for one or more subject <see cref="ITextBuffer"/> objects
    /// exposed through a single <see cref="ITextView"/>.
    /// </summary>
    public interface IIntellisenseController
    {
        /// <summary>
        /// Detaches the controller from the specified <see cref="ITextView" />.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView" /> from which the controller should detach.</param>
        void Detach ( ITextView textView );

        /// <summary>
        /// Called when a new subject <see cref="ITextBuffer"/> appears in the graph of buffers associated with
        /// the <see cref="ITextView"/>, due to a change in projection or content type.
        /// </summary>
        /// <param name="subjectBuffer">The newly-connected text buffer.</param>
        void ConnectSubjectBuffer(ITextBuffer subjectBuffer);

        /// <summary>
        /// Called when a subject <see cref="ITextBuffer"/> is removed from the graph of buffers associated with
        /// the <see cref="ITextView"/>, due to a change in projection or content type. 
        /// </summary>
        /// <param name="subjectBuffer">The disconnected text buffer.</param>
        /// <remarks>
        /// It is not guaranteed that
        /// the subject buffer was previously connected to this controller.
        /// </remarks>
        void DisconnectSubjectBuffer(ITextBuffer subjectBuffer);
    }
}
