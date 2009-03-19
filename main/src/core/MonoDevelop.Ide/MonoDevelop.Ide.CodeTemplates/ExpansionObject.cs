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

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Ide.CodeTemplates
{
	public class TemplateContext
	{
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
		
		public string GetLengthProperty (string varName)
		{
			return "Length";
		}
		
		public string GetComponentTypeOf (string varName)
		{
			return "object";
		}
		
		public string GetCollections ()
		{
			return "cols";
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
		
		public virtual IEnumerable<string> Descriptions {
			get {
				return new string[] {
					"GetCurrentClassName()",
					"GetSimpleTypeName(\"LongName\")",
					"GetLengthProperty(\"Var\")",
					"GetComponentTypeOf(\"Var\")",
					"GetCollections()"
				};
			}
		}
		
		public virtual string RunFunction (TemplateContext context, string function)
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
				return GetCurrentClassName ();
			case "GetSimpleTypeName":
				return GetSimpleTypeName (match.Groups[2].Value.Trim ('"'));
			case "GetLengthProperty":
				return GetLengthProperty (match.Groups[2].Value.Trim ('"'));
			case "GetComponentTypeOf":
				return GetComponentTypeOf (match.Groups[2].Value.Trim ('"'));
			}
			return null;
		}
	}
}
