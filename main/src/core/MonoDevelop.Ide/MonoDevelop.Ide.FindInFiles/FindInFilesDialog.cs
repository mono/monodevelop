// 
// FindInFilesDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
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
using System.Linq;
using System.Threading;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Content;
using System.Threading.Tasks;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.FindInFiles
{
	public enum PathMode {
		Absolute,
		Relative,
		Hidden
	}

	public partial class FindInFilesDialog : Gtk.Dialog
	{
		readonly bool writeScope = true;
		
		enum SearchScope {
			WholeWorkspace,
			CurrentProject,
			AllOpenFiles,
			Directories,
			CurrentDocument,
			Selection
		}
		
		CheckButton checkbuttonRecursively;
		ComboBoxEntry comboboxentryReplace;
		ComboBoxEntry comboboxentryPath;
		SearchEntry searchentryFileMask;
		Button buttonBrowsePaths;
		Button buttonReplace;
		Label labelFileMask;
		Label labelReplace;
		Label labelPath;
		HBox hboxPath;
		
		Properties properties = null;
		bool replaceMode = false;
		
		static void SetButtonIcon (Button button, string stockIcon)
		{
			Alignment alignment = new Alignment (0.5f, 0.5f, 0f, 0f);
			Label label = new Label (button.Label);
			HBox hbox = new HBox (false, 2);
			ImageView image = new ImageView ();
			
			image.Image = ImageService.GetIcon (stockIcon, IconSize.Menu);
			image.Show ();
			hbox.Add (image);
			
			label.Show ();
			hbox.Add (label);
			
			hbox.Show ();
			alignment.Add (hbox);
			
			button.Child.Destroy ();
			
			alignment.Show ();
			button.Add (alignment);
		}
		
		static Widget GetChildWidget (Container toplevel, Type type)
		{
			foreach (var child in ((Container) toplevel).Children) {
				if (child.GetType () ==  type)
					return child;
				
				if (child is Container) {
					var w = GetChildWidget ((Container) child, type);
					if (w != null)
						return w;
				}
			}
			
			return null;
		}
		
		static void OverrideStockLabel (Button button, string label)
		{
			var widget = GetChildWidget ((Container) button.Child, typeof (Label));
			if (widget != null)
				((Label) widget).LabelProp = label;
		}
		
		FindInFilesDialog (bool showReplace, string directory) : this (showReplace)
		{
			comboboxScope.Active = (int)SearchScope.Directories;
			comboboxentryPath.Entry.Text = directory;
			writeScope = false;
		}
		
		public static string FormatPatternToSelectionOption (string pattern, bool regex)
		{
			if (pattern == null)
				return null;
			if (regex) {
				var sb = new StringBuilder ();
				foreach (var ch in pattern) {
					if (!char.IsLetterOrDigit (ch))
						sb.Append ('\\');
					sb.Append (ch);
				}
				return sb.ToString ();
			}
			return pattern;
		}

		FindInFilesDialog (bool showReplace)
		{
			Build ();
			IdeTheme.ApplyTheme (this);
			
			properties = PropertyService.Get ("MonoDevelop.FindReplaceDialogs.SearchOptions", new Properties ());
			SetButtonIcon (toggleReplaceInFiles, "gtk-find-and-replace");
			SetButtonIcon (toggleFindInFiles, "gtk-find");

			// If we have an active floating window, attach the dialog to it. Otherwise use the main IDE window.
			var current_toplevel = Gtk.Window.ListToplevels ().FirstOrDefault (x => x.IsActive);
			if (current_toplevel is Components.DockNotebook.DockWindow)
				TransientFor = current_toplevel;
			else
				TransientFor = IdeApp.Workbench.RootWindow;

			toggleReplaceInFiles.Active = showReplace;
			toggleFindInFiles.Active = !showReplace;
			
			toggleFindInFiles.Toggled += delegate {
				if (toggleFindInFiles.Active) {
					Title = GettextCatalog.GetString ("Find in Files");
					HideReplaceUI ();
				}
			};
			
			toggleReplaceInFiles.Toggled += delegate {
				if (toggleReplaceInFiles.Active) {
					Title = GettextCatalog.GetString ("Replace in Files");
					ShowReplaceUI ();
				}
			};
			
			buttonSearch.Clicked += HandleSearchClicked;
			buttonClose.Clicked += (sender, e) => Destroy ();
			DeleteEvent += (o, args) => Destroy ();
			buttonSearch.GrabDefault ();

			buttonStop.Clicked += ButtonStopClicked;
			var scopeStore = new ListStore (typeof(string));

			var workspace = IdeApp.Workspace;
			if (workspace != null && workspace.GetAllSolutions ().Count() == 1) {
				scopeStore.AppendValues (GettextCatalog.GetString ("Whole solution"));
			} else {
				scopeStore.AppendValues (GettextCatalog.GetString ("All solutions"));
			}
			scopeStore.AppendValues (GettextCatalog.GetString ("Current project"));
			scopeStore.AppendValues (GettextCatalog.GetString ("All open files"));
			scopeStore.AppendValues (GettextCatalog.GetString ("Directories"));
			scopeStore.AppendValues (GettextCatalog.GetString ("Current document"));
			scopeStore.AppendValues (GettextCatalog.GetString ("Selection"));
			comboboxScope.Model = scopeStore;

			comboboxScope.Changed += HandleScopeChanged;

			InitFromProperties ();
			
			if (showReplace)
				toggleReplaceInFiles.Toggle ();
			else
				toggleFindInFiles.Toggle ();

			if (IdeApp.Workbench.ActiveDocument != null) {
				var view = IdeApp.Workbench.ActiveDocument.Editor;
				if (view != null) {
					string selectedText = FormatPatternToSelectionOption (view.SelectedText, properties.Get ("RegexSearch", false));
					if (!string.IsNullOrEmpty (selectedText)) {
						if (selectedText.Any (c => c == '\n' || c == '\r')) {
//							comboboxScope.Active = ScopeSelection; 
						} else {
							if (comboboxScope.Active == (int) SearchScope.Selection)
								comboboxScope.Active = (int) SearchScope.CurrentDocument;
							comboboxentryFind.Entry.Text = selectedText;
						}
					} else if (comboboxScope.Active == (int) SearchScope.Selection) {
						comboboxScope.Active = (int) SearchScope.CurrentDocument;
					}
					
				}
			}
			comboboxentryFind.Entry.SelectRegion (0, comboboxentryFind.ActiveText.Length);
			comboboxentryFind.GrabFocus ();
			DeleteEvent += delegate { Destroy (); };
			UpdateStopButton ();
			UpdateSensitivity ();
			if (!buttonSearch.Sensitive) {
				comboboxScope.Active = (int)SearchScope.Directories;
			}

			Child.Show ();
			updateTimer = GLib.Timeout.Add (750, delegate {
				UpdateSensitivity ();
				return true;
			});

			SetupAccessibility ();

		}

		void SetupAccessibility ()
		{
			comboboxentryFind.SetCommonAccessibilityAttributes ("FindInFilesDialog.comboboxentryFind",
												"Find",
												GettextCatalog.GetString ("Enter string to find"));
			comboboxentryFind.SetAccessibilityLabelRelationship (labelFind);




			searchentryFileMask.SetCommonAccessibilityAttributes ("FindInFilesDialog.searchentryFileMask",
												"File Mask",
												GettextCatalog.GetString ("Enter the file mask"));
			searchentryFileMask.SetAccessibilityLabelRelationship (labelFileMask);

		}

		void SetupAccessibilityForReplace ()
		{
			comboboxentryReplace.SetCommonAccessibilityAttributes ("FindInFilesDialog.comboboxentryReplace",
											"Replace",
											GettextCatalog.GetString ("Enter string to replace"));
			comboboxentryReplace.SetAccessibilityLabelRelationship (labelReplace);
			
		}


		void SetupAccessibilityForPath ()
		{
			comboboxentryPath.SetCommonAccessibilityAttributes ("FindInFilesDialog.comboboxentryPath",
												"Path",
												GettextCatalog.GetString ("Enter the Path"));
			comboboxentryPath.SetAccessibilityLabelRelationship (labelPath);

		}



		static void TableAddRow (Table table, uint row, Widget column1, Widget column2)
		{
			uint rows = table.NRows;
			Table.TableChild tr;
			
			table.NRows = rows + 1;
			
			foreach (var child in table.Children) {
				tr = (Table.TableChild) table[child];
				uint bottom = tr.BottomAttach;
				uint top = tr.TopAttach;
				
				if (top >= row && top < rows) {
					tr.BottomAttach = bottom + 1;
					tr.TopAttach = top + 1;
				}
			}
			
			if (column1 != null) {
				table.Add (column1);
				
				tr = (Table.TableChild) table[column1];
				tr.XOptions = (AttachOptions) 4;
				tr.YOptions = (AttachOptions) 4;
				tr.BottomAttach = row + 1;
				tr.TopAttach = row;
				tr.LeftAttach = 0;
				tr.RightAttach = 1;
			}
			
			if (column2 != null) {
				table.Add (column2);
				
				tr = (Table.TableChild) table[column2];
				tr.XOptions = (AttachOptions) 4;
				tr.YOptions = (AttachOptions) 4;
				tr.BottomAttach = row + 1;
				tr.TopAttach = row;
				tr.LeftAttach = 1;
				tr.RightAttach = 2;
			}
		}
		
		static void TableRemoveRow (Table table, uint row, Widget column1, Widget column2, bool destroy)
		{
			uint rows = table.NRows;
			
			foreach (var child in table.Children) {
				var tr = (Table.TableChild) table[child];
				uint bottom = tr.BottomAttach;
				uint top = tr.TopAttach;
				
				if (top >= row && top < rows) {
					tr.BottomAttach = bottom - 1;
					tr.TopAttach = top - 1;
				}
			}
			
			if (column1 != null) {
				table.Remove (column1);
				if (destroy)
					column1.Destroy ();
			}
			
			if (column2 != null) {
				table.Remove (column2);
				if (destroy)
					column2.Destroy ();
			}
			
			table.NRows--;
		}
		
		static uint TableGetRowForItem (Table table, Widget item)
		{
			var child = (Table.TableChild) table[item];
			return child.TopAttach;
		}
		
		void ShowReplaceUI ()
		{
			if (replaceMode)
				return;
			
			labelReplace = new Label { Text = GettextCatalog.GetString ("_Replace:"), Xalign = 0f, UseUnderline = true };
			comboboxentryReplace = new ComboBoxEntry ();
			LoadHistory ("MonoDevelop.FindReplaceDialogs.ReplaceHistory", comboboxentryReplace);
			comboboxentryReplace.Show ();
			labelReplace.Show ();
			SetupAccessibilityForReplace ();

			TableAddRow (tableFindAndReplace, 1, labelReplace, comboboxentryReplace);
			
			buttonReplace = new Button () {
				Label = "gtk-find-and-replace",
				UseUnderline = true,
				CanDefault = true,
				UseStock = true,
			};
			// Note: We override the stock label text instead of using SetButtonIcon() because the
			// theme may override whether or not the icons are shown. Using SetButtonIcon() would
			// break the theme by forcing icons even if the theme says "no".
			OverrideStockLabel (buttonReplace, GettextCatalog.GetString ("R_eplace"));
			buttonReplace.Clicked += HandleReplaceClicked;
			buttonReplace.Show ();
			
			AddActionWidget (buttonReplace, 0);
			buttonReplace.GrabDefault ();
			
			replaceMode = true;
			
			Requisition req = SizeRequest ();
			Resize (req.Width, req.Height);
		}
		
		void HideReplaceUI ()
		{
			if (!replaceMode)
				return;
			
			buttonReplace.Destroy ();
			buttonReplace = null;
			
			buttonSearch.GrabDefault ();
			
			StoreHistory ("MonoDevelop.FindReplaceDialogs.ReplaceHistory", comboboxentryReplace);
			TableRemoveRow (tableFindAndReplace, 1, labelReplace, comboboxentryReplace, true);
			comboboxentryReplace = null;
			labelReplace = null;
			
			replaceMode = false;
			
			Requisition req = SizeRequest ();
			Resize (req.Width, req.Height);
		}
		
		void ShowDirectoryPathUI ()
		{
			if (labelPath != null)
				return;
			
			// We want to add the Path combo box right below the Scope 
			uint row = TableGetRowForItem (tableFindAndReplace, labelScope) + 1;
			
			// DirectoryScope
			labelPath = new Label {
				LabelProp = GettextCatalog.GetString ("_Path:"),
				UseUnderline = true, 
				Xalign = 0f
			};
			labelPath.Show ();
			
			hboxPath = new HBox ();
			comboboxentryPath = new ComboBoxEntry ();
			comboboxentryPath.Destroyed += ComboboxentryPathDestroyed;
			LoadHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", comboboxentryPath);
			comboboxentryPath.Show ();
			hboxPath.PackStart (comboboxentryPath);
			
			labelPath.MnemonicWidget = comboboxentryPath;

			SetupAccessibilityForPath ();
			
			buttonBrowsePaths = new Button { Label = "..." };
			buttonBrowsePaths.Clicked += ButtonBrowsePathsClicked;
			buttonBrowsePaths.Show ();
			hboxPath.PackStart (buttonBrowsePaths, false, false, 0);
			hboxPath.Show ();
			
			// Add the Directory Path row to the table
			TableAddRow (tableFindAndReplace, row++, labelPath, hboxPath);
			
			// Add a checkbox for searching the directory recursively...
			checkbuttonRecursively = new CheckButton {
				Label = GettextCatalog.GetString ("Re_cursively"),
				Active = properties.Get ("SearchPathRecursively", true),
				UseUnderline = true
			};
			
			checkbuttonRecursively.Destroyed += CheckbuttonRecursivelyDestroyed;
			checkbuttonRecursively.Show ();
			
			TableAddRow (tableFindAndReplace, row, null, checkbuttonRecursively);
		}
		
		void HideDirectoryPathUI ()
		{
			if (labelPath == null)
				return;
			
			uint row = TableGetRowForItem (tableFindAndReplace, checkbuttonRecursively);
			TableRemoveRow (tableFindAndReplace, row, null, checkbuttonRecursively, true);
			checkbuttonRecursively = null;
			
			row = TableGetRowForItem (tableFindAndReplace, labelPath);
			TableRemoveRow (tableFindAndReplace, row, labelPath, hboxPath, true);
			// comboboxentryPath and buttonBrowsePaths are destroyed with hboxPath
			buttonBrowsePaths = null;
			comboboxentryPath = null;
			labelPath = null;
			hboxPath = null;
		}
		
		void ShowFileMaskUI ()
		{
			if (labelFileMask != null)
				return;
			
			uint row;
			
			if (checkbuttonRecursively != null)
				row = TableGetRowForItem (tableFindAndReplace, checkbuttonRecursively) + 1;
			else
				row = TableGetRowForItem (tableFindAndReplace, labelScope) + 1;
			
			labelFileMask = new Label {
				LabelProp = GettextCatalog.GetString ("_File Mask:"),
				UseUnderline = true, 
				Xalign = 0f
			};
			labelFileMask.Show ();
			
			searchentryFileMask = new SearchEntry () {
				ForceFilterButtonVisible = false,
				IsCheckMenu = true,
				ActiveFilterID = 0,
				Visible = true,
				Ready = true,
			};
			
			var checkMenuItem = searchentryFileMask.AddFilterOption (0, GettextCatalog.GetString ("Include binary files"));
			checkMenuItem.DrawAsRadio = false;
			checkMenuItem.Active = properties.Get ("IncludeBinaryFiles", false);
			checkMenuItem.Toggled += delegate {
				properties.Set ("IncludeBinaryFiles", checkMenuItem.Active);
			};
			
			var checkMenuItem1 = searchentryFileMask.AddFilterOption (1, GettextCatalog.GetString ("Include hidden files and directories"));
			checkMenuItem1.DrawAsRadio = false;
			checkMenuItem1.Active = properties.Get ("IncludeHiddenFiles", false);
			checkMenuItem1.Toggled += delegate {
				properties.Set ("IncludeHiddenFiles", checkMenuItem1.Active);
			};
			
			searchentryFileMask.Query = properties.Get ("MonoDevelop.FindReplaceDialogs.FileMask", "");
			
			searchentryFileMask.Entry.ActivatesDefault = true;
			searchentryFileMask.Show ();
			
			TableAddRow (tableFindAndReplace, row, labelFileMask, searchentryFileMask);
		}
		
		void HideFileMaskUI ()
		{
			if (labelFileMask == null)
				return;
			
			properties.Set ("MonoDevelop.FindReplaceDialogs.FileMask", searchentryFileMask.Query);
			
			uint row = TableGetRowForItem (tableFindAndReplace, labelFileMask);
			TableRemoveRow (tableFindAndReplace, row, labelFileMask, searchentryFileMask, true);
			searchentryFileMask = null;
			labelFileMask = null;
		}

		void HandleScopeChanged (object sender, EventArgs e)
		{
			switch ((SearchScope)comboboxScope.Active) {
			case SearchScope.WholeWorkspace:
				HideDirectoryPathUI ();
				ShowFileMaskUI ();
				break;
			case SearchScope.CurrentProject:
				HideDirectoryPathUI ();
				ShowFileMaskUI ();
				break;
			case SearchScope.AllOpenFiles:
				HideDirectoryPathUI ();
				ShowFileMaskUI ();
				break;
			case SearchScope.Directories:
				ShowDirectoryPathUI ();
				ShowFileMaskUI ();
				break;
			case SearchScope.CurrentDocument:
				HideDirectoryPathUI ();
				HideFileMaskUI ();
				break;
			case SearchScope.Selection:
				HideDirectoryPathUI ();
				HideFileMaskUI ();
				break;
			}
			UpdateSensitivity ();
			Requisition req = SizeRequest ();
			Resize (req.Width, req.Height);
			//this.QueueResize ();
		}


		void UpdateSensitivity ()
		{
			bool isSensitive = true;
			switch ((SearchScope)comboboxScope.Active) {
			case SearchScope.WholeWorkspace:
				isSensitive = IdeApp.Workspace.IsOpen;
				break;
			case SearchScope.CurrentProject:
				isSensitive = IdeApp.ProjectOperations.CurrentSelectedProject != null;
				break;
			case SearchScope.AllOpenFiles:
				isSensitive = IdeApp.Workbench.Documents.Count > 0;
				break;
			case SearchScope.Directories:
				isSensitive = true;
				break;
			case SearchScope.CurrentDocument:
				isSensitive = IdeApp.Workbench.ActiveDocument != null;
				break;
			case SearchScope.Selection:
				isSensitive = IdeApp.Workbench.ActiveDocument != null;
				break;
			}
			buttonSearch.Sensitive = isSensitive;
			if (buttonReplace != null)
				buttonReplace.Sensitive = isSensitive;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = Math.Max (480, requisition.Width);
		}

		static void ComboboxentryPathDestroyed (object sender, EventArgs e)
		{
			StoreHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", (ComboBoxEntry)sender);
		}

		void ButtonBrowsePathsClicked (object sender, EventArgs e)
		{
			var dlg = new SelectFolderDialog (GettextCatalog.GetString ("Select directory")) {
				TransientFor = this,
			};
			
			string defaultFolder = comboboxentryPath.Entry.Text;
			if (string.IsNullOrEmpty (defaultFolder))
				defaultFolder = IdeApp.Preferences.ProjectsDefaultPath;
			if (!string.IsNullOrEmpty (defaultFolder))
				dlg.CurrentFolder = defaultFolder;
			
			if (dlg.Run ())
				comboboxentryPath.Entry.Text = dlg.SelectedFile;
		}

		void CheckbuttonRecursivelyDestroyed (object sender, EventArgs e)
		{
			properties.Set ("SearchPathRecursively", ((CheckButton)sender).Active);
		}

		const char historySeparator = '\n';
		void InitFromProperties ()
		{
			comboboxScope.Active = properties.Get ("Scope", (int) SearchScope.WholeWorkspace);
				
			//checkbuttonRecursively.Active    = properties.Get ("SearchPathRecursively", true);
			//checkbuttonFileMask.Active       = properties.Get ("UseFileMask", false);
			checkbuttonCaseSensitive.Active = properties.Get ("CaseSensitive", false);
			checkbuttonWholeWordsOnly.Active = properties.Get ("WholeWordsOnly", false);
			checkbuttonRegexSearch.Active = properties.Get ("RegexSearch", false);

			LoadHistory ("MonoDevelop.FindReplaceDialogs.FindHistory", comboboxentryFind);
			
//			LoadHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", comboboxentryPath);
//			LoadHistory ("MonoDevelop.FindReplaceDialogs.FileMaskHistory", comboboxentryFileMask);
		}

		static void LoadHistory (string propertyName, ComboBoxEntry entry)
		{
			var ec = new EntryCompletion ();
/*			entry.Changed += delegate {
				if (!entry.Entry.HasFocus)
					entry.Entry.GrabFocus ();

			};*/


			entry.Entry.Completion = ec;
			var store = new ListStore (typeof(string));
			entry.Entry.Completion.Model = store;
			entry.Model = store;
			entry.Entry.ActivatesDefault = true;
			entry.TextColumn = 0;
			var history = PropertyService.Get<string> (propertyName);
			if (!string.IsNullOrEmpty (history)) {
				string[] items = history.Split (historySeparator);
				foreach (string item in items) {
					if (string.IsNullOrEmpty (item))
						continue;
					store.AppendValues (item);
				}
				entry.Entry.Text = items[0];
			}
		}

		void StorePoperties ()
		{
			if (writeScope)
				properties.Set ("Scope", comboboxScope.Active);
//			properties.Set ("SearchPathRecursively", checkbuttonRecursively.Active);
//			properties.Set ("UseFileMask", checkbuttonFileMask.Active);
			properties.Set ("CaseSensitive", checkbuttonCaseSensitive.Active);
			properties.Set ("WholeWordsOnly", checkbuttonWholeWordsOnly.Active);
			properties.Set ("RegexSearch", checkbuttonRegexSearch.Active);

			StoreHistory ("MonoDevelop.FindReplaceDialogs.FindHistory", comboboxentryFind);
			if (replaceMode)
				StoreHistory ("MonoDevelop.FindReplaceDialogs.ReplaceHistory", comboboxentryReplace);
			if (searchentryFileMask != null)
				properties.Set ("MonoDevelop.FindReplaceDialogs.FileMask", searchentryFileMask.Query);
//			StoreHistory ("MonoDevelop.FindReplaceDialogs.PathHistory", comboboxentryPath);
			//StoreHistory ("MonoDevelop.FindReplaceDialogs.FileMaskHistory", comboboxentryFileMask);
		}

		static void StoreHistory (string propertyName, ComboBoxEntry comboBox)
		{
			var store = (ListStore)comboBox.Model;
			var history = new List<string> ();
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					history.Add ((string)store.GetValue (iter, 0));
				} while (store.IterNext (ref iter));
			}
			const int limit = 20;
			if (history.Count > limit) {
				history.RemoveRange (history.Count - (history.Count - limit), history.Count - limit);
			}
			if (history.Contains (comboBox.Entry.Text))
				history.Remove (comboBox.Entry.Text);
			history.Insert (0, comboBox.Entry.Text);
			PropertyService.Set (propertyName, string.Join (historySeparator.ToString (), history.ToArray ()));
		}

		protected override void OnDestroyed ()
		{
			if (resultPad != null) {
				var resultWidget = resultPad.Control.GetNativeWidget<SearchResultWidget> ();
				if (resultWidget.ResultCount > 0) {
					resultPad.Window.Activate (true);
				}
			}

			if (updateTimer != 0) {
				GLib.Source.Remove (updateTimer);
				updateTimer = 0;
			}
			StorePoperties ();
			base.OnDestroyed ();
		}
		
		public static void ShowFind ()
		{
			ShowSingleInstance (new FindInFilesDialog (false));
		}
		
		public static void ShowReplace ()
		{
			ShowSingleInstance (new FindInFilesDialog (true));
		}
		
		public static void FindInPath (string path)
		{
			ShowSingleInstance (new FindInFilesDialog (false, path));
		}
		
		static FindInFilesDialog currentFindDialog;
		
		static void ShowSingleInstance (FindInFilesDialog newDialog)
		{
			if (currentFindDialog != null) {
				currentFindDialog.Destroy ();
			}
			newDialog.Destroyed += (sender, e) => currentFindDialog = null;
			currentFindDialog = newDialog;
			MessageService.PlaceDialog (currentFindDialog, null);
			currentFindDialog.Present ();
		}

		Scope GetScope ()
		{
			Scope scope = null;

			switch ((SearchScope) comboboxScope.Active) {
			case SearchScope.CurrentDocument:
				scope = new DocumentScope ();
				break;
			case SearchScope.Selection:
				scope = new SelectionScope ();
				break;
			case SearchScope.WholeWorkspace:
				scope = new WholeSolutionScope ();
				break;
			case SearchScope.CurrentProject:
				var currentSelectedProject = IdeApp.ProjectOperations.CurrentSelectedProject;
				if (currentSelectedProject != null) {
					scope = new WholeProjectScope (currentSelectedProject);
					break;
				}
				return null;
			case SearchScope.AllOpenFiles:
				scope = new AllOpenFilesScope ();
				break;
			case SearchScope.Directories: 
				if (!System.IO.Directory.Exists (comboboxentryPath.Entry.Text)) {
					MessageService.ShowError (string.Format (GettextCatalog.GetString ("Directory not found: {0}"),
						comboboxentryPath.Entry.Text));
					return null;
				}
				
				scope = new DirectoryScope (comboboxentryPath.Entry.Text, checkbuttonRecursively.Active) {
					IncludeHiddenFiles = properties.Get ("IncludeHiddenFiles", false)
				};
				break;
			default:
				throw new ApplicationException ("Unknown scope:" + comboboxScope.Active);
			}
			
			scope.IncludeBinaryFiles = properties.Get ("IncludeBinaryFiles", false);
			return scope;
		}

		FilterOptions GetFilterOptions ()
		{
			return new FilterOptions {
				FileMask = searchentryFileMask != null && !string.IsNullOrEmpty (searchentryFileMask.Query) ? searchentryFileMask.Query : "*",
				CaseSensitive = checkbuttonCaseSensitive.Active,
				RegexSearch = checkbuttonRegexSearch.Active,
				WholeWordsOnly = checkbuttonWholeWordsOnly.Active
			};
		}

		static FindReplace find;
		void HandleReplaceClicked (object sender, EventArgs e)
		{
			SearchReplace (comboboxentryFind.Entry.Text, comboboxentryReplace.Entry.Text ?? "", GetScope (), GetFilterOptions (), () => UpdateStopButton (), UpdateResultPad);
		}

		void HandleSearchClicked (object sender, EventArgs e)
		{
			SearchReplace (comboboxentryFind.Entry.Text, null, GetScope (), GetFilterOptions (), () => UpdateStopButton (), UpdateResultPad);
		}

		static CancellationTokenSource searchTokenSource = new CancellationTokenSource ();
		static Task currentTask;
		uint updateTimer;
		SearchResultPad resultPad;

		void UpdateStopButton ()
		{
			buttonStop.Sensitive = currentTask != null && !currentTask.IsCompleted;
		}

		void UpdateResultPad (SearchResultPad pad)
		{
			resultPad = pad;
		}

		void ButtonStopClicked (object sender, EventArgs e)
		{
			searchTokenSource.Cancel ();
		}

		internal static void SearchReplace (string findPattern, string replacePattern, Scope scope, FilterOptions options, System.Action UpdateStopButton, System.Action<SearchResultPad> UpdateResultPad)
		{
			if (find != null && find.IsRunning) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("There is a search already in progress. Do you want to stop it?"), AlertButton.Stop))
					return;
			}
			searchTokenSource.Cancel ();

			if (scope == null)
				return;
			
			find = new FindReplace ();

			string pattern = findPattern;
			if (String.IsNullOrEmpty (pattern))
				return;
			if (!find.ValidatePattern (options, pattern)) {
				MessageService.ShowError (GettextCatalog.GetString ("Search pattern is invalid"));
				return;
			}

			if (replacePattern != null && !find.ValidatePattern (options, replacePattern)) {
				MessageService.ShowError (GettextCatalog.GetString ("Replace pattern is invalid"));
				return;
			}
			var cancelSource = new CancellationTokenSource ();
			searchTokenSource = cancelSource;
			var token = cancelSource.Token;
			currentTask = Task.Run (delegate {
				using (SearchProgressMonitor searchMonitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true)) {

					searchMonitor.PathMode = scope.PathMode;

					if (UpdateResultPad != null) {
						Application.Invoke ((o, args) => {
							UpdateResultPad (searchMonitor.ResultPad);
						});
					}

					searchMonitor.ReportStatus (scope.GetDescription (options, pattern, null));
					if (UpdateStopButton != null) {
						Application.Invoke ((o, args) => {
							UpdateStopButton ();
						});
					}

					DateTime timer = DateTime.Now;
					string errorMessage = null;
						
					try {
						var results = new List<SearchResult> ();
						foreach (SearchResult result in find.FindAll (scope, searchMonitor, pattern, replacePattern, options, token)) {
							if (token.IsCancellationRequested)
								return;
							results.Add (result);
						}
						searchMonitor.ReportResults (results);
					} catch (Exception ex) {
						errorMessage = ex.Message;
						LoggingService.LogError ("Error while search", ex);
					}
						
					string message = null;
					if (errorMessage != null) {
						message = GettextCatalog.GetString ("The search could not be finished: {0}", errorMessage);
						searchMonitor.ReportError (message, null);
					} else if (!searchMonitor.CancellationToken.IsCancellationRequested) {
						string matches = string.Format (GettextCatalog.GetPluralString ("{0} match found", "{0} matches found", find.FoundMatchesCount), find.FoundMatchesCount);
						string files = string.Format (GettextCatalog.GetPluralString ("in {0} file.", "in {0} files.", find.SearchedFilesCount), find.SearchedFilesCount);
						message = GettextCatalog.GetString ("Search completed.") + Environment.NewLine + matches + " " + files;
						searchMonitor.ReportSuccess (message);
					}
					if (message != null)
						searchMonitor.ReportStatus (message);
					searchMonitor.Log.WriteLine (GettextCatalog.GetString ("Search time: {0} seconds."), (DateTime.Now - timer).TotalSeconds);
				}
				if (UpdateStopButton != null) {
					Application.Invoke ((o, args) => {
						UpdateStopButton ();
					});
				}
			});
		}
	}
}
