//
// JSBeautifier.cs
//
// Author:
//       rekna <https://jsbeautifylib.codeplex.com/>
//
// Copyright (c) 2014 Rekna
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
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;

namespace MonoDevelop.JavaScript.Formatting
{
	public enum JSBraceStyle
	{
		Expand,
		Collapse,
		EndExpand
	}

	public class JSBeautifierOptions
	{
		public JSBeautifierOptions ()
		{
			this.IndentSize = 4;
			this.IndentChar = ' ';
			this.IndentWithTabs = false;
			this.PreserveNewlines = true;
			this.MaxPreserveNewlines = 10.0f;
			this.JslintHappy = false;
			this.BraceStyle = JSBraceStyle.Collapse;
			this.KeepArrayIndentation = false;
			this.KeepFunctionIndentation = false;
			this.EvalCode = false;
			//this.UnescapeStrings = false;
			this.BreakChainedMethods = false;
			this.DefaultIndent = 0;
		}

		public uint IndentSize { get; set; }

		public int DefaultIndent { get; set; }

		public char IndentChar { get; set; }

		public bool IndentWithTabs { get; set; }

		public bool PreserveNewlines { get; set; }

		public float MaxPreserveNewlines { get; set; }

		public bool JslintHappy { get; set; }

		public JSBraceStyle BraceStyle { get; set; }

		public bool KeepArrayIndentation { get; set; }

		public bool KeepFunctionIndentation { get; set; }

		public bool EvalCode { get; set; }

		//public bool UnescapeStrings { get; set; }

		public bool BreakChainedMethods { get; set; }

		public static JSBeautifierOptions DefaultOptions ()
		{
			return new JSBeautifierOptions ();
		}
	}

	public class JSBeautifierFlags
	{
		public JSBeautifierFlags (string mode)
		{
			this.PreviousMode = "BLOCK";
			this.Mode = mode;
			this.VarLine = false;
			this.VarLineTainted = false;
			this.VarLineReindented = false;
			this.InHtmlComment = false;
			this.IfLine = false;
			this.ChainExtraIndentation = 0;
			this.InCase = false;
			this.InCaseStatement = false;
			this.CaseBody = false;
			this.EatNextSpace = false;
			this.IndentationLevel = 0;
			this.TernaryDepth = 0;
		}

		public string PreviousMode { get; set; }

		public string Mode { get; set; }

		public bool VarLine { get; set; }

		public bool VarLineTainted { get; set; }

		public bool VarLineReindented { get; set; }

		public bool InHtmlComment { get; set; }

		public bool IfLine { get; set; }

		public int ChainExtraIndentation { get; set; }

		public bool InCase { get; set; }

		public bool InCaseStatement { get; set; }

		public bool CaseBody { get; set; }

		public bool EatNextSpace { get; set; }

		public int IndentationLevel { get; set; }

		public int TernaryDepth { get; set; }
	}

	public class JSBeautifier
	{
		public JSBeautifier ()
			: this (new JSBeautifierOptions ())
		{
		}

		public JSBeautifier (JSBeautifierOptions opts)
		{
			this.Opts = opts;
			this.BlankState ();
		}

		public JSBeautifierOptions Opts { get; set; }

		public JSBeautifierFlags Flags { get; set; }

		private List<JSBeautifierFlags> FlagStore { get; set; }

		private bool WantedNewline { get; set; }

		private bool JustAddedNewline { get; set; }

		private bool DoBlockJustClosed { get; set; }

		private string IndentString { get; set; }

		private string PreindentString { get; set; }

		private string LastWord { get; set; }

		private string LastType { get; set; }

		private string LastText { get; set; }

		private string LastLastText { get; set; }

		private string Input { get; set; }

		private List<string> Output { get; set; }

		private char[] Whitespace { get; set; }

		private string Wordchar { get; set; }

		private string Digits { get; set; }

		private string[] Punct { get; set; }

		private string[] LineStarters { get; set; }

		private int ParserPos { get; set; }

		private int NNewlines { get; set; }

		private void BlankState ()
		{
			// internal flags
			this.Flags = new JSBeautifierFlags ("BLOCK");
			this.FlagStore = new List<JSBeautifierFlags> ();
			this.WantedNewline = false;
			this.JustAddedNewline = false;
			this.DoBlockJustClosed = false;

			if (this.Opts.IndentWithTabs)
				this.IndentString = "\t";
			else
				this.IndentString = new string (this.Opts.IndentChar, (int)this.Opts.IndentSize);

			this.PreindentString = "";
			this.LastWord = "";               // last TK_WORD seen
			this.LastType = "TK_START_EXPR";  // last token type
			this.LastText = "";               // last token text
			this.LastLastText = "";           // pre-last token text
			this.Input = null;
			this.Output = new List<string> (); // formatted javascript gets built here
			this.Whitespace = new[] { '\n', '\r', '\t', ' ' };
			this.Wordchar = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_$";
			this.Digits = "0123456789";
			this.Punct = "+ - * / % & ++ -- = += -= *= /= %= == === != !== > < >= <= >> << >>> >>>= >>= <<= && &= | || ! !! , : ? ^ ^= |= :: <?= <? ?> <%= <% %>".Split (' ');

			// Words which always should start on a new line
			this.LineStarters = "continue,try,throw,return,var,if,switch,case,default,for,while,break,function".Split (',');
			this.SetMode ("BLOCK");
			this.ParserPos = 0;
		}

