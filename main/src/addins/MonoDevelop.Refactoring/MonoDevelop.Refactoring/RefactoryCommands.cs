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
using Mono.TextEditor;
using MonoDevelop.CodeActions;
using MonoDevelop.SourceEditor.QuickTasks;
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

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
	
	public class CurrentRefactoryOperationsHandler : CommandHandler
	{
		protected override void Run (object data)
		{
			var del = (System.Action) data;
			if (del != null)
				del ();
		}
		
//		public static ResolveResult GetResolveResult (MonoDevelop.Ide.Gui.Document doc)
//		{
//			ITextEditorResolver textEditorResolver = doc.GetContent<ITextEditorResolver> ();
//			if (textEditorResolver != null)
//				return textEditorResolver.GetLanguageItem (doc.Editor.Caret.Offset);
//			return null;
//		}
//
		public static object GetItem (MonoDevelop.Ide.Gui.Document doc, out ICSharpCode.NRefactory.Semantics.ResolveResult resolveResult)
		{
			resolveResult = null;
			return null;
		}
		
		public static async Task<SymbolInfo> GetSymolInfoAsync (MonoDevelop.Ide.Gui.Document doc, CancellationToken cancellationToken = default(CancellationToken))
		{
			var offset = doc.Editor.Caret.Offset;
			var unit = await doc.AnalysisDocument.GetSemanticModelAsync (cancellationToken);
			if (unit != null) {
				var root = await unit.SyntaxTree.GetRootAsync (cancellationToken);
				var token = root.FindToken (offset);
				return unit.GetSymbolInfo (token.Parent); 
			}
			return new SymbolInfo ();
		}

/*
		class GotoBase 
		{
			IEntity item;
			
			public GotoBase (IEntity item)
			{
				this.item = item;
			}
			
			public void Run ()
			{
				var cls = item as ITypeDefinition;
				if (cls != null && cls.DirectBaseTypes != null) {
					foreach (var bt in cls.DirectBaseTypes) {
						var def = bt.GetDefinition ();
						if (def != null && def.Kind != TypeKind.Interface) {
							IdeApp.ProjectOperations.JumpToDeclaration (def); 
							return;
						}
					}
				}
				
				var method = item as IMember;
				if (method != null) {
					var baseMethod = InheritanceHelper.GetBaseMember (method); 
					if (baseMethod != null) {
						IdeApp.ProjectOperations.JumpToDeclaration (baseMethod); 
					}
					return;
				}
			}
		}
		
		class FindRefs 
		{
			object obj;
			bool allOverloads;
			public FindRefs (object obj, bool all)
			{
				this.obj = obj;
				this.allOverloads = all;
			}
			
			public void Run ()
			{
				if (allOverloads) {
					FindAllReferencesHandler.FindRefs (obj);
				} else {
					FindReferencesHandler.FindRefs (obj);
				}
			}
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


		DocumentLocation lastLocation;

		static bool HasOverloads (MonoDevelop.Projects.Solution solution, object item)
		{
			var member = item as IMember;
			if (member != null && member.ImplementedInterfaceMembers.Any ())
				return true;
			var method = item as IMethod;
			if (method == null)
				return false;
			return method.DeclaringType.GetMethods (m => m.Name == method.Name).Count () > 1;
		}
*/

		

		bool CanRename (ISymbol symbol)
		{
			if (symbol == null)
				return false;
			switch (symbol.Kind) {
			case SymbolKind.Local:
			case SymbolKind.Parameter:
			case SymbolKind.NamedType:
			case SymbolKind.Namespace:
			case SymbolKind.Method:
			case SymbolKind.Field:
			case SymbolKind.Property:
			case SymbolKind.Event:
			case SymbolKind.Label:
			case SymbolKind.TypeParameter:
			case SymbolKind.RangeVariable:
				return true;
			}
			return false;
		}
		
		protected override void Update (CommandArrayInfo ainfo)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			var info = GetSymolInfoAsync (doc).Result;
			
			bool added = false;

//			var options = new RefactoringOptions (doc) {
//				ResolveResult = resolveResult,
//				SelectedItem = item
//			};
			
			var ciset = new CommandInfoSet ();
			ciset.Text = GettextCatalog.GetString ("Refactor");

			bool canRename = CanRename(info.Symbol);
			if (canRename) {
				ciset.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.EditCommands.Rename), new Action (delegate {
					new MonoDevelop.Refactoring.Rename.RenameHandler ().Start (null);
				}));
				added = true;
			}
			
