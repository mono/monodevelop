// 
// NativeReferenceCommandHandler.cs
//  
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Michael Hutchinson
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
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.MacDev.NativeReferences
{
	public enum NativeReferenceCommands
	{
		Add,
		Delete,
	}
	
	class NativeReferenceFolderCommandHandler : NodeCommandHandler
	{
		[CommandHandler (NativeReferenceCommands.Add)]
		public void Add ()
		{
			var project = (DotNetProject) CurrentNode.GetParentDataItem (typeof(DotNetProject), true);
			
			var dlg = new SelectFileDialog (GettextCatalog.GetString ("Select Native Library"), Gtk.FileChooserAction.Open);
			dlg.SelectMultiple = true;
			dlg.AddAllFilesFilter ();
			//FIXME: add more filters, amke correct for platform
			dlg.AddFilter (GettextCatalog.GetString ("Static Library"), ".a");
			
			if (!dlg.Run ())
				return;
			
			foreach (var file in dlg.SelectedFiles) {
				var item = new NativeReference (file);
				project.Items.Add (item);
			}
			
			IdeApp.ProjectOperations.Save (project);
		}
	}
		
	class NativeReferenceCommandHandler : NodeCommandHandler
	{
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		public void UpdateDelete (CommandInfo info)
		{
			info.Text = GettextCatalog.GetString ("Remove");
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		public void Delete ()
		{
			var item = (NativeReference) CurrentNode.DataItem;
			string question = GettextCatalog.GetString (
				"Are you sure you want to remove the native reference '{0}'?", item.Path.FileNameWithoutExtension);
			
			if (!MessageService.Confirm (question, AlertButton.Remove))
				return;
			
			var project = (DotNetProject) CurrentNode.GetParentDataItem (typeof(DotNetProject), true);
			project.Items.Remove (item);
			
			IdeApp.ProjectOperations.Save (project);
		}
	}
}