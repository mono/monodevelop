// 
// XmlDocumentParser.cs
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
using System.Threading;
using System.Threading.Tasks;

using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor
{
	class XmlDocumentParser : TypeSystemParser
	{
		public override Task<ParsedDocument> Parse (ParseOptions options, CancellationToken cancellationToken)
		{
			return Task.Run (() => ParseInternal (options, cancellationToken));
		}

		ParsedDocument ParseInternal (ParseOptions options, CancellationToken cancellationToken)
		{
			var doc = new XmlParsedDocument (options.FileName);
			doc.Flags |= ParsedDocumentFlags.NonSerializable;
			var xmlParser = new XmlParser (new XmlRootState (), true);

			try {
				xmlParser.Parse (options.Content.CreateReader ());
			} catch (Exception ex) {
				Core.LoggingService.LogError ("Unhandled error parsing xml document", ex);
			}

			doc.XDocument = xmlParser.Nodes.GetRoot ();
			doc.AddRange (xmlParser.Errors);

			if (doc.XDocument != null && doc.XDocument.RootElement != null) {
				if (!doc.XDocument.RootElement.IsEnded)
					doc.XDocument.RootElement.End (xmlParser.Location);
			}

			return doc;
		}
	}
}
