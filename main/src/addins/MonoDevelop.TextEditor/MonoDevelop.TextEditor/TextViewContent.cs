//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Utilities;

using Microsoft.VisualStudio.CodingConventions;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;

#if WINDOWS
using EditorOperationsInterface = Microsoft.VisualStudio.Text.Operations.IEditorOperations3;
#else
using EditorOperationsInterface = Microsoft.VisualStudio.Text.Operations.IEditorOperations4;
#endif

namespace MonoDevelop.TextEditor
{
	abstract partial class TextViewContent<TView, TImports> : ViewContent, INavigable, ICustomCommandTarget, ICommandHandler, ICommandUpdater, IPropertyPadProvider
#if !WINDOWS
		// implementing this correctly requires IEditorOperations4
		, IZoomable
#endif
		where TView : ITextView
		where TImports : TextViewImports
	{
		readonly string mimeType;
		readonly IEditorCommandHandlerService commandService;
		readonly List<IEditorContentProvider> contentProviders;
		readonly Ide.Editor.DefaultSourceEditorOptions sourceEditorOptions;

		PolicyBag policyContainer;
		ICodingConventionContext editorConfigContext;

		public TImports Imports { get; }
		public TView TextView { get; }
		public ITextDocument TextDocument { get; }
		public ITextBuffer TextBuffer { get; }

		protected EditorOperationsInterface EditorOperations { get; }
		protected IEditorOptions EditorOptions { get; }

		protected TextViewContent (
			TImports imports,
			FilePath fileName,
			string mimeType,
			Project ownerProject)
		{
			this.Imports = imports;
			this.mimeType = mimeType;
			this.sourceEditorOptions = Ide.Editor.DefaultSourceEditorOptions.Instance;

			Project = ownerProject;
			ContentName = fileName;

			// FIXME: move this to the end of the .ctor after fixing margin options responsiveness
			UpdateLineNumberMarginOption ();

			//TODO: this can change when the file is renamed
			var contentType = GetContentTypeFromMimeType (fileName, mimeType);

			TextDocument = Imports.TextDocumentFactoryService.CreateAndLoadTextDocument (fileName, contentType);
			TextBuffer = TextDocument.TextBuffer;

			var roles = GetAllPredefinedRoles ();
			//we have multiple copies of VacuousTextDataModel for back-compat reasons
			#pragma warning disable CS0436 // Type conflicts with imported type
			var dataModel = new VacuousTextDataModel (TextBuffer);
			var viewModel = UIExtensionSelector.InvokeBestMatchingFactory (
				Imports.TextViewModelProviders,
				dataModel.ContentType,
				roles,
				provider => provider.CreateTextViewModel (dataModel, roles),
				Imports.ContentTypeRegistryService,
				Imports.GuardedOperations,
				this) ?? new VacuousTextViewModel (dataModel);
			#pragma warning restore CS0436 // Type conflicts with imported type

			TextView = CreateTextView (viewModel, roles);
			control = CreateControl ();

			commandService = Imports.EditorCommandHandlerServiceFactory.GetService (TextView);
			EditorOperations = (EditorOperationsInterface)Imports.EditorOperationsProvider.GetEditorOperations (TextView);
			EditorOptions = Imports.EditorOptionsFactoryService.GetOptions (TextView);
			UpdateTextEditorOptions (this, EventArgs.Empty);
			contentProviders = new List<IEditorContentProvider> (Imports.EditorContentProviderService.GetContentProvidersForView (TextView));

			TextView.Properties [typeof(ViewContent)] = this;

			InstallAdditionalEditorOperationsCommands ();

			SubscribeToEvents ();
		}

		public override void GrabFocus ()
		{
			Ide.Editor.DefaultSourceEditorOptions.SetUseAsyncCompletion (true);
			base.GrabFocus ();
		}

		protected override void OnContentNameChanged ()
		{
			base.OnContentNameChanged ();

			if (TextDocument == null)
				return;

			if (editorConfigContext != null) {
				editorConfigContext.CodingConventionsChangedAsync -= UpdateOptionsFromEditorConfigAsync;
				// TODO: What happens to ITextDocument on a rename???
				Ide.Editor.EditorConfigService.RemoveEditConfigContext (TextDocument.FilePath).Ignore ();
				editorConfigContext = null;
			}

			// TODO: Actually implement file rename support. Below is from old editor.
			//       Need to remove or update mimeType field, too.

			//if (ContentName != textEditorImpl.ContentName && !string.IsNullOrEmpty (textEditorImpl.ContentName))
			//	AutoSave.RemoveAutoSaveFile (textEditorImpl.ContentName);
			//if (ContentName != null) // Happens when a file is converted to an untitled file, but even in that case the text editor should be associated with the old location, otherwise typing can be messed up due to change of .editconfig settings etc.
			//	textEditor.FileName = ContentName;
			//if (this.WorkbenchWindow?.Document != null)
			//	textEditor.InitializeExtensionChain (this.WorkbenchWindow.Document);

			UpdateTextEditorOptions (null, null);
		}

		protected override void OnSetProject (Project project)
		{
			base.OnSetProject (project);

			if (TextDocument == null)
				return;

			UpdateTextEditorOptions (null, null);
		}

		protected abstract TView CreateTextView (ITextViewModel viewModel, ITextViewRoleSet roles);

		// FIXME: ideally we could access this via ITextViewFactoryService
		// but it hasn't been upstreamed to Windows yet
		protected abstract ITextViewRoleSet GetAllPredefinedRoles ();

		protected abstract Components.Control CreateControl ();

		Components.Control control;
		public override Components.Control Control => control;

		public override string TabPageLabel
			=> GettextCatalog.GetString ("Source");

		public override void Dispose ()
		{
			UnsubscribeFromEvents ();
			TextDocument.Dispose ();

			if (policyContainer != null)
				policyContainer.PolicyChanged -= PolicyChanged;
			if (editorConfigContext != null) {
				editorConfigContext.CodingConventionsChangedAsync -= UpdateOptionsFromEditorConfigAsync;
				Ide.Editor.EditorConfigService.RemoveEditConfigContext (ContentName).Ignore ();
			}

			base.Dispose ();
		}

		protected virtual void SubscribeToEvents ()
		{
			sourceEditorOptions.Changed += UpdateTextEditorOptions;
			TextDocument.DirtyStateChanged += HandleTextDocumentDirtyStateChanged;
			TextView.Caret.PositionChanged += CaretPositionChanged;
			TextView.TextBuffer.Changed += TextBufferChanged;
		}

		protected virtual void UnsubscribeFromEvents ()
		{
			sourceEditorOptions.Changed -= UpdateTextEditorOptions;
			TextDocument.DirtyStateChanged -= HandleTextDocumentDirtyStateChanged;
			TextView.Caret.PositionChanged -= CaretPositionChanged;
			TextView.TextBuffer.Changed -= TextBufferChanged;
		}

		void UpdateLineNumberMarginOption ()
		{
			Imports.EditorOptionsFactoryService.GlobalOptions.SetOptionValue (
				DefaultTextViewHostOptions.LineNumberMarginId,
				sourceEditorOptions.ShowLineNumberMargin);
		}

		void UpdateTextEditorOptions (object sender, EventArgs e)
		{
			UpdateTextEditorOptionsAsync ().Forget ();
		}

		async Task UpdateTextEditorOptionsAsync ()
		{
			UpdateLineNumberMarginOption ();

			var newPolicyContainer = Project?.Policies;
			if (newPolicyContainer != policyContainer) {
				if (policyContainer != null)
					policyContainer.PolicyChanged -= PolicyChanged;
				policyContainer = newPolicyContainer;
			}
			if (policyContainer != null)
				policyContainer.PolicyChanged += PolicyChanged;

			UpdateOptionsFromPolicy ();

			var newEditorConfigContext = await Ide.Editor.EditorConfigService.GetEditorConfigContext (ContentName, default);
			if (newEditorConfigContext != editorConfigContext) {
				if (editorConfigContext != null)
					editorConfigContext.CodingConventionsChangedAsync -= UpdateOptionsFromEditorConfigAsync;
				editorConfigContext = newEditorConfigContext;
			}
			if (editorConfigContext != null)
				editorConfigContext.CodingConventionsChangedAsync += UpdateOptionsFromEditorConfigAsync;

			await UpdateOptionsFromEditorConfigAsync (null, null);
		}

		private void UpdateOptionsFromPolicy()
		{
			if (policyContainer == null) {
				EditorOptions.ClearOptionValue (DefaultOptions.ConvertTabsToSpacesOptionName);
				EditorOptions.ClearOptionValue (DefaultOptions.TabSizeOptionName);
				EditorOptions.ClearOptionValue (DefaultOptions.IndentSizeOptionName);
				EditorOptions.ClearOptionValue (DefaultOptions.NewLineCharacterOptionName);
				EditorOptions.ClearOptionValue (DefaultOptions.TrimTrailingWhiteSpaceOptionName);

				return;
			}

			var mimeTypes = Ide.DesktopService.GetMimeTypeInheritanceChain (mimeType);
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);

			EditorOptions.SetOptionValue (DefaultOptions.ConvertTabsToSpacesOptionName, currentPolicy.TabsToSpaces);
			EditorOptions.SetOptionValue (DefaultOptions.TabSizeOptionName, currentPolicy.TabWidth);
			EditorOptions.SetOptionValue (DefaultOptions.IndentSizeOptionName, currentPolicy.IndentWidth);
			EditorOptions.SetOptionValue (DefaultOptions.NewLineCharacterOptionName, currentPolicy.GetEolMarker ());
			EditorOptions.SetOptionValue (DefaultOptions.TrimTrailingWhiteSpaceOptionName, currentPolicy.RemoveTrailingWhitespace);
		}

