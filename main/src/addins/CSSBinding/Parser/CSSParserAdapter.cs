//
// CSSParserAdapter.cs
//
// Author:
//       Diyoda Sajjana <>
//
// Copyright (c) 2013 Diyoda Sajjana
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
using Antlr.Runtime;
using Antlr.Runtime.Misc;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;
using System.Collections.Generic;




namespace Parser
{
	public class CSSParserAdapter : TypeSystemParser
	{
		public CSSParserAdapter ()
		{
		}

		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader content, Project project = null)
		{
			CSSParserManager template = new CSSParserManager (fileName);
			//try {

			ANTLRStringStream input = new ANTLRStringStream(content.ReadToEnd());

			var lexer = new CSSLexer(input);
			CommonTokenStream tokens = new CommonTokenStream(lexer);
			CSSParser parser = new CSSParser(tokens);
			parser.styleSheet();

			//template.ParseWithoutIncludes (tk);
			//} catch (ParserException ex) {
			//	template.LogError (ex.Message, ex.Location);
			//}

			var errors = new List<Error> ();
			errors.AddRange (lexer.lexerErrors);
			errors.AddRange (parser.parserErrors);
			var doc = new CSSParsedDocument (fileName, errors);
			doc.Flags |= ParsedDocumentFlags.NonSerializable;

			return doc;
		}
	}

	public partial class CSSLexer
	{
		public List<Error> lexerErrors = new List<Error>();
		public override void ReportError(RecognitionException e)
		{
			//Handle has to be implimented!
			base.ReportError(e);
			lexerErrors.Add (new Error (ErrorType.Error, e.Message, e.Line, e.CharPositionInLine));
		}

	}

	public partial class CSSParser
	{
		public List<Error> parserErrors = new List<Error>();
		public override void ReportError(RecognitionException e)
		{
			//Handle has to be implimented!
			base.ReportError(e);
			parserErrors.Add (new Error (ErrorType.Error, e.Message, e.Line, e.CharPositionInLine));

		}

	}
}

