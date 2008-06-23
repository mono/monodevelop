//
// CSharpIndentEngine.cs
//
// Author: Jeffrey Stedfast <fejj@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Text;

using MonoDevelop.Ide.Gui.Content;

using CSharpBinding.FormattingStrategy.Properties;

namespace CSharpBinding.FormattingStrategy {
	public partial class CSharpIndentEngine : ICloneable, IDocumentStateEngine {
		IndentStack stack;
		
		// Ponder: should linebuf be dropped in favor of a
		// 'wordbuf' and a 'int curLineLen'? No real need to
		// keep a full line buffer.
		StringBuilder linebuf;
		
		string keyword;
		
		string curIndent;
		
		Inside beganInside;
		
		bool needsReindent;
		bool popVerbatim;
		bool canBeLabel;
		bool isEscaped;
		
		int firstNonLwsp;
		int lastNonLwsp;
		int wordStart;
		
		// previous char in the line
		char pc;
		
		// last significant (real) char in the line
		// (e.g. non-whitespace, not in a comment, etc)
		char rc;
		
		// previous last significant (real) char in the line
		char prc;
		
		int curLineNr;
		int cursor;
		
		// Constructors
		public CSharpIndentEngine ()
		{
			stack = new IndentStack ();
			linebuf = new StringBuilder ();
			Reset ();
		}
		
		// Properties
		public int Position {
			get { return cursor; }
		}
		
		public int LineNumber {
			get { return curLineNr; }
		}
		
		public int LineOffset {
			get { return linebuf.Length; }
		}
		
		public bool NeedsReindent {
			get { return needsReindent; }
		}
		
		public int StackDepth {
			get { return stack.Count; }
		}
		
		public bool IsInsideVerbatimString {
			get { return stack.PeekInside (0) == Inside.VerbatimString; }
		}
		
		public bool IsInsideMultiLineComment {
			get { return stack.PeekInside (0) == Inside.MultiLineComment; }
		}
		
		public bool IsInsideDocLineComment {
			get { return stack.PeekInside (0) == Inside.DocComment; }
		}
		
		public bool LineBeganInsideVerbatimString {
			get { return beganInside == Inside.VerbatimString; }
		}
		
		public bool LineBeganInsideMultiLineComment {
			get { return beganInside == Inside.MultiLineComment; }
		}
		
		string TabsToSpaces (string indent)
		{
			StringBuilder builder;
			int width;
			
			if (indent == String.Empty)
				return String.Empty;
			
			builder = new StringBuilder ();
			width = TextEditorProperties.TabIndent;
			for (int i = 0; i < indent.Length; i++) {
				if (indent[i] == '\t')
					builder.Append (' ', width);
				else
					builder.Append (indent[i]);
			}
			
			return builder.ToString ();
		}
		
		public string ThisLineIndent {
			get {
				if (TextEditorProperties.ConvertTabsToSpaces)
					return TabsToSpaces (curIndent);
				
				return curIndent;
			}
		}
		
		public string NewLineIndent {
			get {
				if (TextEditorProperties.ConvertTabsToSpaces)
					return TabsToSpaces (stack.PeekIndent (0));
				
				return stack.PeekIndent (0);
			}
		}
		
		// Methods
		
		// Resets the CSharpIndentEngine state machine
		public void Reset ()
		{
			stack.Reset ();
			
			linebuf.Length = 0;
			
			keyword = String.Empty;
			curIndent = String.Empty;
			
			needsReindent = false;
			popVerbatim = false;
			canBeLabel = true;
			isEscaped = false;
			
			firstNonLwsp = -1;
			lastNonLwsp = -1;
			wordStart = -1;
			
			prc = '\0';
			pc = '\0';
			rc = '\0';
			
			curLineNr = 1;
			cursor = 0;
		}
		
		// Clone the CSharpIndentEngine - useful if a consumer of this class wants
		// to test things w/o changing the real indent engine state
		public object Clone ()
		{
			CSharpIndentEngine engine = new CSharpIndentEngine ();
			
			engine.stack = (IndentStack) stack.Clone ();
			engine.linebuf = new StringBuilder (linebuf.ToString (), linebuf.Capacity);
			
			engine.keyword = keyword;
			engine.curIndent = curIndent;
			
			engine.needsReindent = needsReindent;
			engine.popVerbatim = popVerbatim;
			engine.canBeLabel = canBeLabel;
			engine.isEscaped = isEscaped;
			
