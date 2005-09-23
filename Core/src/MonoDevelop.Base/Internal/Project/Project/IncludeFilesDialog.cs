// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui;
using MonoDevelop.Internal.Project;

using Gtk;

namespace MonoDevelop.Internal.Project
{
	public class IncludeFilesDialog
	{
		// gtk widgets
		[Glade.Widget] Button okbutton;
		[Glade.Widget] Button cancelbutton;
		[Glade.Widget] Button selectAllButton;
		[Glade.Widget] Button deselectAllButton;
		[Glade.Widget] RadioButton newFilesOnlyRadioButton;
		[Glade.Widget] RadioButton allFilesRadioButton;
//		[Glade.Widget] Label newFilesInProjectLabel;
//		[Glade.Widget] Label viewLabel;
		[Glade.Widget] TreeView IncludeFileListView;
		[Glade.Widget] Dialog IncludeFilesDialogWidget;
		public ListStore store;
		
		// regular members
		StringCollection newFiles;
		Project         project;
		FileUtilityService fileUtilityService = Runtime.FileUtilityService;
		
		public IncludeFilesDialog(Project project, StringCollection newFiles)
		{
			Runtime.LoggingService.Info ("*** Include files dialog ***");
			// we must do it from *here* otherwise, we get this assembly, not the caller
			Glade.XML glade = new Glade.XML (null, "Base.glade", "IncludeFilesDialogWidget", null);
			glade.Autoconnect (this);
			
			// set up dialog title
			this.IncludeFilesDialogWidget.Title = String.Format (GettextCatalog.GetString ("Found new files in {0}"), project.Name);
			
			newFilesOnlyRadioButton.Active = true;
			this.newFiles = newFiles;
			this.project  = project;
			
			this.InitialiseIncludeFileList();
			
			// wire in event handlers
			okbutton.Clicked += new EventHandler(AcceptEvent);
			cancelbutton.Clicked += new EventHandler(CancelEvent);
			selectAllButton.Clicked += new EventHandler(SelectAll);
			deselectAllButton.Clicked += new EventHandler(DeselectAll);

			// FIXME: I'm pretty sure that these radio buttons 
			// don't actually work in SD 0.98 either, so disabling them
			newFilesOnlyRadioButton.Sensitive = false;
			allFilesRadioButton.Sensitive = false;
		}
		
		#region includeFileListView methods and events 
		
		// initialises and populates the include file list tree view
		private void InitialiseIncludeFileList()
		{
			// set up the list store and treeview
			store = new ListStore (typeof(bool), typeof(string));
			IncludeFileListView.Selection.Mode = SelectionMode.None;
			IncludeFileListView.Model = store;
			CellRendererToggle rendererToggle = new CellRendererToggle ();
			rendererToggle.Activatable = true;
			rendererToggle.Toggled += new ToggledHandler (ItemToggled);
			IncludeFileListView.AppendColumn ("Choosen", rendererToggle, "active", 0);
			IncludeFileListView.AppendColumn ("Name", new CellRendererText (), "text", 1);
			
			// add the found files to the check list box						
			foreach (string file in newFiles) {
				string name = fileUtilityService.AbsoluteToRelativePath(project.BaseDirectory, file);
				store.AppendValues (false, name);
			}
		}
		
		private void ItemToggled (object o, ToggledArgs args)
		{
			const int column = 0;
			Gtk.TreeIter iter;
			
			if (store.GetIterFromString(out iter, args.Path)) {
				bool val = (bool) store.GetValue(iter, column);
				store.SetValue(iter, column, !val);
			}
		}
		
		#endregion
		
		void AcceptEvent(object sender, EventArgs e)
		{
			TreeIter first;	
			store.GetIterFirst(out first);
			TreeIter current = first;
 			for (int i = 0; i < store.IterNChildren() ; ++i) {
				// get column raw values
				bool isSelected = (bool) store.GetValue(current, 0);
				string fileName = (string) store.GetValue(current, 1);
			
				// process raw values into actual project details
				string file = fileUtilityService.RelativeToAbsolutePath(project.BaseDirectory,fileName);
				ProjectFile finfo = new ProjectFile(file);
				if (isSelected) {
					finfo.BuildAction = project.IsCompileable(file) ? BuildAction.Compile : BuildAction.Nothing;
				} else {
					finfo.BuildAction = BuildAction.Exclude;
				}
				project.ProjectFiles.Add(finfo);
				
				store.IterNext(ref current);
			}
			
			Runtime.ProjectService.SaveCombine ();
			
			IncludeFilesDialogWidget.Destroy();
		}
		
		void CancelEvent(object sender, EventArgs e)
		{
			IncludeFilesDialogWidget.Destroy();
		}
		
		void SelectAll(object sender, EventArgs e)
		{
			SetAllCheckedValues(true);
		}
		
		void DeselectAll(object sender, EventArgs e)
		{
			SetAllCheckedValues(false);
		}
		
		private void SetAllCheckedValues(bool value)
		{
			TreeIter first;	
			store.GetIterFirst(out first);
			TreeIter current = first;
 			for (int i = 0; i < store.IterNChildren() ; ++i) {
				store.SetValue(current, 0, value);
				
				store.IterNext(ref current);
			}
		}
		
		public void ShowDialog()
		{
			this.IncludeFilesDialogWidget.Show ();
		}

	}
}

