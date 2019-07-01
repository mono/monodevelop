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
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Utilities;

using Microsoft.VisualStudio.CodingConventions;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Gui.Documents;

using AutoSave = MonoDevelop.Ide.Editor.AutoSave;
using EditorConfigService = MonoDevelop.Ide.Editor.EditorConfigService;
using DefaultSourceEditorOptions = MonoDevelop.Ide.Editor.DefaultSourceEditorOptions;
using MonoDevelop.Components;
using System.Threading;
using TextEditorFactory = MonoDevelop.Ide.Editor.TextEditorFactory;

#if WINDOWS
using EditorOperationsInterface = Microsoft.VisualStudio.Text.Operations.IEditorOperations3;
#else
using EditorOperationsInterface = Microsoft.VisualStudio.Text.Operations.IEditorOperations4;
#endif

namespace MonoDevelop.TextEditor
{
	abstract partial class TextViewContent<TView, TImports> :
		FileDocumentController,
		ICustomCommandTarget,
		ICommandHandler,
		ICommandUpdater,
		IPropertyPadProvider,
		IDocumentReloadPresenter
#if !WINDOWS
		// implementing this correctly requires IEditorOperations4
		, IZoomable
#endif
		where TView : ITextView
		where TImports : TextViewImports
	{
		IEditorCommandHandlerService commandService;
		List<IEditorContentProvider> contentProviders;
		DefaultSourceEditorOptions sourceEditorOptions;
		IInfoBarPresenter infoBarPresenter;

		static IEditorOptions globalOptions;
		static bool settingZoomLevel;

		PolicyContainer policyContainer;
		ICodingConventionContext editorConfigContext;
		bool warnOverwrite;
		IDisposable textBufferRegistration;

		public TImports Imports { get; }
		public TView TextView { get; private set; }
		public ITextDocument TextDocument { get; private set; }
		public ITextBuffer TextBuffer { get; private set; }

		protected EditorOperationsInterface EditorOperations { get; private set; }
		protected IEditorOptions EditorOptions { get; private set; }

		protected override Type FileModelType => typeof (TextBufferFileModel);

		FileTypeCondition fileTypeCondition = new FileTypeCondition ();

		protected TextViewContent (TImports imports)
		{
			this.Imports = imports;
			this.sourceEditorOptions = DefaultSourceEditorOptions.Instance;
			this.ExtensionContext.RegisterCondition ("FileType", fileTypeCondition);
		}

		protected override async Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			await base.OnInitialize (modelDescriptor, status);
			await Model.Load ();

			// let's update the file type condition
			fileTypeCondition.SetFileName (this.FilePath);
		}

		protected override async Task<Control> OnGetViewControlAsync (CancellationToken token, DocumentViewContent view)
		{
			// FIXME: move this to the end of the .ctor after fixing margin options responsiveness
			UpdateLineNumberMarginOption ();

			var fileModel = (TextBufferFileModel)Model;

			TextDocument = fileModel.TextDocument;
			TextBuffer = TextDocument.TextBuffer;

			UpdateTextBufferRegistration ();

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

			TextView.Properties [typeof (DocumentController)] = this;

			infoBarPresenter = Imports.InfoBarPresenterFactory?.TryGetInfoBarPresenter (TextView);

			InstallAdditionalEditorOperationsCommands ();

			UpdateBufferOptions ();
			SubscribeToEvents ();

			// Set up this static event handling just once
			if (globalOptions == null) {
				globalOptions = Imports.EditorOptionsFactoryService.GlobalOptions;

				// From Mono.TextEditor.TextEditorOptions
				const double ZOOM_FACTOR = 1.1f;
				const int ZOOM_MIN_POW = -4;
				const int ZOOM_MAX_POW = 8;
				var ZOOM_MIN = Math.Pow (ZOOM_FACTOR, ZOOM_MIN_POW);
				var ZOOM_MAX = Math.Pow (ZOOM_FACTOR, ZOOM_MAX_POW);

#if !WINDOWS
				globalOptions.SetMinZoomLevel (ZOOM_MIN * 100);
				globalOptions.SetMaxZoomLevel (ZOOM_MAX * 100);
#endif

				OnConfigurationZoomLevelChanged (null, EventArgs.Empty);

				globalOptions.OptionChanged += OnGlobalOptionsChanged;
				// Check for option changing in old editor
				TextEditorFactory.ZoomLevel.Changed += OnConfigurationZoomLevelChanged;
			}

			// Content providers can provide additional content
			NotifyContentChanged ();

			await Load (false);

			return control;
		}

