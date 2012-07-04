// 
// AspNetParsedDocument.cs
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

using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Xml.StateEngine;
using MonoDevelop.AspNet.StateEngine;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.AspNet.Parser
{
	public class AspNetParsedDocument : XmlParsedDocument
	{
		
		public AspNetParsedDocument (string fileName, WebSubtype type, RootNode rootNode, PageInfo info) : base (fileName)
		{
			Flags |= ParsedDocumentFlags.NonSerializable;
			Info = info;
			RootNode = rootNode;
			Type = type;
		}

		// Constructor used for testing the XDOM
		public AspNetParsedDocument (string fileName, WebSubtype type, RootNode rootNode, PageInfo info, XDocument xDoc) : 
			base (fileName)
		{
			Flags |= ParsedDocumentFlags.NonSerializable;
			Info = info;
			RootNode = rootNode;
			Type = type;
			XDocument = xDoc;
		}
		
		public PageInfo Info { get; private set; }
		public RootNode RootNode { get; private set; }
		public WebSubtype Type { get; private set; }
		
		public override IEnumerable<FoldingRegion> Foldings {
			get {				
				if (XDocument == null)
					yield break;
				foreach (XNode node in XDocument.AllDescendentNodes) {
					if (node is MonoDevelop.Xml.StateEngine.XElement) {
						var el = node as XElement;
						
						if (el.IsClosed && (el.ClosingTag.Region.BeginLine - el.Region.EndLine) > 2) {
							// display the ID tags for ASP.NET list controls
							string controlId = string.Empty;
							if (el.Name.HasPrefix && (el.Name.Prefix == "asp")) {
								XName idAttr = new XName ("id");
								foreach (XAttribute attr in el.Attributes) {
									if (attr.Name.ToLower () == idAttr)
										controlId = string.Format (" ID=\"{0}\"", attr.Value);
								}
							}
							
							yield return new FoldingRegion (
								string.Format ("<{0}{1}... >", el.Name.FullName, controlId),
							    new DomRegion (el.Region.Begin, el.ClosingTag.Region.End));       
						}
					} else if (node is XDocType) {
						XDocType dt = (XDocType)node;
						string id = !String.IsNullOrEmpty (dt.PublicFpi) ? dt.PublicFpi
									: !String.IsNullOrEmpty (dt.Uri) ? dt.Uri : null;

						if (id != null && dt.Region.EndLine - dt.Region.BeginLine > 2) {
							if (id.Length > 50)
								id = id.Substring (0, 47) + "...";

							FoldingRegion fr = new FoldingRegion (string.Format ("<!DOCTYPE {0}>", id), dt.Region);
							fr.IsFoldedByDefault = true;
							yield return fr;
						}
					} else if (node is XComment) {
						if (node.Region.EndLine - node.Region.BeginLine > 2)
							yield return new FoldingRegion ("<!-- -->", node.Region);
						
					} else if (node is AspNetServerComment) {
						var cnode = node as AspNetServerComment;
						if ((cnode.Region.EndLine - cnode.Region.BeginLine) > 2)
							yield return new FoldingRegion (cnode.FriendlyPathRepresentation, cnode.Region);
						
					} else if (node is AspNetDirective) {
						var cnode = node as AspNetDirective;
						if ((cnode.Region.EndLine - cnode.Region.BeginLine) > 2)
							yield return new FoldingRegion (cnode.FriendlyPathRepresentation, cnode.Region);
						
					} else if (node is AspNetExpression) {
						var cnode = node as AspNetExpression;
						if ((cnode.Region.EndLine - cnode.Region.BeginLine) > 2)
							yield return new FoldingRegion (cnode.FriendlyPathRepresentation, cnode.Region);
						
					} else if (node is AspNetRenderBlock) {
						var cnode = node as AspNetRenderBlock;
						if ((cnode.Region.EndLine - cnode.Region.BeginLine) > 2)
							yield return new FoldingRegion (cnode.FriendlyPathRepresentation, cnode.Region);
					}
				}
			}
		}
	}
}
