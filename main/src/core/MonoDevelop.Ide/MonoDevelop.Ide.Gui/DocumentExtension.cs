//
// DocumentExtension.cs
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
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// An extension that is attached to a document
	/// </summary>
	public class DocumentExtension: ChainedExtension, ICommandRouter
	{
		DocumentExtension next;

		Document document;

		internal DocumentExtensionNode SourceExtensionNode { get; set; }

		protected override void InitializeChain (ChainedExtension next)
		{
			base.InitializeChain (next);
			this.next = FindNextImplementation<DocumentExtension> (next);
		}

		internal void Init (Document document)
		{
			this.document = document;
			Initialize ();
		}

		/// <summary>
		/// Document to which this extension is bound
		/// </summary>
		public Document Document => document;

		/// <summary>
		/// Window that shows the view of the document
		/// </summary>
		public IWorkbenchWindow Window => document.Window;

		/// <summary>
		/// Project to which the document belongs
		/// </summary>
		public WorkspaceObject Owner => document.Project;

		internal DocumentExtension Next => next;

		/// <summary>
		/// Invoked just after creating the extension chain of the object
		/// </summary>
		internal protected virtual void Initialize ()
		{
		}

		/// <summary>
		/// Invoked after all extensions have been initialized
		/// </summary>
		internal protected virtual void OnExtensionChainCreated ()
		{
		}

		/// <summary>
		/// Returns <c>true</c> if this extension should be enabled for the provided document.
		/// </summary>
		internal protected virtual bool SupportsDocument (Document document)
		{
			return SourceExtensionNode == null || SourceExtensionNode.CanHandleDocument (document);
		}

		/// <summary>
		/// Invoked when the document is being saved. <paramref name="fileSaveInformation"/> can be <see langword="null"/> if the document is not a file.
		/// </summary>
		/// <param name="fileSaveInformation">File information.</param>
		internal protected virtual Task OnSave (FileSaveInformation fileSaveInformation)
		{
			return next.OnSave (fileSaveInformation);
		}

		/// <summary>
		/// Invoked after the document has been saved. <paramref name="fileSaveInformation"/> can be <see langword="null"/> if the document is not a file.
		/// </summary>
		/// <param name="fileSaveInformation">File information.</param>
		internal protected virtual void OnSaved (FileSaveInformation fileSaveInformation)
		{
			next.OnSaved (fileSaveInformation);
		}

		/// <summary>
		/// Invoked when changes in the document have to be discarded
		/// </summary>
		internal protected virtual void OnDiscardChanges ()
		{
			next.OnDiscardChanges ();
		}

		/// <summary>
		/// Invoked after a document has been loaded.
		/// </summary>
		internal protected virtual Task OnLoaded (FileOpenInformation fileOpenInformation)
		{
			return next.OnLoaded (fileOpenInformation);
		}

		/// <summary>
		/// Invoked after a new document is created from a file template.
		/// </summary>
		/// <param name="fileCreationInformation">File creation data</param>
		internal protected virtual Task OnLoadedNew (FileCreationInformation fileCreationInformation)
		{
			return next.OnLoadedNew (fileCreationInformation);
		}

		/// <summary>
		/// Invoked to get a content object of the provided type
		/// </summary>
		/// <returns>The object, or null if not found</returns>
		/// <param name="type">Type of the object</param>
		internal protected virtual object OnGetContent (Type type)
		{
			if (type.IsInstanceOfType (this))
				return this;
			else
				return null;
		}

		/// <summary>
		/// Invoked to get a collection of content objects of the provided type
		/// </summary>
		/// <returns>The collection of objects of the provided type</returns>
		/// <param name="type">Type of the object</param>
		internal protected virtual IEnumerable OnGetContents (Type type)
		{
			var c = OnGetContent (type);
			if (c != null)
				yield return c;
		}

		/// <summary>
		/// Invoked when the project of the document changes
		/// </summary>
		internal protected virtual void OnOwnerChanged ()
		{
			next.OnOwnerChanged ();
		}

		/// <summary>
		/// Invoked when the document becomes the active document in the shell
		/// </summary>
		internal protected virtual void OnActivated ()
		{
			next.OnActivated ();
		}

		/// <summary>
		/// Gets the project reload capability.
		/// </summary>
		/// <returns>The project reload capability.</returns>
		internal protected virtual ProjectReloadCapability OnGetProjectReloadCapability ()
		{
			return next.OnGetProjectReloadCapability ();
		}

		object ICommandRouter.GetNextCommandTarget ()
		{
			return next;
		}
	}
}
