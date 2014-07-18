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
using MonoDevelop.CodeActions;
using MonoDevelop.SourceEditor.QuickTasks;
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Collections.Immutable;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.CodeActions;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;
using System.Threading;
using MonoDevelop.Core.Text;
using MonoDevelop.AnalysisCore;
using MonoDevelop.Ide.Editor;

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
		
		public static object GetItem (MonoDevelop.Ide.Gui.Document doc, out ICSharpCode.NRefactory.Semantics.ResolveResult resolveResult)
		{
			resolveResult = null;
			return null;
		}
		
		public static async Task<RefactoringSymbolInfo> GetSymbolInfoAsync (Microsoft.CodeAnalysis.Document document, int offset, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			var unit = await document.GetSemanticModelAsync (cancellationToken);
			if (unit != null) {
				var root = await unit.SyntaxTree.GetRootAsync (cancellationToken);
				var token = root.FindToken (offset);
				
				return new RefactoringSymbolInfo (unit.GetSymbolInfo (token.Parent)) {
					DeclaredSymbol = unit.GetDeclaredSymbol (token.Parent)
				};
			}
			return RefactoringSymbolInfo.Empty;
		}
		
		class FindDerivedClasses
		{
			ITypeDefinition type;
			
			public FindDerivedClasses (ITypeDefinition type)
			{
				this.type = type;
			}
			
			public void Run ()
			{
				FindDerivedClassesHandler.FindDerivedClasses (type);
			}
		}

		class RefactoringDocumentInfo
		{
			public IEnumerable<CodeAction> validActions;
			public MonoDevelop.Ide.TypeSystem.ParsedDocument lastDocument;

			public override string ToString ()
			{
				return string.Format ("[RefactoringDocumentInfo: #validActions={0}, lastDocument={1}]", validActions != null ? validActions.Count ().ToString () : "null", lastDocument);
			}
		}


		MonoDevelop.Ide.Editor.DocumentLocation lastLocation;

		static bool HasOverloads (Solution solution, object item)
		{
			var member = item as IMember;
			if (member != null && member.ImplementedInterfaceMembers.Any ())
				return true;
			var method = item as IMethod;
			if (method == null)
				return false;
			return method.DeclaringType.GetMethods (m => m.Name == method.Name).Count () > 1;
		}


		protected override void Update (CommandArrayInfo ainfo)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return;
			var info = GetSymbolInfoAsync (analysisDocument, doc.Editor.Caret.Offset).Result;
			
			bool added = false;

			var ciset = new CommandInfoSet ();
			ciset.Text = GettextCatalog.GetString ("Refactor");

			bool canRename = MonoDevelop.Refactoring.Rename.RenameHandler.CanRename (info.Symbol ?? info.DeclaredSymbol);
			if (canRename) {
				ciset.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.EditCommands.Rename), new Action (delegate {
					new MonoDevelop.Refactoring.Rename.RenameRefactoring ().Rename (info.Symbol ?? info.DeclaredSymbol);
				}));
				added = true;
			}
			
			foreach (var refactoring in RefactoringService.Refactorings) {
				if (refactoring.IsValid (options)) {
					CommandInfo info = new CommandInfo (refactoring.GetMenuDescription (options));
					info.AccelKey = refactoring.AccelKey;
					ciset.CommandInfos.Add (info, new Action (new RefactoringOperationWrapper (refactoring, options).Operation));
				}
			}
			var refactoringInfo = doc.Annotation<RefactoringDocumentInfo> ();
			if (refactoringInfo == null) {
				refactoringInfo = new RefactoringDocumentInfo ();
				doc.AddAnnotation (refactoringInfo);
			}
			var loc = doc.Editor.CaretLocation;
			bool first = true;
			if (refactoringInfo.lastDocument != doc.ParsedDocument || loc != lastLocation) {

				if (AnalysisOptions.EnableFancyFeatures) {
					var ext = doc.GetContent <CodeActionEditorExtension> ();
					refactoringInfo.validActions = ext != null ? ext.GetCurrentFixes () : null;
				} else {
					refactoringInfo.validActions = RefactoringService.GetValidActions (doc, loc).Result;
				}

				lastLocation = loc;
				refactoringInfo.lastDocument = doc.ParsedDocument;
			}
			if (refactoringInfo.validActions != null && refactoringInfo.lastDocument != null && refactoringInfo.lastDocument.CreateRefactoringContext != null) {
				var context = refactoringInfo.lastDocument.CreateRefactoringContext (doc.Editor, doc.Editor.CaretLocation, doc, CancellationToken.None);

				foreach (var fix_ in refactoringInfo.validActions.OrderByDescending (i => Tuple.Create (CodeActionEditorExtension.IsAnalysisOrErrorFix(i), (int)i.Severity, CodeActionEditorExtension.GetUsage (i.IdString)))) {
					if (CodeActionEditorExtension.IsAnalysisOrErrorFix (fix_))
						continue;
					var fix = fix_;
					if (first) {
						first = false;
						if (ciset.CommandInfos.Count > 0)
							ciset.CommandInfos.AddSeparator ();
					}

					ciset.CommandInfos.Add (fix.Title, new Action (() => RefactoringService.ApplyFix (fix, context)));
				}
			}

			if (ciset.CommandInfos.Count > 0) {
				ainfo.Add (ciset, null);
				added = true;
			}
			
			if (IdeApp.ProjectOperations.CanJumpToDeclaration (item)) {
				var type = item as IType;
				if (type != null && type.GetDefinition ().Parts.Count > 1) {
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
				if (doc.HasProject) {
					if (Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindSimilarSymbols (sym, doc.GetCompilationAsync ().Result).Count () > 1)
						ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindAllReferences), new System.Action (() => FindAllReferencesHandler.FindRefs (info.Symbol, doc.GetCompilationAsync ())));
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