			engine.firstNonLwsp = firstNonLwsp;
			engine.lastNonLwsp = lastNonLwsp;
			engine.wordStart = wordStart;
			
			engine.prc = prc;
			engine.pc = pc;
			engine.rc = rc;
			
			engine.curLineNr = curLineNr;
			engine.cursor = cursor;
			
			return engine;
		}
		
		void TrimIndent ()
		{
			switch (stack.PeekInside (0)) {
			case Inside.FoldedStatement:
			case Inside.Block:
			case Inside.Case:
				if (curIndent == String.Empty)
					return;
				
				// chop off the last tab (e.g. unindent 1 level)
				curIndent = curIndent.Substring (0, curIndent.Length - 1);
				break;
			default:
				curIndent = stack.PeekIndent (0);
				break;
			}
		}
		
		// Check to see if @keyword is a "special" keyword - e.g. loop/if/else
		// constructs that we always want to indent if folded
		static bool KeywordIsSpecial (string keyword)
		{
			string[] specials = new string [] {
				"foreach",
				"while",
				"for",
				"else",
				"if"
			};
			
			for (int i = 0; i < specials.Length; i++) {
				if (keyword == specials[i])
					return true;
			}
			
			return false;
		}
		
		// Check to see if linebuf contains a keyword we're interested in (not all keywords)
		string WordIsKeyword ()
		{
			string str = linebuf.ToString (wordStart, linebuf.Length - wordStart);
			string[] keywords = new string [] {
				"namespace",
				"interface",
				"struct",
				"class",
				"enum",
				"switch",
				"case",
				"foreach",
				"while",
				"for",
				"do",
				"else",
				"if",
				"base",
				"this",
				"="
			};
			
			for (int i = 0; i < keywords.Length; i++) {
				if (str == keywords[i])
					return keywords[i];
			}
			
			return null;
		}
		
		bool WordIsDefault ()
		{
			string str = linebuf.ToString (wordStart, linebuf.Length - wordStart).Trim ();
			
			return str == "default";
		}
		
		//directive keywords that we care about
		static string[] directiveKeywords = new string [] {"region", "endregion" };
		
		string GetDirectiveKeyword (char currentChar)
		{
			string str = linebuf.ToString ().TrimStart ().Substring (1);
			
			if (str.Length == 0)
				return null;
			
			for (int i = 0; i < directiveKeywords.Length; i++) {
				if (directiveKeywords[i].StartsWith (str)) {
					if (str + currentChar == directiveKeywords[i])
						return directiveKeywords[i];
					else
						return null;
				}
			}
			
			return string.Empty;
		}
		
		bool Folded2LevelsNonSpecial ()
		{
			return stack.PeekInside (0) == Inside.FoldedStatement &&
				stack.PeekInside (1) == Inside.FoldedStatement &&
				!KeywordIsSpecial (stack.PeekKeyword (0)) &&
				!KeywordIsSpecial (keyword);
		}
		
		bool FoldedClassDeclaration ()
		{
			return stack.PeekInside (0) == Inside.FoldedStatement &&
				(keyword == "base" || keyword == "class" || keyword == "interface");
		}
		
		void PushFoldedStatement ()
		{
			string indent = null;
			
			// Note: nesting of folded statements stops after 2 unless a "special" folded
			// statement is introduced, in which case the cycle restarts
			//
			// Note: We also try to only fold class declarations once
			
			if (Folded2LevelsNonSpecial () || FoldedClassDeclaration ())
				indent = stack.PeekIndent (0);
			
			if (indent != null)
				stack.Push (Inside.FoldedStatement, keyword, curLineNr, 0, indent);
			else
				stack.Push (Inside.FoldedStatement, keyword, curLineNr, 0);
			
			keyword = String.Empty;
		}
		
		// Handlers for specific characters
		void PushHash (Inside inside)
		{
			// ignore if we are inside a string, char, or comment
			if ((inside & (Inside.StringOrChar | Inside.Comment)) != 0)
				return;
			
			// ignore if '#' is not the first significant char on the line
			if (rc != '\0')
				return;
			
			stack.Push (Inside.PreProcessor, null, curLineNr, 0);
			
			curIndent = String.Empty;
			needsReindent = true;
		}
		
