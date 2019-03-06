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
	/// <summary>
	/// Declares a document controller extension. Extensions for file controllers must provide a
	/// file filter using the FileExtension, MimeType or FileName properties
	/// </summary>
	public class ExportDocumentControllerExtensionAttribute : ExportDocumentControllerBaseAttribute
	{
		string nodeId;

		[NodeAttribute ("controllerType")]
		public Type ControllerType { get; set; }

		public string ControllerTypeName { get; set; }

		public bool CanHandle (DocumentController controller)
		{
			if (controller is FileDocumentController fileController) {
				return CanHandle (fileController.FilePath, fileController.MimeType);
			} else if (ControllerType != null) {
				return ControllerType.IsInstanceOfType (controller);
			}
			return true;
		}

		internal virtual string NodeId {
			get => nodeId ?? ExtensionNode.Id;
			set => nodeId = value;
		}
	}
}