		private void SetMode (string mode)
		{
			JSBeautifierFlags prev = new JSBeautifierFlags ("BLOCK");
			if (this.Flags != null) {
				this.FlagStore.Add (this.Flags);
				prev = this.Flags;
			}
			this.Flags = new JSBeautifierFlags (mode);

			if (this.FlagStore.Count == 1)
				this.Flags.IndentationLevel = 0;
			else {
				this.Flags.IndentationLevel = prev.IndentationLevel;
				if (prev.VarLine && prev.VarLineReindented)
					this.Flags.IndentationLevel = this.Flags.IndentationLevel + 1;
			}

			this.Flags.PreviousMode = prev.Mode;
		}

		public string Beautify (string s, JSBeautifierOptions opts = null)
		{
			if (opts != null)
				this.Opts = opts;

			this.BlankState ();

			while (s.Length != 0 && (s [0] == ' ' || s [0] == '\t')) {
				this.PreindentString += s [0];
				s = s.Remove (0, 1);
			}

			var defaultIndent = Opts.DefaultIndent * Opts.IndentSize;
			while (defaultIndent  != 0) {
				this.PreindentString += this.Opts.IndentChar;
				defaultIndent--;
			}

			this.Input = s;
			this.ParserPos = 0;
			while (true) {
				Tuple<string, string> token = GetNextToken ();
				// print (token_text, token_type, self.flags.mode)
				string tokenText = token.Item1;
				string tokenType = token.Item2;
				if (tokenType == "TK_EOF")
					break;

				Dictionary<string, Action<string>> handlers = new Dictionary<string, Action<string>> {
					{ "TK_START_EXPR", HandleStartExpr }, 
					{ "TK_END_EXPR", HandleEndExpr },
					{ "TK_START_BLOCK", HandleStartBlock },
					{ "TK_END_BLOCK", HandleEndBlock },
					{ "TK_WORD", HandleWord },
					{ "TK_SEMICOLON", HandleSemicolon },
					{ "TK_STRING", HandleString },
					{ "TK_EQUALS", HandleEquals },
					{ "TK_OPERATOR", HandleOperator },
					{ "TK_COMMA", HandleComma },
					{ "TK_BLOCK_COMMENT", HandleBlockComment },
					{ "TK_INLINE_COMMENT", HandleInlineComment },
					{ "TK_COMMENT", HandleComment },
					{ "TK_DOT", HandleDot },
					{ "TK_UNKNOWN", HandleUnknown }
				};
				handlers [tokenType] (tokenText);

				this.LastLastText = this.LastText;
				this.LastType = tokenType;
				this.LastText = tokenText;
			}

			Regex regex = new Regex (@"[\n ]+$");

			string sweetCode = this.PreindentString + regex.Replace (string.Concat (this.Output), "", 1);
			return sweetCode;
		}

		private void TrimOutput (bool eatNewlines = false)
		{
			while (this.Output.Count != 0 &&
			       (this.Output [this.Output.Count - 1] == " " ||
			       this.Output [this.Output.Count - 1] == this.IndentString ||
			       this.Output [this.Output.Count - 1] == this.PreindentString ||
			       (eatNewlines && (this.Output [this.Output.Count - 1] == "\n" || this.Output [this.Output.Count - 1] == "\r")))) {
				this.Output.RemoveAt (this.Output.Count - 1);
			}
		}

		private bool IsSpecialWord (string s)
		{
			return s == "case" || s == "return" || s == "do" || s == "if" || s == "throw" || s == "else";
		}

		private bool IsArray (string mode)
		{
			return mode == "[EXPRESSION]" || mode == "[INDENTED-EXPRESSION]";
		}

		private bool IsExpression (string mode)
		{
			return mode == "[EXPRESSION]" ||
			mode == "[INDENTED-EXPRESSION]" ||
			mode == "(EXPRESSION)" ||
			mode == "(FOR-EXPRESSION)" ||
			mode == "(COND-EXPRESSION)";
		}

		private void AppendNewlineForced ()
		{
			bool oldArrayIndentation = this.Opts.KeepArrayIndentation;
			this.Opts.KeepArrayIndentation = false;
			this.AppendNewline ();
			this.Opts.KeepArrayIndentation = oldArrayIndentation;
		}

		private void AppendNewline (bool ignoreRepeated = true, bool resetStatementFlags = true)
		{
			this.Flags.EatNextSpace = false;

			if (this.Opts.KeepArrayIndentation && this.IsArray (this.Flags.Mode))
				return;

			if (resetStatementFlags) {
				this.Flags.IfLine = false;
				this.Flags.ChainExtraIndentation = 0;
			}

			this.TrimOutput ();

			if (this.Output.Count == 0)
				return;

			if (this.Output [this.Output.Count - 1] != "\n" || !ignoreRepeated) {
				this.JustAddedNewline = true;
				this.Output.Add ("\n");
			}

			if (this.PreindentString != null && this.PreindentString.Length != 0)
				this.Output.Add (this.PreindentString);

			foreach (int i in Enumerable.Range(0, this.Flags.IndentationLevel + this.Flags.ChainExtraIndentation))
				this.Output.Add (this.IndentString);

			if (this.Flags.VarLine && this.Flags.VarLineReindented)
				this.Output.Add (this.IndentString);
		}

