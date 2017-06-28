//
// GtkNewProjectDialogBackend.UI.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Ide.Projects
{
	partial class GtkNewProjectDialogBackend : IdeDialog
	{
		Color bannerBackgroundColor = Styles.NewProjectDialog.BannerBackgroundColor.ToGdkColor ();
		Color bannerLineColor = Styles.NewProjectDialog.BannerLineColor.ToGdkColor ();
		Color whiteColor = Styles.NewProjectDialog.BannerForegroundColor.ToGdkColor ();
		Color categoriesBackgroundColor = Styles.NewProjectDialog.CategoriesBackgroundColor.ToGdkColor ();
		Color templateListBackgroundColor = Styles.NewProjectDialog.TemplateListBackgroundColor.ToGdkColor ();
		Color templateBackgroundColor = Styles.NewProjectDialog.TemplateBackgroundColor.ToGdkColor ();
		Color templateSectionSeparatorColor = Styles.NewProjectDialog.TemplateSectionSeparatorColor.ToGdkColor ();

		VBox centreVBox;
		HBox templatesHBox;
		Button cancelButton;
		Button previousButton;
		Button nextButton;
		Label topBannerLabel;

		TreeView templateCategoriesTreeView;
		const int TemplateCategoryNameColumn = 0;
		const int TemplateCategoryIconColumn = 1;
		const int TemplateCategoryColumn = 2;
		ListStore templateCategoriesListStore =
			new ListStore(typeof (string), typeof (Xwt.Drawing.Image), typeof(TemplateCategory));
		TreeView templatesTreeView;
		const int TemplateNameColumn = 0;
		const int TemplateIconColumn = 1;
		const int TemplateColumn = 2;
		ListStore templatesListStore =
			new ListStore(typeof (string), typeof (Xwt.Drawing.Image), typeof(SolutionTemplate));
		VBox templateVBox;
		ImageView templateImage;
		Label templateNameLabel;
		Label templateDescriptionLabel;
		GtkProjectConfigurationWidget projectConfigurationWidget;
		GtkTemplateCellRenderer templateTextRenderer;
		GtkTemplateCategoryCellRenderer categoryTextRenderer;
		LanguageCellRenderer languageCellRenderer;

		static GtkNewProjectDialogBackend ()
		{
			UpdateStyles ();
			Styles.Changed += (sender, e) => UpdateStyles ();
		}

		static void UpdateStyles ()
		{
			var categoriesBackgroundColorHex = Styles.ColorGetHex (Styles.NewProjectDialog.CategoriesBackgroundColor);
			var templateListBackgroundColorHex = Styles.ColorGetHex (Styles.NewProjectDialog.TemplateListBackgroundColor);

			string rcstyle = "style \"templateCategoriesTreeView\"\r\n{\r\n" +
				"    base[NORMAL] = \"" + categoriesBackgroundColorHex + "\"\r\n" +
				"    GtkTreeView::even-row-color = \"" + categoriesBackgroundColorHex + "\"\r\n" +
				"}\r\n";
			rcstyle += "style \"templatesTreeView\"\r\n{\r\n" +
				"    base[NORMAL] = \"" + templateListBackgroundColorHex + "\"\r\n" +
				"    GtkTreeView::even-row-color = \"" + templateListBackgroundColorHex + "\"" +
				"\r\n}";

			rcstyle += "widget \"*templateCategoriesTreeView*\" style \"templateCategoriesTreeView\"\r\n";
			rcstyle += "widget \"*templatesTreeView*\" style \"templatesTreeView\"\r\n";

			Rc.ParseString (rcstyle);
		}

		void Build ()
		{
			BorderWidth = 0;
			DefaultWidth = 901;
			DefaultHeight = 632;

			Name = "wizard_dialog";
			Title = GettextCatalog.GetString ("New Project");
			WindowPosition = WindowPosition.CenterOnParent;
			TransientFor = IdeApp.Workbench.RootWindow;

			projectConfigurationWidget = new GtkProjectConfigurationWidget ();
			projectConfigurationWidget.Name = "projectConfigurationWidget";

			// Top banner of dialog.
			var topLabelEventBox = new EventBox ();
			topLabelEventBox.Accessible.SetShouldIgnore (true);
			topLabelEventBox.Name = "topLabelEventBox";
			topLabelEventBox.HeightRequest = 52;
			topLabelEventBox.ModifyBg (StateType.Normal, bannerBackgroundColor);
			topLabelEventBox.ModifyFg (StateType.Normal, whiteColor);
			topLabelEventBox.BorderWidth = 0;

			var topBannerTopEdgeLineEventBox = new EventBox ();
			topBannerTopEdgeLineEventBox.Accessible.SetShouldIgnore (true);
			topBannerTopEdgeLineEventBox.Name = "topBannerTopEdgeLineEventBox";
			topBannerTopEdgeLineEventBox.HeightRequest = 1;
			topBannerTopEdgeLineEventBox.ModifyBg (StateType.Normal, bannerLineColor);
			topBannerTopEdgeLineEventBox.BorderWidth = 0;

			var topBannerBottomEdgeLineEventBox = new EventBox ();
			topBannerBottomEdgeLineEventBox.Accessible.SetShouldIgnore (true);
			topBannerBottomEdgeLineEventBox.Name = "topBannerBottomEdgeLineEventBox";
			topBannerBottomEdgeLineEventBox.HeightRequest = 1;
			topBannerBottomEdgeLineEventBox.ModifyBg (StateType.Normal, bannerLineColor);
			topBannerBottomEdgeLineEventBox.BorderWidth = 0;

			topBannerLabel = new Label ();
			topBannerLabel.Name = "topBannerLabel";
			topBannerLabel.Accessible.Name = "topBannerLabel";
			Pango.FontDescription font = topBannerLabel.Style.FontDescription.Copy (); // UNDONE: VV: Use FontService?
			font.Size = (int)(font.Size * 1.8);
			topBannerLabel.ModifyFont (font);
			topBannerLabel.ModifyFg (StateType.Normal, whiteColor);
			var topLabelHBox = new HBox ();
			topLabelHBox.Accessible.SetShouldIgnore (true);
			topLabelHBox.Name = "topLabelHBox";
			topLabelHBox.PackStart (topBannerLabel, false, false, 20);
			topLabelEventBox.Add (topLabelHBox);

			VBox.PackStart (topBannerTopEdgeLineEventBox, false, false, 0);
			VBox.PackStart (topLabelEventBox, false, false, 0);
			VBox.PackStart (topBannerBottomEdgeLineEventBox, false, false, 0);

			// Main templates section.
			centreVBox = new VBox ();
			centreVBox.Accessible.SetShouldIgnore (true);
			centreVBox.Name = "centreVBox";
			VBox.PackStart (centreVBox, true, true, 0);
			templatesHBox = new HBox ();
			templatesHBox.Accessible.SetShouldIgnore (true);
			templatesHBox.Name = "templatesHBox";
			centreVBox.PackEnd (templatesHBox, true, true, 0);

			// Template categories.
			var templateCategoriesBgBox = new EventBox ();
			templateCategoriesBgBox.Accessible.SetShouldIgnore (true);
			templateCategoriesBgBox.Name = "templateCategoriesVBox";
			templateCategoriesBgBox.BorderWidth = 0;
			templateCategoriesBgBox.ModifyBg (StateType.Normal, categoriesBackgroundColor);
			templateCategoriesBgBox.WidthRequest = 220;
			var templateCategoriesScrolledWindow = new ScrolledWindow ();
			templateCategoriesScrolledWindow.Name = "templateCategoriesScrolledWindow";
			templateCategoriesScrolledWindow.HscrollbarPolicy = PolicyType.Never;

			// Template categories tree view.
			templateCategoriesTreeView = new TreeView ();
			templateCategoriesTreeView.Name = "templateCategoriesTreeView";
			templateCategoriesTreeView.Accessible.Name = "templateCategoriesTreeView";
			templateCategoriesTreeView.Accessible.SetTitle (GettextCatalog.GetString ("Project Categories"));
			templateCategoriesTreeView.Accessible.Description = GettextCatalog.GetString ("Select the project category to see all possible project templates");
			templateCategoriesTreeView.BorderWidth = 0;
			templateCategoriesTreeView.HeadersVisible = false;
			templateCategoriesTreeView.Model = templateCategoriesListStore;
			templateCategoriesTreeView.SearchColumn = -1; // disable the interactive search
			templateCategoriesTreeView.AppendColumn (CreateTemplateCategoriesTreeViewColumn ());
			templateCategoriesScrolledWindow.Add (templateCategoriesTreeView);
			templateCategoriesBgBox.Add (templateCategoriesScrolledWindow);
			templatesHBox.PackStart (templateCategoriesBgBox, false, false, 0);

			// Templates.
			var templatesBgBox = new EventBox ();
			templatesBgBox.Accessible.SetShouldIgnore (true);
			templatesBgBox.ModifyBg (StateType.Normal, templateListBackgroundColor);
			templatesBgBox.Name = "templatesVBox";
			templatesBgBox.WidthRequest = 400;
			templatesHBox.PackStart (templatesBgBox, false, false, 0);
			var templatesScrolledWindow = new ScrolledWindow ();
			templatesScrolledWindow.Name = "templatesScrolledWindow";
			templatesScrolledWindow.HscrollbarPolicy = PolicyType.Never;

			// Templates tree view.
			templatesTreeView = new TreeView ();
			templatesTreeView.Name = "templatesTreeView";
			templatesTreeView.Accessible.Name = "templatesTreeView";
			templatesTreeView.Accessible.SetTitle (GettextCatalog.GetString ("Project Templates"));
			templatesTreeView.Accessible.Description = GettextCatalog.GetString ("Select the project template");
			templatesTreeView.HeadersVisible = false;
			templatesTreeView.Model = templatesListStore;
			templatesTreeView.SearchColumn = -1; // disable the interactive search
			templatesTreeView.AppendColumn (CreateTemplateListTreeViewColumn ());
			templatesScrolledWindow.Add (templatesTreeView);
			templatesBgBox.Add (templatesScrolledWindow);

			// Accessibilityy
			templateCategoriesTreeView.Accessible.AddLinkedUIElement (templatesTreeView.Accessible);

			// Template
			var templateEventBox = new EventBox ();
			templateEventBox.Accessible.SetShouldIgnore (true);
			templateEventBox.Name = "templateEventBox";
			templateEventBox.ModifyBg (StateType.Normal, templateBackgroundColor);
			templatesHBox.PackStart (templateEventBox, true, true, 0);
			templateVBox = new VBox ();
			templateVBox.Accessible.SetShouldIgnore (true);
			templateVBox.Visible = false;
			templateVBox.BorderWidth = 20;
			templateVBox.Spacing = 10;
			templateEventBox.Add (templateVBox);

			// Template large image.
			templateImage = new ImageView ();
			templateImage.Accessible.SetShouldIgnore (true);
			templateImage.Name = "templateImage";
			templateImage.HeightRequest = 140;
			templateImage.WidthRequest = 240;
			templateVBox.PackStart (templateImage, false, false, 10);

			// Template description.
			templateNameLabel = new Label ();
			templateNameLabel.Name = "templateNameLabel";
			templateNameLabel.Accessible.Name = "templateNameLabel";
			templateNameLabel.Accessible.Description = GettextCatalog.GetString ("The name of the selected template");
			templateNameLabel.WidthRequest = 240;
			templateNameLabel.Wrap = true;
			templateNameLabel.Xalign = 0;
			templateNameLabel.Markup = MarkupTemplateName ("TemplateName");
			templateVBox.PackStart (templateNameLabel, false, false, 0);
			templateDescriptionLabel = new Label ();
			templateDescriptionLabel.Name = "templateDescriptionLabel";
			templateDescriptionLabel.Accessible.Name = "templateDescriptionLabel";
			templateDescriptionLabel.Accessible.SetLabel (GettextCatalog.GetString ("The description of the selected template"));
			templateDescriptionLabel.WidthRequest = 240;
			templateDescriptionLabel.Wrap = true;
			templateDescriptionLabel.Xalign = 0;
			templateVBox.PackStart (templateDescriptionLabel, false, false, 0);

			var tempLabel = new Label ();
			tempLabel.Accessible.SetShouldIgnore (true);
			templateVBox.PackStart (tempLabel, true, true, 0);

			templatesTreeView.Accessible.AddLinkedUIElement (templateNameLabel.Accessible);
			templatesTreeView.Accessible.AddLinkedUIElement (templateDescriptionLabel.Accessible);

			// Template - button separator.
			var templateSectionSeparatorEventBox = new EventBox ();
			templateSectionSeparatorEventBox.Accessible.SetShouldIgnore (true);
			templateSectionSeparatorEventBox.Name = "templateSectionSeparatorEventBox";
			templateSectionSeparatorEventBox.HeightRequest = 1;
			templateSectionSeparatorEventBox.ModifyBg (StateType.Normal, templateSectionSeparatorColor);
			VBox.PackStart (templateSectionSeparatorEventBox, false, false, 0);

			// Buttons at bottom of dialog.
			var bottomHBox = new HBox ();
			bottomHBox.Accessible.SetShouldIgnore (true);
			bottomHBox.Name = "bottomHBox";
			VBox.PackStart (bottomHBox, false, false, 0);

			// Cancel button - bottom left.
			var cancelButtonBox = new HButtonBox ();
			cancelButtonBox.Accessible.SetShouldIgnore (true);
			cancelButtonBox.Name = "cancelButtonBox";
			cancelButtonBox.BorderWidth = 16;
			cancelButton = new Button ();
			cancelButton.Name = "cancelButton";
			cancelButton.Accessible.Name = "cancelButton";
			cancelButton.Accessible.Description = GettextCatalog.GetString ("Cancel the dialog");
			cancelButton.Label = "gtk-cancel";
			cancelButton.UseStock = true;
			cancelButtonBox.PackStart (cancelButton, false, false, 0);
			bottomHBox.PackStart (cancelButtonBox, false, false, 0);

			// Previous button - bottom right.
			var previousNextButtonBox = new HButtonBox ();
			previousNextButtonBox.Accessible.SetShouldIgnore (true);
			previousNextButtonBox.Name = "previousNextButtonBox";
			previousNextButtonBox.BorderWidth = 16;
			previousNextButtonBox.Spacing = 9;
			bottomHBox.PackStart (previousNextButtonBox);
			previousNextButtonBox.Layout = ButtonBoxStyle.End;

			previousButton = new Button ();
			previousButton.Name = "previousButton";
			previousButton.Accessible.Name = "previousButton";
			previousButton.Accessible.Description = GettextCatalog.GetString ("Return to the previous page");
			previousButton.Label = GettextCatalog.GetString ("Previous");
			previousButton.Sensitive = false;
			previousNextButtonBox.PackEnd (previousButton);

			// Next button - bottom right.
			nextButton = new Button ();
			nextButton.Name = "nextButton";
			nextButton.Accessible.Name = "nextButton";
			nextButton.Accessible.Description = GettextCatalog.GetString ("Move to the next page");
			nextButton.Label = GettextCatalog.GetString ("Next");
			previousNextButtonBox.PackEnd (nextButton);

			// Remove default button action area.
			VBox.Remove (ActionArea);

			if (Child != null) {
				Child.ShowAll ();
			}

			Show ();

			templatesTreeView.HasFocus = true;
			Resizable = false;
		}

		TreeViewColumn CreateTemplateCategoriesTreeViewColumn ()
		{
			var column = new TreeViewColumn ();

			categoryTextRenderer = new GtkTemplateCategoryCellRenderer ();
			categoryTextRenderer.Xpad = 17;
			categoryTextRenderer.Ypad = 0;
			categoryTextRenderer.CellBackgroundGdk = categoriesBackgroundColor;

			column.PackStart (categoryTextRenderer, true);
			column.AddAttribute (categoryTextRenderer, "markup", column: 0);

			column.SetCellDataFunc (categoryTextRenderer, SetTemplateCategoryCellData);

			return column;
		}

		TreeViewColumn CreateTemplateListTreeViewColumn ()
		{
			var column = new TreeViewColumn ();

			templateTextRenderer = new GtkTemplateCellRenderer ();
			templateTextRenderer.Xpad = 14;
			templateTextRenderer.Ypad = 0;
			templateTextRenderer.Ellipsize = Pango.EllipsizeMode.End;
			templateTextRenderer.CellBackgroundGdk = templateListBackgroundColor;

			column.PackStart (templateTextRenderer, true);
			column.AddAttribute (templateTextRenderer, "markup", column: 0);

			column.SetCellDataFunc (templateTextRenderer, SetTemplateTextCellData);

			languageCellRenderer = new LanguageCellRenderer ();
			languageCellRenderer.CellBackgroundGdk = templateListBackgroundColor;

			column.PackEnd (languageCellRenderer, false);
			column.SetCellDataFunc (languageCellRenderer, SetLanguageCellData);
			return column;
		}

		/// <summary>
		/// When the dialog has Resizable set to false then the DefaultHeight and
		/// DefaultWidth are ignored and the size for the dialog changes to fit the
		/// widgets which will sometimes shrink the dialog. The size also changes
		/// on moving from page to page so override the requisition if it is too small.
		/// </summary>
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			if (requisition.Height < DefaultHeight)
				requisition.Height = DefaultHeight;

			if (requisition.Width < DefaultWidth)
				requisition.Width = DefaultWidth;
		}
	}
}

