// LoadSavePanel.cs
//  
// Author:
//       Todd Berman <tberman@sevenl.net>
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2004 Todd Berman
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
using System.IO;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using Gtk;
using MonoDevelop.Components;

#pragma warning disable 612

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	
	internal class LoadSavePanel : OptionsPanel
	{
		//FIXME: Add mneumonics for Window, Macintosh and Unix Radio Buttons. 
		//       Remove mneumonic from terminator label.

		LoadSavePanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return widget = new LoadSavePanelWidget ();
		}
		
		public override bool ValidateChanges ()
		{
			return widget.ValidateChanges ();
		}

		
		public override void ApplyChanges ()
		{
			widget.Store();
		}
	}

	partial class LoadSavePanelWidget : Gtk.Bin
	{
		List<string> formats = new List<string> ();
		
		public LoadSavePanelWidget ()
		{
			Build ();
			
			folderEntry.Path = IdeApp.ProjectOperations.ProjectsDefaultPath;
			
			loadUserDataCheckButton.Active = IdeApp.Preferences.LoadDocumentUserProperties;
			createBackupCopyCheckButton.Active = IdeApp.Preferences.CreateFileBackupCopies;
			loadPrevProjectCheckButton.Active = IdeApp.Preferences.LoadPrevSolutionOnStartup;
		}
		
		public bool ValidateChanges ()
		{
			// check for correct settings
			string projectPath = folderEntry.Path;
			if (projectPath.Length > 0) {
				if (!FileService.IsValidPath(projectPath)) {
					MessageService.ShowError (GettextCatalog.GetString ("Invalid project path specified"));
					return false;
				}
			}
			return true;
		}
		
		public void Store () 
		{
			IdeApp.Preferences.LoadPrevSolutionOnStartup = loadPrevProjectCheckButton.Active;
			IdeApp.Preferences.LoadDocumentUserProperties = loadUserDataCheckButton.Active;
			IdeApp.Preferences.CreateFileBackupCopies = createBackupCopyCheckButton.Active;
			IdeApp.ProjectOperations.ProjectsDefaultPath = folderEntry.Path;
		}
	}
}

#pragma warning restore 612