		private void Append (string s)
		{
			if (s == " ") {
				// do not add just a single space after the // comment, ever
				if (this.LastType == "TK_COMMENT") {
					this.AppendNewline ();
					return;
				}

				// make sure only single space gets drawn
				if (this.Flags.EatNextSpace)
					this.Flags.EatNextSpace = false;
				else if (this.Output.Count != 0 &&
				         this.Output [this.Output.Count - 1] != " " &&
				         this.Output [this.Output.Count - 1] != "\n" &&
				         this.Output [this.Output.Count - 1] != this.IndentString) {
					this.Output.Add (" ");
				}
			} else {
				this.JustAddedNewline = false;
				this.Flags.EatNextSpace = false;
				this.Output.Add (s);
			}
		}

		private void Indent ()
		{
			this.Flags.IndentationLevel = this.Flags.IndentationLevel + 1;
		}

		private void RemoveIndent ()
		{
			if (this.Output.Count != 0 &&
			    (this.Output [this.Output.Count - 1] == this.IndentString ||
			    this.Output [this.Output.Count - 1] == this.PreindentString)) {
				this.Output.RemoveAt (this.Output.Count - 1);
			}
		}

		private void RestoreMode ()
		{
			this.DoBlockJustClosed = this.Flags.Mode == "DO_BLOCK";
			if (this.FlagStore.Count > 0) {
				string mode = this.Flags.Mode;
				this.Flags = this.FlagStore [this.FlagStore.Count - 1];
				this.FlagStore.RemoveAt (this.FlagStore.Count - 1);
				this.Flags.PreviousMode = mode;
			}
		}

