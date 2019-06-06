//
// TextEditorViewContent.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// The TextEditor object needs to be available through BaseViewContent.GetContent therefore we need to insert a 
	/// decorator in between.
	/// </summary>
	class TextEditorViewContent : FileDocumentController, ICommandRouter
	{
		TextEditor textEditor;
		ITextEditorImpl textEditorImpl;
		DocumentContext documentContext;
		FileDescriptor fileDescriptor;

		MonoDevelop.Projects.Policies.PolicyContainer policyContainer;

		protected override Type FileModelType => typeof (TextBufferFileModel);

		public TextEditorViewContent ()
		{
		}

		protected override async Task OnLoad (bool reloading)
		{
			await base.OnLoad (reloading);
			if (isDisposed || textEditor == null)
				return;
 			textEditor.SetNotDirtyState ();
			textEditor.IsDirty = false;
		}

		protected override async Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			fileDescriptor = modelDescriptor as FileDescriptor;
			await base.OnInitialize (modelDescriptor, status);
			Encoding = fileDescriptor.Encoding;
		}

		protected override async Task<Control> OnGetViewControlAsync (CancellationToken token, DocumentViewContent view)
		{
			if (textEditor == null) {
				await Model.Load (); 
				var editor = TextEditorFactory.CreateNewEditor ((TextBufferFileModel)Model);
				var impl = editor.Implementation;

				await Init (editor, impl);
				HasUnsavedChanges = impl.IsDirty;
				await UpdateStyleParent (Owner, editor.MimeType, token);

				// Editor extensions can provide additional content
				NotifyContentChanged ();
			}
			return textEditor;
		}

		Task Init (TextEditor editor, ITextEditorImpl editorImpl)
		{
			this.textEditor = editor;
			textEditor.FileName = FilePath;
			this.textEditorImpl = editorImpl;
			editorImpl.DocumentController = this;
			this.textEditor.MimeTypeChanged += UpdateTextEditorOptions;
			DefaultSourceEditorOptions.Instance.Changed += UpdateTextEditorOptions;
			editorImpl.DirtyChanged += ViewContent_DirtyChanged;
			editor.Options = DefaultSourceEditorOptions.Instance.Create ();
			this.TabPageLabel = GettextCatalog.GetString ("Source");

			if (documentContext != null)
				textEditor.InitializeExtensionChain (documentContext);

			if (IsNewDocument)
				return LoadNew ();
			else
				return Load (false);
		}

		protected override void OnModelChanged (DocumentModel oldModel, DocumentModel newModel)
		{
			// Don't call base since we are not interested in binding the HasUnsavedChanges change event from the model
			if (Model != null)
				IsNewDocument = Model.IsNew;
		}

		protected override void OnContentChanged ()
		{
			if (documentContext == null) {
				var context = GetContent<DocumentContext> ();
				if (context != null) {
					documentContext = context;
					if (textEditor != null)
						textEditor.InitializeExtensionChain (documentContext);
				}
			}
			// Calling base class after initializing the editor extension chain
			// so that it picks additional content from the extension
			base.OnContentChanged ();
		}

		protected override void OnFileNameChanged ()
		{
			base.OnFileNameChanged ();
			if (textEditor != null) {
				if (FilePath != textEditorImpl.ContentName && !string.IsNullOrEmpty (textEditorImpl.ContentName))
					AutoSave.RemoveAutoSaveFile (textEditorImpl.ContentName);
				textEditor.FileName = FilePath; // TOTEST: VSTS #770920
				if (documentContext != null)
					textEditor.InitializeExtensionChain (documentContext);
				UpdateTextEditorOptions (null, null);
			}
		}

		void ViewContent_DirtyChanged (object sender, EventArgs e)
		{
			HasUnsavedChanges = textEditorImpl.IsDirty;
		}

		void HandleDirtyChanged (object sender, EventArgs e)
		{
			HasUnsavedChanges = textEditorImpl.IsDirty;
			InformAutoSave ();
		}

		void HandleTextChanged (object sender, TextChangeEventArgs e)
		{
			InformAutoSave ();
		}

		CancellationTokenSource editorOptionsUpdateCancellationSource;
		void UpdateTextEditorOptions (object sender, EventArgs e)
		{
			editorOptionsUpdateCancellationSource?.Cancel ();
			editorOptionsUpdateCancellationSource = new CancellationTokenSource ();
			UpdateStyleParent (Owner, textEditor.MimeType, editorOptionsUpdateCancellationSource.Token).Ignore ();
		}

		uint autoSaveTimer = 0;
		Task autoSaveTask;
		void InformAutoSave ()
		{
			if (isDisposed)
				return;
			RemoveAutoSaveTimer ();
			autoSaveTimer = GLib.Timeout.Add (500, delegate {
				autoSaveTimer = 0;
				if (autoSaveTask != null && !autoSaveTask.IsCompleted)
					return false;

				autoSaveTask = AutoSave.InformAutoSaveThread (textEditor.CreateSnapshot (), textEditor.FileName, HasUnsavedChanges);
				return false;
			});
		}


		void RemoveAutoSaveTimer ()
		{
			if (autoSaveTimer == 0)
				return;
			GLib.Source.Remove (autoSaveTimer);
			autoSaveTimer = 0;
		}

		void RemovePolicyChangeHandler ()
		{
			if (policyContainer != null)
				policyContainer.PolicyChanged -= HandlePolicyChanged;
		}

		async Task UpdateStyleParent (WorkspaceObject owner, string mimeType, CancellationToken token)
		{
			var styleParent = owner as IPolicyProvider;
			RemovePolicyChangeHandler ();

			if (string.IsNullOrEmpty (mimeType))
				mimeType = "text/plain";

			var mimeTypes = IdeServices.DesktopService.GetMimeTypeInheritanceChain (mimeType);

			if (styleParent != null)
				policyContainer = styleParent.Policies;
			else
				policyContainer = MonoDevelop.Projects.Policies.PolicyService.DefaultPolicies;
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);
			policyContainer.PolicyChanged += HandlePolicyChanged;
			((DefaultSourceEditorOptions)textEditor.Options).UpdateStylePolicy (currentPolicy);

			var context = await EditorConfigService.GetEditorConfigContext (textEditor.FileName, token);
			if (context == null)
				return;
			((DefaultSourceEditorOptions)textEditor.Options).SetContext (context);
		}

		void HandlePolicyChanged (object sender, MonoDevelop.Projects.Policies.PolicyChangedEventArgs args)
		{
			var mimeTypes = IdeServices.DesktopService.GetMimeTypeInheritanceChain (textEditor.MimeType);
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);
			((DefaultSourceEditorOptions)textEditor.Options).UpdateStylePolicy (currentPolicy);
		}

		async Task RunFirstTimeFoldUpdate (string text)
		{
			if (string.IsNullOrEmpty (text)) 
				return;

			try {
				ParsedDocument parsedDocument = null;

				var foldingParser = IdeApp.TypeSystemService.GetFoldingParser (textEditor.MimeType);
				if (foldingParser != null) {
					parsedDocument = foldingParser.Parse (textEditor.FileName, text);
				} else {
					var normalParser = IdeApp.TypeSystemService.GetParser (textEditor.MimeType);
					if (normalParser != null) {
						parsedDocument = await normalParser.Parse(
							new TypeSystem.ParseOptions {
								FileName = textEditor.FileName,
								Content = new StringTextSource(text),
								Project = Owner as Project
							});
					}
				}
				if (parsedDocument != null) {
					await FoldingTextEditorExtension.UpdateFoldings (textEditor, parsedDocument, textEditor.CaretLocation, true);
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error running first time fold update", e);
			}
		}

		async Task Load (bool reloading)
		{
			textEditorImpl.DirtyChanged -= HandleDirtyChanged;
			textEditor.TextChanged -= HandleTextChanged;
			await textEditorImpl.Load (FilePath, Encoding, reloading);
			await RunFirstTimeFoldUpdate (textEditor.Text);
			textEditorImpl.InformLoadComplete ();
			textEditor.TextChanged += HandleTextChanged;
			textEditorImpl.DirtyChanged += HandleDirtyChanged;
		}

		async Task LoadNew ()
		{
			textEditor.MimeType = MimeType;
			string text = null;
			var res = await TextFileUtility.GetTextAsync (FileModel.GetContent ());
			text = textEditor.Text = res.Text;
			textEditor.Encoding = res.Encoding;
			await RunFirstTimeFoldUpdate (text);
			textEditorImpl.InformLoadComplete ();
		}

		protected override Task OnSave ()
		{
			if (!string.IsNullOrEmpty (textEditorImpl.ContentName))
				AutoSave.RemoveAutoSaveFile (textEditorImpl.ContentName);
			return textEditorImpl.Save (new FileSaveInformation (FilePath, Encoding));
		}

		protected override void OnOwnerChanged ()
		{
			base.OnOwnerChanged ();
			if (textEditorImpl != null) {
				textEditorImpl.Project = Owner as Project;
				UpdateTextEditorOptions (null, null);
			}
		}

		internal protected override ProjectReloadCapability OnGetProjectReloadCapability ()
		{
			return textEditorImpl != null ? textEditorImpl.ProjectReloadCapability : ProjectReloadCapability.Full;
		}

		protected override IEnumerable<object> OnGetContents (Type type)
		{
			foreach (var r in base.OnGetContents (type))
				yield return r;

			if (textEditorImpl != null) {
				if (type == typeof(ITextBuffer)) {
					yield return textEditor.TextView.TextBuffer;
					yield break;
				}
				if (type == typeof (ITextView)) {
					yield return textEditor.TextView;
					yield break;
				}
				if (type.IsAssignableFrom (typeof (TextEditor))) {
					yield return textEditor;
					yield break;
				}

				var ext = textEditorImpl.EditorExtension;
				while (ext != null) {
					foreach (var r in ext.OnGetContents (type))
						yield return r;
					ext = ext.Next;
				}

				foreach (var r in textEditorImpl.GetContents (type))
					yield return r;
			}
		}

		public string TabAccessibilityDescription {
			get {
				return GettextCatalog.GetString ("The main source editor");
			}
		}

		#region IDisposable implementation
		bool isDisposed;

		protected override void OnDispose ()
		{
			if (isDisposed)
				return;

			isDisposed = true;

			if (textEditorImpl != null) {

				if (autoSaveTask != null)
					autoSaveTask.Wait (TimeSpan.FromSeconds (5));
				RemoveAutoSaveTimer ();
				if (!string.IsNullOrEmpty (textEditorImpl.ContentName))
					AutoSave.RemoveAutoSaveFile (textEditorImpl.ContentName);

				EditorConfigService.RemoveEditConfigContext (textEditor.FileName).Ignore ();
				textEditorImpl.DirtyChanged -= HandleDirtyChanged;
				textEditor.MimeTypeChanged -= UpdateTextEditorOptions;
				textEditor.TextChanged -= HandleTextChanged;
				textEditorImpl.DirtyChanged -= ViewContent_DirtyChanged; ;

				DefaultSourceEditorOptions.Instance.Changed -= UpdateTextEditorOptions;
				textEditor.Dispose ();
			}
			RemovePolicyChangeHandler ();

			editorOptionsUpdateCancellationSource?.Cancel ();

			base.OnDispose ();
		}

		#endregion

		#region ICommandRouter implementation

		object ICommandRouter.GetNextCommandTarget ()
		{
			return textEditor;
		}

		#endregion

		protected override void OnGrabFocus (DocumentView view)
		{
			textEditor.GrabFocus ();
			try {
				DefaultSourceEditorOptions.SetUseAsyncCompletion (false);
			} catch (Exception e) {
				LoggingService.LogInternalError ("Error while setting up async completion.", e);
			}
		}
	}
}