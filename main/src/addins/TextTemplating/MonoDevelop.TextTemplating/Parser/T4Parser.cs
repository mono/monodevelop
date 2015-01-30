// 
// T4Parser.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using Mono.TextTemplating;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;

namespace MonoDevelop.TextTemplating.Parser
{
	public class T4Parser : TypeSystemParser
	{
		public override System.Threading.Tasks.Task<ParsedDocument> Parse (ParseOptions parseOptions, System.Threading.CancellationToken cancellationToken)
		{
			var fileName = parseOptions.FileName;
			ParsedTemplate template = new ParsedTemplate (fileName);
			var readOnlyDoc = TextEditorFactory.CreateNewReadonlyDocument (parseOptions.Content, fileName);

			try {
				var tk = new Tokeniser (fileName, readOnlyDoc.Text);
				template.ParseWithoutIncludes (tk);
			} catch (ParserException ex) {
				template.LogError (ex.Message, ex.Location);
			}
			var errors = new List<Error> ();
			foreach (System.CodeDom.Compiler.CompilerError err in template.Errors) {
				errors.Add (new Error (err.IsWarning ? ErrorType.Warning : ErrorType.Error, err.ErrorText, new DocumentLocation (err.Line, err.Column)));
			}
			var doc = new T4ParsedDocument (fileName, template.RawSegments, errors);
			doc.Flags |= ParsedDocumentFlags.NonSerializable;

			return System.Threading.Tasks.Task.FromResult((ParsedDocument)doc);
		}
	}
}
