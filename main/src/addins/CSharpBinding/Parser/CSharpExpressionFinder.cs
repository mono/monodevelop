//  ExpressionFinder.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Daniel Grunwald <daniel@danielgrunwald.de>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.CSharpBinding
{
	/// <summary>
	/// Description of ExpressionFinder.
	/// </summary>
	public class CSharpExpressionFinder : IExpressionFinder
	{
		public CSharpExpressionFinder ()
		{
		}
		
		#region Capture Context
		ExpressionResult CreateResult(string expression, string inText, int offset)
		{
			if (expression == null)
				return new ExpressionResult(null);
			if (expression.StartsWith("using "))
				return new ExpressionResult(expression.Substring(6).TrimStart(), DomRegion.Empty, ExpressionContext.Namespace);
			if (!hadParenthesis && expression.StartsWith("new ")) {
				return new ExpressionResult(expression.Substring(4).TrimStart(), DomRegion.Empty, GetCreationContext());
			}
			if (IsInAttribute(inText, offset))
				return new ExpressionResult(expression, DomRegion.Empty, ExpressionContext.Attribute);
			return new ExpressionResult(expression);
		}
		
		ExpressionContext GetCreationContext()
		{
			return null;
/*			UnGetToken();
			if (GetNextNonWhiteSpace() == '=') { // was: "= new"
				ReadNextToken();
				if (curTokenType == Ident) {     // was: "ident = new"
					int typeEnd = offset;
					ReadNextToken();
					int typeStart = -1;
					while (curTokenType == Ident) {
						typeStart = offset + 1;
						ReadNextToken();
						if (curTokenType == Dot) {
							ReadNextToken();
						} else {
							break;
						}
					}
					if (typeStart >= 0) {
						string className = text.Substring(typeStart, typeEnd - typeStart);
						int pos = className.IndexOf('<');
						string nonGenericClassName, genericPart;
						int typeParameterCount = 0;
						if (pos > 0) {
							nonGenericClassName = className.Substring(0, pos);
							genericPart = className.Substring(pos);
							pos = 0;
							do {
								typeParameterCount += 1;
								pos = genericPart.IndexOf(',', pos + 1);
							} while (pos > 0);
						} else {
							nonGenericClassName = className;
							genericPart = null;
						}
						ClassFinder finder = new ClassFinder(fileName, text, typeStart);
						IReturnType t = finder.SearchType(nonGenericClassName, typeParameterCount);
						IType c = (t != null) ? t.GetUnderlyingClass() : null;
						if (c != null) {
							ExpressionContext context = ExpressionContext.TypeDerivingFrom(c, true);
							if (context.ShowEntry(c)) {
								if (genericPart != null) {
									DefaultClass genericClass = new DefaultClass(c.CompilationUnit, c.ClassType, c.Modifiers, c.Region, c.DeclaringType);
									genericClass.FullyQualifiedName = c.FullyQualifiedName + genericPart;
									genericClass.Documentation = c.Documentation;
									context.SuggestedItem = genericClass;
								} else {
									context.SuggestedItem = c;
								}
							}
							return context;
						}
					}
				}
			} else {
				UnGet();
				ReadNextToken();
				if (curTokenType == Ident && lastIdentifier == "throw") {
					return ExpressionContext.TypeDerivingFrom(ProjectContentRegistry.Mscorlib.GetClass("System.Exception"), true);
				}
			}

			return ExpressionContext.ObjectCreation;*/
		}
		
		bool IsInAttribute(string txt, int offset)
		{
			// Get line start:
			int lineStart = offset;
			while (--lineStart > 0 && txt[lineStart] != '\n')
			{}
			
			bool inAttribute = false;
			int parens = 0;
			for (int i = lineStart + 1; i < offset; i++) {
				char ch = txt[i];
				if (char.IsWhiteSpace(ch))
					continue;
				if (!inAttribute) {
					// outside attribute
					if (ch == '[')
						inAttribute = true;
					else
						return false;
				} else if (parens == 0) {
					// inside attribute, outside parameter list
					if (ch == ']')
						inAttribute = false;
					else if (ch == '(')
						parens = 1;
					else if (!char.IsLetterOrDigit(ch) && ch != ',')
						return false;
				} else {
					// inside attribute, inside parameter list
					if (ch == '(')
						parens++;
					else if (ch == ')')
						parens--;
				}
			}
			return inAttribute && parens == 0;
		}
		#endregion
		
		#region RemoveLastPart
		/// <summary>
		/// Removed the last part of the expression.
		/// </summary>
		/// <example>
		/// "arr[i]" => "arr"
		/// "obj.Field" => "obj"
		/// "obj.Method(args,...)" => "obj.Method"
		/// </example>
		public string RemoveLastPart(string expression)
		{
			text = expression;
			offset = text.Length - 1;
			ReadNextToken();
			if (curTokenType == Ident && Peek() == '.')
				GetNext();
			return text.Substring(0, offset + 1);
		}
		#endregion
		
		#region Find Expression
		public ExpressionResult FindExpression(string inText, int offset)
		{
			inText = FilterComments(inText, ref offset);
			return CreateResult(FindExpressionInternal(inText, offset), inText, offset);
		}
		
		public string FindExpressionInternal(string inText, int offset)
		{
			// warning: Do not confuse this.offset and offset
			this.text = inText;
			this.offset = this.lastAccept = offset;
			this.state  = START;
			hadParenthesis = false;
			if (this.text == null) {
				return null;
			}
			
			while (state != ERROR) {
				ReadNextToken();
				state = stateTable[state, curTokenType];
				
				if (state == ACCEPT || state == ACCEPT2) {
					lastAccept = this.offset;
				}
				if (state == ACCEPTNOMORE) {
					lastExpressionStartPosition = this.offset + 1;
					return this.text.Substring(this.offset + 1, offset - this.offset);
				}
			}
			
			if (lastAccept < 0)
				return null;
			
			lastExpressionStartPosition = this.lastAccept + 1;
			
			return this.text.Substring(this.lastAccept + 1, offset - this.lastAccept);
		}
		
		int lastExpressionStartPosition;
		
		internal int LastExpressionStartPosition {
			get {
				return lastExpressionStartPosition;
			}
		}
		#endregion
		
		#region FindFullExpression
		public ExpressionResult FindFullExpression(string inText, int offset)
		{
			if (inText == null)
				return new ExpressionResult (null);
			int offsetWithoutComments = offset;
			string textWithoutComments = FilterComments(inText, ref offsetWithoutComments);
			string expressionBeforeOffset = FindExpressionInternal(textWithoutComments, offsetWithoutComments);
			if (expressionBeforeOffset == null || expressionBeforeOffset.Length == 0)
				return CreateResult(null, textWithoutComments, offsetWithoutComments);
			StringBuilder b = new StringBuilder(expressionBeforeOffset);
			// append characters after expression
			bool wordFollowing = false;
			int i;
			for (i = offset + 1; i < inText.Length; ++i) {
				char c = inText[i];
				if (Char.IsLetterOrDigit(c) || c == '_') {
					if (Char.IsWhiteSpace(inText, i - 1)) {
						wordFollowing = true;
						break;
					}
					b.Append(c);
				} else if (Char.IsWhiteSpace(c)) {
					// ignore whitespace
				} else if (c == '(' || c == '[') {
					int otherBracket = SearchBracketForward(inText, i + 1, c, (c == '(') ? ')' : ']');
					if (otherBracket < 0)
						break;
					if (c == '[') {
						// do not include [] when it is an array declaration (versus indexer call)
						bool ok = false;
						for (int j = i + 1; j < otherBracket; j++) {
							if (inText[j] != ',' && !char.IsWhiteSpace(inText, j)) {
								ok = true;
								break;
							}
						}
						if (!ok) {
							break;
						}
					}
					b.Append(inText, i, otherBracket - i + 1);
					break;
				} else if (c == '<') {
					// accept only if this is a generic type reference
					int typeParameterEnd = FindEndOfTypeParameters(inText, i);
					if (typeParameterEnd < 0)
						break;
					b.Append(inText, i, typeParameterEnd - i + 1);
					i = typeParameterEnd;
				} else {
					break;
				}
			}
			ExpressionResult res = CreateResult(b.ToString(), textWithoutComments, offsetWithoutComments);
			if (res.ExpressionContext == ExpressionContext.Default && wordFollowing) {
				b = new StringBuilder();
				for (; i < inText.Length; ++i) {
					char c = inText[i];
					if (char.IsLetterOrDigit(c) || c == '_')
						b.Append(c);
					else
						break;
				}
				if (b.Length > 0) {
					if (ICSharpCode.NRefactory.Parser.CSharp.Keywords.GetToken(b.ToString()) < 0) {
						res.ExpressionContext = ExpressionContext.Type;
					}
				}
			}
			return res;
		}
		
		int FindEndOfTypeParameters(string inText, int offset)
		{
			int level = 0;
			for (int i = offset; i < inText.Length; ++i) {
				char c = inText[i];
				if (Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c)) {
					// ignore identifiers and whitespace
				} else if (c == ',' || c == '?' || c == '[' || c == ']') {
					// ,  : seperating generic type parameters
					// ?  : nullable types
					// [] : arrays
				} else if (c == '<') {
					++level;
				} else if (c == '>') {
					--level;
				} else {
					return -1;
				}
				if (level == 0)
					return i;
			}
			return -1;
		}
		#endregion
		
		#region SearchBracketForward
		// like CSharpFormattingStrategy.SearchBracketForward, but operates on a string.
		private int SearchBracketForward(string text, int offset, char openBracket, char closingBracket)
		{
			bool inString = false;
			bool inChar   = false;
			bool verbatim = false;
			
			bool lineComment  = false;
			bool blockComment = false;
			
			if (offset < 0) return -1;
			
			int brackets = 1;
			
			for (; offset < text.Length; ++offset) {
				char ch = text[offset];
				switch (ch) {
					case '\r':
					case '\n':
						lineComment = false;
						inChar = false;
						if (!verbatim) inString = false;
						break;
					case '/':
						if (blockComment) {
							if (offset > 0 && text[offset - 1] == '*') {
								blockComment = false;
							}
						}
						if (!inString && !inChar && offset + 1 < text.Length) {
							if (!blockComment && text[offset + 1] == '/') {
								lineComment = true;
							}
							if (!lineComment && text[offset + 1] == '*') {
								blockComment = true;
							}
						}
						break;
					case '"':
						if (!(inChar || lineComment || blockComment)) {
							if (inString && verbatim) {
								if (offset + 1 < text.Length && text[offset + 1] == '"') {
									++offset; // skip escaped quote
									inString = false; // let the string go on
								} else {
									verbatim = false;
								}
							} else if (!inString && offset > 0 && text[offset - 1] == '@') {
								verbatim = true;
							}
							inString = !inString;
						}
						break;
					case '\'':
						if (!(inString || lineComment || blockComment)) {
							inChar = !inChar;
						}
						break;
					case '\\':
						if ((inString && !verbatim) || inChar)
							++offset; // skip next character
						break;
					default:
						if (ch == openBracket) {
							if (!(inString || inChar || lineComment || blockComment)) {
								++brackets;
							}
						} else if (ch == closingBracket) {
							if (!(inString || inChar || lineComment || blockComment)) {
								--brackets;
								if (brackets == 0) {
									return offset;
								}
							}
						}
						break;
				}
			}
			return -1;
		}
		#endregion
		
		#region Comment Filter and 'inside string watcher'
		int initialOffset;
		public string FilterComments(string text, ref int offset)
		{
			if (text == null || text.Length <= offset)
				return null;
			this.initialOffset = offset;
			StringBuilder result = new StringBuilder();
			int curOffset = 0;
			
			while (curOffset <= initialOffset) {
				char ch = text[curOffset];
				
				switch (ch) {
					case '@':
						if (curOffset + 1 < text.Length && text[curOffset + 1] == '"') {
							result.Append(text[curOffset++]); // @
							result.Append(text[curOffset++]); // "
							if (!ReadVerbatimString(result, text, ref curOffset)) {
								return null;
							}
						}else{
							result.Append(ch);
							++curOffset;
						}
						break;
					case '\'':
						result.Append(ch);
						curOffset++;
						if(! ReadChar(result, text, ref curOffset)) {
							return null;
						}
						break;
					case '"':
						result.Append(ch);
						curOffset++;
						if (!ReadString(result, text, ref curOffset)) {
							return null;
						}
						break;
					case '/':
						if (curOffset + 1 < text.Length && text[curOffset + 1] == '/') {
							offset    -= 2;
							curOffset += 2;
							if (!ReadToEOL(text, ref curOffset, ref offset)) {
								return null;
							}
						} else if (curOffset + 1 < text.Length && text[curOffset + 1] == '*') {
							offset    -= 2;
							curOffset += 2;
							if (!ReadMultiLineComment(text, ref curOffset, ref offset)) {
								return null;
							}
						} else {
							goto default;
						}
						break;
					case '#':
						if (!ReadToEOL(text, ref curOffset, ref offset)) {
							return null;
						}
						break;
					default:
						result.Append(ch);
						++curOffset;
						break;
				}
			}
			
			return result.ToString();
		}
		
		bool ReadToEOL(string text, ref int curOffset, ref int offset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				--offset;
				if (ch == '\n') {
					return true;
				}
			}
			return false;
		}
		
		bool ReadChar(StringBuilder outText, string text, ref int curOffset)
		{
			if (curOffset > initialOffset)
				return false;
			char first = text[curOffset++];
			outText.Append(first);
			if (curOffset > initialOffset)
				return false;
			char second = text[curOffset++];
			outText.Append(second);
			if (first == '\\') {
				// character is escape sequence, so read one char more
				char next;
				do {
					if (curOffset > initialOffset)
						return false;
					next = text[curOffset++];
					outText.Append(next);
					// unicode or hexadecimal character literals can have more content characters
				} while((second == 'u' || second == 'x') && char.IsLetterOrDigit(next));
			}
			return text[curOffset - 1] == '\'';
		}
		
		bool ReadString(StringBuilder outText, string text, ref int curOffset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				outText.Append(ch);
				if (ch == '"') {
					return true;
				} else if (ch == '\\') {
					if (curOffset <= initialOffset)
						outText.Append(text[curOffset++]);
				}
			}
			return false;
		}
		
		bool ReadVerbatimString(StringBuilder outText, string text, ref int curOffset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				outText.Append(ch);
				if (ch == '"') {
					if (curOffset < text.Length && text[curOffset] == '"') {
						outText.Append(text[curOffset++]);
					} else {
						return true;
					}
				}
			}
			return false;
		}
		
		bool ReadMultiLineComment(string text, ref int curOffset, ref int offset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				--offset;
				if (ch == '*') {
					if (curOffset < text.Length && text[curOffset] == '/') {
						++curOffset;
						--offset;
						return true;
					}
				}
			}
			return false;
		}
		#endregion
		
		#region mini backward lexer
		string text;
		int    offset;
		
		char GetNext()
		{
			if (offset >= 0) {
				return text[offset--];
			}
			return '\0';
		}
		
		char GetNextNonWhiteSpace()
		{
			char ch;
			do {
				ch = GetNext();
			} while (char.IsWhiteSpace(ch));
			return ch;
		}
		
		char Peek(int n)
		{
			if (offset - n >= 0) {
				return text[offset - n];
			}
			return '\0';
		}
		
		char Peek()
		{
			if (offset >= 0) {
				return text[offset];
			}
			return '\0';
		}
		