		protected override void OnModelChanged (DocumentModel oldModel, DocumentModel newModel)
		{
			if (Model != null)
				IsNewDocument = Model.IsNew;
		}

		static void OnConfigurationZoomLevelChanged (object sender, EventArgs e)
		{
			if (settingZoomLevel)
				return;

#if !WINDOWS
			globalOptions.SetZoomLevel (TextEditorFactory.ZoomLevel * 100);
#endif
		}

		static void OnGlobalOptionsChanged (object sender, EditorOptionChangedEventArgs e)
		{
#if !WINDOWS
			if (e.OptionId == DefaultTextViewOptions.ZoomLevelId.Name) {
				settingZoomLevel = true;
				TextEditorFactory.ZoomLevel.Set (globalOptions.ZoomLevel () / 100);
				settingZoomLevel = false;
			}
#endif
		}

		void UpdateTextBufferRegistration ()
		{
			textBufferRegistration?.Dispose ();
			textBufferRegistration = null;

			if (TextBuffer != null)
				textBufferRegistration = IdeServices.TypeSystemService.RegisterOpenDocument (Owner, FilePath, TextBuffer);
		}

		protected override void OnFileNameChanged ()
		{
			base.OnFileNameChanged ();

			if (TextDocument == null)
				return;

			UpdateTextBufferRegistration ();

			warnOverwrite = false;

			if (editorConfigContext != null) {
				editorConfigContext.CodingConventionsChangedAsync -= UpdateOptionsFromEditorConfigAsync;
				EditorConfigService.RemoveEditConfigContext (TextDocument.FilePath).Ignore ();
				editorConfigContext = null;
			}

			if (FilePath != TextDocument.FilePath && !string.IsNullOrEmpty (TextDocument.FilePath))
				AutoSave.RemoveAutoSaveFile (TextDocument.FilePath);

			if (FilePath != null) // Happens when a file is converted to an untitled file, but even in that case the text editor should be associated with the old location, otherwise typing can be messed up due to change of .editconfig settings etc.
				TextDocument.Rename (FilePath);

			// update the file type condition with the new path
			fileTypeCondition.SetFileName (FilePath);

			// TODO: Actually implement file rename support. Below is from old editor.
			//       Need to remove or update mimeType field, too.

			//if (this.WorkbenchWindow?.Document != null)
			//	textEditor.InitializeExtensionChain (this.WorkbenchWindow.Document);

			UpdateTextEditorOptions (null, null);
		}

		protected override void OnOwnerChanged ()
		{
			base.OnOwnerChanged ();

			if (TextDocument != null) {
				UpdateTextEditorOptions (null, null);
				UpdateTextBufferRegistration ();
			}
		}

		protected abstract TView CreateTextView (ITextViewModel viewModel, ITextViewRoleSet roles);

		// FIXME: ideally we could access this via ITextViewFactoryService
		// but it hasn't been upstreamed to Windows yet
		protected abstract ITextViewRoleSet GetAllPredefinedRoles ();

		protected abstract Components.Control CreateControl ();

		Components.Control control;

		protected override void OnDispose ()
		{
			if (IsDisposed)
				return;

			textBufferRegistration?.Dispose ();
			textBufferRegistration = null;

			// Parity behavior with the old editor
			if (autoSaveTask != null)
				autoSaveTask.Wait (TimeSpan.FromSeconds (5));
			RemoveAutoSaveTimer ();
			if (!string.IsNullOrEmpty (FilePath))
				AutoSave.RemoveAutoSaveFile (FilePath);

			UnsubscribeFromEvents ();

			if (policyContainer != null)
				policyContainer.PolicyChanged -= PolicyChanged;
			if (editorConfigContext != null) {
				editorConfigContext.CodingConventionsChangedAsync -= UpdateOptionsFromEditorConfigAsync;
				EditorConfigService.RemoveEditConfigContext (FilePath).Ignore ();
			}

			base.OnDispose ();
		}

