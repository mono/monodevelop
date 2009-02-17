//  AssemblyReferencePanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Text;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;

using Gtk;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class AssemblyReferencePanel : HBox, IReferencePanel
	{
		SelectReferenceDialog selectDialog;
		FileChooserWidget chooser;
		
		public AssemblyReferencePanel (SelectReferenceDialog selectDialog)
		{
			this.selectDialog = selectDialog;
			
			chooser = new FileChooserWidget (FileChooserAction.Open, "");
			chooser.SetCurrentFolder (Environment.GetFolderPath (Environment.SpecialFolder.Personal));
			chooser.SelectMultiple = true;

			// this should only allow dll's and exe's
			FileFilter filter = new FileFilter ();
			filter.Name = GettextCatalog.GetString ("Assemblies");
			filter.AddPattern ("*.[Dd][Ll][Ll]");
			filter.AddPattern ("*.[Ee][Xx][Ee]");
			chooser.AddFilter (filter);
			chooser.FileActivated += new EventHandler (SelectReferenceDialog);

			PackStart (chooser, true, true, 0);
			
			PackStart (new Gtk.VSeparator(), false, false, 0);
			
			VBox box = new VBox ();
			Gtk.Button addButton = new Gtk.Button (Gtk.Stock.Add);
			addButton.Clicked += new EventHandler(SelectReferenceDialog);
			box.PackStart (addButton, false, false, 0);
			PackStart (box, false, false, 0);
			
			BorderWidth = 6;
			Spacing = 6;
			ShowAll();
		}
		
		public void SetBasePath (string path)
		{
			chooser.SetCurrentFolder (path);
		}
		
		void SelectReferenceDialog(object sender, EventArgs e)
		{
			string[] selectedFiles = new string[chooser.Filenames.Length];
			chooser.Filenames.CopyTo(selectedFiles, 0);
		
			foreach (string file in selectedFiles) {
				bool isAssembly = true;
				try	{
					System.Reflection.AssemblyName.GetAssemblyName(System.IO.Path.GetFullPath (file));
				} catch {
					isAssembly = false;
				}
			
				if (isAssembly) {
					selectDialog.AddReference (new ProjectReference (ReferenceType.Assembly, file));
				} else {
					MessageService.ShowError (GettextCatalog.GetString ("File '{0}' is not a valid .Net Assembly", file));
				}
			}
		}
		
		public void AddReference(object sender, EventArgs e)
		{
			//LoggingService.LogInfo("This panel will contain a file browser, but so long use the browse button :)");
		}
	}
}
