//
// ExternalToolPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Gtk;

using MonoDevelop.Components;
using MonoDevelop.Ide.ExternalTools;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;

#pragma warning disable 612

namespace MonoDevelop.Ide.ExternalTools
{
	internal class ExternalToolPane : OptionsPanel
	{
		ExternalToolPanelWidget widget;

		public override Control CreatePanelWidget ()
		{
			return widget = new ExternalToolPanelWidget ();
		}
		
		public override bool ValidateChanges ()
		{
			return widget.Validate ();
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
	
	public partial class ExternalToolPanelWidget : Gtk.Bin 
	{
		// gtk controls
		ListStore toolListBoxStore;
		
		// these are the control names which are enabled/disabled depending if tool is selected
		Widget[] dependendControls;
		 
		// needed for treeview listbox
		int toolListBoxItemCount = 0;
		bool lockStoreValues = false;

		public ExternalToolPanelWidget () 
		{
			Build ();
			// instantiate controls			
			toolListBoxStore = new ListStore (typeof (string), typeof (ExternalTool));

			dependendControls = new Widget[] {
				titleTextBox, argumentTextBox, 
				workingDirTextBox, promptArgsCheckBox, useOutputPadCheckBox, 
				titleLabel, argumentLabel, commandLabel, 
				workingDirLabel, browseButton, 
				moveUpButton, moveDownButton,
				saveCurrentFileCheckBox
			};
			 
			foreach (ExternalTool tool in ExternalToolService.Tools) {
				toolListBoxStore.AppendValues (tool.MenuCommand, tool);
				toolListBoxItemCount ++;
			}

			toolListBox.Reorderable = false;
			toolListBox.HeadersVisible = true;
			toolListBox.Selection.Mode = SelectionMode.Multiple;
			toolListBox.Model = toolListBoxStore;
			toolListBox.SearchColumn = -1; // disable the interactive search

			toolListBox.AppendColumn (GettextCatalog.GetString ("Tools"), new CellRendererText (), "text", 0);

			tagSelectorArgs.TagModel = IdeApp.Workbench.GetStringTagModelDescription ();
			tagSelectorArgs.TargetEntry = argumentTextBox;
			
			tagSelectorPath.TagModel = IdeApp.Workbench.GetStringTagModelDescription ();
			tagSelectorPath.TargetEntry = workingDirTextBox;

			toolListBox.Selection.Changed += SelectionChanged;
			removeButton.Clicked          += RemoveButtonClicked;
			addButton.Clicked             += AddButtonClicked;
			moveUpButton.Clicked          += MoveUpButtonClicked;
			moveDownButton.Clicked        += MoveDownButtonClicked;
			
			browseButton.PathChanged        += StoreValuesInSelectedTool;
			titleTextBox.Changed            += StoreValuesInSelectedTool;
			argumentTextBox.Changed         += StoreValuesInSelectedTool;
			workingDirTextBox.Changed       += StoreValuesInSelectedTool;
			promptArgsCheckBox.Toggled      += StoreValuesInSelectedTool;
			useOutputPadCheckBox.Toggled    += StoreValuesInSelectedTool;
			saveCurrentFileCheckBox.Toggled += StoreValuesInSelectedTool;
			
			SelectionChanged (this, EventArgs.Empty);
		}
		
		void MoveUpButtonClicked (object sender, EventArgs e)
		{
			if (toolListBox.Selection.CountSelectedRows () == 1) {
				TreeIter selectedItem;
				TreeModel ls;				
				((ListStore)toolListBox.Model).GetIter (out selectedItem, (TreePath)toolListBox.Selection.GetSelectedRows (out ls)[0]);
				// we know we have a selected item so get it's index
				// use that to get the path of the item before it, and swap the two
				int index = GetSelectedIndex (toolListBox);
				// only swap if at the top
				if (index > 0) {
					TreeIter prev; 
					if (toolListBox.Model.GetIterFromString (out prev, (index - 1).ToString ()))
						((ListStore)ls).Swap (selectedItem, prev);
				}
			}
		}
			 
		void MoveDownButtonClicked (object sender, EventArgs e)
		{
			if (toolListBox.Selection.CountSelectedRows () == 1) {
				TreeIter selectedItem;
				TreeModel ls;				
				((ListStore)toolListBox.Model).GetIter (out selectedItem, (TreePath) toolListBox.Selection.GetSelectedRows(out ls)[0]);
				// swap it with the next one
				TreeIter toSwap = selectedItem;
				if (ls.IterNext (ref toSwap))
					((ListStore)ls).Swap (selectedItem, toSwap);
			}
		}
		
		void StoreValuesInSelectedTool (object sender, EventArgs e)
		{
			if (lockStoreValues) 
				return;
			ExternalTool selectedItem = SelectedTool;
			if (selectedItem == null) 
				return;
			
			toolListBoxStore.SetValue (SelectedIter, 0, titleTextBox.Text);
			selectedItem.MenuCommand        = titleTextBox.Text;
			selectedItem.Command            = browseButton.Path;
			selectedItem.Arguments          = argumentTextBox.Text;
			selectedItem.InitialDirectory   = workingDirTextBox.Text;
			selectedItem.PromptForArguments = promptArgsCheckBox.Active;
			selectedItem.UseOutputPad       = useOutputPadCheckBox.Active;
			selectedItem.SaveCurrentFile    = saveCurrentFileCheckBox.Active;
		}
		
		TreeIter SelectedIter {
			get {
				if (toolListBox.Selection.CountSelectedRows () == 1) {
					TreeIter selectedIter;
					TreeModel ls;
					((ListStore) toolListBox.Model).GetIter (out selectedIter, (TreePath) toolListBox.Selection.GetSelectedRows (out ls)[0]);
					return selectedIter;
				}
				return TreeIter.Zero;
			}
		}
		ExternalTool SelectedTool {
			get {
				if (toolListBox.Selection.CountSelectedRows () == 1) {
					TreeIter selectedIter;
					TreeModel ls;
					((ListStore) toolListBox.Model).GetIter (out selectedIter, (TreePath) toolListBox.Selection.GetSelectedRows (out ls)[0]);
					return toolListBox.Model.GetValue(selectedIter, 1) as ExternalTool;
				}
				return null;
			}
		}
		
		void DisplayTool (ExternalTool externalTool)
		{
			SetEnabledStatus (externalTool != null, dependendControls);
			lockStoreValues = true;
			try {
				if (externalTool != null) {
					titleTextBox.Text              = externalTool.MenuCommand ?? "";
					browseButton.Path              = externalTool.Command ?? "";
					argumentTextBox.Text           = externalTool.Arguments ?? "";
					workingDirTextBox.Text         = externalTool.InitialDirectory ?? "";
					promptArgsCheckBox.Active      = externalTool.PromptForArguments ;
					useOutputPadCheckBox.Active    = externalTool.UseOutputPad;
					saveCurrentFileCheckBox.Active = externalTool.SaveCurrentFile;
				} else {
					titleTextBox.Text = browseButton.Path = argumentTextBox.Text = workingDirTextBox.Text = "";
					promptArgsCheckBox.Active = useOutputPadCheckBox.Active = saveCurrentFileCheckBox.Active = false;
				}
			} finally {
				lockStoreValues = false;
			}
		}
	
		void SelectionChanged (object sender, EventArgs e)
		{
			SetEnabledStatus (toolListBox.Selection.CountSelectedRows () > 0, removeButton);
			DisplayTool (SelectedTool);
		}
		 
		void RemoveButtonClicked (object sender, EventArgs e)
		{
			int selectedItemCount = toolListBox.Selection.CountSelectedRows ();
			if (selectedItemCount > 0) {
				int maxIndex = 0;
				// first copy the selected item paths into a temp array
				TreeIter[] selectedIters = new TreeIter[selectedItemCount];
				TreeModel lv;
				TreePath[] pathList = toolListBox.Selection.GetSelectedRows (out lv);
				for (int i = 0; i < selectedItemCount; i++) {
					TreePath path = (TreePath) pathList[i];
					((ListStore)lv).GetIter (out selectedIters[i], path);
					maxIndex = path.Indices[0];
				}
				 
				// now delete each item in that list
				foreach (TreeIter toDelete in selectedIters) {
					TreeIter itr = toDelete;
					toolListBoxItemCount--;
					((ListStore)lv).Remove (ref itr);
				}
				 
				if (toolListBoxItemCount == 0) {
					SelectionChanged (this, EventArgs.Empty);
				} else {
					SetSelectedIndex (toolListBox, Math.Min(maxIndex,toolListBoxItemCount - 1));
				}
			}
		}
		 
		void AddButtonClicked (object sender, EventArgs e)
		{
			TreeIter itr = toolListBoxStore.AppendValues (GettextCatalog.GetString ("New Tool"), new ExternalTool());
			toolListBoxItemCount ++;
			toolListBox.Selection.UnselectAll ();
			toolListBox.Selection.SelectIter (itr);
		}
		 
		// added this event to get the last select row index from gtk TreeView
		int GetSelectedIndex (Gtk.TreeView tv)
		{
			if (toolListBox.Selection.CountSelectedRows () == 1) {
				TreeIter selectedIter;
				TreeModel lv;
				((ListStore) toolListBox.Model).GetIter(out selectedIter, (TreePath) toolListBox.Selection.GetSelectedRows (out lv)[0]);
				
				// return index of first level node (since only 1 level list model)
				return lv.GetPath (selectedIter).Indices[0];
			}
			return -1;
		}
		 
		// added this event to set a specific row as selected from index
		void SetSelectedIndex (Gtk.TreeView tv, int index)
		{
			//convert index to a path
			TreePath path = new TreePath (index.ToString ());
			tv.Selection.UnselectAll ();
			tv.Selection.SelectPath (path);
		}
	 
		// disables or enables (sets sensitivty) a specified array of widgets
		public void SetEnabledStatus (bool enabled, params Widget[] controls)
		{
			foreach (Widget control in controls) {
				if (control != null) {
					control.Sensitive = enabled;
				} else {
					MessageService.ShowError (GettextCatalog.GetString ("Control not found!"));
				}
			}
		}
		
		static string FilterPath (string path)
		{
			return StringParserService.Parse (path);
		}

		public bool Validate ()
		{
			TreeIter first;
			if (toolListBox.Model.GetIterFirst (out first)) {
				TreeIter current = first;
				do {
					// loop through items in the tree
					ExternalTool tool = toolListBox.Model.GetValue (current, 1) as ExternalTool;
					if (tool == null) {
						continue;
					}

					if (String.IsNullOrEmpty (tool.Command)) {
						MessageService.ShowError (String.Format (GettextCatalog.GetString ("The command of tool \"{0}\" is not set."), tool.MenuCommand));
						return false;
					}

					string path = FilterPath (tool.Command);
					if (!FileService.IsValidPath (path)) {
						MessageService.ShowError (String.Format (GettextCatalog.GetString ("The command of tool \"{0}\" is invalid."), tool.MenuCommand));
						return false;
					}
					path = FilterPath (tool.InitialDirectory);
					if ((tool.InitialDirectory != "") && !FileService.IsValidPath (path)) {
						MessageService.ShowError (String.Format (GettextCatalog.GetString ("The working directory of tool \"{0}\" is invalid.") ,tool.MenuCommand));
						return false;
					}
				} while (toolListBox.Model.IterNext (ref current));
			}
			return true;
		}
		
		public bool Store ()
		{
			List<ExternalTool> newlist = new List<ExternalTool> ();
			TreeIter first;
			if (toolListBox.Model.GetIterFirst (out first)) {
				TreeIter current = first;
				do {
					// loop through items in the tree
					ExternalTool tool = toolListBox.Model.GetValue (current, 1) as ExternalTool;
					newlist.Add (tool);
				} while (toolListBox.Model.IterNext (ref current));
			}
			
			ExternalToolService.Tools = newlist;
			ExternalToolService.SaveTools ();
			return true;
		}
	}
}

#pragma warning restore 612
