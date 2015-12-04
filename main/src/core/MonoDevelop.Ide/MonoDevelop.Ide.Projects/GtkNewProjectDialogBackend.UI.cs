﻿//
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;
using Mono.TextEditor;

namespace MonoDevelop.Ide.Projects
{
	partial class GtkNewProjectDialogBackend : Gtk.Dialog
	{
		Color bannerBackgroundColor = new Color (119, 130, 140);
		Color bannerLineColor = new Color (112, 122, 131);
		Color whiteColor = new Color (255, 255, 255);
		Color categoriesBackgroundColor = new Color (225, 228, 232);
		Color templateListBackgroundColor = new Color (240, 240, 240);
		Color templateBackgroundColor = new Color (255, 255, 255);
		Color templateSectionSeparatorColor = new Color (208, 208, 208);

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

		void Build ()
		{
			BorderWidth = 0;
			WidthRequest = GtkWorkarounds.ConvertToPixelScale (901);
			HeightRequest = GtkWorkarounds.ConvertToPixelScale (632);

			Name = "wizard_dialog";
			Title = GettextCatalog.GetString ("New Project");
			WindowPosition = WindowPosition.CenterOnParent;
			TransientFor = IdeApp.Workbench.RootWindow;

			projectConfigurationWidget = new GtkProjectConfigurationWidget ();
			projectConfigurationWidget.Name = "projectConfigurationWidget";

			// Top banner of dialog.
			var topLabelEventBox = new EventBox ();
			topLabelEventBox.Name = "topLabelEventBox";
			topLabelEventBox.HeightRequest = GtkWorkarounds.ConvertToPixelScale (52);
			topLabelEventBox.ModifyBg (StateType.Normal, bannerBackgroundColor);
			topLabelEventBox.ModifyFg (StateType.Normal, whiteColor);
			topLabelEventBox.BorderWidth = 0;

			var topBannerTopEdgeLineEventBox = new EventBox ();
			topBannerTopEdgeLineEventBox.Name = "topBannerTopEdgeLineEventBox";
			topBannerTopEdgeLineEventBox.HeightRequest = 1;
			topBannerTopEdgeLineEventBox.ModifyBg (StateType.Normal, bannerLineColor);
			topBannerTopEdgeLineEventBox.BorderWidth = 0;

			var topBannerBottomEdgeLineEventBox = new EventBox ();
			topBannerBottomEdgeLineEventBox.Name = "topBannerBottomEdgeLineEventBox";
			topBannerBottomEdgeLineEventBox.HeightRequest = 1;
			topBannerBottomEdgeLineEventBox.ModifyBg (StateType.Normal, bannerLineColor);
			topBannerBottomEdgeLineEventBox.BorderWidth = 0;

			topBannerLabel = new Label ();
			topBannerLabel.Name = "topBannerLabel";
			Pango.FontDescription font = topBannerLabel.Style.FontDescription.Copy ();
			font.Size = (int)(font.Size * 1.8);
			topBannerLabel.ModifyFont (font);
			topBannerLabel.ModifyFg (StateType.Normal, whiteColor);
			var topLabelHBox = new HBox ();
			topLabelHBox.Name = "topLabelHBox";
			topLabelHBox.PackStart (topBannerLabel, false, false, 20);
			topLabelEventBox.Add (topLabelHBox);

			VBox.PackStart (topBannerTopEdgeLineEventBox, false, false, 0);
			VBox.PackStart (topLabelEventBox, false, false, 0);
			VBox.PackStart (topBannerBottomEdgeLineEventBox, false, false, 0);

			// Main templates section.
			centreVBox = new VBox ();
			centreVBox.Name = "centreVBox";
			VBox.PackStart (centreVBox, true, true, 0);
			templatesHBox = new HBox ();
			templatesHBox.Name = "templatesHBox";
			centreVBox.PackEnd (templatesHBox, true, true, 0);

			// Template categories.
			var templateCategoriesVBox = new VBox ();
			templateCategoriesVBox.Name = "templateCategoriesVBox";
			templateCategoriesVBox.BorderWidth = 0;
			templateCategoriesVBox.WidthRequest = GtkWorkarounds.ConvertToPixelScale (220);
			var templateCategoriesScrolledWindow = new ScrolledWindow ();
			templateCategoriesScrolledWindow.Name = "templateCategoriesScrolledWindow";
			templateCategoriesScrolledWindow.HscrollbarPolicy = PolicyType.Never;

			// Template categories tree view.
			templateCategoriesTreeView = new TreeView ();
			templateCategoriesTreeView.Name = "templateCategoriesTreeView";
			templateCategoriesTreeView.BorderWidth = 0;
			templateCategoriesTreeView.HeadersVisible = false;
			templateCategoriesTreeView.Model = templateCategoriesListStore;
			templateCategoriesTreeView.ModifyBase (StateType.Normal, categoriesBackgroundColor);
			templateCategoriesTreeView.AppendColumn (CreateTemplateCategoriesTreeViewColumn ());
			templateCategoriesScrolledWindow.Add (templateCategoriesTreeView);
			templateCategoriesVBox.PackStart (templateCategoriesScrolledWindow, true, true, 0);
			templatesHBox.PackStart (templateCategoriesVBox, false, false, 0);

			// Templates.
			var templatesVBox = new VBox ();
			templatesVBox.Name = "templatesVBox";
			templatesVBox.WidthRequest = GtkWorkarounds.ConvertToPixelScale (400);
			templatesHBox.PackStart (templatesVBox, false, false, 0);
			var templatesScrolledWindow = new ScrolledWindow ();
			templatesScrolledWindow.Name = "templatesScrolledWindow";
			templatesScrolledWindow.HscrollbarPolicy = PolicyType.Never;

			// Templates tree view.
			templatesTreeView = new TreeView ();
			templatesTreeView.Name = "templatesTreeView";
			templatesTreeView.HeadersVisible = false;
			templatesTreeView.Model = templatesListStore;
			templatesTreeView.ModifyBase (StateType.Normal, templateListBackgroundColor);
			templatesTreeView.AppendColumn (CreateTemplateListTreeViewColumn ());
			templatesScrolledWindow.Add (templatesTreeView);
			templatesVBox.PackStart (templatesScrolledWindow, true, true, 0);

			// Template
			var templateEventBox = new EventBox ();
			templateEventBox.Name = "templateEventBox";
			templateEventBox.ModifyBg (StateType.Normal, templateBackgroundColor);
			templatesHBox.PackStart (templateEventBox, true, true, 0);
			templateVBox = new VBox ();
			templateVBox.Visible = false;
			templateVBox.BorderWidth = 20;
			templateVBox.Spacing = 10;
			templateEventBox.Add (templateVBox);

			// Template large image.
			templateImage = new ImageView ();
			templateImage.Name = "templateImage";
			templateImage.HeightRequest = GtkWorkarounds.ConvertToPixelScale (140);
			templateImage.WidthRequest = GtkWorkarounds.ConvertToPixelScale (240);
			templateVBox.PackStart (templateImage, false, false, 10);

			// Template description.
			templateNameLabel = new Label ();
			templateNameLabel.Name = "templateNameLabel";
			templateNameLabel.WidthRequest = GtkWorkarounds.ConvertToPixelScale (240);
			templateNameLabel.Wrap = true;
			templateNameLabel.Xalign = 0;
			templateNameLabel.Markup = MarkupTemplateName ("TemplateName");
			templateVBox.PackStart (templateNameLabel, false, false, 0);
			templateDescriptionLabel = new Label ();
			templateDescriptionLabel.Name = "templateDescriptionLabel";
			templateDescriptionLabel.WidthRequest = GtkWorkarounds.ConvertToPixelScale (240);
			templateDescriptionLabel.Wrap = true;
			templateDescriptionLabel.Xalign = 0;
			templateVBox.PackStart (templateDescriptionLabel, false, false, 0);
			templateVBox.PackStart (new Label (), true, true, 0);

			// Template - button separator.
			var templateSectionSeparatorEventBox = new EventBox ();
			templateSectionSeparatorEventBox.Name = "templateSectionSeparatorEventBox";
			templateSectionSeparatorEventBox.HeightRequest = 1;
			templateSectionSeparatorEventBox.ModifyBg (StateType.Normal, templateSectionSeparatorColor);
			VBox.PackStart (templateSectionSeparatorEventBox, false, false, 0);

			// Buttons at bottom of dialog.
			var bottomHBox = new HBox ();
			bottomHBox.Name = "bottomHBox";
			VBox.PackStart (bottomHBox, false, false, 0);

			// Cancel button - bottom left.
			var cancelButtonBox = new HButtonBox ();
			cancelButtonBox.Name = "cancelButtonBox";
			cancelButtonBox.BorderWidth = 16;
			cancelButton = new Button ();
			cancelButton.Name = "cancelButton";
			cancelButton.Label = "gtk-cancel";
			cancelButton.UseStock = true;
			cancelButtonBox.PackStart (cancelButton, false, false, 0);
			bottomHBox.PackStart (cancelButtonBox, false, false, 0);

			// Previous button - bottom right.
			var previousNextButtonBox = new HButtonBox ();
			previousNextButtonBox.Name = "previousNextButtonBox";
			previousNextButtonBox.BorderWidth = 16;
			previousNextButtonBox.Spacing = 9;
			bottomHBox.PackStart (previousNextButtonBox);
			previousNextButtonBox.Layout = ButtonBoxStyle.End;

			previousButton = new Button ();
			previousButton.Name = "previousButton";
			previousButton.Label = GettextCatalog.GetString ("Previous");
			previousButton.Sensitive = false;
			previousNextButtonBox.PackEnd (previousButton);

			// Next button - bottom right.
			nextButton = new Button ();
			nextButton.Name = "nextButton";
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

			return column;
		}
	}
}

