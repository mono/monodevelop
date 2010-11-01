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
using MonoDevelop.Ide;
using MonoDevelop.Refactoring.RefactorImports;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using Mono.TextEditor;

namespace MonoDevelop.Refactoring
{
	class GenerateNamespaceImport
	{
		public bool GenerateUsing { get; set; }
		public bool InsertNamespace { get; set; }
	}
	
	class ImportSymbolCache
	{
		Dictionary<string, GenerateNamespaceImport> cache = new Dictionary<string, GenerateNamespaceImport> ();
		
		public GenerateNamespaceImport GetResult (ProjectDom dom, ICompilationUnit unit, IType type, TextEditorData data)
		{
			GenerateNamespaceImport result;
			if (cache.TryGetValue (type.Namespace, out result))
				return result;
			result = new GenerateNamespaceImport ();
			cache[type.Namespace] = result;
			
			result.InsertNamespace  = false;
			
			DomLocation location = new DomLocation (data.Caret.Line, data.Caret.Column);
			foreach (IUsing u in unit.Usings.Where (u => u.ValidRegion.Contains (location))) {
				if (u.Namespaces.Any (ns => type.Namespace == ns)) {
					result.GenerateUsing = false;
					return result;
				}
			}
			result.GenerateUsing = true;
			string name = type.DecoratedFullName.Substring (type.Namespace.Length + 1);
			
			foreach (IUsing u in unit.Usings.Where (u => u.ValidRegion.Contains (location))) {
				foreach (string ns in u.Namespaces) {
					if (dom.SearchType (unit, unit.GetTypeAt (location), unit.GetMemberAt (location), ns + "." + name) != null) {
						result.GenerateUsing = false;
						result.InsertNamespace = true;
						return result;
					}
				}
			}
			return result;
		}
	}
		
	class ImportSymbolCompletionData : CompletionData
	{
		TextEditorData data;
		IType type;
		Ambience ambience;
		ICompilationUnit unit;
		ProjectDom dom;
		MonoDevelop.Ide.Gui.Document doc;
		ImportSymbolCache cache;
		
		public IType Type {
			get { return this.type; }
		}
		
		public ImportSymbolCompletionData (MonoDevelop.Ide.Gui.Document doc, ImportSymbolCache cache, ProjectDom dom, IType type)
		{
			this.doc = doc;
			this.cache = cache;
			this.dom = dom;
			this.data = doc.Editor;
			this.ambience = doc.Project != null ? doc.Project.Ambience : AmbienceService.GetAmbienceForFile (doc.FileName);
			this.type = type;
			this.unit = doc.CompilationUnit;
		}
		
		bool initialized = false;
		bool generateUsing, insertNamespace;
		
		void Initialize ()
		{
			if (initialized)
				return;
			initialized = true;
			if (string.IsNullOrEmpty (type.Namespace)) 
				return;
			var result = cache.GetResult (dom, unit, type, data);
			generateUsing = result.GenerateUsing;
			insertNamespace = result.InsertNamespace;
		}
		
		#region IActionCompletionData implementation
		public override void InsertCompletionText (CompletionListWindow window)
		{
			Initialize ();
			string text = insertNamespace ? type.Namespace + "." + type.Name : type.Name;
			if (text != GetCurrentWord (window)) {
				if (window.WasShiftPressed && generateUsing) 
					text = type.Namespace + "." + text;
				window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, GetCurrentWord (window), text);
			}
			
			if (!window.WasShiftPressed && generateUsing) {
				CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
				refactorer.AddGlobalNamespaceImport (dom, data.Document.FileName, type.Namespace);
				// add using to compilation unit (this way the using is valid before the parser thread updates)
				((MonoDevelop.Projects.Dom.CompilationUnit)doc.CompilationUnit).AddAtTop (new DomUsing (new DomRegion (1, 1, int.MaxValue, 1), type.Namespace));
			}
		}
		#endregion
		
		#region ICompletionData implementation
		public override IconId Icon {
			get {
				return type.StockIcon;
			}
		}
		string displayText = null;
		public override string DisplayText {
			get {
				if (displayText == null)
					displayText = ambience.GetString (type, OutputFlags.IncludeGenerics);
				return displayText;
			}
		}
		
		string displayDescription = null;
		public override string DisplayDescription {
			get {
				if (displayDescription == null) {
					Initialize ();
					if (generateUsing || insertNamespace) {
						displayDescription = string.Format (GettextCatalog.GetString ("(from '{0}')"), type.Namespace);
					} else {
						displayDescription = "";
					}
				}
				return displayDescription;
			}
		}
		
		public override string Description {
			get {
				Initialize ();
				if (generateUsing)
					return string.Format (GettextCatalog.GetString ("Add namespace import '{0}'"), type.Namespace);
				return null;
			}
		}
		
		public override string CompletionText {
			get {
				return type.Name;
			}
		}
		#endregion
	}
	
	public class ImportSymbolHandler: CommandHandler
	{	
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.CompilationUnit == null)
				return;
			ITextEditorExtension ext = doc.EditorExtension;
			while (ext != null && !(ext is CompletionTextEditorExtension))
				ext = ext.Next;
			if (ext == null)
				return;
		
			ProjectDom dom = doc.Dom;
			ImportSymbolCache cache = new ImportSymbolCache ();
			
			List<ImportSymbolCompletionData> typeList = new List<ImportSymbolCompletionData> ();
			foreach (IType type in dom.Types) {
				typeList.Add (new ImportSymbolCompletionData (doc, cache, dom, type));
			}
			foreach (var refDom in dom.References) {
				foreach (IType type in refDom.Types) {
					typeList.Add (new ImportSymbolCompletionData (doc, cache, dom, type));
				}
			}
			
			typeList.Sort (delegate (ImportSymbolCompletionData left, ImportSymbolCompletionData right) {
				return left.Type.Name.CompareTo (right.Type.Name);
			});
			
			
			CompletionDataList completionList = new CompletionDataList ();
			completionList.IsSorted = true;
			typeList.ForEach (cd => completionList.Add (cd));
			
			((CompletionTextEditorExtension)ext).ShowCompletion (completionList);
		}
	}
}
