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

using MonoDevelop.ValaBinding.Parser.Afrodite;

namespace MonoDevelop.ValaBinding.Parser
{
	/// <summary>
	/// Parser for Vala source and vapi files
	/// </summary>
	public class ValaDocumentParser: AbstractParser
	{
		private ParsedDocument lastGood;
		
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string content)
		{
			ParsedDocument doc = new ParsedDocument (fileName);
			ProjectInformation pi = ProjectInformationManager.Instance.Get ((null == dom)? null: dom.Project);
			if(null == doc.CompilationUnit){ doc.CompilationUnit = new CompilationUnit (fileName); }
			CompilationUnit cu = (CompilationUnit)doc.CompilationUnit;
			int lastLine = 0;
			ICollection<Symbol> classes = pi.GetClassesForFile (fileName); 
			
			if (null == classes || 0 == classes.Count) {
				return lastGood;
			}
			
			foreach (Symbol node in classes) {
				if (null == node){ continue; }
				List<IMember> members = new List<IMember> ();
				lastLine = node.SourceReferences[0].LastLine;
                
				foreach (Symbol child in node.Children) {
					if (1 > child.SourceReferences.Count || 
					    child.SourceReferences[0].File != node.SourceReferences[0].File){ continue; }
					lastLine = Math.Max (lastLine, child.SourceReferences[0].LastLine+1);
					
					switch (child.MemberType.ToLower ()) {
					case "class":
						members.Add (new DomType (new CompilationUnit (fileName), ClassType.Class, child.Name, new DomLocation (child.SourceReferences[0].FirstLine, 1), string.Empty, new DomRegion (child.SourceReferences[0].FirstLine, int.MaxValue, child.SourceReferences[0].LastLine, int.MaxValue), new List<IMember> ()));
						break;
					case "interface":
						members.Add (new DomType (new CompilationUnit (fileName), ClassType.Interface, child.Name, new DomLocation (child.SourceReferences[0].FirstLine, 1), string.Empty, new DomRegion (child.SourceReferences[0].FirstLine, int.MaxValue, child.SourceReferences[0].LastLine, int.MaxValue), new List<IMember> ()));
						break;
					case "delegate":
						members.Add (new DomType (new CompilationUnit (fileName), ClassType.Delegate, child.Name, new DomLocation (child.SourceReferences[0].FirstLine, 1), string.Empty, new DomRegion (child.SourceReferences[0].FirstLine, int.MaxValue, child.SourceReferences[0].LastLine, int.MaxValue), new List<IMember> ()));
						break;
					case "struct":
						members.Add (new DomType (new CompilationUnit (fileName), ClassType.Struct, child.Name, new DomLocation (child.SourceReferences[0].FirstLine, 1), string.Empty, new DomRegion (child.SourceReferences[0].FirstLine, int.MaxValue, child.SourceReferences[0].LastLine, int.MaxValue), new List<IMember> ()));
						break;
					case "enum":
						members.Add (new DomType (new CompilationUnit (fileName), ClassType.Enum, child.Name, new DomLocation (child.SourceReferences[0].FirstLine, 1), string.Empty, new DomRegion (child.SourceReferences[0].FirstLine, int.MaxValue, child.SourceReferences[0].LastLine, int.MaxValue), new List<IMember> ()));
						break;
					case "method":
					case "creationmethod":
					case "constructor":
						DomMethod method = new DomMethod (child.Name, Modifiers.None, MethodModifier.None, new DomLocation (child.SourceReferences[0].FirstLine, 1), new DomRegion (child.SourceReferences[0].FirstLine, int.MaxValue, child.SourceReferences[0].LastLine, int.MaxValue), new DomReturnType (child.ReturnType.TypeName));
						foreach (DataType param in child.Parameters) {
							method.Add (new DomParameter (method, param.Name, new DomReturnType (param.TypeName)));
						}
						members.Add (method);
						break;
					case "property":
						members.Add (new DomProperty (child.Name, Modifiers.None, new DomLocation (child.SourceReferences[0].FirstLine, 1), new DomRegion (child.SourceReferences[0].FirstLine, int.MaxValue, child.SourceReferences[0].LastLine, int.MaxValue), new DomReturnType ()));
						break;
					case "field":
					case "constant":
					case "errorcode":
						members.Add (new DomField (child.Name, Modifiers.None, new DomLocation (child.SourceReferences[0].FirstLine, 1), new DomReturnType ()));
						break;
					case "signal":
						members.Add (new DomEvent (child.Name, Modifiers.None, new DomLocation (child.SourceReferences[0].FirstLine, 1), new DomReturnType ()));
						break;
					default:
						MonoDevelop.Core.LoggingService.LogDebug ("ValaDocumentParser: Unsupported member type: {0}", child.MemberType);
						break;
					}// Switch on node type
				}// Collect members
				
				cu.Add (new DomType (new CompilationUnit (fileName), ClassType.Class, node.Name, new DomLocation (node.SourceReferences[0].FirstLine, 1), string.Empty, new DomRegion (node.SourceReferences[0].FirstLine, int.MaxValue, lastLine, int.MaxValue), members));
			}// Add each class in file
			
			return (lastGood = doc);
		}// Parse
	}// ValaDocumentParser
}
