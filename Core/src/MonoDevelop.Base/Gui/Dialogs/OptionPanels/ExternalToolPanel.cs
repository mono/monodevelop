 // <file>
 //     <copyright see="prj:///doc/copyright.txt"/>
 //     <license see="prj:///doc/license.txt"/>
 //     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
 //     <version value="$version"/>
 // </file>

using System;
using System.IO;
using System.Collections;
using Gtk;
using Gnome;
using MonoDevelop.Gui.Widgets;

using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui.Components;

namespace MonoDevelop.Gui.Dialogs.OptionPanels
{
	internal class ExternalToolPane: AbstractOptionPanel
	{
		ExternalToolPanelWidget widget;

		public override void LoadPanelContents ()
		{
			widget = new ExternalToolPanelWidget ();
			Add (widget);
		}

		public override bool StorePanelContents ()
		{
			bool result = true;
			result = widget.Store ();
			return result;
		}

		public class ExternalToolPanelWidget :  GladeWidgetExtract 
		{
			static string[,] argumentQuickInsertMenu = new string[,] {
				{GettextCatalog.GetString ("Item Path"), "${ItemPath}"},
				{GettextCatalog.GetString ("_Item Directory"), "${ItemDir}"},
				{GettextCatalog.GetString ("Item file name"), "${ItemFileName}"},
				{GettextCatalog.GetString ("Item extension"), "${ItemExt}"},
				{"-", ""},
				{GettextCatalog.GetString ("Current line"), "${CurLine}"},
				{GettextCatalog.GetString ("Current column"), "${CurCol}"},
				{GettextCatalog.GetString ("Current text"), "${CurText}"},
				{"-", ""},
				{GettextCatalog.GetString ("Target Path"), "${TargetPath}"},
				{GettextCatalog.GetString ("_Target Directory"), "${TargetDir}"},
				{GettextCatalog.GetString ("Target Name"), "${TargetName}"},
				{GettextCatalog.GetString ("Target Extension"), "${TargetExt}"},
				{"-", ""},
				{GettextCatalog.GetString ("_Project Directory"), "${ProjectDir}"},
				{GettextCatalog.GetString ("Project file name"), "${ProjectFileName}"},
				{"-", ""},
				{GettextCatalog.GetString ("_Solution Directory"), "${CombineDir}"},
				{GettextCatalog.GetString ("Solution File Name"), "${CombineFileName}"},
				{"-", ""},
				{GettextCatalog.GetString ("MonoDevelop Startup Directory"), "${StartupPath}"},
			};

			static string[,] workingDirInsertMenu = new string[,] {
				{GettextCatalog.GetString ("_Item Directory"), "${ItemDir}"},
				{"-", ""},
				{GettextCatalog.GetString ("_Target Directory"), "${TargetDir}"},
				{GettextCatalog.GetString ("Target Name"), "${TargetName}"},
				{"-", ""},
				{GettextCatalog.GetString ("_Project Directory"), "${ProjectDir}"},
				{"-", ""},
				{GettextCatalog.GetString ("_Solution Directory"), "${CombineDir}"},
				{"-", ""},
				{GettextCatalog.GetString ("MonoDevelop Startup Directory"), "${StartupPath}"},
			};
			 
			// gtk controls
			[Glade.Widget] ListStore toolListBoxStore;
			[Glade.Widget] Gtk.TreeView toolListBox;
			[Glade.Widget] Gtk.Entry titleTextBox; 
			[Glade.Widget] Gtk.Entry argumentTextBox; 
			[Glade.Widget] Gtk.Entry workingDirTextBox; 
			[Glade.Widget] CheckButton promptArgsCheckBox; 
			[Glade.Widget] CheckButton useOutputPadCheckBox; 
			[Glade.Widget] Label titleLabel; 
			[Glade.Widget] Label argumentLabel; 
			[Glade.Widget] Label commandLabel; 
			[Glade.Widget] Label workingDirLabel; 
 			[Glade.Widget] Gnome.FileEntry browseButton; 
 			[Glade.Widget] Button argumentQuickInsertButton; 
			[Glade.Widget] Button workingDirQuickInsertButton; 
 			[Glade.Widget] Button moveUpButton; 
 			[Glade.Widget] Button moveDownButton;
 			[Glade.Widget] Button addButton; 
			[Glade.Widget] Button removeButton;
			
			MenuButtonEntry argumentMbe;
			MenuButtonEntry workingDirMbe;
			 
			// these are the control names which are enabled/disabled depending if tool is selected
			Widget[] dependendControls;
			 
			// needed for treeview listbox
			int toolListBoxItemCount = 0;

