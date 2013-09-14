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
using MonoDevelop.CSSBinding.Parse.Interfaces;
using MonoDevelop.CSSBinding.Parse.Models;


namespace MonoDevelop.CSSParser
{
	public class CSSParserAdapter : TypeSystemParser
	{
		private object[] GetFolStringAndRelEndCoord(string text)
		{
			var stringList = text.Split (new string[] {  System.Environment.NewLine },StringSplitOptions.None);
			if (stringList.Length == 1) {
				return new object[] {text, 1, text.Length };
			} else {
				return new object[] {stringList.GetValue(0), stringList.Length, stringList.GetValue(stringList.Length-1) };
			}
		}

		private string GetSelectionFoldingText(string text){

			return text.Split(new char[] { '{' }).GetValue (0).ToString () + "{...}";
		}

		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader content, Project project = null)
		{
			CSSParserManager template = new CSSParserManager (fileName);
			string parsingString = content.ReadToEnd ();
			AntlrInputStream input = new AntlrInputStream (parsingString);





			Console.WriteLine (parsingString);

			CSSLexer lexer = new CSSLexer (input);
			CommonTokenStream tokens = new CommonTokenStream(lexer);
			CSSParser parser = new CSSParser(tokens);

			IList<IToken> d = tokens.GetTokens ();

			int tokenCount = tokens.GetNumberOfOnChannelTokens(); //this is caled to load d

			IList<IToken> cs  = new List<IToken>();

			foreach (IToken item in d) {
				if (item.Channel == 2) {
					Console.WriteLine ("hehee:"+item.Text);
					cs.Add (item);
				}
			}

			var parserErrorHandler = new CSSParserErrorHandle ();
			parser.AddErrorListener(parserErrorHandler);
			var x = parser.styleSheet();

			var bodySetContext = x.bodylist().bodyset();
			List<ISegment> foldingSegments = new List<ISegment> ();

			foreach (var item in cs) {
				var relativeEndCoordinates = GetFolStringAndRelEndCoord(item.Text);
				foldingSegments.Add(new CodeSegment (relativeEndCoordinates.GetValue(0).ToString(), 
				                                     new Location (fileName, item.Line, item.Column), 
				                                     new Location (fileName, item.Line + Int32.Parse( relativeEndCoordinates.GetValue(1).ToString())-1, item.Column+ Int32.Parse( relativeEndCoordinates.GetValue(1).ToString())-1),CodeSegmentType.Comment));
			}



			foreach (var item in bodySetContext)
			{
				string foldingString = GetSelectionFoldingText(item.GetText());
				Console.WriteLine ("Passing info test: "+foldingString+" start line num:" + item.Start.Line + " start comumn:" + item.Start.Column + " end lime: " + item.Stop.Line + " end column: " + item.Stop.Column);

				foldingSegments.Add(new CodeSegment(foldingString,new Location(fileName,item.Start.Line ,item.Start.Column), new Location(fileName,item.Stop.Line,item.Stop.Column),CodeSegmentType.CSSElement));

			}

			var errors = new List<Error> ();
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

		}

	}
}

