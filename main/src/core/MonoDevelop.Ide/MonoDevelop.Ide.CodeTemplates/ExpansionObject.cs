// 
// ExpansionObject.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;

namespace MonoDevelop.Ide.CodeTemplates
{
	public class TemplateContext
	{
		public CodeTemplate Template {
			get;
			set;
		}
		public ProjectDom ProjectDom {
			get;
			set;
		}
		
		public ParsedDocument ParsedDocument {
			get;
			set;
		}
		
		public DomLocation InsertPosition {
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
			IType type = CurrentContext.ParsedDocument.CompilationUnit.GetTypeAt (CurrentContext.InsertPosition.Line, CurrentContext.InsertPosition.Column);
			if (type == null)
				return null;
			return type.Name;
		}
		
		public string GetLengthProperty (Func<string, string> callback, string varName)
		{
			if (callback == null)
				return "Count";
			
			string var = callback (varName);
			
			ITextEditorResolver textEditorResolver = CurrentContext.Document.GetContent <ITextEditorResolver> ();
			if (textEditorResolver != null) {
				ResolveResult result = textEditorResolver.GetLanguageItem (CurrentContext.Document.TextEditor.GetPositionFromLineColumn (CurrentContext.InsertPosition.Line, CurrentContext.InsertPosition.Column), var);
				if (result != null && result.ResolvedType.ArrayDimensions > 0)
					return "Length";
			}
			return "Count";
		}
		
		public string GetComponentTypeOf (Func<string, string> callback, string varName)
		{
			if (callback == null)
				return "var";
			
			string var = callback (varName);
			ITextEditorResolver textEditorResolver = CurrentContext.Document.GetContent <ITextEditorResolver> ();
			if (textEditorResolver != null) {
				ResolveResult result =  textEditorResolver.GetLanguageItem (CurrentContext.Document.TextEditor.CursorPosition, var);
				if (result != null) {
					IReturnType componentType =  DomType.GetComponentType (CurrentContext.ProjectDom, result.ResolvedType);
					if (componentType != null) {
						Ambience ambience = AmbienceService.GetAmbience (CurrentContext.Template.MimeType);
						return ambience != null ? ambience.GetString (componentType, OutputFlags.None) :  componentType.ToInvariantString ();
					}
				}
			}

			return "var";
		}
		MonoDevelop.Projects.Gui.Completion.ICompletionDataList list;
		public MonoDevelop.TextEditor.PopupWindow.IListDataProvider GetCollections ()
		{
			List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>> ();
			CompletionTextEditorExtension ext = CurrentContext.Document.GetContent <CompletionTextEditorExtension> ();
			if (ext != null) {
				if (list == null)
					list = ext.CodeCompletionCommand (CurrentContext.Document.TextEditor.CurrentCodeCompletionContext);
				
				Console.WriteLine ("get collections: " + list.Count);
				foreach (object o in list) {
					MonoDevelop.Projects.Gui.Completion.IMemberCompletionData data = o as MonoDevelop.Projects.Gui.Completion.IMemberCompletionData;
					if (data == null)
						continue;
					
					if (data.Member is IMember) {
						IMember m = data.Member as IMember;
						if (DomType.GetComponentType (CurrentContext.ProjectDom, m.ReturnType) != null)
							result.Add (new KeyValuePair<string, string>(data.Icon, m.Name));
					}
				}
				
				foreach (object o in list) {
					MonoDevelop.Projects.Gui.Completion.IMemberCompletionData data = o as MonoDevelop.Projects.Gui.Completion.IMemberCompletionData;
					if (data == null)
						continue;
					if (data.Member is IParameter) {
						IParameter m = data.Member as IParameter;
						if (DomType.GetComponentType (CurrentContext.ProjectDom, m.ReturnType) != null)
							result.Add (new KeyValuePair<string, string>(data.Icon, m.Name));
					}
				}
				
				foreach (object o in list) {
					MonoDevelop.Projects.Gui.Completion.IMemberCompletionData data = o as MonoDevelop.Projects.Gui.Completion.IMemberCompletionData;
					if (data == null)
						continue;
					if (data.Member is LocalVariable) {
						LocalVariable m = data.Member as LocalVariable;
						if (DomType.GetComponentType (CurrentContext.ProjectDom, m.ReturnType) != null)
							result.Add (new KeyValuePair<string, string>(data.Icon, m.Name));
					}
				}
			}
			return new CodeTemplateListDataProvider (result);
		}
		
		public string GetSimpleTypeName (string fullTypeName)
		{
			IType foundType = null;
			
			string curType = fullTypeName;
			while (foundType == null) {
				foundType = CurrentContext.ProjectDom.GetType (curType);
				int idx = curType.LastIndexOf ('.');
				if (idx < 0)
					break; 
				curType = fullTypeName.Substring (0, idx);
			}
			
			if (foundType == null) 
				foundType = new DomType (fullTypeName);
			
			foreach (IUsing u in CurrentContext.ParsedDocument.CompilationUnit.Usings) {
				foreach (string includedNamespace in u.Namespaces) {
					if (includedNamespace == foundType.Namespace)
						return fullTypeName.Substring (includedNamespace.Length + 1);
				}
			}
			return fullTypeName;
		}
		
		static Regex functionRegEx = new Regex ("([^(]*)\\(([^(]*)\\)", RegexOptions.Compiled);
		
		public virtual string[] Descriptions {
			get {
				return new string[] {
					"",
					"GetCurrentClassName()",
					"GetSimpleTypeName(\"LongName\")",
					"GetLengthProperty(\"Var\")",
					"GetComponentTypeOf(\"Var\")",
					"GetCollections()"
				};
			}
		}
		
		public virtual MonoDevelop.TextEditor.PopupWindow.IListDataProvider RunFunction (TemplateContext context, Func<string, string> callback, string function)
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
			case "GetSimpleTypeName":
				return new CodeTemplateListDataProvider (GetSimpleTypeName (match.Groups[2].Value.Trim ('"')));
			case "GetLengthProperty":
				return new CodeTemplateListDataProvider (GetLengthProperty (callback, match.Groups[2].Value.Trim ('"')));
			case "GetComponentTypeOf":
				return new CodeTemplateListDataProvider (GetComponentTypeOf (callback, match.Groups[2].Value.Trim ('"')));
			}
			return null;
		}
	}
}
