// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Text;
using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui.Widgets;

using Gtk;

namespace MonoDevelop.Gui.Dialogs
{
	internal class AssemblyReferencePanel : HBox, IReferencePanel
	{
		SelectReferenceDialog selectDialog;
		FileChooserWidget chooser;
		
		public AssemblyReferencePanel(SelectReferenceDialog selectDialog)
		{
			this.selectDialog = selectDialog;
			
			chooser = new FileChooserWidget (FileChooserAction.Open, "");
			chooser.SetCurrentFolder (Environment.GetFolderPath (Environment.SpecialFolder.Personal));
			chooser.SelectMultiple = true;

			// this should only allow dll's and exe's
			FileFilter filter = new FileFilter ();
			filter.Name = GettextCatalog.GetString ("Assemblies");
			filter.AddPattern ("*.dll");
			filter.AddPattern ("*.exe");
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
		
		void SelectReferenceDialog(object sender, EventArgs e)
		{
			string[] selectedFiles = new string[chooser.Filenames.Length];
			chooser.Filenames.CopyTo(selectedFiles, 0);
		
			foreach (string file in selectedFiles) {
				bool isAssembly = true;
				try	{
					System.Reflection.AssemblyName.GetAssemblyName(System.IO.Path.GetFullPath (file));
				} catch (Exception assemblyExcep) {
					isAssembly = false;
				}
			
				if (isAssembly) {
				selectDialog.AddReference(ReferenceType.Assembly,
					System.IO.Path.GetFileName(file),
					file);
				} else {
					// FIXME: il8n this
					Runtime.MessageService.ShowError(String.Format (GettextCatalog.GetString ("File {0} is not a valid .Net Assembly"), file));
				}
			}
		}
		
		public void AddReference(object sender, EventArgs e)
		{
			//System.Runtime.LoggingService.Info("This panel will contain a file browser, but so long use the browse button :)");
		}
	}
}