		private Tuple<string, string> GetNextToken ()
		{
			this.NNewlines = 0;

			if (this.ParserPos >= this.Input.Length)
				return new Tuple<string, string> ("", "TK_EOF");

			this.WantedNewline = false;
			char c = this.Input [this.ParserPos];
			this.ParserPos += 1;
			bool keepWhitespace = this.Opts.KeepArrayIndentation && this.IsArray (this.Flags.Mode);

			if (keepWhitespace) {
				int whitespaceCount = 0;

				while (this.Whitespace.Contains (c)) {
					if (c == '\n') {
						this.TrimOutput ();
						this.Output.Add ("\n");
						this.JustAddedNewline = true;
						whitespaceCount = 0;
					} else if (c == '\t')
						whitespaceCount += 4;
					else if (c == '\r') {
					} else
						whitespaceCount += 1;

					if (this.ParserPos >= this.Input.Length)
						return new Tuple<string, string> ("", "TK_EOF");

					c = this.Input [this.ParserPos];
					this.ParserPos += 1;
				}

				if (this.JustAddedNewline)
					foreach (int i in Enumerable.Range(0, whitespaceCount))
						this.Output.Add (" ");
			} else { //  not keep_whitespace
				while (this.Whitespace.Contains (c)) {
					if (c == '\n')
					if (this.Opts.MaxPreserveNewlines == 0 || this.Opts.MaxPreserveNewlines > this.NNewlines)
						this.NNewlines += 1;

					if (this.ParserPos >= this.Input.Length)
						return new Tuple<string, string> ("", "TK_EOF");

					c = this.Input [this.ParserPos];
					this.ParserPos += 1;
				}

				if (this.Opts.PreserveNewlines && this.NNewlines > 1)
					foreach (int i in Enumerable.Range(0, this.NNewlines)) {
						this.AppendNewline (i == 0);
						this.JustAddedNewline = true;
					}
				this.WantedNewline = this.NNewlines > 0;
			}

			string cc = c.ToString ();

			if (this.Wordchar.Contains (c)) {
				if (this.ParserPos < this.Input.Length) {
					cc = c.ToString ();
					while (this.Wordchar.Contains (this.Input [this.ParserPos])) {
						cc += this.Input [this.ParserPos];
						this.ParserPos += 1;
						if (this.ParserPos == this.Input.Length)
							break;
					}
				}

				// small and surprisingly unugly hack for 1E-10 representation
				if (this.ParserPos != this.Input.Length && "+-".Contains (this.Input [this.ParserPos]) && Regex.IsMatch (cc, "^[0-9]+[Ee]$")) {
					char sign = this.Input [this.ParserPos];
					this.ParserPos++;
					var t = this.GetNextToken ();
					cc += sign + t.Item1;
					return new Tuple<string, string> (cc, "TK_WORD");
				}

				if (cc == "in") // in is an operator, need to hack
					return new Tuple<string, string> (cc, "TK_OPERATOR");

				if (this.WantedNewline && this.LastType != "TK_OPERATOR" && this.LastType != "TK_EQUALS" &&
				    !this.Flags.IfLine && (this.Opts.PreserveNewlines || this.LastText != "var"))
					this.AppendNewline ();

				return new Tuple<string, string> (cc, "TK_WORD");
			}

			if ("([".Contains (c))
				return new Tuple<string, string> (c.ToString (), "TK_START_EXPR");

			if (")]".Contains (c))
				return new Tuple<string, string> (c.ToString (), "TK_END_EXPR");

			if (c == '{')
				return new Tuple<string, string> (c.ToString (), "TK_START_BLOCK");

			if (c == '}')
				return new Tuple<string, string> (c.ToString (), "TK_END_BLOCK");

			if (c == ';')
				return new Tuple<string, string> (c.ToString (), "TK_SEMICOLON");

			if (c == '/') {
				string comment = "";
				string commentMode = "TK_INLINE_COMMENT";

				if (this.Input [this.ParserPos] == '*') { // peek /* .. */ comment
					this.ParserPos += 1;
					if (this.ParserPos < this.Input.Length) {
						while (!(this.Input [this.ParserPos] == '*' && this.ParserPos + 1 < this.Input.Length && this.Input [this.ParserPos + 1] == '/') &&
						       this.ParserPos < this.Input.Length) {
							c = this.Input [this.ParserPos];
							comment += c;
							if ("\r\n".Contains (c))
								commentMode = "TK_BLOCK_COMMENT";
							this.ParserPos += 1;
							if (this.ParserPos >= this.Input.Length)
								break;
						}
					}

					this.ParserPos += 2;
					return new Tuple<string, string> ("/*" + comment + "*/", commentMode);
				}

				if (this.Input [this.ParserPos] == '/') { // peek // comment
					comment = c.ToString ();
					while (!("\r\n").Contains (this.Input [this.ParserPos])) {
						comment += this.Input [this.ParserPos];
						this.ParserPos += 1;
						if (this.ParserPos >= this.Input.Length)
							break;
					}
					if (this.WantedNewline)
						this.AppendNewline ();
					return new Tuple<string, string> (comment, "TK_COMMENT");
				}
			}

			if (c == '\'' || c == '"' ||
			    (c == '/' &&
			    ((this.LastType == "TK_WORD" && this.IsSpecialWord (this.LastText)) ||
			    (this.LastType == "TK_END_EXPR" && (this.Flags.PreviousMode == "(FOR-EXPRESSION)" || this.Flags.PreviousMode == "(COND-EXPRESSION)")) ||
			    ((new[] {
				"TK_COMMENT",
				"TK_START_EXPR",
				"TK_START_BLOCK",
				"TK_END_BLOCK",
				"TK_OPERATOR",
				"TK_EQUALS",
				"TK_EOF",
				"TK_SEMICOLON",
				"TK_COMMA"
			}).Contains (this.LastType))))) {
				char sep = c;
				bool esc = false;
				int esc1 = 0;
				int esc2 = 0;
				string resultingString = c.ToString ();
				bool inCharClass = false;
				if (this.ParserPos < this.Input.Length) {
					if (sep == '/') {
						// handle regexp
						inCharClass = false;
						while (esc || inCharClass || this.Input [this.ParserPos] != sep) {
							resultingString += this.Input [this.ParserPos];
							if (!esc) {
								esc = this.Input [this.ParserPos] == '\\';
								if (this.Input [this.ParserPos] == '[')
									inCharClass = true;
								else if (this.Input [this.ParserPos] == ']')
									inCharClass = false;
							} else
								esc = false;
							this.ParserPos += 1;
							if (this.ParserPos >= this.Input.Length)
								// ncomplete regex when end-of-file reached
								// bail out with what has received so far
								return new Tuple<string, string> (resultingString, "TK_STRING");
						}
					} else {
						// handle string
						while (esc || this.Input [this.ParserPos] != sep) {
							resultingString += this.Input [this.ParserPos];
							if (esc1 != 0 && esc1 >= esc2) {
								if (!int.TryParse (new string (resultingString.Skip (Math.Max (0, resultingString.Count () - esc2)).Take (esc2).ToArray ()), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out esc1))
									esc1 = 0;
								if (esc1 != 0 && esc1 >= 0x20 && esc1 <= 0x7e) {
									// FIXME
									resultingString = new string (resultingString.Take (2 + esc2).ToArray ());
									if ((char)esc1 == sep || (char)esc1 == '\\')
										resultingString += '\\';
									resultingString += (char)esc1;
								}
								esc1 = 0;
							}
							if (esc1 != 0)
								++esc1;
							else if (!esc)
								esc = this.Input [this.ParserPos] == '\\';
							else {
								esc = false;
								// TODO
								//if (/*this.Opts.UnescapeStrings*/false)
								/*{
                                    if (this.Input[this.ParserPos] == 'x')
                                    {
                                        ++esc1;
                                        esc2 = 2;
                                    }
                                    else if (this.Input[this.ParserPos] == 'u')
                                    {
                                        ++esc1;
                                        esc2 = 4;
                                    }
                                }*/
							}
							this.ParserPos += 1;
							if (this.ParserPos >= this.Input.Length)
								// incomplete string when end-of-file reached
								// bail out with what has received so far
								return new Tuple<string, string> (resultingString, "TK_STRING");
						}
					}
				}

				this.ParserPos += 1;
				resultingString += sep;
				if (sep == '/') {
					// regexps may have modifiers /regexp/MOD, so fetch those too
					while (this.ParserPos < this.Input.Length && this.Wordchar.Contains (this.Input [this.ParserPos])) {
						resultingString += this.Input [this.ParserPos];
						this.ParserPos += 1;
					}
				}
				return new Tuple<string, string> (resultingString, "TK_STRING");
			}

			if (c == '#') {
				string resultString = "";
				// she-bang
				if (this.Output.Count == 0 && this.Input.Length > 1 && this.Input [this.ParserPos] == '!') {
					resultString = c.ToString ();
					while (this.ParserPos < this.Input.Length && c != '\n') {
						c = this.Input [this.ParserPos];
						resultString += c;
						this.ParserPos += 1;
					}
					this.Output.Add (resultString.Trim () + '\n');
					this.AppendNewline ();
					return this.GetNextToken ();
				}

				//  Spidermonkey-specific sharp variables for circular references
				// https://developer.mozilla.org/En/Sharp_variables_in_JavaScript
				// http://mxr.mozilla.org/mozilla-central/source/js/src/jsscan.cpp around line 1935
				string sharp = "#";

				if (this.ParserPos < this.Input.Length && this.Digits.Contains (this.Input [this.ParserPos])) {
					while (true) {
						c = this.Input [this.ParserPos];
						sharp += c;
						this.ParserPos += 1;
						if (this.ParserPos >= this.Input.Length || c == '#' || c == '=')
							break;
					}
				}

				if (c == '#' || this.ParserPos >= this.Input.Length) {
					// pass
				} else if (this.Input [this.ParserPos] == '[' && this.Input [this.ParserPos + 1] == ']') {
					sharp += "[]";
					this.ParserPos += 2;
				} else if (this.Input [this.ParserPos] == '{' && this.Input [this.ParserPos + 1] == '}') {
					sharp += "{}";
					this.ParserPos += 2;
				}
				return new Tuple<string, string> (sharp, "TK_WORD");
			}

			if (c == '<' && this.Input.Substring (this.ParserPos - 1, Math.Min (4, this.Input.Length - this.ParserPos + 1)) == "<!--") {
				this.ParserPos += 3;
				string ss = "<!--";
				while (this.ParserPos < this.Input.Length && this.Input [this.ParserPos] != '\n') {
					ss += this.Input [this.ParserPos];
					this.ParserPos += 1;
				}
				this.Flags.InHtmlComment = true;
				return new Tuple<string, string> (ss, "TK_COMMENT");
			}

			if (c == '-' && this.Flags.InHtmlComment && this.Input.Substring (this.ParserPos - 1, 3) == "-->") {
				this.Flags.InHtmlComment = false;
				this.ParserPos += 2;
				if (this.WantedNewline)
					this.AppendNewline ();
				return new Tuple<string, string> ("-->", "TK_COMMENT");
			}

			if (c == '.')
				return new Tuple<string, string> (".", "TK_DOT");

			if (this.Punct.Contains (c.ToString ())) {
				string ss = c.ToString ();
				while (this.ParserPos < this.Input.Length && this.Punct.Contains (ss + this.Input [this.ParserPos])) {
					ss += this.Input [this.ParserPos];
					this.ParserPos += 1;
					if (this.ParserPos >= this.Input.Length)
						break;
				}

				if (ss == "=")
					return new Tuple<string, string> ("=", "TK_EQUALS");

				if (ss == ",")
					return new Tuple<string, string> (",", "TK_COMMA");

				return new Tuple<string, string> (ss, "TK_OPERATOR");
			}

			return new Tuple<string, string> (c.ToString (), "TK_UNKNOWN");
		}