		protected virtual void SubscribeToEvents ()
		{
			sourceEditorOptions.Changed += UpdateTextEditorOptions;
			TextDocument.DirtyStateChanged += HandleTextDocumentDirtyStateChanged;
			TextBuffer.Changed += HandleTextBufferChanged;
			TextView.Options.OptionChanged += TextBufferOptionsChanged;
		}

		protected virtual void UnsubscribeFromEvents ()
		{
			if (sourceEditorOptions != null)
				sourceEditorOptions.Changed -= UpdateTextEditorOptions;

			if (TextDocument != null)
				TextDocument.DirtyStateChanged -= HandleTextDocumentDirtyStateChanged;

			if (TextBuffer != null)
				TextBuffer.Changed -= HandleTextBufferChanged;

			// while this actually generates a "warning" about potentially comparing value types,
			// we can be fairly confident that's not actually going to happen - and while the correct
			// change would be to ensure TView is a "class", that's too big of a change to try and
			// and combat this bug with.
			// In addition, this will get JITTed into a cast to Object and a check for null.
			if (TextView != null && TextView.Options != null)
				TextView.Options.OptionChanged -= TextBufferOptionsChanged;
		}

		void UpdateBufferOptions ()
		{
			IsReadOnly = TextView.Options.DoesViewProhibitUserInput ();
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

			var newPolicyContainer = (Owner as IPolicyProvider)?.Policies;
			if (newPolicyContainer != policyContainer) {
				if (policyContainer != null)
					policyContainer.PolicyChanged -= PolicyChanged;
				policyContainer = newPolicyContainer;
			}
			if (policyContainer != null)
				policyContainer.PolicyChanged += PolicyChanged;

			UpdateOptionsFromPolicy ();

			var newEditorConfigContext = await EditorConfigService.GetEditorConfigContext (FilePath, default);
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
				EditorOptions.ClearOptionValue (DefaultTextViewOptions.VerticalRulersName);

				return;
			}

