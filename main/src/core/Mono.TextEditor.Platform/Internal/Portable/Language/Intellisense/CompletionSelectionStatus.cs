////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents the full selection status of a completion set. 
    /// </summary>
    /// <remarks>
    /// Completion sets maintain their own selection status, which is a
    /// combination of a completion item, a value indicating whether or not the completion is fully selected, and a value
    /// indicating whether or not the completion is a unique match.
    /// </remarks>
    public class CompletionSelectionStatus
    {
        private Completion _completion;
        private bool _isSelected;
        private bool _isUnique;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompletionSelectionStatus"/>.
        /// </summary>
        /// <param name="completion">The selected completion in this selection status.</param>
        /// <param name="isSelected"><c>true</c> if the completion is fully-selected, <c>false</c> otherwise.</param>
        /// <param name="isUnique"><c>true</c> if the completion is a unique match, <c>false</c> otherwise.</param>
        public CompletionSelectionStatus(Completion completion, bool isSelected, bool isUnique)
        {
            _completion = completion;
            _isSelected = isSelected;
            _isUnique = isUnique;
        }

        /// <summary>
        /// Gets the selected completion represented by this selection status instance.
        /// </summary>
        public Completion Completion
        {
            get { return _completion; }
        }

        /// <summary>
        /// Determines whether the completion is fully-selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
        }

        /// <summary>
        /// Determines whether the completion is a unique match.
        /// </summary>
        public bool IsUnique
        {
            get { return _isUnique; }
        }

        /// <summary>
        /// Determines whether two instances of <see cref="CompletionSelectionStatus"/> are the same.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            CompletionSelectionStatus otherStatus = obj as CompletionSelectionStatus;
            if (otherStatus != null)
            {
                return this == otherStatus;
            }

            return false;
        }

        /// <summary>
        /// Gets the hash code of this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Determines whether two instances of <see cref="CompletionSelectionStatus"/> are the same.
        /// </summary>
        /// <param name="status1">The first instance.</param>
        /// <param name="status2">The second instance.</param>
        /// <returns><c>true</c> if the instances are the same, otherwise <c>false</c>.</returns>
        public static bool operator ==(CompletionSelectionStatus status1, CompletionSelectionStatus status2)
        {
            if (object.ReferenceEquals(status1, status2))
            {
                return true;
            }
            if (object.ReferenceEquals(status1, null) || object.ReferenceEquals(status2, null))
            {
                return false;
            }

            if (
                (status1.Completion == status2.Completion) &&
                (status1.IsSelected == status2.IsSelected) &&
                (status1.IsUnique == status2.IsUnique)
               )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether two instances of <see cref="CompletionSelectionStatus"/> are different.
        /// </summary>
        /// <param name="status1">The first instance.</param>
        /// <param name="status2">The second instance.</param>
        /// <returns><c>true</c> if the instances are different, otherwise <c>false</c>.</returns>
        public static bool operator !=(CompletionSelectionStatus status1, CompletionSelectionStatus status2)
        {
            return !(status1 == status2);
        }
    }
}
