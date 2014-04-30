// 
// FindReferencesHandler.cs
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

using System;
using System.Threading;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.Tasks;
using System.Threading.Tasks;

namespace MonoDevelop.Refactoring
{
	public class FindReferencesHandler : CommandHandler
	{
		public static void FindRefs (ISymbol obj)
		{
			var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			var solution = RoslynTypeSystemService.Workspace.CurrentSolution;
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					foreach (var mref in SymbolFinder.FindReferencesAsync (obj, solution).Result) {
						foreach (var loc in mref.Locations) {
							var sr = new SearchResult (new FileProvider (loc.Document.FilePath), loc.Location.SourceSpan.Start, loc.Location.SourceSpan.Length);
							monitor.ReportResult (sr);
						}
					}
				} catch (Exception ex) {
					if (monitor != null)
						monitor.ReportError ("Error finding references", ex);
					else
						LoggingService.LogError ("Error finding references", ex);
				} finally {
					if (monitor != null)
						monitor.Dispose ();
				}
			});
		}
		protected override void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			
			var info = CurrentRefactoryOperationsHandler.GetSymbolInfoAsync (doc.AnalysisDocument, doc.Editor.Caret.Offset).Result;
			var sym = info.Symbol ?? info.DeclaredSymbol;
			if (sym != null)
				FindRefs (sym);
		}
	}

	public class FindAllReferencesHandler : CommandHandler
	{
		public static void FindRefs (ISymbol obj, Task<Compilation> compilation)
		{
			var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			var solution = RoslynTypeSystemService.Workspace.CurrentSolution;
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					foreach (var simSym in SymbolFinder.FindSimilarSymbols (obj, compilation.Result)) {
						foreach (var mref in SymbolFinder.FindReferencesAsync (simSym, solution).Result) {
							foreach (var loc in mref.Locations) {
								var sr = new SearchResult (new FileProvider (loc.Document.FilePath), loc.Location.SourceSpan.Start, loc.Location.SourceSpan.Length);
								monitor.ReportResult (sr);
							}
						}
					}
				} catch (Exception ex) {
					if (monitor != null)
						monitor.ReportError ("Error finding references", ex);
					else
						LoggingService.LogError ("Error finding references", ex);
				} finally {
					if (monitor != null)
						monitor.Dispose ();
				}
			});
		}
		
		protected override void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			var info = CurrentRefactoryOperationsHandler.GetSymbolInfoAsync (doc.AnalysisDocument, doc.Editor.Caret.Offset).Result;
			var sym = info.Symbol ?? info.DeclaredSymbol;
			if (sym != null)
				FindRefs (sym, doc.GetCompilationAsync ());
		}
	}
}