/*		void UnGet()
		{
			++offset;
		}
		
		void UnGetToken()
		{
			do {
				UnGet();
			} while (char.IsLetterOrDigit(Peek()));
		}
*/		
		// tokens for our lexer
		static int Err     = 0;
		static int Dot     = 1;
		static int StrLit  = 2;
		static int Ident   = 3;
		static int New     = 4;
		static int Bracket = 5;
		static int Parent  = 6;
		static int Curly   = 7;
		static int Using   = 8;
		static int Digit   = 9;
		int curTokenType;
		
//		readonly static string[] tokenStateName = new string[] {
//			"Err", "Dot", "StrLit", "Ident", "New", "Bracket", "Paren", "Curly", "Using", "Digit"
//		};
//		
		/// <summary>
		/// used to control whether an expression is in a ObjectCreation context (new *expr*),
		/// or is in the default context (e.g. "new MainForm().Show()", 'new ' is there part of the expression
		/// </summary>
		bool hadParenthesis;
		
		//string lastIdentifier;
		
		void ReadNextToken()
		{
			curTokenType = Err;
			char ch = GetNextNonWhiteSpace();
			if (ch == '\0') {
				return;
			}
			
			switch (ch) {
				case '}':
					if (ReadBracket('{', '}')) {
						curTokenType = Curly;
					}
					break;
				case ')':
					if (ReadBracket('(', ')')) {
						hadParenthesis = true;
						curTokenType = Parent;
					}
					break;
				case ']':
					if (ReadBracket('[', ']')) {
						curTokenType = Bracket;
					}
					break;
				case '>':
					if (ReadTypeParameters()) {
						// hack: ignore type parameters and continue reading without changing state
						ReadNextToken();
					}
					break;
				case '.':
					curTokenType = Dot;
					break;
				case ':':
					if (GetNext() == ':') {
						// treat :: like dot
						curTokenType = Dot;
					}
					break;
				case '\'':
				case '"':
					if (ReadStringLiteral(ch)) {
						curTokenType = StrLit;
					}
					break;
				default:
					if (IsNumber(ch)) {
						ReadDigit(ch);
						curTokenType = Digit;
					} else if (IsIdentifierPart(ch)) {
						string ident = ReadIdentifier(ch);
						if (ident != null) {
							switch (ident) {
								case "new":
									curTokenType = New;
									break;
								case "using":
									curTokenType = Using;
									break;
								case "return":
								case "throw":
								case "in":
								case "else":
									// treat as error / end of expression
									break;
								default:
									curTokenType = Ident;
									//lastIdentifier = ident;
									break;
							}
						}
					}
					
					break;
			}
		}
		bool IsNumber(char ch)
		{
			if (!Char.IsDigit(ch))
				return false;
			int n = 0;
			while (true) {
				ch = Peek(n);
				if (Char.IsDigit(ch)) {
					n++;
					continue;
				}
				return n > 0 && !Char.IsLetter(ch);
			}
		}
		bool ReadStringLiteral(char litStart)
		{
			while (true) {
				char ch = GetNext();
				if (ch == '\0') {
					return false;
				}
				if (ch == litStart) {
					if (Peek() == '@' && litStart == '"') {
						GetNext();
					}
					return true;
				}
			}
		}
		
		bool ReadTypeParameters()
		{
			int level = 1;
			while (level > 0) {
				char ch = GetNext();
				switch (ch) {
					case '?':
					case '[':
					case ',':
					case ']':
						break;
					case '<':
						--level;
						break;
					case '>':
						++level;
						break;
					default:
						if (!char.IsWhiteSpace(ch) && !char.IsLetterOrDigit(ch))
							return false;
						break;
				}
			}
			return true;
		}
		
		bool ReadBracket(char openBracket, char closingBracket)
		{
			int curlyBraceLevel    = 0;
			int squareBracketLevel = 0;
			int parenthesisLevel   = 0;
			switch (openBracket) {
				case '(':
					parenthesisLevel++;
					break;
				case '[':
					squareBracketLevel++;
					break;
				case '{':
					curlyBraceLevel++;
					break;
			}
			
			while (parenthesisLevel != 0 || squareBracketLevel != 0 || curlyBraceLevel != 0) {
				char ch = GetNext();
				switch (ch) {
					case '\0':
						return false;
					case '(':
						parenthesisLevel--;
						break;
					case '[':
						squareBracketLevel--;
						break;
					case '{':
						curlyBraceLevel--;
						break;
					case ')':
						parenthesisLevel++;
						break;
					case ']':
						squareBracketLevel++;
						break;
					case '}':
						curlyBraceLevel++;
						break;
				}
			}
			return true;
		}
		
		string ReadIdentifier(char ch)
		{
			string identifier = ch.ToString();
			while (IsIdentifierPart(Peek())) {
				identifier = GetNext() + identifier;
			}
			return identifier;
		}
		
		void ReadDigit(char ch)
		{
			//string digit = ch.ToString();
			while (Char.IsDigit(Peek()) || Peek() == '.') {
				GetNext();
				//digit = GetNext() + digit;
			}
			//return digit;
		}
		
		bool IsIdentifierPart(char ch)
		{
			return Char.IsLetterOrDigit(ch) || ch == '_' || ch == '@';
		}
		#endregion
		
		#region finite state machine
		readonly static int ERROR  = 0;
		readonly static int START  = 1;
		readonly static int DOT    = 2;
		readonly static int MORE   = 3;
		readonly static int CURLY  = 4;
		readonly static int CURLY2 = 5;
		readonly static int CURLY3 = 6;
		
		readonly static int ACCEPT = 7;
		readonly static int ACCEPTNOMORE = 8;
		readonly static int ACCEPT2 = 9;
		
