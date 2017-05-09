// 
// ExpansionObject.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Text.RegularExpressions;

using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Tasks;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using System.Linq;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using System.Web.SessionState;

namespace MonoDevelop.Ide.CodeTemplates
{
	public class TemplateContext
	{
		public CodeTemplate Template {
			get;
			set;
		}
		
		public SemanticModel Compilation {
			get {
				var analysisDocument = DocumentContext.ParsedDocument;
				if (analysisDocument == null)
					return null;
				return analysisDocument.GetAst<SemanticModel> ();
			}
		}

		public DocumentLocation InsertPosition {
			get;
			set;
		}
		
		public string SelectedText {
			get;
			set;
		}
		
		public string TemplateCode {
			get;
			set;
		}
		
		public string LineIndent {
			get;
			set;
		}
		
		public DocumentContext DocumentContext {
			get;
			set;
		}

		public TextEditor Editor {
			get;
			set;
		}
	}
	
	public class ExpansionObject
	{
		public TemplateContext CurrentContext {
			get;
			set;
		}
		
		public string GetCurrentClassName ()
		{
			var compilation = CurrentContext.Compilation;
			if (compilation == null)
				return null;
			var enclosingSymbol = compilation.GetEnclosingSymbol (CurrentContext.Editor.CaretOffset);

			if (!(enclosingSymbol is ITypeSymbol))
				enclosingSymbol = enclosingSymbol.ContainingType;

			return enclosingSymbol != null ? enclosingSymbol.Name : null;
		}
		
		public string GetConstructorModifier ()
		{
			var compilation = CurrentContext.Compilation;
			if (compilation == null)
				return null;
			var enclosingSymbol = compilation.GetEnclosingSymbol (CurrentContext.Editor.CaretOffset);

			if (!(enclosingSymbol is ITypeSymbol))
				enclosingSymbol = enclosingSymbol.ContainingType;

			return enclosingSymbol != null && enclosingSymbol.IsStatic ? "static " : "public ";
		}
		
		public string GetLengthProperty (Func<string, string> callback, string varName)
		{
			SemanticModel semanticModel;
			if (!CurrentContext.DocumentContext.AnalysisDocument.TryGetSemanticModel (out semanticModel))
				semanticModel = CurrentContext.Compilation;
			if (callback == null || semanticModel == null)
				return "Count";
			string var = callback (varName);
			var offset = CurrentContext.Editor.CaretOffset;
			var sym = semanticModel.LookupSymbols (offset, name: var).FirstOrDefault ();
			if (sym == null)
				return "Count";
			var returnType = sym.GetReturnType ();
			if (returnType?.Kind == SymbolKind.ArrayType)
				return "Length";
			return "Count";
		}
		
		ITypeSymbol GetElementType (Compilation compilation, ITypeSymbol type)
		{
			ITypeSymbol tmp = null;
			foreach (var baseType in type.AllInterfaces) {
				if (baseType != null && baseType.Name == "IEnumerable") {
					if (baseType.TypeArguments.Length > 0) {
						return baseType.TypeArguments [0];
					} else if (baseType.ContainingNamespace.ToDisplayString (Ambience.LabelFormat) == "System.Collections") {
						tmp = compilation.GetSpecialType (SpecialType.System_Object);
					}
				}
			}
			return tmp;
		}
		
		public string GetComponentTypeOf (Func<string, string> callback, string varName)
		{
			//if (callback == null)
			return "var";
			/*var compilation = CurrentContext.Compilation;
			if (compilation == null)
				return null;
		
			string var = callback (varName);

			var offset = CurrentContext.Editor.CaretOffset;
			var sym = compilation.LookupSymbols (offset, name: var).FirstOrDefault ();
			if (sym == null)
				return "var";
			var rt = sym.GetReturnType ();
			if (rt != null)
				return rt.ToMinimalDisplayString (compilation, offset);
			return "var";*/
		}

