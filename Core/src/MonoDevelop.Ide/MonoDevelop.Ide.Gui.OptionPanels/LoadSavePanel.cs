// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;

using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;

using Gtk;
using MonoDevelop.Components;

#pragma warning disable 612

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	
	internal class LoadSavePanel : AbstractOptionPanel
	{
		//FIXME: Add mneumonics for Window, Macintosh and Unix Radio Buttons. 
		//       Remove mneumonic from terminator label.

		LoadSavePanelWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new LoadSavePanelWidget ());
		}
		
		public override bool StorePanelContents()
		{			
			bool succes = widget.Store();
			return succes;
		}
	}

	partial class LoadSavePanelWidget : Gtk.Bin
	{
		public LoadSavePanelWidget ()
		{
			Build ();
			
			//
			// load the internationalized strings.
			//
			folderEntry.Path = Runtime.Properties.GetProperty(
				"MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", 
				System.IO.Path.Combine(System.Environment.GetEnvironmentVariable ("HOME"),
						"Projects")).ToString();
			//
			// setup the properties
			//
			loadUserDataCheckButton.Active = Runtime.Properties.GetProperty (
				"SharpDevelop.LoadDocumentProperties", true);
			createBackupCopyCheckButton.Active = Runtime.Properties.GetProperty (
				"SharpDevelop.CreateBackupCopy", false);
			loadPrevProjectCheckButton.Active = (bool) Runtime.Properties.GetProperty(
				"SharpDevelop.LoadPrevProjectOnStartup", false);
		}
		
		public bool Store () 
		{
			Runtime.Properties.SetProperty("SharpDevelop.LoadPrevProjectOnStartup", loadPrevProjectCheckButton.Active);
			Runtime.Properties.SetProperty ("SharpDevelop.LoadDocumentProperties",  loadUserDataCheckButton.Active);
			Runtime.Properties.SetProperty ("SharpDevelop.CreateBackupCopy",        createBackupCopyCheckButton.Active);
			
			// check for correct settings
			string projectPath = folderEntry.Path;
			if (projectPath.Length > 0) {
				if (!Runtime.FileService.IsValidFileName(projectPath)) {
					Services.MessageService.ShowError("Invalid project path specified");
					return false;
				}
			}
			Runtime.Properties.SetProperty("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", projectPath);
			
			return true;
		}
	}
}

#pragma warning restore 612
