// Copyright (c) Microsoft Corporation
// All rights reserved

// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{

    using System.Windows.Automation.Provider;

    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Provides a value pattern for the <see cref="IWpfTextView"/>. This pattern supplies the entire
    /// content of the view as its value.
    /// </summary>
    public class ViewValuePatternProvider : PatternProvider, IValueProvider
    {

        public ViewValuePatternProvider(IWpfTextView wpfTextView) : base(wpfTextView) { }

        #region IValueProvider Members

        /// <summary>
        /// Gets a value that specifies whether the value of the control is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get 
            {
                return TextView.Options.GetOptionValue<bool>(DefaultTextViewOptions.ViewProhibitUserInputId);
            }
        }

        /// <summary>
        /// Sets the value of the control.
        /// </summary>
        public void SetValue(string value)
        {
            //clear selection if any exists
            if (!TextView.Selection.IsEmpty)
            {
                TextView.Selection.Clear();
            }
            
            //apply text change
            using (ITextEdit textEdit = TextView.TextBuffer.CreateEdit())
            {
                textEdit.Replace(new SnapshotSpan(TextView.TextSnapshot, 0, TextView.TextSnapshot.Length), value);
                textEdit.Apply();
            }
        }

        /// <summary>
        /// Get the value of the control
        /// </summary>
        public string Value
        {
            get 
            {
                SnapshotSpan text = new SnapshotSpan(TextView.TextSnapshot, 0, TextView.TextSnapshot.Length);
                return TextView.Options.GetOptionValue<bool>(DefaultTextViewOptions.ProduceScreenReaderFriendlyTextId) ? 
                    ScreenReaderTranslator.Translate(text, TextView.TextViewModel.DataBuffer.ContentType) :
                    TextView.TextSnapshot.GetText(text);
            }
        }

        #endregion //IValueProvider Members
    }
}
