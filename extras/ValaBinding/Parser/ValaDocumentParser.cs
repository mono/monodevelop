// 
// ValaDocumentParser.cs
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
using System.Threading;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.ValaBinding.Parser
{
	/// <summary>
	/// Parser for Vala source and vapi files
	/// </summary>
	public class ValaDocumentParser: AbstractParser
	{
		public ValaDocumentParser(): base("Vala", "text/x-vala")
		{
		}
	
		public override bool CanParse (string fileName)
		{
			string extension = Path.GetExtension (fileName);
			if(!string.IsNullOrEmpty (extension)) {
				extension = extension.ToUpper ();                
				return (extension == ".VALA" || extension == ".VAPI");
			}
			
			return false;
		}// CanParse
	
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string content)
		{
			ParsedDocument doc = new ParsedDocument (fileName);
			ProjectInformation pi = ProjectInformationManager.Instance.Get (dom.Project);
			if(null == doc.CompilationUnit){ doc.CompilationUnit = new CompilationUnit (fileName); }
			CompilationUnit cu = (CompilationUnit)doc.CompilationUnit;
			int lastLine = 0;
			
			foreach (CodeNode node in pi.GetClassesForFile (fileName)) {
				if (null == node){ continue; }
				List<IMember> members = new List<IMember> ();
				lastLine = node.LastLine;
                
				foreach (CodeNode child in pi.GetChildren (node)) {
					if (child.File != node.File){ continue; }
					Console.WriteLine ("Got {0}: {1}-{2}", child.Name, child.FirstLine, child.LastLine);
					lastLine = Math.Max (lastLine, child.LastLine+1);
					
					switch (child.NodeType) {
					case "class":
						members.Add (new DomType (new CompilationUnit (fileName), ClassType.Class, child.Name, new DomLocation (child.FirstLine, 1), string.Empty, new DomRegion (child.FirstLine+1, child.LastLine+1), new List<IMember> ()));
						break;
					case "delegate":
						members.Add (new DomType (new CompilationUnit (fileName), ClassType.Delegate, child.Name, new DomLocation (child.FirstLine, 1), string.Empty, new DomRegion (child.FirstLine+1, child.LastLine+1), new List<IMember> ()));
						break;
					case "struct":
						members.Add (new DomType (new CompilationUnit (fileName), ClassType.Struct, child.Name, new DomLocation (child.FirstLine, 1), string.Empty, new DomRegion (child.FirstLine+1, child.LastLine+1), new List<IMember> ()));
						break;
					case "enums":
						members.Add (new DomType (new CompilationUnit (fileName), ClassType.Enum, child.Name, new DomLocation (child.FirstLine, 1), string.Empty, new DomRegion (child.FirstLine+1, child.LastLine+1), new List<IMember> ()));
						break;
					case "method":
						members.Add (new DomMethod (child.Name, Modifiers.None, MethodModifier.None, new DomLocation (child.FirstLine, 1), new DomRegion (child.FirstLine+1, child.LastLine+1), new DomReturnType (((Function)child).ReturnType)));
						break;
					case "property":
						members.Add (new DomProperty (child.Name, Modifiers.None, new DomLocation (child.FirstLine, 1), new DomRegion (child.FirstLine+1, child.LastLine+1), new DomReturnType ()));
						break;
					case "field":
						members.Add (new DomField (child.Name, Modifiers.None, new DomLocation (child.FirstLine, 1), new DomReturnType ()));
						break;
					case "signal":
						members.Add (new DomEvent (child.Name, Modifiers.None, new DomLocation (child.FirstLine, 1), new DomReturnType ()));
						break;
					default:
						Console.WriteLine("Unsupported member type: {0}", child.NodeType);
						break;
					}// Switch on node type
				}// Collect members
				
				cu.Add (new DomType (new CompilationUnit (fileName), ClassType.Class, node.Name, new DomLocation (node.FirstLine, 1), string.Empty, new DomRegion (node.FirstLine+1, lastLine+1), members));
			}// Add each class in file
			
			return doc;
		}// Parse
	}// ValaDocumentParser
}
