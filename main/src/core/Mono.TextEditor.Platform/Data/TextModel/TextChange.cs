// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using Microsoft.VisualStudio.Text.Utilities;

    /// <summary>
    /// Describes a single contiguous atomic text change operation on the Text Buffer.
    /// 
    /// All text changes are modeled as the replacement of parameter oldText with parameter newText
    /// 
    /// Insertion: oldText == "" and newText != ""
    /// Deletion:  oldText != "" and newText == ""
    /// Replace:   oldText != "" and newText != ""
    /// </summary>
    internal partial class TextChange : ITextChange2
    {
        #region Private Members

        private int _oldPosition;
        private int _newPosition;
        internal ChangeString _oldText, _newText;
        private int? _lineCountDelta = null;
        private LineBreakBoundaryConditions _lineBreakBoundaryConditions;
        private bool _isOpaque;
        private int _masterChangeOffset = -1;

        #endregion // Private Members

        /// <summary>
        /// Constructs a Text Change object.
        /// </summary>
        /// <param name="oldPosition">
        /// The character position in the TextBuffer at which the text change happened.
        /// </param>
        /// <param name="oldText">
        /// The text in the buffer that was replaced.
        /// </param>
        /// <param name="newText">
        /// The text that replaces the old text.
        /// </param>
        /// <param name="boundaryConditions">
        /// Information about neighboring line break characters.
        /// </param>
        public TextChange(int oldPosition, string oldText, string newText, LineBreakBoundaryConditions boundaryConditions)
        {
            if (oldPosition < 0)
            {
                throw new ArgumentOutOfRangeException("oldPosition");
            }
            _oldPosition = oldPosition;
            _newPosition = oldPosition;
            _oldText = new LiteralChangeString(oldText);
            _newText = new LiteralChangeString(newText);
            _lineBreakBoundaryConditions = boundaryConditions;
        }

        /// <summary>
        /// Constructs a Text Change object.
        /// </summary>
        /// <param name="oldPosition">
        /// The character position in the TextBuffer at which the text change happened.
        /// </param>
        /// <param name="oldText">
        /// The text in the buffer that was replaced.
        /// </param>
        /// <param name="newText">
        /// The text that replaces the old text.
        /// </param>
        /// <param name="boundaryConditions">
        /// Information about neighboring line break characters.
        /// </param>
        public TextChange(int oldPosition, ChangeString oldText, ChangeString newText, LineBreakBoundaryConditions boundaryConditions)
        {
            if (oldPosition < 0)
            {
                throw new ArgumentOutOfRangeException("oldPosition");
            }
            _oldPosition = oldPosition;
            _newPosition = oldPosition;
            _oldText = oldText;
            _newText = newText;
            _lineBreakBoundaryConditions = boundaryConditions;
        }

        /// <summary>
        /// Constructs a Text Change object.
        /// </summary>
        /// <param name="oldPosition">
        /// The character position in the TextBuffer at which the text change happened.
        /// </param>
        /// <param name="oldText">
        /// The text in the buffer that was replaced.
        /// </param>
        /// <param name="newText">
        /// The text that replaces the old text.
        /// </param>
        /// <param name="currentSnapshot">
        /// Context in which change occurs.
        /// </param>
        public TextChange(int oldPosition, string oldText, string newText, ITextSnapshot currentSnapshot) :
            this(oldPosition, oldText, newText, ComputeLineBreakBoundaryConditions(currentSnapshot, oldPosition, oldText.Length))
        {
        }

        /// <summary>
        /// Constructs a Text Change object.
        /// </summary>
        /// <param name="oldPosition">
        /// The character position in the TextBuffer at which the text change happened.
        /// </param>
        /// <param name="oldText">
        /// The text in the buffer that was replaced.
        /// </param>
        /// <param name="newText">
        /// The text that replaces the old text.
        /// </param>
        /// <param name="currentSnapshot">
        /// Context in which change occurs.
        /// </param>
        public TextChange(int oldPosition, ChangeString oldText, ChangeString newText, ITextSnapshot currentSnapshot) :
            this(oldPosition, oldText, newText, ComputeLineBreakBoundaryConditions(currentSnapshot, oldPosition, oldText.Length))
        {
        }

        #region Public Properties

        public Span OldSpan
        {
            get { return new Span(_oldPosition, _oldText.Length);  }
        }

        public Span NewSpan
        {
            get { return new Span(_newPosition, _newText.Length); }
        }

        public int OldPosition
        {
            get { return _oldPosition; }
            internal set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                _oldPosition = value;
            }
        }

        public int NewPosition
        {
            get { return _newPosition; }
            internal set 
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                _newPosition = value; 
            }
        }
        
        public int Delta
        {
            get { return _newText.Length - _oldText.Length; }
        }

        public int OldEnd
        {
            get { return _oldPosition + _oldText.Length; }
        }

        public int NewEnd
        {
            get { return _newPosition + _newText.Length; }
        }

        public string OldText
        {
            get { return _oldText.Text; }
        }

        public string NewText
        {
            get { return _newText.Text; }
        }

        public int NewLength
        {
            get { return _newText.Length; }
        }

        public int OldLength
        {
            get { return _oldText.Length; }
        }

        public int LineCountDelta
        {
            get
            {
                // we are lazy
                if (!_lineCountDelta.HasValue)
                {
                    _lineCountDelta = TextModelUtilities.ComputeLineCountDelta(_lineBreakBoundaryConditions, _oldText, _newText);
                }
                return _lineCountDelta.Value; 
            }
        }

        public bool IsOpaque
        {
            get { return _isOpaque; }
            internal set { _isOpaque = value; }
        }
        #endregion // Public Properties

        #region Internal Properties
        internal LineBreakBoundaryConditions LineBreakBoundaryConditions
        {
            get { return _lineBreakBoundaryConditions; }
            set
            {
                _lineBreakBoundaryConditions = value;
                _lineCountDelta = null;
            }
        }

        /// <summary>
        /// Retrieves the old text as a <see cref="ChangeString"/>.
        /// </summary>
        internal ChangeString OldChangeString
        {
            get { return _oldText; }
        }

        /// <summary>
        /// Retrieves the new text as a <see cref="ChangeString"/>.
        /// </summary>
        internal ChangeString NewChangeString
        {
            get { return _newText; }
        }

        internal void RecordMasterChangeOffset(int masterChangeOffset)
        {
            if (masterChangeOffset < 0)
                throw new ArgumentOutOfRangeException("masterChangeOffset", "MasterChangeOffset should be non-negative.");
            if (_masterChangeOffset != -1)
                throw new InvalidOperationException("MasterChangeOffset has already been set.");
            _masterChangeOffset = masterChangeOffset;
        }

        internal int MasterChangeOffset { get { return _masterChangeOffset == -1 ? 0 : _masterChangeOffset; } }

        internal static int Compare(TextChange x, TextChange y)
        {
            int diff = x.OldPosition - y.OldPosition;
            if (diff != 0)
                return diff;
            else
                return x.MasterChangeOffset - y.MasterChangeOffset;
        }

        #endregion

        #region Private helper
        private static LineBreakBoundaryConditions ComputeLineBreakBoundaryConditions(ITextSnapshot currentSnapshot, int position, int oldLength)
        {
            LineBreakBoundaryConditions conditions = LineBreakBoundaryConditions.None;
            if (position > 0 && currentSnapshot[position - 1] == '\r')
            {
                conditions = LineBreakBoundaryConditions.PrecedingReturn;
            }
            int end = position + oldLength;
            if (end < currentSnapshot.Length && currentSnapshot[end] == '\n')
            {
                conditions = conditions | LineBreakBoundaryConditions.SucceedingNewline;
            }
            return conditions;
        }
        #endregion

        #region Overridden methods
        public string ToString(bool brief)
        {
            if (brief)
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "old={0} new={1}", this.OldSpan, this.NewSpan);
            }
            else
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                     "old={0}:'{1}' new={2}:'{3}'",
                                     this.OldSpan, TextUtilities.Escape(this.OldText), this.NewSpan, TextUtilities.Escape(this.NewText, 40));
            }
        }

        public override string ToString()
        {
            return ToString(false);
        }
        #endregion
    }
}
