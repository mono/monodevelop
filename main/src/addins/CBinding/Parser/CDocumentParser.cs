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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide;

namespace CBinding.Parser
{
	/// <summary>
	/// Ctags-based document parser helper
	/// </summary>
	public class CDocumentParser: AbstractParser
	{
		public CDocumentParser (): base ("Native", "text/x-csrc", "text/x-chdr", "text/x-c++src", "text/x-c++hdr")
		{
		}
		
		public override bool CanParse (string fileName)
		{
			string extension = Path.GetExtension (fileName);
			return (Array.Exists (CProject.SourceExtensions, delegate (string ext){ 
				return ext.Equals (extension, StringComparison.OrdinalIgnoreCase);
			}) || CProject.IsHeaderFile (fileName));
		}// CanParse
		
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string content)
		{
			ParsedDocument doc = new ParsedDocument (fileName);
			doc.Flags |= ParsedDocumentFlags.NonSerializable;
			Project p = (null == dom || null == dom.Project)? 
				IdeApp.Workspace.GetProjectContainingFile (fileName):
				dom.Project;
			ProjectInformation pi = ProjectInformationManager.Instance.Get (p);
			CompilationUnit cu;
			doc.CompilationUnit = cu = new CompilationUnit (fileName);
			IType tmp;
			IMember member;
			string[] contentLines = content.Split (new string[]{Environment.NewLine}, StringSplitOptions.None);
			DomType globals = new DomType (cu, ClassType.Unknown, GettextCatalog.GetString ("(Global Scope)"), new DomLocation (1, 1), string.Empty, new DomRegion (1, int.MaxValue), new List<IMember> ());
			
			lock (pi) {
				// Add containers to type list
				foreach (LanguageItem li in pi.Containers ()) {
					if (null == li.Parent && FilePath.Equals (li.File, fileName)) {
						tmp = LanguageItemToIMember (pi, li, contentLines) as IType;
						if (null != tmp){ cu.Add (tmp); }
					}
				}
				
				// Add global category for unscoped symbols
				foreach (LanguageItem li in pi.InstanceMembers ()) {
					if (null == li.Parent && FilePath.Equals (li.File, fileName)) {
						member = LanguageItemToIMember (pi, li, contentLines);
						if (null != member) { 
							globals.Add (member); 
						}
					}
				}
			}
			
			cu.Add (globals);
			
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
		static IMember LanguageItemToIMember (ProjectInformation pi, LanguageItem item, string[] contentLines)
		{
			if (item is Class || item is Structure) {
				DomType klass = new DomType (new CompilationUnit (item.File), ClassType.Class, item.Name, new DomLocation ((int)item.Line, 1), string.Empty, new DomRegion ((int)item.Line+1, FindFunctionEnd (contentLines, (int)item.Line-1)+2), new List<IMember> ());
				
				foreach (LanguageItem li in pi.AllItems ()) {
					if (klass.Equals (li.Parent) && FilePath.Equals (li.File, item.File)) {
						klass.Add (LanguageItemToIMember (pi, li, contentLines));
					}
				}
				return klass;
			}
			if (item is Enumeration) {
				return new DomType (new CompilationUnit (item.File), ClassType.Enum, item.Name, new DomLocation ((int)item.Line, 1), string.Empty, new DomRegion ((int)item.Line+1, (int)item.Line+1), new List<IMember> ());
			}
			if (item is Function) {
				return new DomMethod (item.Name, Modifiers.None, MethodModifier.None, new DomLocation ((int)item.Line, 1), new DomRegion ((int)item.Line+1, FindFunctionEnd (contentLines, (int)item.Line-1)+2), new DomReturnType ());
			}
			if (item is Member) {
				return new DomField (item.Name, Modifiers.None, new DomLocation ((int)item.Line, 1), new DomReturnType ());
			}
			return null;
		}
	}
}
