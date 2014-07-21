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

namespace MonoDevelop.Refactoring
{
	public static class RefactoringService
	{
		static RefactoringService ()
		{

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
		
		public static void AcceptChanges (IProgressMonitor monitor, List<Change> changes, MonoDevelop.Ide.ITextFileProvider fileProvider)
		{
			var rctx = new RefactoringOptions (null, null);
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

		//static readonly CodeAnalysisBatchRunner runner = new CodeAnalysisBatchRunner();

		/// <summary>
		/// Queues a code analysis job.
		/// </summary>
		/// <param name="job">The job to queue.</param>
		/// <param name="progressMessage">
		/// The message used for a progress monitor, or null if no progress monitor should be used.
		/// </param>
//		public static IJobContext QueueCodeIssueAnalysis(IAnalysisJob job, string progressMessage = null)
//		{
//			if (progressMessage != null)
//				job = new ProgressMonitorWrapperJob (job, progressMessage);
//			return runner.QueueJob (job);
//			return null;
//		}
	}
}