		ICompletionDataList list;
		public IListDataProvider<string> GetCollections ()
		{
			var result = new List<CodeTemplateVariableValue> ();
			var ext = CurrentContext.DocumentContext.GetContent <CompletionTextEditorExtension> ();
			var analysisProject = TypeSystemService.GetCodeAnalysisProject (CurrentContext.DocumentContext.Project);
			var compilation = analysisProject != null ? analysisProject.GetCompilationAsync ().Result : null;

			if (ext != null) {
				if (list == null)
					list = ext.HandleCodeCompletionAsync (
						CurrentContext.DocumentContext.GetContent <MonoDevelop.Ide.CodeCompletion.ICompletionWidget> ().CurrentCodeCompletionContext,
						CompletionTriggerInfo.CodeCompletionCommand).Result;
				
				foreach (var data in list.OfType<ISymbolCompletionData> ()) {
					if (data.Symbol == null)
						continue;
					var type = data.Symbol.GetReturnType ();
					if (type == null)
						continue;
					if (GetElementType (compilation, type) != null) {
						var method = data as IMethodSymbol;
						if (method != null) {
							if (method.Parameters.Length == 0)
								result.Add (new CodeTemplateVariableValue (data.Symbol.Name + " ()", ((CompletionData)data).Icon));
							continue;
						}
						if (!result.Any (r => r.Text == data.Symbol.Name))
							result.Add (new CodeTemplateVariableValue (data.Symbol.Name, ((CompletionData)data).Icon));
					}
				}
				
				foreach (var data in list.OfType<ISymbolCompletionData> ()) {
					var m = data.Symbol as IParameterSymbol;
					if (m != null) {
						if (GetElementType (compilation, m.Type) != null && !result.Any (r => r.Text == m.Name))
							result.Add (new CodeTemplateVariableValue (m.Name, ((CompletionData)data).Icon));
					}
				}
				
				foreach (var sym in list.OfType<ISymbolCompletionData> ()) {
					var m = sym.Symbol as ILocalSymbol;
					if (m == null)
						continue;
					if (GetElementType (compilation, m.Type) != null && !result.Any (r => r.Text == m.Name))
						result.Add (new CodeTemplateVariableValue (m.Name, ((CompletionData)sym).Icon));
				}
			}
			return new CodeTemplateListDataProvider (result);
		}
		
		public string GetSimpleTypeName (string fullTypeName)
		{
			var compilation = CurrentContext.Compilation;
			if (compilation == null)
				return fullTypeName.Replace ("#", ".");
			string ns = "";
			string name = "";
			string member = "";
			
			int idx = fullTypeName.IndexOf ('#');
			if (idx < 0) {
				name = fullTypeName;
			} else {
				ns = fullTypeName.Substring (0, idx);
				name = fullTypeName.Substring (idx + 1);
			}
			
			idx = name.IndexOf ('.');
			if (idx >= 0) {
				member = name.Substring (idx + 1);
				name = name.Substring (0, idx);
			}

			var metadataName = string.IsNullOrEmpty (ns) ? name : ns + "." + name;
			var type = compilation.Compilation.GetTypeByMetadataName (metadataName);
			if (type != null) {
				var minimalName = type.ToMinimalDisplayString (compilation, CurrentContext.Editor.CaretOffset);
				return string.IsNullOrEmpty (member) ? minimalName :  minimalName + "." + member;
			}
			return fullTypeName.Replace ("#", ".");
		}
		

		static System.Text.RegularExpressions.Regex functionRegEx = new System.Text.RegularExpressions.Regex ("([^(]*)\\(([^(]*)\\)", RegexOptions.Compiled);
		
		
		// We should use reflection here (but for 5 functions it doesn't hurt) !!! - Mike
		public virtual string[] Descriptions {
			get {
				return new string[] {
					"",
					"GetCurrentClassName()",
					"GetConstructorModifier()",
					"GetSimpleTypeName(\"LongName\")",
					"GetLengthProperty(\"Var\")",
					"GetComponentTypeOf(\"Var\")",
					"GetCollections()"
				};
			}
		}
		
		public virtual IListDataProvider<string> RunFunction (TemplateContext context, Func<string, string> callback, string function)
		{
			this.CurrentContext = context;
			var match = functionRegEx.Match (function);
			if (!match.Success)
				return null;
			string name = match.Groups[1].Value;
			switch (name) {
			case "GetCollections":
				return GetCollections ();
			case "GetCurrentClassName":
				return new CodeTemplateListDataProvider (GetCurrentClassName ());
			case "GetConstructorModifier":
				return new CodeTemplateListDataProvider (GetConstructorModifier ());
			case "GetSimpleTypeName":
				return new CodeTemplateListDataProvider (GetSimpleTypeName (match.Groups[2].Value.Trim ('"')));
			case "GetLengthProperty":
				return new CodeTemplateListDataProvider (GetLengthProperty (callback, match.Groups == null || match.Groups.Count < 3 ? null : match.Groups[2].Value.Trim ('"')));
			case "GetComponentTypeOf":
				return new CodeTemplateListDataProvider (GetComponentTypeOf (callback, match.Groups[2].Value.Trim ('"')));
			}
			return null;
		}
	}
}
