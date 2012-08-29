// 
// HtmlParser.cs
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
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Xml.StateEngine;
using MonoDevelop.AspNet.StateEngine;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.Html
{
	public class HtmlParser : TypeSystemParser
	{
		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader tr, Project project = null)
		{
			var doc = new XmlParsedDocument (fileName);
//			doc.Flags = ParsedDocumentFlags.NonSerializable;
			
			try {
				Parser xmlParser = new Parser (
					new XmlFreeState (new HtmlTagState (true), new HtmlClosingTagState (true)),
					true);
				
				xmlParser.Parse (tr);
				doc.XDocument = xmlParser.Nodes.GetRoot ();
				doc.Add (xmlParser.Errors);
				if (doc.XDocument != null)
					doc.Add (Validate (doc.XDocument));
			}
			catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Unhandled error parsing HTML document", ex);
			}
			return doc;
		}
		
		IEnumerable<Error> Validate (XDocument doc)
		{
			foreach (XNode node in doc.Nodes) {
				if (node is XElement && !Object.ReferenceEquals (node, doc.RootElement)) {
					yield return new Error (ErrorType.Warning, "More than one root element", node.Region);
				}
			}
		}
	}
}
