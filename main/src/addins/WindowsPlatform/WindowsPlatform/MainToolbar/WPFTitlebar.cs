using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Components.Windows;

namespace WindowsPlatform.MainToolbar
{
    public class WPFTitlebar : GtkWPFWidget
    {
        TitleBar titlebar;

        public WPFTitlebar (TitleBar titlebar) : base (titlebar)
        {
            this.titlebar = titlebar;
            HeightRequest = System.Windows.Forms.SystemInformation.CaptionHeight;
        }

        protected override void RepositionWpfWindow()
        {
            int scale = (int)MonoDevelop.Components.GtkWorkarounds.GetScaleFactor (this);

            RepositionWpfWindow (scale, 1);
        }
    }
}
