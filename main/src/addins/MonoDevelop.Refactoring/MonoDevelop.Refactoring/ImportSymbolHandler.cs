// 
// RefactoryCommands.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Text;
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
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Refactoring;
using MonoDevelop.Refactoring.RefactorImports;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using Mono.TextEditor;

namespace MonoDevelop.Refactoring
{
	public class ImportSymbolCompletionData : IActionCompletionData
	{
		TextEditorData data;
		IType type;
		Ambience ambience;
		ICompilationUnit unit;
		ProjectDom dom;
		MonoDevelop.Ide.Gui.Document doc;
		public ImportSymbolCompletionData (MonoDevelop.Ide.Gui.Document doc, ProjectDom dom, IType type)
		{
			this.doc = doc;
			this.dom = dom;
			this.data = doc.TextEditorData;
			this.ambience = doc.Project.Ambience;
			this.type = type;
			this.unit = doc.CompilationUnit;
		}
		
		bool initialized = false;
		
		bool generateUsing;
		bool insertNamespace;
		
		void Initialize ()
		{
			if (initialized)
				return;
			initialized = true;
			insertNamespace = false;
			DomLocation location = new DomLocation (data.Caret.Line, data.Caret.Column);
			foreach (IUsing u in unit.Usings.Where (u => u.ValidRegion.Contains (location))) {
				if (u.Namespaces.Any (ns => type.Namespace == ns)) {
					generateUsing = false;
					return;
				}
			}
			generateUsing = true;
			string name = type.DecoratedFullName.Substring (type.Namespace.Length);
			
			foreach (IUsing u in unit.Usings.Where (u => u.ValidRegion.Contains (location))) {
				foreach (string ns in u.Namespaces) {
					if (dom.SearchType (unit, unit.GetTypeAt (location), unit.GetMemberAt (location), ns + "." + name) != null) {
						generateUsing = false;
						insertNamespace = true;
						return;
					}
				}
			}
		}
		
		#region IActionCompletionData implementation
		public void InsertCompletionText (ICompletionWidget widget, CodeCompletionContext context)
		{
			Initialize ();
			string text = insertNamespace ? type.Namespace + "." + type.Name : type.Name;
			data.Replace (context.TriggerOffset, data.Caret.Offset - context.TriggerOffset, text);
			data.Caret.Offset = context.TriggerOffset + CompletionText.Length;
			if (generateUsing) {
				CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
				refactorer.AddGlobalNamespaceImport (dom, data.Document.FileName, type.Namespace);
				// add using to compilation unit (this way the using is valid before the parser thread updates)
				((MonoDevelop.Projects.Dom.CompilationUnit)doc.CompilationUnit).AddAtTop (new DomUsing (new DomRegion (1, 1, int.MaxValue, 1), type.Namespace));
			}
		}
		#endregion
		
		#region ICompletionData implementation
		public IconId Icon {
			get {
				return type.StockIcon;
			}
		}
		
		public string DisplayText {
			get {
				return ambience.GetString (type, OutputFlags.IncludeGenerics);
			}
		}
		
		public string Description {
			get {
				Initialize ();
				if (generateUsing) {
					return string.Format (GettextCatalog.GetString ("Add namespace import '{0}'"), type.Namespace);
				} 
				return null;;
			}
		}
		
		public string CompletionText {
			get {
				return type.Name;
			}
		}
		
		public CompletionCategory CompletionCategory {
			get {
				return null;
			}
		}
		
		public DisplayFlags DisplayFlags {
			get {
				return DisplayFlags.None;
			}
		}
		#endregion
	}
	
	public class ImportSymbolHandler: CommandHandler
	{
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || IdeApp.ProjectOperations.CurrentSelectedSolution == null)
				return;
			ITextEditorExtension ext = doc.EditorExtension;
			while (ext != null && !(ext is CompletionTextEditorExtension))
				ext = ext.Next;
			if (ext == null)
				return;
			
			ProjectDom dom = ProjectDomService.GetProjectDom (doc.Project);
			
			ICompletionDataList completionList = new CompletionDataList ();
			foreach (IType type in dom.Types) {
				completionList.Add (new ImportSymbolCompletionData (doc, dom, type));
			}
			
			foreach (var refDom in dom.References) {
				foreach (IType type in refDom.Types) {
					completionList.Add (new ImportSymbolCompletionData (doc, dom, type));
				}
			}
			
			((CompletionTextEditorExtension)ext).ShowCompletion (completionList);
		}
	}
}
