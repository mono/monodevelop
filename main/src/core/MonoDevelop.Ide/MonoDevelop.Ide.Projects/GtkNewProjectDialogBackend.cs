﻿//
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
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Ide.Projects;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Projects
{
	partial class GtkNewProjectDialogBackend : INewProjectDialogBackend
	{
		INewProjectDialogController controller;
		Xwt.Menu popupMenu;
		bool isLastPressedKeySpace;

		public GtkNewProjectDialogBackend ()
		{
			this.Build ();

			// Set up the list store so the test framework can work out the correct columns
			SemanticModelAttribute modelAttr = new SemanticModelAttribute ("templateCategoriesListStore__Name", "templateCategoriesListStore__Icon", "templateCategoriesListStore__Category");
			TypeDescriptor.AddAttributes (templateCategoriesTreeStore, modelAttr);
			modelAttr = new SemanticModelAttribute ("templateListStore__Name", "templateListStore__Icon", "templateListStore__Template");
			TypeDescriptor.AddAttributes (templatesTreeStore, modelAttr);

			templateCategoriesTreeView.Selection.Changed += TemplateCategoriesTreeViewSelectionChanged;
			templateCategoriesTreeView.Selection.SelectFunction = TemplateCategoriesTreeViewSelection;

			templatesTreeView.Selection.Changed += TemplatesTreeViewSelectionChanged;
			templatesTreeView.ButtonPressEvent += TemplatesTreeViewButtonPressed;
			templatesTreeView.Selection.SelectFunction = TemplatesTreeViewSelection;
			templatesTreeView.RowActivated += TreeViewRowActivated;
			templatesTreeView.KeyPressEvent += TemplatesTreeViewKeyPressed;

			cancelButton.Clicked += CancelButtonClicked;
			nextButton.Clicked += NextButtonClicked;
			previousButton.Clicked += (sender, e) => MoveToPreviousPage ();

			nextButton.CanDefault = true;
			nextButton.GrabDefault ();

			// Setup the treeview to be able to have a context menu
			var actionHandler = new ActionDelegate (templatesTreeView);
			actionHandler.PerformShowMenu += PerformShowMenu;
		}

		void ProjectCreationFailed (object obj, EventArgs args) => ShowProjectCreationAccessibityNotification (true);
		void ProjectCreated(object obj, EventArgs args) => ShowProjectCreationAccessibityNotification (false);
		async void NextButtonClicked (object sender, EventArgs e) => await MoveToNextPage ();

		void ShowProjectCreationAccessibityNotification (bool hasError)
		{
			var projectTemplate = controller.SelectedTemplate;

			string messageText;

			if (hasError)
				messageText = GettextCatalog.GetString ("{0} failed to create", projectTemplate.Name);
			else
				messageText = GettextCatalog.GetString ("{0} successfully created", projectTemplate.Name);

			this.Accessible.MakeAccessibilityAnnouncement (messageText);
		}

		public void ShowDialog ()
		{
			MessageService.ShowCustomDialog (this, DesktopService.GetFocusedTopLevelWindow ());
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
			controller.ProjectCreationFailed += ProjectCreationFailed;
			controller.ProjectCreated += ProjectCreated;
			languageCellRenderer.SelectedLanguage = controller.SelectedLanguage;
			topBannerLabel.Text = controller.BannerText;
			LoadTemplates ();
			SelectTemplateDefinedbyController ();
			if (CanMoveToNextPage && !controller.ShowTemplateSelection)
				MoveToNextPage ().Ignore();
		}

		void SetTemplateCategoryCellData (TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter it)
		{
			var categoryTextRenderer = (GtkTemplateCategoryCellRenderer)renderer;
			categoryTextRenderer.Category = (TemplateCategory)model.GetValue (it, TemplateCategoryColumn);
			categoryTextRenderer.CategoryIcon = model.GetValue (it, TemplateCategoryIconColumn) as Xwt.Drawing.Image;
			categoryTextRenderer.CategoryName = model.GetValue (it, TemplateCategoryNameColumn) as string;
		}

		static void SetTemplateTextCellData (TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter it)
		{
			var template = (SolutionTemplate)model.GetValue (it, TemplateColumn);
			var templateTextRenderer = (GtkTemplateCellRenderer)renderer;
			templateTextRenderer.Template = template;
			templateTextRenderer.TemplateIcon = model.GetValue (it, TemplateIconColumn) as Xwt.Drawing.Image;
			templateTextRenderer.TemplateCategory = model.GetValue (it, TemplateNameColumn) as string;
		}

		static void SetLanguageCellData (TreeViewColumn col, CellRenderer renderer, TreeModel model, TreeIter it)
		{
			var template = (SolutionTemplate)model.GetValue (it, TemplateColumn);
			var languageRenderer = (LanguageCellRenderer)renderer;
			languageRenderer.Template = template;
		}

		void HandlePopup (SolutionTemplate template, uint eventTime)
		{
			var engine = Platform.IsMac ? Xwt.Toolkit.NativeEngine : Xwt.Toolkit.CurrentEngine;
			var xwtParent = Xwt.Toolkit.CurrentEngine.WrapWidget (templatesTreeView);
			engine.Invoke (() => {
				if (popupMenu == null) {
					popupMenu = new Xwt.Menu ();
				}
				ClearPopupMenuItems ();
				AddLanguageMenuItems (popupMenu, template);
				Gdk.Rectangle rect = languageCellRenderer.GetLanguageRect ();

				try {
					popupMenu.Popup (xwtParent, rect.X, rect.Bottom);
				} catch {
					// popup at mouse position if the toolkit is not supported
					popupMenu.Popup ();
				}

			});
		}

		[GLib.ConnectBefore]
		void TemplatesTreeViewButtonPressed (object o, ButtonPressEventArgs args)
		{

			SolutionTemplate template = GetSelectedTemplate ();
			if ((template == null) || (template.AvailableLanguages.Count <= 1)) {
				return;
			}

			if (languageCellRenderer.IsLanguageButtonPressed (args.Event)) {
				HandlePopup (template, args.Event.Time);
			}
		}

		[GLib.ConnectBefore]
		private void TemplatesTreeViewKeyPressed (object o, KeyPressEventArgs args)
		{
			isLastPressedKeySpace = args.Event.Key == Gdk.Key.space;

			if (isLastPressedKeySpace) {
				isLastPressedKeySpace = true;
				var template = GetSelectedTemplate ();

				if (template == null)
					return;
				if (template.AvailableLanguages.Count > 1)
					HandlePopup (template, 0);
				else
					System.Media.SystemSounds.Beep.Play ();
			}
		}

		void ClearPopupMenuItems ()
		{
			popupMenu.Items.Clear ();
		}

		void PerformShowMenu (object sender, EventArgs args)
		{
			SolutionTemplate template = GetSelectedTemplate ();
			if ((template == null) || (template.AvailableLanguages.Count <= 1)) {
				return;
			}

			HandlePopup (template, Gdk.EventHelper.GetTime (null));
		}

		void AddLanguageMenuItems (Xwt.Menu menu, SolutionTemplate template)
		{
			foreach (string language in template.AvailableLanguages.OrderBy (item => item)) {
				var menuItem = new Xwt.MenuItem (language);
				menuItem.Accessible.Label = LanguageCellRenderer.GetAccessibleLanguageName (language);
				menuItem.Clicked += (o, e) => {
					languageCellRenderer.SelectedLanguage = language;
					controller.SelectedLanguage = language;
					templatesTreeView.QueueDraw ();
					ShowSelectedTemplate ();
				};
				menu.Items.Add (menuItem);
			}
		}

		bool TemplateCategoriesTreeViewSelection (TreeSelection selection, TreeModel model, TreePath path, bool path_currently_selected)
		{
			TreeIter iter;
			if (model.GetIter (out iter, path)) {
				var category = model.GetValue (iter, TemplateCategoryColumn) as TemplateCategory;
				if (category?.IsTopLevel == true) { // don't allow selection for top level categories
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
			if (templateTextRenderer.RenderRecentTemplate && controller.SelectedTemplate != null) {
				// reset selected language if a recent template has been selected
				templateTextRenderer.SelectedLanguage = controller.SelectedTemplate.Language;
				languageCellRenderer.SelectedLanguage = controller.SelectedTemplate.Language;
				controller.SelectedLanguage = controller.SelectedTemplate.Language;
			}
			ShowSelectedTemplate ();
		}

		void CancelButtonClicked (object sender, EventArgs e)
		{
			Destroy ();
		}

		protected override void OnDestroyed ()
		{
			if (popupMenu != null) {
				popupMenu.Dispose ();
				popupMenu = null;
			}

			if (!controller.IsFirstPage)
				templatesHBox.Destroy ();

			if (!controller.IsLastPage)
				projectConfigurationWidget.Destroy ();

			controller.ProjectCreationFailed -= ProjectCreationFailed;
			controller.ProjectCreated -= ProjectCreated;

			base.OnDestroyed ();
		}

		void LoadTemplates ()
		{
			if (controller.RecentTemplates.Count > 0)
				AddRecentTemplateCategory ();
			foreach (TemplateCategory category in controller.TemplateCategories) {
				AddTopLevelTemplateCategory (category);
			}
		}

		void AddRecentTemplateCategory ()
		{
			Xwt.Drawing.Image icon = GetIcon ("md-recent", IconSize.Menu);
			categoryTextRenderer.CategoryIconWidth = (int)icon.Width;

			templateCategoriesTreeStore.AppendValues (
				Core.GettextCatalog.GetString ("Recently used"),
				icon,
				null);
		}

		void AddTopLevelTemplateCategory (TemplateCategory category)
		{
			Xwt.Drawing.Image icon = GetIcon (category.IconId ?? "md-platform-other", IconSize.Menu);
			categoryTextRenderer.CategoryIconWidth = (int)icon.Width;

			var iter = templateCategoriesTreeStore.AppendValues (
				MarkupTopLevelCategoryName (category.Name),
				icon,
				category);

			foreach (TemplateCategory subCategory in category.Categories) {
				AddSubTemplateCategory (iter, subCategory);
			}
			templateCategoriesTreeView.ExpandAll ();
		}

		void AddSubTemplateCategory (TreeIter iter, TemplateCategory category)
		{
			templateCategoriesTreeStore.AppendValues (
				iter,
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
			} else if (category == null) {
				ShowRecentTemplates ();
				SelectFirstTemplate ();
			}
		}

		void ClearSelectedCategoryInformation ()
		{
			templatesTreeStore.Clear ();
		}

		TemplateCategory GetSelectedTemplateCategory ()
		{
			TreeIter item;
			if (templateCategoriesTreeView.Selection.GetSelected (out item)) {
				return templateCategoriesTreeStore.GetValue (item, TemplateCategoryColumn) as TemplateCategory;
			}
			return null;
		}

		void ShowTemplatesForCategory (TemplateCategory category)
		{
			templateTextRenderer.RenderRecentTemplate = false;
			languageCellRenderer.RenderRecentTemplate = false;
			foreach (TemplateCategory subCategory in category.Categories) {
				var iter = templatesTreeStore.AppendValues (
					MarkupTopLevelCategoryName (subCategory.Name),
					null,
					null);

				foreach (SolutionTemplate template in subCategory.Templates) {
					if (template.HasProjects || controller.IsNewSolution) {
						templatesTreeStore.AppendValues (
							iter,
							template.Name,
							GetIcon (template.IconId, IconSize.Dnd),
							template);
					}
				}
			}
			templatesTreeView.ExpandAll ();
		}

		void ShowRecentTemplates ()
		{
			templateTextRenderer.RenderRecentTemplate = true;
			languageCellRenderer.RenderRecentTemplate = true;
			var iter = templatesTreeStore.AppendValues (
				MarkupTopLevelCategoryName (Core.GettextCatalog.GetString ("Recently used templates")),
				null,
				null);
			foreach (SolutionTemplate template in controller.RecentTemplates) {
				if (template.HasProjects || controller.IsNewSolution) {
					templatesTreeStore.AppendValues (
						iter,
						controller.GetCategoryPathText (template),
						GetIcon (template.IconId, IconSize.Dnd),
						template);
				}
			}
			templatesTreeView.ExpandAll ();
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
				return templatesTreeStore.GetValue (item, TemplateColumn) as SolutionTemplate;
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

			if (selectedTemplate != null && controller.RecentTemplates.Count > 0) {
				if (controller.RecentTemplates.Contains (selectedTemplate)) {
					SelectRecentTemplatesCategory ();
					SelectTemplate (selectedTemplate);
					return;
				}
			}

			if (controller.SelectedSecondLevelCategory == null) {
				SelectFirstSubTemplateCategory ();
				return;
			}

			SelectTemplateCategory (controller.SelectedSecondLevelCategory);

			if (selectedTemplate != null) {
				SelectTemplate (selectedTemplate);
			}
		}

		void SelectRecentTemplatesCategory ()
		{
			TreeIter iter = TreeIter.Zero;
			// recent templates entry is always the first one and has no category assigned to it
			if (templateCategoriesTreeStore.GetIterFirst (out iter) && templateCategoriesTreeStore.GetValue (iter, TemplateCategoryColumn) == null) {
				templateCategoriesTreeView.Selection.SelectIter (iter);
				TreePath path = templateCategoriesTreeStore.GetPath (iter);
				templateCategoriesTreeView.ScrollToCell (path, null, true, 1, 0);
			}
		}

		void SelectFirstSubTemplateCategory ()
		{
			var iters = WalkTree (templateCategoriesTreeStore, TreeIter.Zero);

			foreach (var iter in iters) {
				var currentCategory = templateCategoriesTreeStore.GetValue (iter, TemplateCategoryColumn) as TemplateCategory;
				if (currentCategory?.IsTopLevel == false) {
					templateCategoriesTreeView.Selection.SelectIter (iter);
					TreePath path = templateCategoriesTreeStore.GetPath (iter);
					templateCategoriesTreeView.ScrollToCell (path, null, true, 1, 0);
					break;
				}
			}
		}

		void SelectTemplateCategory (TemplateCategory category)
		{
			var iters = WalkTree (templateCategoriesTreeStore, TreeIter.Zero);

			foreach (var iter in iters) {
				var currentCategory = templateCategoriesTreeStore.GetValue (iter, TemplateCategoryColumn) as TemplateCategory;
				if (currentCategory == category) {
					templateCategoriesTreeView.Selection.SelectIter (iter);
					TreePath path = templateCategoriesTreeStore.GetPath (iter);
					templateCategoriesTreeView.ScrollToCell (path, null, true, 1, 0);
					break;
				}
			}
		}

		IEnumerable<TreeIter> WalkTree (TreeStore model, TreeIter iter)
		{
			if (iter.Equals(TreeIter.Zero)) {
				if (!model.GetIterFirst (out TreeIter first)) {
					yield break;
				}
				iter = first;
			}

			do {
				yield return iter;
				if (model.IterHasChild (iter)) {
					if (model.IterChildren (out TreeIter childIter, iter))
						foreach (var node in WalkTree (model, childIter))
							yield return node;
				}

			} while (model.IterNext (ref iter));
		}

		void SelectTemplate (SolutionTemplate template)
		{
			var iters = WalkTree (templatesTreeStore, TreeIter.Zero);

			foreach (var iter in iters) {
				var currentTemplate = templatesTreeStore.GetValue (iter, TemplateColumn) as SolutionTemplate;
				if (currentTemplate == template) {
					templatesTreeView.Selection.SelectIter (iter);
					TreePath path = templatesTreeStore.GetPath (iter);
					templatesTreeView.ScrollToCell (path, null, true, 1, 0);
					break;
				}
			}
		}

		void SelectFirstTemplate ()
		{
			// first child of first iter
			if (templatesTreeStore.GetIterFromString (out TreeIter iter, "0:0")) {
				templatesTreeView.Selection.SelectIter (iter);
			}
		}

		async Task MoveToNextPage ()
		{
			if (controller.IsLastPage) {
				try {
					CanMoveToNextPage = false;
					await controller.Create ();
				} catch {
					throw;
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
			if (CanMoveToNextPage && !isLastPressedKeySpace && 
			    IsSolutionTemplateOnActivatedRow ((Gtk.TreeView)o, args))
				MoveToNextPage ().Ignore();
			isLastPressedKeySpace = false;
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

