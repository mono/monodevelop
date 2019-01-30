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

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// The TextEditor object needs to be available through BaseViewContent.GetContent therefore we need to insert a 
	/// decorator in between.
	/// </summary>
	class TextEditorViewContent : FileDocumentController, ICommandRouter
	{
		readonly TextEditor textEditor;
		readonly ITextEditorImpl textEditorImpl;

		MonoDevelop.Projects.Policies.PolicyContainer policyContainer;

		public TextEditorViewContent (TextEditor textEditor, ITextEditorImpl textEditorImpl)
		{
			if (textEditor == null)
				throw new ArgumentNullException (nameof (textEditor));
			if (textEditorImpl == null)
				throw new ArgumentNullException (nameof (textEditorImpl));
			this.textEditor = textEditor;
			this.textEditorImpl = textEditorImpl;
			textEditorImpl.DocumentController = this;
			this.textEditor.MimeTypeChanged += UpdateTextEditorOptions;
			DefaultSourceEditorOptions.Instance.Changed += UpdateTextEditorOptions;
			textEditorImpl.DirtyChanged += ViewContent_DirtyChanged; 
			textEditor.Options = DefaultSourceEditorOptions.Instance.Create ();
		}

		protected override Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			return base.OnInitialize (modelDescriptor, status);
		}

		protected override void OnFileNameChanged ()
		{
			base.OnFileNameChanged ();
			if (FilePath != textEditorImpl.ContentName && !string.IsNullOrEmpty (textEditorImpl.ContentName))
				AutoSave.RemoveAutoSaveFile (textEditorImpl.ContentName);
			textEditor.FileName = FilePath;
			if (this.WorkbenchWindow?.Document != null)
				textEditor.InitializeExtensionChain (this.WorkbenchWindow.Document.DocumentContext);
			UpdateTextEditorOptions (null, null);
		}

		void ViewContent_DirtyChanged (object sender, EventArgs e)
		{
			IsDirty = textEditorImpl.IsDirty;
		}

		void HandleDirtyChanged (object sender, EventArgs e)
		{
			IsDirty = textEditorImpl.IsDirty;
			InformAutoSave ();
		}

		void HandleTextChanged (object sender, TextChangeEventArgs e)
		{
			InformAutoSave ();
		}

		void UpdateTextEditorOptions (object sender, EventArgs e)
		{
			UpdateStyleParent (Owner, textEditor.MimeType).Ignore ();
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

				autoSaveTask = AutoSave.InformAutoSaveThread (textEditor.CreateSnapshot (), textEditor.FileName, IsDirty);
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

		async Task UpdateStyleParent (WorkspaceObject owner, string mimeType)
		{
			var styleParent = owner as IPolicyProvider;
			RemovePolicyChangeHandler ();

			if (string.IsNullOrEmpty (mimeType))
				mimeType = "text/plain";

			var mimeTypes = IdeApp.DesktopService.GetMimeTypeInheritanceChain (mimeType);

			if (styleParent != null)
				policyContainer = styleParent.Policies;
			else
				policyContainer = MonoDevelop.Projects.Policies.PolicyService.DefaultPolicies;
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);
			policyContainer.PolicyChanged += HandlePolicyChanged;
			((DefaultSourceEditorOptions)textEditor.Options).UpdateStylePolicy (currentPolicy);

			var context = await EditorConfigService.GetEditorConfigContext (textEditor.FileName, default (CancellationToken));
			if (context == null)
				return;
			((DefaultSourceEditorOptions)textEditor.Options).SetContext (context);
		}

		void HandlePolicyChanged (object sender, MonoDevelop.Projects.Policies.PolicyChangedEventArgs args)
		{
			var mimeTypes = IdeApp.DesktopService.GetMimeTypeInheritanceChain (textEditor.MimeType);
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);
			((DefaultSourceEditorOptions)textEditor.Options).UpdateStylePolicy (currentPolicy);
		}

		void CancelDocumentParsedUpdate ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}

		CancellationTokenSource src = new CancellationTokenSource ();

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
			var res = await TextFileUtility.GetTextAsync (await FileModel.GetContent ());
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

		protected override void OnDiscardChanges ()
		{
			if (autoSaveTask != null)
				autoSaveTask.Wait (TimeSpan.FromSeconds (5));
			RemoveAutoSaveTimer ();
			if (!string.IsNullOrEmpty (textEditorImpl.ContentName))
				AutoSave.RemoveAutoSaveFile (textEditorImpl.ContentName);
			textEditorImpl.DiscardChanges ();
		}

		protected override void OnOwnerChanged ()
		{
			base.OnOwnerChanged ();
			textEditorImpl.Project = Owner as Project;
			UpdateTextEditorOptions (null, null);
		}

		protected override ProjectReloadCapability OnGetProjectReloadCapability ()
		{
			return textEditorImpl.ProjectReloadCapability;
		}

		protected override IEnumerable<object> OnGetContents (Type type)
		{
			foreach (var r in base.OnGetContents (type))
				yield return r;
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

		protected override async Task<Control> OnGetViewControlAsync (CancellationToken token, DocumentViewContent view)
		{
			if (IsNewDocument)
				await LoadNew ();
			else
				await Load (false);

			IsDirty = textEditorImpl.IsDirty;
			return textEditor;
		}

		public string TabPageLabel {
			get { return GettextCatalog.GetString ("Source"); }
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
			
			base.Dispose ();

			isDisposed = true;
			EditorConfigService.RemoveEditConfigContext (textEditor.FileName).Ignore ();
			CancelDocumentParsedUpdate ();
			textEditorImpl.DirtyChanged -= HandleDirtyChanged;
			textEditor.MimeTypeChanged -= UpdateTextEditorOptions;
			textEditor.TextChanged -= HandleTextChanged;
			textEditorImpl.DirtyChanged -= ViewContent_DirtyChanged; ;

			DefaultSourceEditorOptions.Instance.Changed -= UpdateTextEditorOptions;
			RemovePolicyChangeHandler ();
			RemoveAutoSaveTimer ();
			textEditor.Dispose ();
		}

		#endregion

		#region ICommandRouter implementation

		object ICommandRouter.GetNextCommandTarget ()
		{
			return textEditor;
		}

		#endregion
	


	}
}