//			foreach (var refactoring in RefactoringService.Refactorings) {
//				if (refactoring.IsValid (options)) {
//					CommandInfo info = new CommandInfo (refactoring.GetMenuDescription (options));
//					info.AccelKey = refactoring.AccelKey;
//					ciset.CommandInfos.Add (info, new Action (new RefactoringOperationWrapper (refactoring, options).Operation));
//				}
//			}
//			var refactoringInfo = doc.Annotation<RefactoringDocumentInfo> ();
//			if (refactoringInfo == null) {
//				refactoringInfo = new RefactoringDocumentInfo ();
//				doc.AddAnnotation (refactoringInfo);
//			}
//			var loc = doc.Editor.Caret.Location;
//			bool first = true;
//			if (refactoringInfo.lastDocument != doc.ParsedDocument || loc != lastLocation) {
//
//				if (QuickTaskStrip.Enab///
////					ciset.CommandInfos.Add (fix.Title, new Action (() => RefactoringService.ApplyFix (fix, context)));
////				}
//			}
//
//			if (ciset.CommandInfos.Count > 0) {
//				ainfo.Add (ciset, null);
//				added = true;
//			}
//			
//			if (canRename) {leFancyFeatures) {
//					var ext = doc.GetContent <CodeActionEditorExtension> ();
//					//refactoringInfo.validActions = ext != null ? ext.GetCurrentFixes () : null;
//				} else {
//					refactoringInfo.validActions = RefactoringService.GetValidActions (doc, loc).Result;
//				}
//
//				lastLocation = loc;
//				refactoringInfo.lastDocument = doc.ParsedDocument;
//			}
//			if (refactoringInfo.validActions != null && refactoringInfo.lastDocument != null && refactoringInfo.lastDocument.CreateRefactoringContext != null) {
//				var context = refactoringInfo.lastDocument.CreateRefactoringContext (doc, CancellationToken.None);
//
////				foreach (var fix_ in refactoringInfo.validActions.OrderByDescending (i => Tuple.Create (CodeActionEditorExtension.IsAnalysisOrErrorFix(i), (int)i.Severity, CodeActionEditorExtension.GetUsage (i.IdString)))) {
////					if (CodeActionEditorExtension.IsAnalysisOrErrorFix (fix_))
////						continue;
////					var fix = fix_;
////					if (first) {
////						first = false;
////						if (ciset.CommandInfos.Count > 0)
////							ciset.CommandInfos.AddSeparator ();
////					}
////
////					ciset.CommandInfos.Add (fix.Title, new Action (() => RefactoringService.ApplyFix (fix, context)));
////				}
//			}
//
//			if (ciset.CommandInfos.Count > 0) {
//				ainfo.Add (ciset, null);
//				added = true;
//			}
//			
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
//
//			if (item is IMember) {
//				var member = (IMember)item;
//				if (member.IsOverride || member.ImplementedInterfaceMembers.Any ()) {
//					ainfo.Add (GettextCatalog.GetString ("Go to _Base Symbol"), new System.Action (new GotoBase (member).Run));
//					added = true;
//				}
//			}
//
//			if (!(item is IMethod && ((IMethod)item).SymbolKind == SymbolKind.Operator) && (item is IEntity || item is ITypeParameter || item is IVariable || item is INamespace)) {
//
//				ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindReferences), new System.Action (new FindRefs (item, false).Run));
//				if (doc.HasProject && HasOverloads (doc.Project.ParentSolution, item))
//					ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindAllReferences), new System.Action (new FindRefs (item, true).Run));
//				added = true;
//			}
//
//			if (item is IMember) {
//				var member = (IMember)item;
//				if (member.IsVirtual || member.IsAbstract || member.DeclaringType.Kind == TypeKind.Interface) {
//					var handler = new FindDerivedSymbolsHandler (doc, member);
//					if (handler.IsValid) {
//						ainfo.Add (GettextCatalog.GetString ("Find Derived Symbols"), new System.Action (handler.Run));
//						added = true;
//					}
//				}
//			}
//			if (item is IMember) {
//				var member = (IMember)item;
//				if (member.SymbolKind == SymbolKind.Method || member.SymbolKind == SymbolKind.Indexer) {
//					var findMemberOverloadsHandler = new FindMemberOverloadsHandler (doc, member);
//					if (findMemberOverloadsHandler.IsValid) {
//						ainfo.Add (GettextCatalog.GetString ("Find Member Overloads"), new System.Action (findMemberOverloadsHandler.Run));
//						added = true;
//					}
//				}
//			}
//
//			if (item is ITypeDefinition) {
//				ITypeDefinition cls = (ITypeDefinition)item;
//				foreach (var bc in cls.DirectBaseTypes) {
//					if (bc != null && bc.GetDefinition () != null && bc.GetDefinition ().Kind != TypeKind.Interface/* TODO: && IdeApp.ProjectOperations.CanJumpToDeclaration (bc)*/) {
//						ainfo.Add (GettextCatalog.GetString ("Go to _Base"), new System.Action (new GotoBase ((ITypeDefinition)item).Run));
//						break;
//					}
//				}
//				if ((cls.Kind == TypeKind.Class && !cls.IsSealed) || cls.Kind == TypeKind.Interface) {
//					ainfo.Add (cls.Kind != TypeKind.Interface ? GettextCatalog.GetString ("Find _derived classes") : GettextCatalog.GetString ("Find _implementor classes"), new System.Action (new FindDerivedClasses (cls).Run));
//				}
//				ainfo.Add (GettextCatalog.GetString ("Find Extension Methods"), new System.Action (new FindExtensionMethodHandler (doc, cls).Run));
//				added = true;
//
//			}

			if (added)
				ainfo.AddSeparator ();
		}

//		
//
//		class RefactoringOperationWrapper
//		{
//			RefactoringOperation refactoring;
//			RefactoringOptions options;
//			
//			public RefactoringOperationWrapper (RefactoringOperation refactoring, RefactoringOptions options)
//			{
//				this.refactoring = refactoring;
//				this.options = options;
//			}
//			
//			public void Operation ()
//			{
//				refactoring.Run (options);
//			}
//		}
//
//		bool IsModifiable (object member)
//		{
//			IType t = member as IType;
//			if (t != null) 
//				return t.GetDefinition ().Region.FileName == IdeApp.Workbench.ActiveDocument.FileName;
//			if (member is IMember)
//				return ((IMember)member).DeclaringTypeDefinition.Region.FileName == IdeApp.Workbench.ActiveDocument.FileName;
//			return false;
//		}
//
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

//		public static bool ContainsAbstractMembers (ITypeSymbol cls)
//		{
//			if (cls == null)
//				return false;
//			return cls.GetMembers ().Any (m => m.IsAbstract);
//		}
	}
}