		void PushSlash (Inside inside)
		{
			// ignore these
			if ((inside & (Inside.PreProcessor | Inside.StringOrChar )) != 0)
				return;
			if (inside == Inside.LineComment) {
				stack.Pop (); // pop line comment
				stack.Push (Inside.DocComment, keyword, curLineNr, 0);
			} else if (inside == Inside.MultiLineComment) {
				// check for end of multi-line comment block
				if (pc == '*') {
					// restore the keyword and pop the multiline comment
					keyword = stack.PeekKeyword (0);
					stack.Pop ();
				}
			} else {
				
				// FoldedStatement, Block, Attribute or ParenList
				// check for the start of a single-line comment
				if (pc == '/') {
					stack.Push (Inside.LineComment, keyword, curLineNr, 0);
					
					// drop the previous '/': it belongs to this comment
					rc = prc;
				}
			}
		}
		
		void PushBackSlash (Inside inside)
		{
			// string and char literals can have \-escapes
			if ((inside & (Inside.StringLiteral | Inside.CharLiteral)) != 0)
				isEscaped = !isEscaped;
		}
		
		void PushStar (Inside inside)
		{
			int n;
			
			if (pc != '/')
				return;
			
			// got a "/*" - might start a MultiLineComment
			if ((inside & (Inside.StringOrChar | Inside.Comment)) != 0) {
				if ((inside & Inside.MultiLineComment) != 0)
					Console.WriteLine ("Watch out! Nested /* */ comment detected!");
				return;
			}
			
			// push a new multiline comment onto the stack
			if (inside != Inside.PreProcessor)
				n = linebuf.Length - firstNonLwsp;
			else
				n = linebuf.Length;
			
			stack.Push (Inside.MultiLineComment, keyword, curLineNr, n);
			
			// drop the previous '/': it belongs to this comment block
			rc = prc;
		}
		
		void PushQuote (Inside inside)
		{
			Inside type;
			
			// ignore if in these
			if ((inside & (Inside.PreProcessor | Inside.Comment | Inside.CharLiteral)) != 0)
				return;
			
			if (inside == Inside.VerbatimString) {
				if (popVerbatim) {
					// back in the verbatim-string-literal token
					popVerbatim = false;
				} else {
					/* need to see the next char before we pop the
					 * verbatim-string-literal */
					popVerbatim = true;
				}
			} else if (inside == Inside.StringLiteral) {
				// check that it isn't escaped
				if (!isEscaped) {
					keyword = stack.PeekKeyword (0);
					stack.Pop ();
				}
			} else {
				// FoldedStatement, Block, Attribute or ParenList
				if (pc == '@')
					type = Inside.VerbatimString;
				else
					type = Inside.StringLiteral;
				
				// push a new string onto the stack
				stack.Push (type, keyword, curLineNr, 0);
			}
		}
		
		void PushSQuote (Inside inside)
		{
			if (inside == Inside.CharLiteral) {
				// check that it's not escaped
				if (isEscaped)
					return;
				
				keyword = stack.PeekKeyword (0);
				stack.Pop ();
				return;
			}
			
			if ((inside & (Inside.PreProcessor | Inside.String | Inside.Comment)) != 0) {
				// won't be starting a CharLiteral, so ignore it
				return;
			}
			
			// push a new char literal onto the stack 
			stack.Push (Inside.CharLiteral, keyword, curLineNr, 0);
		}
		
		void PushColon (Inside inside)
		{
			if (inside != Inside.Block && inside != Inside.Case)
				return;
			
			// can't be a case/label if there's no preceeding text
			if (wordStart == -1)
				return;
			
			// goto-label or case statement
			if (keyword == "case" || keyword == "default") {
				// case (or default) statement
				if (stack.PeekKeyword (0) != "switch")
					return;
				
				if (inside == Inside.Case) {
					stack.Pop ();
					
					string newIndent = stack.PeekIndent (0);
					if (curIndent != newIndent) {
						curIndent = newIndent;
						needsReindent = true;
					}
				}
				
				if (!FormattingProperties.IndentCaseLabels) {
					needsReindent = true;
					TrimIndent ();
				}
				
				stack.Push (Inside.Case, "switch", curLineNr, 0);
			} else if (canBeLabel) {
				GotoLabelIndentStyle style = FormattingProperties.GotoLabelIndentStyle;
				
				// indent goto labels as specified
				switch (style) {
				case GotoLabelIndentStyle.LeftJustify:
					needsReindent = true;
					curIndent = " ";
					break;
				case GotoLabelIndentStyle.OneLess:
					needsReindent = true;
					TrimIndent ();
					curIndent += " ";
					break;
				default:
					break;
				}
				
				canBeLabel = false;
			} else if (pc == ':') {
				// :: operator, need to undo the "unindent label" operation we did for the previous ':'
				curIndent = stack.PeekIndent (0);
				needsReindent = true;
			}
		}
		
