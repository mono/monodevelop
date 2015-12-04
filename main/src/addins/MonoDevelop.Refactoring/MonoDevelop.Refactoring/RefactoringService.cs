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
using ICSharpCode.NRefactory;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.CodeActions;
using MonoDevelop.CodeIssues;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using System.Diagnostics;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide;

namespace MonoDevelop.Refactoring
{
	public static class RefactoringService
	{
		static readonly List<RefactoringOperation> refactorings = new List<RefactoringOperation>();
		static readonly List<CodeActionProvider> contextActions = new List<CodeActionProvider> ();
		static readonly List<CodeIssueProvider> inspectors = new List<CodeIssueProvider> ();
		
		public static IEnumerable<CodeActionProvider> ContextAddinNodes {
			get {
				return contextActions;
			}
		} 

		public static void AddProvider (CodeActionProvider provider)
		{
			contextActions.Add (provider);
		}
		
		public static void AddProvider (CodeIssueProvider provider)
		{
			inspectors.Add (provider);
		}

		public static List<CodeIssueProvider> Inspectors {
			get {
				return inspectors;
			}
		}

		static RefactoringService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/Refactorings", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					refactorings.Add ((RefactoringOperation)args.ExtensionObject);
					break;
				case ExtensionChange.Remove:
					refactorings.Remove ((RefactoringOperation)args.ExtensionObject);
					break;
				}
			});

			
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/CodeActions", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					contextActions.Add (((CodeActionAddinNode)args.ExtensionNode).Action);
					break;
				case ExtensionChange.Remove:
					contextActions.Remove (((CodeActionAddinNode)args.ExtensionNode).Action);
					break;
				}
			});
			
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/CodeActionSource", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					contextActions.AddRange (((ICodeActionProviderSource)args.ExtensionObject).GetProviders ());
					break;
				}
			});
			
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/CodeIssues", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					inspectors.Add (((CodeIssueAddinNode)args.ExtensionNode).Inspector);
					break;
				case ExtensionChange.Remove:
					inspectors.Remove (((CodeIssueAddinNode)args.ExtensionNode).Inspector);
					break;
				}
			});
			
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/CodeIssueSource", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					var source = (ICodeIssueProviderSource)args.ExtensionObject;
					var providers = source.GetProviders ();
					inspectors.AddRange (providers);
					break;
				}
			});
			
		}
		
		public static IEnumerable<RefactoringOperation> Refactorings {
			get {
				return refactorings;
			}
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
		
		public static void AcceptChanges (IProgressMonitor monitor, List<Change> changes)
		{
			AcceptChanges (monitor, changes, MonoDevelop.Ide.TextFileProvider.Instance);
		}
		
		public static void AcceptChanges (IProgressMonitor monitor, List<Change> changes, MonoDevelop.Projects.Text.ITextFileProvider fileProvider)
		{
			var rctx = new RefactoringOptions (null);
			var handler = new RenameHandler (changes);
			FileService.FileRenamed += handler.FileRename;
			var fileNames = new HashSet<FilePath> ();
			for (int i = 0; i < changes.Count; i++) {
				changes[i].PerformChange (monitor, rctx);
				var replaceChange = changes[i] as TextReplaceChange;
				if (replaceChange == null)
					continue;
				for (int j = i + 1; j < changes.Count; j++) {
					var change = changes[j] as TextReplaceChange;
					if (change == null)
						continue;
					fileNames.Add (change.FileName);
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
			FileService.NotifyFilesChanged (fileNames);
			FileService.FileRenamed -= handler.FileRename;
			TextReplaceChange.FinishRefactoringOperation ();
		}
		
		public static IEnumerable<CodeIssueProvider> GetInspectors (string mimeType)
		{
			return inspectors.Where (i => i.MimeType == mimeType);
		}

		static Stopwatch validActionsWatch = new Stopwatch ();
		static Stopwatch actionWatch = new Stopwatch ();

		public static IEnumerable<CodeAction> GetValidActions (Document doc, TextLocation loc, CancellationToken cancellationToken = default (CancellationToken))
		{
			var editor = doc.Editor;
			string disabledNodes = editor != null ? PropertyService.Get ("ContextActions." + editor.MimeType, "") ?? "" : "";
			var result = new List<CodeAction> ();
			var timer = InstrumentationService.CreateTimerCounter ("Source analysis background task", "Source analysis");
			timer.BeginTiming ();
			validActionsWatch.Restart ();
			var timeTable = new Dictionary<CodeActionProvider, long> ();
			try {
				var parsedDocument = doc.ParsedDocument;
				if (editor != null && parsedDocument != null && parsedDocument.CreateRefactoringContext != null) {
					var ctx = parsedDocument.CreateRefactoringContext (doc, cancellationToken);
					if (ctx != null) {
						foreach (var provider in contextActions.Where (fix =>
							fix.MimeType == editor.MimeType &&
							disabledNodes.IndexOf (fix.IdString, StringComparison.Ordinal) < 0))
						{
							try {
								actionWatch.Restart ();
								result.AddRange (provider.GetActions (doc, ctx, loc, cancellationToken));
								actionWatch.Stop ();
								timeTable[provider] = actionWatch.ElapsedMilliseconds;
							} catch (Exception ex) {
								LoggingService.LogError ("Error in context action provider " + provider.Title, ex);
							}
						}
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error in analysis service", ex);
			} finally {
				timer.EndTiming ();
				validActionsWatch.Stop ();
				if (validActionsWatch.ElapsedMilliseconds > 1000) {
					LoggingService.LogWarning ("Warning slow edit action update."); 
					foreach (var pair in timeTable) {
						if (pair.Value > 50)
							LoggingService.LogInfo ("ACTION '" + pair.Key.Title + "' took " + pair.Value +"ms"); 
					}
				}
			}
			return (IEnumerable<CodeAction>)result;
		}

		public static void QueueQuickFixAnalysis (Document doc, TextLocation loc, CancellationToken token, Action<List<CodeAction>> callback)
		{
			var ext = doc.GetContent<MonoDevelop.AnalysisCore.Gui.ResultsEditorExtension> ();
			var issues = ext != null ? ext.GetResultsAtOffset (doc.Editor.LocationToOffset (loc), token).OrderBy (r => r.Level).ToList () : new List<Result> ();

			ThreadPool.QueueUserWorkItem (delegate {
				try {
					var result = new List<CodeAction> ();
					foreach (var r in issues) {
						if (token.IsCancellationRequested)
							return;
						var fresult = r as FixableResult;
						if (fresult == null)
							continue;
						foreach (var action in FixOperationsHandler.GetActions (doc, fresult)) {
							result.Add (new AnalysisContextActionProvider.AnalysisCodeAction (action, r) {
								DocumentRegion = action.DocumentRegion
							});
						}
					}
					result.AddRange (GetValidActions (doc, loc));
					callback (result);
				} catch (Exception ex) {
					LoggingService.LogError ("Error in analysis service", ex);
				}
			});
		}	

		public static IList<CodeAction> ApplyFixes (IEnumerable<CodeAction> fixes, IRefactoringContext refactoringContext)
		{
			if (fixes == null)
				throw new ArgumentNullException ("fixes");
			if (refactoringContext == null)
				throw new ArgumentNullException ("refactoringContext");
			var allFixes = fixes as IList<CodeAction> ?? fixes.ToArray ();
			if (allFixes.Count == 0)
				return new List<CodeAction> ();
				
			var scriptProvider = refactoringContext as IRefactoringContext;
			if (scriptProvider == null) {
				return RunAll (allFixes, refactoringContext, null);
			}
			using (var script = scriptProvider.CreateScript ()) {
				return RunAll (allFixes, refactoringContext, script);
			}
		}

		const string EnableRefactorings = "RefactoringSettings.EnableRefactorings";

		internal static bool CheckUserSettings()
		{
			var hasRefactoringSettings = IdeApp.ProjectOperations.CurrentSelectedSolution == null ||
				IdeApp.ProjectOperations.CurrentSelectedSolution.UserProperties.HasValue (EnableRefactorings);
			if (!hasRefactoringSettings) {
				var useRefactoringsButton     = new AlertButton (GettextCatalog.GetString("Use refactorings on this solution"));
				var text = GettextCatalog.GetString (
@"WARNING: The Xamarin Studio refactoring operations do not yet support C# 6.

You may continue to use refactoring operations with C# 6, however you should check the results carefully to make sure that they have not made incorrect changes to your code. In particular, the ""?."" null propagating dereference will be changed to ""."", a simple dereference, which can cause unexpected NullReferenceExceptions at runtime.");
				var message = new QuestionMessage (text);
				message.Buttons.Add (useRefactoringsButton);
				message.Buttons.Add (AlertButton.Cancel);
				message.Icon = Gtk.Stock.DialogWarning;
				message.DefaultButton = 1;

				var result = MessageService.AskQuestion (message);
				if (result == AlertButton.Cancel)
					return false;
				ShowFixes = result == useRefactoringsButton;
			}
			return ShowFixes;
		}

		internal static bool ShowFixes {
			get {
				if (Ide.IdeApp.ProjectOperations.CurrentSelectedSolution != null) {
					var hasRefactoringSettings = IdeApp.ProjectOperations.CurrentSelectedSolution.UserProperties.HasValue (EnableRefactorings);
					return !hasRefactoringSettings || IdeApp.ProjectOperations.CurrentSelectedSolution.UserProperties.GetValue<bool> (EnableRefactorings);
				}
				return true;
			}
			set {
				IdeApp.ProjectOperations.CurrentSelectedSolution.UserProperties.SetValue (EnableRefactorings, value);
				IdeApp.ProjectOperations.CurrentSelectedSolution.SaveUserProperties ();
			}
		}


		public static void ApplyFix (CodeAction action, IRefactoringContext context)
		{
			if (!CheckUserSettings ())
				return;
			using (var script = context.CreateScript ()) {
				action.Run (context, script);
			}
		}

		static List<CodeAction> RunAll (IEnumerable<CodeAction> allFixes, IRefactoringContext refactoringContext, object script)
		{
			var appliedFixes = new List<CodeAction> ();
			foreach (var fix in allFixes) {
				fix.Run (refactoringContext, script);
				appliedFixes.Add (fix);
			}
			return appliedFixes;
		}

		public static DocumentLocation GetCorrectResolveLocation (Document doc, DocumentLocation location)
		{
			if (doc == null)
				return location;
			var editor = doc.Editor;
			if (editor == null || location.Column == 1)
				return location;

			if (editor.IsSomethingSelected)
				return editor.MainSelection.Start;

			var line = editor.GetLine (location.Line);
			if (line == null || location.Column > line.LengthIncludingDelimiter)
				return location;
			int offset = editor.LocationToOffset (location);
			if (offset > 0 && !char.IsLetterOrDigit (doc.Editor.GetCharAt (offset)) && char.IsLetterOrDigit (doc.Editor.GetCharAt (offset - 1)))
				return new DocumentLocation (location.Line, location.Column - 1);
			return location;
		}

		static readonly CodeAnalysisBatchRunner runner = new CodeAnalysisBatchRunner();

		/// <summary>
		/// Queues a code analysis job.
		/// </summary>
		/// <param name="job">The job to queue.</param>
		/// <param name="progressMessage">
		/// The message used for a progress monitor, or null if no progress monitor should be used.
		/// </param>
		public static IJobContext QueueCodeIssueAnalysis(IAnalysisJob job, string progressMessage = null)
		{
			if (progressMessage != null)
				job = new ProgressMonitorWrapperJob (job, progressMessage);
			return runner.QueueJob (job);
		}
	}
}