			public ExternalToolPanelWidget () : base ("Base.glade", "ExternalToolPanel") 
			{
				// instantiate controls			
				toolListBoxStore = new ListStore (typeof (string), typeof (ExternalTool));

				dependendControls = new Widget[] {
					titleTextBox, argumentTextBox, 
					workingDirTextBox, promptArgsCheckBox, useOutputPadCheckBox, 
					titleLabel, argumentLabel, commandLabel, 
					workingDirLabel, browseButton, argumentQuickInsertButton, 
					workingDirQuickInsertButton, moveUpButton, moveDownButton};
				 
				foreach (object o in ToolLoader.Tool) {
					toolListBoxStore.AppendValues (((ExternalTool) o).MenuCommand, (ExternalTool) o);
					toolListBoxItemCount ++;
				}

				toolListBox.Reorderable = false;
				toolListBox.HeadersVisible = true;
				toolListBox.Selection.Mode = SelectionMode.Multiple;
				toolListBox.Model = toolListBoxStore;
					 
				toolListBox.AppendColumn (GettextCatalog.GetString ("_Tools"), new CellRendererText (), "text", 0);

				argumentMbe = new MenuButtonEntry (argumentTextBox, argumentQuickInsertButton, argumentQuickInsertMenu);
				workingDirMbe = new MenuButtonEntry (workingDirTextBox, workingDirQuickInsertButton, workingDirInsertMenu);

				toolListBox.Selection.Changed += new EventHandler (selectEvent);
				removeButton.Clicked += new EventHandler (removeEvent);
				addButton.Clicked += new EventHandler (addEvent);
				moveUpButton.Clicked += new EventHandler (moveUpEvent);
				moveDownButton.Clicked += new EventHandler (moveDownEvent);
				
				selectEvent (this, EventArgs.Empty);
			}
			
			void moveUpEvent (object sender, EventArgs e)
			{
				if(toolListBox.Selection.CountSelectedRows () == 1)
				{
					TreeIter selectedItem;
					TreeModel ls;				
					((ListStore) toolListBox.Model).GetIter (
						out selectedItem, (TreePath) toolListBox.Selection.GetSelectedRows(out ls)[0]);
					// we know we have a selected item so get it's index
					// use that to get the path of the item before it, and swap the two
					int index = GetSelectedIndex (toolListBox);
					// only swap if at the top
					if (index > 0)
					{
						TreeIter prev; 
						if(toolListBox.Model.GetIterFromString (out prev, (index - 1).ToString ()))
						{
							((ListStore) ls).Swap (selectedItem, prev);
						}
					}
				}
			}
				 
			void moveDownEvent (object sender, EventArgs e)
			{
				if(toolListBox.Selection.CountSelectedRows () == 1)
				{
					TreeIter selectedItem;
					TreeModel ls;				
					((ListStore) toolListBox.Model).GetIter (
						out selectedItem, (TreePath) toolListBox.Selection.GetSelectedRows(out ls)[0]);
					// swap it with the next one
					TreeIter toSwap = selectedItem;
					if (ls.IterNext (ref toSwap))
					{
						((ListStore) ls).Swap (selectedItem, toSwap);
					}
				}
			}
				 
			void setToolValues (object sender, EventArgs e)
			{
				ExternalTool selectedItem = null;
				if (toolListBox.Selection.CountSelectedRows () == 1)
				{
					TreeIter selectedIter;
					TreeModel lv;				
					((ListStore) toolListBox.Model).GetIter (
						out selectedIter, (TreePath) toolListBox.Selection.GetSelectedRows (out lv)[0]);
						 
					// get the value as an external tool object
					selectedItem = lv.GetValue (selectedIter, 1) as ExternalTool;
						 	 
					lv.SetValue (selectedIter, 0, titleTextBox.Text);
					selectedItem.MenuCommand = titleTextBox.Text;
					selectedItem.Command = browseButton.Filename;
					selectedItem.Arguments = argumentTextBox.Text;
					selectedItem.InitialDirectory = workingDirTextBox.Text;
					selectedItem.PromptForArguments = promptArgsCheckBox.Active;
					selectedItem.UseOutputPad = useOutputPadCheckBox.Active;
				}
			}
				 
