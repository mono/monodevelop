using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using Microsoft.VisualStudio.Platform;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Gui;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    class DefaultSignatureHelpPresenterSurfaceElement
    {
        SignatureHelpSessionView dataContext;
        public SignatureHelpSessionView DataContext
        {
            get
            {
                return dataContext;
            }
            set
            {
                if (value == dataContext)
                    return;
                if (dataContext != null)
                    dataContext.PropertyChanged -= DataContext_PropertyChanged;
                dataContext = value;
                if (dataContext != null)
                    dataContext.PropertyChanged += DataContext_PropertyChanged;
                DataContext_PropertyChanged (this, new System.ComponentModel.PropertyChangedEventArgs (""));
            }
        }

        private void DataContext_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            while (vb.Children.Length > 0)
                vb.Remove(vb.Children[0]);
            if (DataContext != null)
            {
                vb.PackStart(DataContext.SignatureWpfViewVisualElement, true, true, 0);
                vb.PackStart(descriptionBox, true, true, 0);
            }
            ShowTooltipInfo();
        }

        VBox descriptionBox = new VBox(false, 0);
        VBox vb = new VBox(false, 0);
        VBox vb2 = new VBox(false, 0);
        Cairo.Color foreColor;
        public Xwt.Widget Content;
        private XwtPopupWindowTheme Theme { get => ((XwtThemedPopup)Content.ParentWindow).Theme; }
        public DefaultSignatureHelpPresenterSurfaceElement()
        {
            descriptionBox.Spacing = 4;


            HBox hb = new HBox(false, 0);
            hb.PackStart(vb, true, true, 0);

            vb2.Spacing = 4;
            vb2.PackStart(hb, true, true, 0);

            vb2.ShowAll();
            vb2.ParentSet += Vb2_ParentSet;
            Content = Xwt.Toolkit.CurrentEngine.WrapWidget(vb2, Xwt.NativeWidgetSizing.DefaultPreferredSize);

            Styles.Changed += HandleThemeChanged;
            IdeApp.Preferences.ColorScheme.Changed += HandleThemeChanged;
            Content.Disposed += Content_Disposed;
        }

        private void Vb2_ParentSet(object o, ParentSetArgs args)
        {
            ShowTooltipInfo();
        }

        void UpdateStyle()
        {
            var scheme = SyntaxHighlightingService.GetEditorTheme(IdeApp.Preferences.ColorScheme);
            if (!scheme.FitsIdeTheme(IdeApp.Preferences.UserInterfaceTheme))
                scheme = SyntaxHighlightingService.GetDefaultColorStyle(IdeApp.Preferences.UserInterfaceTheme);
            Theme.SetSchemeColors(scheme);
            Theme.Font = FontService.SansFont.CopyModified(Styles.FontScale11).ToXwtFont();
            Theme.ShadowColor = Styles.PopoverWindow.ShadowColor;
            foreColor = Styles.PopoverWindow.DefaultTextColor.ToCairoColor();

            if (DataContext != null)
            {
                DataContext.SignatureWpfViewVisualElement.ModifyFg(StateType.Normal, foreColor.ToGdkColor());
                DataContext.SignatureWpfViewVisualElement.FontDescription = FontService.GetFontDescription("Editor").CopyModified(Styles.FontScale11);
            }
            //if (this.Visible)
            //	QueueDraw ();
        }

        void HandleThemeChanged(object sender, EventArgs e)
        {
            UpdateStyle();
        }


        private void Content_Disposed(object sender, EventArgs e)
        {
            if (Content == null)
                return;
            Styles.Changed -= HandleThemeChanged;
            IdeApp.Preferences.ColorScheme.Changed -= HandleThemeChanged;
            Content.Disposed -= Content_Disposed;
            ((XwtThemedPopup)Content.ParentWindow).PagerLeftClicked -= DownButtonClick;
            ((XwtThemedPopup)Content.ParentWindow).PagerRightClicked -= UpButtonClick;
            Content = null;
        }
        bool arrowEventsRegistered;
        void ShowTooltipInfo()
        {
            if (Content.ParentWindow == null)
                return;
            if (DataContext == null)
                return;
            if (!arrowEventsRegistered)
            {
                UpdateStyle();
                arrowEventsRegistered = true;
                ((XwtThemedPopup)Content.ParentWindow).PagerLeftClicked += DownButtonClick;
                ((XwtThemedPopup)Content.ParentWindow).PagerRightClicked += UpButtonClick;
            }
            Theme.NumPages = DataContext.Session.Signatures.Count;
            Theme.CurrentPage = DataContext.Session.Signatures.IndexOf(dataContext.Session.SelectedSignature);

            if (Theme.NumPages > 1)
            {
                Theme.DrawPager = true;
                Theme.PagerVertical = true;
            }
            ClearDescriptions();
            if (Theme.DrawPager)
                DataContext.SignatureWpfViewVisualElement.WidthRequest = DataContext.SignatureWpfViewVisualElement.RealWidth + 70;

            if (!string.IsNullOrEmpty(dataContext.Session.SelectedSignature.Documentation))
                descriptionBox.PackStart(CreateCategory(TooltipInformationWindow.GetHeaderMarkup(GettextCatalog.GetString("Summary")), dataContext.Session.SelectedSignature.Documentation), true, true, 4);

            descriptionBox.ShowAll();
            Content.QueueForReallocate();
        }

        public event EventHandler UpButtonClick;
        public event EventHandler DownButtonClick;

        void CurrentTooltipInformation_Changed(object sender, EventArgs e)
        {
            ShowTooltipInfo();
        }

        void ClearDescriptions()
        {
            while (descriptionBox.Children.Length > 0)
            {
                var child = descriptionBox.Children[0];
                descriptionBox.Remove(child);
                child.Destroy();
            }
        }

        VBox CreateCategory(string categoryName, string categoryContentMarkup)
        {
            return TooltipInformationWindow.CreateCategory(categoryName, categoryContentMarkup, foreColor, Theme.Font.ToPangoFont());
        }

        internal void Hide()
        {
            //Content.Hide ();
        }
    }
}