		private void HandleStartExpr (string  tokenText)
		{
			if (tokenText == "[") {
				if (this.LastType == "TK_WORD" || this.LastText == ")") {
					if (this.LineStarters.Contains (this.LastText))
						this.Append (" ");

					this.SetMode ("(EXPRESSION)");
					this.Append (tokenText);
					return;
				}

				if (this.Flags.Mode == "[EXPRESSION]" || this.Flags.Mode == "[INDENTED-EXPRESSION]") {
					if (this.LastLastText == "]" && this.LastText == ",") {
						// # ], [ goes to a new line
						if (this.Flags.Mode == "[EXPRESSION]") {
							this.Flags.Mode = "[INDENTED-EXPRESSION]";
							if (!this.Opts.KeepArrayIndentation)
								this.Indent ();
						}
						this.SetMode ("[EXPRESSION]");
						if (!this.Opts.KeepArrayIndentation)
							this.AppendNewline ();
					} else if (this.LastText == "[") {
						if (this.Flags.Mode == "[EXPRESSION]") {
							this.Flags.Mode = "[INDENTED-EXPRESSION]";
							if (!this.Opts.KeepArrayIndentation)
								this.Indent ();
						}
						this.SetMode ("[EXPRESSION]");
						if (!this.Opts.KeepArrayIndentation)
							this.AppendNewline ();
					} else
						this.SetMode ("[EXPRESSION]");
				} else
					this.SetMode ("[EXPRESSION]");
			} else {
				if (this.LastText == "for")
					this.SetMode ("(FOR-EXPRESSION)");
				else if (this.LastText == "if" || this.LastText == "while")
					this.SetMode ("(COND-EXPRESSION)");
				else
					this.SetMode ("(EXPRESSION)");
			}

			if (this.LastText == ";" || this.LastType == "TK_START_BLOCK")
				this.AppendNewline ();
			else if (this.LastType == "TK_END_EXPR" || this.LastType == "TK_START_EXPR" || this.LastType == "TK_END_BLOCK" || this.LastText == ".") {
				// do nothing on (( and )( and ][ and ]( and .(
				if (this.WantedNewline)
					this.AppendNewline ();
			} else if (this.LastType != "TK_WORD" && this.LastType != "TK_OPERATOR")
				this.Append (" ");
			else if (this.LastWord == "function" || this.LastWord == "typeof") {
				// function() vs function (), typeof() vs typeof ()
				if (this.Opts.JslintHappy)
					this.Append (" ");
			} else if (this.LineStarters.Contains (this.LastText) || this.LastText == "catch")
				this.Append (" ");

			this.Append (tokenText);
		}

