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
using System.Text;
using System.Text.RegularExpressions;

using MonoDevelop.Ide.Gui.Content;
using Mono.TextEditor.PopupWindow;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Completion;
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace MonoDevelop.Ide.CodeTemplates
{
	public class TemplateContext
	{
		public CodeTemplate Template {
			get;
			set;
		}
		
		public ICompilation Compilation {
			get {
				return Document.Compilation;
			}
		}
		
		public IUnresolvedFile ParsedDocument {
			get;
			set;
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
		
		public MonoDevelop.Ide.Gui.Document Document {
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
			if (CurrentContext.ParsedDocument == null)
				return null;
			IUnresolvedTypeDefinition type = null;
			var provider = CurrentContext.Document.GetContent<ITextEditorMemberPositionProvider>();
			if (provider == null) {
				type = CurrentContext.ParsedDocument.GetInnermostTypeDefinition (CurrentContext.InsertPosition.Line, CurrentContext.InsertPosition.Column);
			} else {
				type = provider.GetTypeAt (CurrentContext.Document.Editor.LocationToOffset (CurrentContext.InsertPosition));
			}
			
			if (type == null)
				return null;
			return type.Name;
		}
		
		public string GetConstructorModifier ()
		{
			if (CurrentContext.ParsedDocument == null)
				return null;
			IUnresolvedTypeDefinition type = null;
			var provider = CurrentContext.Document.GetContent<ITextEditorMemberPositionProvider>();
			if (provider == null) {
				type = CurrentContext.ParsedDocument.GetInnermostTypeDefinition (CurrentContext.InsertPosition.Line, CurrentContext.InsertPosition.Column);
			} else {
				type = provider.GetTypeAt (CurrentContext.Document.Editor.LocationToOffset (CurrentContext.InsertPosition));
			}
			
			if (type == null)
				return "";
			return type.IsStatic ? "static " : "public ";
		}
		
		public string GetLengthProperty (Func<string, string> callback, string varName)
		{
			if (callback == null)
				return "Count";
			
			string var = callback (varName);
			
			ITextEditorResolver textEditorResolver = CurrentContext.Document.GetContent <ITextEditorResolver> ();
			if (textEditorResolver != null) {
				var result = textEditorResolver.GetLanguageItem (CurrentContext.Document.Editor.Document.LocationToOffset (CurrentContext.InsertPosition), var);
				if (result.Type.IsReferenceType.HasValue && !result.Type.IsReferenceType.Value)
					return "Length";
			}
			return "Count";
		}
		
		IType GetElementType (IType result)
		{
			foreach (var baseType in result.GetAllBaseTypes ()) {
				var baseTypeDef = baseType.GetDefinition();
				if (baseTypeDef != null && baseTypeDef.Name == "IEnumerable") {
					if (baseTypeDef.Namespace == "System.Collections.Generic" && baseTypeDef.TypeParameterCount == 1) {
						if (baseType.TypeArguments.Count > 0)
							return baseType.TypeArguments[0];
					} else if (baseTypeDef.Namespace == "System.Collections" && baseTypeDef.TypeParameterCount == 0) {
						return CurrentContext.Compilation.FindType (KnownTypeCode.Object);
					}
				}
			}
			return new UnknownType ("", "", 0);
		}
		
		
		public string GetComponentTypeOf (Func<string, string> callback, string varName)
		{
			if (callback == null)
				return "var";
			
			string var = callback (varName);
			ITextEditorResolver textEditorResolver = CurrentContext.Document.GetContent <ITextEditorResolver> ();
			if (textEditorResolver != null) {
				var result = textEditorResolver.GetLanguageItem (CurrentContext.Document.Editor.Caret.Offset, var);
				if (result != null) {
					var componentType = GetElementType (result.Type);
					if (componentType.Kind != TypeKind.Unknown) {
						var generator = CodeGenerator.CreateGenerator (CurrentContext.Document);
						if (generator != null)
							return generator.GetShortTypeString (CurrentContext.Document, componentType);
					}
				}
			}
			
			return "var";
		}
		MonoDevelop.Ide.CodeCompletion.ICompletionDataList list;
		public IListDataProvider<string> GetCollections ()
		{
			var result = new List<CodeTemplateVariableValue> ();
			var ext = CurrentContext.Document.GetContent <CompletionTextEditorExtension> ();
			if (ext != null) {
				if (list == null)
					list = ext.CodeCompletionCommand (
						CurrentContext.Document.GetContent <MonoDevelop.Ide.CodeCompletion.ICompletionWidget> ().CurrentCodeCompletionContext);
				
				foreach (object o in list) {
					var data = o as IEntityCompletionData;
					if (data == null)
						continue;
					
					if (data.Entity is IMember) {
						var m = data.Entity as IMember;
						if (GetElementType (m.ReturnType).Kind != TypeKind.Unknown) {
							if (m is IMethod) {
								if (((IMethod)m).Parameters.Count == 0)
									result.Add (new CodeTemplateVariableValue (m.Name + " ()", ((CompletionData)data).Icon));
								continue;
							}

							result.Add (new CodeTemplateVariableValue (m.Name, ((CompletionData)data).Icon));
						}
					}
				}
				
				foreach (object o in list) {
					var data = o as IEntityCompletionData;
					if (data == null)
						continue;
					if (data.Entity is IParameter) {
						var m = data.Entity as IParameter;
						if (GetElementType (m.Type).Kind != TypeKind.Unknown)
							result.Add (new CodeTemplateVariableValue (m.Name, ((CompletionData)data).Icon));
					}
				}
				
				foreach (object o in list) {
					var data = o as IVariableCompletionData;
					if (data == null)
						continue;
					var m = data.Variable;
					if (GetElementType (m.Type).Kind != TypeKind.Unknown)
						result.Add (new CodeTemplateVariableValue (m.Name, ((CompletionData)data).Icon));
				}
			}
			return new CodeTemplateListDataProvider (result);
		}
		
		public string GetSimpleTypeName (string fullTypeName)
		{
			if (CurrentContext.ParsedDocument == null)
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
				member = name.Substring (idx);
				name = name.Substring (0, idx);
			}

			var type = new GetClassTypeReference (ns, name, 0).Resolve (
				CurrentContext.Document.ParsedDocument.GetTypeResolveContext (
				CurrentContext.Document.Compilation, 
				CurrentContext.Document.Editor.Caret.Location
				)
			);
			bool stripAttribute = false;
			if (type == null || type.Kind == TypeKind.Unknown) {
				type = new GetClassTypeReference (ns, name + "Attribute", 0).Resolve (
					CurrentContext.Document.ParsedDocument.GetTypeResolveContext (
					CurrentContext.Document.Compilation, 
					CurrentContext.Document.Editor.Caret.Location
					)
				);	
				stripAttribute = true;
			}
			if (type == null || type.Kind == TypeKind.Unknown)
				return fullTypeName.Replace ("#", ".");
			var generator = CodeGenerator.CreateGenerator (CurrentContext.Document);
			if (generator != null) {
				var result = generator.GetShortTypeString (CurrentContext.Document, type) + member;
				if (stripAttribute && result.EndsWith ("Attribute", StringComparison.Ordinal))
				    result = result.Substring (0, result.Length - "Attribute".Length);
				return result;
			}
			return fullTypeName.Replace ("#", ".");
		}
		
		static Regex functionRegEx = new Regex ("([^(]*)\\(([^(]*)\\)", RegexOptions.Compiled);
		
		
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
			Match match = functionRegEx.Match (function);
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