//		readonly static string[] stateName = new string[] {
//			"ERROR",
//			"START",
//			"DOT",
//			"MORE",
//			"CURLY",
//			"CURLY2",
//			"CURLY3",
//			"ACCEPT",
//			"ACCEPTNOMORE",
//			"ACCEPT2"
//		};
		
		int state = 0;
		int lastAccept = 0;
		static int[,] stateTable = new int[,] {
			//                   Err,     Dot,     Str,      ID,         New,     Brk,     Par,     Cur,   Using,       digit
			/*ERROR*/        { ERROR,   ERROR,   ERROR,   ERROR,        ERROR,  ERROR,   ERROR,   ERROR,   ERROR,        ERROR},
			/*START*/        { ERROR,     DOT,  ACCEPT,  ACCEPT,        ERROR,   MORE, ACCEPT2,   CURLY,   ACCEPTNOMORE, ERROR},
			/*DOT*/          { ERROR,   ERROR,  ACCEPT,  ACCEPT,        ERROR,   MORE,  ACCEPT,   CURLY,   ERROR,        ACCEPT},
			/*MORE*/         { ERROR,   ERROR,  ACCEPT,  ACCEPT,        ERROR,   MORE, ACCEPT2,   CURLY,   ERROR,        ACCEPT},
			/*CURLY*/        { ERROR,   ERROR,   ERROR,   ERROR,        ERROR, CURLY2,   ERROR,   ERROR,   ERROR,        ERROR},
			/*CURLY2*/       { ERROR,   ERROR,   ERROR,  CURLY3,        ERROR,  ERROR,   ERROR,   ERROR,   ERROR,        CURLY3},
			/*CURLY3*/       { ERROR,   ERROR,   ERROR,   ERROR, ACCEPTNOMORE,  ERROR,   ERROR,   ERROR,   ERROR,        ERROR},
			/*ACCEPT*/       { ERROR,    MORE,   ERROR,   ERROR,       ACCEPT,  ERROR,   ERROR,   ERROR,   ACCEPTNOMORE, ERROR},
			/*ACCEPTNOMORE*/ { ERROR,   ERROR,   ERROR,   ERROR,        ERROR,  ERROR,   ERROR,   ERROR,   ERROR,        ERROR},
			/*ACCEPT2*/      { ERROR,    MORE,   ERROR,  ACCEPT,       ACCEPT,  ERROR,   ERROR,   ERROR,   ERROR,        ACCEPT},
		};
		#endregion
	}
}
