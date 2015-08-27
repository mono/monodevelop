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
using System.ComponentModel;
using System.Linq;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Ide.Projects
{
	partial class GtkNewProjectDialogBackend : INewProjectDialogBackend
	{
		INewProjectDialogController controller;
		Menu popupMenu;

		public GtkNewProjectDialogBackend ()
		{
			this.Build ();

			// Set up the list store so the test framework can work out the correct columns
			SemanticModelAttribute modelAttr = new SemanticModelAttribute ("templateCategoriesListStore__Name", "templateCategoriesListStore__Icon", "templateCategoriesListStore__Category");
			TypeDescriptor.AddAttributes (templateCategoriesListStore, modelAttr);
			modelAttr = new SemanticModelAttribute ("templateListStore__Name", "templateListStore__Icon", "templateListStore__Template");
			TypeDescriptor.AddAttributes (templatesListStore, modelAttr);

			templateCategoriesTreeView.Selection.Changed += TemplateCategoriesTreeViewSelectionChanged;
			templateCategoriesTreeView.Selection.SelectFunction = TemplateCategoriesTreeViewSelection;
			templatesTreeView.Selection.Changed += TemplatesTreeViewSelectionChanged;
			templatesTreeView.ButtonPressEvent += TemplatesTreeViewButtonPressed;
			templatesTreeView.Selection.SelectFunction = TemplatesTreeViewSelection;
			templatesTreeView.RowActivated += TreeViewRowActivated;
			cancelButton.Clicked += CancelButtonClicked;
			nextButton.Clicked += (sender, e) => MoveToNextPage ();
			previousButton.Clicked += (sender, e) => MoveToPreviousPage ();

			nextButton.CanDefault = true;
			nextButton.GrabDefault ();
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
			templateTextRenderer.SelectedLanguage = controller.SelectedLanguage;
			topBannerLabel.Text = controller.BannerText;

			LoadTemplates ();
			SelectTemplateDefinedbyController ();
		}

		void SetTemplateCategoryCellData (TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter it)
		{
			categoryTextRenderer.Category = (TemplateCategory)model.GetValue (it, TemplateCategoryColumn);
			categoryTextRenderer.CategoryIcon = model.GetValue (it, TemplateCategoryIconColumn) as Xwt.Drawing.Image;
			categoryTextRenderer.CategoryName = model.GetValue (it, TemplateCategoryNameColumn) as string;
		}

		void SetTemplateTextCellData (TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter it)
		{
			var template = (SolutionTemplate)model.GetValue (it, TemplateColumn);
			templateTextRenderer.Template = template;
			templateTextRenderer.TemplateIcon = model.GetValue (it, TemplateIconColumn) as Xwt.Drawing.Image;
			templateTextRenderer.TemplateCategory = model.GetValue (it, TemplateNameColumn) as string;
		}

		[GLib.ConnectBefore]
		void TemplatesTreeViewButtonPressed (object o, ButtonPressEventArgs args)
		{
			SolutionTemplate template = GetSelectedTemplate ();
			if ((template == null) || (template.AvailableLanguages.Count <= 1)) {
				return;
			}

			if (templateTextRenderer.IsLanguageButtonPressed (args.Event)) {
				if (popupMenu == null) {
					popupMenu = new Menu ();
					popupMenu.AttachToWidget (this, null);
				}
				ClearPopupMenuItems ();
				AddLanguageMenuItems (popupMenu, template);
				popupMenu.ModifyBg (StateType.Normal, GtkTemplateCellRenderer.LanguageButtonBackgroundColor);
				popupMenu.ShowAll ();

				MenuPositionFunc posFunc = (Menu m, out int x, out int y, out bool pushIn) => {
					Gdk.Rectangle rect = templateTextRenderer.GetLanguageRect ();
					Gdk.Rectangle screenRect = GtkUtil.ToScreenCoordinates (templatesTreeView, templatesTreeView.GdkWindow, rect);
					x = screenRect.X;
					y = screenRect.Bottom;
					pushIn = false;
				};
				popupMenu.Popup (null, null, posFunc, 0, args.Event.Time);
			}
		}

		void ClearPopupMenuItems ()
		{
			foreach (Widget widget in popupMenu.Children) {
				widget.Destroy ();
			}
		}

		void AddLanguageMenuItems (Menu menu, SolutionTemplate template)
		{
			foreach (string language in template.AvailableLanguages.OrderBy (item => item)) {
				var menuItem = new MenuItem (language);
				menuItem.Activated += (o, e) => {
					templateTextRenderer.SelectedLanguage = language;
					controller.SelectedLanguage = language;
					templatesTreeView.QueueDraw ();
					ShowSelectedTemplate ();
				};
				menu.Append (menuItem);
			}
		}

		bool TemplateCategoriesTreeViewSelection (TreeSelection selection, TreeModel model, TreePath path, bool path_currently_selected)
		{
			TreeIter iter;
			if (model.GetIter (out iter, path)) {
				var category = model.GetValue (iter, TemplateCategoryColumn) as TemplateCategory;
				if (category.IsTopLevel) {
					return false;
				}
			}

			return true;
		}

		bool TemplatesTreeViewSelection (TreeSelection selection, TreeModel model, TreePath path, bool path_currently_selected)
		{
			TreeIter iter;
			if (model.GetIter (out iter, path)) {
				var template = model.GetValue (iter, TemplateColumn) as SolutionTemplate;
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
			controller.SelectedTemplate = GetSelectedTemplate ();
			ShowSelectedTemplate ();
		}

		void CancelButtonClicked (object sender, EventArgs e)
		{
			Destroy ();
		}

		public override void Destroy ()
		{
			if (popupMenu != null) {
				popupMenu.Destroy ();
				popupMenu = null;
			}
			base.Destroy ();
		}

		void LoadTemplates ()
		{
			foreach (TemplateCategory category in controller.TemplateCategories) {
				AddTopLevelTemplateCategory (category);
			}
		}

		void AddTopLevelTemplateCategory (TemplateCategory category)
		{
			Xwt.Drawing.Image icon = GetIcon (category.IconId, IconSize.Menu);
			categoryTextRenderer.CategoryIconWidth = (int)icon.Width;

			templateCategoriesListStore.AppendValues (
				MarkupTopLevelCategoryName (category.Name),
				icon,
				category);

			foreach (TemplateCategory subCategory in category.Categories) {
				AddSubTemplateCategory (subCategory);
			}
		}

		void AddSubTemplateCategory (TemplateCategory category)
		{
			templateCategoriesListStore.AppendValues (
				GLib.Markup.EscapeText (category.Name),
				null,
				category);
		}

		static string MarkupTopLevelCategoryName (string name)
		{
			return "<span font_weight='bold'>" + GLib.Markup.EscapeText (name) + "</span>";
		}

		static string MarkupTemplateName (string name)
		{
			return "<span font_weight='bold' size='larger'>" + GLib.Markup.EscapeText (name) + "</span>";
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
					MarkupTopLevelCategoryName (subCategory.Name),
					null,
					null);

				foreach (SolutionTemplate template in subCategory.Templates) {
					if (template.HasProjects || controller.IsNewSolution) {
						templatesListStore.AppendValues (
							template.Name,
							GetIcon (template.IconId, IconSize.Dnd),
							template);
					}
				}
			}
		}

		static Xwt.Drawing.Image GetIcon (string id, IconSize size)
		{
			return ImageService.GetIcon (id, size);
		}

		void ShowSelectedTemplate ()
		{
			ClearSelectedTemplateInformation ();

			SolutionTemplate template = controller.GetSelectedTemplateForSelectedLanguage ();
			if (template != null) {
				ShowTemplate (template);
			}

			CanMoveToNextPage = controller.CanMoveToNextPage;
		}

		void ClearSelectedTemplateInformation ()
		{
			templateVBox.Visible = false;
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
			templateNameLabel.Markup = MarkupTemplateName (template.Name);
			templateDescriptionLabel.Text = template.Description;
			templateImage.Image = controller.GetImage (template);
			templateVBox.Visible = true;
			templateVBox.ShowAll ();
		}

		void SelectTemplateDefinedbyController ()
		{
			SolutionTemplate selectedTemplate = controller.SelectedTemplate;

			if (controller.SelectedSecondLevelCategory == null) {
				SelectFirstSubTemplateCategory ();
				return;
			}

			SelectTemplateCategory (controller.SelectedSecondLevelCategory);

			if (selectedTemplate != null) {
				SelectTemplate (selectedTemplate);
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
					TreePath path = templatesListStore.GetPath (iter);
					templatesTreeView.ScrollToCell (path, null, true, 1, 0);
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
			if (controller.IsLastPage) {
				try {
					CanMoveToNextPage = false;
					controller.Create ();
				} finally {
					CanMoveToNextPage = true;
				}
				return;
			}

			controller.MoveToNextPage ();

			SolutionTemplate template = controller.GetSelectedTemplateForSelectedLanguage ();
			if (template == null)
				return;

			Widget widget = GetWidgetToDisplay ();

			centreVBox.Remove (centreVBox.Children [0]);
			widget.ShowAll ();
			centreVBox.PackStart (widget, true, true, 0);
			FocusWidget (widget);

			topBannerLabel.Text = controller.BannerText;

			previousButton.Sensitive = controller.CanMoveToPreviousPage;
			nextButton.Label = controller.NextButtonText;
			CanMoveToNextPage = controller.CanMoveToNextPage;
		}

		void MoveToPreviousPage ()
		{
			controller.MoveToPreviousPage ();

			Widget widget = GetWidgetToDisplay ();
			widget.ShowAll ();

			centreVBox.Remove (centreVBox.Children [0]);
			centreVBox.PackStart (widget, true, true, 0);
			FocusWidget (widget);

			topBannerLabel.Text = controller.BannerText;

			previousButton.Sensitive = controller.CanMoveToPreviousPage;
			nextButton.Label = controller.NextButtonText;
			CanMoveToNextPage = controller.CanMoveToNextPage;
		}

		Widget GetWidgetToDisplay ()
		{
			if (controller.IsFirstPage) {
				return templatesHBox;
			} else if (controller.IsLastPage) {
				controller.FinalConfiguration.UpdateFromParameters ();
				projectConfigurationWidget.Load (controller.FinalConfiguration, controller.GetFinalPageControls ());
				return projectConfigurationWidget;
			} else {
				return controller.CurrentWizardPage;
			}
		}

		void FocusWidget (Widget widget)
		{
			var widgetToFocus = widget; 
			var commandRouter = widget as CommandRouterContainer;
			if ((commandRouter != null) && commandRouter.Children.Any ()) {
				widgetToFocus = commandRouter.Children [0];
			}

			widgetToFocus.GrabFocus ();
		}

		void TreeViewRowActivated (object o, RowActivatedArgs args)
		{
			if (CanMoveToNextPage && IsSolutionTemplateOnActivatedRow ((Gtk.TreeView)o, args)) {
				MoveToNextPage ();
			}
		}

		bool IsSolutionTemplateOnActivatedRow (TreeView treeView, RowActivatedArgs args)
		{
			TreeModel model = treeView.Model;
			TreeIter iter;
			if (model.GetIter (out iter, args.Path)) {
				var template = model.GetValue (iter, TemplateColumn) as SolutionTemplate;
				return template != null;
			}
			return false;
		}
	}
}

