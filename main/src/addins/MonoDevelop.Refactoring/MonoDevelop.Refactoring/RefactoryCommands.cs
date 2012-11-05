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
using System.Text;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.CodeActions;

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
		
		public static ResolveResult GetResolveResult (MonoDevelop.Ide.Gui.Document doc)
		{
			ITextEditorResolver textEditorResolver = doc.GetContent<ITextEditorResolver> ();
			if (textEditorResolver != null)
				return textEditorResolver.GetLanguageItem (doc.Editor.Caret.Offset);
			return null;
		}
		
		public static object GetItem (MonoDevelop.Ide.Gui.Document doc, out ResolveResult resolveResult)
		{
			resolveResult = GetResolveResult (doc);
			if (resolveResult is LocalResolveResult) 
				return ((LocalResolveResult)resolveResult).Variable;
			if (resolveResult is MemberResolveResult)
				return ((MemberResolveResult)resolveResult).Member;
			if (resolveResult is MethodGroupResolveResult) {
				var mg = ((MethodGroupResolveResult)resolveResult);
				var method = mg.Methods.FirstOrDefault ();
				if (method == null && mg.GetExtensionMethods ().Any ()) 
					method = mg.GetExtensionMethods ().First ().FirstOrDefault ();
				return method;
			}
			if (resolveResult is TypeResolveResult)
				return resolveResult.Type;
			return null;
		}
		
		class JumpTo
		{
			object el;
			
			public JumpTo (object el)
			{
				this.el = el;
			}
			
			public void Run ()
			{
				if (el is IUnresolvedEntity) {
					var e = (IUnresolvedEntity)el;
					IdeApp.Workbench.OpenDocument (e.Region.FileName, e.Region.BeginLine, e.Region.BeginColumn);
					return;
				} 
				if (el is IVariable)
					IdeApp.ProjectOperations.JumpToDeclaration ((IVariable)el);
				if (el is INamedElement)
					IdeApp.ProjectOperations.JumpToDeclaration ((INamedElement)el);
			}
		}

		
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
						if (def != null && def.Kind != TypeKind.Interface && !def.Region.IsEmpty) {
							IdeApp.Workbench.OpenDocument (def.Region.FileName, def.Region.BeginLine, def.Region.BeginColumn);
							return;
						}
					}
				}
				
				var method = item as IMethod;
				if (method != null) {
					foreach (var def in method.DeclaringTypeDefinition.DirectBaseTypes) {
						if (def != null && def.Kind != TypeKind.Interface && !def.GetDefinition ().Region.IsEmpty) {
							IMethod baseMethod = null;
							foreach (var m in def.GetMethods ()) {
								if (m.Name == method.Name && ParameterListComparer.Instance.Equals (m.Parameters, method.Parameters)) {
									baseMethod = m;
									break;
								}
							}
							if (baseMethod != null)
								IdeApp.Workbench.OpenDocument (baseMethod.Region.FileName, baseMethod.Region.BeginLine, baseMethod.Region.EndLine);
							return;
						}
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

		IEnumerable<MonoDevelop.CodeActions.CodeAction> validActions;
		MonoDevelop.Ide.TypeSystem.ParsedDocument lastDocument;

		DocumentLocation lastLocation;

		protected override void Update (CommandArrayInfo ainfo)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			
			var parsedDocument = doc.ParsedDocument;
			if (parsedDocument == null || parsedDocument.IsInvalid)
				return;
			
			ResolveResult resolveResult;
			object item = GetItem (doc, out resolveResult);
			bool added = false;

			var options = new RefactoringOptions (doc) {
				ResolveResult = resolveResult,
				SelectedItem = item
			};
			
			var ciset = new CommandInfoSet ();
			ciset.Text = GettextCatalog.GetString ("Refactor");
			
			
			bool canRename;
			if (item is IVariable || item is IParameter) {
				canRename = true; 
			} else if (item is ITypeDefinition) { 
				canRename = !((ITypeDefinition)item).Region.IsEmpty;
			} else if (item is IType) { 
				canRename = ((IType)item).Kind == TypeKind.TypeParameter;
			} else if (item is IMember) {
				canRename = !((IMember)item).Region.IsEmpty;
			} else {
				canRename = false;
			}
			if (canRename) {
				ciset.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.EditCommands.Rename), new System.Action (delegate {
					new MonoDevelop.Refactoring.Rename.RenameHandler ().Start (null);
				}));
				added = true;
			}
			
			foreach (var refactoring in RefactoringService.Refactorings) {
				if (refactoring.IsValid (options)) {
					CommandInfo info = new CommandInfo (refactoring.GetMenuDescription (options));
					info.AccelKey = refactoring.AccelKey;
					ciset.CommandInfos.Add (info, new System.Action (new RefactoringOperationWrapper (refactoring, options).Operation));
				}
			}

			var loc = doc.Editor.Caret.Location;
			bool first = true;
			if (lastDocument != doc.ParsedDocument || loc != lastLocation) {
				validActions = RefactoringService.GetValidActions (doc, loc).Result;
				lastLocation = loc;
				lastDocument = doc.ParsedDocument;
			}
			foreach (var fix_ in validActions) {
				var fix = fix_;
				if (first) {
					first = false;
					if (ciset.CommandInfos.Count > 0)
						ciset.CommandInfos.AddSeparator ();
				}
				ciset.CommandInfos.Add (fix.Title, new System.Action (() => fix.Run (doc, loc)));
			}

			if (ciset.CommandInfos.Count > 0) {
				ainfo.Add (ciset, null);
				added = true;
			}
			
			if (IdeApp.ProjectOperations.CanJumpToDeclaration (item)) {
				var type = item as ICSharpCode.NRefactory.TypeSystem.IType;
				if (type != null && type.GetDefinition ().Parts.Count > 1) {
					var declSet = new CommandInfoSet ();
					declSet.Text = GettextCatalog.GetString ("_Go to declaration");
					var ct = type.GetDefinition ();
					foreach (var part in ct.Parts)
						declSet.CommandInfos.Add (string.Format (GettextCatalog.GetString ("{0}, Line {1}"), FormatFileName (part.Region.FileName), part.Region.BeginLine), new System.Action (new JumpTo (part).Run));
					ainfo.Add (declSet);
				} else {
					ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.GotoDeclaration), new System.Action (new JumpTo (item).Run));
				}
				added = true;
			}

			if (item is IEntity || item is ITypeParameter || item is IVariable) {
				ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindReferences), new System.Action (new FindRefs (item, false).Run));
				if (doc.HasProject && ReferenceFinder.HasOverloads (doc.Project.ParentSolution, item))
					ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindAllReferences), new System.Action (new FindRefs (item, true).Run));
				added = true;
			}
			
			if (item is IMethod) {
				IMethod method = item as IMethod;
				if (method.IsOverride) {
					ainfo.Add (GettextCatalog.GetString ("Go to _base"), new System.Action (new GotoBase ((IMethod)item).Run));
					added = true;
				}
			} else if (item is ITypeDefinition) {
				ITypeDefinition cls = (ITypeDefinition)item;
				foreach (var bc in cls.DirectBaseTypes) {
					if (bc != null && bc.GetDefinition () != null && bc.GetDefinition ().Kind != TypeKind.Interface/* TODO: && IdeApp.ProjectOperations.CanJumpToDeclaration (bc)*/) {
						ainfo.Add (GettextCatalog.GetString ("Go to _base"), new System.Action (new GotoBase ((ITypeDefinition)item).Run));
						break;
					}
				}
				if ((cls.Kind == TypeKind.Class && !cls.IsSealed) || cls.Kind == TypeKind.Interface) {
					ainfo.Add (cls.Kind != TypeKind.Interface ? GettextCatalog.GetString ("Find _derived classes") : GettextCatalog.GetString ("Find _implementor classes"), new System.Action (new FindDerivedClasses (cls).Run));
				}
//				if (baseConstructor != null) {
//					Refactorer refactorer2 = new Refactorer (ctx, pinfo, baseConstructor.DeclaringType, baseConstructor, null);
//					ainfo.Add (GettextCatalog.GetString ("Go to _base"), new RefactoryOperation (refactorer2.GoToBase));
//				}
			}
			
			
			

			
			if (resolveResult != null) {
//				List<string> namespaces = QuickFixHandler.GetResolveableNamespaces (options, out resolveDirect);
//			
//				if (item == null || namespaces.Count > 1) {
//					if (item == null) {
//						foreach (string ns in namespaces) {
//							// remove used namespaces for conflict resolving. 
//							if (options.Document.CompilationUnit.IsNamespaceUsedAt (ns, options.ResolveResult.ResolvedExpression.Region.Start))
//								continue;
//							CommandInfo info = resolveMenu.CommandInfos.Add ("using " + ns + ";", new RefactoryOperation (new ResolveNameOperation (ctx, doc, resolveResult, ns).AddImport));
//							info.Icon = MonoDevelop.Ide.Gui.Stock.AddNamespace;
//						}
//						// remove all unused namespaces (for resolving conflicts)
//						namespaces.RemoveAll (ns => !doc.CompilationUnit.IsNamespaceUsedAt (ns, resolveResult.ResolvedExpression.Region.Start));
//					}
//					
//					if (namespaces.Count > (item == null ? 0 : 1))
//						ainfo.Add (resolveMenu, null);
//				}
			}
			
			
