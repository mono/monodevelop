//
// DocumentController.cs
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
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	public abstract class DocumentController
	{
		List<AttachedDocumentController> attachedControllers = new List<AttachedDocumentController> ();

		DocumentModel model;

		bool isDirty;
		bool isReadOnly;
		bool isNewDocument;

		/// <summary>
		/// Role of the controller
		/// </summary>
		/// <value>The role.</value>
		public DocumentControllerRole Role { get; internal set; }

		/// <summary>
		/// Model that contains the data for this controller. Null if the controller doesn't use a model.
		/// </summary>
		/// <value>The model.</value>
		public DocumentModel Model {
			get { return model; }
			protected set {
				if (value != model) {
					model = value;
					OnModelChanged ();
				}
			}
		}

		protected virtual void OnModelChanged ()
		{
			if (Model is FileDocumentModel fileModel) {
				IsNewDocument
			}
			ModelChanged?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Raised when the Model property changes
		/// </summary>
		public event EventHandler IsNewDocumentChanged;

		/// <summary>
		/// Owner project or solution item
		/// </summary>
		/// <value>The owner.</value>
		public WorkspaceObject Owner { get; set; }

		/// <summary>
		/// Initializes the controller
		/// </summary>
		/// <returns>The initialize.</returns>
		/// <param name="status">Status of the controller/view, returned by a GetDocumentStatus() call from a previous session</param>
		public virtual Task Initialize (PropertyBag status)
		{
		}

		public Task Save ()
		{
			return OnSave ();
		}

		/// <summary>
		/// Saves the document. If the controller has a model, the default implementation will save the model.
		/// </summary>
		protected virtual Task OnSave ()
		{
			if (Model != null)
				return Model.Save ();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Attaches a controller to this controller. 
		/// </summary>
		public void AttachController (AttachedDocumentController controller)
		{
			attachedControllers.Add (controller);
		}

		/// <summary>
		/// List of controllers attached to this one
		/// </summary>
		/// <value>The attached controllers.</value>
		public IEnumerable<AttachedDocumentController> AttachedControllers => attachedControllers;

		/// <summary>
		/// Gets the capability of this view for being reassigned a project
		/// </summary>
		public ProjectReloadCapability ProjectReloadCapability {
			get {
				return attachedControllers.Select (c => c.ProjectReloadCapability).Append (OnGetProjectReloadCapability ()).Min ();
			}
		}

		/// <summary>
		/// Gets the capability of this view for being reassigned a project
		/// </summary>
		protected virtual ProjectReloadCapability OnGetProjectReloadCapability ()
		{
			return ProjectReloadCapability.None;
		}

		/// <summary>
		/// Title shown in the document tab
		/// </summary>
		public virtual string DocumentTitle { get; }

		/// <summary>
		/// Icon of the document tab
		/// </summary>
		/// <value>The stock icon identifier.</value>
		public virtual string DocumentIconId { get; }

		/// <summary>
		/// Returns true when the document has just been created and has not yet been saved
		/// </summary>
		public bool IsNewDocument {
			get { return isNewDocument; }
			protected set {
				if (value != isNewDocument) {
					isNewDocument = value;
					IsNewDocumentChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Raised when the IsReadyOnly property changes
		/// </summary>
		public event EventHandler IsNewDocumentChanged;

		/// <summary>
		/// Returns the current editing status of the controller.
		/// </summary>
		public PropertyBag GetDocumentStatus ()
		{
			return OnGetDocumentStatus ();
		}

		/// <summary>
		/// Override to return the current editing status of the controller.
		/// </summary>
		protected virtual PropertyBag OnGetDocumentStatus ()
		{
			return new PropertyBag ();
		}

		/// <summary>
		/// Returs true if the document has been modified
		/// </summary>
		public bool IsDirty {
			get { return isDirty; }
			protected set {
				if (value != isDirty) {
					isDirty = value;
					DirtyChanged?.Invoke (this, EventArgs.Empty);
				}
			}

		/// <summary>
		/// Raised when the IsDirty property changes
		/// </summary>
		public event EventHandler DirtyChanged;

		/// <summary>
		/// Returs true if the document is read-only (it can't be saved)
		/// </summary>
		public bool IsReadyOnly {
			get { return isReadOnly; }
			protected set {
				if (value != isReadOnly) {
					isReadOnly = value;
					ReadOnlyChanged?.Invoke (this, EventArgs.Empty);
				}
			}

		/// <summary>
		/// Raised when the IsReadyOnly property changes
		/// </summary>
		public event EventHandler ReadOnlyChanged;
	}
}
