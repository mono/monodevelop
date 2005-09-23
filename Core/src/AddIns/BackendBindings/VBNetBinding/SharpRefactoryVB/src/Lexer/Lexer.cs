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

namespace ICSharpCode.SharpRefactory.Parser.VB
{
	public class Token
	{
		public int kind;
		
		public int col;
		public int line;
		
		public object    literalValue = null;
		public string    val;
		public Token     next;
		public ArrayList specials;
		
		public Point EndLocation {
			get {
				return new Point(col + val.Length - 1, line);
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
		static  Hashtable keywords = new Hashtable();
		
		int col  = 1;
		int line = 1;
		
		bool lineEnd = false;
		
		Errors errors   = new Errors();
		
		SpecialTracker specialTracker = new SpecialTracker();
		
		Token lastToken = null;
		Token curToken  = null;
		Token peekToken = null;
		
		string[]       specialCommentTags = null;
		Hashtable      specialCommentHash = null;
		ArrayList      tagComments = new ArrayList();
		
		public ArrayList TagComments
		{
			get {
				return tagComments;
			}
		}
		
		public string[] SpecialCommentTags
		{
			get {
				return specialCommentTags;
			}
			set {
				specialCommentTags = value;
				specialCommentHash = new Hashtable();
				if (specialCommentTags != null) {
					foreach (string str in specialCommentTags) {
						specialCommentHash[str] = 0;
					}
				}
			}
		}
		
		public SpecialTracker SpecialTracker
		{
			get {
				return specialTracker;
			}
		}
		
		public Errors Errors
		{
			get {
				return errors;
			}
		}
		
		public Token Token
		{
			get {
				return lastToken;
			}
		}
		
		public Token LookAhead
		{
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
			if (curToken == null) { // first call of NextToken()
				curToken = Next();
				specialTracker.InformToken(curToken.kind);
				return curToken;
			}
			
			lastToken = curToken;
			
			if (curToken.next == null) {
				curToken.next = Next();
				specialTracker.InformToken(curToken.next.kind);
			}
			
			curToken = curToken.next;
			
			if (curToken.kind == Tokens.EOF && !(lastToken.kind == Tokens.EOL)) { // be sure that before EOF there is an EOL token
				curToken = new Token(Tokens.EOL, curToken.col, curToken.line, "\n");
				curToken.next = new Token(Tokens.EOF, curToken.col, curToken.line, "\n");
				specialTracker.InformToken(curToken.next.kind);
			}
			
			return curToken;
		}
		
		public ArrayList RetriveComments()
		{
			return specialTracker.RetrieveSpecials();
		}
		
//		public ArrayList RetrieveSpecials()
//		{
//			if (lastToken == null) {
//				return this.specialTracker.RetrieveSpecials();
//			}
//			
//			Debug.Assert(lastToken.specials != null);
//			
//			ArrayList tmp = lastToken.specials;
//			lastToken.specials = null;
//			return tmp;
//		}
//		
		public Lexer(IReader reader)
		{
			this.reader = reader;
		}
		
		public Token Next()
		{
			while (!reader.Eos()) {

				char ch = reader.GetNext();
		
				++col;
				if (Char.IsWhiteSpace(ch)) {
				
					if (ch == '\n') {
						int x = col - 1;
						int y = line;
						++line;
						col = 1;
						if (reader.Peek() == '\r') {
							reader.GetNext();
							if (!lineEnd) {
								lineEnd = true;
								return new Token(Tokens.EOL, x -1 , y, "\n\r");
							}
						}
						if (!lineEnd) {
							lineEnd = true;
							return new Token(Tokens.EOL, x, y, "\n");
						}
					}
					continue;

				}
				if (ch == '_') {
					if (reader.Eos()) {
						errors.Error(line, col, String.Format("No EOF expected after _"));
					}
					ch = reader.GetNext();
					++col;
					if (!Char.IsWhiteSpace(ch)) {
						reader.UnGet();
						--col;
						int x = col;
						int y = line;
						string s = ReadIdent('_');
						lineEnd = false;

						return new Token(Tokens.Identifier, x, y, s);
					}
					while (Char.IsWhiteSpace(ch)) {
						if (ch == '\n') {
							++line;
							col = 0;
							break;
						}
						if (!reader.Eos()) {
							ch = reader.GetNext();
							++col;
						}
					}
					if (ch != '\n') {
						errors.Error(line, col, String.Format("Return expected"));
					}
					continue;
				}
				
				if (ch == '#') {
					while (Char.IsWhiteSpace(reader.Peek())) {
						++col;
						reader.GetNext();
					}
					if (Char.IsDigit(reader.Peek())) {
						int x = col;
						int y = line;
						string s = ReadDate();
						DateTime time = DateTime.Now;
						try {
							time = System.DateTime.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
						} catch (Exception e) {
							errors.Error(line, col, String.Format("Invalid date time {0}", e));
						}
						return new Token(Tokens.LiteralDate, x, y, s, time);
					} else {
						ReadPreprocessorDirective();
						continue;
					}
				}
				
				if (ch == '[') { // Identifier
					lineEnd = false;
					if (reader.Eos()) {
						errors.Error(line, col, String.Format("Identifier expected"));
					}
					ch = reader.GetNext();
					++col;
					if (ch == ']' || Char.IsWhiteSpace(ch)) {
						errors.Error(line, col, String.Format("Identifier expected"));
					}
					int x = col - 1;
					int y = line;
					string s = ReadIdent(ch);
					if (reader.Eos()) {
						errors.Error(line, col, String.Format("']' expected"));
					}
					ch = reader.GetNext();
					++col;
					if (!(ch == ']')) {
						errors.Error(line, col, String.Format("']' expected"));
					}
//					Console.WriteLine(">" + s + "<");
					return new Token(Tokens.Identifier, x, y, s);
				}
				if (Char.IsLetter(ch)) {
					int x = col - 1;
					int y = line;
					string s = ReadIdent(ch);
					if (Keywords.IsKeyword(s)) {
						lineEnd = false;
						return new Token(Keywords.GetToken(s), x, y, s);
					}
					
					// handle 'REM' comments 
					if (s.ToUpper() == "REM") {
						ReadComment();
						if (!lineEnd) {
							lineEnd = true;
							return new Token(Tokens.EOL, x, y, "\n");
						}
						continue;
					}
						
					lineEnd = false;
					return new Token(Tokens.Identifier, x, y, s);
				
				}
				if (Char.IsDigit(ch)) {
					lineEnd = false;
					return ReadDigit(ch, col);
				}
				if (ch == '&') {
					lineEnd = false;
					if (reader.Eos()) {
						return ReadOperator('&');
					}
					ch = reader.GetNext();
					++col;
					if (Char.ToUpper(ch) == 'H' || Char.ToUpper(ch) == 'O') {
						reader.UnGet();
						--col;
						return ReadDigit('&', col);
					} else {
						reader.UnGet();
						return ReadOperator('&');
					}
				}
				if (ch == '\'') {
					int x = col - 1;
					int y = line;
					ReadComment();
					if (!lineEnd) {
						lineEnd = true;
						return new Token(Tokens.EOL, x, y, "\n");
					}
					continue;
				}
				if (ch == '"') {
					lineEnd = false;
					int x = col - 1;
					int y = line;
					string s = ReadString();
					if (!reader.Eos() && (reader.Peek() == 'C' || reader.Peek() == 'c')) {
						reader.GetNext();
						++col;
						if (s.Length != 1) {
							errors.Error(line, col, String.Format("Chars can only have Length 1 "));
						}
						return new Token(Tokens.LiteralCharacter, x, y, String.Concat('"', s , "\"C") , s[0]);
					}
					return new Token(Tokens.LiteralString, x, y,  String.Concat('"', s , '"'), s);
				}
				Token token = ReadOperator(ch);
				if (token != null) {
					lineEnd = false;
					return token;
				}
				errors.Error(line, col, String.Format("Unknown char({0}) which can't be read", ch));
			}
			
			return new Token(Tokens.EOF);
		}
		
		string ReadIdent(char ch) 
		{
			StringBuilder s = new StringBuilder(ch.ToString());
			while (!reader.Eos() && (Char.IsLetterOrDigit(ch = reader.GetNext()) || ch == '_')) {
				++col;
				s.Append(ch.ToString());
			}
			++col;
			if (reader.Eos()) {
				--col;
				return s.ToString();
			}
			reader.UnGet();
			--col;
			if (!reader.Eos() && "%&@!#$".IndexOf(Char.ToUpper(reader.Peek())) != -1) {
				reader.GetNext();
				++col;
			}
			return s.ToString();
		}
		
		Token ReadDigit(char ch, int x)
		{
			StringBuilder sb = new StringBuilder(ch.ToString());
			int y = line;
			string digit = "";
			if (ch != '&') {
				digit += ch;
			}
			
			bool ishex      = false;
			bool isokt      = false;
			bool issingle   = false;
			bool isdouble   = false;
			bool isdecimal  = false;
			
			if (reader.Eos()) {
				if (ch == '&') {
					errors.Error(line, col, String.Format("digit expected"));
				}
				return new Token(Tokens.LiteralInteger, x, y, sb.ToString() ,ch - '0');
			}
			if (ch == '&' && Char.ToUpper(reader.Peek()) == 'H') {
				const string hex = "0123456789ABCDEF";
				sb.Append(reader.GetNext()); // skip 'H'
				++col;
				while (!reader.Eos() && hex.IndexOf(Char.ToUpper(reader.Peek())) != -1) {
					ch = reader.GetNext();
					sb.Append(ch); 
					digit += Char.ToUpper(ch);
					++col;
				}
				ishex = true;
			} else if (!reader.Eos() && ch == '&' && Char.ToUpper(reader.Peek()) == 'O') {
				const string okt = "01234567";
				sb.Append(reader.GetNext()); // skip 'O'
				++col;
				while (!reader.Eos() && okt.IndexOf(Char.ToUpper(reader.Peek())) != -1) {
					ch = reader.GetNext();
					sb.Append(ch); 
					digit += Char.ToUpper(ch);
					++col;
				}
				isokt = true;
			} else {
				while (!reader.Eos() && Char.IsDigit(reader.Peek())) {
					ch = reader.GetNext();;
					digit += ch;
					sb.Append(ch);
					++col;
				}
			}
			if (!reader.Eos() && "%&SIL".IndexOf(Char.ToUpper(reader.Peek())) != -1 || ishex || isokt) {
				ch = reader.GetNext();
				sb.Append(ch); 
				ch = Char.ToUpper(ch);
				++col;
				if (isokt) {
					long number = 0L;
					for (int i = 0; i < digit.Length; ++i) {
						number = number * 8 + digit[i] - '0';
					}
					if (ch == 'S') {
						return new Token(Tokens.LiteralSingle, x, y, sb.ToString(), (short)number);
					} else if (ch == '%' || ch == 'I') {
						return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (int)number);
					} else if (ch == '&' || ch == 'L') {
						return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (long)number);
					} else {
						if (number > int.MaxValue || number < int.MinValue) {
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (long)number);
						} else {
							return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (int)number);
						}
					}
				}
				if (ch == 'S') {
					return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int16.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
				} else if (ch == '%' || ch == 'I') {
					return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int32.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
				} else if (ch == '&' || ch == 'L') {
					return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int64.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
				} else if (ishex) {
					reader.UnGet();
					--col;
					long number = Int64.Parse(digit, NumberStyles.HexNumber);
					if (number > int.MaxValue || number < int.MinValue) {
						return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), number);
					} else {
						return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), (int)number);
					}
				}
			}
			if (!reader.Eos() && reader.Peek() == '.') { // read floating point number
				reader.GetNext();
				if (!Char.IsDigit(reader.Peek())) {
					reader.UnGet();
				} else {
					isdouble = true; // double is default
					if (ishex || isokt) {
						errors.Error(line, col, String.Format("No hexadecimal or oktadecimal floating point values allowed"));
					}
					digit += '.';
					++col;
					while (!reader.Eos() && Char.IsDigit(reader.Peek())){ // read decimal digits beyond the dot
						digit += reader.GetNext();
						++col;
					}
				}
			}
			
			if (!reader.Eos() && Char.ToUpper(reader.Peek()) == 'E') { // read exponent
				isdouble = true;
				digit +=  reader.GetNext();
				++col;
				if (!reader.Eos() && (reader.Peek() == '-' || reader.Peek() == '+')) {
					digit += reader.GetNext();
					++col;
				}
				while (!reader.Eos() && Char.IsDigit(reader.Peek())) { // read exponent value
					digit += reader.GetNext();
					++col;
				}
			}
			
			if (!reader.Eos()) {
				if (Char.ToUpper(reader.Peek()) == 'R' || Char.ToUpper(reader.Peek()) == '#') { // double type suffix (obsolete, double is default)
					reader.GetNext();
					++col;
					isdouble = true;
				} else if (Char.ToUpper(reader.Peek()) == 'D' || Char.ToUpper(reader.Peek()) == '@') { // decimal value
					reader.GetNext();
					++col;
					isdecimal = true;
				} else if (Char.ToUpper(reader.Peek()) == 'F' || Char.ToUpper(reader.Peek()) == '!') { // decimal value
					reader.GetNext();
					++col;
					issingle = true;
				}
			}
			
			if (issingle) {
				NumberFormatInfo mi = new NumberFormatInfo();
				mi.CurrencyDecimalSeparator = ".";
				return new Token(Tokens.LiteralSingle, x, y, sb.ToString(), Single.Parse(digit, mi));
			}
			if (isdecimal) {
				NumberFormatInfo mi = new NumberFormatInfo();
				mi.CurrencyDecimalSeparator = ".";
				return new Token(Tokens.LiteralDecimal, x, y, sb.ToString(), Decimal.Parse(digit, mi));
			}
			if (isdouble) {
				NumberFormatInfo mi = new NumberFormatInfo();
				mi.CurrencyDecimalSeparator = ".";
				return new Token(Tokens.LiteralDouble, x, y, sb.ToString(), Double.Parse(digit, mi));
			}
			try {
				return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int32.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
			} catch (Exception) {
				try {
					return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), Int64.Parse(digit, ishex ? NumberStyles.HexNumber : NumberStyles.Number));
				} catch (Exception) {
					errors.Error(line, col, String.Format("{0} is not a parseable number (too long?)", sb.ToString()));
					// fallback, when nothing helps :)
					return new Token(Tokens.LiteralInteger, x, y, sb.ToString(), 0);
				}
			}
		}
		
		void ReadPreprocessorDirective()
		{
			Point start = new Point(col - 1, line);
			string directive = ReadIdent('#');
			string argument  = ReadToEOL();
			this.specialTracker.AddPreProcessingDirective(directive, argument.Trim(), start, new Point(start.X + directive.Length + argument.Length, start.Y));
		}
		
		string ReadToEOL()
		{
			StringBuilder sb = new StringBuilder();
			if (!reader.Eos()) {
				char ch = reader.GetNext();
				while (!reader.Eos()) {
					if (ch == '\r') {
						if (reader.Peek() == '\n') {
							ch = reader.GetNext();
						}
					}
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
		
		string ReadDate()
		{
			char ch = '\0';
			StringBuilder sb = new StringBuilder();
			while (!reader.Eos()) {
				ch = reader.GetNext();
				++col;
				if (ch == '#') {
					break;
				} else if (ch == '\n') {
					errors.Error(line, col, String.Format("No return allowed inside Date literal"));
				} else {
					sb.Append(ch);
				}
			}
			if (ch != '#') {
				errors.Error(line, col, String.Format("End of File reached before Date literal terminated"));
			}
			return sb.ToString();
		}
		
		string ReadString()
		{
			char ch = '\0';
			StringBuilder s = new StringBuilder();
			while (!reader.Eos()) {
				ch = reader.GetNext();
				++col;
				if (ch == '"') {
					if (!reader.Eos() && reader.Peek() == '"') {
						s.Append('"');
						reader.GetNext();
						++col;
					} else {
						break;
					}
				} else if (ch == '\n') {
					errors.Error(line, col, String.Format("No return allowed inside String literal"));
				} else {
					s.Append(ch);
				}
			}
			if (ch != '"') {
				errors.Error(line, col, String.Format("End of File reached before String terminated "));
			}
			return s.ToString();
		}
		
		protected bool HandleLineEnd(char ch)
		{
			if (WasLineEnd(ch)) {
				++line;
				col = 1;
				return true;
			}
			return false;
		}
		
		protected bool WasLineEnd(char ch)
		{
			// Handle MS-DOS or MacOS line ends.
			if (ch == '\r') {
				if (reader.Peek() == '\n') { // MS-DOS line end '\r\n'
					ch = (char)reader.GetNext();
				} else { // assume MacOS line end which is '\r'
					ch = '\n';
				}
			}
			return ch == '\n';
		}
		
		void ReadComment()
		{
			StringBuilder curWord = new StringBuilder();
			StringBuilder comment = new StringBuilder();
			
			int nextChar;
			while ((nextChar = reader.GetNext()) != -1) {
				char ch = (char)nextChar;
				comment.Append(ch);
				++col;
				if (HandleLineEnd(ch) || nextChar == 0) {
					specialTracker.StartComment(CommentType.SingleLine, new Point(col, line));
					specialTracker.AddString(comment.ToString());
					specialTracker.FinishComment();
					return;
				}
				
				if (Char.IsLetter(ch)) {
					curWord.Append(ch);
				} else {
					string tag = curWord.ToString();
					curWord = new StringBuilder();
					if (specialCommentHash != null && specialCommentHash[tag] != null) {
						Point p = new Point(col, line);
						string commentStr = ReadToEOL();
						tagComments.Add(new TagComment(tag, commentStr, p));
						return;
					}
				}
			}
		}
		
		Token ReadOperator(char ch)
		{
			int x = col;
			int y = line;
			switch(ch) {
				case '+':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
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
							case '=':
								++col;
								return new Token(Tokens.MinusAssign, x, y, "-=");
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
				case '\\':
					switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.DivIntegerAssign, x, y, "\\=");
							default:
								reader.UnGet();
								break;
						}
					return new Token(Tokens.DivInteger, x, y, "\\");
				case '&':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.ConcatStringAssign, x, y, "&=");
							default:
								reader.UnGet();
								break;
						}
					}
					return new Token(Tokens.ConcatString, x, y, "&");
				case '^':
					switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.PowerAssign, x, y, "^=");
							default:
								reader.UnGet();
								break;
						}
					return new Token(Tokens.Power, x, y, "^");
				case ':':
					return new Token(Tokens.Colon, x, y, ":");
				case '=':
					return new Token(Tokens.Assign, x, y, "=");
				case '<':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.LessEqual, x, y, "<=");
							case '>':
								++col;
								return new Token(Tokens.NotEqual, x, y, "<>");
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
							default:
								reader.UnGet();
								return new Token(Tokens.LessThan, x, y, "<");
						}
					}
					return new Token(Tokens.LessThan, x, y, "<");
				case '>':
					if (!reader.Eos()) {
						switch (reader.GetNext()) {
							case '=':
								++col;
								return new Token(Tokens.GreaterEqual, x, y, ">=");
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
							default:
								reader.UnGet();
								return new Token(Tokens.GreaterThan, x, y, ">");
						}
					}
					return new Token(Tokens.GreaterThan, x, y, "<=");
				case ',':
					return new Token(Tokens.Comma, x, y, ",");
				case '.':
					if (Char.IsDigit(reader.Peek())) {
						 reader.UnGet();
						 --col;
						 return ReadDigit('0', col);
					}
					return new Token(Tokens.Dot, x, y, ".");
				case '(':
					return new Token(Tokens.OpenParenthesis, x, y, "(");
				case ')':
					return new Token(Tokens.CloseParenthesis, x, y, ")");
				case '{':
					return new Token(Tokens.OpenCurlyBrace, x, y, "{");
				case '}':
					return new Token(Tokens.CloseCurlyBrace, x, y, "}");
				case '[':
					return new Token(Tokens.OpenSquareBracket, x, y, "[");
				case ']':
					return new Token(Tokens.CloseSquareBracket, x, y, "]");
			}
			return null;
		}
	}
}
