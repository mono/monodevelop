//
// DocumentControllerExtension.cs
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
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// Controller extensions are bound to a document controller and can participate in the serialization
	/// of the document, or implement additional commands, and can (optionally) provide additional views
	/// for a model or controller.
	/// </summary>
	[TypeExtensionPoint (Path = DocumentController.DocumentControllerExtensionsPath, ExtensionAttributeType = typeof (ExportDocumentControllerExtensionAttribute), Name = "Document controller extensions")]
	public class DocumentControllerExtension : ChainedExtension, IDisposable, ICommandRouter
	{
		DocumentControllerExtension next;

		internal TypeExtensionNode<ExportDocumentControllerExtensionAttribute> SourceExtensionNode { get; set; }

		internal string Id => GetType ().FullName + "_" + SourceExtensionNode?.Data.NodeId;

		/// <summary>
		/// Gets the capability of this view for being reassigned a project
		/// </summary>
		public virtual ProjectReloadCapability ProjectReloadCapability => Controller.OnGetProjectReloadCapability ();

		/// <summary>
		/// Controller to which this extension is bound
		/// </summary>
		/// <value>The controller.</value>
		protected DocumentController Controller { get; private set; }

		/// <summary>
		/// Called to check if this controller extension is supported for the provided controller.
		/// If the extension is supported, it will be attached to the controller. If it is not,
		/// it will be disposed and discarded.
		/// </summary>
		/// <returns>True if the extension is supported</returns>
		/// <param name="controller">Controller.</param>
		public virtual Task<bool> SupportsController (DocumentController controller)
		{
			return Task.FromResult (SourceExtensionNode == null || SourceExtensionNode.Data.CanHandle (controller));
		}

		internal Task Init (DocumentController controller, Properties status)
		{
			Controller = controller;
			return Initialize (status);
		}

		protected override void InitializeChain (ChainedExtension next)
		{
			base.InitializeChain (next);
			this.next = FindNextImplementation<DocumentControllerExtension> (next);
		}

		/// <summary>
		/// Initializes the controller
		/// </summary>
		/// <returns>The initialize.</returns>
		/// <param name="status">Status of the controller/view, returned by a GetDocumentStatus() call from a previous session</param>
		public virtual Task Initialize (Properties status)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Saves the document. If the controller has a model, the default implementation will save the model.
		/// </summary>
		public virtual Task OnSave ()
		{
			return next.OnSave ();
		}

		/// <summary>
		/// Sets the current editing status of the extension
		/// </summary>
		public virtual void SetDocumentStatus (Properties properties)
		{
		}

		/// <summary>
		/// Returns the current editing status of the controller extension.
		/// </summary>
		public virtual Properties GetDocumentStatus ()
		{
			return null;
		}

		/// <summary>
		/// Return true if the document has been modified
		/// </summary>
		public bool IsDirty { get; set; }

		/// <summary>
		/// Creates and initializes the view for the controller
		/// </summary>
		/// <returns>The initialize view.</returns>
		internal protected virtual Task<DocumentView> OnInitializeView ()
		{
			return next.OnInitializeView ();
		}

		internal void OnExtensionChainCreated ()
		{
		}

		protected virtual void OnFocused ()
		{
		}

		protected virtual void OnUnfocused ()
		{
		}

		internal protected virtual void OnContentShown ()
		{
		}

		internal protected virtual void OnContentHidden ()
		{
		}

		/// <summary>
		/// Invoked when the document that contains this controller has been closed, and before the controller hierarchy is disposed
		/// </summary>
		internal protected virtual void OnClosed ()
		{
		}

		public IEnumerable<object> GetContents (Type type)
		{
			return OnGetContents (type);
		}

		public void NotifyContentChanged ()
		{
			Controller?.NotifyContentChanged ();
		}

		protected virtual object OnGetContent (Type type)
		{
			if (type.IsInstanceOfType (this))
				return this;
			else
				return null;
		}

		protected virtual IEnumerable<object> OnGetContents (Type type)
		{
			var c = OnGetContent (type);
			if (c != null)
				yield return c;
		}

		internal void RunContentChanged ()
		{
			try {
				OnContentChanged ();
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}
		}

		protected virtual void OnContentChanged ()
		{
		}

		internal void RunOwnerChanged ()
		{
			try {
				OnOwnerChanged ();
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}
		}

		protected virtual void OnOwnerChanged ()
		{
		}

		object ICommandRouter.GetNextCommandTarget ()
		{
			return next;
		}
	}

	[ExportDocumentControllerExtension(Id = "Default")]
	class DefaultDocumentControllerExtension : DocumentControllerExtension
	{
		public override Task<bool> SupportsController (DocumentController controller)
		{
			return Task.FromResult (false);
		}
	}
}
