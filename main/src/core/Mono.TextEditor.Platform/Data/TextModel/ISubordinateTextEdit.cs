namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;

    /// <summary>
    /// These methods augment ITextEdit to support editing of source buffers of projection and elision buffers.
    /// </summary>
    internal interface ISubordinateTextEdit
    {
        /// <summary>
        /// Compute effects of an edit on source buffers, if any, and add the source edits to the BufferGroup.
        /// </summary>
        void PreApply();

        /// <summary>
        /// Checks whether the edit on the buffer is allowed to continue.
        /// </summary>
        /// <param name="cancelAction">Action to perform immediately upon edit cancelation.</param>
        /// <returns>True if the edit can continue.</returns>
        bool CheckForCancellation(Action cancelAction);

        /// <summary>
        /// Commit effects of the edit, applying them to source buffers (if any).
        /// </summary>
        void FinalApply();

        /// <summary>
        /// The <see cref="ITextBuffer"/> to which this edit applies.
        /// </summary>
        ITextBuffer TextBuffer { get; }

        /// <summary>
        /// Restores any edit-in-progress state on the associated buffer.
        /// </summary>
        void CancelApplication();

        /// <summary>
        /// Whether the edit has been canceled.
        /// </summary>
        bool Canceled { get; }

        /// <summary>
        /// Mark the latest change in the edit as corresponding to a particular part of a master edit.
        /// </summary>
        /// <param name="masterChangeOffset">The index into the master edit's inserted text
        /// that corresponds to the text that's being inserted by this subordinate edit.</param>
        void RecordMasterChangeOffset(int masterChangeOffset);
    }
}
