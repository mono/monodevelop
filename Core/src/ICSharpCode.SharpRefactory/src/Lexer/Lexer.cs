// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using ICSharpCode.SharpRefactory.Parser;

namespace ICSharpCode.SharpRefactory.Parser
{
	public class Token
	{
		public int kind;
		
		public int col;
		public int line;
		
		public object literalValue = null;
		public string val;
		public Token  next;
		
		public Point EndLocation {
			get {
				return new Point(col + val.Length, line);
			}
		}
		public Point Location {
			get {
				return new Point(col, line);
			}
		}
		
		public Token()
		{
		}
		
		public Token(int kind)
		{
			this.kind = kind;
		}
		
//		public Token(Tokens kind, int col, int line)
//		{
//			this.kind = kind;
//			this.col  = col;
//			this.line = line;
//		}
		
		public Token(int kind, int col, int line, string val)
		{
			this.kind = kind;
			this.col  = col;
			this.line = line;
			this.val  = val;
		}
		
		public Token(int kind, int col, int line, string val, object literalValue)
		{
			this.kind         = kind;
			this.col          = col;
			this.line         = line;
			this.val          = val;
			this.literalValue = literalValue;
		}
	}
	
	public class Lexer
	{
		IReader reader;
		
		int col  = 1;
		int line = 1;
		
		Errors         errors   = new Errors();
		SpecialTracker specialTracker = new SpecialTracker();
		Token          lastToken = null;
		Token          curToken  = null;
		Token          peekToken = null;
		
		public SpecialTracker SpecialTracker {
			get {
				return specialTracker;
			}
		}
		
		public Errors Errors {
			get {
				return errors;
			}
		}
		
		public Token Token {
			get {
				return lastToken;
			}
		}
		
		public Token LookAhead {
			get {
				return curToken;
			}
		}
		
		public void StartPeek()
		{
			peekToken = curToken;
		}
		
		public Token Peek()
		{
			if (peekToken.next == null) {
				peekToken.next = Next();
				specialTracker.InformToken(peekToken.next.kind);
			}
			peekToken = peekToken.next;
			return peekToken;
		}
		
		public Token NextToken()
		{
			if (curToken == null) {
				curToken = Next();
				specialTracker.InformToken(curToken.kind);
				return curToken;
			}
			
			lastToken = curToken;
			
			if (curToken.next == null) {
				curToken.next = Next();
				specialTracker.InformToken(curToken.next.kind);
			}
			
			curToken  = curToken.next;
			return curToken;
		}
		
		public Lexer(IReader reader)
		{
			this.reader = reader;
		}
		
		Token Next()
		{
			while (!reader.Eos()) {
				char ch = reader.GetNext();
				
				if (Char.IsWhiteSpace(ch)) {
					++col;
					
					if (ch == '\n') {
						specialTracker.AddEndOfLine();
						++line;
						col = 1;
					}
					continue;
				}
				
				if (Char.IsLetter(ch) || ch == '_') {
					int x = col;
					int y = line;
					string s = ReadIdent(ch);
					if (Keywords.IsKeyword(s)) {
						return new Token(Keywords.GetToken(s), x, y, s);
					}
					return new Token(Tokens.Identifier, x, y, s);
				}
				
				if (Char.IsDigit(ch)) {
					return ReadDigit(ch, col);
				}
				
				if (ch == '/') {
					if (reader.Peek() == '/' || reader.Peek() == '*') {
						++col;
						ReadComment();
						continue;
					}
				} else if (ch == '#') {
					Point start = new Point(col, line);
					++col;
					string directive = ReadIdent('#');
					string argument  = ReadToEOL();
					this.specialTracker.AddPreProcessingDirective(directive, argument, start, new Point(start.X + directive.Length + argument.Length, start.Y));
					continue;
				}
				
				if (ch == '"') {
					++col;
					return ReadString();
				}
				
				if (ch == '\'') {
					++col;
					return ReadChar();
				}
				
				if (ch == '@') {
					int x = col;
					int y = line;
					ch = reader.GetNext();
					++col;
					if (ch == '"') {
						return ReadVerbatimString();
					}
					if (Char.IsLetterOrDigit(ch)) {
						return new Token(Tokens.Identifier, x, y, ReadIdent(ch));
					}
					errors.Error(y, x, String.Format("Unexpected char in Lexer.Next() : {0}", ch));
				}
				
				Token token = ReadOperator(ch);
				
				// try error recovery :)
				if (token == null) {
					return Next();
				}
				return token;
			}
			
			return new Token(Tokens.EOF, col, line, String.Empty);
		}
		