		void PushSemicolon (Inside inside)
		{
			if ((inside & (Inside.PreProcessor | Inside.StringOrChar | Inside.Comment)) != 0)
				return;
			
			if (inside == Inside.FoldedStatement) {
				// chain-pop folded statements
				while (stack.PeekInside (0) == Inside.FoldedStatement)
					stack.Pop ();
			}
			
			keyword = String.Empty;
		}
		
		void PushOpenSq (Inside inside)
		{
			int n = 1;
			
			if ((inside & (Inside.PreProcessor | Inside.StringOrChar | Inside.Comment)) != 0)
				return;
			
			// push a new attribute onto the stack
			if (firstNonLwsp != -1)
				n += linebuf.Length - firstNonLwsp;
			
			stack.Push (Inside.Attribute, keyword, curLineNr, n);
		}
		
		void PushCloseSq (Inside inside)
		{
			if ((inside & (Inside.PreProcessor | Inside.StringOrChar | Inside.Comment)) != 0)
				return;
			
			if (inside != Inside.Attribute) {
				//Console.WriteLine ("can't pop a '[' if we ain't got one?");
				return;
			}
			
			// pop this attribute off the stack
			keyword = stack.PeekKeyword (0);
			stack.Pop ();
		}
		
		void PushOpenParen (Inside inside)
		{
			int n = 1;
			
			if ((inside & (Inside.PreProcessor | Inside.StringOrChar | Inside.Comment)) != 0)
				return;
			
			// push a new paren list onto the stack
			if (firstNonLwsp != -1)
				n += linebuf.Length - firstNonLwsp;
			
			stack.Push (Inside.ParenList, keyword, curLineNr, n);
			
			keyword = String.Empty;
		}
		
		void PushCloseParen (Inside inside)
		{
			if ((inside & (Inside.PreProcessor | Inside.StringOrChar | Inside.Comment)) != 0)
				return;
			
			if (inside != Inside.ParenList) {
				//Console.WriteLine ("can't pop a '(' if we ain't got one?");
				return;
			}
			
			// pop this paren list off the stack
			keyword = stack.PeekKeyword (0);
			stack.Pop ();
		}
		
		void PushOpenBrace (Inside inside)
		{
			if ((inside & (Inside.PreProcessor | Inside.StringOrChar | Inside.Comment)) != 0)
				return;
			
			// push a new block onto the stack
			if (inside == Inside.FoldedStatement) {
				string pKeyword;
				
				if (firstNonLwsp == -1) {
					pKeyword = stack.PeekKeyword (0);
					stack.Pop ();
				} else {
					pKeyword = keyword;
				}
				
				while (true) {
					if (stack.PeekInside (0) != Inside.FoldedStatement)
						break;
					string kw = stack.PeekKeyword (0);
					stack.Pop ();
					TrimIndent ();
					if (!string.IsNullOrEmpty (kw)) {
						pKeyword = kw;
						break;
					}
				}
				
				if (firstNonLwsp == -1)
					curIndent = stack.PeekIndent (0);
				
				stack.Push (Inside.Block, pKeyword, curLineNr, 0);
			} else if (inside == Inside.Case && (keyword == "default" || keyword == "case")) {
				if (curLineNr == stack.PeekLineNr (0) || firstNonLwsp == -1) {
					// e.g. "case 0: {" or "case 0:\n{"
					stack.Push (Inside.Block, keyword, curLineNr, -1);
					
					if (firstNonLwsp == -1)
						TrimIndent ();
				} else {
					stack.Push (Inside.Block, keyword, curLineNr, 0);
				}
			} else {
				stack.Push (Inside.Block, keyword, curLineNr, 0);
			}
			
			keyword = String.Empty;
			if (firstNonLwsp == -1)
				needsReindent = true;
		}
		
		void PushCloseBrace (Inside inside)
		{
			if ((inside & (Inside.PreProcessor | Inside.StringOrChar | Inside.Comment)) != 0)
				return;
			
			if (inside != Inside.Block && inside != Inside.Case) {
				//Console.WriteLine ("can't pop a '{' if we ain't got one?");
				return;
			}
			
			if (inside == Inside.Case) {
				curIndent = stack.PeekIndent (1);
				keyword = stack.PeekKeyword (0);
				inside = stack.PeekInside (1);
				stack.Pop ();
			}
			
			// pop this block off the stack
			keyword = stack.PeekKeyword (0);
			if (keyword != "case" && keyword != "default")
				keyword = String.Empty;
			
			stack.Pop ();
			
			while (stack.PeekInside (0) == Inside.FoldedStatement)
				stack.Pop ();
			
			if (firstNonLwsp == -1) {
				needsReindent = true;
				TrimIndent ();
			}
		}
		
