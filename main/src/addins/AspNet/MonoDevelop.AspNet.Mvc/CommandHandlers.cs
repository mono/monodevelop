// 
// CommandHandlers.cs
//  
// Author:
//       Piotr Dowgiallo <sparekd@gmail.com>
// 
// Copyright (c) 2012 Piotr Dowgiallo
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.AspNet.Mvc.Gui;

namespace MonoDevelop.AspNet.Mvc
{
	class GoToViewCommandHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			MvcCommandsCommonHandler.Update (info);
		}

		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var currentLocation = doc.Editor.Caret.Location;

			string controllerName = doc.ParsedDocument.GetTopLevelTypeDefinition (currentLocation).Name;
			int pos = controllerName.LastIndexOf ("Controller");
			if (pos != -1)
				controllerName = controllerName.Remove (pos);

			string actionName = doc.ParsedDocument.GetMember (currentLocation).Name;
			var viewFoldersPaths = new FilePath[] {
				doc.Project.BaseDirectory.Combine ("Views", controllerName),
				doc.Project.BaseDirectory.Combine ("Views", "Shared")
			};
			var viewExtensions = new string[] { ".aspx", ".cshtml" };

			foreach (var folder in viewFoldersPaths) {
				foreach (var ext in viewExtensions) {
					var possibleFile = folder.Combine (actionName + ext);
					if (File.Exists (possibleFile)) {
						IdeApp.Workbench.OpenDocument (possibleFile);
						return;
					}
				}
			}

			MessageService.ShowError ("Matching view cannot be found.");
		}
	}

	class AddViewFromControllerCommandHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			MvcCommandsCommonHandler.Update (info);
		}

		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var project = doc.Project as AspMvcProject;
			var currentLocation = doc.Editor.Caret.Location;

			string controllerName = doc.ParsedDocument.GetTopLevelTypeDefinition (currentLocation).Name;
			int pos = controllerName.LastIndexOf ("Controller");
			if (pos != -1)
				controllerName = controllerName.Remove (pos);

			string path = doc.Project.BaseDirectory.Combine ("Views", controllerName);
			string actionName = doc.ParsedDocument.GetMember (currentLocation).Name;
			FolderCommandHandler.AddView (project, path, actionName);
		}
	}

	class GoToControllerCommandHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || !(doc.Project is AspMvcProject)) {
				info.Enabled = info.Visible = false;
				return;
			}
			var rootFolder = doc.Project.BaseDirectory.Combine ("Views");
			if (!doc.FileName.ParentDirectory.IsChildPathOf (rootFolder))
				info.Enabled = info.Visible = false;
		}

		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var name = doc.FileName.ParentDirectory.FileName;
			var controller = doc.ProjectContent.GetAllTypeDefinitions ().FirstOrDefault (t => t.Name == name + "Controller");

			if (controller != null)
				IdeApp.Workbench.OpenDocument (controller.UnresolvedFile.FileName);
			else
				MessageService.ShowError ("Matching controller cannot be found.");
		}
	}

	static class MvcCommandsCommonHandler
	{
		public static void Update (CommandInfo info)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || !(doc.Project is AspMvcProject)) {
				info.Enabled = info.Visible = false;
				return;
			}

			var currentLocation = doc.Editor.Caret.Location;
			var topLevelType = doc.ParsedDocument.GetTopLevelTypeDefinition (currentLocation);
			if (topLevelType == null || !topLevelType.BaseTypes.Any (t => t.ToString () == "Controller")) {
				info.Enabled = info.Visible = false;
				return;
			}

			var correctReturnTypes = new string[] { "ActionResult", "ViewResultBase", "ViewResult", "PartialViewResult" };
			var member = doc.ParsedDocument.GetMember (currentLocation) as IUnresolvedMethod;
			if (member == null || !member.IsPublic || !correctReturnTypes.Any (t => t == member.ReturnType.ToString ()))
				info.Enabled = info.Visible = false;
		}
	}
}

