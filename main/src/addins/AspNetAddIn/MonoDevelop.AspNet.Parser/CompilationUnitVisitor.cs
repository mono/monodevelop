// 
// CompilationUnitVisitor.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using MonoDevelop.Projects.Parser;
using MonoDevelop.AspNet.Parser.Dom;

namespace MonoDevelop.AspNet.Parser
{
	
	
	public class CompilationUnitVisitor : Visitor
	{
		MonoDevelop.Projects.Parser.DefaultCompilationUnit cu;
		
		public CompilationUnitVisitor (MonoDevelop.Projects.Parser.DefaultCompilationUnit cu)
		{
			this.cu = cu;
		}
		
		MonoDevelop.Projects.Parser.ICompilationUnit CU {
			get { return cu; }
		}
		
		public override void Visit (TagNode node)
		{
			BuildRegion (node);
		}
		
		#region Region-building
		
		void BuildRegion (TagNode node)
		{
			if (!IsMultiLine (node.Location, node.EndLocation))
			    return;
			
			string id = (string) node.Attributes["id"];
			string name;
			if (id != null)
				name = string.Concat ("<", node.TagName, "#", id);
			else
				name = string.Concat ("<", node.TagName);
				
			
			if (node.EndLocation != null)
				name = string.Concat (name, ">...</", node.TagName, ">");
			else
				name = string.Concat (name, " />");
			
			AddRegion (name, node.Location, node.EndLocation);
		}
		
		void AddRegion (string name, ILocation startLocation, ILocation endLocation)
		{
			DefaultRegion region;
			if (endLocation == null)
				region = new DefaultRegion (startLocation.BeginLine, startLocation.BeginColumn + 1, startLocation.EndLine, startLocation.EndColumn + 1);
			else
				region = new DefaultRegion (startLocation.BeginLine, startLocation.BeginColumn + 1, endLocation.EndLine, endLocation.EndColumn + 1);
			
			cu.FoldingRegions.Add (new FoldingRegion (name, region));
		}
		
		bool IsMultiLine (ILocation startLocation, ILocation endLocation)
		{
			return (startLocation.BeginLine < startLocation.EndLine || 
			    (endLocation != null && endLocation.EndLine > startLocation.BeginLine));
		}
		
		#endregion
	}
}
