// 
// MoonlightParsedDocument.cs
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
using System.Collections.Generic;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.Moonlight
{
	
	
	public class MoonlightParsedDocument : ParsedDocument
	{
		public MoonlightParsedDocument (string fileName)
			: base (fileName)
		{
		}
		
		public MonoDevelop.Xml.StateEngine.XDocument XDocument { get; internal set; }
		
		public override IEnumerable<FoldingRegion> GenerateFolds()
		{
			if (XDocument == null)
				yield break;
			
			foreach (XNode node in XDocument.AllDescendentNodes) {
				if (node is XCData)
				{
					if (node.Region.End.Line - node.Region.Start.Line > 2)
						yield return new FoldingRegion ("<![CDATA[ ]]>", node.Region);
				}
				else if (node is XComment)
				{
					if (node.Region.End.Line - node.Region.Start.Line > 2)
						yield return new FoldingRegion ("<!-- -->", node.Region);
				}
				else if (node is XElement)
				{
					XElement el = (XElement) node;
					if (el.IsClosed && el.ClosingTag.Region.End.Line - el.Region.Start.Line > 2) {
						yield return new FoldingRegion
							(string.Format ("<{0}...>", el.Name.FullName),
							 new DomRegion (el.Region.Start, el.ClosingTag.Region.End));
					}
				}
			}
		}

	}
}