		private Task UpdateOptionsFromEditorConfigAsync (object sender, CodingConventionsChangedEventArgs args)
		{
			if (editorConfigContext == null)
				return Task.FromResult (false);

			if (editorConfigContext.CurrentConventions.UniversalConventions.TryGetIndentStyle (out var indentStyle))
				EditorOptions.SetOptionValue (DefaultOptions.ConvertTabsToSpacesOptionName, indentStyle == IndentStyle.Spaces);
			if (editorConfigContext.CurrentConventions.UniversalConventions.TryGetTabWidth (out var tabWidth))
				EditorOptions.SetOptionValue (DefaultOptions.TabSizeOptionName, tabWidth);
			if (editorConfigContext.CurrentConventions.UniversalConventions.TryGetIndentSize (out var indentSize))
				EditorOptions.SetOptionValue (DefaultOptions.IndentSizeOptionName, indentSize);
			if (editorConfigContext.CurrentConventions.UniversalConventions.TryGetLineEnding (out var lineEnding))
				EditorOptions.SetOptionValue (DefaultOptions.NewLineCharacterOptionName, lineEnding);
			if (editorConfigContext.CurrentConventions.UniversalConventions.TryGetAllowTrailingWhitespace (out var allowTrailingWhitespace))
				EditorOptions.SetOptionValue (DefaultOptions.TrimTrailingWhiteSpaceOptionName, !allowTrailingWhitespace);

			return Task.FromResult (true);
		}

