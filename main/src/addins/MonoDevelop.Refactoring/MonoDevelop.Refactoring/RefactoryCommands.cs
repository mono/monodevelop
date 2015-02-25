//
// RefactoryCommands.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.CodeActions;
using MonoDevelop.CodeIssues;
using Microsoft.CodeAnalysis.CSharp;

namespace MonoDevelop.Refactoring
{
	public enum RefactoryCommands
	{
		CurrentRefactoryOperations,
		GotoDeclaration, // in 'referenced' in IdeViMode.cs as string
		FindReferences,
		FindAllReferences,
		FindDerivedClasses,
		DeclareLocal,
		RemoveUnusedImports,
		SortImports,
		RemoveSortImports,
		ExtractMethod,
		CreateMethod,
		IntroduceConstant,
		IntegrateTemporaryVariable,
		ImportSymbol,
		QuickFix,
		Resolve
	}

	public class RefactoringSymbolInfo
	{
		public readonly static RefactoringSymbolInfo Empty = new RefactoringSymbolInfo(new SymbolInfo());

		SymbolInfo symbolInfo;

		public ISymbol Symbol {
			get {
				return symbolInfo.Symbol;
			}
		}

		public ImmutableArray<ISymbol> CandidateSymbols {
			get {
				return symbolInfo.CandidateSymbols;
			}
		}

		public ISymbol DeclaredSymbol {
			get;
			internal set;
		}

		public RefactoringSymbolInfo (SymbolInfo symbolInfo)
		{
			this.symbolInfo = symbolInfo;
		}
	}

	public class CurrentRefactoryOperationsHandler : CommandHandler
	{
		protected override void Run (object data)
		{
			var del = (System.Action) data;
			if (del != null)
				del ();
		}

		public static async Task<RefactoringSymbolInfo> GetSymbolInfoAsync (DocumentContext document, int offset, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (document.ParsedDocument == null)
				return RefactoringSymbolInfo.Empty;
			var unit = document.ParsedDocument.GetAst<SemanticModel> ();
			if (unit != null) {
				var root = await unit.SyntaxTree.GetRootAsync (cancellationToken).ConfigureAwait (false);
				try {
					var token = root.FindToken (offset);
					var symbol = unit.GetSymbolInfo (token.Parent);
					return new RefactoringSymbolInfo (symbol) {
						DeclaredSymbol = token.IsKind (SyntaxKind.IdentifierToken) ? unit.GetDeclaredSymbol (token.Parent) : null
					};
				} catch (Exception) {
					return RefactoringSymbolInfo.Empty;
				}
			}
			return RefactoringSymbolInfo.Empty;
		}

		static CommandInfoSet CreateFixMenu (TextEditor editor, DocumentContext ctx, CodeActionContainer container)
		{
			if (editor == null)
				throw new ArgumentNullException ("editor");
			if (ctx == null)
				throw new ArgumentNullException ("ctx");
			if (container == null)
				throw new ArgumentNullException ("container");
			var result = new CommandInfoSet ();
			result.Text = GettextCatalog.GetString ("Fix");
			foreach (var diagnostic in container.CodeDiagnosticActions) {
				var info = new CommandInfo (diagnostic.Item2.Title);
				result.CommandInfos.Add (info, new Action (new CodeActionEditorExtension.ContextActionRunner (diagnostic.Item2, editor, ctx).Run));
			}
			if (result.CommandInfos.Count == 0)
				return result;
			result.CommandInfos.AddSeparator ();
			foreach (var fix in container.CodeDiagnosticActions) {

				var inspector = fix.Item1.GetCodeDiagnosticDescriptor (null); 

				var label = GettextCatalog.GetString ("_Options for \"{0}\"", fix.Item2.Title);
				var subMenu = new CommandInfoSet ();
				subMenu.Text = label;

//				if (inspector.CanSuppressWithAttribute) {
//					var menuItem = new FixMenuEntry (GettextCatalog.GetString ("_Suppress with attribute"),
//						delegate {
//							
//							inspector.SuppressWithAttribute (Editor, DocumentContext, GetTextSpan (fix.Item2)); 
//						});
//					subMenu.Add (menuItem);
//				}

				if (inspector.CanDisableWithPragma) {
					var info = new CommandInfo (GettextCatalog.GetString ("_Suppress with #pragma"));
					subMenu.CommandInfos.Add (info, new Action (() => inspector.DisableWithPragma (editor, ctx, CodeActionEditorExtension.GetTextSpan (fix.Item2))));
				}

				if (inspector.CanDisableOnce) {
					var info = new CommandInfo (GettextCatalog.GetString ("_Disable Once"));
					subMenu.CommandInfos.Add (info, new Action (() => inspector.DisableOnce (editor, ctx, CodeActionEditorExtension.GetTextSpan (fix.Item2))));
				}

				if (inspector.CanDisableAndRestore) {
					var info = new CommandInfo (GettextCatalog.GetString ("Disable _and Restore"));
					subMenu.CommandInfos.Add (info, new Action (() => inspector.DisableAndRestore (editor, ctx, CodeActionEditorExtension.GetTextSpan (fix.Item2))));
				}

				var configInfo = new CommandInfo (GettextCatalog.GetString ("_Configure Rule"));
				subMenu.CommandInfos.Add (configInfo, new Action (() => {
					IdeApp.Workbench.ShowGlobalPreferencesDialog (null, "C#", dialog => {
						var panel = dialog.GetPanel<CodeIssuePanel> ("C#");
						if (panel == null)
							return;
						panel.Widget.SelectCodeIssue (inspector.IdString);
					});
				}));

				result.CommandInfos.Add (subMenu);
			}

			return result;
		}

