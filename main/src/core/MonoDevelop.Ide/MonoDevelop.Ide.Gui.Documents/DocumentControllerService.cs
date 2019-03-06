//
// DocumentControllerService.cs
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
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Documents
{
	[DefaultServiceImplementation]
	public class DocumentControllerService : Service
	{
		internal const string DocumentControllerFactoriesPath = "/MonoDevelop/Ide/DocumentControllerFactories";

		List<DocumentControllerFactory> registeredFactories = new List<DocumentControllerFactory> ();
		List<TypeExtensionNode<ExportDocumentControllerExtensionAttribute>> customExtensionNodes = new List<TypeExtensionNode<ExportDocumentControllerExtensionAttribute>> ();

		/// <summary>
		/// Checks if this factory can create a controller for the provided file, and returns the kind of
		/// controller it can create.
		/// </summary>
		public async Task<DocumentControllerDescription []> GetSupportedControllers (ModelDescriptor modelDescriptor)
		{
			var result = new List<DocumentControllerDescription> ();
			foreach (var factory in GetFactories (modelDescriptor)) {
				factory.ServiceProvider = ServiceProvider;
				foreach (var desc in await factory.GetSupportedControllersAsync (modelDescriptor)) {
					desc.Factory = factory;
					desc.ServiceProvider = ServiceProvider;
					result.Add (desc);
				}
			}
			return result.ToArray ();
		}

		IEnumerable<DocumentControllerFactory> GetFactories (ModelDescriptor modelDescriptor)
		{
			foreach (var node in AddinManager.GetExtensionNodes (DocumentControllerFactoriesPath)) {
				if (node is TypeExtensionNode<ExportFileDocumentControllerAttribute> controllerNode)
					yield return controllerNode.Data.Factory;
				else if (node is TypeExtensionNode<ExportDocumentControllerFactoryAttribute> factoryNode && factoryNode.Data.CanHandle (modelDescriptor))
					yield return factoryNode.Data.Factory;
			}
			foreach (var factory in registeredFactories)
				yield return factory;
		}

		public void RegisterFactory (DocumentControllerFactory factory)
		{
			registeredFactories.Add (factory);
		}

		public void UnregisterFactory (DocumentControllerFactory factory)
		{
			registeredFactories.Remove (factory);
		}

		internal IEnumerable<TypeExtensionNode<ExportDocumentControllerExtensionAttribute>> GetModelExtensions (ExtensionContext ctx)
		{
			return ctx.GetExtensionNodes<TypeExtensionNode<ExportDocumentControllerExtensionAttribute>> (DocumentController.DocumentControllerExtensionsPath).Concat (customExtensionNodes);
		}

		internal void RegisterControllerExtension (ExportDocumentControllerExtensionAttribute attribute, Type extensionType)
		{
			var node = new CustomControllerExtensionNode (attribute, extensionType);
			customExtensionNodes.Add (node);
		}

		internal void UnregisterControllerExtension (ExportDocumentControllerExtensionAttribute attribute)
		{
			customExtensionNodes.RemoveAll (n => n.Data == attribute);
		}
	}

	class CustomControllerExtensionNode: TypeExtensionNode<ExportDocumentControllerExtensionAttribute>
	{
		Type type;
		static int id;

		public CustomControllerExtensionNode (ExportDocumentControllerExtensionAttribute attribute, Type type)
		{
			this.type = type;
			attribute.NodeId = "_id_" + (id++);
			GetType ().GetProperty ("Data").SetValue (this, attribute);
		}

		public override object CreateInstance ()
		{
			return Activator.CreateInstance (type);
		}
	}
}
