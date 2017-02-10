// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{
    using System;
    using System.Linq;
    using System.Windows.Automation.Provider;

    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Provides a value pattern for the selection of the text view.
    /// </summary>
    public class SelectionValuePatternProvider : PatternProvider, IValueProvider
    {

        public SelectionValuePatternProvider(IWpfTextView wpfTextView) : base(wpfTextView) { }

        #region IValueProvider Members

        /// <summary>
        /// Gets a value that specifies whether the value of the control is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get 
            {
                if (TextView.Selection.IsEmpty)
                    return true;

                if (TextView.Options.GetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId))
                    return true;

                foreach (var span in TextView.Selection.SelectedSpans)
                {
                    if (TextView.TextBuffer.IsReadOnly(span))
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Sets the value of the control.
        /// </summary>
        public void SetValue(string value)
        {
            if (!TextView.Selection.IsEmpty)
            {
                //track current selection
                SnapshotSpan selectionStart = new SnapshotSpan(TextView.Selection.Start.Position, 0);
                ITrackingSpan newSelection = TextView.TextSnapshot.CreateTrackingSpan(selectionStart, SpanTrackingMode.EdgeInclusive);
                bool isReversed = TextView.Selection.IsReversed;

                
                //apply text change
                using (ITextEdit textEdit = TextView.TextBuffer.CreateEdit())
                {
                    // First, delete the selection
                    foreach (var span in TextView.Selection.SelectedSpans)
                    {
                        textEdit.Delete(span);
                    }

                    // Now, do the insertion
                    textEdit.Insert(selectionStart.Start, value);

                    textEdit.Apply();
                }

                //reselect new value
                TextView.Selection.Select(newSelection.GetSpan(TextView.TextSnapshot), isReversed);
                TextView.Caret.MoveTo(TextView.Selection.ActivePoint);
            }
        }

        /// <summary>
        /// Get the value of the control
        /// </summary>
        public string Value
        {
            get 
            {
                if (TextView.Selection.IsEmpty)
                {
                    return string.Empty;
                }
                else
                {
                    if (TextView.Options.GetOptionValue(DefaultTextViewOptions.ProduceScreenReaderFriendlyTextId))
                    {
                        return string.Join(System.Environment.NewLine, 
                            TextView.Selection.SelectedSpans.Select(
                            (span) => ScreenReaderTranslator.Translate(span, TextView.TextViewModel.DataBuffer.ContentType))
                            .ToArray());
                    }
                    else
                    {
                        return string.Join(System.Environment.NewLine, TextView.Selection.SelectedSpans
                                                                               .Select((span) => span.GetText())
                                                                               .ToArray());
                    }
                }
            }
        }

        #endregion //IValueProvider Members
    }
}
