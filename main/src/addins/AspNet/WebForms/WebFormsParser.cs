// 
// AspNetParser.cs
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
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.AspNet.WebForms.Parser;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;

namespace MonoDevelop.AspNet.WebForms
{
	public class WebFormsParser : TypeSystemParser
	{
		public override System.Threading.Tasks.Task<ParsedDocument> Parse (ParseOptions parseOptions, System.Threading.CancellationToken cancellationToken)
		{
			var info = new WebFormsPageInfo ();
			var errors = new List<Error> ();

			var parser = new XmlParser (
				new WebFormsRootState (),
				true
			);
			
			try {
				parser.Parse (parseOptions.Content.CreateReader ());
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error parsing ASP.NET document '" + (parseOptions.FileName ?? "") + "'", ex);
				errors.Add (new Error (ErrorType.Error, "Unhandled error parsing ASP.NET document: " + ex.Message));
			}

			// get the errors from the StateEngine parser
			errors.AddRange (parser.Errors);

			// populating the PageInfo instance
			XDocument xDoc = parser.Nodes.GetRoot ();
			info.Populate (xDoc, errors);
			
			var type = AspNetAppProjectFlavor.DetermineWebSubtype (parseOptions.FileName);
			if (type != info.Subtype) {
				if (info.Subtype == WebSubtype.None) {
					errors.Add (new Error (ErrorType.Error, "File directive is missing", new DocumentLocation (1, 1)));
				} else {
					type = info.Subtype;
					errors.Add (new Error (ErrorType.Warning, "File directive does not match page extension", new DocumentLocation (1, 1)));
				}
			}
			
			var result = new WebFormsParsedDocument (parseOptions.FileName, type, info, xDoc);
			result.AddRange (errors);
			
			return System.Threading.Tasks.Task.FromResult((ParsedDocument)result);
		}
	}
}
