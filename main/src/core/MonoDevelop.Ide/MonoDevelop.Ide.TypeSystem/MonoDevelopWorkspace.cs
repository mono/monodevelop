//
// MonoDevelopWorkspace.cs
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
using Microsoft.CodeAnalysis;
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis.Host;
using MonoDevelop.Core.Text;
using System.Collections.Concurrent;
using MonoDevelop.Ide.CodeFormatting;
using Gtk;
using MonoDevelop.Ide.Editor.Projection;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Text;
using System.Collections.Immutable;
using System.ComponentModel;
using Mono.Addins;
using MonoDevelop.Core.AddIns;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionCrawler;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.RoslynServices;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace : Workspace
	{
		public const string ServiceLayer = nameof(MonoDevelopWorkspace);

		// Background compiler is used to trigger compilations in the background for the solution and hold onto them
		// so in case nothing references the solution in current stacks, they're not collected.
		// We previously used to experience pathological GC times on large solutions, and this was caused
		// by the compilations being freed out of memory due to only being weakly referenced, and recomputing them on
		// a case by case basis.
		BackgroundCompiler backgroundCompiler;
		internal readonly WorkspaceId Id;

		CancellationTokenSource src = new CancellationTokenSource ();
		bool disposed;
		readonly object updatingProjectDataLock = new object ();
		Lazy<MonoDevelopMetadataReferenceManager> manager;
		Lazy<MetadataReferenceHandler> metadataHandler;
		ProjectionData Projections { get; }
		OpenDocumentsData OpenDocuments { get; }
		ProjectDataMap ProjectMap { get; }
		ProjectSystemHandler ProjectHandler { get; }

		public MonoDevelop.Projects.Solution MonoDevelopSolution { get; }

		internal MonoDevelopMetadataReferenceManager MetadataReferenceManager => manager.Value;
		internal static HostServices HostServices => CompositionManager.Instance.HostServices;

		static MonoDevelopWorkspace ()
		{
			Tasks.CommentTasksProvider.Initialize ();
		}

		/// <summary>
		/// This bypasses the type system service. Use with care.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal void OpenSolutionInfo (SolutionInfo sInfo)
		{
			OnSolutionAdded (sInfo);
		}

		internal MonoDevelopWorkspace (MonoDevelop.Projects.Solution solution) : base (HostServices, WorkspaceKind.Host)
		{
			this.MonoDevelopSolution = solution;
			this.Id = WorkspaceId.Next ();

			Projections = new ProjectionData ();
			OpenDocuments = new OpenDocumentsData ();
			ProjectMap = new ProjectDataMap (this);
			ProjectHandler = new ProjectSystemHandler (this, ProjectMap, Projections);
			manager = new Lazy<MonoDevelopMetadataReferenceManager> (() => Services.GetService<MonoDevelopMetadataReferenceManager> ());
			metadataHandler = new Lazy<MetadataReferenceHandler> (() => new MetadataReferenceHandler (MetadataReferenceManager, ProjectMap));

			if (IdeApp.Workspace != null && solution != null) {
				IdeApp.Workspace.ActiveConfigurationChanged += HandleActiveConfigurationChanged;
			}
			backgroundCompiler = new BackgroundCompiler (this);

			var cacheService = Services.GetService<IWorkspaceCacheService> ();
			if (cacheService != null)
				cacheService.CacheFlushRequested += OnCacheFlushRequested;

			// Trigger running compiler syntax and semantic errors via the diagnostic analyzer engine
			IdeApp.Preferences.Roslyn.FullSolutionAnalysisRuntimeEnabled = true;
			Options = Options.WithChangedOption (Microsoft.CodeAnalysis.Diagnostics.InternalRuntimeDiagnosticOptions.Syntax, true)
				.WithChangedOption (Microsoft.CodeAnalysis.Diagnostics.InternalRuntimeDiagnosticOptions.Semantic, true)
            // Turn on FSA on a new workspace addition
				.WithChangedOption (RuntimeOptions.FullSolutionAnalysis, true)
				.WithChangedOption (RuntimeOptions.FullSolutionAnalysisInfoBarShown, false)

			// Always use persistent storage regardless of solution size, at least until a consensus is reached
			// https://github.com/mono/monodevelop/issues/4149 https://github.com/dotnet/roslyn/issues/25453
			    .WithChangedOption (Microsoft.CodeAnalysis.Storage.StorageOptions.SolutionSizeThreshold, MonoDevelop.Core.Platform.IsLinux ? int.MaxValue : 0);

			if (IdeApp.Preferences.EnableSourceAnalysis) {
				var solutionCrawler = Services.GetService<ISolutionCrawlerRegistrationService> ();
				solutionCrawler.Register (this);
			}

			IdeApp.Preferences.EnableSourceAnalysis.Changed += OnEnableSourceAnalysisChanged;

			// TODO: Unhack C# here when monodevelop workspace supports more than C#
			IdeApp.Preferences.Roslyn.FullSolutionAnalysisRuntimeEnabledChanged += OnEnableFullSourceAnalysisChanged;

			foreach (var factory in AddinManager.GetExtensionObjects<Microsoft.CodeAnalysis.Options.IDocumentOptionsProviderFactory>("/MonoDevelop/Ide/TypeService/OptionProviders"))
				Services.GetRequiredService<Microsoft.CodeAnalysis.Options.IOptionService> ().RegisterDocumentOptionsProvider (factory.Create (this));

			if (solution != null)
				DesktopService.MemoryMonitor.StatusChanged += OnMemoryStatusChanged;
		}

		bool lowMemoryLogged;
		void OnMemoryStatusChanged (object sender, PlatformMemoryStatusEventArgs args)
		{
			// Disable full solution analysis when the OS triggers a warning about memory pressure.
			if (args.MemoryStatus == PlatformMemoryStatus.Normal)
				return;

			// record that we had hit critical memory barrier
			if (!lowMemoryLogged) {
				lowMemoryLogged = true;
				Logger.Log (FunctionId.VirtualMemory_MemoryLow, KeyValueLogMessage.Create (m => {
					// which message we are logging and memory left in bytes when this is called.
					m ["MSG"] = args.MemoryStatus;
					//m ["MemoryLeft"] = (long)wParam;
				}));
			}

			var cacheService = Services.GetService<IWorkspaceCacheService> () as MonoDevelopWorkspaceCacheService;
			cacheService?.FlushCaches ();

			if (!ShouldTurnOffFullSolutionAnalysis ())
				return;

			Options = Options.WithChangedOption (RuntimeOptions.FullSolutionAnalysis, false);
			IdeApp.Preferences.Roslyn.FullSolutionAnalysisRuntimeEnabled = false;
			if (IsUserOptionOn ()) {
				// let user know full analysis is turned off due to memory concern.
				// make sure we show info bar only once for the same solution.
				Options = Options.WithChangedOption (RuntimeOptions.FullSolutionAnalysisInfoBarShown, true);

				const string LowVMMoreInfoLink = "https://go.microsoft.com/fwlink/?linkid=2003417&clcid=0x409";
				Services.GetService<IErrorReportingService> ().ShowGlobalErrorInfo (
					GettextCatalog.GetString ("{0} has suspended some advanced features to improve performance", BrandingService.ApplicationName),
					new InfoBarUI ("Learn more", InfoBarUI.UIKind.HyperLink, () => DesktopService.ShowUrl (LowVMMoreInfoLink), closeAfterAction: false),
					new InfoBarUI ("Restore", InfoBarUI.UIKind.Button, () => Options = Options.WithChangedOption (RuntimeOptions.FullSolutionAnalysis, true))
				);
			}
		}

		void OnCacheFlushRequested (object sender, EventArgs args)
		{
			if (backgroundCompiler != null) {
				backgroundCompiler.Dispose ();
				backgroundCompiler = null; // PartialSemanticsEnabled will now return false
			}

			// No longer need cache notifications
			var cacheService = Services.GetService<IWorkspaceCacheService> ();
			if (cacheService != null)
				cacheService.CacheFlushRequested -= OnCacheFlushRequested;
		}

		bool ShouldTurnOffFullSolutionAnalysis ()
		{
			// conditions
			// 1. if our full solution analysis option is on (not user full solution analysis option, but our internal one) and
			// 2. if infobar is never shown to users for this solution
			return Options.GetOption (RuntimeOptions.FullSolutionAnalysis) && !Options.GetOption (RuntimeOptions.FullSolutionAnalysisInfoBarShown);
		}

		bool IsUserOptionOn ()
		{
			// check languages currently on solution. since we only show info bar once, we don't need to track solution changes.
			var languages = CurrentSolution.Projects.Select (p => p.Language).Distinct ();
			foreach (var language in languages) {
				if (IdeApp.Preferences.Roslyn.For (language).SolutionCrawlerClosedFileDiagnostic) {
					return true;
				}
			}

			return false;
		}

		void OnEnableSourceAnalysisChanged(object sender, EventArgs args)
		{
			var solutionCrawler = Services.GetService<ISolutionCrawlerRegistrationService> ();
			if (IdeApp.Preferences.EnableSourceAnalysis)
				solutionCrawler.Register (this);
			else
				solutionCrawler.Unregister (this);
		}

		void OnEnableFullSourceAnalysisChanged (object sender, EventArgs args)
		{
			// we only want to turn on FSA if the option is explicitly enabled,
			// we don't want to turn it off here.
			if (IdeApp.Preferences.Roslyn.FullSolutionAnalysisRuntimeEnabled) {
				Options = Options.WithChangedOption (RuntimeOptions.FullSolutionAnalysis, true);
			}
		}

		protected internal override bool PartialSemanticsEnabled => backgroundCompiler != null;

		protected override void Dispose (bool finalize)
		{
			base.Dispose (finalize);
			if (disposed)
				return;

			disposed = true;

			MetadataReferenceManager.ClearCache ();

			IdeApp.Preferences.EnableSourceAnalysis.Changed -= OnEnableSourceAnalysisChanged;
			IdeApp.Preferences.Roslyn.FullSolutionAnalysisRuntimeEnabledChanged -= OnEnableFullSourceAnalysisChanged;
			DesktopService.MemoryMonitor.StatusChanged -= OnMemoryStatusChanged;

			CancelLoad ();
			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.ActiveConfigurationChanged -= HandleActiveConfigurationChanged;
			}
			if (MonoDevelopSolution != null) {
				foreach (var prj in MonoDevelopSolution.GetAllProjects ()) {
					UnloadMonoProject (prj);
				}
			}

			var solutionCrawler = Services.GetService<ISolutionCrawlerRegistrationService> ();
			solutionCrawler.Unregister (this);

			if (backgroundCompiler != null) {
				backgroundCompiler.Dispose ();
				backgroundCompiler = null; // PartialSemanticsEnabled will now return false
			}
		}

		internal void InformDocumentTextChange (DocumentId id, SourceText text)
		{
			base.ApplyDocumentTextChanged (id, text);
		}

		void CancelLoad ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}

		internal static event EventHandler LoadingFinished;

		internal void HideStatusIcon ()
		{
			TypeSystemService.HideTypeInformationGatheringIcon (() => {
				LoadingFinished?.Invoke (this, EventArgs.Empty);
				WorkspaceLoaded?.Invoke (this, EventArgs.Empty);
			});
		}

		public event EventHandler WorkspaceLoaded;

		internal void ShowStatusIcon ()
		{
			TypeSystemService.ShowTypeInformationGatheringIcon ();
		}

		async void HandleActiveConfigurationChanged (object sender, EventArgs e)
		{
			ShowStatusIcon ();
			CancelLoad ();
			var token = src.Token;

			try {
				var si = await ProjectHandler.CreateSolutionInfo (MonoDevelopSolution, token).ConfigureAwait (false);
				if (si != null)
					OnSolutionReloaded (si);
			} catch (OperationCanceledException) {
			} catch (AggregateException ae) {
				ae.Flatten ().Handle (x => x is OperationCanceledException);
			} catch (Exception ex) {
				LoggingService.LogError ("Error while reloading solution.", ex);
			} finally {
				HideStatusIcon ();
			}
    }

		internal void ReloadModifiedProject (MonoDevelop.Projects.Project project)
		{
			ProjectHandler.ReloadModifiedProject (project);
		}

		internal Task<SolutionInfo> TryLoadSolution (CancellationToken cancellationToken = default(CancellationToken))
		{
			return ProjectHandler.CreateSolutionInfo (MonoDevelopSolution, CancellationTokenSource.CreateLinkedTokenSource (cancellationToken, src.Token).Token);
		}

		internal void UnloadSolution ()
		{
			OnSolutionRemoved ();
		}

		void UnloadMonoProject (MonoDevelop.Projects.Project project)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));
			project.Modified -= OnProjectModified;
		}

		#region Open documents
		public override bool CanOpenDocuments => true;

		public override void OpenDocument (DocumentId documentId, bool activate = true)
		{
			var doc = GetDocument (documentId);
			if (doc != null) {
				var mdProject = GetMonoProject (doc.Project);
				if (doc != null) {
					IdeApp.Workbench.OpenDocument (doc.FilePath, mdProject, activate);
				}
			}
		}

		readonly Dictionary<DocumentInfo, SourceTextContainer> generatedFiles = new Dictionary<DocumentInfo, SourceTextContainer> ();
		internal void AddAndOpenDocumentInternal (DocumentInfo documentInfo, SourceTextContainer textContainer)
		{
			lock (generatedFiles) {
				generatedFiles[documentInfo] = textContainer;
				OnDocumentAdded (documentInfo);
				OnDocumentOpened (documentInfo.Id, textContainer);
			}
		}

		internal void CloseAndRemoveDocumentInternal (DocumentId documentId, TextLoader reloader)
		{
			lock (generatedFiles) {
				var documentInfo = generatedFiles.FirstOrDefault (kvp => kvp.Key.Id == documentId).Key;
				if (documentInfo != null && generatedFiles.Remove(documentInfo) && CurrentSolution.ContainsDocument(documentId)) {
					OnDocumentClosed (documentId, reloader);
					OnDocumentRemoved (documentId);
				}
			}
		}

		internal void InformDocumentOpen (DocumentId documentId, TextEditor editor, DocumentContext context)
		{
			var document = InternalInformDocumentOpen (documentId, editor, context);
			if (document as Document != null) {
				foreach (var linkedDoc in ((Document)document).GetLinkedDocumentIds ()) {
					InternalInformDocumentOpen (linkedDoc, editor, context);
				}
			}
			OnDocumentContextUpdated (documentId);
		}

		TextDocument InternalInformDocumentOpen (DocumentId documentId, TextEditor editor, DocumentContext context)
		{
			var project = this.CurrentSolution.GetProject (documentId.ProjectId);
			if (project == null)
				return null;
			TextDocument document = project.GetDocument (documentId) ?? project.GetAdditionalDocument (documentId);
			if (document == null || OpenDocuments.Contains (documentId)) {
				return document;
			}
			var textContainer = editor.TextView.TextBuffer.AsTextContainer ();
			OpenDocuments.Add (documentId, textContainer, editor, context);
			if (document is Document) {
				OnDocumentOpened (documentId, textContainer);
			} else {
				OnAdditionalDocumentOpened (documentId, textContainer);
			}
			return document;
		}

		ProjectChanges projectChanges;

		protected override void OnDocumentClosing (DocumentId documentId)
		{
			base.OnDocumentClosing (documentId);
			OpenDocuments.Remove (documentId);
		}