		private void HandleEndExpr (string tokenText)
		{
			if (tokenText == "]") {
				if (this.Opts.KeepArrayIndentation) {
					if (this.LastText == "}") {
						this.RemoveIndent ();
						this.Append (tokenText);
						this.RestoreMode ();
						return;
					}
				} else if (this.Flags.Mode == "[INDENTED-EXPRESSION]") {
					if (this.LastText == "]") {
						this.RestoreMode ();
						this.AppendNewline ();
						this.Append (tokenText);
						return;
					}
				}
			}
			this.RestoreMode ();
			this.Append (tokenText);
		}

		private void HandleStartBlock (string tokenText)
		{
			if (this.LastWord == "do")
				this.SetMode ("DO_BLOCK");
			else
				this.SetMode ("BLOCK");

			if (this.Opts.BraceStyle == JSBraceStyle.Expand) {
				if (this.LastType != "TK_OPERATOR") {
					if (this.LastText == "=" || (this.IsSpecialWord (this.LastText) && this.LastText != "else"))
						this.Append (" ");
					else
						this.AppendNewline (true);
				}
				this.Append (tokenText);
				this.Indent ();
			} else {
				if (this.LastType != "TK_OPERATOR" && this.LastType != "TK_START_EXPR") {
					if (this.LastType == "TK_START_BLOCK")
						this.AppendNewline ();
					else
						this.Append (" ");
				} else {
					// if TK_OPERATOR or TK_START_EXPR
					if (this.IsArray (this.Flags.PreviousMode) && this.LastText == ",") {
						if (this.LastLastText == "}")
							this.Append (" ");
						else
							this.AppendNewline ();
					}
				}
				this.Indent ();
				this.Append (tokenText);
			}
		}

		private void HandleEndBlock (string tokenText)
		{
			this.RestoreMode ();
			if (this.Opts.BraceStyle == JSBraceStyle.Expand) {
				if (this.LastText != "{")
					this.AppendNewline ();
			} else {
				if (this.LastType == "TK_START_BLOCK") {
					if (this.JustAddedNewline)
						this.RemoveIndent ();
					else
						this.TrimOutput ();
				} else {
					if (this.IsArray (this.Flags.Mode) && this.Opts.KeepArrayIndentation) {
						this.Opts.KeepArrayIndentation = false;
						this.AppendNewline ();
						this.Opts.KeepArrayIndentation = true;
					} else
						this.AppendNewline ();
				}
			}
			this.Append (tokenText);
		}

