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
using System.IO;

using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Xml.StateEngine;
using MonoDevelop.AspNet.StateEngine;

namespace MonoDevelop.AspNet.Parser
{
	public class AspNetParser : AbstractTypeSystemParser
	{
		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader tr, Project project = null)
		{
			var info = new PageInfo ();
			var errors = new List<Error> ();

			Xml.StateEngine.Parser parser = new Xml.StateEngine.Parser (
				new AspNetFreeState (),
				true
			);
			
			try {
				parser.Parse (tr);
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error parsing ASP.NET document '" + (fileName ?? "") + "'", ex);
				errors.Add (new Error (ErrorType.Error, "Unhandled error parsing ASP.NET document: " + ex.Message));
			}

			// get the errors from the StateEngine parser
			errors.AddRange (parser.Errors);

			// populating the PageInfo instance
			XDocument xDoc = parser.Nodes.GetRoot ();
			info.Populate (xDoc, errors);
			
			var type = AspNetAppProject.DetermineWebSubtype (fileName);
			if (type != info.Subtype) {
				if (info.Subtype == WebSubtype.None) {
					errors.Add (new Error (ErrorType.Error, "File directive is missing", 1, 1));
				} else {
					type = info.Subtype;
					errors.Add (new Error (ErrorType.Warning, "File directive does not match page extension", 1, 1));
				}
			}
			
			var result = new AspNetParsedDocument (fileName, type, info, xDoc);
			result.Add (errors);
							
			/*
			if (MonoDevelop.Core.LoggingService.IsLevelEnabled (MonoDevelop.Core.Logging.LogLevel.Debug)) {
				DebugStringVisitor dbg = new DebugStringVisitor ();
				rootNode.AcceptVisit (dbg);
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				sb.AppendLine ("Parsed AspNet file:");
				sb.AppendLine (dbg.DebugString);
				if (errors.Count > 0) {
					sb.AppendLine ("Errors:");
					foreach (ParserException ex in errors)
						sb.AppendLine (ex.ToString ());
				}
				MonoDevelop.Core.LoggingService.LogDebug (sb.ToString ());
			}*/
			
			return result;
		}
	}
}
