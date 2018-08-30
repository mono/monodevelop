//
// DocumnentExtensionNode.cs
//
// Author:
//       lluis <>
//
// Copyright (c) 2018 
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
using System.Linq;
using Mono.Addins;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Extensions
{
	[ExtensionNode (ExtensionAttributeType = typeof (ExportDocumentExtensionAttribute))]
	public class DocumentExtensionNode: TypeExtensionNode
	{
		[NodeAttribute (Description = "File extensions. Comma separated list. Specify '*' to match all files.")]
		string fileExtensions;

		[NodeAttribute (Description = "Mime types. Comma separated list. Specify '*' to match all files.")]
		string mimeTypes;

		[NodeAttribute (Description = "File names. Comma separated list. Specify '*' to match all files.")]
		string name;

		string [] extensions;
		string [] types;
		string [] names;

		public DocumentExtensionNode (string fileExtensions, string mimeTypes, string name)
		{
			this.fileExtensions = fileExtensions;
			this.mimeTypes = mimeTypes;
			this.name = name;
		}

		public DocumentExtensionNode ()
		{
		}

		public virtual bool CanHandleDocument (Document document)
		{
			if (fileExtensions == "*" || mimeTypes == "*" || name == "*")
				return true;

			if (types.Length > 0 && document.MimeType != null) {
				if (types.Any (t => DesktopService.GetMimeTypeIsSubtype (document.MimeType, t)))
					return true;
			}

			var fileName = document.FileName;
			if (fileName.IsNullOrEmpty)
				return false;

			if (extensions.Length > 0) {
				string ext = fileName.Extension;
				if (extensions.Any (fe => string.Compare (fe, ext, StringComparison.OrdinalIgnoreCase) == 0))
					return true;
			}
			if (names.Length > 0) {
				string name = fileName.FileName;
				if (names.Any (fn => string.Compare (fn, name, StringComparison.OrdinalIgnoreCase) == 0))
					return true;
			}
			return false;
		}

		public virtual DocumentExtension CreateExtension ()
		{
			return (DocumentExtension)CreateInstance (typeof (DocumentExtension));
		}
	}
}