		private void HandleWord (string tokenText)
		{
			if (this.DoBlockJustClosed) {
				this.Append (" ");
				this.Append (tokenText);
				this.Append (" ");
				this.DoBlockJustClosed = false;
				return;
			}
			if (tokenText == "function") {
				if (this.Flags.VarLine && this.LastText != "=")
					this.Flags.VarLineReindented = !this.Opts.KeepFunctionIndentation;

				if ((this.JustAddedNewline || this.LastText == ";") && this.LastText != "{") {
					// make sure there is a nice clean space of at least one blank line
					// before a new function definition
					int haveNewlines = this.NNewlines;
					if (!this.JustAddedNewline)
						haveNewlines = 0;
					if (!this.Opts.PreserveNewlines)
						haveNewlines = 1;
					foreach (int i in Enumerable.Range(0, 2 - haveNewlines))
						this.AppendNewline (false);
				}
				if ((this.LastText == "get" || this.LastText == "set" || this.LastText == "new") || this.LastType == "TK_WORD")
					this.Append (" ");

				if (this.LastType == "TK_WORD") {
					if (this.LastText == "get" || this.LastText == "set" || this.LastText == "new" || this.LastText == "return")
						this.Append (" ");
					else
						this.AppendNewline ();
				} else if (this.LastType == "TK_OPERATOR" || this.LastText == "=")
					// foo = function
					this.Append (" ");
				else if (this.IsExpression (this.Flags.Mode)) {
					// (function
				} else
					this.AppendNewline ();
				this.Append ("function");
				this.LastWord = "function";
				return;
			}

			if (tokenText == "case" || (tokenText == "default" && this.Flags.InCaseStatement)) {
				this.AppendNewline ();
				if (this.Flags.CaseBody) {
					this.RemoveIndent ();
					this.Flags.CaseBody = false;
					this.Flags.IndentationLevel -= 1;
				}
				this.Append (tokenText);
				this.Flags.InCase = true;
				this.Flags.InCaseStatement = true;
				return;
			}

			string prefix = "NONE";

			if (this.LastType == "TK_END_BLOCK") {
				if (tokenText != "else" && tokenText != "catch" && tokenText != "finally")
					prefix = "NEWLINE";
				else {
					if (this.Opts.BraceStyle == JSBraceStyle.Expand || this.Opts.BraceStyle == JSBraceStyle.EndExpand)
						prefix = "NEWLINE";
					else {
						prefix = "SPACE";
						this.Append (" ");
					}
				}
			} else if (this.LastType == "TK_SEMICOLON" && (this.Flags.Mode == "BLOCK" || this.Flags.Mode == "DO_BLOCK"))
				prefix = "NEWLINE";
			else if (this.LastType == "TK_SEMICOLON" && this.IsExpression (this.Flags.Mode))
				prefix = "SPACE";
			else if (this.LastType == "TK_STRING")
				prefix = "NEWLINE";
			else if (this.LastType == "TK_WORD") {
				if (this.LastText == "else") {
					// eat newlines between ...else *** some_op...
					// won't preserve extra newlines in this place (if any), but don't care that much
					this.TrimOutput (true);
				}
				prefix = "SPACE";
			} else if (this.LastType == "TK_START_BLOCK")
				prefix = "NEWLINE";
			else if (this.LastType == "TK_END_EXPR") {
				this.Append (" ");
				prefix = "NEWLINE";
			}

			if (this.Flags.IfLine && this.LastType == "TK_END_EXPR")
				this.Flags.IfLine = false;

			if (this.LineStarters.Contains (tokenText)) {
				if (this.LastText == "else")
					prefix = "SPACE";
				else
					prefix = "NEWLINE";
			}

			if (tokenText == "else" || tokenText == "catch" || tokenText == "finally") {
				if (this.LastType != "TK_END_BLOCK" || this.Opts.BraceStyle == JSBraceStyle.Expand || this.Opts.BraceStyle == JSBraceStyle.EndExpand)
					this.AppendNewline ();
				else {
					this.TrimOutput (true);
					this.Append (" ");
				}
			} else if (prefix == "NEWLINE") {
				if (this.IsSpecialWord (this.LastText)) {
					// no newline between return nnn
					this.Append (" ");
				} else if (this.LastType != "TK_END_EXPR") {
					if ((this.LastType != "TK_START_EXPR" || tokenText != "var") && this.LastText != ":") {
						// no need to force newline on VAR -
						// for (var x = 0...
						if (tokenText == "if" && this.LastWord == "else" && this.LastText != "{")
							this.Append (" ");
						else {
							this.Flags.VarLine = false;
							this.Flags.VarLineReindented = false;
							this.AppendNewline ();
						}
					}
				} else if (this.LineStarters.Contains (tokenText) && this.LastText != ")") {
					this.Flags.VarLine = false;
					this.Flags.VarLineReindented = false;
					this.AppendNewline ();
				}
			} else if (this.IsArray (this.Flags.Mode) && this.LastText == "," && this.LastLastText == "}")
				this.AppendNewline (); //}, in lists get a newline
			else if (prefix == "SPACE")
				this.Append (" ");

			this.Append (tokenText);
			this.LastWord = tokenText;

			if (tokenText == "var") {
				this.Flags.VarLine = true;
				this.Flags.VarLineReindented = false;
				this.Flags.VarLineTainted = false;
			}

			if (tokenText == "if")
				this.Flags.IfLine = true;

			if (tokenText == "else")
				this.Flags.IfLine = false;
		}

		private void HandleSemicolon (string tokenText)
		{
			this.Append (tokenText);
			this.Flags.VarLine = false;
			this.Flags.VarLineReindented = false;
			if (this.Flags.Mode == "OBJECT")
				// OBJECT mode is weird and doesn't get reset too well.
				this.Flags.Mode = "BLOCK";
		}

		private void HandleString (string tokenText)
		{
			if (this.LastType == "TK_END_EXPR" && (this.Flags.PreviousMode == "(COND-EXPRESSION)" || this.Flags.PreviousMode == "(FOR-EXPRESSION)"))
				this.Append (" ");

			if (this.LastType == "TK_COMMENT" || this.LastType == "TK_STRING" || this.LastType == "TK_START_BLOCK" || this.LastType == "TK_END_BLOCK" || this.LastType == "TK_SEMICOLON")
				this.AppendNewline ();
			else if (this.LastType == "TK_WORD")
				this.Append (" ");
			else if (this.Opts.PreserveNewlines && this.WantedNewline && this.Flags.Mode != "OBJECT") {
				this.AppendNewline ();
				this.Append (this.IndentString);
			}
			this.Append (tokenText);
		}

		private void HandleEquals (string tokenText)
		{
			if (this.Flags.VarLine)
				// just got an '=' in a var-line, different line breaking rules will apply
				this.Flags.VarLineTainted = true;
			this.Append (" ");
			this.Append (tokenText);
			this.Append (" ");
		}