//				
//				if (cls.GetSourceProject () != null && includeModifyCommands && ((cls.ClassType == ClassType.Class) || (cls.ClassType == ClassType.Struct))) {
//					ciset.CommandInfos.Add (GettextCatalog.GetString ("_Encapsulate Fields..."), new RefactoryOperation (refactorer.EncapsulateField));
//					ciset.CommandInfos.Add (GettextCatalog.GetString ("Override/Implement members..."), new RefactoryOperation (refactorer.OverrideOrImplementMembers));
//				}
//				
//				ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindReferences), new RefactoryOperation (refactorer.FindReferences));
//				
//				if (canRename && cls.ClassType == ClassType.Interface && eclass != null) {
//					// is now provided by the refactoring command infrastructure:
////					ciset.CommandInfos.Add (GettextCatalog.GetString ("Implement Interface (explicit)"), new RefactoryOperation (refactorer.ImplementExplicitInterface));
////					ciset.CommandInfos.Add (GettextCatalog.GetString ("Implement Interface (implicit)"), new RefactoryOperation (refactorer.ImplementImplicitInterface));
//				} else if (canRename && includeModifyCommands && cls.BaseType != null && cls.ClassType != ClassType.Interface && cls == eclass) {
//					// Class might have interfaces... offer to implement them
//					CommandInfoSet impset = new CommandInfoSet ();
//					CommandInfoSet expset = new CommandInfoSet ();
//					CommandInfoSet abstactset = new CommandInfoSet ();
//					bool ifaceAdded = false;
//					bool abstractAdded = false;
//					
//					foreach (IReturnType rt in cls.BaseTypes) {
//						IType iface = ctx.GetType (rt);
//						if (iface == null)
//							continue;
//						if (iface.ClassType == ClassType.Interface) {
//							Refactorer ifaceRefactorer = new Refactorer (ctx, pinfo, cls, iface, rt);
//							impset.CommandInfos.Add (ambience.GetString (rt, OutputFlags.IncludeGenerics), new RefactoryOperation (ifaceRefactorer.ImplementImplicitInterface));
//							expset.CommandInfos.Add (ambience.GetString (rt, OutputFlags.IncludeGenerics), new RefactoryOperation (ifaceRefactorer.ImplementExplicitInterface));
//							ifaceAdded = true;
//						} else if (ContainsAbstractMembers (iface)) {
//							Refactorer ifaceRefactorer = new Refactorer (ctx, pinfo, cls, iface, rt);
//							abstactset.CommandInfos.Add (ambience.GetString (rt, OutputFlags.IncludeGenerics), new RefactoryOperation (ifaceRefactorer.ImplementAbstractMembers));
//							abstractAdded = true;
//						}
//					}
//					
//					if (ifaceAdded) {
//						impset.Text = GettextCatalog.GetString ("Implement Interface (implicit)");
//						ciset.CommandInfos.Add (impset, null);
//						
//						expset.Text = GettextCatalog.GetString ("Implement Interface (explicit)");
//						ciset.CommandInfos.Add (expset, null);
//					}
//					if (abstractAdded) {
//						abstactset.Text = GettextCatalog.GetString ("Implement abstract members");
//						ciset.CommandInfos.Add (abstactset, null);
//					}
//				}
//			} 
			
			
//			IMember eitem = resolveResult != null ? (resolveResult.CallingMember ?? resolveResult.CallingType) : null;
//			
//			string itemName = null;
//			if (item is IMember)
//				itemName = ((IMember)item).Name;
//
//			if (item != null && eitem != null && (eitem.Equals (item) || (eitem.Name == itemName && !(eitem is IProperty) && !(eitem is IMethod)))) {
//				// If this occurs, then @item is either its own enclosing item, in
//				// which case, we don't want to show it twice, or it is the base-class
//				// version of @eitem, in which case we don't want to show the base-class
//				// @item, we'd rather show the item the user /actually/ requested, @eitem.
//				item = eitem;
//				eitem = null;
//			}
//
//			IType eclass = null;
//
//			if (item is IType) {
//				if (((IType)item).ClassType == ClassType.Interface)
//					eclass = FindEnclosingClass (ctx, editor.Name, line, column); else
//					eclass = (IType)item;
//				if (eitem is IMethod && ((IMethod)eitem).IsConstructor && eitem.DeclaringType.Equals (item)) {
//					item = eitem;
//					eitem = null;
//				}
//			}
//			
//			INode realItem = item;
//			if (item is InstantiatedType)
//				realItem = ((InstantiatedType)item).UninstantiatedType;
//			if (realItem is CompoundType) {
//				editor.GetLineColumnFromPosition (editor.CursorPosition, out line, out column);
//				((CompoundType)realItem).SetMainPart (doc.FileName, line, column);
//				item = realItem;
//			}
//			
//			
//			
//			var unit = doc.CompilationUnit;
//			if (unit != null && unit.Usings != null && unit.Usings.Any (u => !u.IsFromNamespace && u.Region.Contains (line, column))) {
//				CommandInfoSet organizeUsingsMenu = new CommandInfoSet ();
//				organizeUsingsMenu.Text = GettextCatalog.GetString ("_Organize Usings");
//				organizeUsingsMenu.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.RemoveUnusedImports), new RefactoryOperation (delegate {
//					new RemoveUnusedImportsHandler ().Start (options);
//				}));
//				organizeUsingsMenu.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (MonoDevelop.Refactoring.RefactoryCommands.SortImports), new RefactoryOperation (delegate {
//					new SortImportsHandler ().Start (options);
//				}));
//				organizeUsingsMenu.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (MonoDevelop.Refactoring.RefactoryCommands.RemoveSortImports), new RefactoryOperation (delegate {
//					new RemoveSortImportsHandler ().Start (options);
//				}));
//				ainfo.Add (organizeUsingsMenu, null);
//				added = true;
//			}
//			
//			IUnresolvedFile pinfo = doc.CompilationUnit;
//			if (pinfo == null)
//				return;
//			
//			
//			Refactorer refactorer = new Refactorer (ctx, pinfo, eclass, realItem, null);
//			Ambience ambience = AmbienceService.GetAmbienceForFile (pinfo.FileName);
//			bool includeModifyCommands = this.IsModifiable (item);
//			
//			
//			// case: clicked on base in "constructor" - so pointing to the base constructor using argument count
//			// not 100% correct, but it's the fastest thing to do.
//			if (resolveResult is BaseResolveResult && eitem is IMethod && ((IMethod)eitem).IsConstructor) {
//				IType type = item as IType;
//				IMethod baseConstructor = null;
//				int idx1 = resolveResult.ResolvedExpression.Expression.IndexOf ('(');
//				int idx2 = resolveResult.ResolvedExpression.Expression.IndexOf (')');
//				int paramCount = 0;
//				if (idx1 > 0 && idx2 > 0) {
//					if (idx2 - idx1 > 1)
//						paramCount++;
//					for (int i=idx1; i < idx2; i++) {
//						if (resolveResult.ResolvedExpression.Expression[i] == ',') 
//							paramCount++;
//					}
//				}
//				foreach (IMethod m in type.Methods) {
//					if (m.IsConstructor && m.Parameters.Count == paramCount)
//						baseConstructor = m;
//				}
//				Refactorer refactorer2 = new Refactorer (ctx, pinfo, baseConstructor.DeclaringType, baseConstructor, null);
//				ainfo.Add (GettextCatalog.GetString ("Go to _base"), new RefactoryOperation (refactorer2.GoToBase));
//			}
//			
//				else if (item is IField) {
//				if (includeModifyCommands) {
//					if (canRename)
//						ciset.CommandInfos.Add (GettextCatalog.GetString ("_Encapsulate Field..."), new RefactoryOperation (refactorer.EncapsulateField));
//				}
//			} else 
			
			if (added)
				ainfo.AddSeparator ();
			/*
			while (item != null) {
				CommandInfo ci;

				// case: clicked on base in "constructor" - so pointing to the base constructor using argument count
				// not 100% correct, but it's the fastest thing to do.
				if (resolveResult is BaseResolveResult && eitem is IMethod && ((IMethod)eitem).IsConstructor) {
					IType type = item as IType;
					IMethod baseConstructor = null;
					int idx1 = resolveResult.ResolvedExpression.Expression.IndexOf ('(');
					int idx2 = resolveResult.ResolvedExpression.Expression.IndexOf (')');
					int paramCount = 0;
					if (idx1 > 0 && idx2 > 0) {
						if (idx2 - idx1 > 1)
							paramCount++;
						for (int i=idx1; i < idx2; i++) {
							if (resolveResult.ResolvedExpression.Expression[i] == ',') 
								paramCount++;
						}
					}
					foreach (IMethod m in type.Methods) {
						if (m.IsConstructor && m.Parameters.Count == paramCount)
							baseConstructor = m;
					}
					if (baseConstructor != null && (ci = BuildRefactoryMenuForItem (ctx, doc.CompilationUnit, null, baseConstructor, true)) != null) {
						ainfo.Add (ci, null);
						added = true;
					}
				}
			
				// Add the selected item
				if ((ci = BuildRefactoryMenuForItem (ctx, doc.CompilationUnit, eclass, item, IsModifiable (item))) != null) {
					ainfo.Add (ci, null);
					added = true;
				}
				if (item is IParameter) {
					// Add the encompasing method for the previous item in the menu
					item = ((IParameter) item).DeclaringMember;
					if (item != null && (ci = BuildRefactoryMenuForItem (ctx, doc.CompilationUnit, null, item, true)) != null) {
						ainfo.Add (ci, null);
						added = true;
					}
				}
				
				
				if (item is IMember && !(eitem != null && eitem is IMember)) {
					// Add the encompasing class for the previous item in the menu
					item = ((IMember) item).DeclaringType;
					if (item != null && (ci = BuildRefactoryMenuForItem (ctx, doc.CompilationUnit, null, item, IsModifiable (item))) != null) {
						ainfo.Add (ci, null);
						added = true;
					}
				}
				
				item = eitem;
				eitem = null;
				eclass = null;
			}
			
			if (added)
				ainfo.AddSeparator ();*/
		}
		

		class RefactoringOperationWrapper
		{
			RefactoringOperation refactoring;
			RefactoringOptions options;
			
			public RefactoringOperationWrapper (RefactoringOperation refactoring, RefactoringOptions options)
			{
				this.refactoring = refactoring;
				this.options = options;
			}
			
			public void Operation ()
			{
				refactoring.Run (options);
			}
		}