			void selectEvent (object sender, EventArgs e)
			{
				SetEnabledStatus (toolListBox.Selection.CountSelectedRows () > 0, removeButton);
					 
				titleTextBox.Changed -= new EventHandler (setToolValues);
				argumentTextBox.Changed -= new EventHandler (setToolValues);
				workingDirTextBox.Changed -= new EventHandler (setToolValues);
				promptArgsCheckBox.Toggled -= new EventHandler (setToolValues);
				useOutputPadCheckBox.Toggled -= new EventHandler (setToolValues);
				 
				if (toolListBox.Selection.CountSelectedRows () == 1) {				
					TreeIter selectedIter;
					TreeModel ls;
					((ListStore) toolListBox.Model).GetIter (
						out selectedIter, (TreePath) toolListBox.Selection.GetSelectedRows (out ls)[0]);

					// get the value as an external tool object				
					ExternalTool selectedItem = (ExternalTool) toolListBox.Model.GetValue(selectedIter, 1);
					 
					SetEnabledStatus (true, dependendControls);
					titleTextBox.Text = selectedItem.MenuCommand;
					browseButton.Filename = selectedItem.Command;
					argumentTextBox.Text = selectedItem.Arguments;
					workingDirTextBox.Text = selectedItem.InitialDirectory;
					promptArgsCheckBox.Active = selectedItem.PromptForArguments;
					useOutputPadCheckBox.Active = selectedItem.UseOutputPad;
				} else {
					SetEnabledStatus (false, dependendControls);
					titleTextBox.Text = String.Empty;
					browseButton.Filename = String.Empty;
					argumentTextBox.Text = String.Empty;
					workingDirTextBox.Text = String.Empty;
					promptArgsCheckBox.Active = false;
					useOutputPadCheckBox.Active = false;
				}
				 
				titleTextBox.Changed += new EventHandler (setToolValues);
				argumentTextBox.Changed += new EventHandler (setToolValues);
				workingDirTextBox.Changed += new EventHandler (setToolValues);
				promptArgsCheckBox.Toggled += new EventHandler (setToolValues);
				useOutputPadCheckBox.Toggled += new EventHandler (setToolValues);
			}
			 
			void removeEvent (object sender, EventArgs e)
			{
				int selectedItemCount = toolListBox.Selection.CountSelectedRows();
				if(selectedItemCount > 0) {
					int maxIndex = 0;
					// first copy the selected item paths into a temp array
					TreeIter[] selectedIters = new TreeIter[selectedItemCount];
					TreeModel lv;
					TreePath[] pathList = toolListBox.Selection.GetSelectedRows(out lv);								
					for(int i = 0; i < selectedItemCount; i++) {
						TreePath path = (TreePath) pathList[i];
						((ListStore)lv).GetIter(out selectedIters[i], path);
						maxIndex = path.Indices[0];
					}
					 
					// now delete each item in that list
					foreach(TreeIter toDelete in selectedIters) {
						TreeIter itr = toDelete;
						toolListBoxItemCount --;
						((ListStore)lv).Remove(ref itr);
					}
					 
					if (toolListBoxItemCount == 0) {
						selectEvent(this, EventArgs.Empty);
					} else {
						SetSelectedIndex(toolListBox, Math.Min(maxIndex,toolListBoxItemCount - 1));
					}
				}
			}
			 
			void addEvent (object sender, EventArgs e)
			{
				TreeIter itr = toolListBoxStore.AppendValues (GettextCatalog.GetString ("New Tool"), new ExternalTool());
				toolListBoxItemCount ++;
				toolListBox.Selection.UnselectAll ();
				toolListBox.Selection.SelectIter (itr);
			}
			 
			// added this event to get the last select row index from gtk TreeView
			int GetSelectedIndex (Gtk.TreeView tv)
			{
				if (toolListBox.Selection.CountSelectedRows () == 1)
				{
					TreeIter selectedIter;
					TreeModel lv;				
					((ListStore) toolListBox.Model).GetIter(
						out selectedIter, (TreePath) toolListBox.Selection.GetSelectedRows(out lv)[0]);
					 
					// return index of first level node (since only 1 level list model)
					return lv.GetPath (selectedIter).Indices[0];
				}
				else
				{
					return -1;
				}
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
			public void SetEnabledStatus(bool enabled, params Widget[] controls)
			{
				foreach (Widget control in controls) {				
					if (control == null) {
						Runtime.MessageService.ShowError (GettextCatalog.GetString ("Control not found!"));
					} else {
						control.Sensitive = enabled;
					}
				}
			}

			public bool Store ()
			{
				ArrayList newlist = new ArrayList ();
				TreeIter first;
				if (toolListBox.Model.GetIterFirst (out first))
				{
					TreeIter current = first;
					 
					do {
						// loop through items in the tree
						 
					ExternalTool tool = toolListBox.Model.GetValue (current, 1) as ExternalTool;
					
					if (!Runtime.FileUtilityService.IsValidFileName (tool.Command)) {
						Runtime.MessageService.ShowError (String.Format(GettextCatalog.GetString ("The command of tool \"{0}\" is invalid."), 
										 tool.MenuCommand));
						return false;
					}
					
					if ((tool.InitialDirectory != "") && (!Runtime.FileUtilityService.IsValidFileName(tool.InitialDirectory))) {
						Runtime.MessageService.ShowError (String.Format(GettextCatalog.GetString ("The working directory of tool \"{0}\" is invalid.") ,
											 tool.MenuCommand));
						return false;
					}
					
					newlist.Add (tool);				 
					
					}
					while (toolListBox.Model.IterNext (ref current));
				}
				
				ToolLoader.Tool = newlist;
				ToolLoader.SaveTools ();
				return true;
			}
		}
	}
}
