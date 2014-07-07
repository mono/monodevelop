//
// GoToViewCommandHandler.cs
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

using System;
using System.IO;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.AspNet.Commands
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

			var controller = doc.ParsedDocument.GetTopLevelTypeDefinition (currentLocation);
			string controllerName = controller.Name;
			int pos = controllerName.LastIndexOf ("Controller", StringComparison.Ordinal);
			if (pos > 0)
				controllerName = controllerName.Remove (pos);

			var baseDirectory = doc.FileName.ParentDirectory.ParentDirectory;

			string actionName = doc.ParsedDocument.GetMember (currentLocation).Name;
			var viewFoldersPaths = new [] {
				baseDirectory.Combine ("Views", controllerName),
				baseDirectory.Combine ("Views", "Shared")
			};
			var viewExtensions = new [] { ".aspx", ".cshtml" };

			foreach (var folder in viewFoldersPaths) {
				foreach (var ext in viewExtensions) {
					var possibleFile = folder.Combine (actionName + ext);
					if (File.Exists (possibleFile)) {
						IdeApp.Workbench.OpenDocument (possibleFile, doc.Project);
						return;
					}
				}
			}

			MessageService.ShowError ("Matching view cannot be found.");
		}
	}
}