/*		public class ResolveNameOperation
		{
			ProjectDom ctx;
			Document doc;
			string ns;
			ResolveResult resolveResult;
			
			public ResolveNameOperation (ProjectDom ctx, Document doc, ResolveResult resolveResult, string ns)
			{
				this.ctx = ctx;
				this.doc = doc;
				this.resolveResult = resolveResult;
				this.ns = ns;
			}
			
			public void AddImport ()
			{
				CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
				
				if (resolveResult is NamespaceResolveResult) {
					refactorer.AddLocalNamespaceImport (ctx, doc.FileName, ns, resolveResult.ResolvedExpression.Region.Start);
				} else {
					refactorer.AddGlobalNamespaceImport (ctx, doc.FileName, ns);
				}
			}
			
			public void ResolveName ()
			{
				int pos = doc.Editor.Document.LocationToOffset (resolveResult.ResolvedExpression.Region.Start.Line, resolveResult.ResolvedExpression.Region.Start.Column);
				if (pos < 0) {
					LoggingService.LogError ("Invalid expression position: " + resolveResult.ResolvedExpression);
					return;
				}
				doc.Editor.Insert (pos, ns + ".");
				if (doc.Editor.Caret.Offset >= pos)
					doc.Editor.Caret.Offset += (ns + ".").Length;
				doc.Editor.Document.CommitLineUpdate (resolveResult.ResolvedExpression.Region.Start.Line);
			}
		}
*/


		bool IsModifiable (object member)
		{
			IType t = member as IType;
			if (t != null) 
				return t.GetDefinition ().Region.FileName == IdeApp.Workbench.ActiveDocument.FileName;
			if (member is IMember)
				return ((IMember)member).DeclaringTypeDefinition.Region.FileName == IdeApp.Workbench.ActiveDocument.FileName;
			return false;
		}
		