		string ReadIdent(char ch)
		{
			StringBuilder s = new StringBuilder(ch.ToString());
			++col;
			while (!reader.Eos() && (Char.IsLetterOrDigit(ch = reader.GetNext()) || ch == '_')) {
				s.Append(ch.ToString());
				++col;
			}
			if (!reader.Eos()) {
				reader.UnGet();
			}
			return s.ToString();
		}
		
		Token ReadDigit(char ch, int x)
		{
			int y = line;
			++col;
			StringBuilder sb = new StringBuilder(ch.ToString());
			StringBuilder prefix = new StringBuilder();
			StringBuilder suffix = new StringBuilder();
			
			bool ishex      = false;
			bool isunsigned = false;
			bool islong     = false;
			bool isfloat    = false;
			bool isdouble   = false;
			bool isdecimal  = false;
			
			if (ch == '0' && Char.ToUpper(reader.Peek()) == 'X') {
				const string hex = "0123456789ABCDEF";
				reader.GetNext(); // skip 'x'
				++col;
				while (hex.IndexOf(Char.ToUpper(reader.Peek())) != -1) {
					sb.Append(Char.ToUpper(reader.GetNext()));
					++col;
				}
				ishex = true;
				prefix.Append("0x");
			} else {
				while (Char.IsDigit(reader.Peek())) {
					sb.Append(reader.GetNext());
					++col;
				}
			}
			
			if (reader.Peek() == '.') { // read floating point number
				isdouble = true; // double is default
				if (ishex) {
					errors.Error(y, x, String.Format("No hexadecimal floating point values allowed"));
				}
				sb.Append(reader.GetNext());
				++col;
				
				while (Char.IsDigit(reader.Peek())) { // read decimal digits beyond the dot
					sb.Append(reader.GetNext());
					++col;
				}
			}
			
			if (Char.ToUpper(reader.Peek()) == 'E') { // read exponent
				isdouble = true;
				sb.Append(reader.GetNext());
				++col;
				if (reader.Peek() == '-' || reader.Peek() == '+') {
					sb.Append(reader.GetNext());
					++col;
				}
				while (Char.IsDigit(reader.Peek())) { // read exponent value
					sb.Append(reader.GetNext());
					++col;
				}
				isunsigned = true;
			}
			
			if (Char.ToUpper(reader.Peek()) == 'F') { // float value
				suffix.Append(reader.Peek());
				reader.GetNext();
				++col;
				isfloat = true;
			} else if (Char.ToUpper(reader.Peek()) == 'D') { // double type suffix (obsolete, double is default)
				suffix.Append(reader.Peek());
				reader.GetNext();
				++col;
				isdouble = true;
			} else if (Char.ToUpper(reader.Peek()) == 'M') { // decimal value
				suffix.Append(reader.Peek());
				reader.GetNext();
				++col;
				isdecimal = true;
			} else if (!isdouble) {
				if (Char.ToUpper(reader.Peek()) == 'U') {
					suffix.Append(reader.Peek());
					reader.GetNext();
					++col;
					isunsigned = true;
				}
				
				if (Char.ToUpper(reader.Peek()) == 'L') {
					suffix.Append(reader.Peek());
					reader.GetNext();
					++col;
					islong = true;
					if (!isunsigned && Char.ToUpper(reader.Peek()) == 'U') {
						suffix.Append(reader.Peek());
						reader.GetNext();
						++col;
						isunsigned = true;
					}
				}
			}
			
			string digit = sb.ToString();
			string stringValue = String.Concat(prefix.ToString(), digit, suffix.ToString());
			if (isfloat) {
				try {
					NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
					numberFormatInfo.CurrencyDecimalSeparator = ".";
					return new Token(Tokens.Literal, x, y, stringValue, Single.Parse(digit, numberFormatInfo));
				} catch (Exception) {
					errors.Error(y, x, String.Format("Can't parse float {0}", digit));
					return new Token(Tokens.Literal, x, y, stringValue, 0f);
				}
			}
			if (isdecimal) {
				try {
					NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
					numberFormatInfo.CurrencyDecimalSeparator = ".";
					return new Token(Tokens.Literal, x, y, stringValue, Decimal.Parse(digit, numberFormatInfo));
				} catch (Exception) {
					errors.Error(y, x, String.Format("Can't parse decimal {0}", digit));
					return new Token(Tokens.Literal, x, y, stringValue, 0m);
				}
			}
			if (isdouble) {
				try {
					NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
					numberFormatInfo.CurrencyDecimalSeparator = ".";
					return new Token(Tokens.Literal, x, y, stringValue, Double.Parse(digit, numberFormatInfo));
				} catch (Exception) {
					errors.Error(y, x, String.Format("Can't parse double {0}", digit));
					return new Token(Tokens.Literal, x, y, stringValue, 0d);
				}
			}
			
			long d = 0;
			// FIXME: http://bugzilla.ximian.com/show_bug.cgi?id=72221
			try {
				d = long.Parse (digit, ishex ? NumberStyles.HexNumber : NumberStyles.Integer);
			}
			catch {
				errors.Error(y, x, String.Format("Can't parse integral constant {0}", digit));
				return new Token(Tokens.Literal, x, y, stringValue.ToString(), 0);
			}
			if (d < long.MinValue || d > long.MaxValue) {
				islong = true;
				isunsigned = true;	
			}
			else if (d < uint.MinValue || d > uint.MaxValue) {
				islong = true;	
			}
			else if (d < int.MinValue || d > int.MaxValue) {
				isunsigned = true;	
			}
			if (islong) {
				if (isunsigned) {
					try {
						return new Token(Tokens.Literal, x, y, stringValue, UInt64.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
					} catch (Exception) {
						errors.Error(y, x, String.Format("Can't parse unsigned long {0}", digit));
						return new Token(Tokens.Literal, x, y, stringValue, 0UL);
					}
				} else {
					try {
						return new Token(Tokens.Literal, x, y, stringValue, Int64.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
					} catch (Exception) {
						errors.Error(y, x, String.Format("Can't parse long {0}", digit));
						return new Token(Tokens.Literal, x, y, stringValue, 0L);
					}
				}
			} else {
				if (isunsigned) {
					try {
						return new Token(Tokens.Literal, x, y, stringValue, UInt32.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
					} catch (Exception) {
						errors.Error(y, x, String.Format("Can't parse unsigned int {0}", digit));
						return new Token(Tokens.Literal, x, y, stringValue, 0U);
					}
				} else {
					try {
						return new Token(Tokens.Literal, x, y, stringValue, Int32.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
					} catch (Exception) {
						errors.Error(y, x, String.Format("Can't parse int {0}", digit));
						return new Token(Tokens.Literal, x, y, stringValue, 0);
					}
				}
			}
		}
		
		Token ReadString()
		{
			int x = col;
			int y = line;
			
			char ch = '\0';
			StringBuilder s             = new StringBuilder();
			StringBuilder originalValue = new StringBuilder();
			originalValue.Append('"');
			while (!reader.Eos() && ((ch = reader.GetNext()) != '"')) {
				++col;
				if (ch == '\\') {
					originalValue.Append('\\');
					originalValue.Append(ReadEscapeSequence(out ch));
					s.Append(ch);
				} else if (ch == '\n') {
					errors.Error(y, x, String.Format("No new line is allowed inside a string literal"));
					break;
				} else {
					originalValue.Append(ch);
					s.Append(ch);
				}
			}
			if (ch != '"') {
				errors.Error(y, x, String.Format("End of file reached inside string literal"));
			}
			originalValue.Append('"');
			return new Token(Tokens.Literal, x, y, originalValue.ToString(), s.ToString());
		}
		
		Token ReadVerbatimString()
		{
			int x = col;
			int y = line;
			char ch = '\0';
			StringBuilder s = new StringBuilder();
			while (!reader.Eos()) {
				ch = reader.GetNext();
				if (ch == '"') {
					if (reader.Peek() != '"') {
						break;
					}
					reader.GetNext();
				}
				++col;
				if (ch == '\n') {
					++line;
					col = 1;
				}
				s.Append(ch);
			}
			if (ch != '"') {
				errors.Error(y, x, String.Format("End of file reached inside verbatim string literal"));
			}
			return new Token(Tokens.Literal, x, y, String.Concat("@\"", s.ToString(), '"'), s.ToString());
		}
		
		string hexdigits = "0123456789ABCDEF";
		
		string ReadEscapeSequence(out char ch)
		{
			StringBuilder s = new StringBuilder();
			if (reader.Eos()) {
				errors.Error(line, col, String.Format("End of file reached inside escape sequence"));
			}
			char c = reader.GetNext();
			s.Append(c);
			++col;
			switch (c)  {
				case '\'':
					ch = '\'';
					break;
				case '\"':
					ch = '\"';
					break;
				case '\\':
					ch = '\\';
					break;
				case '0':
					ch = '\0';
					break;
				case 'a':
					ch = '\a';
					break;
				case 'b':
					ch = '\b';
					break;
				case 'f':
					ch = '\f';
					break;
				case 'n':
					ch = '\n';
					break;
				case 'r':
					ch = '\r';
					break;
				case 't':
					ch = '\t';
					break;
				case 'v':
					ch = '\v';
					break;
				case 'u':
				case 'x':
					c = reader.GetNext();
					int number = hexdigits.IndexOf(Char.ToUpper(c));
					if (number < 0) {
						errors.Error(line, col, String.Format("Invalid char in literal : {0}", c));
					}
					s.Append(c);
					for (int i = 0; i < 3; ++i) {
						c = reader.GetNext();
						int idx = hexdigits.IndexOf(Char.ToUpper(c));
						if (idx >= 0) {
							s.Append(c);
							number = idx * (16 * (i + 1)) + number;
						} else {
							reader.UnGet();
							break;
						}
					}
					ch = (char)number;
					break;
				default:
					errors.Error(line, col, String.Format("Unexpected escape sequence : {0}", c));
					ch = '\0';
					break;
			}
			return s.ToString();
		}
		
		Token ReadChar()
		{
			int x = col;
			int y = line;
			
			if (reader.Eos()) {
				errors.Error(y, x, String.Format("End of file reached inside character literal"));
			}
			StringBuilder originalValue = new StringBuilder();
			char  ch = reader.GetNext();
			originalValue.Append("'");
			originalValue.Append(ch);
			++col;
			
			if (ch == '\\') {
				originalValue.Append(ReadEscapeSequence(out ch));
			}
			
			if (reader.Eos()) {
				errors.Error(y, x, String.Format("End of file reached inside character literal"));
			}
			if (reader.GetNext() != '\'') {
				errors.Error(y, x, String.Format("Char not terminated"));
			}
			originalValue.Append("'");
			return new Token(Tokens.Literal, x, y, originalValue.ToString(), ch);
		}
		
		Token ReadOperator(char ch)
		{
			int x = col;
			int y = line;
			++col;
			switch (ch) {
				case '+':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '+':
								++col;
								return new Token(Tokens.Increment, x, y, "++");
							case '=':
								++col;
								return new Token(Tokens.PlusAssign, x, y, "+=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.Plus, x, y, "+");
				case '-':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '-':
								++col;
								return new Token(Tokens.Decrement, x, y, "--");
							case '=':
								++col;
								return new Token(Tokens.MinusAssign, x, y, "-=");
							case '>':
								++col;
								return new Token(Tokens.Pointer, x, y, "->");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.Minus, x, y, "-");
				case '*':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.TimesAssign, x, y, "*=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.Times, x, y, "*");
				case '/':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.DivAssign, x, y, "/=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.Div, x, y, "/");
				case '%':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.ModAssign, x, y, "%=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.Mod, x, y, "%");
				case '&':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '&':
								++col;
								return new Token(Tokens.LogicalAnd, x, y, "&&");
							case '=':
								++col;
								return new Token(Tokens.BitwiseAndAssign, x, y, "&=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.BitwiseAnd, x, y, "&");
				case '|':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '|':
								++col;
								return new Token(Tokens.LogicalOr, x, y, "||");
							case '=':
								++col;
								return new Token(Tokens.BitwiseOrAssign, x, y, "|=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.BitwiseOr, x, y, "|");
				case '^':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.XorAssign, x, y, "^=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.Xor, x, y, "^");
				case '!':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.NotEqual, x, y, "!=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.Not, x, y, "!");
				case '~':
					return new Token(Tokens.BitwiseComplement, x, y, "~");
				case '=':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.Equal, x, y, "==");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.Assign, x, y, "=");
				case '<':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '<':
								if (!reader.Eos()) {
									switch (reader.GetNext()) {
										case '=':
											col += 2;
											return new Token(Tokens.ShiftLeftAssign, x, y, "<<=");
										default:
											++col;
											reader.UnGet();
											break;
									}
								}
								return new Token(Tokens.ShiftLeft, x, y, "<<");
							case '=':
								++col;
								return new Token(Tokens.LessEqual, x, y, "<=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.LessThan, x, y, "<");
				case '>':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '>':
								if (!reader.Eos()) {
									switch (reader.GetNext()) {
										case '=':
											col += 2;
											return new Token(Tokens.ShiftRightAssign, x, y, ">>=");
										default:
											++col;
											reader.UnGet();
											break;
									}
								}
								return new Token(Tokens.ShiftRight, x, y, ">>");
							case '=':
								++col;
								return new Token(Tokens.GreaterEqual, x, y, ">=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.GreaterThan, x, y, ">");
				case '?':
					return new Token(Tokens.Question, x, y, "?");
				case ';':
					return new Token(Tokens.Semicolon, x, y, ";");
				case ':':
					return new Token(Tokens.Colon, x, y, ":");
				case ',':
					return new Token(Tokens.Comma, x, y, ",");
				case '.':
					if (Char.IsDigit(reader.Peek())) {
						 reader.UnGet();
						 col -= 2;
						 return ReadDigit('0', col + 1);
					}
					return new Token(Tokens.Dot, x, y, ".");
				case ')':
					return new Token(Tokens.CloseParenthesis, x, y, ")");
				case '(':
					return new Token(Tokens.OpenParenthesis, x, y, "(");
				case ']':
					return new Token(Tokens.CloseSquareBracket, x, y, "]");
				case '[':
					return new Token(Tokens.OpenSquareBracket, x, y, "[");
				case '}':
					return new Token(Tokens.CloseCurlyBrace, x, y, "}");
				case '{':
					return new Token(Tokens.OpenCurlyBrace, x, y, "{");
				default:
					--col;
					return null;
			}
		}
		
		void ReadComment()
		{
			char ch = reader.GetNext();
			++col;
			switch (ch) {
				case '*':
					ReadMultiLineComment();
					break;
				case '/':
					if (reader.GetNext() == '/') {
						ReadSingleLineComment(CommentType.Documentation);
					} else {
						reader.UnGet();
						ReadSingleLineComment(CommentType.SingleLine);
					}
					break;
				default:
					errors.Error(line, col, String.Format("Error while reading comment"));
					break;
			}
		}
		
		string ReadToEOL()
		{
			StringBuilder sb = new StringBuilder();
			if (!reader.Eos()) {
				char ch = reader.GetNext();
				while (!reader.Eos()) {
					if (ch == '\n') {
						++line;
						col = 1;
						return sb.ToString();
					} else {
						sb.Append(ch);
					}
					ch = reader.GetNext();
					++col;
				}
			}
			return sb.ToString();
		}
		
		void ReadSingleLineComment(CommentType commentType)
		{
			specialTracker.StartComment(commentType, new Point(line, col));
			specialTracker.AddString(ReadToEOL());
			specialTracker.FinishComment();
		}
		
		void ReadMultiLineComment()
		{
			specialTracker.StartComment(CommentType.Block, new Point(line, col));
			while (!reader.Eos()) {
				char ch;
				switch (ch = reader.GetNext()) {
					case '\n':
						specialTracker.AddChar('\n');
						++line;
						col = 1;
						break;
					case '*':
						++col;
						switch (reader.Peek()) {
							case '/':
								reader.GetNext();
								++col;
								specialTracker.FinishComment();
								return;
							default:
								specialTracker.AddChar('*');
								continue;
						}
					default:
						specialTracker.AddChar(ch);
						++col;
						break;
				}
			}
			specialTracker.FinishComment();
		}
	}
}
