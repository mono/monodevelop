// ErrorListPad.cs
//  
// Author:
//       Todd Berman <tberman@sevenl.net>
//       David Makovský <yakeen@sannyas-on.net>
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2006 David Makovský
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Drawing;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Gui.Content;

using Gtk;
using System.Text;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using System.Linq;
using MonoDevelop.Components.AutoTest;
using System.ComponentModel;
using MonoDevelop.Ide.BuildOutputView;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Gui.Pads
{
	class ErrorListPad : PadContent
	{
		HPaned control;
		ScrolledWindow sw;
		PadTreeView view;
		BuildOutputViewContent buildOutputViewContent;
		BuildOutput buildOutput;
		TreeStore store;
		TreeModelFilter filter;
		TreeModelSort sort;
		ToggleButton errorBtn, warnBtn, msgBtn;
		Button logBtn;
		Label errorBtnLbl, warnBtnLbl, msgBtnLbl, logBtnLbl;
		SearchEntry searchEntry;
		string currentSearchPattern = null;
		Hashtable tasks = new Hashtable ();
		int errorCount;
		int warningCount;
		int infoCount;

		Clipboard clipboard;

		Xwt.Drawing.Image iconWarning;
		Xwt.Drawing.Image iconError;
		Xwt.Drawing.Image iconInfo;
		Xwt.Drawing.Image iconEmpty;

		static readonly string restoreID = "Monodevelop.ErrorListColumns";
		public readonly ConfigurationProperty<bool> ShowErrors = ConfigurationProperty.Create ("SharpDevelop.TaskList.ShowErrors", true);
		public readonly ConfigurationProperty<bool> ShowWarnings = ConfigurationProperty.Create ("SharpDevelop.TaskList.ShowWarnings", true);
		public readonly ConfigurationProperty<bool> ShowMessages = ConfigurationProperty.Create ("SharpDevelop.TaskList.ShowMessages", true);

		static class DataColumns
		{
			internal const int Type = 0;
			internal const int Read = 1;
			internal const int Task = 2;
			internal const int Description = 3;
		}

		static class VisibleColumns
		{
			internal const int Type = 0;
			internal const int Marked = 1;
			internal const int Line = 2;
			internal const int Description = 3;
			internal const int File = 4;
			internal const int Project = 5;
			internal const int Path = 6;
			internal const int Category = 7;
		}

		public override Control Control {
			get {
				if (control == null)
					CreateControl ();
				return control;
			}
		}

		public override string Id {
			get { return "MonoDevelop.Ide.Gui.Pads.ErrorListPad"; }
		}

		ToggleButton MakeButton (string image, string name, bool active, out Label label)
		{
			var btnBox = MakeHBox (image, out label);

			var btn = new ToggleButton { Name = name, Active = active };
			btn.Child = btnBox;

			return btn;
		} 

		Button MakeButton (string image, string name, out Label label) 
		{
			var btnBox = MakeHBox (image, out label);

			var btn = new Button { Name = name };
			btn.Child = btnBox;

			return btn;
		}

		HBox MakeHBox (string image, out Label label)
		{
			var btnBox = new HBox (false, 2);
			btnBox.Accessible.SetShouldIgnore (true);
			var imageView = new ImageView (image, Gtk.IconSize.Menu);
			imageView.Accessible.SetShouldIgnore (true);
			btnBox.PackStart (imageView);

			label = new Label ();
			label.Accessible.SetShouldIgnore (true);
			btnBox.PackStart (label);

			return btnBox;
		}

		protected override void Initialize (IPadWindow window)
		{
			window.Title = GettextCatalog.GetString ("Errors");

			DockItemToolbar toolbar = window.GetToolbar (DockPositionType.Top);
			toolbar.Accessible.Name = "ErrorPad.Toolbar";
			toolbar.Accessible.SetLabel ("Error Pad Toolbar");
			toolbar.Accessible.SetRole ("AXToolbar", "Pad toolbar");
			toolbar.Accessible.Description = GettextCatalog.GetString ("The Error pad toolbar");

			errorBtn = MakeButton (Stock.Error, "toggleErrors", ShowErrors, out errorBtnLbl);
			errorBtn.Accessible.Name = "ErrorPad.ErrorButton";

			errorBtn.Toggled += new EventHandler (FilterChanged);
			errorBtn.TooltipText = GettextCatalog.GetString ("Show Errors");
			errorBtn.Accessible.Description = GettextCatalog.GetString ("Show Errors");
			UpdateErrorsNum ();
			toolbar.Add (errorBtn);

			warnBtn = MakeButton (Stock.Warning, "toggleWarnings", ShowWarnings, out warnBtnLbl);
			warnBtn.Accessible.Name = "ErrorPad.WarningButton";
			warnBtn.Toggled += new EventHandler (FilterChanged);
			warnBtn.TooltipText = GettextCatalog.GetString ("Show Warnings");
			warnBtn.Accessible.Description = GettextCatalog.GetString ("Show Warnings");
			UpdateWarningsNum ();
			toolbar.Add (warnBtn);

			msgBtn = MakeButton (Stock.Information, "toggleMessages", ShowMessages, out msgBtnLbl);
			msgBtn.Accessible.Name = "ErrorPad.MessageButton";
			msgBtn.Toggled += new EventHandler (FilterChanged);
			msgBtn.TooltipText = GettextCatalog.GetString ("Show Messages");
			msgBtn.Accessible.Description = GettextCatalog.GetString ("Show Messages");
			UpdateMessagesNum ();
			toolbar.Add (msgBtn);

			var sep = new SeparatorToolItem ();
			sep.Accessible.SetShouldIgnore (true);
			toolbar.Add (sep);

			logBtn = MakeButton ("md-message-log", "toggleBuildOutput", out logBtnLbl);
			logBtn.Accessible.Name = "ErrorPad.LogButton";
			logBtn.TooltipText = GettextCatalog.GetString ("Show build output");
			logBtn.Accessible.Description = GettextCatalog.GetString ("Show build output");

			logBtnLbl.Text = GettextCatalog.GetString ("Build Output");
			logBtn.Accessible.SetTitle (logBtnLbl.Text);

			logBtn.Clicked += HandleLogBtnClicked;
			toolbar.Add (logBtn);

			buildOutput = new BuildOutput ();

			//Dummy widget to take all space between "Build Output" button and SearchEntry
			var spacer = new HBox ();
			spacer.Accessible.SetShouldIgnore (true);
			toolbar.Add (spacer, true);

			searchEntry = new SearchEntry ();
			searchEntry.Accessible.SetLabel (GettextCatalog.GetString ("Search"));
			searchEntry.Accessible.Name = "ErrorPad.Search";
			searchEntry.Accessible.Description = GettextCatalog.GetString ("Search the error data");
			searchEntry.Entry.Changed += searchPatternChanged;
			searchEntry.WidthRequest = 200;
			searchEntry.Visible = true;
			toolbar.Add (searchEntry);

			toolbar.ShowAll ();

			UpdatePadIcon ();

			IdeApp.ProjectOperations.StartBuild += OnBuildStarted;
		}

		void searchPatternChanged (object sender, EventArgs e)
		{
			currentSearchPattern = searchEntry.Entry.Text;
			filter.Refilter ();
		}

		void CreateControl ()
		{
			control = new HPaned ();

			store = new Gtk.TreeStore (typeof (Xwt.Drawing.Image), // image - type
									   typeof (bool),       // read?
									   typeof (TaskListEntry),       // read? -- use Pango weight
									   typeof (string));
			SemanticModelAttribute modelAttr = new SemanticModelAttribute ("store__Type", "store__Read", "store__Task", "store__Description");
			TypeDescriptor.AddAttributes (store, modelAttr);

			TreeModelFilterVisibleFunc filterFunct = new TreeModelFilterVisibleFunc (FilterTasks);
			filter = new TreeModelFilter (store, null);
			filter.VisibleFunc = filterFunct;
			
			sort = new TreeModelSort (filter);
			sort.SetSortFunc (VisibleColumns.Type, SeverityIterSort);
			sort.SetSortFunc (VisibleColumns.Project, ProjectIterSort);
			sort.SetSortFunc (VisibleColumns.File, FileIterSort);
			sort.SetSortFunc (VisibleColumns.Category, CategoryIterSort);

			view = new PadTreeView (sort);
			view.Selection.Mode = SelectionMode.Multiple;
			view.ShowExpanders = true;
			view.RulesHint = true;

			view.DoPopupMenu += ShowPopup;

			AddColumns ();
			LoadColumnsVisibility ();
			view.Columns [VisibleColumns.Type].SortColumnId = VisibleColumns.Type;
			view.Columns [VisibleColumns.Project].SortColumnId = VisibleColumns.Project;
			view.Columns [VisibleColumns.File].SortColumnId = VisibleColumns.File;
			view.Columns [VisibleColumns.Category].SortColumnId = VisibleColumns.Category;

			sw = new MonoDevelop.Components.CompactScrolledWindow ();
			sw.ShadowType = ShadowType.None;
			sw.Add (view);
			TaskService.Errors.TasksRemoved += ShowResults;
			TaskService.Errors.TasksAdded += TaskAdded;
			TaskService.Errors.TasksChanged += TaskChanged;
			TaskService.Errors.CurrentLocationTaskChanged += HandleTaskServiceErrorsCurrentLocationTaskChanged;

			IdeApp.Workspace.FirstWorkspaceItemOpened += OnCombineOpen;
			IdeApp.Workspace.LastWorkspaceItemClosed += OnCombineClosed;

			view.RowActivated += new RowActivatedHandler (OnRowActivated);

			iconWarning = ImageService.GetIcon (Ide.Gui.Stock.Warning, Gtk.IconSize.Menu);
			iconError = ImageService.GetIcon (Ide.Gui.Stock.Error, Gtk.IconSize.Menu);
			iconInfo = ImageService.GetIcon (Ide.Gui.Stock.Information, Gtk.IconSize.Menu);
			iconEmpty = ImageService.GetIcon (Ide.Gui.Stock.Empty, Gtk.IconSize.Menu);

			control.Add1 (sw);

			control.ShowAll ();

			// Load existing tasks
			foreach (TaskListEntry t in TaskService.Errors) {
				AddTask (t);
			}
		}

		public override void Dispose ()
		{
			IdeApp.Workspace.FirstWorkspaceItemOpened -= OnCombineOpen;
			IdeApp.Workspace.LastWorkspaceItemClosed -= OnCombineClosed;
			IdeApp.ProjectOperations.StartBuild -= OnBuildStarted;

			buildOutput?.Dispose ();
			buildOutputViewContent?.Dispose ();
			buildOutputDoc?.Close ();

			// Set the model to null as it makes Gtk clean up faster
			if (view != null) {
				view.Model = null;
			}

			base.Dispose ();
		}

		public ProgressMonitor GetBuildProgressMonitor ()
		{ 
			if (control == null)
				CreateControl ();
			
			return buildOutput.GetProgressMonitor ();
		}

		void HandleTaskServiceErrorsCurrentLocationTaskChanged (object sender, EventArgs e)
		{
			if (TaskService.Errors.CurrentLocationTask == null) {
				view.Selection.UnselectAll ();
				return;
			}
			TreeIter it;
			if (!view.Model.GetIterFirst (out it))
				return;
			do {
				TaskListEntry t = (TaskListEntry)view.Model.GetValue (it, DataColumns.Task);
				if (t == TaskService.Errors.CurrentLocationTask) {
					view.Selection.SelectIter (it);
					view.ScrollToCell (view.Model.GetPath (it), view.Columns [0], false, 0, 0);
					it = filter.ConvertIterToChildIter (sort.ConvertIterToChildIter (it));
					store.SetValue (it, DataColumns.Read, true);
					return;
				}
			} while (view.Model.IterNext (ref it));
		}

		internal void SelectTaskListEntry (TaskListEntry taskListEntry)
		{
			TreeIter iter;
			if (!view.Model.GetIterFirst (out iter))
				return;
			do {
				var t = (TaskListEntry)view.Model.GetValue (iter, DataColumns.Task);
				if (t == taskListEntry) {
					view.Selection.SelectIter (iter);
					view.ScrollToCell (view.Model.GetPath (iter), view.Columns [0], false, 0, 0);
					return;
				}
			} while (view.Model.IterNext (ref iter));
		}

		void LoadColumnsVisibility ()
		{
			var columns = PropertyService.Get (restoreID, string.Join (";", Enumerable.Repeat ("TRUE", view.Columns.Length)));
			var tokens = columns.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (view.Columns.Length == tokens.Length) {
				for (int i = 0; i < tokens.Length; i++) {
					bool visible;
					if (bool.TryParse (tokens [i], out visible))
						view.Columns [i].Visible = visible;
				}
			}
		}

		void ShowPopup (Gdk.EventButton evt)
		{
			var menu = new ContextMenu ();

			var help = new ContextMenuItem (GettextCatalog.GetString ("Show Error Reference"));
			help.Clicked += OnShowReference;
			menu.Add (help);

			var copy = new ContextMenuItem (GettextCatalog.GetString ("_Copy"));
			copy.Clicked += OnTaskCopied;
			menu.Add (copy);

			var goBuild = new ContextMenuItem (GettextCatalog.GetString ("Go to Log"));
			goBuild.Clicked += async (s, e) => await OnGoToLog (s, e);
			menu.Add (goBuild);

			var jump = new ContextMenuItem (GettextCatalog.GetString ("_Go to Task"));
			jump.Clicked += OnTaskJumpto;
			menu.Add (jump);

			var columnsMenu = new ColumnSelectorMenu (view,
			                                          restoreID,
			                                          GettextCatalog.GetString ("Type"),
			                                          GettextCatalog.GetString ("Validity"));

			var columns = new ContextMenuItem (GettextCatalog.GetString ("Columns"));
			columns.SubMenu = columnsMenu;
			menu.Add (columns);

			help.Sensitive = copy.Sensitive = jump.Sensitive =
				view.Selection != null &&
				view.Selection.CountSelectedRows () > 0 &&
				view.IsAColumnVisible ();

			// Disable Help and Go To if multiple rows selected.
			if (help.Sensitive && view.Selection.CountSelectedRows () > 1) {
				help.Sensitive = false;
				jump.Sensitive = false;
			}

			string dummyString;
			help.Sensitive &= GetSelectedErrorReference (out dummyString);

			menu.Show (view, evt);
		}

		async Task OnGoToLog (object o, EventArgs args)
		{
			var rows = view.Selection.GetSelectedRows ();
			if (!rows.Any ())
				return;

			TreeIter iter, sortedIter;
			if (view.Model.GetIter (out sortedIter, rows [0])) {
				iter = filter.ConvertIterToChildIter (sort.ConvertIterToChildIter (sortedIter));

				store.SetValue (iter, DataColumns.Read, true);

				TaskListEntry task = store.GetValue (iter, DataColumns.Task) as TaskListEntry;
				if (task != null) {
					await OpenBuildOutputViewDocument ().ConfigureAwait (false);
					if (task.Severity == TaskSeverity.Error) {
						buildOutputViewContent.GoToError (task.Message, task.GetProjectWithExtension ());
					} else if (task.Severity == TaskSeverity.Warning) {
						buildOutputViewContent.GoToWarning (task.Message, task.GetProjectWithExtension ());
					} else if (task.Severity == TaskSeverity.Information) {
						buildOutputViewContent.GoToMessage (task.Message, task.GetProjectWithExtension ());
					}
				}
			}
		}

		TaskListEntry SelectedTask {
			get {
				TreeIter iter;
				var rows = view.Selection.GetSelectedRows ();
				if (rows.Any () && view.Model.GetIter (out iter, rows[0]))
					return view.Model.GetValue (iter, DataColumns.Task) as TaskListEntry;
				return null; // no one selected
			}
		}

		IEnumerable<TaskListEntry> GetSelectedTasks ()
		{
			TreeIter iter;
			foreach (var row in view.Selection.GetSelectedRows ()) {
				if (view.Model.GetIter (out iter, row)) {
					var task = view.Model.GetValue (iter, DataColumns.Task) as TaskListEntry;
					if (task != null)
						yield return task;
				}
			}
		}

		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			OnTaskCopied (null, null);
		}

		void OnTaskCopied (object o, EventArgs args)
		{
			var selectedTasks = GetSelectedTasks ().ToArray ();
			if (!selectedTasks.Any ())
				return;

			var text = new StringBuilder ();

			for (int i = 0; i < selectedTasks.Length; i++) {
				if (i > 0)
					text.Append (Environment.NewLine);

				TaskListEntry task = selectedTasks [i];
				if (!string.IsNullOrEmpty (task.FileName)) {
					text.Append (task.FileName);
					if (task.Line >= 1) {
						text.Append ("(").Append (task.Column);
						if (task.Column >= 0)
							text.Append (",").Append (task.Column);
						text.Append (")");
					}
					text.Append (": ");
				}
				text.Append (task.Severity.ToString ());
				if (!string.IsNullOrEmpty (task.Code)) {
					text.Append (" ").Append (task.Code);
				}
				text.Append (": ");
				text.Append (task.Description);
				if (task.WorkspaceObject != null)
					text.Append (" (").Append (task.WorkspaceObject.Name).Append (")");

				if (!string.IsNullOrEmpty (task.Category)) {
					text.Append (" ").Append (task.Category);
				}
			}

			clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = text.ToString ();
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = text.ToString ();
		}

		void OnShowReference (object o, EventArgs args)
		{
			string reference = null;
			if (GetSelectedErrorReference (out reference) && reference != null)
				DesktopService.ShowUrl (reference);
		}

		bool GetSelectedErrorReference (out string reference)
		{
			string webRequest = "http://google.com/search?q=";
			TaskListEntry task = SelectedTask;
			if (task != null && task.HasDocumentationLink ()) {
				reference = task.DocumentationLink;
				return true;
			}
			if (task != null && !string.IsNullOrEmpty (task.HelpKeyword)) {
				reference = webRequest + System.Web.HttpUtility.UrlEncode (task.HelpKeyword);
				return true;
			}
			if (task != null && !string.IsNullOrEmpty (task.Code)) {
				reference = webRequest + System.Web.HttpUtility.UrlEncode (task.Code);
				return true;
			}
			reference = null;
			return false;
		}

		void OnTaskJumpto (object o, EventArgs args)
		{
			var rows = view.Selection.GetSelectedRows ();
			if (!rows.Any ())
				return;

			TreeIter iter, sortedIter;
			if (view.Model.GetIter (out sortedIter, rows [0])) {
				iter = filter.ConvertIterToChildIter (sort.ConvertIterToChildIter (sortedIter));
				store.SetValue (iter, DataColumns.Read, true);
				TaskListEntry task = store.GetValue (iter, DataColumns.Task) as TaskListEntry;
				if (task != null) {
					TaskService.ShowStatus (task);
					task.JumpToPosition ();
					TaskService.Errors.CurrentLocationTask = task;
					IdeApp.Workbench.ActiveLocationList = TaskService.Errors;
				}
			}
		}

		void AddColumns ()
		{
			CellRendererImage iconRender = new CellRendererImage ();

			Gtk.CellRendererToggle toggleRender = new Gtk.CellRendererToggle ();
			toggleRender.Toggled += new ToggledHandler (ItemToggled);

			TreeViewColumn col;
			col = view.AppendColumn ("!", iconRender, "image", DataColumns.Type);

			col = view.AppendColumn ("", toggleRender);
			col.SetCellDataFunc (toggleRender, new Gtk.TreeCellDataFunc (ToggleDataFunc));

			col = view.AppendColumn (GettextCatalog.GetString ("Line"), view.TextRenderer);
			col.SetCellDataFunc (view.TextRenderer, new Gtk.TreeCellDataFunc (LineDataFunc));

			var descriptionCellRenderer = new DescriptionCellRendererText ();
			view.RegisterRenderForFontChanges (descriptionCellRenderer);
			var descriptionCol = view.AppendColumn (GettextCatalog.GetString ("Description"), descriptionCellRenderer);
			descriptionCol.SetCellDataFunc (descriptionCellRenderer, new Gtk.TreeCellDataFunc (DescriptionDataFunc));
			descriptionCol.Resizable = true;
			descriptionCellRenderer.PreferedMaxWidth = IdeApp.Workbench.RootWindow.Allocation.Width / 3;
			descriptionCellRenderer.WrapWidth = descriptionCellRenderer.PreferedMaxWidth;
			descriptionCellRenderer.WrapMode = Pango.WrapMode.Word;

			col = view.AppendColumn (GettextCatalog.GetString ("File"), view.TextRenderer);
			col.SetCellDataFunc (view.TextRenderer, new Gtk.TreeCellDataFunc (FileDataFunc));
			col.Resizable = true;
			
			col = view.AppendColumn (GettextCatalog.GetString ("Project"), view.TextRenderer);
			col.SetCellDataFunc (view.TextRenderer, new Gtk.TreeCellDataFunc (ProjectDataFunc));
			col.Resizable = true;
			
			col = view.AppendColumn (GettextCatalog.GetString ("Path"), view.TextRenderer);
			col.SetCellDataFunc (view.TextRenderer, new Gtk.TreeCellDataFunc (PathDataFunc));
			col.Resizable = true;

			col = view.AppendColumn (GettextCatalog.GetString ("Category"), view.TextRenderer);
			col.SetCellDataFunc (view.TextRenderer, new Gtk.TreeCellDataFunc (CategoryDataFunc));
			col.Resizable = true;
		}

		static void ToggleDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererToggle toggleRenderer = (Gtk.CellRendererToggle)cell;
			TaskListEntry task = model.GetValue (iter, DataColumns.Task) as TaskListEntry; 
			if (task == null) {
				toggleRenderer.Visible = false;
				return;
			}
			toggleRenderer.Active = task.Completed;
		}
		
		static void LineDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText textRenderer = (Gtk.CellRendererText)cell;
			TaskListEntry task = model.GetValue (iter, DataColumns.Task) as TaskListEntry; 
			if (task == null) {
				textRenderer.Text = "";
				return;
			}
			SetText (textRenderer, model, iter, task, task.Line != 0 ? task.Line.ToString () : "");
		}

		static void DescriptionDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			var textRenderer = (CellRendererText)cell;
			TaskListEntry task = model.GetValue (iter, DataColumns.Task) as TaskListEntry; 
			var text = model.GetValue (iter, DataColumns.Description) as string;
			if (task == null) {
				if (model.IterParent (out iter, iter)) {
					task = model.GetValue (iter, DataColumns.Task) as TaskListEntry;
					if (task == null) {
						textRenderer.Text = "";
						return;
					}
				} else {
					textRenderer.Text = "";
					return;
				}
			}
			SetText (textRenderer, model, iter, task, text);
		}

		static void FileDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText textRenderer = (Gtk.CellRendererText)cell;
			TaskListEntry task = model.GetValue (iter, DataColumns.Task) as TaskListEntry; 
			if (task == null) {
				textRenderer.Text = "";
				return;
			}

			SetText (textRenderer, model, iter, task, task.GetFile ());
		}
		
		static void ProjectDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText textRenderer = (Gtk.CellRendererText)cell;
			TaskListEntry task = model.GetValue (iter, DataColumns.Task) as TaskListEntry; 
			if (task == null) {
				textRenderer.Text = "";
				return;
			}
			SetText (textRenderer, model, iter, task, task.GetProject ());
		}
		
		static void PathDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText textRenderer = (Gtk.CellRendererText)cell;
			TaskListEntry task = model.GetValue (iter, DataColumns.Task) as TaskListEntry; 
			if (task == null) {
				textRenderer.Text = "";
				return;
			}
			SetText (textRenderer, model, iter, task, task.GetPath ());
		}

		static void CategoryDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText textRenderer = (Gtk.CellRendererText)cell;
			var task = model.GetValue (iter, DataColumns.Task) as TaskListEntry;
			if (task == null) {
				textRenderer.Text = "";
				return;
			}
			SetText (textRenderer, model, iter, task, task.Category ?? "");
		}
		
		static void SetText (CellRendererText textRenderer, TreeModel model, TreeIter iter, TaskListEntry task, string text)
		{
			textRenderer.Text = text;
			textRenderer.Weight = (int)((bool)model.GetValue (iter, DataColumns.Read) ? Pango.Weight.Normal : Pango.Weight.Bold);
			textRenderer.Strikethrough = task.Completed;
		}
		
		void OnCombineOpen(object sender, EventArgs e)
		{
			Clear();
		}
		
		void OnCombineClosed(object sender, EventArgs e)
		{
			Clear();

			buildOutput.Dispose ();
			if (buildOutputViewContent != null) {
				buildOutputViewContent.Dispose ();
				buildOutputViewContent = null;
			}

			buildOutputDoc?.Close ();

			buildOutput = new BuildOutput ();
		}

		void OnBuildStarted (object sender, EventArgs e)
		{
			buildOutput.Clear ();
		}
		
		void OnRowActivated (object o, RowActivatedArgs args)
		{
			OnTaskJumpto (null, null);
		}
		
		public CompilerResults CompilerResults = null;
		
		void FilterChanged (object sender, EventArgs e)
		{
			
			ShowErrors.Value = errorBtn.Active;
			ShowWarnings.Value = warnBtn.Active;
			ShowMessages.Value = msgBtn.Active;
			
			filter.Refilter ();
		}

		internal void SetFilter (bool showErrors, bool showWarnings, bool showMessages)
		{
			errorBtn.Active = showErrors;
			warnBtn.Active = showWarnings;
			msgBtn.Active = showMessages;
		}


		bool FilterTasks (TreeModel model, TreeIter iter)
		{
			bool canShow = false;

			try {
				TaskListEntry task = store.GetValue (iter, DataColumns.Task) as TaskListEntry;
				if (task == null)
					return true;
				if (task.Severity == TaskSeverity.Error && errorBtn.Active) canShow = true;
				else if (task.Severity == TaskSeverity.Warning && warnBtn.Active) canShow = true;
				else if (task.Severity == TaskSeverity.Information && msgBtn.Active) canShow = true;

				if (canShow && !string.IsNullOrWhiteSpace (currentSearchPattern)) {
					canShow = (task.Description != null && task.Description.IndexOf (currentSearchPattern, StringComparison.OrdinalIgnoreCase) != -1) ||
						(task.Code != null && task.Code.IndexOf (currentSearchPattern, StringComparison.OrdinalIgnoreCase) != -1) ||
						(task.FileName != null && task.FileName.FileName.IndexOf (currentSearchPattern, StringComparison.OrdinalIgnoreCase) != -1) ||
						(task.WorkspaceObject != null && task.WorkspaceObject.Name != null && task.WorkspaceObject.Name.IndexOf (currentSearchPattern, StringComparison.OrdinalIgnoreCase) != -1) ||
						(task.Category != null && task.Category.IndexOf (currentSearchPattern, StringComparison.OrdinalIgnoreCase) != -1);
				}
			} catch {
				//Not yet fully added
				return false;
			}
			
			return canShow;
		}

		public void ShowResults (object sender, EventArgs e)
		{
			Clear();

			AddTasks (TaskService.Errors);
		}

		private void Clear()
		{
			errorCount = warningCount = infoCount = 0;
			if (view.IsRealized)
				view.ScrollToPoint (0, 0);
			store.Clear ();
			tasks.Clear ();
			UpdateErrorsNum ();
			UpdateWarningsNum ();
			UpdateMessagesNum ();
			UpdatePadIcon ();
		}
		
		void TaskChanged (object sender, TaskEventArgs e)
		{
			this.view.QueueDraw ();
		}
	
		void TaskAdded (object sender, TaskEventArgs e)
		{
			AddTasks (e.Tasks);
		}
		
		public void AddTasks (IEnumerable<TaskListEntry> tasks)
		{
			int n = 1;
			foreach (TaskListEntry t in tasks) {
				AddTaskInternal (t);
				if ((n++ % 100) == 0) {
					// Adding many tasks is a bit slow, so refresh the
					// ui at every block of 100.
					DispatchService.RunPendingEvents ();
				}
			}
			filter.Refilter ();
		}
		
		public void AddTask (TaskListEntry t)
		{
			AddTaskInternal (t);
			filter.Refilter ();
		}

		void AddTaskInternal (TaskListEntry t)
		{
			if (tasks.Contains (t)) return;

			Xwt.Drawing.Image stock;
			
			switch (t.Severity) {
				case TaskSeverity.Error:
					stock = iconError;
					errorCount++;
					UpdateErrorsNum ();
					break; 
				case TaskSeverity.Warning:
					stock = iconWarning;
					warningCount++;
					UpdateWarningsNum ();	
					break;
				default:
					stock = iconInfo;
					infoCount++;
					UpdateMessagesNum ();
					break;
			}
			
			tasks [t] = t;

			var indexOfNewLine = t.Description.IndexOfAny (new [] { '\n', '\r' });
			if (indexOfNewLine != -1) {
				var iter = store.AppendValues (stock, false, t, t.Description.Remove (indexOfNewLine));
				store.AppendValues (iter, iconEmpty, false, null, t.Description);
			} else {
				store.AppendValues (stock, false, t, t.Description);
			}

			UpdatePadIcon ();
		}

		void UpdateErrorsNum () 
		{
			errorBtnLbl.Text = " " + string.Format(GettextCatalog.GetPluralString("{0} Error", "{0} Errors", errorCount), errorCount);
			errorBtn.Accessible.SetTitle (errorBtnLbl.Text);
		}

		void UpdateWarningsNum ()
		{
			warnBtnLbl.Text = " " + string.Format(GettextCatalog.GetPluralString("{0} Warning", "{0} Warnings", warningCount), warningCount);
			warnBtn.Accessible.SetTitle (warnBtnLbl.Text);
		}

		void UpdateMessagesNum ()
		{
			msgBtnLbl.Text = " " + string.Format(GettextCatalog.GetPluralString("{0} Message", "{0} Messages", infoCount), infoCount);
			msgBtn.Accessible.SetTitle (msgBtnLbl.Text);
		}

		void UpdatePadIcon ()
		{
			if (errorCount > 0)
				Window.Icon = "md-errors-list-has-errors";
			else if (warningCount > 0)
				Window.Icon = "md-errors-list-has-warnings";
			else
				Window.Icon = "md-errors-list";
		}
		
		private void ItemToggled (object o, ToggledArgs args)
		{
			Gtk.TreeIter iter;

			if (view.Model.GetIterFromString (out iter, args.Path)) {
				TaskListEntry task = (TaskListEntry)view.Model.GetValue (iter, DataColumns.Task);
				task.Completed = !task.Completed;
				TaskService.FireTaskToggleEvent (this, new TaskEventArgs (task));
			}
		}

		static int SeverityIterSort(TreeModel model, TreeIter a, TreeIter z)
		{
			TaskListEntry aTask = model.GetValue(a, DataColumns.Task) as TaskListEntry,
			     zTask = model.GetValue(z, DataColumns.Task) as TaskListEntry;
			     
			return (aTask != null && zTask != null) ?
			       aTask.Severity.CompareTo(zTask.Severity) :
			       0;
		}
		
		static int ProjectIterSort (TreeModel model, TreeIter a, TreeIter z)
		{
			TaskListEntry aTask = model.GetValue (a, DataColumns.Task) as TaskListEntry,
			     zTask = model.GetValue (z, DataColumns.Task) as TaskListEntry;
			     
			return (aTask != null && zTask != null) ?
			       string.Compare (aTask.GetProject (), zTask.GetProject (), StringComparison.Ordinal) :
			       0;
		}
		
		static int FileIterSort (TreeModel model, TreeIter a, TreeIter z)
		{
			TaskListEntry aTask = model.GetValue (a, DataColumns.Task) as TaskListEntry,
			     zTask = model.GetValue (z, DataColumns.Task) as TaskListEntry;
			     
			return (aTask != null && zTask != null) ?
			       aTask.FileName.CompareTo (zTask.FileName) :
			       0;
		}

		static int CategoryIterSort (TreeModel model, TreeIter a, TreeIter z)
		{
			TaskListEntry aTask = model.GetValue (a, DataColumns.Task) as TaskListEntry,
				 zTask = model.GetValue (z, DataColumns.Task) as TaskListEntry;

			return (aTask?.Category != null && zTask?.Category != null) ?
			       string.Compare (aTask.Category, zTask.Category, StringComparison.Ordinal) :
			       0;
		}

		internal void FocusOutputView ()
		{
			HandleLogBtnClicked (this, EventArgs.Empty);
		}

		Document buildOutputDoc;
		async void HandleLogBtnClicked (object sender, EventArgs e)
		{
			await OpenBuildOutputViewDocument ().ConfigureAwait (false);
		}

		async Task OpenBuildOutputViewDocument () 
		{
			if (buildOutputViewContent == null) {
				buildOutputViewContent = new BuildOutputViewContent (buildOutput);
				buildOutputDoc = IdeApp.Workbench.OpenDocument (buildOutputViewContent, true);
				buildOutputDoc.Closed += BuildOutputDocClosed;

				await buildOutputViewContent.ProcessLogs (false);
			} else if (buildOutputDoc != null) {
				buildOutputDoc.Select ();
			}
		}

		void BuildOutputDocClosed (object sender, EventArgs e)
		{
			buildOutputViewContent.Dispose ();
			buildOutputViewContent = null;
			buildOutputDoc = null;
		}

		class DescriptionCellRendererText : CellRendererText
		{
			public int PreferedMaxWidth { get; set; }

			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				int originalWrapWidth = WrapWidth;
				WrapWidth = -1;
				// First calculate Width with WrapWidth=-1 which will give us
				// Width of text in one line(without wrapping)
				base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
				int oneLineWidth = width;
				WrapWidth = originalWrapWidth;
				// originalWrapWidth(aka WrapWidth) equals to actual width of Column if oneLineWidth is bigger
				// then column width/height we must recalculate, because Height is atm for one line
				// and not multipline that WrapWidth creates...
				if (oneLineWidth > originalWrapWidth) {
					base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
				}
				width = Math.Min (oneLineWidth, PreferedMaxWidth);
			}
		}
	}

	public static class TaskListEntryExtensions
	{
		public static string GetPath (this TaskListEntry task)
		{
			if (task.WorkspaceObject != null)
				return FileService.AbsoluteToRelativePath (task.WorkspaceObject.BaseDirectory, task.FileName);

			return task.FileName;
		}

		public static string GetProject (this TaskListEntry task)
		{
			return (task != null && task.WorkspaceObject is SolutionFolderItem) ? task.WorkspaceObject.Name : string.Empty;
		}

		public static string GetProjectWithExtension (this TaskListEntry task)
		{
			return (task != null && task.WorkspaceObject is SolutionItem) ? Path.GetFileName (((SolutionItem)task.WorkspaceObject).FileName) : string.Empty;
		}

		public static string GetFile (this TaskListEntry task)
		{
			string tmpPath = "";
			string fileName = "";
			try {
				tmpPath = GetPath (task);
				fileName = Path.GetFileName (tmpPath);
			} catch (Exception) {
				fileName = tmpPath;
			}
			return fileName;
		}
	}
}
