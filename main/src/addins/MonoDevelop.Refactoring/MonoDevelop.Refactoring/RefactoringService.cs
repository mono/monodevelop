// 
// RefactoringService.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

//#define PROFILE

using System;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using System.Linq;
using MonoDevelop.AnalysisCore;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.CodeActions;
using MonoDevelop.CodeIssues;
using MonoDevelop.Ide.TypeSystem;
using System.Diagnostics;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.Refactoring
{ 
	public static class RefactoringService
	{
		internal static Func<TextEditor, DocumentContext, OptionSet> OptionSetCreation;
		static ImmutableList<FindReferencesProvider> findReferencesProvider = ImmutableList<FindReferencesProvider>.Empty;
		static ImmutableList<FindReferenceUsagesProvider> findReferenceUsagesProviders = ImmutableList<FindReferenceUsagesProvider>.Empty;
		static List<JumpToDeclarationHandler> jumpToDeclarationHandler = new List<JumpToDeclarationHandler> ();

		static RefactoringService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/FindReferencesProvider", delegate(object sender, ExtensionNodeEventArgs args) {
				var provider  = (FindReferencesProvider) args.ExtensionObject;
				switch (args.Change) {
					case ExtensionChange.Add:
					findReferencesProvider = findReferencesProvider.Add (provider);
					break;
					case ExtensionChange.Remove:
					findReferencesProvider = findReferencesProvider.Remove (provider);
					break;
				}
			});

			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/FindReferenceUsagesProvider", delegate(object sender, ExtensionNodeEventArgs args) {
				var provider  = (FindReferenceUsagesProvider) args.ExtensionObject;
				switch (args.Change) {
				case ExtensionChange.Add:
					findReferenceUsagesProviders = findReferenceUsagesProviders.Add (provider);
					break;
				case ExtensionChange.Remove:
					findReferenceUsagesProviders = findReferenceUsagesProviders.Remove (provider);
					break;
				}
			});

			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/JumpToDeclarationHandler", delegate(object sender, ExtensionNodeEventArgs args) {
				var provider  = (JumpToDeclarationHandler) args.ExtensionObject;
				switch (args.Change) {
					case ExtensionChange.Add:
					jumpToDeclarationHandler.Add (provider);
					break;
					case ExtensionChange.Remove:
					jumpToDeclarationHandler.Remove (provider);
					break;
				}
			});

			//legacy option, no longer used
			PropertyService.Set ("CodeActionUsages", null);
		}
		
		class RenameHandler 
		{
			readonly IEnumerable<Change> changes;
			public RenameHandler (IEnumerable<Change> changes)
			{
				this.changes = changes;
			}
			public void FileRename (object sender, FileCopyEventArgs e)
			{
				foreach (FileCopyEventInfo args in e) {
					foreach (Change change in changes) {
						var replaceChange = change as TextReplaceChange;
						if (replaceChange == null)
							continue;
						if (args.SourceFile == replaceChange.FileName)
							replaceChange.FileName = args.TargetFile;
					}
				}
			}
		}
		
		public static void AcceptChanges (ProgressMonitor monitor, IList<Change> changes)
		{
			AcceptChanges (monitor, changes, MonoDevelop.Ide.TextFileProvider.Instance);
		}

		public static async Task RoslynJumpToDeclaration (ISymbol symbol, Projects.Project hintProject = null, CancellationToken token = default(CancellationToken))
		{
			var result = await TryJumpToDeclarationAsync (symbol.GetDocumentationCommentId (), hintProject, token).ConfigureAwait (false);
			if (!result) {
				await Runtime.RunInMainThread (delegate {
					IdeApp.ProjectOperations.JumpToDeclaration (symbol, hintProject);
				});
			}
		}

		public static void AcceptChanges (ProgressMonitor monitor, IList<Change> changes, MonoDevelop.Ide.ITextFileProvider fileProvider)
		{
			var rctx = new RefactoringOptions (null, null);
			var handler = new RenameHandler (changes);
			FileService.FileRenamed += handler.FileRename;
			var ws = TypeSystemService.Workspace as MonoDevelopWorkspace;
			string originalName;
			int originalOffset;
			try {
				for (int i = 0; i < changes.Count; i++) {
					var change = changes [i] as TextReplaceChange;
					if (change == null)
						continue;

					if (ws.TryGetOriginalFileFromProjection (change.FileName, change.Offset, out originalName, out originalOffset)) {
						change.FileName = originalName;
						change.Offset = originalOffset;
					}
				}
				if (changes.All (x => x is TextReplaceChange)) {
					List<Change> newChanges = new List<Change> (changes);
					newChanges.Sort ((Change x, Change y) => ((TextReplaceChange)x).Offset.CompareTo (((TextReplaceChange)y).Offset));
					changes = newChanges;
				}


				for (int i = 0; i < changes.Count; i++) {
					changes [i].PerformChange (monitor, rctx);
					var replaceChange = changes [i] as TextReplaceChange;
					if (replaceChange == null)
						continue;

					for (int j = i + 1; j < changes.Count; j++) {
						var change = changes [j] as TextReplaceChange;
						if (change == null)
							continue;

						if (replaceChange.Offset >= 0 && change.Offset >= 0 && replaceChange.FileName == change.FileName) {
							if (replaceChange.Offset < change.Offset) {
								change.Offset -= replaceChange.RemovedChars;
								if (!string.IsNullOrEmpty (replaceChange.InsertedText))
									change.Offset += replaceChange.InsertedText.Length;
							} else if (replaceChange.Offset < change.Offset + change.RemovedChars) {
								change.RemovedChars = Math.Max (0, change.RemovedChars - replaceChange.RemovedChars);
								change.Offset = replaceChange.Offset + (!string.IsNullOrEmpty (replaceChange.InsertedText) ? replaceChange.InsertedText.Length : 0);
							}
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while applying refactoring changes", e);
			} finally {
				FileService.FileRenamed -= handler.FileRename;
				TextReplaceChange.FinishRefactoringOperation ();
			}
		}

		//		public static void QueueQuickFixAnalysis (Document doc, TextLocation loc, CancellationToken token, Action<List<CodeAction>> callback)
		//		{
		//			var ext = doc.GetContent<MonoDevelop.AnalysisCore.Gui.ResultsEditorExtension> ();
		//			var issues = ext != null ? ext.GetResultsAtOffset (doc.Editor.LocationToOffset (loc), token).OrderBy (r => r.Level).ToList () : new List<Result> ();
		//
		//			ThreadPool.QueueUserWorkItem (delegate {
		//				try {
		//					var result = new List<CodeAction> ();
		//					foreach (var r in issues) {
		//						if (token.IsCancellationRequested)
		//							return;
		//						var fresult = r as FixableResult;
		//						if (fresult == null)
		//							continue;
		////						foreach (var action in FixOperationsHandler.GetActions (doc, fresult)) {
		////							result.Add (new AnalysisContextActionProvider.AnalysisCodeAction (action, r) {
		////								DocumentRegion = action.DocumentRegion
		////							});
		////						}
		//					}
		//					result.AddRange (GetValidActions (doc, loc).Result);
		//					callback (result);
		//				} catch (Exception ex) {
		//					LoggingService.LogError ("Error in analysis service", ex);
		//				}
		//			});
		//		}	

		public static MonoDevelop.Ide.Editor.DocumentLocation GetCorrectResolveLocation (IReadonlyTextDocument editor, MonoDevelop.Ide.Editor.DocumentLocation location)
		{
			if (editor == null || location.Column == 1)
				return location;

			/*if (editor is TextEditor) {
				if (((TextEditor)editor).IsSomethingSelected)
					return ((TextEditor)editor).SelectionRegion.Begin;
			}*/
			var line = editor.GetLine (location.Line);
			if (line == null || location.Column > line.LengthIncludingDelimiter)
				return location;
			int offset = editor.LocationToOffset (location);
			if (offset > 0 && !char.IsLetterOrDigit (editor.GetCharAt (offset)) && char.IsLetterOrDigit (editor.GetCharAt (offset - 1)))
				return new MonoDevelop.Ide.Editor.DocumentLocation (location.Line, location.Column - 1);
			return location;
		}

		public static async Task FindReferencesAsync (string documentIdString, Projects.Project hintProject = null)
		{
			if (hintProject == null)
				hintProject = IdeApp.Workbench.ActiveDocument?.Project;
			ITimeTracker timer = null;
			var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			try {
				var metadata = Counters.CreateFindReferencesMetadata ();
				timer = Counters.FindReferences.BeginTiming (metadata);
				var tasks = new List<(Task task, FindReferencesProvider provider)> (findReferencesProvider.Count);
				foreach (var provider in findReferencesProvider) {
					try {
						tasks.Add ((provider.FindReferences (documentIdString, hintProject, monitor), provider));
					} catch (OperationCanceledException) {
						metadata.SetUserCancel ();
						return;
					} catch (Exception ex) {
						metadata.SetFailure ();
						if (monitor != null)
							monitor.ReportError ("Error finding references", ex);
						LoggingService.LogError ("Error finding references", ex);
						findReferencesProvider = findReferencesProvider.Remove (provider);
					}
				}
				foreach (var task in tasks) {
					try {
						await task.task;
					} catch (OperationCanceledException) {
						metadata.SetUserCancel ();
						return;
					} catch (Exception ex) {
						metadata.SetFailure ();
						if (monitor != null)
							monitor.ReportError ("Error finding references", ex);
						LoggingService.LogError ("Error finding references", ex);
						findReferencesProvider = findReferencesProvider.Remove (task.provider);
					}
				}
			} finally {
				if (monitor != null)
					monitor.Dispose ();
				if (timer != null)
					timer.Dispose ();
			}
		}

		public static async Task FindReferenceUsagesAsync(Projects.ProjectReference projectReference)
		{
			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
				try {
					foreach (var provider in findReferenceUsagesProviders) {
						await provider.FindReferences(projectReference, monitor);
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Error finding reference usages", ex);
				}
			}
		}

		public static async Task FindAllReferencesAsync (string documentIdString, Projects.Project hintProject = null)
		{
			if (hintProject == null)
				hintProject = IdeApp.Workbench.ActiveDocument?.Project;
			ITimeTracker timer = null;
			var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			try {
				var metadata = Counters.CreateFindReferencesMetadata ();
				timer = Counters.FindReferences.BeginTiming (metadata);
				var tasks = new List<(Task task, FindReferencesProvider provider)> (findReferencesProvider.Count);
				foreach (var provider in findReferencesProvider) {
					try {
						tasks.Add ((provider.FindAllReferences (documentIdString, hintProject, monitor), provider));
					} catch (OperationCanceledException) {
						metadata.SetUserCancel ();
						return;
					} catch (Exception ex) {
						metadata.SetFailure ();
						if (monitor != null)
							monitor.ReportError ("Error finding references", ex);
						LoggingService.LogError ("Error finding references", ex);
						findReferencesProvider = findReferencesProvider.Remove (provider);
					}
				}
				foreach (var task in tasks) {
					try {
						await task.task;
					} catch (OperationCanceledException) {
						metadata.SetUserCancel ();
						return;
					} catch (Exception ex) {
						metadata.SetFailure ();
						if (monitor != null)
							monitor.ReportError ("Error finding references", ex);
						LoggingService.LogError ("Error finding references", ex);
						findReferencesProvider = findReferencesProvider.Remove (task.provider);
					}
				}
			} finally {
				if (monitor != null)
					monitor.Dispose ();
				if (timer != null)
					timer.Dispose ();
			}
		}

		public static async Task<bool> TryJumpToDeclarationAsync (string documentIdString, Projects.Project hintProject = null, CancellationToken token = default(CancellationToken))
		{
				if (hintProject == null)
					hintProject = IdeApp.Workbench.ActiveDocument?.Project;
			for (int i = 0; i < jumpToDeclarationHandler.Count; i++) {
				var handler = jumpToDeclarationHandler [i];
				try {
					if (await handler.TryJumpToDeclarationAsync (documentIdString, hintProject, token))
						return true;
				} catch (OperationCanceledException) {
				} catch (Exception ex) {
					LoggingService.LogError ("Error jumping to declaration", ex);
				}
			}

			return false;
		}
	}

	internal static class Counters
	{
		public static TimerCounter<FindReferencesMetadata> FindReferences = InstrumentationService.CreateTimerCounter<FindReferencesMetadata> ("Find references", "Code Navigation", id: "CodeNavigation.FindReferences");
		public static TimerCounter<FixesMenuMetadata> FixesMenu = InstrumentationService.CreateTimerCounter<FixesMenuMetadata> ("Show fixes", "Code Actions", id: "CodeActions.ShowFixes");

		public static FindReferencesMetadata CreateFindReferencesMetadata ()
		{
			var metadata = new FindReferencesMetadata {
				ResultString = "Success"
			};
			return metadata;
		}

		public class FindReferencesMetadata : CounterMetadata
		{
			public string ResultString {
				get => (string)Properties["Result"];
				set => Properties["Result"] = value;
			}
		}

		public class FixesMenuMetadata : CounterMetadata
		{
			public FixesMenuMetadata ()
			{
			}

			public bool TriggeredBySmartTag {
				get => GetProperty<bool> ();
				set => SetProperty (value);
			}
		}
	}
}