		void PushNewLine (Inside inside)
		{
		 top:
			switch (inside) {
			case Inside.PreProcessor:
				// pop the preprocesor state unless the eoln is escaped
				if (rc != '\\') {
					keyword = stack.PeekKeyword (0);
					stack.Pop ();
				}
				break;
			case Inside.MultiLineComment:
				// nothing to do
				break;
			case Inside.DocComment:
			case Inside.LineComment:
				// pop the line comment
				keyword = stack.PeekKeyword (0);
				stack.Pop ();
				
				inside = stack.PeekInside (0);
				goto top;
			case Inside.VerbatimString:
				// nothing to do
				break;
			case Inside.StringLiteral:
				if (isEscaped) {
					/* I don't think c# allows breaking a
					 * normal string across lines even
					 * when escaping the carriage
					 * return... but how else should we
					 * handle this? */
					break;
				}
				
				/* not escaped... error!! but what can we do,
				 * eh? allow folding across multiple lines I
				 * guess... */
				break;
			case Inside.CharLiteral:
				/* this is an error... what to do? guess we'll
				 * just pretend it never happened */
				break;
			case Inside.Attribute:
				// nothing to do
				break;
			case Inside.ParenList:
				// nothing to do
				break;
			default:
				// Empty, FoldedStatement, and Block
				switch (rc) {
				case '\0':
					// nothing entered on this line
					break;
				case ':':
					canBeLabel = canBeLabel && inside != Inside.FoldedStatement;
					
					if ((keyword == "default" || keyword == "case") || canBeLabel)
						break;
					
					PushFoldedStatement ();
					break;
				case '[':
					// handled elsewhere
					break;
				case ']':
					// handled elsewhere
					break;
				case '(':
					// handled elsewhere
					break;
				case '{':
					// handled elsewhere
					break;
				case '}':
					// handled elsewhere
					break;
				case ';':
					// handled elsewhere
					break;
				case ',':
					// avoid indenting if we are in a list
					break;
				default:
					if (stack.PeekLineNr (0) == curLineNr) {
						// is this right? I don't remember why I did this...
						break;
					}
					
					if (inside == Inside.Block) {
						if (stack.PeekKeyword (0) == "struct" ||
						    stack.PeekKeyword (0) == "enum" ||
						    stack.PeekKeyword (0) == "=") {
							// just variable/value declarations
							break;
						}
					}
					
					PushFoldedStatement ();
					break;
				}
				
				break;
			}
			
			linebuf.Length = 0;
			
			beganInside = stack.PeekInside (0);
			curIndent = stack.PeekIndent (0);
			
			canBeLabel = true;
			
			firstNonLwsp = -1;
			lastNonLwsp = -1;
			wordStart = -1;
			
			prc = '\0';
			pc = '\0';
			rc = '\0';
			
			curLineNr++;
			cursor++;
		}
		