		protected override void Update (CommandArrayInfo ainfo)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null)
				return;
			var semanticModel = doc.ParsedDocument.GetAst<SemanticModel> ();
			if (semanticModel == null)
				return;
			var info = GetSymbolInfoAsync (doc, doc.Editor.CaretOffset).Result;
			bool added = false;

			var ext = doc.GetContent<CodeActionEditorExtension> ();

			if (ext != null && ext.Fixes != null) {
				var fixMenu = CreateFixMenu (doc.Editor, doc, ext.Fixes);
				if (fixMenu.CommandInfos.Count > 0) {
					ainfo.Add (fixMenu, null);
					added = true;
				}
			}
			var ciset = new CommandInfoSet ();
			ciset.Text = GettextCatalog.GetString ("Refactor");

			bool canRename = MonoDevelop.Refactoring.Rename.RenameHandler.CanRename (info.Symbol ?? info.DeclaredSymbol);
			if (canRename) {
				ciset.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.EditCommands.Rename), new Action (delegate {
					new MonoDevelop.Refactoring.Rename.RenameRefactoring ().Rename (info.Symbol ?? info.DeclaredSymbol);
				}));
				added = true;
			}
			bool first = true;
			if (ext != null && ext.Fixes != null) {
				foreach (var fix in ext.Fixes.CodeRefactoringActions) {
					if (added & first)
						ciset.CommandInfos.AddSeparator ();
					var info2 = new CommandInfo (fix.Item2.Title);
					ciset.CommandInfos.Add (info2, new Action (new CodeActionEditorExtension.ContextActionRunner (fix.Item2, doc.Editor, doc).Run));
					added = true;
					first = false;
				}
			}

			if (ciset.CommandInfos.Count > 0) {
				ainfo.Add (ciset, null);
				added = true;
			}

			if (IdeApp.ProjectOperations.CanJumpToDeclaration (info.Symbol) || info.Symbol == null && IdeApp.ProjectOperations.CanJumpToDeclaration (info.CandidateSymbols.FirstOrDefault ())) {
				var type = (info.Symbol ?? info.CandidateSymbols.FirstOrDefault ()) as INamedTypeSymbol;
				if (type != null && type.Locations.Length > 1) {
					var declSet = new CommandInfoSet ();
					declSet.Text = GettextCatalog.GetString ("_Go to Declaration");
					foreach (var part in type.Locations) {
						int line = 0;
						declSet.CommandInfos.Add (string.Format (GettextCatalog.GetString ("{0}, Line {1}"), FormatFileName (part.SourceTree.FilePath), line), new Action (() => IdeApp.ProjectOperations.JumpTo (type, part, doc.Project)));
					}
					ainfo.Add (declSet);
				} else {
					ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.GotoDeclaration), new Action (() => GotoDeclarationHandler.JumpToDeclaration (doc, info)));
				}
				added = true;
			}


			if (info.DeclaredSymbol != null && GotoBaseDeclarationHandler.CanGotoBase (info.DeclaredSymbol)) {
				ainfo.Add (GotoBaseDeclarationHandler.GetDescription (info.DeclaredSymbol), new Action (() => GotoBaseDeclarationHandler.GotoBase (doc, info.DeclaredSymbol)));
				added = true;
			}

			if (canRename) {
				var sym = info.Symbol ?? info.DeclaredSymbol;
				ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindReferences), new System.Action (() => FindReferencesHandler.FindRefs (sym)));
				if (doc.HasProject && sym != null) {
					try {
						if (Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindSimilarSymbols (sym, semanticModel.Compilation).Count () > 1)
							ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindAllReferences), new System.Action (() => FindAllReferencesHandler.FindRefs (info.Symbol, semanticModel.Compilation)));
					} catch (Exception) {
						// silently ignore roslyn bug.
					}
				}
				added = true;
			}
			if (info.DeclaredSymbol != null) {
				string description;
				if (FindDerivedSymbolsHandler.CanFindDerivedSymbols (info.DeclaredSymbol, out description)) {
					ainfo.Add (description, new Action (() => FindDerivedSymbolsHandler.FindDerivedSymbols (info.DeclaredSymbol)));
					added = true;
				}

				if (FindMemberOverloadsHandler.CanFindMemberOverloads (info.DeclaredSymbol, out description)) {
					ainfo.Add (description, new Action (() => FindMemberOverloadsHandler.FindOverloads (info.DeclaredSymbol)));
					added = true;
				}

				if (FindExtensionMethodHandler.CanFindExtensionMethods (info.DeclaredSymbol, out description)) {
					ainfo.Add (description, new Action (() => FindExtensionMethodHandler.FindExtensionMethods (info.DeclaredSymbol)));
					added = true;
				}
			}
		}

		static string FormatFileName (string fileName)
		{
			if (fileName == null)
				return null;
			char[] seperators = { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };
			int idx = fileName.LastIndexOfAny (seperators);
			if (idx > 0) 
				idx = fileName.LastIndexOfAny (seperators, idx - 1);
			if (idx > 0) 
				return "..." + fileName.Substring (idx);
			return fileName;
		}
	}
}
