//
// GtkNewProjectDialogBackend.cs
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

using System;
using System.Linq;
using Gtk;
using Mono.Unix;
using MonoDevelop.Components;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Ide.Projects
{
	public partial class GtkNewProjectDialogBackend : INewProjectDialogBackend
	{
		INewProjectDialogController controller;
		int currentPage;
		TemplateWizard wizard;

		public GtkNewProjectDialogBackend ()
		{
			this.Build ();
			templateTextRenderer.SelectedLanguage = "C#";

			templateCategoriesTreeView.Selection.Changed += TemplateCategoriesTreeViewSelectionChanged;
			templatesTreeView.Selection.Changed += TemplatesTreeViewSelectionChanged;
			templatesTreeView.ButtonPressEvent += TemplatesTreeViewButtonPressed;
			templatesTreeView.Selection.SelectFunction = TemplatesTreeViewSelection;
			cancelButton.Clicked += CancelButtonClicked;
			nextButton.Clicked += (sender, e) => MoveToNextPage ();
			previousButton.Clicked += (sender, e) => MoveToPreviousPage ();
		}

		public void ShowDialog ()
		{
			MessageService.ShowCustomDialog (this);
		}

		public void CloseDialog ()
		{
			Destroy ();
		}

		public bool CanMoveToNextPage {
			get { return nextButton.Sensitive; }
			set { nextButton.Sensitive = value; }
		}

		public void RegisterController (INewProjectDialogController controller)
		{
			this.controller = controller;
			LoadTemplates ();
			SelectTemplateDefinedbyController ();
		}

		void SetTemplateTextCellData (TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter it)
		{
			var template = (SolutionTemplate)model.GetValue (it, TemplateColumn);
			templateTextRenderer.Template = template;
		}

		[GLib.ConnectBefore]
		void TemplatesTreeViewButtonPressed (object o, ButtonPressEventArgs args)
		{
			SolutionTemplate template = GetSelectedTemplate ();
			if ((template == null) || (template.AvailableLanguages.Count <= 1)) {
				return;
			}

			if (templateTextRenderer.IsLanguageButtonPressed (args.Event)) {
				var menu = new Menu ();
				menu.AttachToWidget (this, null);
				AddLanguageMenuItems (menu, template);
				menu.ModifyBg (StateType.Normal, TemplateCellRendererText.LanguageButtonBackgroundColor);
				menu.ShowAll ();

				MenuPositionFunc posFunc = (Menu m, out int x, out int y, out bool pushIn) => {
					Gdk.Rectangle rect = templateTextRenderer.GetLanguageRect ();
					Gdk.Rectangle screenRect = GtkUtil.ToScreenCoordinates (templatesTreeView, templatesTreeView.ParentWindow, rect);
					x = screenRect.X;
					y = screenRect.Bottom;
					pushIn = false;
				};
				menu.Popup (null, null, posFunc, 0, args.Event.Time);
			}
		}

		void AddLanguageMenuItems (Menu menu, SolutionTemplate template)
		{
			foreach (string language in template.AvailableLanguages.OrderBy (item => item)) {
				var menuItem = new MenuItem (language);
				menuItem.Activated += (o, e) => {
					templateTextRenderer.SelectedLanguage = language;
					templatesTreeView.QueueDraw ();
					ShowSelectedTemplate ();
				};
				menu.Append (menuItem);
			}
		}

		bool TemplatesTreeViewSelection (TreeSelection selection, TreeModel model, TreePath path, bool path_currently_selected)
		{
			TreeIter iter;
			if (model.GetIter (out iter, path)) {
				var template = templatesListStore.GetValue (iter, TemplateColumn) as SolutionTemplate;
				if (template == null) {
					return false;
				}
			}

			return true;
		}

		void TemplateCategoriesTreeViewSelectionChanged (object sender, EventArgs e)
		{
			ShowTemplatesForSelectedCategory ();
		}

		void TemplatesTreeViewSelectionChanged (object sender, EventArgs e)
		{
			ShowSelectedTemplate ();
		}

		void CancelButtonClicked (object sender, EventArgs e)
		{
			Destroy ();
		}

		void LoadTemplates ()
		{
			foreach (TemplateCategory category in controller.TemplateCategories) {
				AddTopLevelTemplateCategory (category);
			}
		}

		void AddTopLevelTemplateCategory (TemplateCategory category)
		{
			templateCategoriesListStore.AppendValues (
				GetIcon (category.IconId, IconSize.Menu),
				MarkupTopLevelCategoryName (category.Name),
				category);

			foreach (TemplateCategory subCategory in category.Categories) {
				AddSubTemplateCategory (subCategory);
			}
		}

		void AddSubTemplateCategory (TemplateCategory category)
		{
			templateCategoriesListStore.AppendValues (
				null,
				category.Name,
				category);
		}

		static string MarkupTopLevelCategoryName (string name)
		{
			return "<span font_weight='bold' size='larger'>" + name + "</span>";
		}

		void ShowTemplatesForSelectedCategory ()
		{
			ClearSelectedCategoryInformation ();

			TemplateCategory category = GetSelectedTemplateCategory ();
			if ((category != null) && (category.IconId == null)) {
				ShowTemplatesForCategory (category);
				SelectFirstTemplate ();
			}
		}

		void ClearSelectedCategoryInformation ()
		{
			templatesListStore.Clear ();
		}

		TemplateCategory GetSelectedTemplateCategory ()
		{
			TreeIter item;
			if (templateCategoriesTreeView.Selection.GetSelected (out item)) {
				return templateCategoriesListStore.GetValue (item, TemplateCategoryColumn) as TemplateCategory;
			}
			return null;
		}

		void ShowTemplatesForCategory (TemplateCategory category)
		{
			foreach (TemplateCategory subCategory in category.Categories) {
				templatesListStore.AppendValues (
					null,
					MarkupTopLevelCategoryName (subCategory.Name),
					null);

				foreach (SolutionTemplate template in subCategory.Templates) {
					templatesListStore.AppendValues (
						GetIcon (template.IconId, IconSize.Dnd),
						template.Name,
						template);
				}
			}
		}

		static Gdk.Pixbuf GetIcon (string id, IconSize size)
		{
			Xwt.Drawing.Image image = ImageService.GetIcon (id, size);
			if (image != null) {
				return image.ToPixbuf ();
			}
			return null;
		}

		void ShowSelectedTemplate ()
		{
			ClearSelectedTemplateInformation ();

			SolutionTemplate template = GetSelectedTemplateForSelectedLanguage ();
			if (template != null) {
				ShowTemplate (template);
			}

			CanMoveToNextPage = (template != null);
		}

		void ClearSelectedTemplateInformation ()
		{
			templateVBox.Visible = false;
		}

		SolutionTemplate GetSelectedTemplateForSelectedLanguage ()
		{
			SolutionTemplate template = GetSelectedTemplate ();
			if (template != null) {
				SolutionTemplate languageTemplate = template.GetTemplate (templateTextRenderer.SelectedLanguage);
				if (languageTemplate != null) {
					return languageTemplate;
				}
			}

			return template;
		}

		SolutionTemplate GetSelectedTemplate ()
		{
			TreeIter item;
			if (templatesTreeView.Selection.GetSelected (out item)) {
				return templatesListStore.GetValue (item, TemplateColumn) as SolutionTemplate;
			}
			return null;
		}

		void ShowTemplate (SolutionTemplate template)
		{
			templateNameLabel.Markup = MarkupTopLevelCategoryName (template.Name);
			templateDescriptionLabel.Text = template.Description;
			templateImage.Pixbuf = FromResource (template.LargeImageId);
			templateVBox.Visible = true;
			templateVBox.ShowAll ();
		}

		Gdk.Pixbuf FromResource (string id)
		{
			var image = Xwt.Drawing.Image.FromResource (id);
			if (image != null) {
				return image.ToPixbuf ();
			}
			return null;
		}

		void SelectTemplateDefinedbyController ()
		{
			if (controller.SelectedSecondLevelCategory == null) {
				SelectFirstSubTemplateCategory ();
				return;
			}

			SelectTemplateCategory (controller.SelectedSecondLevelCategory);

			if (controller.SelectedTemplate != null) {
				SelectTemplate (controller.SelectedTemplate);
			}
		}

		void SelectFirstSubTemplateCategory ()
		{
			TreeIter iter = TreeIter.Zero;
			if (templateCategoriesListStore.IterNthChild (out iter, 1)) {
				templateCategoriesTreeView.Selection.SelectIter (iter);
			}
		}

		void SelectTemplateCategory (TemplateCategory category)
		{
			TreeIter iter = TreeIter.Zero;
			if (!templateCategoriesListStore.GetIterFirst (out iter)) {
				return;
			}

			while (templateCategoriesListStore.IterNext (ref iter)) {
				var currentCategory = templateCategoriesListStore.GetValue (iter, TemplateCategoryColumn) as TemplateCategory;
				if (currentCategory == category) {
					templateCategoriesTreeView.Selection.SelectIter (iter);
					break;
				}
			}
		}

		void SelectTemplate (SolutionTemplate template)
		{
			TreeIter iter = TreeIter.Zero;
			if (!templatesListStore.GetIterFirst (out iter)) {
				return;
			}

			while (templatesListStore.IterNext (ref iter)) {
				var currentTemplate = templatesListStore.GetValue (iter, TemplateColumn) as SolutionTemplate;
				if (currentTemplate == template) {
					templatesTreeView.Selection.SelectIter (iter);
					break;
				}
			}
		}

		void SelectFirstTemplate ()
		{
			TreeIter iter = TreeIter.Zero;
			if (templatesListStore.IterNthChild (out iter, 1)) {
				templatesTreeView.Selection.SelectIter (iter);
			}
		}

		void MoveToNextPage ()
		{
			SolutionTemplate template = GetSelectedTemplateForSelectedLanguage ();
			if (template == null)
				return;

			if (projectConfigurationWidget == centreVBox.Children [0]) {
				controller.SelectedTemplate = template;
				controller.Create ();
				return;
			}

			Widget widget = GetNextPageWidget (template);

			centreVBox.Remove (centreVBox.Children [0]);
			widget.Show ();
			centreVBox.PackStart (widget, true, true, 0);

			if (widget is WizardPage) {
				//topBannerLabel.Text = ((WizardPage)widget).Title;
			} else {
				topBannerLabel.Text = configureYourProjectBannerText;
			}

			previousButton.Sensitive = true;
			if (widget == projectConfigurationWidget) {
				nextButton.Label = Catalog.GetString ("Create");
				CanMoveToNextPage = false;
			}
		}

		void MoveToPreviousPage ()
		{
			Widget widget = GetPreviousPageWidget (centreVBox.Children [0]);
			widget.Show ();

			centreVBox.Remove (centreVBox.Children [0]);
			centreVBox.PackStart (widget, true, true, 0);

			if (widget is WizardPage) {
//				topBannerLabel.Text = ((WizardPage)widget).Title;
			} else {
				topBannerLabel.Text = chooseTemplateBannerText;
			}

			previousButton.Sensitive = (wizard != null);
			nextButton.Label = Catalog.GetString ("Next");
		}

		Widget GetNextPageWidget (SolutionTemplate template)
		{
			currentPage++;

			if (template.HasWizard) {
				wizard = controller.CreateTemplateWizard (template.Wizard);
				if (wizard != null) {
					WizardPage page = wizard.GetPage (currentPage);
					if (page != null) {
					//	return page;
					}
				}
			}

			projectConfigurationWidget.Load (controller.FinalConfiguration);
			return projectConfigurationWidget;
		}

		Widget GetPreviousPageWidget (Widget existingWidget)
		{
			currentPage--;

//			if (existingWidget == projectConfigurationWidget) {
//				if (wizard != null) {
//					WizardPage page = wizard.GetPage (currentPage);
//					if (page != null) {
//						return page;
//					}
//				}
//			}

			wizard = null;
			return templatesHBox;
		}
	}
}

