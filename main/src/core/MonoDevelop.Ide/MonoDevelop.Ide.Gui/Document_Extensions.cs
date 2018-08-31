//
// Document_Extensions.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui
{
	public partial class Document: ICommandRouter
	{
		const string DocumentExtensionsPath = "/MonoDevelop/Ide/DocumentExtensions";

		ExtensionChain extensionChain;
		DocumentExtension documentExtension;
		ExtensionContext extensionContext;

		void InitializeDocumentExtensionChain ()
		{
			if (documentExtension != null)
				return;

			// Create an initial empty extension chain. This avoid crashes in case a call to SupportsObject ends
			// calling methods from the extension

			var defaultExtension = new DefaultDocumentExtension ();
			extensionChain = ExtensionChain.Create (new [] { defaultExtension });
			defaultExtension.Init (this);

			// Collect extensions that support this object

			extensionContext = CreateExtensionContext ();

			var extensions = new List<DocumentExtension> ();
			foreach (var node in GetModelExtensions (extensionContext)) {
				if (node.CanHandleDocument (this)) {
					var ext = node.CreateExtension ();
					if (ext.SupportsDocument (this)) {
						ext.SourceExtensionNode = node;
						extensions.Add (ext);
					}
				}
			}

			defaultExtension.Dispose ();

			// Now create the final extension chain

			extensions.Reverse ();
			defaultExtension = new DefaultDocumentExtension ();
			extensionChain.SetDefaultInsertionPosition (defaultExtension);
			extensions.Add (defaultExtension);
			extensionChain = ExtensionChain.Create (extensions.ToArray ());
			foreach (var e in extensions)
				e.Init (this);

			documentExtension = extensionChain.GetExtension<DocumentExtension> ();

			foreach (var e in extensions)
				e.OnExtensionChainCreated ();
		}

		IEnumerable<DocumentExtensionNode> GetModelExtensions (ExtensionContext ctx)
		{
			return ctx.GetExtensionNodes (DocumentExtensionsPath).Cast<DocumentExtensionNode> ().ToArray ();
		}

		ExtensionContext CreateExtensionContext ()
		{
			var context = AddinManager.CreateExtensionContext ();
			if (!FileName.IsNullOrEmpty) {
				var cond = new FileTypeCondition ();
				cond.SetFileName (FileName);
				context.RegisterCondition ("FileType", cond);
			}
			return context;
		}

		/// <summary>
		/// Ensures that this document has the extensions it requires according to its current state
		/// </summary>
		/// <remarks>
		/// This method will load new extensions that this document supports and will unload extensions that are not supported anymore.
		/// The set of extensions that a project supports may change over time, depending on the status of the document.
		/// </remarks>
		void RefreshExtensions ()
		{
			// First of all look for new extensions that should be attached

			// Get the list of nodes for which an extension has been created

			var allExtensions = extensionChain.GetAllExtensions ().OfType<DocumentExtension> ().ToList ();
			var loadedNodes = allExtensions.Where (ex => ex.SourceExtensionNode != null)
				.Select (ex => ex.SourceExtensionNode.Id).ToList ();
			var newExtensions = new List<DocumentExtension> ();

			DocumentExtensionNode lastAddedNode = null;

			// Ensure conditions are re-evaluated.
			extensionContext = CreateExtensionContext ();

			foreach (DocumentExtensionNode node in GetModelExtensions (extensionContext)) {
				// If the node already generated an extension, skip it
				if (loadedNodes.Contains (node.Id)) {
					lastAddedNode = node;
					loadedNodes.Remove (node.Id);
					continue;
				}

				// Maybe the node can now generate an extension for this document
				if (node.CanHandleDocument (this)) {
					var ext = node.CreateExtension ();
					if (ext.SupportsDocument (this)) {
						ext.SourceExtensionNode = node;
						newExtensions.Add (ext);
						if (lastAddedNode != null) {
							// There is an extension before this one. Find it and add the new extension after it.
							var prevExtension = allExtensions.FirstOrDefault (ex => ex.SourceExtensionNode?.Id == lastAddedNode.Id);
							extensionChain.AddExtension (ext, prevExtension);
						} else
							extensionChain.AddExtension (ext);
						ext.Init (this);
					}
				}
			}

			// Now dispose extensions that are not supported anymore

			foreach (var ext in allExtensions) {
				if (!ext.SupportsDocument (this))
					ext.Dispose ();
			}

			if (loadedNodes.Any ()) {
				foreach (var ext in allExtensions.Where (ex => ex.SourceExtensionNode != null)) {
					if (loadedNodes.Contains (ext.SourceExtensionNode.Id)) {
						ext.Dispose ();
						loadedNodes.Remove (ext.SourceExtensionNode.Id);
					}
				}
			}

			foreach (var e in newExtensions)
				e.OnExtensionChainCreated ();

			// If a file is loaded in the document, notify the new extensions about that
			if (!FileName.IsNullOrEmpty) {
				foreach (var e in newExtensions)
					e.OnLoaded (new FileOpenInformation (FileName, Project));
			}
		}

		object ICommandRouter.GetNextCommandTarget ()
		{
			return documentExtension;
		}

		class DefaultDocumentExtension : DocumentExtension, ICommandRouter
		{
			internal protected override Task OnSave (FileSaveInformation fileSaveInformation)
			{
				return Document.DoSaveViewContent (fileSaveInformation);
			}

			internal protected override void OnSaved (FileSaveInformation fileSaveInformation)
			{
				Document.OnSaved (EventArgs.Empty);
			}

			internal protected override Task OnLoaded (FileOpenInformation fileOpenInformation)
			{
				// Do nothing, since the view content has already been loaded at this point
				return Task.CompletedTask;
			}

			internal protected override Task OnLoadedNew (FileCreationInformation fileCreationInformation)
			{
				// Do nothing, since the view content has already been initialized at this point
				return Task.CompletedTask;
			}

			internal protected override void OnDiscardChanges ()
			{
				Document.window.ViewContent.DiscardChanges ();
			}

			internal protected override void OnOwnerChanged ()
			{
			}

			internal protected override void OnActivated ()
			{
				// Do nothing
			}

			internal protected override ProjectReloadCapability OnGetProjectReloadCapability ()
			{
				return Document.Window.ViewContent.ProjectReloadCapability;
			}

			object ICommandRouter.GetNextCommandTarget ()
			{
				return ((SdiWorkspaceWindow)Document.Window).Parent;
			}
		}
	}
}
