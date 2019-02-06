//
// TextEditorDisplayBinding.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui;
using System.IO;
using MonoDevelop.Projects;
using System.ComponentModel;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Gui.Documents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Editor
{
	[ExportDocumentControllerFactory (MimeType = "*")]
	public class TextEditorDisplayBinding : FileDocumentControllerFactory
	{
		public override IEnumerable<DocumentControllerDescription> GetSupportedControllers (FileDescriptor file)
		{
			if (!file.FilePath.IsNullOrEmpty) {
				if (!IdeApp.DesktopService.GetFileIsText (file.FilePath, file.MimeType))
					yield break;
			}

			if (!string.IsNullOrEmpty (file.MimeType)) {
				if (!IdeApp.DesktopService.GetMimeTypeIsText (file.MimeType))
					yield break;
			}

			yield return new DocumentControllerDescription {
				 Name = GettextCatalog.GetString ("Source Code Editor"),
				 Role = DocumentControllerRole.Source,
				 CanUseAsDefault = true
			};
		}

		public override Task<DocumentController> CreateController (FileDescriptor file, DocumentControllerDescription controllerDescription)
		{
			TextEditor editor;

			// HACK: CreateNewEditor really needs to know whether the document exists (& should be loaded)
			// or we're creating an empty document with the given file name & mime type.
			//
			// That information could be added to FilePath but fileName is converted to a string below
			// which means the information is lost.
			editor = TextEditorFactory.CreateNewEditor (file.FilePath, file.MimeType);

			return Task.FromResult< DocumentController> (editor.GetViewContent ());
		}

		public override string Id => "MonoDevelop.Ide.Editor.TextEditorDisplayBinding";
	}
}