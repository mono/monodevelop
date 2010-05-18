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

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using System.Collections.Generic;
using MonoDevelop.Core;


namespace MonoDevelop.AspNet.Parser
{
	
	
	public class AspNetParser : AbstractParser
	{
		public AspNetParser () : base (null, "application/x-aspx", "application/x-ascx", "application/x-master-page")
		{
		}
		
		public override bool CanParse (string fileName)
		{
			return fileName.EndsWith (".aspx") || fileName.EndsWith (".ascx") || fileName.EndsWith (".master");
		}
		
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string fileContent)
		{
			using (var tr = new StringReader (fileContent)) {
				var info = new PageInfo ();
				var rootNode = new RootNode ();
				var errors = new List<Error> ();
				
				try {
					rootNode.Parse (fileName, tr);
				} catch (Exception ex) {
					LoggingService.LogError ("Unhandled error parsing ASP.NET document '" + (fileName ?? "") + "'", ex);
					errors.Add (new Error (ErrorType.Error, 0, 0, "Unhandled error parsing ASP.NET document: " + ex.Message));
				}
				
				foreach (var pe in rootNode.ParseErrors)
					errors.Add (new Error (ErrorType.Error, pe.Location.BeginLine, pe.Location.BeginColumn, pe.Message));
				
				info.Populate (rootNode, errors);
				
				var result = new AspNetParsedDocument (fileName, rootNode, info);
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
		
		internal void AddError (ErrorType type, ILocation location, string message)
		{
			
		}
		
		void Init (TextReader sr)
		{
			
			
			
		}
	}
}