		private void HandleComma (string tokenText)
		{
			if (this.LastType == "TK_COMMENT")
				this.AppendNewline ();

			if (this.Flags.VarLine) {
				if (this.IsExpression (this.Flags.Mode) || this.LastType == "TK_END_BLOCK") {
					// do not break on comma, for ( var a = 1, b = 2
					this.Flags.VarLineTainted = false;
				}
				if (this.Flags.VarLineTainted) {
					this.Append (tokenText);
					this.Flags.VarLineReindented = true;
					this.Flags.VarLineTainted = false;
					this.AppendNewline ();
					return;
				} else
					this.Flags.VarLineTainted = false;
				this.Append (tokenText);
				this.Append (" ");
				return;
			}

			if (this.LastType == "TK_END_BLOCK" && this.Flags.Mode != "(EXPRESSION)") {
				this.Append (tokenText);
				if (this.Flags.Mode == "OBJECT" && this.LastText == "}")
					this.AppendNewline ();
				else
					this.Append (" ");
			} else {
				if (this.Flags.Mode == "OBJECT") {
					this.Append (tokenText);
					this.AppendNewline ();
				} else {
					// EXPR or DO_BLOCK
					this.Append (tokenText);
					this.Append (" ");
				}
			}
		}

		private void HandleOperator (string tokenText)
		{
			bool spaceBefore = true;
			bool spaceAfter = true;

			if (this.IsSpecialWord (this.LastText)) {
				// return had a special handling in TK_WORD
				this.Append (" ");
				this.Append (tokenText);
				return;
			}

			// hack for actionscript's import .*;
			if (tokenText == "*" && this.LastType == "TK_DOT" && !this.LastLastText.All (char.IsDigit)) {
				this.Append (tokenText);
				return;
			}

			if (tokenText == ":" && this.Flags.InCase) {
				this.Flags.CaseBody = true;
				this.Indent ();
				this.Append (tokenText);
				this.AppendNewline ();
				this.Flags.InCase = true;
				return;
			}

			if (tokenText == "::") {
				// no spaces around the exotic namespacing syntax operator
				this.Append (tokenText);
				return;
			}

			if ((tokenText == "++" || tokenText == "--" || tokenText == "!") || (tokenText == "+" || tokenText == "-") &&
			    ((this.LastType == "TK_START_BLOCK" || this.LastType == "TK_START_EXPR" || this.LastType == "TK_EQUALS" || this.LastType == "TK_OPERATOR") ||
			    (this.LineStarters.Contains (this.LastText) || this.LastText == ","))) {
				spaceBefore = false;
				spaceAfter = false;

				if (this.LastText == ";" && this.IsExpression (this.Flags.Mode)) {
					// for (;; ++i)
					// ^^
					spaceBefore = true;
				}

				if (this.LastText == "TK_WORD" && this.LineStarters.Contains (this.LastText))
					spaceBefore = true;

				if (this.Flags.Mode == "BLOCK" && (this.LastText == ";" || this.LastText == "{")) {
					// { foo: --i }
					// foo(): --bar
					this.AppendNewline ();
				}
			} else if (tokenText == ":") {
				if (this.Flags.TernaryDepth == 0) {
					if (this.Flags.Mode == "BLOCK")
						this.Flags.Mode = "OBJECT";
					spaceBefore = false;
				} else
					this.Flags.TernaryDepth -= 1;
			} else if (tokenText == "?")
				this.Flags.TernaryDepth += 1;

			if (spaceBefore)
				this.Append (" ");

			this.Append (tokenText);

			if (spaceAfter)
				this.Append (" ");
		}

		private void HandleBlockComment (string tokenText)
		{
			string[] lines = tokenText.Replace ("\r", "").Split ('\n');
			// all lines start with an asterisk? that's a proper box comment

			if (!lines.Skip (1).Where (x => x.Trim () == "" || x.TrimStart () [0] != '*').Any (l => !string.IsNullOrEmpty (l))) {
				this.AppendNewline ();
				this.Append (lines [0]);
				foreach (string line in lines.Skip(1)) {
					this.AppendNewline ();
					this.Append (" " + line.Trim ());
				}
			} else {
				// simple block comment: leave intact
				if (lines.Length > 1) {
					// multiline comment starts on a new line
					this.AppendNewline ();
				} else {
					// single line /* ... */ comment stays on the same line
					this.Append (" ");
				}
				foreach (string line in lines) {
					this.Append (line);
					this.Append ("\n");
				}
			}
			this.AppendNewline ();
		}

		private void HandleInlineComment (string tokenText)
		{
			this.Append (" ");
			this.Append (tokenText);
			if (this.IsExpression (this.Flags.Mode))
				this.Append (" ");
			else
				this.AppendNewlineForced ();
		}

		private void HandleComment (string tokenText)
		{
			if (this.LastText == "," && !this.WantedNewline)
				this.TrimOutput (true);

			if (this.LastType != "TK_COMMENT") {
				if (this.WantedNewline)
					this.AppendNewline ();
				else
					this.Append (" ");
			}

			this.Append (tokenText);
			this.AppendNewline ();
		}

		private void HandleDot (string tokenText)
		{
			if (this.IsSpecialWord (this.LastText))
				this.Append (" ");
			else if (this.LastText == ")") {
				if (this.Opts.BreakChainedMethods || this.WantedNewline) {
					this.Flags.ChainExtraIndentation = 1;
					this.AppendNewline (true, false);
				}
			}
			this.Append (tokenText);
		}

		private void HandleUnknown (string tokenText)
		{
			this.Append (tokenText);
		}
	}
}

