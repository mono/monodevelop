//
// ExportDocumentController.cs
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

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// Declares a file document controller. It is mandatory to provide a
	/// file filter using the FileExtension, MimeType or FileName properties
	/// </summary>
	public class ExportFileDocumentControllerAttribute: ExportDocumentControllerBaseAttribute
	{
		DocumentControllerFactory factory;

		public DocumentControllerFactory Factory {
			get {
				if (factory == null)
					factory = new DefaultDocumentControllerFactory (this);
				return factory;
			}
		}

		[NodeAttribute ("canUseAsDefault")]
		public bool CanUseAsDefault { get; set; }

		/// <summary>
		/// Role that the new controller will have
		/// </summary>
		[NodeAttribute ("role")]
		public DocumentControllerRole Role { get; set; }

		/// <summary>
		/// Name to show in visualizer selectors
		/// </summary>
		/// <value>The display name.</value>
		[NodeAttribute ("name", Localizable = true)]
		public string Name { get; set; }
	}

	class DefaultDocumentControllerFactory : FileDocumentControllerFactory
	{
		ExportFileDocumentControllerAttribute attribute;

		public DefaultDocumentControllerFactory (ExportFileDocumentControllerAttribute attribute)
		{
			this.attribute = attribute;
		}

		public override string Id => attribute.Id;

		public override Task<DocumentController> CreateController (FileDescriptor file, DocumentControllerDescription controllerDescription)
		{
			var node = (InstanceExtensionNode) attribute.ExtensionNode;
			return Task.FromResult ((DocumentController) node.CreateInstance (typeof (DocumentController)));
		}

		protected override IEnumerable<DocumentControllerDescription> GetSupportedControllers (FileDescriptor file)
		{
			if (attribute.CanHandle (file.FilePath, file.MimeType)) {
				yield return new DocumentControllerDescription {
					CanUseAsDefault = attribute.CanUseAsDefault,
					Role = attribute.Role,
					Name = attribute.Name
				};
			}
		}
	}
}
