// AssemblyReferencePanel.cs
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
	}
}