		// This is the main logic of this class...
		public void Push (char c)
		{
			Inside inside, after;
			
			inside = stack.PeekInside (0);
			
			// pop the verbatim-string-literal
			if (inside == Inside.VerbatimString && popVerbatim && c != '"') {
				keyword = stack.PeekKeyword (0);
				popVerbatim = false;
				stack.Pop ();
				
				inside = stack.PeekInside (0);
			}
			
			needsReindent = false;
			
			if ((inside & (Inside.PreProcessor | Inside.StringOrChar | Inside.Comment)) == 0 &&
			    wordStart != -1) {
				if (char.IsWhiteSpace (c) || c == '(' || c == '{') {
					string tmp = WordIsKeyword ();
					if (tmp != null)
						keyword = tmp;
				} else if (c == ':' && WordIsDefault ()) {
					keyword = "default";
				}	
			//get the keyword for preprocessor directives
			} else if ((inside & (Inside.PreProcessor)) != 0 && stack.PeekKeyword (0) == null) {
				//replace the stack item with a keyworded one
				string preProcessorKeyword = GetDirectiveKeyword (c);
				int peekLine = stack.PeekLineNr (0);
				stack.Pop ();
				stack.Push (Inside.PreProcessor, preProcessorKeyword, peekLine, 0);
				
				//regions need to pop back out
				if (preProcessorKeyword == "region" || preProcessorKeyword == "endregion") {
					curIndent = stack.PeekIndent (0);
					needsReindent = true;
				}
			}
			
			//Console.WriteLine ("Pushing '{0}'; wordStart = {1}; keyword = {2}", c, wordStart, keyword);
			
			switch (c) {
			case '#':
				PushHash (inside);
				break;
			case '/':
				PushSlash (inside);
				break;
			case '\\':
				PushBackSlash (inside);
				break;
			case '*':
				PushStar (inside);
				break;
			case '"':
				PushQuote (inside);
				break;
			case '\'':
				PushSQuote (inside);
				break;
			case ':':
				PushColon (inside);
				break;
			case ';':
				PushSemicolon (inside);
				break;
			case '[':
				PushOpenSq (inside);
				break;
			case ']':
				PushCloseSq (inside);
				break;
			case '(':
				PushOpenParen (inside);
				break;
			case ')':
				PushCloseParen (inside);
				break;
			case '{':
				PushOpenBrace (inside);
				break;
			case '}':
				PushCloseBrace (inside);
				break;
			case '\n':
				PushNewLine (inside);
				return;
			default:
				break;
			}
			
			after = stack.PeekInside (0);
			if ((after & (Inside.PreProcessor | Inside.StringOrChar | Inside.Comment)) == 0) {
				if (!Char.IsWhiteSpace (c)) {
					if (firstNonLwsp == -1)
						firstNonLwsp = linebuf.Length;
					
					if (wordStart != -1 && c != ':' && Char.IsWhiteSpace (pc)) {
						// goto labels must be single word tokens
						canBeLabel = false;
					} else if (wordStart == -1 && Char.IsDigit (c)) {
						// labels cannot start with a digit
						canBeLabel = false;
					}
					
					lastNonLwsp = linebuf.Length;
					
					if (c != ':') {
						if (Char.IsWhiteSpace (pc) || rc == ':')
							wordStart = linebuf.Length;
						else if (pc == '\0')
							wordStart = 0;
					}
				}
			} else if (c != '\\' && (after & (Inside.StringLiteral | Inside.CharLiteral)) != 0) {
				// Note: PushBackSlash() will handle untoggling isEscaped if c == '\\'
				isEscaped = false;
			}
			
			pc = c;
			prc = rc;
			// Note: even though PreProcessor directive chars are insignificant, we do need to
			//       check for rc != '\\' at the end of a line.
			if ((inside & Inside.Comment) == 0 &&
			    (after & Inside.Comment) == 0 &&
			    !Char.IsWhiteSpace (c))
				rc = c;
			
			linebuf.Append (c);
			
			cursor++;
		}
		
		public void Debug ()
		{
			Console.WriteLine ("\ncurLine = {0}", linebuf);
			Console.WriteLine ("curLineNr = {0}\ncursor = {1}\nneedsReindent = {2}",
			                   curLineNr, cursor, needsReindent);
			Console.WriteLine ("stack:");
			for (int i = 0; i < stack.Count; i++) {
				switch (stack.PeekInside (i)) {
				case Inside.PreProcessor:
					Console.WriteLine ("\tpreprocessor directive");
					break;
				case Inside.MultiLineComment:
					Console.WriteLine ("\t/* */ comment block");
					break;
				case Inside.LineComment:
					Console.WriteLine ("\t// comment");
					break;
				case Inside.VerbatimString:
					Console.WriteLine ("\tverbatim string");
					break;
				case Inside.StringLiteral:
					Console.WriteLine ("\tstring literal");
					break;
				case Inside.CharLiteral:
					Console.WriteLine ("\tchar literal");
					break;
				case Inside.Attribute:
					Console.WriteLine ("\t[ ] attribute");
					break;
				case Inside.ParenList:
					Console.WriteLine ("\t( ) paren list");
					break;
				case Inside.FoldedStatement:
					if (stack.PeekKeyword (i) != String.Empty)
						Console.WriteLine ("\t{0}-statement", stack.PeekKeyword (i));
					else
						Console.WriteLine ("\tfolded statement?");
					break;
				case Inside.Case:
					Console.WriteLine ("\tcase statement");
					break;
				case Inside.Block:
					if (stack.PeekKeyword (i) != String.Empty)
						Console.WriteLine ("\t{0} {1} block", stack.PeekKeyword (i), "{ }");
					else
						Console.WriteLine ("\tmethod {0} block?", "{ }");
					break;
				}
			}
		}
	}
}