//		internal override bool CanChangeActiveContextDocument {
//			get {
//				return true;
//			}
//		}

		internal void InformDocumentClose (DocumentId analysisDocument, string filePath)
		{
			try {
				if (!OpenDocuments.Remove (analysisDocument)) {
					//Apparently something else opened this file via AddAndOpenDocumentInternal(e.g. .cshtml)
					//it's job of whatever opened to also call CloseAndRemoveDocumentInternal
					return;
				}
				if (!CurrentSolution.ContainsDocument (analysisDocument))
					return;
				var loader = new MonoDevelopTextLoader (filePath);
				var document = this.GetDocument (analysisDocument);
				// FIXME: Is this really needed?
				OpenDocuments.Remove (analysisDocument);

				if (document == null) {
					var ad = this.GetAdditionalDocument (analysisDocument);
					if (ad != null)
						OnAdditionalDocumentClosed (analysisDocument, loader);
					return;
				}
				OnDocumentClosed (analysisDocument, loader);
				foreach (var linkedDoc in document.GetLinkedDocumentIds ()) {
					OnDocumentClosed (linkedDoc, loader);
				}
			} catch (Exception e) {
				LoggingService.LogError ("Exception while closing document.", e);
			}
		}

		public override void CloseDocument (DocumentId documentId)
		{
		}

		//FIXME: this should NOT be async. our implementation is doing some very expensive things like formatting that it shouldn't need to do.
		protected override void ApplyDocumentTextChanged (DocumentId id, SourceText text)
		{
			lock (projectModifyLock)
				tryApplyState_documentTextChangedTasks.Add (ApplyDocumentTextChangedCore (id, text));
		}

		async Task ApplyDocumentTextChangedCore (DocumentId id, SourceText text)
		{
			var document = GetDocument (id);
			if (document == null)
				return;

			var hostDocument = MonoDevelopHostDocumentRegistration.FromDocument (document);
			if (hostDocument != null) {
				hostDocument.UpdateText (text);
				return;
			}

			var (projection, filePath) = Projections.Get (document.FilePath);
			var data = TextFileProvider.Instance.GetTextEditorData (filePath, out bool isOpen);
			// Guard against already done changes in linked files.
			// This shouldn't happen but the roslyn merging seems not to be working correctly in all cases :/
			if (document.GetLinkedDocumentIds ().Length > 0 && isOpen && !(text.GetType ().FullName == "Microsoft.CodeAnalysis.Text.ChangedText")) {
				return;
			}

			lock (tryApplyState_documentTextChangedContents) {
				if (tryApplyState_documentTextChangedContents.TryGetValue (filePath, out SourceText formerText)) {
					if (formerText.Length == text.Length && formerText.ToString () == text.ToString ())
						return;
				}
				tryApplyState_documentTextChangedContents[filePath] = text;
			}

			if (!isOpen || !document.TryGetText (out SourceText oldFile)) {
				oldFile = await document.GetTextAsync ();
			}
			var changes = text.GetTextChanges (oldFile).OrderByDescending (c => c.Span.Start).ToList ();
			int delta = 0;

			if (!isOpen) {
				delta = ApplyChanges (projection, data, changes);
				var formatter = CodeFormatterService.GetFormatter (data.MimeType);
				if (formatter != null && formatter.SupportsPartialDocumentFormatting) {
					var mp = GetMonoProject (CurrentSolution.GetProject (id.ProjectId));
					string currentText = data.Text;

					foreach (var change in changes) {
						delta -= change.Span.Length - change.NewText.Length;
						var startOffset = change.Span.Start - delta;

						if (projection != null) {
							if (projection.TryConvertFromProjectionToOriginal (startOffset, out int originalOffset))
								startOffset = originalOffset;
						}

						string str;
						if (change.NewText.Length == 0) {
							str = formatter.FormatText (mp.Policies, currentText, TextSegment.FromBounds (Math.Max (0, startOffset - 1), Math.Min (data.Length, startOffset + 1)));
						} else {
							str = formatter.FormatText (mp.Policies, currentText, new TextSegment (startOffset, change.NewText.Length));
						}
						data.ReplaceText (startOffset, change.NewText.Length, str);
					}
				}
				data.Save ();
				if (projection != null) {
					await UpdateProjectionsDocuments (document, data);
				} else {
					OnDocumentTextChanged (id, new MonoDevelopSourceText (data), PreservationMode.PreserveValue);
				}
			} else {
				var formatter = CodeFormatterService.GetFormatter (data.MimeType);
				var documentContext = IdeApp.Workbench.Documents.FirstOrDefault (d => FilePath.PathComparer.Compare (d.FileName, filePath) == 0);
				var root = await projectChanges.NewProject.GetDocument (id).GetSyntaxRootAsync ();
				var annotatedNode = root.DescendantNodesAndSelf ().FirstOrDefault (n => n.HasAnnotation (TypeSystemService.InsertionModeAnnotation));
				SyntaxToken? renameTokenOpt = root.GetAnnotatedNodesAndTokens (Microsoft.CodeAnalysis.CodeActions.RenameAnnotation.Kind)
												  .Where (s => s.IsToken)
												  .Select (s => s.AsToken ())
												  .Cast<SyntaxToken?> ()
												  .FirstOrDefault ();

				if (documentContext != null) {
					var editor = (TextEditor)data;
					await Runtime.RunInMainThread (async () => {
						using (var undo = editor.OpenUndoGroup ()) {
							var oldVersion = editor.Version;
							delta = ApplyChanges (projection, data, changes);
							var versionBeforeFormat = editor.Version;

							if (formatter != null && formatter.SupportsOnTheFlyFormatting) {
								foreach (var change in changes) {
									delta -= change.Span.Length - change.NewText.Length;
									var startOffset = change.Span.Start - delta;
									if (projection != null) {
										if (projection.TryConvertFromProjectionToOriginal (startOffset, out int originalOffset))
											startOffset = originalOffset;
									}
									if (change.NewText.Length == 0) {
										formatter.OnTheFlyFormat (editor, documentContext, TextSegment.FromBounds (Math.Max (0, startOffset - 1), Math.Min (data.Length, startOffset + 1)));
									} else {
										formatter.OnTheFlyFormat (editor, documentContext, new TextSegment (startOffset, change.NewText.Length));
									}
								}
							}
							if (annotatedNode != null && GetInsertionPoints != null) {
								IdeApp.Workbench.Documents.First (d => d.FileName == editor.FileName).Select ();
								var formattedVersion = editor.Version;

								int startOffset = versionBeforeFormat.MoveOffsetTo (editor.Version, annotatedNode.Span.Start);
								int endOffset = versionBeforeFormat.MoveOffsetTo (editor.Version, annotatedNode.Span.End);

								// alway whole line start & delimiter
								var startLine = editor.GetLineByOffset (startOffset);
								startOffset = startLine.Offset;

								var endLine = editor.GetLineByOffset (endOffset);
								endOffset = endLine.EndOffsetIncludingDelimiter + 1;

								var insertionCursorSegment = TextSegment.FromBounds (startOffset, endOffset);
								string textToInsert = editor.GetTextAt (insertionCursorSegment).TrimEnd ();
								editor.RemoveText (insertionCursorSegment);
								var insertionPoints = await GetInsertionPoints (editor, editor.CaretOffset);
								if (insertionPoints.Count == 0) {
									// Just to get sure if no insertion points -> go back to the formatted version.
									var textChanges = editor.Version.GetChangesTo (formattedVersion).ToList ();
									using (var undo2 = editor.OpenUndoGroup ()) {
										foreach (var textChange in textChanges) {
											foreach (var v in textChange.TextChanges.Reverse ()) {
												editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
											}
										}
									}
									return;
								}
								string insertionModeOperation;
								const int CSharpMethodKind = 8875;


								bool isMethod = annotatedNode.RawKind == CSharpMethodKind;

								if (!isMethod) {
									// atm only for generate field/property : remove all new lines generated & just insert the plain node.
									// for methods it's not so easy because of "extract code" changes.
									foreach (var textChange in editor.Version.GetChangesTo (oldVersion).ToList ()) {
										foreach (var v in textChange.TextChanges.Reverse ()) {
											editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
										}
									}
								}

								switch (annotatedNode.RawKind) {
								case 8873: // C# field
									insertionModeOperation = GettextCatalog.GetString ("Insert Field");
									break;
								case CSharpMethodKind:
									insertionModeOperation = GettextCatalog.GetString ("Insert Method");
									break;
								case 8892: // C# property
									insertionModeOperation = GettextCatalog.GetString ("Insert Property");
									break;
								default:
									insertionModeOperation = GettextCatalog.GetString ("Insert Code");
									break;
								}

								var options = new InsertionModeOptions (
									insertionModeOperation,
									insertionPoints,
									point => {
										if (!point.Success)
											return;
										point.InsertionPoint.Insert (editor, textToInsert);
									}
								);
								options.ModeExitedAction += delegate (InsertionCursorEventArgs args) {
									if (!args.Success) {
										var textChanges = editor.Version.GetChangesTo (oldVersion).ToList ();
										using (var undo2 = editor.OpenUndoGroup ()) {
											foreach (var textChange in textChanges) {
												foreach (var v in textChange.TextChanges.Reverse ())
													editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
											}
										}
									}
								};
								for (int i = 0; i < insertionPoints.Count; i++) {
									if (insertionPoints [i].Location.Line < editor.CaretLine) {
										options.FirstSelectedInsertionPoint = Math.Min (isMethod ? i + 1 : i, insertionPoints.Count - 1);
									} else {
										break;
									}
								}
								options.ModeExitedAction += delegate {
									if (renameTokenOpt.HasValue)
										StartRenameSession (editor, documentContext, versionBeforeFormat, renameTokenOpt.Value);
								};
								editor.StartInsertionMode (options);
							}
						}
					});
				}

				if (projection != null) {
					await UpdateProjectionsDocuments (document, data);
				} else {
					OnDocumentTextChanged (id, new MonoDevelopSourceText (data), PreservationMode.PreserveValue);
				}
			}
		}
		internal static Func<TextEditor, int, Task<List<InsertionPoint>>> GetInsertionPoints;
		internal static Action<TextEditor, DocumentContext, ITextSourceVersion, SyntaxToken?> StartRenameSession;

		async Task UpdateProjectionsDocuments (Document document, ITextDocument data)
		{
			var project = TypeSystemService.GetMonoProject (document.Project);
			var file = project.Files.GetFile (data.FileName);
			var node = TypeSystemService.GetTypeSystemParserNode (data.MimeType, file.BuildAction);
			if (node != null && node.Parser.CanGenerateProjection (data.MimeType, file.BuildAction, project.SupportedLanguages)) {
				var options = new ParseOptions {
					FileName = file.FilePath,
					Project = project,
					Content = TextFileProvider.Instance.GetReadOnlyTextEditorData (file.FilePath),
				};
				var projections = await node.Parser.GenerateProjections (options);
				UpdateProjectionEntry (file, projections);
				var projectId = GetProjectId (project);
				var projectdata = ProjectMap.GetData (projectId);
				foreach (var projected in projections) {
					OnDocumentTextChanged (projectdata.DocumentData.Get (projected.Document.FileName), new MonoDevelopSourceText (projected.Document), PreservationMode.PreserveValue);
				}
			}
		}

		static int ApplyChanges (Projection projection, ITextDocument data, List<Microsoft.CodeAnalysis.Text.TextChange> changes)
		{
			int delta = 0;
			foreach (var change in changes) {
				var offset = change.Span.Start;

				if (projection != null) {
					//If change is outside projection segments don't apply it...
					if (projection.TryConvertFromProjectionToOriginal (offset, out int originalOffset)) {
						offset = originalOffset;
						data.ReplaceText (offset, change.Span.Length, change.NewText);
						delta += change.Span.Length - change.NewText.Length;
					}
				} else {
					data.ReplaceText (offset, change.Span.Length, change.NewText);
					delta += change.Span.Length - change.NewText.Length;
				}
			}

			return delta;
		}

		// used to pass additional state from Apply* to TryApplyChanges so it can batch certain operations such as saving projects
		HashSet<MonoDevelop.Projects.Project> tryApplyState_changedProjects = new HashSet<MonoDevelop.Projects.Project> ();
		List<Task> tryApplyState_documentTextChangedTasks = new List<Task> ();
		Dictionary<string, SourceText> tryApplyState_documentTextChangedContents =  new Dictionary<string, SourceText> ();

		internal override bool TryApplyChanges (Solution newSolution, IProgressTracker progressTracker)
		{
			// this is supported on the main thread only
			// see https://github.com/dotnet/roslyn/pull/18043
			// as a result, we can assume that the things it calls are _also_ main thread only
			Runtime.CheckMainThread ();
			lock (projectModifyLock) {
				freezeProjectModify = true;
				try {
					var ret = base.TryApplyChanges (newSolution, progressTracker);

					if (tryApplyState_documentTextChangedTasks.Count > 0) {
						Task.WhenAll (tryApplyState_documentTextChangedTasks).ContinueWith (t => {
							try {
								t.Wait ();
							} catch (Exception ex) {
								LoggingService.LogError ("Error applying changes to documents", ex);
							}
							if (IdeApp.Workbench != null) {
								var changedFiles = new HashSet<string> (tryApplyState_documentTextChangedContents.Keys, FilePath.PathComparer);
								foreach (var w in IdeApp.Workbench.Documents) {
									if (w.IsFile && changedFiles.Contains (w.FileName)) {
										w.StartReparseThread ();
									}
								}
							}
						}, CancellationToken.None, TaskContinuationOptions.None, Runtime.MainTaskScheduler);
					}

					if (tryApplyState_changedProjects.Count > 0) {
						IdeApp.ProjectOperations.SaveAsync (tryApplyState_changedProjects);
					}

					return ret;
				} finally {
					tryApplyState_documentTextChangedContents.Clear ();
					tryApplyState_documentTextChangedTasks.Clear ();
					tryApplyState_changedProjects.Clear ();
					freezeProjectModify = false;
				}
			}
		}

		public override bool CanApplyChange (ApplyChangesKind feature)
		{
			switch (feature) {
			case ApplyChangesKind.AddDocument:
			case ApplyChangesKind.RemoveDocument:
			case ApplyChangesKind.ChangeDocument:
			//HACK: we don't actually support adding and removing metadata references from project
			//however, our MetadataReferenceCache currently depends on (incorrectly) using TryApplyChanges
			case ApplyChangesKind.AddMetadataReference:
			case ApplyChangesKind.RemoveMetadataReference:
			case ApplyChangesKind.AddProjectReference:
			case ApplyChangesKind.RemoveProjectReference:
				return true;
			default:
				return false;
			}
		}

		protected override void ApplyProjectChanges (ProjectChanges projectChanges)
		{
			this.projectChanges = projectChanges;
			base.ApplyProjectChanges (projectChanges);
		}

		protected override void ApplyDocumentAdded (DocumentInfo info, SourceText text)
		{
			var id = info.Id;
			MonoDevelop.Projects.Project mdProject = null;

			if (id.ProjectId != null) {
				var project = CurrentSolution.GetProject (id.ProjectId);
				mdProject = GetMonoProject (project);
				if (mdProject == null)
					LoggingService.LogWarning ("Couldn't find project for newly generated file {0} (Project {1}).", info.Name, info.Id.ProjectId);
			}

			var path = DetermineFilePath (info.Id, info.Name, info.FilePath, info.Folders, mdProject?.FileName.ParentDirectory, true);
			// If file is already part of project don't re-add it, example of this is .cshtml
			if (mdProject?.IsFileInProject (path) == true) {
				this.OnDocumentAdded (info);
				return;
			}
			info = info.WithFilePath (path).WithTextLoader (new MonoDevelopTextLoader (path));

			string formattedText;
			var formatter = CodeFormatterService.GetFormatter (DesktopService.GetMimeTypeForUri (path));
			if (formatter != null && mdProject != null) {
				formattedText = formatter.FormatText (mdProject.Policies, text.ToString ());
			} else {
				formattedText = text.ToString ();
			}

			var textSource = new StringTextSource (formattedText, text.Encoding ?? System.Text.Encoding.UTF8);
			try {
				textSource.WriteTextTo (path);
			} catch (Exception e) {
				LoggingService.LogError ("Exception while saving file to " + path, e);
			}

			if (mdProject != null) {
				var data = ProjectMap.GetData (id.ProjectId);
				data.DocumentData.Add (info.Id, path);
				var file = new MonoDevelop.Projects.ProjectFile (path);
				mdProject.Files.Add (file);
				tryApplyState_changedProjects.Add (mdProject);
			}

			this.OnDocumentAdded (info);
		}

		protected override void ApplyDocumentRemoved (DocumentId documentId)
		{
			var document = GetDocument (documentId);
			var mdProject = GetMonoProject (documentId.ProjectId);
			if (document == null || mdProject == null) {
				return;
			}

			FilePath filePath = document.FilePath;
			var projectFile = mdProject.Files.GetFile (filePath);
			if (projectFile == null) {
				return;
			}

			//force-close the old doc even if it's dirty
			var openDoc = IdeApp.Workbench.Documents.FirstOrDefault (d => d.IsFile && filePath.Equals (d.FileName));
			if (openDoc != null && openDoc.IsDirty) {
				openDoc.Save ();
				((Gui.SdiWorkspaceWindow)openDoc.Window).CloseWindow (true, true).Wait ();
			}

			//this will fire a OnDocumentRemoved event via OnFileRemoved
			mdProject.Files.Remove (projectFile);
			FileService.DeleteFile (filePath);
			tryApplyState_changedProjects.Add (mdProject);
		}

		string DetermineFilePath (DocumentId id, string name, string filePath, IReadOnlyList<string> docFolders, string defaultFolder, bool createDirectory = false)
		{
			var path = filePath;

			if (string.IsNullOrEmpty (path)) {
				var monoProject = GetMonoProject (id.ProjectId);

				// If the first namespace name matches the name of the project, then we don't want to
				// generate a folder for that.  The project is implicitly a folder with that name.
				IEnumerable<string> folders;
				if (docFolders != null && monoProject != null && docFolders.FirstOrDefault () == monoProject.Name) {
					folders = docFolders.Skip (1);
				} else {
					folders = docFolders;
				}

				if (folders.Any ()) {
					string baseDirectory = Path.Combine (monoProject?.BaseDirectory ?? MonoDevelopSolution.BaseDirectory, Path.Combine (folders.ToArray ()));
					try {
						if (createDirectory && !Directory.Exists (baseDirectory))
							Directory.CreateDirectory (baseDirectory);
					} catch (Exception e) {
						LoggingService.LogError ("Error while creating directory for a new file : " + baseDirectory, e);
					}
					path = Path.Combine (baseDirectory, name);
				} else {
					path = Path.Combine (defaultFolder, name);
				}
			}
			return path;
		}

		protected override void ApplyMetadataReferenceAdded (ProjectId projectId, MetadataReference metadataReference)
		{
			var mdProject = GetMonoProject (projectId) as MonoDevelop.Projects.DotNetProject;
			var path = GetMetadataPath (metadataReference);
			if (mdProject == null || path == null)
				return;
			foreach (var r in mdProject.References) {
				if (r.ReferenceType == MonoDevelop.Projects.ReferenceType.Assembly && r.Reference == path) {
					LoggingService.LogWarning ("Warning duplicate reference is added " + path);
					return;
				}

				if (r.ReferenceType == MonoDevelop.Projects.ReferenceType.Project) {
					foreach (var fn in r.GetReferencedFileNames (MonoDevelop.Projects.ConfigurationSelector.Default)) {
						if (fn == path) {
							LoggingService.LogWarning ("Warning duplicate reference is added " + path + " for project " + r.Reference);
							return;
						}
					}
				}
			}

			mdProject.AddReference (path);
			tryApplyState_changedProjects.Add (mdProject);
			this.OnMetadataReferenceAdded (projectId, metadataReference);
		}

		protected override void ApplyMetadataReferenceRemoved (ProjectId projectId, MetadataReference metadataReference)
		{
			var mdProject = GetMonoProject (projectId) as MonoDevelop.Projects.DotNetProject;
			var path = GetMetadataPath (metadataReference);
			if (mdProject == null || path == null)
				return;
			var item = mdProject.References.FirstOrDefault (r => r.ReferenceType == MonoDevelop.Projects.ReferenceType.Assembly && r.Reference == path);
			if (item == null)
				return;
			mdProject.References.Remove (item);
			tryApplyState_changedProjects.Add (mdProject);
			this.OnMetadataReferenceRemoved (projectId, metadataReference);
		}

		string GetMetadataPath (MetadataReference metadataReference)
		{
			if (metadataReference is PortableExecutableReference fileMetadata) {
				return fileMetadata.FilePath;
			}
			return null;
		}

		protected override void ApplyProjectReferenceAdded (ProjectId projectId, ProjectReference projectReference)
		{
			var mdProject = GetMonoProject (projectId) as MonoDevelop.Projects.DotNetProject;
			var projectToReference = GetMonoProject (projectReference.ProjectId);
			if (mdProject == null || projectToReference == null)
				return;
			var mdRef = MonoDevelop.Projects.ProjectReference.CreateProjectReference (projectToReference);
			mdProject.References.Add (mdRef);
			tryApplyState_changedProjects.Add (mdProject);
			this.OnProjectReferenceAdded (projectId, projectReference);
		}

		protected override void ApplyProjectReferenceRemoved (ProjectId projectId, ProjectReference projectReference)
		{
			var mdProject = GetMonoProject (projectId) as MonoDevelop.Projects.DotNetProject;
			var projectToReference = GetMonoProject (projectReference.ProjectId);
			if (mdProject == null || projectToReference == null)
				return;
			foreach (var pr in mdProject.References.OfType<MonoDevelop.Projects.ProjectReference>()) {
				if (pr.ProjectGuid == projectToReference.ItemId) {
					mdProject.References.Remove (pr);
					tryApplyState_changedProjects.Add (mdProject);
					this.OnProjectReferenceRemoved (projectId, projectReference);
					break;
				}
			}
		}

		#endregion

		internal Document GetDocument (DocumentId documentId, CancellationToken cancellationToken = default (CancellationToken))
		{
			var project = CurrentSolution.GetProject (documentId.ProjectId);
			if (project == null)
				return null;
			return project.GetDocument (documentId);
		}

		internal TextDocument GetAdditionalDocument (DocumentId documentId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var project = CurrentSolution.GetProject (documentId.ProjectId);
			if (project == null)
				return null;
			return project.GetAdditionalDocument (documentId);
		}

		internal Task UpdateFileContent (string fileName, string text)
		{
			SourceText newText = SourceText.From (text);
			var tasks = new List<Task> ();
			lock (updatingProjectDataLock) {
				foreach (var kv in ProjectMap.projectDataMap) {
					var projectId = kv.Key;
					var docId = this.GetDocumentId (projectId, fileName);
					if (docId != null) {
						try {
							if (this.GetDocument (docId) != null) {
								base.OnDocumentTextChanged (docId, newText, PreservationMode.PreserveIdentity);
							} else if (this.GetAdditionalDocument (docId) != null) {
								base.OnAdditionalDocumentTextChanged (docId, newText, PreservationMode.PreserveIdentity);
							}
						} catch (Exception e) {
							LoggingService.LogWarning ("Roslyn error on text change", e);
						}
					}
					var monoProject = GetMonoProject (projectId);
					if (monoProject != null) {
						var pf = monoProject.GetProjectFile (fileName);
						if (pf != null) {
							var mimeType = DesktopService.GetMimeTypeForUri (fileName);
							if (TypeSystemService.CanParseProjections (monoProject, mimeType, fileName)) {
								var parseOptions = new ParseOptions { Project = monoProject, FileName = fileName, Content = new StringTextSource (text), BuildAction = pf.BuildAction };
								var task = TypeSystemService.ParseProjection (parseOptions, mimeType);
								tasks.Add (task);
							}
						}
					}
				}
			}

			return Task.WhenAll (tasks);
		}

		internal void RemoveProject (MonoDevelop.Projects.Project project)
		{
			var id = GetProjectId (project);
			if (id != null) {
				foreach (var docId in GetOpenDocumentIds (id).ToList ()) {
					ClearOpenDocument (docId);
				}

				ProjectMap.RemoveProject (project, id);
				UnloadMonoProject (project);

				OnProjectRemoved (id);
			}
		}

		#region Project modification handlers

		List<MonoDevelop.Projects.DotNetProject> modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
		readonly object projectModifyLock = new object ();
		bool freezeProjectModify;
		Dictionary<MonoDevelop.Projects.DotNetProject, CancellationTokenSource> projectModifiedCts = new Dictionary<MonoDevelop.Projects.DotNetProject, CancellationTokenSource> ();
		void OnProjectModified (object sender, MonoDevelop.Projects.SolutionItemModifiedEventArgs args)
		{
			lock (projectModifyLock) {
				if (freezeProjectModify)
					return;
				try {
					if (!args.Any (x => x.Hint == "TargetFramework" || x.Hint == "References" || x.Hint == "CompilerParameters" || x.Hint == "Files"))
						return;
					var project = sender as MonoDevelop.Projects.DotNetProject;
					if (project == null)
						return;
					var projectId = GetProjectId (project);
					if (projectModifiedCts.TryGetValue (project, out var cts))
						cts.Cancel ();
					cts = new CancellationTokenSource ();
					projectModifiedCts [project] = cts;
					if (CurrentSolution.ContainsProject (projectId)) {
						var projectInfo = ProjectHandler.LoadProject (project, cts.Token, null).ContinueWith (t => {
							if (t.IsCanceled)
								return;
							if (t.IsFaulted) {
								LoggingService.LogError ("Failed to reload project", t.Exception);
								return;
							}
							try {
								lock (projectModifyLock) {
									// correct openDocument ids - they may change due to project reload.
									OpenDocuments.CorrectDocumentIds (project, t.Result);
									OnProjectReloaded (t.Result);
								}
							} catch (Exception e) {
								LoggingService.LogError ("Error while reloading project " + project.Name, e);
							}
						}, cts.Token);
					} else {
						modifiedProjects.Add (project);
					}
				} catch (Exception ex) {
					LoggingService.LogInternalError (ex);
				}
			}
		}

		#endregion
	}

	//	static class MonoDevelopWorkspaceFeatures
	//	{
	//		static FeaturePack pack;
	//
	//		public static FeaturePack Features {
	//			get {
	//				if (pack == null)
	//					Interlocked.CompareExchange (ref pack, ComputePack (), null);
	//				return pack;
	//			}
	//		}
	//
	//		static FeaturePack ComputePack ()
	//		{
	//			var assemblies = new List<Assembly> ();
	//			var workspaceCoreAssembly = typeof(Workspace).Assembly;
	//			assemblies.Add (workspaceCoreAssembly);
	//
	//			LoadAssembly (assemblies, "Microsoft.CodeAnalysis.CSharp.Workspaces");
	//			//LoadAssembly (assemblies, "Microsoft.CodeAnalysis.VisualBasic.Workspaces");
	//
	//			var catalogs = assemblies.Select (a => new System.ComponentModel.Composition.Hosting.AssemblyCatalog (a));
	//
	//			return new MefExportPack (catalogs);
	//		}
	//
	//		static void LoadAssembly (List<Assembly> assemblies, string assemblyName)
	//		{
	//			try {
	//				var loadedAssembly = Assembly.Load (assemblyName);
	//				assemblies.Add (loadedAssembly);
	//			} catch (Exception e) {
	//				LoggingService.LogWarning ("Couldn't load assembly:" + assemblyName, e);
	//			}
	//		}
	//	}

	public class RoslynProjectEventArgs : EventArgs
	{
		public ProjectId ProjectId { get; private set; }

		public RoslynProjectEventArgs (ProjectId projectId)
		{
			ProjectId = projectId;
		}
	}

}
