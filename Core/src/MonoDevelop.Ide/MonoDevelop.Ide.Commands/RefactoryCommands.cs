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
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Projects.CodeGeneration;

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
				ITextBuffer editor = IdeApp.Workbench.ActiveDocument.Content as ITextBuffer;
				if (editor != null) {
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
					if (item != null) {
						CommandInfo ci = BuildRefactoryMenuForItem (ctx, item);
						if (ci != null)
							ainfo.Add (ci, null);
					}
					
					// Look for the enclosing language item
					
					ILanguageItem eitem = ctx.GetEnclosingLanguageItem (line, column, editor);
					if (eitem != null && eitem != item) {
						CommandInfo ci = BuildRefactoryMenuForItem (ctx, eitem);
						if (ci != null)
							ainfo.Add (ci, null);
					}
				}
			}
		}
		
		CommandInfo BuildRefactoryMenuForItem (IParserContext ctx, ILanguageItem item)
		{
			Refactorer refactorer = new Refactorer (ctx, item);
			CommandInfoSet ciset = new CommandInfoSet ();
			string txt;
			
			if ((item is IMember && ((IMember)item).Region != null) || (item is IClass && ((IClass)item).Region != null)) {
				ciset.CommandInfos.Add (GettextCatalog.GetString ("Go to declaration"), new RefactoryOperation (refactorer.GoToDeclaration));
			}
			
			if (item is IMember || item is IClass) {
				ciset.CommandInfos.Add (GettextCatalog.GetString ("Find references"), new RefactoryOperation (refactorer.FindReferences)).Enabled = false;
			}
			
			if (item is IClass) {
				txt = GettextCatalog.GetString ("Class {0}", item.Name);
				ciset.CommandInfos.Add (GettextCatalog.GetString ("Go to base"), new RefactoryOperation (refactorer.GoToBase));
				ciset.CommandInfos.Add (GettextCatalog.GetString ("Find derived classes"), new RefactoryOperation (refactorer.FindDerivedClasses)).Enabled = false;
			}
			else if (item is IField) {
				txt = GettextCatalog.GetString ("Field {0} : {1}", item.Name, ((IField)item).ReturnType.Name);
				AddRefactoryMenuForClass (ctx, ciset, ((IField)item).ReturnType.FullyQualifiedName);
			} else if (item is IProperty) {
				txt = GettextCatalog.GetString ("Property {0} : {1}", item.Name, ((IProperty)item).ReturnType.Name);
				AddRefactoryMenuForClass (ctx, ciset, ((IProperty)item).ReturnType.FullyQualifiedName);
			} else if (item is IEvent)
				txt = GettextCatalog.GetString ("Event {0}", item.Name);
			else if (item is IMethod)
				txt = GettextCatalog.GetString ("Method {0}", item.Name);
			else if (item is IIndexer)
				txt = GettextCatalog.GetString ("Indexer {0}", item.Name);
			else if (item is IParameter) {
				txt = GettextCatalog.GetString ("Parameter {0}", item.Name);
				AddRefactoryMenuForClass (ctx, ciset, ((IParameter)item).ReturnType.FullyQualifiedName);
			} else if (item is LocalVariable) {
				LocalVariable var = (LocalVariable) item;
				AddRefactoryMenuForClass (ctx, ciset, var.ReturnType.FullyQualifiedName);
				txt = GettextCatalog.GetString ("Variable {0}", item.Name);
			}
			else
				return null;
				
			if (item is IMember) {
				IClass cls = ((IMember)item).DeclaringType;
				if (cls != null) {
					CommandInfo ci = BuildRefactoryMenuForItem (ctx, cls);
					if (ci != null)
						ciset.CommandInfos.Add (ci, null);
				}
			} 

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
			if (item is IMember) {
				IMember mem = (IMember) item;
				if (mem.DeclaringType != null && mem.DeclaringType.Region != null && mem.Region != null)
					IdeApp.Workbench.OpenDocument (mem.DeclaringType.Region.FileName, mem.Region.BeginLine, mem.Region.BeginColumn, true);
			} else if (item is IClass) {
				IClass cls = (IClass) item;
				if (cls.Region != null)
					IdeApp.Workbench.OpenDocument (cls.Region.FileName, cls.Region.BeginLine, cls.Region.BeginColumn, true);
			}
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
				CodeRefactorer refactorer = new CodeRefactorer (IdeApp.ProjectOperations.CurrentOpenCombine, IdeApp.ProjectOperations.ParserDatabase);
				
				if (item is IMember) {
					references = refactorer.FindMemberReferences (monitor, ((IMember)item).DeclaringType, (IMember)item, RefactoryScope.Solution);
				} else if (item is IClass) {
					references = refactorer.FindClassReferences (monitor, (IClass)item, RefactoryScope.Solution);
				}
				
				if (references != null) {
					foreach (MemberReference mref in references) {
						monitor.ReportResult (mref.FileName, mref.Line, mref.Column, item.Name);
					}
				}
			}
		}
		
		public void GoToBase ()
		{
			IClass cls = (IClass) item;
			if (cls == null) return;
			
			if (cls.BaseTypes != null) {
				foreach (string bc in cls.BaseTypes) {
					IClass bcls = ctx.GetClass (bc, true, true);
					if (bcls != null && bcls.ClassType != ClassType.Interface && bcls.Region != null) {
						IdeApp.Workbench.OpenDocument (bcls.Region.FileName, bcls.Region.BeginLine, bcls.Region.BeginColumn, true);
						return;
					}
				}
			}
		}
		
		public void FindDerivedClasses ()
		{
		}
	}
}
