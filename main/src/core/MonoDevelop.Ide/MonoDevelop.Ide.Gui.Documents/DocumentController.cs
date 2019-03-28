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
using MonoDevelop.Projects;
using MonoDevelop.Components;
using Mono.Addins;
using MonoDevelop.Ide.Extensions;
using System.Threading;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// A controller is a class that implements the logic for loading,
	/// displaying and interacting with the contents of a document.
	/// </summary>
	public abstract class DocumentController : IDisposable, ICommandDelegator
	{
		internal const string DocumentControllerExtensionsPath = "/MonoDevelop/Ide/DocumentControllerExtensions";

		DocumentModel model;
		WorkspaceObject owner;
		ExtensionContext extensionContext;
		DocumentView viewItem;
		DocumentView finalViewItem;
		ServiceProvider serviceProvider;

		bool initialized;
		bool hasUnsavedChanges;
		bool isReadOnly;
		bool isNewDocument;
		string documentTitle;
		string documentIconId;
		string tabPageLabel;
		string accessibilityDescription;
		bool loaded;
		Xwt.Drawing.Image documentIcon;
		bool usingIconId;
		Properties lastKnownStatus;

		internal IWorkbenchWindow WorkbenchWindow { get; set; }

		ExtensionChain extensionChain;
		DocumentControllerExtension itemExtension;
		private bool showNotification;

		/// <summary>
		/// Raised when the IsReadyOnly property changes
		/// </summary>
		public event EventHandler IsNewDocumentChanged;

		/// <summary>
		/// Raised when the Model property changes
		/// </summary>
		public event EventHandler ModelChanged;

		/// <summary>
		/// Raised when the owner of the controller changes
		/// </summary>
		public event EventHandler OwnerChanged;

		/// <summary>
		/// Raised when the DocumentTitle property changes
		/// </summary>
		public event EventHandler DocumentTitleChanged;

		/// <summary>
		/// Raised when the DocumentIconId property changes
		/// </summary>
		public event EventHandler DocumentIconChanged;

		/// <summary>
		/// Raised when the TabPageLabel property changes
		/// </summary>
		public event EventHandler TabPageLabelChanged;

		/// <summary>
		/// Raised when the IsDirty property changes
		/// </summary>
		public event EventHandler HasUnsavedChangesChanged;

		/// <summary>
		/// Raised when the content of the document changes, which means that GetContent() may return different content objects
		/// </summary>
		public event EventHandler ContentChanged;

		/// <summary>
		/// Raised when the IsReadyOnly property changes
		/// </summary>
		public event EventHandler IsReadOnlyChanged;

		/// <summary>
		/// Raised when the AccessibilityDescription property changes
		/// </summary>
		public event EventHandler AccessibilityDescriptionChanged;

		/// <summary>
		/// Raised when the ShowNotification property changes
		/// </summary>
		public event EventHandler ShowNotificationChanged;

		/// <summary>
		/// Gets or sets the service provider used to create this controller
		/// </summary>
		/// <value>The service provider.</value>
		internal protected ServiceProvider ServiceProvider {
			get { return serviceProvider ?? Runtime.ServiceProvider; }
			set { serviceProvider = value; }
		}

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
			get {
				CheckInitialized ();
				return model;
			}
			set {
				if (value == null)
					throw new ArgumentNullException ();
				if (!CanAssignModel (value.GetType ()))
					throw new InvalidOperationException ("Model can't be assigned");
				SetModel (value);
			}
		}

		/// <summary>
		/// Returs true if the document has been modified and the changes are not yet saved
		/// </summary>
		public bool HasUnsavedChanges {
			get {
				CheckInitialized ();
				return hasUnsavedChanges;
			}
			set {
				if (value != hasUnsavedChanges) {
					hasUnsavedChanges = value;
					HasUnsavedChangesChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Returs true if the document is read-only
		/// </summary>
		/// <remarks>
		/// For example, a file that is read-only on disk. The Save command may still be ofered, since it may
		/// be possible to save changes to another file.
		/// </remarks>
		public bool IsReadOnly {
			get {
				CheckInitialized ();
				return isReadOnly || IsViewOnly;
			}
			protected set {
				if (value != isReadOnly) {
					isReadOnly = value;
					IsReadOnlyChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Owner project or solution item
		/// </summary>
		/// <value>The owner.</value>
		public WorkspaceObject Owner {
			get {
				CheckInitialized ();
				return owner;
			}
			set {
				if (value != owner) {
					owner = value;

					NotifyOwnerChanged ();
					RefreshExtensions ().Ignore ();
				}
			}
		}

		/// <summary>
		/// Title shown in the document tab
		/// </summary>
		public string DocumentTitle {
			get {
				CheckInitialized ();
				return documentTitle;
			}
			set {
				if (value != documentTitle) {
					documentTitle = value;
					DocumentTitleChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// When set to true, the document tab will show a notification marker 
		/// </summary>
		public bool ShowNotification {
			get {
				CheckInitialized ();
				return showNotification;
			}
			set {
				if (showNotification != value) {
					showNotification = value;
					ShowNotificationChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}


		/// <summary>
		/// Icon of the document tab
		/// </summary>
		/// <value>The stock icon identifier.</value>
		protected string DocumentIconId {
			get {
				CheckInitialized ();
				return documentIconId;
			}
			set {
				if (value != documentIconId || !usingIconId) {
					documentIconId = value;
					documentIcon = ImageService.GetIcon (value);
					usingIconId = true;
					DocumentIconChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Accessibility description for the document or view tab
		/// </summary>
		public string AccessibilityDescription {
			get {
				CheckInitialized ();
				return accessibilityDescription;
			}
			set {
				if (value != documentTitle) {
					accessibilityDescription = value;
					AccessibilityDescriptionChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Icon of the document tab
		/// </summary>
		/// <value>The stock icon identifier.</value>
		public Xwt.Drawing.Image DocumentIcon {
			get {
				CheckInitialized ();
				return documentIcon;
			}
			set {
				if (value != documentIcon) {
					documentIcon = value;
					documentIconId = null;
					usingIconId = false;
					DocumentIconChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Title shown in the view tab, when a document shows more than one view
		/// </summary>
		public string TabPageLabel {
			get {
				CheckInitialized ();
				if (tabPageLabel == null) {
					switch (Role) {
					case DocumentControllerRole.Preview: return GettextCatalog.GetString ("Preview");
					case DocumentControllerRole.VisualDesign: return GettextCatalog.GetString ("Designer");
					case DocumentControllerRole.Tool: return GettextCatalog.GetString ("Tools");
					}
					return GettextCatalog.GetString ("Source");
				}
				return tabPageLabel;
			}
			set {
				if (value != tabPageLabel) {
					tabPageLabel = value;
					TabPageLabelChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Returns true when the document has just been created and has not yet been saved
		/// </summary>
		public bool IsNewDocument {
			get {
				CheckInitialized ();
				return isNewDocument;
			}
			protected set {
				if (value != isNewDocument) {
					isNewDocument = value;
					IsNewDocumentChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Gets the capability of this view for being reassigned a project
		/// </summary>
		public ProjectReloadCapability ProjectReloadCapability {
			get {
				CheckInitialized ();
				if (extensionChain != null)
					return extensionChain.GetAllExtensions ().OfType<DocumentControllerExtension> ().Select (c => c.ProjectReloadCapability).Append (OnGetProjectReloadCapability ()).Min ();
				else
					return OnGetProjectReloadCapability ();
			}
		}

		public Document Document { get; internal set; }

		/// <summary>
		/// Returns true if the controller only display data, but it doesn't allow to modify it.
		/// The main side effect is that the save command won't be available.
		/// </summary>
		public bool IsViewOnly {
			get {
				CheckInitialized ();
				return ControllerIsViewOnly;
			}
		}

		protected virtual bool ControllerIsViewOnly => false;

		internal FilePath OriginalContentName { get; set; }

		internal bool Initialized => initialized;

		/// <summary>
		/// Initializes the controller
		/// </summary>
		/// <returns>The initialize.</returns>
		/// <param name="status">Status of the controller/view, returned by a GetDocumentStatus() call from a previous session</param>
		public async Task Initialize (ModelDescriptor modelDescriptor, Properties status = null)
		{
			if (initialized)
				throw new InvalidOperationException ("Already initialized");
			initialized = true;

			lastKnownStatus = status ?? new Properties ();
			var controllerStatus = lastKnownStatus.Get<Properties> ("Controller");

			await OnInitialize (modelDescriptor, controllerStatus);

			extensionContext = CreateExtensionContext ();
			await InitializeExtensionChain ();
		}

		public void Dispose ()
		{
			OnDispose ();
			extensionChain?.Dispose ();
		}

		/// <summary>
		/// Tries to reuse this controler to display the content identified by the provide descriptor.
		/// </summary>
		/// <returns><c>true</c>, if the controller could be used to display the content, <c>false</c> otherwise.</returns>
		/// <param name="modelDescriptor">Model descriptor.</param>
		public bool TryReuseDocument (ModelDescriptor modelDescriptor)
		{
			CheckInitialized ();
			return OnTryReuseDocument (modelDescriptor);
		}

		/// <summary>
		/// Checks if this controller supports the assignement of a new model
		/// </summary>
		/// <returns><c>true</c>, if a model of the provided type can be assigned.</returns>
		/// <param name="type">A type of model</param>
		public bool CanAssignModel (Type type)
		{
			CheckInitialized ();
			return OnCanAssignModel (type);
		}

		public async Task Save ()
		{
			CheckInitialized ();
			await itemExtension.OnSave ();
			IsNewDocument = false;
		}

		public async Task Reload ()
		{
			CheckInitialized ();
			var status = GetDocumentStatus ();
			await OnLoad (true);
			await RefreshExtensions ();
			SetDocumentStatus (status);
		}

		const string ExtensionPropertyPrefix = "Extension_";

		/// <summary>
		/// Returns the current editing status of the controller.
		/// </summary>
		public Properties GetDocumentStatus ()
		{
			CheckInitialized ();

			// The lastKnownStatus instance is being reused here. It may contain status from
			// extensions that have been disposed, but that's ok, the status will be restored
			// if the extensions are loaded again

			var status = OnGetDocumentStatus ();
			lastKnownStatus.Set ("Controller", status);
			if (extensionChain != null) {
				foreach (var ex in extensionChain.GetAllExtensions ().OfType<DocumentControllerExtension> ()) {
					var s = ex.GetDocumentStatus ();
					lastKnownStatus.Set (ExtensionPropertyPrefix + ex.Id, s);
				}
			}
			return lastKnownStatus;
		}

		/// <summary>
		/// Sets the current editing status of the controller.
		/// </summary>
		public void SetDocumentStatus (Properties properties)
		{
			CheckInitialized ();
			lastKnownStatus = properties;
			var controllerProps = properties.Get<Properties> ("Controller");
			if (controllerProps != null)
				OnSetDocumentStatus (controllerProps);
			if (extensionChain != null) {
				foreach (var ext in extensionChain.GetAllExtensions ().OfType<DocumentControllerExtension> ()) {
					var s = GetExtensionStatus (ext);
					if (s != null)
						ext.SetDocumentStatus (s);
				}
			}
		}

		Properties GetExtensionStatus (DocumentControllerExtension ext)
		{
			return lastKnownStatus?.Get<Properties> (ExtensionPropertyPrefix + ext.Id);
		}

		void StoreExtensionStatus (DocumentControllerExtension ext)
		{
			var s = ext.GetDocumentStatus ();
			lastKnownStatus.Set (ExtensionPropertyPrefix + ext.Id, s);
		}

		public object GetContent (Type type)
		{
			return GetContents (type).FirstOrDefault ();
		}

		public T GetContent<T> () where T : class
		{
			return GetContents<T> ().FirstOrDefault ();
		}

		public IEnumerable<T> GetContents<T> () where T : class
		{
			return GetContents (typeof (T)).Cast<T> ();
		}

		public IEnumerable<object> GetContents (Type type)
		{
			CheckInitialized ();
			var contents = OnGetContents (type);

			if (extensionChain != null) {
				foreach (var ext in extensionChain.GetAllExtensions ().OfType<DocumentControllerExtension> ())
					contents = contents.Concat (ext.GetContents (type));
			}
			return contents;
		}

		/// <summary>
		/// Gets the view that will show the content of the document.
		/// </summary>
		/// <returns>The view</returns>
		public async Task<DocumentView> GetDocumentView ()
		{
			CheckInitialized ();
			if (finalViewItem == null) {
				try {
					finalViewItem = await itemExtension.OnInitializeView ();
				} catch (Exception ex) {
					LoggingService.LogError ("View container initialization failed", ex);
					finalViewItem = new DocumentViewContent (() => (Control)null);
				}
				NotifyContentChanged ();
			}
			return finalViewItem;
		}

		/// <summary>
		/// Ensures that this controller has the extensions it requires according to its current state
		/// </summary>
		/// <remarks>
		/// This method will load new extensions that this controller supports and will unload extensions that are not supported anymore.
		/// The set of extensions that a controller supports may change over time, depending on the status of the controller.
		/// </remarks>
		public async Task RefreshExtensions ()
		{
			CheckInitialized ();
			if (extensionChain == null)
				return;

			bool extensionsChanged = false;

			// First of all look for new extensions that should be attached

			// Get the list of nodes for which an extension has been created

			var allExtensions = extensionChain.GetAllExtensions ().OfType<DocumentControllerExtension> ().ToList ();
			var loadedNodes = allExtensions.Where (ex => ex.SourceExtensionNode != null)
				.Select (ex => ex.SourceExtensionNode.Data.NodeId).ToList ();

			var newExtensions = new List<DocumentControllerExtension> ();

			TypeExtensionNode<ExportDocumentControllerExtensionAttribute> lastAddedNode = null;

			// Ensure conditions are re-evaluated.
			extensionContext = CreateExtensionContext ();

			var documentControllerService = await Runtime.GetService<DocumentControllerService> ();

			using (extensionChain.BatchModify ()) {
				foreach (var node in documentControllerService.GetModelExtensions (extensionContext)) {
					// If the node already generated an extension, skip it
					if (loadedNodes.Contains (node.Data.NodeId)) {
						lastAddedNode = node;
						loadedNodes.Remove (node.Data.NodeId);
						continue;
					}

					// Maybe the node can now generate an extension for this project
					if (node.Data.CanHandle (this)) {
						var ext = (DocumentControllerExtension)node.CreateInstance ();
						if (await ext.SupportsController (this)) {
							ext.SourceExtensionNode = node;
							newExtensions.Add (ext);
							if (lastAddedNode != null) {
								// There is an extension before this one. Find it and add the new extension after it.
								var prevExtension = allExtensions.FirstOrDefault (ex => ex.SourceExtensionNode?.Data.NodeId == lastAddedNode.Data.NodeId && !string.IsNullOrEmpty (lastAddedNode.Data.NodeId));
								extensionChain.AddExtension (ext, prevExtension);
							} else
								extensionChain.AddExtension (ext);
							await ext.Init (this, GetExtensionStatus (ext));
							extensionsChanged = true;
						} else
							ext.Dispose ();
					}
				}

				// Now dispose extensions that are not supported anymore

				foreach (var ext in allExtensions) {
					if (!await ext.SupportsController (this)) {
						// Store the status, it will be restored if the extension is reactivated
						StoreExtensionStatus (ext);
						ext.Dispose ();
						extensionsChanged = true;
					}
				}

				if (loadedNodes.Any ()) {
					foreach (var ext in allExtensions.Where (ex => ex.SourceExtensionNode != null)) {
						if (loadedNodes.Contains (ext.SourceExtensionNode.Data.NodeId)) {
							ext.Dispose ();
							loadedNodes.Remove (ext.SourceExtensionNode.Data.NodeId);
							extensionsChanged = true;
						}
					}
				}
			}

			foreach (var e in newExtensions)
				e.OnExtensionChainCreated ();

			if (extensionsChanged)
				NotifyContentChanged ();
		}



		// ****** Private methods ******



		protected void SetModel (DocumentModel value)
		{
			if (value != model) {
				if (model != null)
					model.Changed -= Model_Changed;
				var oldModel = model;
				model = value;
				if (model != null)
					model.Changed += Model_Changed;
				OnModelChanged (oldModel, model);
				RefreshExtensions ().Ignore ();
				ModelChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		void Model_Changed (object sender, EventArgs e)
		{
		}

		async Task InitializeExtensionChain ()
		{
			// Create an initial empty extension chain. This avoid crashes in case a call to SupportsObject ends
			// calling methods from the extension

			var tempExtensions = new List<DocumentControllerExtension> { new DefaultControllerExtension () };
			extensionChain = ExtensionChain.Create (tempExtensions.ToArray ());
			foreach (var e in tempExtensions)
				await e.Init (this, null);

			// Collect extensions that support this object

			var documentControllerService = await Runtime.GetService<DocumentControllerService> ();

			var extensions = new List<DocumentControllerExtension> ();
			foreach (var node in documentControllerService.GetModelExtensions (extensionContext)) {
				if (node.Data.CanHandle (this)) {
					var ext = node.CreateInstance ();
					if (!(ext is DocumentControllerExtension controllerExtension))
						throw new InvalidOperationException ("Invalid document controller extension type: " + ext.GetType ());
					if (await controllerExtension.SupportsController (this)) {
						controllerExtension.SourceExtensionNode = node;
						extensions.Add (controllerExtension);
					} else
						controllerExtension.Dispose ();
				}
			}

			extensionChain.Dispose ();

			// Now create the final extension chain

			extensions.Reverse ();
			var defaultExts = new List<DocumentControllerExtension> { new DefaultControllerExtension () };
			defaultExts.Reverse ();
			extensions.AddRange (defaultExts);
			extensionChain = ExtensionChain.Create (extensions.ToArray ());
			extensionChain.SetDefaultInsertionPosition (defaultExts.FirstOrDefault ());

			foreach (var e in extensions)
				await e.Init (this, GetExtensionStatus (e));

			itemExtension = extensionChain.GetExtension<DocumentControllerExtension> ();

			foreach (var e in extensions)
				e.OnExtensionChainCreated ();

			OnExtensionChainCreated ();

			if (extensions.Count - defaultExts.Count > 0)
				NotifyContentChanged ();
		}

		protected virtual void OnExtensionChainCreated ()
		{
		}

		ExtensionContext CreateExtensionContext ()
		{
			return AddinManager.CreateExtensionContext ();
		}

		public ExtensionContext ExtensionContext {
			get { return extensionContext ?? AddinManager.AddinEngine; }
		}

		internal Task EnsureLoaded ()
		{
			if (!loaded) {
				loaded = true;
				return OnLoad (false);
			}
			return Task.CompletedTask;
		}

		void UpdateContentExtensions ()
		{
			var pathDoc = GetContent<IPathedDocument> ();
			if (pathDoc != null && viewItem is DocumentViewContent content)
				content.ShowPathBar (pathDoc);
		}

		public void NotifyContentChanged ()
		{
			if (!initialized)
				return;
			try {
				OnContentChanged ();
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}

			if (extensionChain != null) {
				foreach (var ext in extensionChain.GetAllExtensions ().OfType<DocumentControllerExtension> ().ToList ())
					ext.RunContentChanged ();
			}
			UpdateContentExtensions ();
			ContentChanged?.Invoke (this, EventArgs.Empty);
			RefreshExtensions ().Ignore ();

		}

		internal void NotifyFocused ()
		{
			OnFocused ();
		}

		internal void NotifyUnfocused ()
		{
			OnUnfocused ();
		}

		internal void GrabFocusForView (DocumentView view)
		{
			OnGrabFocus (view);
		}

		internal void NotifyShown ()
		{
			try {
				OnContentShown ();
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}

			if (extensionChain != null) {
				foreach (var ext in extensionChain.GetAllExtensions ().OfType<DocumentControllerExtension> ()) {
					try {
						ext.OnContentShown ();
					} catch (Exception ex) {
						LoggingService.LogInternalError (ex);
					}
				}
			}
		}

		internal void NotifyHidden ()
		{
			try {
				OnContentHidden ();
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}

			if (extensionChain != null) {
				foreach (var ext in extensionChain.GetAllExtensions ().OfType<DocumentControllerExtension> ()) {
					try {
						ext.OnContentHidden ();
					} catch (Exception ex) {
						LoggingService.LogInternalError (ex);
					}
				}
			}
		}


		// ****** Virtual and protected methods ******

		/// <summary>
		/// Gets the document view created for this controller, or null if it has not yet been initialized
		/// </summary>
		/// <value>The document view.</value>
		protected DocumentView DocumentView => viewItem;

		/// <summary>
		/// Initializes the controller
		/// </summary>
		/// <returns>The initialize.</returns>
		/// <param name="status">Status of the controller/view, returned by a GetDocumentStatus() call from a previous session. It can be null if status was not available.</param>
		protected virtual Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			return Task.CompletedTask;
		}

		async Task<DocumentView> InternalInitializeView ()
		{
			if (viewItem == null) {
				try {
					viewItem = await OnInitializeView ();
				} catch (Exception ex) {
					LoggingService.LogError ("View container initialization failed", ex);
					viewItem = new DocumentViewContent (() => (Control)null);
				}
				viewItem.SourceController = this;
			}
			return viewItem;
		}

		/// <summary>
		/// Creates and initializes the view for this controller
		/// </summary>
		/// <returns>The new view.</returns>
		protected virtual Task<DocumentView> OnInitializeView ()
		{
			return Task.FromResult<DocumentView> (new DocumentViewContent (OnGetViewControlCallback));
		}

		Task<Control> OnGetViewControlCallback (CancellationToken token)
		{
			return OnGetViewControlAsync (token, (DocumentViewContent)viewItem);
		}

		/// <summary>
		/// Called to (asynchronously) get the control that the main view is going to show.
		/// </summary>
		/// <returns>The control</returns>
		/// <param name="token">Cancellation token for the async operation</param>
		/// <param name="view">View to which the control will be added.</param>
		protected virtual Task<Control> OnGetViewControlAsync (CancellationToken token, DocumentViewContent view)
		{
			return Task.FromResult (OnGetViewControl (view));
		}

		/// <summary>
		/// Called to get the control that the main view is going to show.
		/// </summary>
		/// <returns>The control</returns>
		/// <param name="view">View to which the control will be added.</param>
		protected virtual Control OnGetViewControl (DocumentViewContent view)
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Called when a new DocumentModel is assigned to this controller.
		/// It is also called when the controller is set to null.
		/// </summary>
		protected virtual void OnModelChanged (DocumentModel oldModel, DocumentModel newModel)
		{
			if (Model != null) {
				IsNewDocument = Model.IsNew;
				HasUnsavedChanges = Model.HasUnsavedChanges;
				Model.HasUnsavedChangesChanged += Model_HasUnsavedChangesChanged;
			}
			if (oldModel != null)
				oldModel.HasUnsavedChangesChanged -= Model_HasUnsavedChangesChanged;
		}

		void Model_HasUnsavedChangesChanged (object sender, EventArgs e)
		{
			if (Model != null)
				HasUnsavedChanges = Model.HasUnsavedChanges;
		}

		/// <summary>
		/// Called to check if this controller supports being assigned a model of the provided type
		/// </summary>
		/// <returns><c>true</c>, if can assign model was oned, <c>false</c> otherwise.</returns>
		/// <param name="type">Type.</param>
		protected virtual bool OnCanAssignModel (Type type)
		{
			return false;
		}

		void NotifyOwnerChanged ()
		{
			try {
				OnOwnerChanged ();
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}

			if (extensionChain != null) {
				foreach (var ext in extensionChain.GetAllExtensions ().OfType<DocumentControllerExtension> ().ToList ())
					ext.RunOwnerChanged ();
			}

			OwnerChanged?.Invoke (this, EventArgs.Empty);
		}

		protected virtual void OnOwnerChanged ()
		{
		}

		protected virtual Task OnLoad (bool reloading)
		{
			return Model.Reload () ?? Task.CompletedTask;
		}

		internal Task DoSave ()
		{
			return OnSave ();
		}

		/// <summary>
		/// Saves the document. If the controller has a model, the default implementation will save the model.
		/// </summary>
		protected virtual async Task OnSave ()
		{
			if (Model != null)
				await Model.Save ();
		}

		/// <summary>
		/// Gets the capability of this view for being reassigned a project
		/// </summary>
		internal protected virtual ProjectReloadCapability OnGetProjectReloadCapability ()
		{
			return ProjectReloadCapability.None;
		}

		/// <summary>
		/// Tries to reuse this controler to display the content identified by the provide descriptor.
		/// </summary>
		/// <returns><c>true</c>, if the controller could be used to display the content, <c>false</c> otherwise.</returns>
		/// <param name="modelDescriptor">Model descriptor.</param>
		protected virtual bool OnTryReuseDocument (ModelDescriptor modelDescriptor)
		{
			return false;
		}

		/// <summary>
		/// Override to return the current editing status of the controller.
		/// </summary>
		protected virtual Properties OnGetDocumentStatus ()
		{
			return null;
		}

		/// <summary>
		/// Override to set the current editing status of the controller.
		/// </summary>
		protected virtual void OnSetDocumentStatus (Properties properties)
		{
		}

		protected virtual object OnGetContent (Type type)
		{
			if (type.IsAssignableFrom (GetType()))
				return this;
			return null;
		}

		protected virtual IEnumerable<object> OnGetContents (Type type)
		{
			var c = OnGetContent (type);
			if (c != null)
				yield return c;
		}

		protected virtual void OnContentChanged ()
		{
		}

		protected virtual void OnDispose ()
		{
			if (Model != null)
				Model.Dispose ();
		}

		public IEnumerable<FilePath> GetDocumentFiles ()
		{
			return OnGetDocumentFiles ();
		}

		protected virtual IEnumerable<FilePath> OnGetDocumentFiles ()
		{
			if (Model is FileModel file)
				yield return file.FilePath;
		}

		/// <summary>
		/// Called when the content of this controller is made visible in the shell.
		/// </summary>
		protected virtual void OnContentShown ()
		{
		}

		/// <summary>
		/// Called when the content of this controller is hidden in the shell
		/// </summary>
		protected virtual void OnContentHidden ()
		{
		}

		/// <summary>
		/// Called when the view of this controller got the mouse and keyboard focus
		/// </summary>
		protected virtual void OnFocused ()
		{
		}

		/// <summary>
		/// Called when the view of this controller lost the mouse and keyboard focus
		/// </summary>
		protected virtual void OnUnfocused ()
		{
		}

		/// <summary>
		/// Called when the view of this controller needs to grab the focus (usually because it has
		/// become the active document).
		/// </summary>
		/// <param name="view">View.</param>
		protected virtual void OnGrabFocus (DocumentView view)
		{
			// Use the default GrabFocus implementation in DocumentView.
			view.DefaultGrabFocus ();
		}

		protected bool CheckInitialized ()
		{
			if (!initialized)
				throw new InvalidOperationException ("Document model not initialized");
			return true;
		}

		object ICommandDelegator.GetDelegatedCommandTarget ()
		{
			return itemExtension;
		}

		class DefaultControllerExtension : DocumentControllerExtension
		{
			internal protected override Task<DocumentView> OnInitializeView ()
			{
				return Controller.InternalInitializeView ();
			}

			public override Task OnSave ()
			{
				return Controller.DoSave ();
			}
		}
	}
}
