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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Editor
{
	public class XmlParsedDocument : DefaultParsedDocument
	{
		public XmlParsedDocument (string fileName) : base (fileName)
		{
		}
		
		public XDocument XDocument { get; set; }

		public override Task<IReadOnlyList<FoldingRegion>> GetFoldingsAsync (System.Threading.CancellationToken cancellationToken)
		{
			return Task.FromResult((IReadOnlyList<FoldingRegion>)Foldings.ToList ());
		}

		public IEnumerable<FoldingRegion> Foldings {
			get {
				if (XDocument == null)
					yield break;
				foreach (XNode node in XDocument.AllDescendentNodes) {
					if (node is XCData) {
						if (node.Region.EndLine - node.Region.BeginLine > 2)
							yield return new FoldingRegion ("<![CDATA[ ]]>", node.Region);
					} 
					else if (node is XComment) {
						if (node.Region.EndLine - node.Region.BeginLine > 2)
							yield return new FoldingRegion ("<!-- -->", node.Region);
					}
					else if (node is XElement el) {
						if (el.IsClosed && el.ClosingTag.Region.EndLine - el.Region.BeginLine > 2) {
							yield return new FoldingRegion (
								$"<{el.Name.FullName}...>",
								new DocumentRegion (el.Region.Begin, el.ClosingTag.Region.End));
						}
					} else if (node is XDocType dt) {
						string id = !string.IsNullOrEmpty (dt.PublicFpi) ? dt.PublicFpi
							: !string.IsNullOrEmpty (dt.Uri) ? dt.Uri : null;

						if (id != null && dt.Region.EndLine - dt.Region.BeginLine > 2) {
							if (id.Length > 50)
								id = id.Substring (0, 47) + "...";

							var fr = new FoldingRegion ($"<!DOCTYPE {id}>", dt.Region) {
								IsFoldedByDefault = true
							};
							yield return fr;
						}
					}
				}
			}
		}
	}
}
