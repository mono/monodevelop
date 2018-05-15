// 
// RenameTextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Core;
using MonoDevelop.Refactoring.Rename;
using ICSharpCode.NRefactory6.CSharp.Features.GotoDefinition;
using System.Threading;
using Microsoft.CodeAnalysis;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonoDevelop.CSharp.Refactoring
{
	sealed class CSharpFeaturesTextEditorExtension : TextEditorExtension
	{
		static CSharpFeaturesTextEditorExtension ()
		{
			GoToDefinitionService.TryNavigateToSymbol = delegate (ISymbol symbol, Microsoft.CodeAnalysis.Project project, bool usePreviewTab) {
				RefactoringService.RoslynJumpToDeclaration (symbol, TypeSystemService.GetMonoProject (project));
				return true;
			};

			GoToDefinitionService.TryNavigateToSpan = delegate (Workspace workspace, DocumentId documentId, Microsoft.CodeAnalysis.Text.TextSpan textSpan, bool usePreviewTab) {
				var project = workspace.CurrentSolution.GetProject (documentId.ProjectId);
				if (project == null)
					return false;
				var fileName = project.GetDocument (documentId).FilePath;
				var offset = textSpan.Start;
				string projectedName;
				int projectedOffset;
				if (TypeSystemService.GetWorkspace (TypeSystemService.GetMonoProject(project).ParentSolution).TryGetOriginalFileFromProjection (fileName, offset, out projectedName, out projectedOffset)) {
					fileName = projectedName;
					offset = projectedOffset;
				}
				IdeApp.Workbench.OpenDocument (new FileOpenInformation (fileName, TypeSystemService.GetMonoProject (project)) {
					Offset = offset
				});
				return true;
			};

			GoToDefinitionService.DisplayMultiple = delegate (IEnumerable<Tuple<Solution, ISymbol, Location>> list) {
				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
					foreach (var part in list) {
						if (monitor.CancellationToken.IsCancellationRequested)
							return;

						monitor.ReportResult (GotoDeclarationHandler.GetJumpTypePartSearchResult (part.Item2, part.Item3));
					}
				}
			};
		}


		public override bool IsValidInContext (MonoDevelop.Ide.Editor.DocumentContext context)
		{
			return context.Name != null && context.Name.EndsWith (".cs", FilePath.PathComparison);
		}

		[CommandUpdateHandler (EditCommands.Rename)]
		public void RenameCommand_Update (CommandInfo ci)
		{
			new RenameHandler ().UpdateCommandInfo (ci);
		}

		[CommandHandler (EditCommands.Rename)]
		public async void RenameCommand ()
		{
			await new RenameHandler ().Run (Editor, DocumentContext);
		}

		[CommandHandler (TextEditorCommands.ExpandSelection)]
		public void ExpandSelection ()
		{
			ExpandSelectionHandler.Run ();
		}

		[CommandHandler (TextEditorCommands.ShrinkSelection)]
		public void ShrinkSelection ()
		{
			ShrinkSelectionHandler.Run ();
		}

		[CommandUpdateHandler (RefactoryCommands.GotoDeclaration)]
		public async Task GotoDeclaration_Update (CommandInfo ci, CancellationToken cancellationToken)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			if (DocumentContext.AnalysisDocument == null) {
				ci.Enabled = false;
				return;
			}
			var symbol = await GoToDefinitionService.FindSymbolAsync (base.DocumentContext.AnalysisDocument, Editor.CaretOffset, cancellationToken);
			ci.Enabled = symbol != null;
		}

		[CommandHandler (RefactoryCommands.GotoDeclaration)]
		public void GotoDeclaration ()
		{
			GoToDefinitionService.TryGoToDefinition (base.DocumentContext.AnalysisDocument, Editor.CaretOffset, default(CancellationToken));
		}

		static readonly FindReferencesHandler findReferencesHandler = new FindReferencesHandler ();
		[CommandUpdateHandler (RefactoryCommands.FindReferences)]
		public void FindReferences_Update (CommandInfo ci)
		{
			findReferencesHandler.Update (ci);
		}

		[CommandHandler (RefactoryCommands.FindReferences)]
		public void FindReferences ()
		{
			findReferencesHandler.Run (null);
		}

		static readonly FindAllReferencesHandler findAllReferencesHandler = new FindAllReferencesHandler ();
		[CommandUpdateHandler (RefactoryCommands.FindAllReferences)]
		public void FindAllReferencesHandler_Update (CommandInfo ci)
		{
			findAllReferencesHandler.Update (ci);
		}

		[CommandHandler (RefactoryCommands.FindAllReferences)]
		public void FindAllReferences ()
		{
			findAllReferencesHandler.Run (null);
		}
	}
}

