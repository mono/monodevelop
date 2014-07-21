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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Refactoring
{
	class GenerateNamespaceImport
	{
		public bool GenerateUsing { get; set; }
		public bool InsertNamespace { get; set; }
	}
	
	class ImportSymbolCache
	{
		SemanticModel semanticModel;

		Dictionary<INamespaceSymbol, GenerateNamespaceImport> cache = new Dictionary<INamespaceSymbol, GenerateNamespaceImport> ();

		public ImportSymbolCache (SemanticModel semanticModel)
		{
			this.semanticModel = semanticModel;
		}

		public static string GetNamespaceString (INamespaceSymbol ns)
		{
			if (ns == null || ns.IsGlobalNamespace)
				return "";
			var c = GetNamespaceString (ns.ContainingNamespace);
			if (string.IsNullOrEmpty (c))
				return ns.Name;
			return c + "." + ns.Name;
		}
		
		public GenerateNamespaceImport GetResult (INamedTypeSymbol type, MonoDevelop.Ide.Gui.Document doc)
		{
			GenerateNamespaceImport result;
			if (cache.TryGetValue (type.ContainingNamespace, out result))
				return result;
			result = new GenerateNamespaceImport ();
			cache[type.ContainingNamespace] = result;
			var data = doc.Editor;
			result.InsertNamespace  = false;
			var nameSpaces = RefactoringOptions.GetUsedNamespacesAsync (doc.Editor, doc, data.CaretOffset).Result;
			foreach (var ns in nameSpaces) {
				if (GetNamespaceString (type.ContainingNamespace) == ns) {

					result.GenerateUsing = false;
					return result;
				}
			}
			result.GenerateUsing = true;
			
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return result;
			var model = analysisDocument.GetSemanticModelAsync ().Result;
			
			return result;
		}
	}
		
	class ImportSymbolCompletionData : CompletionData
	{
		INamedTypeSymbol type;
		Ambience ambience;
		MonoDevelop.Ide.Gui.Document doc;
		ImportSymbolCache cache;
		
		public INamedTypeSymbol Type {
			get { return this.type; }
		}
		
		public ImportSymbolCompletionData (MonoDevelop.Ide.Gui.Document doc, ImportSymbolCache cache, INamedTypeSymbol type)
		{
			this.doc = doc;
			this.cache = cache;
//			this.data = doc.Editor;
			this.ambience = AmbienceService.GetAmbience (doc.Editor.MimeType);
			this.type = type;
			this.DisplayFlags |= ICSharpCode.NRefactory6.CSharp.Completion.DisplayFlags.IsImportCompletion;
		}
		
		bool initialized = false;
		bool generateUsing, insertNamespace;
		
		void Initialize ()
		{
			if (initialized)
				return;
			initialized = true;
			if (type.ContainingNamespace.IsGlobalNamespace) 
				return;
			var result = cache.GetResult (type, doc);
			generateUsing = result.GenerateUsing;
			insertNamespace = result.InsertNamespace;
		}
		
		#region IActionCompletionData implementation
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			Initialize ();
			using (var undo = doc.Editor.OpenUndoGroup ()) {
				string text = insertNamespace ? ImportSymbolCache.GetNamespaceString (type.ContainingNamespace) + "." + type.Name : type.Name;
				if (text != GetCurrentWord (window)) {
					if (window.WasShiftPressed && generateUsing) 
						text = ImportSymbolCache.GetNamespaceString (type.ContainingNamespace) + "." + text;
					window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, GetCurrentWord (window), text);
				}
				
				if (!window.WasShiftPressed && generateUsing) {
					var generator = CodeGenerator.CreateGenerator (doc);
					if (generator != null) {
						generator.AddGlobalNamespaceImport (doc, ImportSymbolCache.GetNamespaceString (type.ContainingNamespace));
						// reparse
						doc.UpdateParseDocument ();
					}
				}
			}
			ka |= KeyActions.Ignore;
		}
		#endregion
		
		#region ICompletionData implementation
		public override IconId Icon {
			get {
				return type.GetStockIcon ();
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
		
		static string GetDefaultDisplaySelection (string description, bool isSelected)
		{
			if (!isSelected)
				return "<span foreground=\"darkgray\">" + description + "</span>";
			return description;
		}

		string displayDescription = null;
		public override string GetDisplayDescription (bool isSelected)
		{
			if (displayDescription == null) {
				Initialize ();
				if (generateUsing || insertNamespace) {
					displayDescription = string.Format (GettextCatalog.GetString ("(from '{0}')"), type.ContainingNamespace.Name);
				} else {
					displayDescription = "";
				}
			}
			return GetDefaultDisplaySelection (displayDescription, isSelected);
		}

		
		public override string Description {
			get {
				Initialize ();
				if (generateUsing)
					return string.Format (GettextCatalog.GetString ("Add namespace import '{0}'"), type.ContainingNamespace.Name);
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
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return;
			var completionExt = doc.GetContent<CompletionTextEditorExtension> ();
			if (completionExt == null)
				return;
			
			var semanticModel = analysisDocument.GetSemanticModelAsync ().Result; 
			
			var cache = new ImportSymbolCache (semanticModel);

			var typeList = new List<ImportSymbolCompletionData> ();
			var stack = new Stack<INamespaceSymbol> ();
			stack.Push (semanticModel.Compilation.GlobalNamespace);
			
			while (stack.Count > 0) {
				var curNs = stack.Pop ();
				foreach (var type in curNs.GetTypeMembers ()) {
					if (!type.CanBeReferencedByName)
						continue;
					typeList.Add (new ImportSymbolCompletionData (doc, cache, type));
				}

				foreach (var childNs in curNs.GetNamespaceMembers ())
					stack.Push (childNs);
			}
			
			typeList.Sort (delegate (ImportSymbolCompletionData left, ImportSymbolCompletionData right) {
				return left.Type.Name.CompareTo (right.Type.Name);
			});
			
			
			var completionList = new CompletionDataList ();
			completionList.IsSorted = true;
			typeList.ForEach (completionList.Add);
			
			completionExt.ShowCompletion (completionList);
		}
	}
}
