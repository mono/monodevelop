//
// DocumentExtension.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui
{
	public class DocumentExtension: ChainedExtension, ICommandRouter
	{
		DocumentExtension next;

		Document document;
		IWorkbenchWindow workbenchWindow;

		internal DocumentExtensionNode SourceExtensionNode { get; set; }

		protected override void InitializeChain (ChainedExtension next)
		{
			base.InitializeChain (next);
			this.next = FindNextImplementation<DocumentExtension> (next);
		}

		internal void Init (Document document)
		{
			this.document = document;
			this.workbenchWindow = workbenchWindow;
			Initialize ();
		}

		/// <summary>
		/// Document to which this extension is bound
		/// </summary>
		public Document Document => document;

		/// <summary>
		/// Window that shows the view of the document
		/// </summary>
		public IWorkbenchWindow WorkbenchWindow => document.Window;

		/// <summary>
		/// Project to which the document belongs
		/// </summary>
		public Project Project => document.Project;

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

		public virtual Task OnSave (FileSaveInformation fileSaveInformation)
		{
			return next.OnSave (fileSaveInformation);
		}
		
		public virtual void DiscardChanges ()
		{
			next.DiscardChanges ();
		}

		public virtual Task OnLoaded (FileOpenInformation fileOpenInformation)
		{
			return next.OnLoaded (fileOpenInformation);
		}

		public virtual Task OnLoadedNew (System.IO.Stream content, string mimeType)
		{
			return next.OnLoadedNew (content, mimeType);
		}

		internal protected virtual object OnGetContent (Type type)
		{
			if (type.IsInstanceOfType (this))
				return this;
			else
				return null;
		}

		internal protected virtual IEnumerable OnGetContents (Type type)
		{
			var c = OnGetContent (type);
			if (c != null)
				yield return c;
		}

		public virtual void OnOwnerChanged ()
		{
			next.OnOwnerChanged ();
		}

		public virtual ProjectReloadCapability GetProjectReloadCapability ()
		{
			return next.GetProjectReloadCapability ();
		}

		object ICommandRouter.GetNextCommandTarget ()
		{
			return next;
		}
	}
}
