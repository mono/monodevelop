using System.Collections.Generic;
using System.Windows;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Utilities
{
    internal sealed class WorkaroundMargin : System.Windows.Controls.ContentControl, IWpfTextViewMargin
    {
        public WorkaroundMargin()
        {
            this.IsTabStop = false;
        }

        public System.Windows.FrameworkElement VisualElement
        {
            get { return this; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return marginName == "Workaround" ? this : null;
        }

        public double MarginSize
        {
            get { return 0.0; }
        }

        public void Dispose()
        {
        }
    }

    class WorkaroundMetadata : IWpfTextViewMarginMetadata
    {
        private string[] emptyStrings = new string[0];

        public string MarginContainer
        {
            get { return string.Empty; }
        }

        public IEnumerable<string> Replaces
        {
            get { return emptyStrings; }
        }

        public string OptionName
        {
            get { return string.Empty; }
        }

        public GridUnitType GridUnitType
        {
            get { return GridUnitType.Auto; }
        }

        public double GridCellLength
        {
            get { return 0.0; }
        }

        public IEnumerable<string> After
        {
            get { return emptyStrings; }
        }

        public IEnumerable<string> Before
        {
            get { return emptyStrings; }
        }

        public string Name
        {
            get { return string.Empty; }
        }

        public IEnumerable<string> ContentTypes
        {
            get { return emptyStrings; }
        }

        public IEnumerable<string> TextViewRoles
        {
            get { return emptyStrings; }
        }
    }
}