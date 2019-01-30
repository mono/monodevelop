//
// DocumentControllerExtensionFactory.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Collections.Generic;
using System.Linq;
using Mono.Addins;
namespace MonoDevelop.Ide.Gui.Documents
{
	public class DocumentControllerExtensionAttribute : Mono.Addins.CustomExtensionAttribute
	{
		[NodeAttribute ("fileExtension")]
		public string FileExtension {
			get => FileExtensions?.FirstOrDefault ();
			set => FileExtensions = new [] { value };
		}

		[NodeAttribute ("fileExtensions")]
		public string [] FileExtensions { get; set; }

		[NodeAttribute ("mimeType")]
		public string MimeType {
			get => MimeTypes?.FirstOrDefault ();
			set => MimeTypes = new [] { value };
		}

		[NodeAttribute ("mimeTypes")]
		public string [] MimeTypes { get; set; }

		[NodeAttribute ("fileName")]
		public string FileName {
			get => FileNames?.FirstOrDefault ();
			set => FileNames = new [] { value };
		}

		[NodeAttribute ("fileNames")]
		public string [] FileNames { get; set; }

		[NodeAttribute ("controllerType")]
		public Type ControllerType { get; set; }

		public string ControllerTypeName { get; set; }

		public bool CanHandle (DocumentController controller)
		{
			if (controller is FileDocumentController fileController) {
				if (FileExtensions != null && FileExtensions.Contains ("*"))
					return true;
				if (MimeTypes != null && MimeTypes.Contains ("*"))
					return true;
				if (FileNames != null && FileNames.Contains ("*"))
					return true;

				if (!string.IsNullOrEmpty (fileController.FilePath) && FileExtensions != null && FileExtensions.Length > 0) {
					string ext = System.IO.Path.GetExtension (fileController.FilePath);
					foreach (var allowedExtension in FileExtensions) {
						if (string.Equals (ext, allowedExtension, StringComparison.OrdinalIgnoreCase)) {
							return true;
						}
					}
				}

				if (MimeTypes != null && MimeTypes.Length > 0) {
					IEnumerable<string> mimeTypeChain;
					if (!string.IsNullOrEmpty (fileController.MimeType))
						mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (fileController.MimeType);
					else
						mimeTypeChain = DesktopService.GetMimeTypeInheritanceChainForFile (fileController.FilePath);
					foreach (var mimeType in mimeTypeChain) {
						foreach (var allowedMime in MimeTypes) {
							if (mimeType == allowedMime) {
								return true;
							}
						}
					}
				}

				if (FileNames != null && FileNames.Length > 0 && !string.IsNullOrEmpty (fileController.FilePath)) {
					string name = fileController.FilePath.FileName;
					foreach (var allowedName in FileNames) {
						if (string.Equals (name, allowedName, StringComparison.OrdinalIgnoreCase)) {
							return true;
						}
					}
				}

				return false;
			} else if (ControllerType != null) {
				return ControllerType.IsInstanceOfType (controller);
			}
			return true;
		}
	}
}
