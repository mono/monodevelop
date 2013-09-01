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
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;
using System.Collections.Generic;


using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DFA = Antlr4.Runtime.Dfa.DFA;


namespace MonoDevelop.CSSParser
{
	public class CSSParserAdapter : TypeSystemParser
	{

		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader content, Project project = null)
		{
			CSSParserManager template = new CSSParserManager (fileName);

			AntlrInputStream input = new AntlrInputStream(content.ReadToEnd());

			var lexer = new CSSLexer(input);
			CommonTokenStream tokens = new CommonTokenStream(lexer);
			CSSParser parser = new CSSParser(tokens);

			var parserErrorHandler = new CSSParserErrorHandle ();
//			var lexerErrorHandler = new CSSLexerErrorHandle ();
			parser.AddErrorListener(parserErrorHandler);
			var x = parser.styleSheet();

			var bodySetContext = x.bodylist().bodyset();
			List<ISegment> foldingSegments = new List<ISegment> ();

		
			foreach (var item in bodySetContext)
			{
				string foldingString = item.GetText().Split(new char[] { '{' }).GetValue (0).ToString () + "{...";

				Console.WriteLine ("Passing info test: "+foldingString+" start line num:" + item.Start.Line + " start comumn:" + item.Start.Column + " end lime: " + item.Stop.Line + " end column: " + item.Stop.Column);

				foldingSegments.Add(new CodeSegment(foldingString,new Location(fileName,item.Start.Line ,item.Start.Column), new Location(fileName,item.Stop.Line,item.Stop.Column)));

			}

			var errors = new List<Error> ();
//			errors.AddRange (lexerErrorHandler.lexerErrors);
			errors.AddRange (parserErrorHandler.parserErrors);
//			foreach (var item in errors) {
//				Console.WriteLine (item.ErrorType +"----em:" + item.Message + "-----bc:" + item.Region.BeginColumn +"-----el:"+ item.Region.EndColumn);
//			}
			var doc = new CSSParsedDocument (fileName,foldingSegments, errors);
			doc.Flags |= ParsedDocumentFlags.NonSerializable;

			return doc;
		}  
	}

//	public class CSSLexerErrorHandle : IAntlrErrorListener<IToken>
//	{
//		public List<Error> lexerErrors = new List<Error>();
//
//		public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
//		{
//			lexerErrors.Add (new Error (ErrorType.Error, msg,new DomRegion(line,charPositionInLine)));
//
//		}
//
//	}

	public class CSSParserErrorHandle : IAntlrErrorListener<IToken>
	{
		public List<Error> parserErrors = new List<Error>();

		public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
		{
			parserErrors.Add (new Error (ErrorType.Error, msg,new DomRegion(line,charPositionInLine)));
//			Console.WriteLine (msg + " ---" + line +"-----:" + offendingSymbol.Text);

		}

	}
}