		private void PolicyChanged (object sender, PolicyChangedEventArgs e)
			=> UpdateTextEditorOptions (sender, e);

		protected override object OnGetContent (Type type)
		{
			foreach (var provider in contentProviders) {
				var content = provider.GetContent (TextView, type);
				if (content != null) {
					return content;
				}
			}
			return GetIntrinsicType (type);
		}

		protected override IEnumerable<object> OnGetContents (Type type)
		{
			foreach (var provider in contentProviders) {
				var contents = provider.GetContents (TextView, type);
				if (contents != null) {
					foreach (var content in contents)
						yield return content;
				}
			}

			var intrinsicType = GetIntrinsicType (type);
			if (intrinsicType != null) {
				yield return intrinsicType;
			}
		}

		object GetIntrinsicType (Type type)
		{
			if (type.IsInstanceOfType (TextBuffer))
				return TextBuffer;
			if (type.IsInstanceOfType (TextDocument))
				return TextDocument;
			if (type.IsInstanceOfType (TextView))
				return TextView;
			if (type.IsInstanceOfType (this))
				return this;
			return null;
		}

		public override Task Save ()
		{
			FormatOnSave ();
			TextDocument.Save ();
			return Task.CompletedTask;
		}

		public override Task Save (FileSaveInformation fileSaveInformation)
		{
			FormatOnSave ();
			TextDocument.SaveAs (fileSaveInformation.FileName, overwrite: true);
			return Task.CompletedTask;
		}

		void FormatOnSave ()
		{
			if (!PropertyService.Get ("AutoFormatDocumentOnSave", false))
				return;
			try {
				commandService.Execute ((t, b) => new FormatDocumentCommandArgs (t, b), null);
			} catch (Exception e) {
				LoggingService.LogError ("Error while formatting on save", e);
			}
		}

		public override bool IsDirty => TextDocument.IsDirty;

		void HandleTextDocumentDirtyStateChanged (object sender, EventArgs e)
			=> OnDirtyChanged ();

		static readonly string[] textContentType = { "text" };

		IContentType GetContentTypeFromMimeType (string filePath, string mimeType)
			=> Ide.MimeTypeCatalog.Instance.GetContentTypeForMimeType (mimeType)
				?? (ContentName != null ? Ide.Composition.CompositionManager.GetExportedValue<IFileToContentTypeService> ().GetContentTypeForFilePath (ContentName) : null)
				?? Microsoft.VisualStudio.Platform.PlatformCatalog.Instance.ContentTypeRegistryService.UnknownContentType;

		public override ProjectReloadCapability ProjectReloadCapability
			=> ProjectReloadCapability.Full;

		void CaretPositionChanged (object sender, CaretPositionChangedEventArgs e)
		{
			TryLogNavPoint (true);
		}

		void TextBufferChanged (object sender, TextContentChangedEventArgs e)
		{
			TryLogNavPoint (false);
		}

		object IPropertyPadProvider.GetActiveComponent ()
		{
			if (WorkbenchWindow?.Document is Document doc && doc.HasProject) {
				return Project.Files.GetFile (doc.Name);
			}
			return null;
		}

		object IPropertyPadProvider.GetProvider () => null;

		void IPropertyPadProvider.OnEndEditing (object obj) { }

		void IPropertyPadProvider.OnChanged (object obj)
		{
			if (WorkbenchWindow?.Document is Document doc && doc.HasProject) {
				Ide.IdeApp.ProjectOperations.SaveAsync (doc.Project);
			}
		}
	}
}