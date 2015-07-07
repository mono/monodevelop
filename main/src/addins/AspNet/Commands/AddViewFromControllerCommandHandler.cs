//
// AddViewFromControllerCommandHandler.cs
//
// Author:
//       Piotr Dowgiallo <sparekd@gmail.com>
// 
// Copyright (c) 2012 Piotr Dowgiallo.
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.AspNet.Projects;

namespace MonoDevelop.AspNet.Commands
{
	class AddViewFromControllerCommandHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			MvcCommandsCommonHandler.Update (info);
		}

		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var project = (AspNetAppProject)doc.Project;
			var currentLocation = doc.Editor.Caret.Location;

			string controllerName = doc.ParsedDocument.GetTopLevelTypeDefinition (currentLocation).Name;
			int pos = controllerName.LastIndexOf ("Controller", StringComparison.Ordinal);
			if (pos > 0)
				controllerName = controllerName.Remove (pos);

			string path = doc.FileName.ParentDirectory.ParentDirectory.Combine ("Views", controllerName);
			string actionName = doc.ParsedDocument.GetMember (currentLocation).Name;
			AddView (project, path, actionName);
		}

		public static void AddView (AspNetAppProject project, string path, string name)
		{
			var provider = project.LanguageBinding.GetCodeDomProvider ();
			if (provider == null)
				throw new InvalidOperationException ("Project language has null CodeDOM provider");

			string outputFile = null;
			MvcTextTemplateHost host = null;
			AddViewDialog dialog = null;

			try {
				dialog = new AddViewDialog (project);
				dialog.ViewName = name;

				bool fileGood = false;
				while (!fileGood) {
					var resp = (Gtk.ResponseType) MessageService.RunCustomDialog (dialog);
					dialog.Hide ();
					if (resp != Gtk.ResponseType.Ok || ! dialog.IsValid ())
						return;

					string ext = ".cshtml";
					if (dialog.ActiveViewEngine == "Aspx")
						ext = dialog.IsPartialView ? ".ascx" : ".aspx";

					if (!System.IO.Directory.Exists (path))
						System.IO.Directory.CreateDirectory (path);

					outputFile = System.IO.Path.Combine (path, dialog.ViewName) + ext;

					if (System.IO.File.Exists (outputFile)) {
						fileGood = MessageService.AskQuestion ("Overwrite file?",
							String.Format ("The file '{0}' already exists.\n", dialog.ViewName) +
							"Would you like to overwrite it?", AlertButton.OverwriteFile, AlertButton.Cancel)
							!= AlertButton.Cancel;
					} else
						break;
				}

				host = new MvcTextTemplateHost {
					LanguageExtension = provider.FileExtension,
					ItemName = dialog.ViewName,
					ViewDataTypeString = ""
				};

				if (dialog.HasMaster) {
					host.IsViewContentPage = true;
					host.ContentPlaceholder = dialog.PrimaryPlaceHolder;
					host.MasterPage = dialog.MasterFile;
					host.ContentPlaceHolders = dialog.ContentPlaceHolders;
				}
				else if (dialog.IsPartialView)
					host.IsViewUserControl = true;
				else
					host.IsViewPage = true;

				if (dialog.IsStronglyTyped)
					host.ViewDataTypeString = dialog.ViewDataTypeString;

				host.ProcessTemplate (dialog.TemplateFile, outputFile);
				MonoDevelop.TextTemplating.TextTemplatingService.ShowTemplateHostErrors (host.Errors);

			} finally {
				if (host != null)
					host.Dispose ();
				if (dialog != null) {
					dialog.Destroy ();
					dialog.Dispose ();
				}
			}

			if (System.IO.File.Exists (outputFile)) {
				project.AddFile (outputFile);
				IdeApp.ProjectOperations.Save (project);
			}
		}
	}
	
}
