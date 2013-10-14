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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp.Resolver;

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
		
		public GenerateNamespaceImport GetResult (IUnresolvedFile unit, IType type, MonoDevelop.Ide.Gui.Document doc)
		{
			GenerateNamespaceImport result;
			if (cache.TryGetValue (type.Namespace, out result))
				return result;
			result = new GenerateNamespaceImport ();
			cache[type.Namespace] = result;
			TextEditorData data = doc.Editor;
			
			result.InsertNamespace  = false;
			var loc = new TextLocation (data.Caret.Line, data.Caret.Column);
			foreach (var ns in RefactoringOptions.GetUsedNamespaces (doc, loc)) {
				if (type.Namespace == ns) {
					result.GenerateUsing = false;
					return result;
				}
			}
			
			result.GenerateUsing = true;
			string name = type.Name;
			
			foreach (string ns in RefactoringOptions.GetUsedNamespaces (doc, loc)) {
				if (doc.Compilation.MainAssembly.GetTypeDefinition (ns, name, type.TypeParameterCount) != null) {
					result.GenerateUsing = false;
					result.InsertNamespace = true;
					return result;
				}
			}
			return result;
		}
	}
		
	class ImportSymbolCompletionData : CompletionData
	{
		IType type;
		Ambience ambience;
		ParsedDocument unit;
		MonoDevelop.Ide.Gui.Document doc;
		ImportSymbolCache cache;
		
		public IType Type {
			get { return this.type; }
		}
		
		public ImportSymbolCompletionData (MonoDevelop.Ide.Gui.Document doc, ImportSymbolCache cache, IType type)
		{
			this.doc = doc;
			this.cache = cache;
//			this.data = doc.Editor;
			this.ambience = AmbienceService.GetAmbience (doc.Editor.MimeType);
			this.type = type;
			this.unit = doc.ParsedDocument;
			this.DisplayFlags |= ICSharpCode.NRefactory.Completion.DisplayFlags.IsImportCompletion;
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
			var result = cache.GetResult (unit.ParsedFile, type, doc);
			generateUsing = result.GenerateUsing;
			insertNamespace = result.InsertNamespace;
		}
		
		#region IActionCompletionData implementation
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			Initialize ();
			using (var undo = doc.Editor.OpenUndoGroup ()) {
				string text = insertNamespace ? type.Namespace + "." + type.Name : type.Name;
				if (text != GetCurrentWord (window)) {
					if (window.WasShiftPressed && generateUsing) 
						text = type.Namespace + "." + text;
					window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, GetCurrentWord (window), text);
				}
				
				if (!window.WasShiftPressed && generateUsing) {
					var generator = CodeGenerator.CreateGenerator (doc);
					if (generator != null) {
						generator.AddGlobalNamespaceImport (doc, type.Namespace);
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
					displayDescription = string.Format (GettextCatalog.GetString ("(from '{0}')"), type.Namespace);
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
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null)
				return;
			ITextEditorExtension ext = doc.EditorExtension;
			while (ext != null && !(ext is CompletionTextEditorExtension))
				ext = ext.Next;
			if (ext == null)
				return;
		
			var dom = doc.Compilation;
			ImportSymbolCache cache = new ImportSymbolCache ();
			var lookup = new MemberLookup (null, doc.Compilation.MainAssembly);

			List<ImportSymbolCompletionData> typeList = new List<ImportSymbolCompletionData> ();
			foreach (var type in dom.GetAllTypeDefinitions ()) {
				if (!lookup.IsAccessible (type, false))
					continue;
				typeList.Add (new ImportSymbolCompletionData (doc, cache, type));
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

/*
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Ide.TypeSystem;

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
		
		public GenerateNamespaceImport GetResult (IUnresolvedFile unit, string typeNamespace, string typeName, MonoDevelop.Ide.Gui.Document doc)
		{
			GenerateNamespaceImport result;
			if (cache.TryGetValue (typeNamespace, out result))
				return result;
			result = new GenerateNamespaceImport ();
			cache[typeNamespace] = result;
			TextEditorData data = doc.Editor;
			
			result.InsertNamespace  = false;
			var loc = new TextLocation (data.Caret.Line, data.Caret.Column);
			foreach (var ns in RefactoringOptions.GetUsedNamespaces (doc, loc)) {
				if (typeNamespace == ns) {
					result.GenerateUsing = false;
					return result;
				}
			}
			
			result.GenerateUsing = true;
			string name = typeName;
			
			foreach (string ns in RefactoringOptions.GetUsedNamespaces (doc, loc)) {
				if (doc.Compilation.MainAssembly.GetTypeDefinition (ns, name, 0) != null) {
					result.GenerateUsing = false;
					result.InsertNamespace = true;
					return result;
				}
			}
			return result;
		}
	}
		
	class ImportSymbolCompletionData : CompletionData
	{
		internal readonly string typeName;
		ParsedDocument unit;
		MonoDevelop.Ide.Gui.Document doc;
		ImportSymbolCache cache;
		TypeKind kind;
		Lazy<FrameworkLookup.AssemblyLookup> lookup;
		MonoDevelop.Projects.ProjectReference reference;
		public ImportSymbolCompletionData (MonoDevelop.Ide.Gui.Document doc, ImportSymbolCache cache, string typeName, TypeKind kind, Lazy<FrameworkLookup.AssemblyLookup> lookup, MonoDevelop.Projects.ProjectReference reference)
		{
			this.doc = doc;
			this.cache = cache;
			this.unit = doc.ParsedDocument;
			this.kind = kind;
			this.typeName = typeName;
			this.lookup = lookup;
			this.reference = reference;
		}
		
		bool initialized = false;
		bool generateReference;
		bool generateUsing;
		bool insertNamespace;
		
		void Initialize ()
		{
			if (initialized)
				return;
			initialized = true;
			var netProject = (DotNetProject)doc.Project;
			generateReference = true;
			foreach (var r in netProject.References) {
				if (r.Equals (reference)) {
					generateReference = false;
					break;
				}
			}
			var result = cache.GetResult (unit.ParsedFile, lookup.Value.Namespace, typeName, doc);
			generateUsing = result.GenerateUsing;
			insertNamespace = result.InsertNamespace;
		}
		
		#region IActionCompletionData implementation
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			Initialize ();
			string text = insertNamespace ? lookup.Value.Namespace + "." + typeName : typeName;
			if (text != GetCurrentWord (window)) {
				if (window.WasShiftPressed && generateReference) 
					text = lookup.Value.Namespace + "." + text;
				window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, GetCurrentWord (window), text);
			}

			if (generateReference) {
				var project = doc.Project;
				project.Items.Add (reference);
				IdeApp.ProjectOperations.Save (project);
			}

			if (!window.WasShiftPressed && generateUsing) {
				var generator = CodeGenerator.CreateGenerator (doc);
				if (generator != null) {
					generator.AddGlobalNamespaceImport (doc, lookup.Value.Namespace);
					// reparse
					doc.UpdateParseDocument ();
				}
			}
		}
		#endregion
		
		#region ICompletionData implementation
		public override IconId Icon {
			get {
				switch (kind) {
				case TypeKind.Delegate:
					return MonoDevelop.Ide.Gui.Stock.Delegate;
				case TypeKind.Struct:
					return MonoDevelop.Ide.Gui.Stock.Struct;
				case TypeKind.Interface:
					return MonoDevelop.Ide.Gui.Stock.Interface;
				case TypeKind.Enum:
					return MonoDevelop.Ide.Gui.Stock.Enum;
				default:
					return MonoDevelop.Ide.Gui.Stock.Class;
				}
			}
		}

		public override string DisplayText {
			get {
				return typeName;
			}
		}
		
		string displayDescription = null;
		public override string DisplayDescription {
			get {
				if (displayDescription == null) {
					Initialize ();
					if (generateReference) {
						displayDescription = string.Format (GettextCatalog.GetString ("(reference '{0}')"), reference.Reference);
					} else if (generateUsing) {
						displayDescription = string.Format (GettextCatalog.GetString ("(from '{0}')"), lookup.Value.Namespace);
					} else {
						displayDescription = "";
					}
				}
				return displayDescription;
			}
		}
		
		public override string Description {
			get {
				return DisplayDescription;
			}
		}
		
		public override string CompletionText {
			get {
				return typeName;
			}
		}
		#endregion
	}
	
	public class ImportSymbolHandler: CommandHandler
	{	
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null)
				return;
			ITextEditorExtension ext = doc.EditorExtension;
			while (ext != null && !(ext is CompletionTextEditorExtension))
				ext = ext.Next;
			if (ext == null)
				return;

			var dom = doc.Compilation;
			ImportSymbolCache cache = new ImportSymbolCache ();
			List<ImportSymbolCompletionData> typeList = new List<ImportSymbolCompletionData> ();
			Dictionary<string, MonoDevelop.Projects.ProjectReference> referenceCache = new Dictionary<string, MonoDevelop.Projects.ProjectReference> ();
			var netProject = (DotNetProject)doc.Project;
			foreach (var type in TypeSystemService.GetFrameworkLookup (netProject).GetAllTypes ()) {
				var r = type.Item3.Value;
				MonoDevelop.Projects.ProjectReference reference;
				if (!referenceCache.TryGetValue (r.FullName, out reference)) {
					var systemAssembly = netProject.AssemblyContext.GetAssemblyFromFullName (r.FullName, r.Package, netProject.TargetFramework);
					if (systemAssembly == null) {
						reference = null;
					} else {
						reference = new MonoDevelop.Projects.ProjectReference (systemAssembly);
					}
					referenceCache [r.FullName] = reference;
				}
				if (reference == null)
					continue;
				typeList.Add (new ImportSymbolCompletionData (doc, cache, type.Item2, type.Item1, type.Item3, reference));
			}

			typeList.Sort (delegate (ImportSymbolCompletionData left, ImportSymbolCompletionData right) {
				return left.typeName.CompareTo (right.typeName);
			});

			
			var completionList = new CompletionDataList ();
			completionList.IsSorted = true;

			typeList.ForEach (cd => completionList.Add (cd));

			((CompletionTextEditorExtension)ext).ShowCompletion (completionList);
		}
	}
}
 * */
