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
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Projects;
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
					IParserContext ctx = null;
					if (doc.Project != null)
						ctx = RefactoryService.ParserDatabase.GetProjectParserContext (doc.Project);
					else
						ctx = RefactoryService.ParserDatabase.GetFileParserContext (doc.FileName);
					
					// Look for an identifier at the cursor position
					
					string id = editor.SelectedText;
// TODO: Project Conversion
//					if (id.Length == 0) {
//						IExpressionFinder finder = Services.ParserService.GetExpressionFinder (editor.Name);
//						if (finder == null)
//							return;
//						id = finder.FindFullExpression (editor.Text, editor.CursorPosition).Expression;
//						if (id == null) return;
//					}
//					
					ILanguageItem item = ctx.ResolveIdentifier (id, line, column, editor.Name, null);
					ILanguageItem eitem = ctx.GetEnclosingLanguageItem (line, column, editor);
					
					if (item != null && eitem != null && eitem.Name == item.Name) {
						// If this occurs, then @item is the base-class version of @eitem
						// in which case we don't want to show the base-class @item, we'd
						// rather show the item the user /actually/ requested, @eitem.
						item = eitem;
						eitem = null;
					}
					
					while (item != null) {
						CommandInfo ci;
						
						// Add the selected item
						if ((ci = BuildRefactoryMenuForItem (ctx, item)) != null) {
							ainfo.Add (ci, null);
							added = true;
						}
						
						if (item is IParameter) {
							// Add the encompasing method for the previous item in the menu
							item = ((IParameter) item).DeclaringMember;
							if (item != null && (ci = BuildRefactoryMenuForItem (ctx, item)) != null) {
								ainfo.Add (ci, null);
								added = true;
							}
						}
						
						if (item is IMember) {
							// Add the encompasing class for the previous item in the menu
							item = ((IMember) item).DeclaringType;
							if (item != null && (ci = BuildRefactoryMenuForItem (ctx, item)) != null) {
								ainfo.Add (ci, null);
								added = true;
							}
						}
						
						item = eitem;
						eitem = null;
					}
					
					if (added)
						ainfo.AddSeparator ();
				}
			}
		}
		
		CommandInfo BuildRefactoryMenuForItem (IParserContext ctx, ILanguageItem item)
		{
			Refactorer refactorer = new Refactorer (ctx, item);
			CommandInfoSet ciset = new CommandInfoSet ();
			bool canRename = false;
			string txt;
//	TODO: Project Conversion
//			if (IdeApp.ProjectOperations.CanJumpToDeclaration (item))
//				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Go to declaration"), new RefactoryOperation (refactorer.GoToDeclaration));
//			
			if ((item is IMember) && !(item is IClass))
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Find references"), new RefactoryOperation (refactorer.FindReferences));
//	TODO: Project Conversion
//			if ((item is LocalVariable) || (item is IParameter) || 
//			    (((item is IMember) || (item is IClass)) && IdeApp.ProjectOperations.CanJumpToDeclaration (item))) {
//				// We can rename local variables (always), method params (always), 
//				// or class/members (if we can jump to their declarations)
//				canRename = true;
//				
//				if (!(item is IClass)) {
//					// Defer adding this item for Classes until later
//					ciset.CommandInfos.Add (GettextCatalog.GetString ("_Rename"), new RefactoryOperation (refactorer.Rename));
//				}
//			}
			
			if (item is IClass) {
				IClass cls = (IClass) item;
				
				if (cls.ClassType == ClassType.Interface)
					txt = GettextCatalog.GetString ("Interface {0}", item.Name);
				else
					txt = GettextCatalog.GetString ("Class {0}", item.Name);
//	TODO: Project Conversion
//				if (cls.BaseTypes.Count > 0) {
//					foreach (IReturnType rt in cls.BaseTypes) {
//						IClass bc = ctx.GetClass (rt.FullyQualifiedName, null, true, true);
//						if (bc != null && bc.ClassType != ClassType.Interface && IdeApp.ProjectOperations.CanJumpToDeclaration (bc)) {
//							ciset.CommandInfos.Add (GettextCatalog.GetString ("Go to _base"), new RefactoryOperation (refactorer.GoToBase));
//						}
//					}
//				}
//				
				ciset.CommandInfos.Add (GettextCatalog.GetString ("Find _derived classes"), new RefactoryOperation (refactorer.FindDerivedClasses));
				ciset.CommandInfos.Add (GettextCatalog.GetString ("_Find references"), new RefactoryOperation (refactorer.FindReferences));
				
				if (canRename)
					ciset.CommandInfos.Add (GettextCatalog.GetString ("_Rename"), new RefactoryOperation (refactorer.Rename));
				
				if (cls.ClassType == ClassType.Interface) {
					// An interface is selected, so just need to provide these 2 submenu items
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Implement Interface (implicit)"), new RefactoryOperation (refactorer.ImplementImplicitInterface));
					ciset.CommandInfos.Add (GettextCatalog.GetString ("Implement Interface (explicit)"), new RefactoryOperation (refactorer.ImplementExplicitInterface));
				} else if (cls.BaseTypes.Count > 0) {
					// Class might have interfaces... offer to implement them
					CommandInfoSet impset = new CommandInfoSet ();
					CommandInfoSet expset = new CommandInfoSet ();
					bool added = false;
					
					foreach (IReturnType rt in cls.BaseTypes) {
						IClass iface = ctx.GetClass (rt.FullyQualifiedName, null, true, true);
						if (iface != null && iface.ClassType == ClassType.Interface) {
							Refactorer ifaceRefactorer = new Refactorer (ctx, iface);
							
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
				CommandInfo ci = BuildRefactoryMenuForItem (ctx, cls);
				if (ci != null)
					ciset.CommandInfos.Add (ci, null);
			}
		}
		
		delegate void RefactoryOperation ();
	}
	
	class Refactorer
	{
		ILanguageItem item;
		IParserContext ctx;
		MemberReferenceCollection references;
		ISearchProgressMonitor monitor;
		
		public Refactorer (IParserContext ctx, ILanguageItem item)
		{
			this.item = item;
			this.ctx = ctx;
		}
		
		public void GoToDeclaration ()
		{
//	TODO: Project Conversion
//			IdeApp.ProjectOperations.JumpToDeclaration (item);
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
//	TODO: Project Conversion			
//			using (monitor) {
//				CodeRefactorer refactorer = IdeApp.ProjectOperations.CodeRefactorer;
//				
//				if (item is IMember) {
//					IMember member = (IMember) item;
//					
//					// private is filled only in keyword case
//					if (member.IsPrivate || (!member.IsProtectedOrInternal && !member.IsPublic)) {
//						// look in project to be partial classes safe
//						references = refactorer.FindMemberReferences (monitor, member.DeclaringType, member,
//											      RefactoryScope.Project);
//					} else {
//						// for all other types look in solution because
//						// internal members can be used in friend assemblies
//						references = refactorer.FindMemberReferences (monitor, member.DeclaringType, member,
//											      RefactoryScope.Solution);
//					}
//				} else if (item is IClass) {
//					references = refactorer.FindClassReferences (monitor, (IClass)item, RefactoryScope.Solution);
//				}
//				
//				if (references != null) {
//					foreach (MemberReference mref in references) {
//						monitor.ReportResult (mref.FileName, mref.Line, mref.Column, mref.TextLine);
//					}
//				}
//			}
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
//	TODO: Project Conversion
//			using (monitor) {
//				IClass cls = (IClass) item;
//				if (cls == null) return;
//			
//				IClass[] classes = IdeApp.ProjectOperations.CodeRefactorer.FindDerivedClasses (cls);
//				foreach (IClass sub in classes) {
//					if (sub.Region != null)
//						monitor.ReportResult (sub.Region.FileName, sub.Region.BeginLine, sub.Region.BeginColumn, sub.FullyQualifiedName);
//				}
//			}
		}
		
		void ImplementInterface (bool explicitly)
		{
			// FIXME: implement me
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
