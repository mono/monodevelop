//
// OutlineNode.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.AspNet.Parser.Dom;
using System.Web.Razor.Parser.SyntaxTree;

namespace MonoDevelop.AspNet.Mvc.Parser
{
	public class OutlineNode
	{
		public string Name { get; set; }
		public DomRegion Location { get; set; }

		public OutlineNode ()
		{
		}

		public OutlineNode (TagNode node)
		{
			Name = node.TagName;
			string att = null;
			if (node.Attributes != null)
				att = node.Attributes["id"] as string;
			if (att != null)
				Name = "<" + Name + "#" + att + ">";
			else
				Name = "<" + Name + ">";

			ILocation start = node.Location, end;
			if (node.EndLocation != null)
				end = node.EndLocation;
			else
				end = start;
			Location = new DomRegion (start.BeginLine, start.BeginColumn + 1, end.EndLine, end.EndColumn + 1);
		}

		public OutlineNode (Block block)
		{
			Name = RazorUtils.GetShortName (block);
		}
	}
}
