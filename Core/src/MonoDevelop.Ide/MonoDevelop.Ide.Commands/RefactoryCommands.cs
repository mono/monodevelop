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
using System.CodeDom;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;
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
			if (doc != null) {
				ITextBuffer editor = IdeApp.Workbench.ActiveDocument.GetContent <ITextBuffer>();
				if (editor != null) {
					bool added = false;
					int line, column;
					
					editor.GetLineColumnFromPosition (editor.CursorPosition, out line, out column);
					IParserContext ctx;
					if (doc.Project != null)
						ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (doc.Project);
					else
						ctx = IdeApp.ProjectOperations.ParserDatabase.GetFileParserContext (doc.FileName);
					
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
					
					if (item != null && eitem != null && eitem.Name == item.Name) {
						// If this occurs, then @item is the base-class version of @eitem
						// in which case we don't want to show the base-class @item, we'd
						// rather show the item the user /actually/ requested, @eitem.
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
						if ((ci = BuildRefactoryMenuForItem (ctx, eclass, item)) != null) {
							ainfo.Add (ci, null);
							added = true;
						}
						
						if (item is IParameter) {
							// Add the encompasing method for the previous item in the menu
							item = ((IParameter) item).DeclaringMember;
							if (item != null && (ci = BuildRefactoryMenuForItem (ctx, null, item)) != null) {
								ainfo.Add (ci, null);
								added = true;
							}
						}
						
						if (item is IMember) {
							// Add the encompasing class for the previous item in the menu
							item = ((IMember) item).DeclaringType;
							if (item != null && (ci = BuildRefactoryMenuForItem (ctx, null, item)) != null) {
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
				if (classes[i] != null &&
				    classes[i].Region != null &&
				    (line > classes[i].Region.BeginLine ||
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
		
		CommandInfo BuildRefactoryMenuForItem (IParserContext ctx, IClass eclass, ILanguageItem item)
		{
			Refactorer refactorer = new Refactorer (ctx, eclass, item);
			CommandInfoSet ciset = new CommandInfoSet ();
			bool canRename = false;
			string txt;
			
			if (IdeApp.ProjectOperations.CanJumpToDeclaration (item))
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Go to declaration"), new RefactoryOperation (refactorer.GoToDeclaration));
			
			if ((item is IMember) && !(item is IClass))
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Find references"), new RefactoryOperation (refactorer.FindReferences));
			
			if ((item is LocalVariable) || (item is IParameter) || 
			    (((item is IMember) || (item is IClass)) && IdeApp.ProjectOperations.CanJumpToDeclaration (item))) {
				// We can rename local variables (always), method params (always), 
				// or class/members (if we can jump to their declarations)
				canRename = true;
				
				if (!(item is IClass)) {
					// Defer adding this item for Classes until later
					ciset.CommandInfos.Add (GettextCatalog.GetString ("_Rename"), new RefactoryOperation (refactorer.Rename));
				}
			}
			
			if (item is IClass) {
				IClass cls = (IClass) item;
				
				if (cls.ClassType == ClassType.Interface)
					txt = GettextCatalog.GetString ("Interface {0}", item.Name);
				else
					txt = GettextCatalog.GetString ("Class {0}", item.Name);
				
				if (cls.BaseTypes.Count > 0) {
					foreach (IReturnType rt in cls.BaseTypes) {
						IClass bc = ctx.GetClass (rt.FullyQualifiedName, null, true, true);
						if (bc != null && bc.ClassType != ClassType.Interface && IdeApp.ProjectOperations.CanJumpToDeclaration (bc)) {
							ciset.CommandInfos.Add (GettextCatalog.GetString ("Go to _base"), new RefactoryOperation (refactorer.GoToBase));
						}
					}
				}
				
				ciset.CommandInfos.Add (GettextCatalog.GetString ("Find _derived classes"), new RefactoryOperation (refactorer.FindDerivedClasses));
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Find references"), new RefactoryOperation (refactorer.FindReferences));
				
				if (canRename)
					ciset.CommandInfos.Add (GettextCatalog.GetString ("_Rename"), new RefactoryOperation (refactorer.Rename));
				
				if (cls.ClassType == ClassType.Interface && eclass != null) {
					// An interface is selected, so just need to provide these 2 submenu items
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Implement Interface (implicit)"), new RefactoryOperation (refactorer.ImplementImplicitInterface));
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Implement Interface (explicit)"), new RefactoryOperation (refactorer.ImplementExplicitInterface));
				} else if (cls.BaseTypes.Count > 0 && cls.ClassType != ClassType.Interface && cls == eclass) {
					// Class might have interfaces... offer to implement them
					CommandInfoSet impset = new CommandInfoSet ();
					CommandInfoSet expset = new CommandInfoSet ();
					bool added = false;
					
					foreach (IReturnType rt in cls.BaseTypes) {
						IClass iface = ctx.GetClass (rt.FullyQualifiedName, null, true, true);
						if (iface != null && iface.ClassType == ClassType.Interface) {
							Refactorer ifaceRefactorer = new Refactorer (ctx, cls, iface);
							
							impset.CommandInfos.Add (iface.Name, new RefactoryOperation (ifaceRefactorer.ImplementImplicitInterface));
							expset.CommandInfos.Add (iface.Name, new RefactoryOperation (ifaceRefactorer.ImplementExplicitInterface));
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
				txt = GettextCatalog.GetString ("Field '{0}'", item.Name);
				AddRefactoryMenuForClass (ctx, ciset, ((IField)item).ReturnType.FullyQualifiedName);
			} else if (item is IProperty) {
				txt = GettextCatalog.GetString ("Property '{0}'", item.Name);
				AddRefactoryMenuForClass (ctx, ciset, ((IProperty)item).ReturnType.FullyQualifiedName);
			} else if (item is IEvent) {
				txt = GettextCatalog.GetString ("Event {0}", item.Name);
			} else if (item is IMethod) {
				IMethod method = item as IMethod;
				
				if (method.IsConstructor)
					txt = GettextCatalog.GetString ("Constructor {0}", method.DeclaringType.Name);
				else
					txt = GettextCatalog.GetString ("Method {0}", item.Name);
			} else if (item is IIndexer) {
				txt = GettextCatalog.GetString ("Indexer {0}", item.Name);
			} else if (item is IParameter) {
				txt = GettextCatalog.GetString ("Parameter {0}", item.Name);
				AddRefactoryMenuForClass (ctx, ciset, ((IParameter)item).ReturnType.FullyQualifiedName);
			} else if (item is LocalVariable) {
				LocalVariable var = (LocalVariable) item;
				AddRefactoryMenuForClass (ctx, ciset, var.ReturnType.FullyQualifiedName);
				txt = GettextCatalog.GetString ("Variable {0}", item.Name);
			} else
				return null;
			
			ciset.Text = txt;
			return ciset;
		}
		
		void AddRefactoryMenuForClass (IParserContext ctx, CommandInfoSet ciset, string className)
		{
			IClass cls = ctx.GetClass (className, true, true);
			if (cls != null) {
				CommandInfo ci = BuildRefactoryMenuForItem (ctx, null, cls);
				if (ci != null)
					ciset.CommandInfos.Add (ci, null);
			}
		}
		
		delegate void RefactoryOperation ();
	}
	
	class Refactorer
	{
		IClass klass;
		ILanguageItem item;
		IParserContext ctx;
		MemberReferenceCollection references;
		ISearchProgressMonitor monitor;
		
		public Refactorer (IParserContext ctx, IClass klass, ILanguageItem item)
		{
			this.klass = klass;
			this.item = item;
			this.ctx = ctx;
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
				CodeRefactorer refactorer = IdeApp.ProjectOperations.CodeRefactorer;
				
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
			IClass cls = (IClass) item;
			if (cls == null) return;
			
			if (cls.BaseTypes != null) {
				foreach (IReturnType bc in cls.BaseTypes) {
					IClass bcls = ctx.GetClass (bc.FullyQualifiedName, true, true);
					if (bcls != null && bcls.ClassType != ClassType.Interface && bcls.Region != null) {
						IdeApp.Workbench.OpenDocument (bcls.Region.FileName, bcls.Region.BeginLine, bcls.Region.BeginColumn, true);
						return;
					}
				}
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
			
				IClass[] classes = IdeApp.ProjectOperations.CodeRefactorer.FindDerivedClasses (cls);
				foreach (IClass sub in classes) {
					if (sub.Region != null)
						monitor.ReportResult (sub.Region.FileName, sub.Region.BeginLine, sub.Region.BeginColumn, sub.FullyQualifiedName);
				}
			}
		}
		
		void ImplementInterface (bool explicitly)
		{
			CodeRefactorer refactorer = IdeApp.ProjectOperations.CodeRefactorer;
			CodeThrowExceptionStatement throwNotImplemented = new CodeThrowExceptionStatement ();
			IClass iface = item as IClass;
			bool alreadyImplemented;
			int i, j;
			
			if (klass == null)
				return;
			
			if (iface == null)
				return;
			
			// Add stubs of props, methods and events in reverse order as each
			// item is always written to the top of the class.
			
			// Stub out non-implemented events defined by @iface
			for (i = iface.Events.Count - 1; i >= 0; i--) {
				IEvent ev = iface.Events[i];
				
				for (j = 0, alreadyImplemented = false; j < klass.Events.Count; j++) {
					if (klass.Events[j].FullyQualifiedName == ev.FullyQualifiedName) {
						alreadyImplemented = true;
						break;
					}
				}
				
				if (alreadyImplemented)
					continue;
				
				CodeMemberEvent member;
				
				member = new CodeMemberEvent ();
				if (explicitly)
					member.Name = ev.FullyQualifiedName;
				else
					member.Name = ev.Name;
				
				member.Type = new CodeTypeReference (ev.ReturnType.Name);
				
				refactorer.AddMember (klass, member);
			}
			
			// Stub out non-implemented methods defined by @iface
			for (i = iface.Methods.Count - 1; i >= 0; i--) {
				IMethod method = iface.Methods[i];
				
				for (j = 0, alreadyImplemented = false; j < klass.Methods.Count; j++) {
					if (klass.Methods[j].FullyQualifiedName == method.FullyQualifiedName) {
						alreadyImplemented = true;
						break;
					}
				}
				
				if (alreadyImplemented)
					continue;
				
				CodeMemberMethod member;
				member = new CodeMemberMethod ();
				if (explicitly)
					member.Name = method.FullyQualifiedName;
				else
					member.Name = method.Name;
				
				member.ReturnType = new CodeTypeReference (method.ReturnType.Name);
				member.Attributes = MemberAttributes.Public;
				foreach (IParameter param in method.Parameters) {
					CodeParameterDeclarationExpression par;
					par = new CodeParameterDeclarationExpression (param.ReturnType.Name, param.Name);
					member.Parameters.Add (par);
				}
				
				refactorer.AddMember (klass, member);
			}
			
			// Stub out non-implemented properties defined by @iface
			for (i = iface.Properties.Count - 1; i >= 0; i--) {
				IProperty prop = iface.Properties[i];
				
				for (j = 0, alreadyImplemented = false; j < klass.Properties.Count; j++) {
					if (klass.Properties[j].FullyQualifiedName == prop.FullyQualifiedName) {
						alreadyImplemented = true;
						break;
					}
				}
				
				if (alreadyImplemented)
					continue;
				
				CodeMemberProperty member;
				
				member = new CodeMemberProperty ();
				if (explicitly)
					member.Name = prop.FullyQualifiedName;
				else
					member.Name = prop.Name;
				
				member.Type = new CodeTypeReference (prop.ReturnType.Name);
				member.Attributes = MemberAttributes.Public;
				member.HasGet = prop.CanGet;
				member.HasSet = prop.CanSet;
				
				refactorer.AddMember (klass, member);
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
		
		public void Rename ()
		{
			RenameItemDialog dialog = new RenameItemDialog (ctx, item);
			dialog.Show ();
		}
	}
}
