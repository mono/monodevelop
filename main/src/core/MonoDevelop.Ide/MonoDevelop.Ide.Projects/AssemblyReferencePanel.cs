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
using MonoDevelop.Components;

using Gtk;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Ide.Projects
{
	internal class AssemblyReferencePanel : VBox, IReferencePanel
	{
		SelectReferenceDialog selectDialog;
		FileChooserWidget chooser;
		Gtk.Label detailsLabel;
		Gtk.Button addButton;
		
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
			chooser.SelectionChanged += HandleChooserSelectionChanged;
			chooser.BorderWidth = 6;

			PackStart (chooser, true, true, 0);
			
			HeaderBox hbox = new HeaderBox (1, 0, 0, 0);
			hbox.GradientBackround = true;
			hbox.SetPadding (6,6,6,6);
			
			HBox box = new HBox ();
			detailsLabel = new Label ();
			detailsLabel.Xalign = 0;
			detailsLabel.Ellipsize = Pango.EllipsizeMode.End;
			box.PackStart (detailsLabel, true, true, 0);
			addButton = new Gtk.Button (Gtk.Stock.Add);
			box.PackEnd (addButton, false, false, 0);
			hbox.Add (box);
			PackStart (hbox, false, false, 0);
			
			addButton.Clicked += SelectReferenceDialog;
			
			Spacing = 6;
			ShowAll();
		}
		
		public void SetProject (DotNetProject configureProject)
		{
			SetBasePath (configureProject.BaseDirectory);
		}
		
		public void SignalRefChange (ProjectReference refInfo, bool newState)
		{
		}
		
		public void SetFilter (string filter)
		{
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
					SystemAssemblyService.GetAssemblyName (System.IO.Path.GetFullPath (file));
				} catch {
					isAssembly = false;
				}
			
				if (isAssembly) {
					selectDialog.AddReference (new ProjectReference (ReferenceType.Assembly, file));
					selectDialog.RegisterFileReference (file);
				} else {
					MessageService.ShowError (GettextCatalog.GetString ("File '{0}' is not a valid .Net Assembly", file));
				}
			}
		}

		void HandleChooserSelectionChanged (object sender, EventArgs e)
		{
			if (chooser.Filenames.Length == 0) {
				detailsLabel.Text = "";
				addButton.Sensitive = false;
				return;
			}
		
			bool allAssemblies = true;
			foreach (string file in chooser.Filenames) {
				try	{
					SystemAssemblyService.GetAssemblyName (System.IO.Path.GetFullPath (file));
				} catch {
					allAssemblies = false;
					break;
				}
			}
			
			if (!allAssemblies) {
				detailsLabel.Text = "";
				addButton.Sensitive = false;
				return;
			}
			
			if (chooser.Filenames.Length == 1) {
				string aname = SystemAssemblyService.GetAssemblyName (chooser.Filenames[0]);
				detailsLabel.Text = aname;
			} else {
				detailsLabel.Text = GettextCatalog.GetString ("{0} Assemblies selected", chooser.Filenames.Length);
			}
			addButton.Sensitive = true;
		}
	}
}
