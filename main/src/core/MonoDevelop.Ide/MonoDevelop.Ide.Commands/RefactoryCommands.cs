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
using System.Text;
using System.CodeDom;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Ambience;
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
					IParseInformation pinfo;
					IParserContext ctx;
					if (doc.Project != null) {
						ctx = IdeApp.Workspace.ParserDatabase.GetProjectParserContext (doc.Project);
						pinfo = IdeApp.Workspace.ParserDatabase.UpdateFile (doc.Project, doc.FileName, editor.Text);
					} else {
						ctx = IdeApp.Workspace.ParserDatabase.GetFileParserContext (doc.FileName);
						pinfo = IdeApp.Workspace.ParserDatabase.UpdateFile (doc.FileName, editor.Text);
					}
					
					// Look for an identifier at the cursor position
					
					string id = editor.SelectedText;
					if (id.Length == 0) {
						IExpressionFinder finder = Services.ParserService.GetExpressionFinder (editor.Name);
						if (finder == null)
							return;
						id = finder.FindFullExpression (editor.Text, editor.CursorPosition).Expression;
						if (id == null) return;
					}
					
					ILanguageItem item = ctx.ResolveIdentifier (id, line, column, editor.Name, null);
					ILanguageItem eitem = ctx.GetEnclosingLanguageItem (line, column, editor);
					
					if (item != null && eitem != null && 
					    (eitem == item || (eitem.Name == item.Name && !(eitem is IProperty) && !(eitem is IMethod)))) {
						// If this occurs, then @item is either its own enclosing item, in
						// which case, we don't want to show it twice, or it is the base-class
						// version of @eitem, in which case we don't want to show the base-class
						// @item, we'd rather show the item the user /actually/ requested, @eitem.
						item = eitem;
						eitem = null;
					}
					
					IClass eclass = null;
					
					if (item is IClass) {
						if (((IClass) item).ClassType == ClassType.Interface)
							eclass = FindEnclosingClass (ctx, editor.Name, line, column);
						else
							eclass = (IClass) item;
					}
					
					while (item != null) {
						CommandInfo ci;
						
						// Add the selected item
						if ((ci = BuildRefactoryMenuForItem (ctx, pinfo, eclass, item)) != null) {
							ainfo.Add (ci, null);
							added = true;
						}
						
						if (item is IParameter) {
							// Add the encompasing method for the previous item in the menu
							item = ((IParameter) item).DeclaringMember;
							if (item != null && (ci = BuildRefactoryMenuForItem (ctx, pinfo, null, item)) != null) {
								ainfo.Add (ci, null);
								added = true;
							}
						}
						
						if (item is IMember && !(eitem != null && eitem is IMember)) {
							// Add the encompasing class for the previous item in the menu
							item = ((IMember) item).DeclaringType;
							if (item != null && (ci = BuildRefactoryMenuForItem (ctx, pinfo, null, item)) != null) {
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
		
		// public class Funkadelic : IAwesomeSauce, IRockOn { ...
		//        ----------------   -------------
		// finds this ^ if you clicked on this ^
		IClass FindEnclosingClass (IParserContext ctx, string fileName, int line, int col)
		{
			IClass [] classes = ctx.GetFileContents (fileName);
			IClass klass = null;
			
			if (classes == null)
				return null;
			
			for (int i = 0; i < classes.Length; i++) {
				if ((line > classes[i].Region.BeginLine ||
				     (line == classes[i].Region.BeginLine && col >= classes[i].Region.BeginColumn)) &&
				    (line < classes[i].Region.EndLine ||
				     (line == classes[i].Region.EndLine && col <= classes[i].Region.EndColumn))) {
					klass = classes[i];
					break;
				}
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
		
		CommandInfo BuildRefactoryMenuForItem (IParserContext ctx, IParseInformation pinfo, IClass eclass, ILanguageItem item)
		{
			Refactorer refactorer = new Refactorer (ctx, pinfo, eclass, item, null);
			CommandInfoSet ciset = new CommandInfoSet ();
			Ambience ambience = null;
			Project project = IdeApp.Workbench.ActiveDocument.Project;
			if (project != null) {
				ambience = project.Ambience;
			} else 
				ambience = new NetAmbience ();
			string itemName = EscapeName (ambience.Convert (item, ConversionFlags.ShowGenericParameters | ConversionFlags.IncludeHTMLMarkup));
			bool canRename = false;
			string txt;
			if (IdeApp.ProjectOperations.CanJumpToDeclaration (item))
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Go to declaration"), new RefactoryOperation (refactorer.GoToDeclaration));
			
			if ((item is IMember || item is LocalVariable || item is IParameter) && !(item is IClass))
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Find references"), new RefactoryOperation (refactorer.FindReferences));
			
			// We can rename local variables (always), method params (always), 
			// or class/members (if they belong to a project)
			if ((item is LocalVariable) || (item is IParameter))
				canRename = true;
			else if (item is IClass)
				canRename = ((IClass) item).SourceProject != null;
			else if (item is IMember) {
				IClass cls = ((IMember) item).DeclaringType;
				canRename = cls != null && cls.SourceProject != null;
			}
			
			if (canRename && !(item is IClass)) {
				// Defer adding this item for Classes until later
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Rename"), new RefactoryOperation (refactorer.Rename));
			}

			if (item is IClass) {
				IClass cls = (IClass) item;
				
				if (cls.ClassType == ClassType.Enum)
					txt = GettextCatalog.GetString ("Enum <b>{0}</b>", itemName);
				else if (cls.ClassType == ClassType.Struct)
					txt = GettextCatalog.GetString ("Struct <b>{0}</b>", itemName);
				else if (cls.ClassType == ClassType.Interface)
					txt = GettextCatalog.GetString ("Interface <b>{0}</b>", itemName);
				else
					txt = GettextCatalog.GetString ("Class <b>{0}</b>", itemName);
				
				if (cls.BaseTypes.Count > 0 && cls.ClassType == ClassType.Class) {
					foreach (IReturnType rt in cls.BaseTypes) {
						IClass bc = ctx.GetClass (rt.FullyQualifiedName, null, true, true);
						if (bc != null && bc.ClassType != ClassType.Interface && IdeApp.ProjectOperations.CanJumpToDeclaration (bc)) {
							ciset.CommandInfos.Add (GettextCatalog.GetString ("Go to _base"), new RefactoryOperation (refactorer.GoToBase));
							break;
						}
					}
				}
				
				if ((cls.ClassType == ClassType.Class && !cls.IsSealed) || cls.ClassType == ClassType.Interface)
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Find _derived classes"), new RefactoryOperation (refactorer.FindDerivedClasses));

				if (cls.SourceProject != null && ((cls.ClassType == ClassType.Class) || (cls.ClassType == ClassType.Struct))) {
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
				} else if (canRename && cls.BaseTypes.Count > 0 && cls.ClassType != ClassType.Interface && cls == eclass) {
					// Class might have interfaces... offer to implement them
					CommandInfoSet impset = new CommandInfoSet ();
					CommandInfoSet expset = new CommandInfoSet ();
					bool added = false;
					
					foreach (IReturnType rt in cls.BaseTypes) {
						IClass iface = ctx.GetClass (rt.FullyQualifiedName, rt.GenericArguments, true, true);
						if (iface != null && iface.ClassType == ClassType.Interface) {
							Refactorer ifaceRefactorer = new Refactorer (ctx, pinfo, cls, iface, rt);
							impset.CommandInfos.Add (ambience.Convert (rt, ConversionFlags.ShowGenericParameters), new RefactoryOperation (ifaceRefactorer.ImplementImplicitInterface));
							expset.CommandInfos.Add (ambience.Convert (rt, ConversionFlags.ShowGenericParameters), new RefactoryOperation (ifaceRefactorer.ImplementExplicitInterface));
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
			} else if (item is IField) {
				txt = GettextCatalog.GetString ("Field <b>{0}</b>", itemName);
				if (canRename)
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Encapsulate Field..."), new RefactoryOperation (refactorer.EncapsulateField));
				AddRefactoryMenuForClass (ctx, pinfo, ciset, ((IField) item).ReturnType.FullyQualifiedName);
			} else if (item is IProperty) {
				txt = GettextCatalog.GetString ("Property <b>{0}</b>", itemName);
				AddRefactoryMenuForClass (ctx, pinfo, ciset, ((IProperty) item).ReturnType.FullyQualifiedName);
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
			} else if (item is IIndexer) {
				txt = GettextCatalog.GetString ("Indexer <b>{0}</b>", itemName);
			} else if (item is IParameter) {
				txt = GettextCatalog.GetString ("Parameter <b>{0}</b>", itemName);
				AddRefactoryMenuForClass (ctx, pinfo, ciset, ((IParameter) item).ReturnType.FullyQualifiedName);
			} else if (item is LocalVariable) {
				LocalVariable var = (LocalVariable) item;
				AddRefactoryMenuForClass (ctx, pinfo, ciset, var.ReturnType.FullyQualifiedName);
				txt = GettextCatalog.GetString ("Variable <b>{0}</b>", itemName);
			} else {
				return null;
			}
			
			ciset.Text = txt;
			ciset.UseMarkup = true;
			return ciset;
		}
		
		void AddRefactoryMenuForClass (IParserContext ctx, IParseInformation pinfo, CommandInfoSet ciset, string className)
		{
			IClass cls = ctx.GetClass (className, true, true);
			if (cls != null) {
				CommandInfo ci = BuildRefactoryMenuForItem (ctx, pinfo, null, cls);
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
		IParseInformation pinfo;
		IParserContext ctx;
		ILanguageItem item;
		IClass klass;
		IReturnType hintReturnType;
		
		public Refactorer (IParserContext ctx, IParseInformation pinfo, IClass klass, ILanguageItem item, IReturnType hintReturnType)
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
				
				if (item is IMember) {
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
				} else if (item is IClass) {
					references = refactorer.FindClassReferences (monitor, (IClass)item, RefactoryScope.Solution);
				} else if (item is LocalVariable) {
					references = refactorer.FindVariableReferences (monitor, (LocalVariable)item);
				} else if (item is IParameter) {
					references = refactorer.FindParameterReferences (monitor, (IParameter)item);
				}
				
				if (references != null) {
					foreach (MemberReference mref in references) {
						monitor.ReportResult (mref.FileName, mref.Line, mref.Column, mref.TextLine);
					}
				}
			}
		}
		
		public void GoToBase ()
		{
			IClass cls = item as IClass;
			if (cls != null && cls.BaseTypes != null) {
				foreach (IReturnType bc in cls.BaseTypes) {
					IClass bcls = ctx.GetClass (bc.FullyQualifiedName, true, true);
					if (bcls != null && bcls.ClassType != ClassType.Interface && bcls.Region != null) {
						IdeApp.Workbench.OpenDocument (bcls.Region.FileName, bcls.Region.BeginLine, bcls.Region.BeginColumn, true);
						return;
					}
				}
				return;
			}
			IMethod method = item as IMethod;
			if (method != null) {
				foreach (IReturnType bc in method.DeclaringType.BaseTypes) {
					IClass bcls = ctx.GetClass (bc.FullyQualifiedName, true, true);
					if (bcls != null && bcls.ClassType != ClassType.Interface && bcls.Region != null) {
						IMethod baseMethod = null;
						foreach (IMethod m in bcls.Methods) {
							if (m.Name == method.Name && m.Parameters.Count == m.Parameters.Count) {
								baseMethod = m;
								break;
							}
						}
						if (baseMethod != null)
							IdeApp.Workbench.OpenDocument (bcls.Region.FileName, baseMethod.Region.BeginLine, baseMethod.Region.BeginColumn, true);
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
				IClass cls = (IClass) item;
				if (cls == null) return;

				CodeRefactorer cr = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
				IClass[] classes = cr.FindDerivedClasses (cls);
				foreach (IClass sub in classes) {
					if (sub.Region != null)
						monitor.ReportResult (sub.Region.FileName, sub.Region.BeginLine, sub.Region.BeginColumn, sub.FullyQualifiedName);
				}
			}
		}
		
		void ImplementInterface (bool explicitly)
		{
			CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
			IClass iface = item as IClass;
			
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
					dialog = new EncapsulateFieldDialog (ctx, (IClass) item);

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
				OverridesImplementsDialog dialog = new MonoDevelop.Ide.OverridesImplementsDialog ((IClass) item);
				dialog.Show ();
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