			var mimeTypes = IdeServices.DesktopService.GetMimeTypeInheritanceChain (MimeType);
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);

			EditorOptions.SetOptionValue (DefaultOptions.ConvertTabsToSpacesOptionName, currentPolicy.TabsToSpaces);
			EditorOptions.SetOptionValue (DefaultOptions.TabSizeOptionName, currentPolicy.TabWidth);
			EditorOptions.SetOptionValue (DefaultOptions.IndentSizeOptionName, currentPolicy.IndentWidth);
			EditorOptions.SetOptionValue (DefaultOptions.NewLineCharacterOptionName, currentPolicy.GetEolMarker ());
			EditorOptions.SetOptionValue (DefaultOptions.TrimTrailingWhiteSpaceOptionName, currentPolicy.RemoveTrailingWhitespace);

			EditorOptions.SetOptionValue (
				DefaultTextViewOptions.VerticalRulersName,
				PropertyService.Get<bool> ("ShowRuler") ? new [] { currentPolicy.FileWidth } : Array.Empty<int> ());
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

			var setVerticalRulers = false;
			int [] verticalRulers = null;

			if (editorConfigContext.CurrentConventions.TryGetConventionValue<string> (EditorConfigService.RulersConvention, out var rulers)) {
				setVerticalRulers = true;
				if (!string.IsNullOrEmpty(rulers)) {
					verticalRulers = Array.ConvertAll (rulers.Split (','), val => {
						if (int.TryParse (val, out var col))
							return col;
						return 0;
					});
				}
			} else if (editorConfigContext.CurrentConventions.TryGetConventionValue<string> (EditorConfigService.MaxLineLengthConvention, out var maxLineLength)) {
				if (maxLineLength != "off" && int.TryParse (maxLineLength, out var i)) {
					setVerticalRulers = true;
					verticalRulers = new [] { i };
				} else
					setVerticalRulers = false;
			}

			if (setVerticalRulers)
				EditorOptions.SetOptionValue (DefaultTextViewOptions.VerticalRulersName, verticalRulers ?? Array.Empty<int> ());

			return Task.FromResult (true);
		}

		private void PolicyChanged (object sender, PolicyChangedEventArgs e)
			=> UpdateTextEditorOptions (sender, e);

		protected override object OnGetContent (Type type)
		{
			if (contentProviders != null) {
				foreach (var provider in contentProviders) {
					var content = provider.GetContent (TextView, type);
					if (content != null) {
						return content;
					}
				}
			}
			return GetIntrinsicType (type);
		}

		protected override IEnumerable<object> OnGetContents (Type type)
		{
			if (contentProviders != null) {
				foreach (var provider in contentProviders) {
					var contents = provider.GetContents (TextView, type);
					if (contents != null) {
						foreach (var content in contents)
							yield return content;
					}
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

		Task Load (bool reloading)
		{
			// We actually load initial content at construction time, so this
			// overload only needs to cover reload and autosave scenarios

			if (warnOverwrite) {
				warnOverwrite = false;
				DismissInfoBar ();
				ShowNotification = false;
			}

			if (reloading) {
				TextDocument.Reload ();
			} else if (AutoSave.AutoSaveExists (FilePath)) {
				var autosaveContent = AutoSave.LoadAutoSave (FilePath);

				MarkDirty ();
				warnOverwrite = true;

				// Set editor read-only until user picks one of the above options.
				var setWritable = !TextView.Options.DoesViewProhibitUserInput ();
				if (setWritable)
					TextView.Options.SetOptionValue (DefaultTextViewOptions.ViewProhibitUserInputId, true);

				var (primaryMessageText, secondaryMessageText) = SplitMessageString (
					BrandingService.BrandApplicationName (GettextCatalog.GetString (
						"<b>An autosave file has been found for this file.</b>\n" +
						"This could mean that another instance of MonoDevelop is editing this " +
						"file, or that MonoDevelop crashed with unsaved changes.\n\n" +
						"Do you want to use the original file, or load from the autosave file?")));

				PresentInfobar (
					primaryMessageText,
					secondaryMessageText,
					new InfoBarAction (
						GetButtonString (GettextCatalog.GetString ("_Use original file")),
						UseOriginalFile),
					new InfoBarAction (
						GetButtonString (GettextCatalog.GetString ("_Load from autosave")),
						LoadFromAutosave,
						isDefault: true));

				void OnActionSelected ()
				{
					DismissInfoBar ();
					if (setWritable)
						TextView.Options.SetOptionValue (DefaultTextViewOptions.ViewProhibitUserInputId, false);
				}

				void LoadFromAutosave ()
				{
					try {
						AutoSave.RemoveAutoSaveFile (FilePath);
						ReplaceContent (autosaveContent.Text, autosaveContent.Encoding);
					} catch (Exception e) {
						LoggingService.LogError ("Could not load the autosave file", e);
					} finally {
						OnActionSelected ();
					}
				}

				void UseOriginalFile ()
				{
					try {
						AutoSave.RemoveAutoSaveFile (FilePath);
					} catch (Exception e) {
						LoggingService.LogError ("Could not remove the autosave file", e);
					} finally {
						OnActionSelected ();
					}
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Replace document content with new content. This marks the document as dirty.
		/// </summary>
		void ReplaceContent (string newContent, Encoding newEncoding)
		{
			var currentSnapshot = TextBuffer.CurrentSnapshot;
			TextDocument.Encoding = newEncoding;
			TextBuffer.Replace (
				new SnapshotSpan (currentSnapshot, 0, currentSnapshot.Length),
				newContent);
		}

		void PresentInfobar (string title, string description, params InfoBarAction [] actions)
		{
			if (infoBarPresenter != null) {
				DismissInfoBar ();
				infoBarPresenter.Present (new InfoBarViewModel (title, description, actions));
			}
		}

		void DismissInfoBar ()
			=> infoBarPresenter?.DismissAll ();

		// TODO: Switch to native timeout, this is copied from TextEditorViewContent
		uint autoSaveTimer;
		Task autoSaveTask;
		void InformAutoSave ()
		{
			if (IsDisposed)
				return;
			RemoveAutoSaveTimer ();
			autoSaveTimer = GLib.Timeout.Add (500, delegate {
				autoSaveTimer = 0;
				if (autoSaveTask != null && !autoSaveTask.IsCompleted)
					return false;

				autoSaveTask = AutoSave.InformAutoSaveThread (
					new AutoSaveTextSourceFacade(TextBuffer, TextDocument), FilePath, HasUnsavedChanges);
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

		protected override async Task OnSave ()
		{
			if (warnOverwrite) {
				string question = GettextCatalog.GetString (
					"This file {0} has been changed outside of {1}. Are you sure you want to overwrite the file?",
					FilePath, BrandingService.ApplicationName
				);
				if (MessageService.AskQuestion (question, AlertButton.Cancel, AlertButton.OverwriteFile) != AlertButton.OverwriteFile)
					return;

				warnOverwrite = false;
				DismissInfoBar ();
				ShowNotification = false;
			}

			if (!string.IsNullOrEmpty (FilePath))
				AutoSave.RemoveAutoSaveFile (FilePath);

			FormatOnSave ();

			await base.OnSave ();
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

		bool manuallyMarkingDirty;
		void MarkDirty ()
		{
			manuallyMarkingDirty = true;
			try {
				TextDocument.UpdateDirtyState (true, DateTime.Now);
			} finally {
				manuallyMarkingDirty = false;
			}
		}

		void HandleTextDocumentDirtyStateChanged (object sender, EventArgs e)
		{
			HasUnsavedChanges = TextDocument.IsDirty;
			if (!manuallyMarkingDirty)
				InformAutoSave ();
		}

		private void HandleTextBufferChanged (object sender, TextContentChangedEventArgs e)
			=> InformAutoSave ();

		static readonly string[] textContentType = { "text" };

		IContentType GetContentTypeFromMimeType (string filePath, string mimeType)
			=> Ide.MimeTypeCatalog.Instance.GetContentTypeForMimeType (mimeType, filePath)
				?? Microsoft.VisualStudio.Platform.PlatformCatalog.Instance.ContentTypeRegistryService.UnknownContentType;

		protected internal override ProjectReloadCapability OnGetProjectReloadCapability () => ProjectReloadCapability.Full;

		void TextBufferOptionsChanged (object sender, EventArgs a)
		{
			UpdateBufferOptions ();
		}

		object IPropertyPadProvider.GetActiveComponent ()
		{
			if (Document?.Owner is Project project)
				return project.GetProjectFile (FilePath);
			return null;
		}

		object IPropertyPadProvider.GetProvider () => null;

		void IPropertyPadProvider.OnEndEditing (object obj) { }

		void IPropertyPadProvider.OnChanged (object obj)
		{
			if (Document?.Owner is Project project)
				Ide.IdeApp.ProjectOperations.SaveAsync (project);
		}

		void IDocumentReloadPresenter.ShowFileChangedWarning (bool multiple)
		{
			var actions = new List<InfoBarAction> {
				new InfoBarAction (GetButtonString (GettextCatalog.GetString ("_Reload from disk")), ReloadFromDisk),
				new InfoBarAction (GetButtonString (GettextCatalog.GetString ("_Keep changes")), KeepChanges, isDefault: !multiple),
			};

			if (multiple) {
				actions.Add (new InfoBarAction (GetButtonString (GettextCatalog.GetString ("_Reload all")), ReloadAll));
				actions.Add (new InfoBarAction (GetButtonString (GettextCatalog.GetString ("_Ignore all")), IgnoreAll));
			}

			ShowNotification = true;
			warnOverwrite = true;
			MarkDirty ();

			var (primaryMessageText, secondaryMessageText) = SplitMessageString (GettextCatalog.GetString (
				"<b>The file \"{0}\" has been changed outside of {1}.</b>\n" +
				"Do you want to keep your changes, or reload the file from disk?",
				FilePath, BrandingService.ApplicationName));

			PresentInfobar (
				primaryMessageText,
				secondaryMessageText,
				actions.ToArray ());

			void ReloadFromDisk ()
			{
				try {
					if (IsDisposed || !File.Exists (FilePath))
						return;

					Load (true);
					ShowNotification = false;
				} catch (Exception ex) {
					MessageService.ShowError ("Could not reload the file.", ex);
				} finally {
					DismissInfoBar ();
				}
			}

			void KeepChanges ()
			{
				if (IsDisposed)
					return;
				ShowNotification = false;
				DismissInfoBar ();
			}

			void ReloadAll () => DocumentRegistry.ReloadAllChangedFiles ();

			void IgnoreAll () => DocumentRegistry.IgnoreAllChangedFiles ();
		}

		void IDocumentReloadPresenter.RemoveMessageBar ()
			=> DismissInfoBar ();

		/// <summary>
		/// Converts strings to title case per the current locale and strips <c>_</c> mnemonic characters,
		/// allowing us to retain original already localized strings from the old editor UI but present
		/// them better in the new editor UI.
		/// </summary>
		static string GetButtonString (string originalButtonString)
			=> System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase (
				originalButtonString.Replace ("_", string.Empty));

		/// <summary>
		/// Strips markup and returns the string before the first line break as a primary string and
		/// everything after as a secondary string, allowing us to retain original already localized
		/// strings from the old editor UI but presen them better in the new editor UI.
		/// </summary>
		static (string primaryString, string secondaryString) SplitMessageString (string originalString)
		{
			if (originalString == null)
				return (null, null);

			if (originalString == string.Empty)
				return (string.Empty, null);

			var strippedText = Xwt.FormattedText.FromMarkup (originalString).Text;
			var secondLineOffset = strippedText.IndexOf ('\n');
			if (secondLineOffset < 0)
				return (strippedText, null);

			return (
				strippedText.Substring (0, secondLineOffset),
				strippedText.Substring (secondLineOffset + 1).TrimStart ());
		}

		/// <summary>
		/// An ITextSource that only implements enough pieces for AutoSave to work.
		///
		/// This can go away when we update AutoSave to use the VS APIs.
		/// </summary>
		class AutoSaveTextSourceFacade : ITextSource
		{
			readonly ITextBuffer textBuffer;
			readonly ITextDocument textDocument;

			public AutoSaveTextSourceFacade (ITextBuffer textBuffer, ITextDocument textDocument)
			{
				this.textBuffer = textBuffer
					?? throw new ArgumentNullException (nameof (textBuffer));
				this.textDocument = textDocument
					?? throw new ArgumentNullException (nameof (textDocument));
			}

			public char this [int offset] => throw new NotImplementedException ();

			public ITextSourceVersion Version => throw new NotImplementedException ();

			public Encoding Encoding => textDocument.Encoding;

			public int Length => throw new NotImplementedException ();

			public string Text => throw new NotImplementedException ();

			public void CopyTo (int sourceIndex, char [] destination, int destinationIndex, int count)
				=> throw new NotImplementedException ();

			public TextReader CreateReader ()
				=> throw new NotImplementedException ();

			public TextReader CreateReader (int offset, int length)
				=> throw new NotImplementedException ();

			public ITextSource CreateSnapshot ()
				=> throw new NotImplementedException ();

			public ITextSource CreateSnapshot (int offset, int length)
				=> throw new NotImplementedException ();

			public char GetCharAt (int offset)
				=> throw new NotImplementedException ();

			public string GetTextAt (int offset, int length)
				=> throw new NotImplementedException ();

			public void WriteTextTo (TextWriter writer)
				=> textBuffer.CurrentSnapshot.Write (writer);

			public void WriteTextTo (TextWriter writer, int offset, int length)
				=> throw new NotImplementedException ();
		}
	}
}
