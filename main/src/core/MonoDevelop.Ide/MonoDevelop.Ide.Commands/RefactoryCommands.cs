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
using System.CodeDom;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Commands
{
	public enum RefactoryCommands
	{
		CurrentRefactoryOperations
	}
	
	public class CurrentRefactoryOperationsHandler: CommandHandler
	{
		protected override void Run (object data)
		{
			RefactoryOperation del = (RefactoryOperation) data;
			if (del != null)
				del ();
		}
		
		protected override void Update (CommandArrayInfo ainfo)
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc != null && doc.FileName != null && IdeApp.ProjectOperations.CurrentSelectedSolution != null) {
				ITextBuffer editor = IdeApp.Workbench.ActiveDocument.GetContent <ITextBuffer>();
				if (editor != null) {
					bool added = false;
					int line, column;
					
					editor.GetLineColumnFromPosition (editor.CursorPosition, out line, out column);
					ICompilationUnit pinfo;
					ProjectDom ctx;
					if (doc.Project != null) {
						ctx = ProjectDomService.GetProjectDom (doc.Project);
						pinfo = doc.CompilationUnit;
					} else {
						ctx = ProjectDom.Empty;
						pinfo = doc.CompilationUnit;
					}
					if (ctx == null)
						return;
					// Look for an identifier at the cursor position
					
					IParser parser = ProjectDomService.GetParserByFileName (editor.Name);
					if (parser == null)
						return;
					ExpressionResult id = new ExpressionResult (editor.SelectedText);
					if (id.Expression.Length == 0) {
						IExpressionFinder finder = parser.CreateExpressionFinder (ctx);
						if (finder == null)
							return;
						id = finder.FindFullExpression (editor.Text, editor.CursorPosition);
						if (id == null) return;
					}
					IResolver resolver = parser.CreateResolver (ctx, doc, editor.Name);
					ResolveResult resolveResult = resolver.Resolve (id, new DomLocation (line, column));
					
					IDomVisitable item = null;
					IMember eitem = resolveResult != null ? (resolveResult.CallingMember ?? resolveResult.CallingType) : null;
					if (resolveResult is ParameterResolveResult) {
						item = ((ParameterResolveResult)resolveResult).Parameter;
					} else if (resolveResult is LocalVariableResolveResult) {
						item = ((LocalVariableResolveResult)resolveResult).LocalVariable;
						//s.Append (ambience.GetString (((LocalVariableResolveResult)result).ResolvedType, WindowConversionFlags));
					} else if (resolveResult is MemberResolveResult) {
						item = ((MemberResolveResult)resolveResult).ResolvedMember;
						if (item == null && ((MemberResolveResult)resolveResult).ResolvedType != null) {
							item = ctx.GetType (((MemberResolveResult)resolveResult).ResolvedType);
						}
					} else if (resolveResult is MethodResolveResult) {
						item = ((MethodResolveResult)resolveResult).MostLikelyMethod;
						if (item == null && ((MethodResolveResult)resolveResult).ResolvedType != null) {
							item = ctx.GetType (((MethodResolveResult)resolveResult).ResolvedType);
						}
					} else if (resolveResult is BaseResolveResult) {
						item = ctx.GetType (((BaseResolveResult)resolveResult).ResolvedType);
					} else if (resolveResult is ThisResolveResult) {
						item = ctx.GetType (((ThisResolveResult)resolveResult).ResolvedType);
					}
					string itemName = null;
					if (item is IMember)
						itemName = ((IMember)item).Name;

 					if (item != null && eitem != null && 
					    (eitem == item || (eitem.Name == itemName && !(eitem is IProperty) && !(eitem is IMethod)))) {
						// If this occurs, then @item is either its own enclosing item, in
						// which case, we don't want to show it twice, or it is the base-class
						// version of @eitem, in which case we don't want to show the base-class
						// @item, we'd rather show the item the user /actually/ requested, @eitem.
						item = eitem;
						eitem = null;
					}
					
					IType eclass = null;
					
					if (item is IType) {
						if (((IType) item).ClassType == ClassType.Interface)
							eclass = FindEnclosingClass (ctx, editor.Name, line, column);
						else
							eclass = (IType) item;
					}
					
					while (item != null) {
						CommandInfo ci;
						
						// Add the selected item
						if ((ci = BuildRefactoryMenuForItem (ctx, pinfo, eclass, item, IsModifiable (item))) != null) {
							ainfo.Add (ci, null);
							added = true;
						}
						
						if (item is IParameter) {
							// Add the encompasing method for the previous item in the menu
							item = ((IParameter) item).DeclaringMember;
							if (item != null && (ci = BuildRefactoryMenuForItem (ctx, pinfo, null, item, true)) != null) {
								ainfo.Add (ci, null);
								added = true;
							}
						}
						
						if (item is IMember && !(eitem != null && eitem is IMember)) {
							// Add the encompasing class for the previous item in the menu
							item = ((IMember) item).DeclaringType;
							if (item != null && (ci = BuildRefactoryMenuForItem (ctx, pinfo, null, item, IsModifiable (item))) != null) {
								ainfo.Add (ci, null);
								added = true;
							}
						}
						
						item = eitem;
						eitem = null;
						eclass = null;
					}
					
					if (added)
						ainfo.AddSeparator ();
				}
			}
		}

		bool IsModifiable (IDomVisitable member)
		{
			IType t = member as IType;
			if (t != null) {
				if (t.CompilationUnit != null) {
					ITextBuffer editor = IdeApp.Workbench.ActiveDocument.GetContent <ITextBuffer>();
					return t.CompilationUnit.FileName == editor.Name;
				}
				else
					return false;
			}
			if (member is IMember)
				return IsModifiable (((IMember)member).DeclaringType);
			else
				return false;
		}
		
		// public class Funkadelic : IAwesomeSauce, IRockOn { ...
		//        ----------------   -------------
		// finds this ^ if you clicked on this ^
		IType FindEnclosingClass (ProjectDom ctx, string fileName, int line, int col)
		{
			IType klass = null;
			foreach (IType c in ctx.GetTypes (fileName)) {
				if (c.BodyRegion.Contains (line, col))
					klass = c;
			}
			
			if (klass != null && klass.ClassType != ClassType.Interface)
				return klass;
			
			return null;
		}
		
		string EscapeName (string name)
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
		}
		
		CommandInfo BuildRefactoryMenuForItem (ProjectDom ctx, ICompilationUnit pinfo, IType eclass, IDomVisitable item, bool includeModifyCommands)
		{
			Refactorer refactorer = new Refactorer (ctx, pinfo, eclass, item, null);
			CommandInfoSet ciset = new CommandInfoSet ();
			Ambience ambience = AmbienceService.GetAmbienceForFile (pinfo.FileName);
			OutputFlags flags = OutputFlags.EmitMarkup;
			if (item is IParameter) {
				flags |= OutputFlags.IncludeParameterName;
			} else {
				flags |= OutputFlags.IncludeParameters;
			}
				
			string itemName = EscapeName (ambience.GetString (item, flags));
			bool canRename = false;
			string txt;
			if (IdeApp.ProjectOperations.CanJumpToDeclaration (item))
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Go to declaration"), new RefactoryOperation (refactorer.GoToDeclaration));
			
			if ((item is IMember || item is LocalVariable || item is IParameter) && !(item is IType))
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Find references"), new RefactoryOperation (refactorer.FindReferences));
			
			// We can rename local variables (always), method params (always), 
			// or class/members (if they belong to a project)
			if ((item is LocalVariable) || (item is IParameter))
				canRename = true;
			else if (item is IType)
				canRename = ((IType) item).SourceProject != null;
			else if (item is IMember) {
				IType cls = ((IMember) item).DeclaringType;
				canRename = cls != null && cls.SourceProject != null;
			}
			
			if (canRename && !(item is IType)) {
				// Defer adding this item for Classes until later
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Rename"), new RefactoryOperation (refactorer.Rename));
			}

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
						if (bc != null && bc.ClassType != ClassType.Interface/* TODO: && IdeApp.ProjectOperations.CanJumpToDeclaration (bc)*/) {
							ciset.CommandInfos.Add (GettextCatalog.GetString ("Go to _base"), new RefactoryOperation (refactorer.GoToBase));
							break;
						}
					}
				}
				
				if ((cls.ClassType == ClassType.Class && !cls.IsSealed) || cls.ClassType == ClassType.Interface)
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Find _derived classes"), new RefactoryOperation (refactorer.FindDerivedClasses));

				if (cls.SourceProject != null && includeModifyCommands && ((cls.ClassType == ClassType.Class) || (cls.ClassType == ClassType.Struct))) {
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Encapsulate Fields..."), new RefactoryOperation (refactorer.EncapsulateField));
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Override/Implement members..."), new RefactoryOperation (refactorer.OverrideOrImplementMembers));
				}
				
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Find references"), new RefactoryOperation (refactorer.FindReferences));
				
				if (canRename)
					ciset.CommandInfos.Add (GettextCatalog.GetString ("_Rename"), new RefactoryOperation (refactorer.Rename));
				
				if (canRename && cls.ClassType == ClassType.Interface && eclass != null) {
					// An interface is selected, so just need to provide these 2 submenu items
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Implement Interface (implicit)"), new RefactoryOperation (refactorer.ImplementImplicitInterface));
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Implement Interface (explicit)"), new RefactoryOperation (refactorer.ImplementExplicitInterface));
				} else if (canRename && includeModifyCommands && cls.BaseType != null && cls.ClassType != ClassType.Interface && cls == eclass) {
					// Class might have interfaces... offer to implement them
					CommandInfoSet impset = new CommandInfoSet ();
					CommandInfoSet expset = new CommandInfoSet ();
					bool added = false;
					
					foreach (IReturnType rt in cls.BaseTypes) {
						IType iface = ctx.GetType (rt);
						if (iface != null && iface.ClassType == ClassType.Interface) {
							Refactorer ifaceRefactorer = new Refactorer (ctx, pinfo, cls, iface, rt);
							impset.CommandInfos.Add (ambience.GetString (rt, OutputFlags.IncludeGenerics), new RefactoryOperation (ifaceRefactorer.ImplementImplicitInterface));
							expset.CommandInfos.Add (ambience.GetString (rt, OutputFlags.IncludeGenerics), new RefactoryOperation (ifaceRefactorer.ImplementExplicitInterface));
							added = true;
						}
					}
					
					if (added) {
						impset.Text = GettextCatalog.GetString ("Implement Interface (implicit)");
						ciset.CommandInfos.Add (impset, null);
						
						expset.Text = GettextCatalog.GetString ("Implement Interface (explicit)");
						ciset.CommandInfos.Add (expset, null);
					}
				}
			} else if ((item is IField) && includeModifyCommands) {
				txt = GettextCatalog.GetString ("Field <b>{0}</b>", itemName);
				if (canRename)
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Encapsulate Field..."), new RefactoryOperation (refactorer.EncapsulateField));
				AddRefactoryMenuForClass (ctx, pinfo, ciset, ((IField) item).ReturnType.FullName);
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
		
		void AddRefactoryMenuForClass (ProjectDom ctx, ICompilationUnit pinfo, CommandInfoSet ciset, string className)
		{
			IType cls = ctx.GetType (className, null, true, true);
			if (cls != null) {
				CommandInfo ci = BuildRefactoryMenuForItem (ctx, pinfo, null, cls, false);
				if (ci != null)
					ciset.CommandInfos.Add (ci, null);
			}
		}
		
		delegate void RefactoryOperation ();
	}
	
	class Refactorer
	{
		MemberReferenceCollection references;
		ISearchProgressMonitor monitor;
		ICompilationUnit pinfo;
		ProjectDom ctx;
		IDomVisitable item;
		IType klass;
		IReturnType hintReturnType;
		
		public Refactorer (ProjectDom ctx, ICompilationUnit pinfo, IType klass, IDomVisitable item, IReturnType hintReturnType)
		{
			this.pinfo = pinfo;
			this.klass = klass;
			this.item = item;
			this.ctx = ctx;
			this.hintReturnType = hintReturnType;
		}
		
		public void GoToDeclaration ()
		{
			IdeApp.ProjectOperations.JumpToDeclaration (item);
		}
		
		public void FindReferences ()
		{
			monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true);
			Thread t = new Thread (new ThreadStart (FindReferencesThread));
			t.IsBackground = true;
			t.Start ();
		}
		
		void FindReferencesThread ()
		{
			using (monitor) {
				CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
				if (item is IType) {
					references = refactorer.FindClassReferences (monitor, (IType)item, RefactoryScope.Solution);
				} else if (item is LocalVariable) {
					references = refactorer.FindVariableReferences (monitor, (LocalVariable)item);
				} else if (item is IParameter) {
					references = refactorer.FindParameterReferences (monitor, (IParameter)item);
				} else if (item is IMember) {
					IMember member = (IMember) item;
					
					// private is filled only in keyword case
					if (member.IsPrivate || (!member.IsProtectedOrInternal && !member.IsPublic)) {
						// look in project to be partial classes safe
						references = refactorer.FindMemberReferences (monitor, member.DeclaringType, member,
						                                              RefactoryScope.Project);
					} else {
						// for all other types look in solution because
						// internal members can be used in friend assemblies
						references = refactorer.FindMemberReferences (monitor, member.DeclaringType, member,
						                                              RefactoryScope.Solution);
					}
				}
				
				if (references != null) {
					foreach (MemberReference mref in references) {
						monitor.ReportResult (mref.FileName, mref.Line, mref.Column, mref.TextLine, mref.Name.Length);
					}
				}
			}
		}
		
		public void GoToBase ()
		{
			IType cls = item as IType;
			if (cls != null && cls.BaseTypes != null) {
				foreach (IReturnType bc in cls.BaseTypes) {
					IType bcls = ctx.GetType (bc);
					if (bcls != null && bcls.ClassType != ClassType.Interface && !bcls.Location.IsEmpty) {
						IdeApp.Workbench.OpenDocument (bcls.CompilationUnit.FileName, bcls.Location.Line, bcls.Location.Column, true);
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
							IdeApp.Workbench.OpenDocument (bcls.CompilationUnit.FileName, baseMethod.Location.Line, baseMethod.Location.Column, true);
						return;
					}
				}
				return;
			}
		}
		
		public void FindDerivedClasses ()
		{
			monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true);
			Thread t = new Thread (new ThreadStart (FindDerivedThread));
			t.IsBackground = true;
			t.Start ();
		}
		
		void FindDerivedThread ()
		{
			using (monitor) {
				IType cls = (IType) item;
				if (cls == null) return;

				CodeRefactorer cr = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
				foreach (IType sub in cr.FindDerivedClasses (cls)) {
					if (!sub.Location.IsEmpty)
						monitor.ReportResult (sub.CompilationUnit.FileName, sub.Location.Line, sub.Location.Column, sub.FullName, 0);
				}
			}
		}
		
		void ImplementInterface (bool explicitly)
		{
			CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
			IType iface = item as IType;
			
			if (klass == null)
				return;
			
			if (iface == null)
				return;
				
			IEditableTextBuffer editor = IdeApp.Workbench.ActiveDocument.GetContent <IEditableTextBuffer>();
			if (editor != null)
				editor.BeginAtomicUndo ();
				
			try {
				refactorer.ImplementInterface (pinfo, klass, iface, explicitly, iface, this.hintReturnType);
			} finally {
				if (editor != null)
					editor.EndAtomicUndo ();
			}
		}
		
		public void ImplementImplicitInterface ()
		{
			ImplementInterface (false);
		}
		
		public void ImplementExplicitInterface ()
		{
			ImplementInterface (true);
		}
		
		public void EncapsulateField ()
		{
			IEditableTextBuffer editor = IdeApp.Workbench.ActiveDocument.GetContent <IEditableTextBuffer>();
			if (editor != null)
				editor.BeginAtomicUndo ();
				
			try {
				EncapsulateFieldDialog dialog;
				if (item is IField)
					dialog = new EncapsulateFieldDialog (ctx, (IField) item);
				else
					dialog = new EncapsulateFieldDialog (ctx, (IType) item);
				
				dialog.Show ();
			} finally {
				if (editor != null)
					editor.EndAtomicUndo ();
			}
			
		}
		
		public void OverrideOrImplementMembers ()
		{
			IEditableTextBuffer editor = IdeApp.Workbench.ActiveDocument.GetContent <IEditableTextBuffer>();
			if (editor != null) 
				editor.BeginAtomicUndo ();
			
			try {
				OverridesImplementsDialog dialog = new MonoDevelop.Ide.OverridesImplementsDialog ((IType)item);
				dialog.Run ();
			} finally {
				if (editor != null)
					editor.EndAtomicUndo ();
			}
		}
		
		public void Rename ()
		{
			RenameItemDialog dialog = new RenameItemDialog (ctx, item);
			dialog.Show ();
		}
	}
}