//		// public class Funkadelic : IAwesomeSauce, IRockOn { ...
//		//        ----------------   -------------
//		// finds this ^ if you clicked on this ^
//		internal static IType FindEnclosingClass (ITypeResolveContext ctx, string fileName, int line, int col)
//		{
//			IType klass = null;
//			foreach (IType c in ctx.GetTypes (fileName)) {
//				if (c.BodyRegion.Contains (line, col))
//					klass = c;
//			}
//			
//			if (klass != null && klass.ClassType != ClassType.Interface)
//				return klass;
//			
//			return null;
//		}
		
	/*	string EscapeName (string name)
		{
			if (name.IndexOf ('_') == -1)
				return name;
			
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < name.Length; i++) {
				if (name[i] == '_')
					sb.Append ('_');
				sb.Append (name[i]);
			}
			
			return sb.ToString ();
		}*/
		
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
		/*
		CommandInfo BuildRefactoryMenuForItem (ITypeResolveContext ctx, IUnresolvedFile pinfo, IType eclass, INode item, bool includeModifyCommands)
		{
			INode realItem = item;
			if (item is InstantiatedType)
				realItem = ((InstantiatedType)item).UninstantiatedType;
			Document doc = IdeApp.Workbench.ActiveDocument;
			ITextBuffer editor = doc.GetContent<ITextBuffer> ();
			
			if (realItem is CompoundType) {
				int line, column;
				editor.GetLineColumnFromPosition (editor.CursorPosition, out line, out column);
				((CompoundType)realItem).SetMainPart (doc.FileName, line, column);
				item = realItem;
			}
			
			Refactorer refactorer = new Refactorer (ctx, pinfo, eclass, realItem, null);
			CommandInfoSet ciset = new CommandInfoSet ();
			Ambience ambience = AmbienceService.GetAmbienceForFile (pinfo.FileName);
			OutputFlags flags = OutputFlags.IncludeMarkup;
			if (item is IParameter) {
				flags |= OutputFlags.IncludeParameterName;
			} else {
				flags |= OutputFlags.IncludeParameters;
			}
			
			string itemName = EscapeName (ambience.GetString (item, flags));
			bool canRename = false;
			string txt;
			if (IdeApp.ProjectOperations.CanJumpToDeclaration (item)) {
				if (item is CompoundType) {
					CommandInfoSet declSet = new CommandInfoSet ();
					declSet.Text = GettextCatalog.GetString ("_Go to declaration");
					CompoundType ct = (CompoundType)item;
					foreach (IType part in ct.Parts) {
						Refactorer partRefactorer = new Refactorer (ctx, pinfo, eclass, part, null);
						declSet.CommandInfos.Add (string.Format (GettextCatalog.GetString ("{0}, Line {1}"), FormatFileName (part.GetDefinition ().Region.FileName), part.Location.Line), new RefactoryOperation (partRefactorer.GoToDeclaration));
					}
					ciset.CommandInfos.Add (declSet);
				} else {
					ciset.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.GotoDeclaration), new RefactoryOperation (refactorer.GoToDeclaration));
				}
			}
			

			if ((item is IMember || item is LocalVariable || item is IParameter) && !(item is IType))
				ciset.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindReferences), new RefactoryOperation (refactorer.FindReferences));
			
			// We can rename local variables (always), method params (always), 
			// or class/members (if they belong to a project)
			if ((item is LocalVariable) || (item is IParameter)) {
				canRename = true; 
			} else if (item is IType) { 
				canRename = ((IType)item).GetSourceProject () != null; 
			} else if (item is IMember) {
				IType cls = ((IMember)item).DeclaringType;
				canRename = cls != null && cls.GetSourceProject () != null;
			}
			
			RefactoringOptions options = new RefactoringOptions () {
				Document = doc,
				Dom = ctx,
				ResolveResult = null,
				SelectedItem = item is InstantiatedType ? ((InstantiatedType)item).UninstantiatedType : item
			};
			foreach (var refactoring in RefactoringService.Refactorings) {
				if (refactoring.IsValid (options)) {
					CommandInfo info = new CommandInfo (refactoring.GetMenuDescription (options));
					info.AccelKey = refactoring.AccelKey;
					ciset.CommandInfos.Add (info, new RefactoryOperation (new RefactoringOperationWrapper (refactoring, options).Operation));
				}
			}
			
//			if (canRename && !(item is IType)) {
//				// Defer adding this item for Classes until later
//				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Rename"), new RefactoryOperation (refactorer.Rename));
//			}
			if (item is IType) {
				IType cls = (IType) item;
				
				if (cls.ClassType == ClassType.Enum)
					txt = GettextCatalog.GetString ("Enum <b>{0}</b>", itemName);
				else if (cls.ClassType == ClassType.Struct)
					txt = GettextCatalog.GetString ("Struct <b>{0}</b>", itemName);
				else if (cls.ClassType == ClassType.Interface)
					txt = GettextCatalog.GetString ("Interface <b>{0}</b>", itemName);
				else if (cls.ClassType == ClassType.Delegate)
					txt = GettextCatalog.GetString ("Delegate <b>{0}</b>", itemName);
				else
					txt = GettextCatalog.GetString ("Class <b>{0}</b>", itemName);
				
				if (cls.BaseType != null && cls.ClassType == ClassType.Class) {
					foreach (IReturnType rt in cls.BaseTypes) {
						IType bc = ctx.GetType (rt);
						if (bc != null && bc.ClassType != ClassType.Interface) {
							ciset.CommandInfos.Add (GettextCatalog.GetString ("Go to _base"), new RefactoryOperation (refactorer.GoToBase));
							break;
						}
					}
				}
				
				if ((cls.ClassType == ClassType.Class && !cls.IsSealed) || cls.ClassType == ClassType.Interface) {
					ciset.CommandInfos.Add (cls.ClassType != ClassType.Interface ? GettextCatalog.GetString ("Find _derived classes") : GettextCatalog.GetString ("Find _implementor classes"), new RefactoryOperation (refactorer.FindDerivedClasses));
				}

				if (cls.GetSourceProject () != null && includeModifyCommands && ((cls.ClassType == ClassType.Class) || (cls.ClassType == ClassType.Struct))) {
					ciset.CommandInfos.Add (GettextCatalog.GetString ("_Encapsulate Fields..."), new RefactoryOperation (refactorer.EncapsulateField));
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Override/Implement members..."), new RefactoryOperation (refactorer.OverrideOrImplementMembers));
				}
				
				ciset.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindReferences), new RefactoryOperation (refactorer.FindReferences));
				
//				if (canRename)
//					ciset.CommandInfos.Add (GettextCatalog.GetString ("_Rename"), new RefactoryOperation (refactorer.Rename));
				
				if (canRename && cls.ClassType == ClassType.Interface && eclass != null) {
					// An interface is selected, so just need to provide these 2 submenu items
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Implement Interface (implicit)"), new RefactoryOperation (refactorer.ImplementImplicitInterface));
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Implement Interface (explicit)"), new RefactoryOperation (refactorer.ImplementExplicitInterface));
				} else if (canRename && includeModifyCommands && cls.BaseType != null && cls.ClassType != ClassType.Interface && cls == eclass) {
					// Class might have interfaces... offer to implement them
					CommandInfoSet impset = new CommandInfoSet ();
					CommandInfoSet expset = new CommandInfoSet ();
					CommandInfoSet abstactset = new CommandInfoSet ();
					bool ifaceAdded = false;
					bool abstractAdded = false;
					
					foreach (IReturnType rt in cls.BaseTypes) {
						IType iface = ctx.GetType (rt);
						if (iface == null)
							continue;
						if (iface.ClassType == ClassType.Interface) {
							Refactorer ifaceRefactorer = new Refactorer (ctx, pinfo, cls, iface, rt);
							impset.CommandInfos.Add (ambience.GetString (rt, OutputFlags.IncludeGenerics), new RefactoryOperation (ifaceRefactorer.ImplementImplicitInterface));
							expset.CommandInfos.Add (ambience.GetString (rt, OutputFlags.IncludeGenerics), new RefactoryOperation (ifaceRefactorer.ImplementExplicitInterface));
							ifaceAdded = true;
						} else if (ContainsAbstractMembers (iface)) {
							Refactorer ifaceRefactorer = new Refactorer (ctx, pinfo, cls, iface, rt);
							abstactset.CommandInfos.Add (ambience.GetString (rt, OutputFlags.IncludeGenerics), new RefactoryOperation (ifaceRefactorer.ImplementAbstractMembers));
							abstractAdded = true;
						}
					}
					
					if (ifaceAdded) {
						impset.Text = GettextCatalog.GetString ("Implement Interface (implicit)");
						ciset.CommandInfos.Add (impset, null);
						
						expset.Text = GettextCatalog.GetString ("Implement Interface (explicit)");
						ciset.CommandInfos.Add (expset, null);
					}
					if (abstractAdded) {
						abstactset.Text = GettextCatalog.GetString ("Implement abstract members");
						ciset.CommandInfos.Add (abstactset, null);
					}
				}
			} else if (item is IField) {
				txt = GettextCatalog.GetString ("Field <b>{0}</b>", itemName);
				if (includeModifyCommands) {
					if (canRename)
						ciset.CommandInfos.Add (GettextCatalog.GetString ("_Encapsulate Field..."), new RefactoryOperation (refactorer.EncapsulateField));
					AddRefactoryMenuForClass (ctx, pinfo, ciset, ((IField) item).ReturnType.FullName);
				}
			} else if (item is IProperty) {
				if (((IProperty)item).IsIndexer) {				
					txt = GettextCatalog.GetString ("Indexer <b>{0}</b>", itemName);		
				} else {
					txt = GettextCatalog.GetString ("Property <b>{0}</b>", itemName);
				}
				AddRefactoryMenuForClass (ctx, pinfo, ciset, ((IProperty) item).ReturnType.FullName);
			} else if (item is IEvent) {
				txt = GettextCatalog.GetString ("Event <b>{0}</b>", itemName);
			} else if (item is IMethod) {
				IMethod method = item as IMethod;
				
				if (method.IsConstructor) {
					txt = GettextCatalog.GetString ("Constructor <b>{0}</b>", EscapeName (method.DeclaringType.Name));
				} else {
					txt = GettextCatalog.GetString ("Method <b>{0}</b>", itemName);
					if (method.IsOverride) 
						ciset.CommandInfos.Add (GettextCatalog.GetString ("Go to _base"), new RefactoryOperation (refactorer.GoToBase));
				}
			} else if (item is IParameter) {
				txt = GettextCatalog.GetString ("Parameter <b>{0}</b>", itemName);
				AddRefactoryMenuForClass (ctx, pinfo, ciset, ((IParameter) item).ReturnType.FullName);
			} else if (item is LocalVariable) {
				LocalVariable var = (LocalVariable) item;
				AddRefactoryMenuForClass (ctx, pinfo, ciset, var.ReturnType.FullName);
				txt = GettextCatalog.GetString ("Variable <b>{0}</b>", itemName);
			} else {
				return null;
			}
			
			ciset.Text = txt;
			ciset.UseMarkup = true;
			return ciset;
		}
		*/
		
		public static bool ContainsAbstractMembers (IType cls)
		{
			if (cls == null)
				return false;
			return cls.GetMembers ().Any (m => m.IsAbstract);
		}
		
	/*	void AddRefactoryMenuForClass (ITypeResolveContext ctx, IUnresolvedFile pinfo, CommandInfoSet ciset, string className)
		{
			IType cls = ctx.GetType (className, null, true, true);
			if (cls != null) {
				CommandInfo ci = BuildRefactoryMenuForItem (ctx, pinfo, null, cls, false);
				if (ci != null)
					ciset.CommandInfos.Add (ci, null);
			}
		}*/
	}
	
	/*
	public class Refactorer
	{
		ISearchProgressMonitor monitor;
		ICompilationUnit pinfo;
		ProjectDom ctx;
		INode item;
		IType klass;
		IReturnType hintReturnType;
		
		public Refactorer (ProjectDom ctx, ICompilationUnit pinfo, IType klass, INode item, IReturnType hintReturnType)
		{
			this.pinfo = pinfo;
			this.klass = klass;
			this.item = item;
			this.ctx = ctx;
			this.hintReturnType = hintReturnType;
		}
		
		public void GoToDeclaration ()
		{
			IdeApp.ProjectOperations.JumpToDeclaration (item, true);
		}
		
		public void FindReferences ()
		{
			monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			ThreadPool.QueueUserWorkItem (FindReferencesThread);
		}
		
		void FindReferencesThread (object state)
		{
			try {
				foreach (MemberReference mref in ReferenceFinder.FindReferences (IdeApp.ProjectOperations.CurrentSelectedSolution, item, monitor)) {
					monitor.ReportResult (new MonoDevelop.Ide.FindInFiles.SearchResult (new FileProvider (mref.FileName), mref.Position, mref.Name.Length));
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
		}
		
		public void GoToBase ()
		{
			IType cls = item as IType;
			if (cls != null && cls.BaseTypes != null) {
				foreach (IReturnType bc in cls.BaseTypes) {
					IType bcls = ctx.GetType (bc);
					if (bcls != null && bcls.ClassType != ClassType.Interface && !bcls.Location.IsEmpty) {
						IdeApp.Workbench.OpenDocument (bcls.CompilationUnit.FileName, bcls.Location.Line, bcls.Location.Column);
						return;
					}
				}
				return;
			}
			IMethod method = item as IMethod;
			if (method != null) {
				foreach (IReturnType bc in method.DeclaringType.BaseTypes) {
					IType bcls = ctx.GetType (bc);
					if (bcls != null && bcls.ClassType != ClassType.Interface && !bcls.Location.IsEmpty) {
						IMethod baseMethod = null;
						foreach (IMethod m in bcls.Methods) {
							if (m.Name == method.Name && m.Parameters.Count == m.Parameters.Count) {
								baseMethod = m;
								break;
							}
						}
						if (baseMethod != null)
							IdeApp.Workbench.OpenDocument (bcls.CompilationUnit.FileName, baseMethod.Location.Line, baseMethod.Location.Column);
						return;
					}
				}
				return;
			}
		}
		
		public void FindDerivedClasses ()
		{
			ThreadPool.QueueUserWorkItem (FindDerivedThread);
		}
		
		void FindDerivedThread (object state)
		{
			monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			using (monitor) {
				IType cls = (IType) item;
				if (cls == null) return;
				
				CodeRefactorer cr = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
				foreach (IType sub in cr.FindDerivedClasses (cls)) {
					if (!sub.Location.IsEmpty) {
						IEditableTextFile textFile = cr.TextFileProvider.GetEditableTextFile (sub.CompilationUnit.FileName);
						if (textFile == null) 
							textFile = new TextFile (sub.CompilationUnit.FileName);
						int position = textFile.GetPositionFromLineColumn (sub.Location.Line, sub.Location.Column);
						monitor.ReportResult (new MonoDevelop.Ide.FindInFiles.SearchResult (new FileProvider (sub.CompilationUnit.FileName, sub.SourceProject as Project), position, 0));
					}
				}
			}
		}
		
		void ImplementInterface (bool explicitly)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var editor = doc.Editor.Parent;
			IType interfaceType = item as IType;
			IType declaringType = klass;
			
			var mode = new Mono.TextEditor.InsertionCursorEditMode (editor, CodeGenerationService.GetInsertionPoints (doc, declaringType));
			var helpWindow = new Mono.TextEditor.PopupWindow.ModeHelpWindow ();
			helpWindow.TransientFor = IdeApp.Workbench.RootWindow;
			helpWindow.TitleText = GettextCatalog.GetString ("<b>Implement Interface -- Targeting</b>");
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Key</b>"), GettextCatalog.GetString ("<b>Behavior</b>")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Up</b>"), GettextCatalog.GetString ("Move to <b>previous</b> target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Down</b>"), GettextCatalog.GetString ("Move to <b>next</b> target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Enter</b>"), GettextCatalog.GetString ("<b>Declare interface implementation</b> at target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Esc</b>"), GettextCatalog.GetString ("<b>Cancel</b> this refactoring.")));
			mode.HelpWindow = helpWindow;
			mode.CurIndex = mode.InsertionPoints.Count - 1;
			mode.StartMode ();
			mode.Exited += delegate(object s, Mono.TextEditor.InsertionCursorEventArgs args) {
				if (args.Success) {
					var generator = doc.CreateCodeGenerator ();
					args.InsertionPoint.Insert (doc.Editor, generator.CreateInterfaceImplementation (declaringType, interfaceType, explicitly));
				}
			};
		}
		
		public void ImplementImplicitInterface ()
		{
			ImplementInterface (false);
		}
		
		public void ImplementExplicitInterface ()
		{
			ImplementInterface (true);
		}
		
		public void ImplementAbstractMembers ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			IType interfaceType = item as IType;
			MonoDevelop.Refactoring.ImplementInterface.ImplementAbstractMembers.Implement (doc, interfaceType);
		}
		
		public void EncapsulateField ()
		{
			EncapsulateFieldDialog dialog;
			if (item is IField) {
				dialog = new EncapsulateFieldDialog (IdeApp.Workbench.ActiveDocument, ctx, (IField) item);
			} else {
				dialog = new EncapsulateFieldDialog (IdeApp.Workbench.ActiveDocument, ctx, (IType) item);
			}
			MessageService.ShowCustomDialog (dialog);
		}
		
		public void OverrideOrImplementMembers ()
		{
			MessageService.ShowCustomDialog (new OverridesImplementsDialog (IdeApp.Workbench.ActiveDocument, (IType)item));
		}
		
		public void Rename ()
		{
		//	RenameItemDialog dialog = new RenameItemDialog (ctx, item);
		//	dialog.Show ();
		}
	}
*/
}
