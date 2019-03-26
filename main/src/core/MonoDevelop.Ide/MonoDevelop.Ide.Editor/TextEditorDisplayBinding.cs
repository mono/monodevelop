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
using System.Collections.Immutable;

namespace MonoDevelop.Ide.Editor
{
	[ExportDocumentControllerFactory (Id = "TextEditor", MimeType = "*")]
	public class TextEditorDisplayBinding : FileDocumentControllerFactory
	{
		protected override async Task<IEnumerable<DocumentControllerDescription>> GetSupportedControllersAsync (FileDescriptor file)
		{
			var list = ImmutableList<DocumentControllerDescription>.Empty;

			var desktopService = await ServiceProvider.GetService<DesktopService> ();
			if (!file.FilePath.IsNullOrEmpty) {
				if (!desktopService.GetFileIsText (file.FilePath, file.MimeType))
					return list;
			}

			if (!string.IsNullOrEmpty (file.MimeType)) {
				if (!desktopService.GetMimeTypeIsText (file.MimeType))
					return list;
			}

			return list.Add (new DocumentControllerDescription {
				 Name = GettextCatalog.GetString ("Source Code Editor"),
				 Role = DocumentControllerRole.Source,
				 CanUseAsDefault = true
			});
		}

		public override Task<DocumentController> CreateController (FileDescriptor file, DocumentControllerDescription controllerDescription)
		{
			return Task.FromResult<DocumentController> (new TextEditorViewContent ());
		}

		public override string Id => "MonoDevelop.Ide.Editor.TextEditorDisplayBinding";
	}
}