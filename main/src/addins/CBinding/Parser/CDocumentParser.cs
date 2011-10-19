// 
// CDocumentParser.cs
//  
// Author:
//       Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com>
// 
// Copyright (c) 2009 Levi Bard
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
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace CBinding.Parser
{
	/// <summary>
	/// Ctags-based document parser helper
	/// </summary>
	public class CDocumentParser:  AbstractTypeSystemParser
	{
		public override ParsedDocument Parse (IProjectContent dom, bool storeAst, string fileName, TextReader reader)
		{
			var doc = new DefaultParsedDocument (fileName);
			doc.Flags |= ParsedDocumentFlags.NonSerializable;
			Project p = (null == dom || null == dom.GetProject ())? 
				IdeApp.Workspace.GetProjectContainingFile (fileName):
				dom.GetProject ();
			ProjectInformation pi = ProjectInformationManager.Instance.Get (p);
			
			string content = reader.ReadToEnd ();
			string[] contentLines = content.Split (new string[]{Environment.NewLine}, StringSplitOptions.None);
			
			var globals = new DefaultTypeDefinition (dom, "", GettextCatalog.GetString ("(Global Scope)"));
			lock (pi) {
				// Add containers to type list
				foreach (LanguageItem li in pi.Containers ()) {
					if (null == li.Parent && FilePath.Equals (li.File, fileName)) {
						var tmp = AddLanguageItem (dom, pi, globals, li, contentLines)  as ITypeDefinition;
						if (null != tmp){ doc.TopLevelTypeDefinitions.Add (tmp); }
					}
				}
				
				// Add global category for unscoped symbols
				foreach (LanguageItem li in pi.InstanceMembers ()) {
					if (null == li.Parent && FilePath.Equals (li.File, fileName)) {
						AddLanguageItem (dom, pi, globals, li, contentLines);
					}
				}
			}
			
			doc.TopLevelTypeDefinitions.Add (globals);
			Console.WriteLine (doc.TopLevelTypeDefinitions.Count);
			return doc;
		}
		
		/// <summary>
		/// Finds the end of a function's definition by matching braces.
		/// </summary>
		/// <param name="content">
		/// A <see cref="System.String"/> array: each line of the content to be searched.
		/// </param>
		/// <param name="startLine">
		/// A <see cref="System.Int32"/>: The earliest line at which the function may start.
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>: The detected end of the function.
		/// </returns>
		static int FindFunctionEnd (string[] content, int startLine) {
			int start = FindFunctionStart (content, startLine);
			if (0 > start){ return startLine; }
			
			int count = 0;
			
			for (int i= start; i<content.Length; ++i) {
				foreach (char c in content[i]) {
					switch (c) {
					case '{':
						++count;
						break;
					case '}':
						if (0 >= --count) {
							return i;
						}
						break;
					}
				}
			}
			
			return startLine;
		}
		
		/// <summary>
		/// Finds the start of a function's definition.
		/// </summary>
		/// <param name="content">
		/// A <see cref="System.String"/> array: each line of the content to be searched.
		/// </param>
		/// <param name="startLine">
		/// A <see cref="System.Int32"/>: The earliest line at which the function may start.
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>: The detected start of the function 
		/// definition, or -1.
		/// </returns>
		static int FindFunctionStart (string[] content, int startLine) {
			int semicolon = -1;
			int bracket = -1;
			
			for (int i=startLine; i<content.Length; ++i) {
				semicolon = content[i].IndexOf (';');
				bracket = content[i].IndexOf ('{');
				if (0 <= semicolon) {
					return (0 > bracket ^ semicolon < bracket)? -1: i;
				} else if (0 <= bracket) {
					return i;
				}
			}
			
			return -1;
		}
		
		static readonly Regex paramExpression = new Regex (@"(?<type>[^\s]+)\s+(?<subtype>[*&]*)(?<name>[^\s[]+)(?<array>\[.*)?", RegexOptions.Compiled);
		
		static object AddLanguageItem (IProjectContent dom, ProjectInformation pi, DefaultTypeDefinition klass, LanguageItem li, string[] contentLines)
		{
			
			if (li is Class || li is Structure || li is Enumeration) {
				var type = LanguageItemToIType (dom, pi, li, contentLines);
				klass.NestedTypes.Add (type);
				return type;
			}
			
			if (li is Function) {
				var method = FunctionToIMethod (pi, klass, (Function)li, contentLines);
				klass.Methods.Add (method);
				return method;
			}
			
			var field = LanguageItemToIField (klass, li, contentLines);
			klass.Fields.Add (field);
			return field;
		}
		
		/// <summary>
		/// Create an IMember from a LanguageItem,
		/// using the source document to locate declaration bounds.
		/// </summary>
		/// <param name="pi">
		/// A <see cref="ProjectInformation"/> for the current project.
		/// </param>
		/// <param name="item">
		/// A <see cref="LanguageItem"/>: The item to convert.
		/// </param>
		/// <param name="contentLines">
		/// A <see cref="System.String[]"/>: The document in which item is defined.
		/// </param>
		static DefaultTypeDefinition LanguageItemToIType (IProjectContent content, ProjectInformation pi, LanguageItem item, string[] contentLines)
		{
			var klass = new DefaultTypeDefinition (content, "", item.File);
			if (item is Class || item is Structure) {
				klass.Region = new DomRegion ((int)item.Line, 1, FindFunctionEnd (contentLines, (int)item.Line-1) + 2, 1);
				klass.Kind = item is Class ? TypeKind.Class : TypeKind.Struct;
				foreach (LanguageItem li in pi.AllItems ()) {
					if (klass.Equals (li.Parent) && FilePath.Equals (li.File, item.File))
						AddLanguageItem (content, pi, klass, li, contentLines);
				}
				return klass;
			}
			
			klass.Region = new DomRegion ((int)item.Line, 1, (int)item.Line + 1, 1);
			klass.Kind = TypeKind.Enum;
			return klass;
		}
		
		static IField LanguageItemToIField (ITypeDefinition type, LanguageItem item, string[] contentLines)
		{
			var result = new DefaultField (type, item.Name);
			result.Region = new DomRegion ((int)item.Line, 1, (int)item.Line + 1, 1);
			return result;
		}
		
		static IMethod FunctionToIMethod (ProjectInformation pi, ITypeDefinition type, Function function, string[] contentLines)
		{
			var method = new DefaultMethod (type, function.Name);
			method.Region = new DomRegion ((int)function.Line, 1, FindFunctionEnd (contentLines, (int)function.Line-1)+2, 1);
			
			Match match;
			bool abort = false;
			var parameters = new List<IParameter> ();
			foreach (string parameter in function.Parameters) {
				match = paramExpression.Match (parameter);
				if (null == match) {
					abort = true;
					break;
				}
				var typeRef = new GetClassTypeReference (string.Format ("{0}{1}{2}", match.Groups["type"].Value, match.Groups["subtype"].Value, match.Groups["array"].Value), 0);
				var p =  new DefaultParameter (typeRef, match.Groups["name"].Value);
				parameters.Add (p);
			}
			if (!abort)
				parameters.ForEach (p => method.Parameters.Add (p));
			return method;
		}
		
		
	}
}
