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

		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader content, Project project = null)
		{
			CSSParserManager template = new CSSParserManager (fileName);
			string parsingString = content.ReadToEnd ();
			AntlrInputStream input = new AntlrInputStream (parsingString);
			CSSLexer lexer = new CSSLexer (input);
			CommonTokenStream tokens = new CommonTokenStream(lexer);
			CSSParser parser = new CSSParser(tokens);

			var foldingList = new FoldingTokensVM ();

			IList<IToken> allTokens = tokens.GetTokens ();
			int tokenCount = tokens.GetNumberOfOnChannelTokens(); //Do not remove this line of code. This forces tokens to be filled

			foldingList.commentList = AccumilateComments (fileName, allTokens);


			var parserErrorHandler = new CSSParserErrorHandle ();
			parser.AddErrorListener(parserErrorHandler);
			var x = parser.styleSheet();
			var bodySetContext = x.bodylist().bodyset();

			foldingList.cssSelectionList= AccumilateCSSElements (fileName, bodySetContext);

			var errors = new List<Error> ();

			errors.AddRange( parserErrorHandler.ParserErrors);



			var doc = new CSSParsedDocument (fileName,foldingList, errors);
			doc.Flags |= ParsedDocumentFlags.NonSerializable;

			return doc;
		}

		private object[] GetFolStringAndRelEndCoord(string text)
		{
			var stringList = text.Split (new string[] {  System.Environment.NewLine },StringSplitOptions.None);
			if (stringList.Length == 1) {
				return new object[] {text, 1, text.Length };
			} else {
				return new object[] {stringList[0], stringList.Length, stringList[stringList.Length-1].Length };
			}
		}

		private string GetSelectionFoldingText(string text){

			return text.Split(new char[] { '{' }).GetValue (0).ToString () + "{...}";
		}

		private List<ISegment> AccumilateComments(string fileName, IList<IToken> allTokens)
		{
			IList<IToken> commentTokens  = new List<IToken>();
			List<ISegment> foldingSegments = new List<ISegment> ();

			foreach (IToken item in allTokens) {
				if (item.Channel == 2) {
					commentTokens.Add (item);
				}
			}

			foreach (var item in commentTokens) {
				var relativeEndCoordinates = GetFolStringAndRelEndCoord(item.Text);
				foldingSegments.Add(new CodeSegment (relativeEndCoordinates.GetValue(0).ToString(), 
				                                     new Location (fileName, item.Line, item.Column), 
				                                     new Location (fileName, item.Line + Int32.Parse( relativeEndCoordinates.GetValue(1).ToString())-1, Int32.Parse( relativeEndCoordinates.GetValue(2).ToString())-1),CodeSegmentType.Comment));
			}

			return foldingSegments;
		}

		private List<ISegment> AccumilateCSSElements (string fileName, IList<CSSParser.BodysetContext> bodySetContext)
		{
			List<ISegment> foldingSegments = new List<ISegment> ();
			foreach (var item in bodySetContext)
			{
				string foldingString = GetSelectionFoldingText(item.GetText());
				foldingSegments.Add(new CodeSegment(foldingString,new Location(fileName,item.Start.Line ,item.Start.Column), new Location(fileName,item.Stop.Line,item.Stop.Column),CodeSegmentType.CSSElement));

			}
			return foldingSegments;
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

	// Accumilate parser errors
	public class CSSParserErrorHandle : IAntlrErrorListener<IToken> 
	{
		private IList<Error> parserErrors;

		public CSSParserErrorHandle()
		{
			parserErrors = new List<Error>();
		}

		public IList<Error> ParserErrors
		{
			get{
				return parserErrors;
			}
		}

		public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
		{
			parserErrors.Add (new Error (ErrorType.Error, msg,line,(charPositionInLine)));
			Console.WriteLine ("line: "+ line + " position :" + charPositionInLine + " message:  " + msg);
		}

	}
}

