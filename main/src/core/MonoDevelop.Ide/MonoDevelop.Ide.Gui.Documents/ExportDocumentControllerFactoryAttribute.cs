//
// ExportDocumentControllerAttribute.cs
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
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// Declares a controller factory. Factories for file controllers must provide a
	/// file filter using the FileExtension, MimeType or FileName properties
	/// </summary>
	public class ExportDocumentControllerFactoryAttribute: ExportDocumentControllerBaseAttribute
	{
		DocumentControllerFactory factory;

		internal DocumentControllerFactory Factory {
			get {
				if (factory == null) {
					factory = (DocumentControllerFactory)((TypeExtensionNode)ExtensionNode).CreateInstance (typeof (DocumentControllerFactory));
					if ((factory is FileDocumentControllerFactory) && !HasFileFilter) {
						LoggingService.LogError ("Document controller factories of type FileDocumentControllerFactory must have a file filter");
						factory = new NopDocumentControllerFactory ();
					}
				}
				return factory;
			}
		}

		public bool CanHandle (ModelDescriptor modelDescriptor)
		{
			if (modelDescriptor is FileDescriptor file)
				return CanHandle (file.FilePath, file.MimeType);
			return true;
		}
	}

	class NopDocumentControllerFactory : DocumentControllerFactory
	{
		public override Task<DocumentController> CreateController (ModelDescriptor modelDescriptor, DocumentControllerDescription controllerDescription)
		{
			throw new NotImplementedException ();
		}

		protected override IEnumerable<DocumentControllerDescription> GetSupportedControllers (ModelDescriptor modelDescriptor)
		{
			yield break;
		}
	}
}
