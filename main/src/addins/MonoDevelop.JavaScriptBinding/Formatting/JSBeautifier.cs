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

namespace MonoDevelop.JavaScript.Formatting
{
	public class JSBeautifyOptions
	{
		public int IndentSize { get; set; }

		public char IndentChar { get; set; }

		public int IndentLevel { get; set; }

		public bool PreserveNewlines { get; set; }
	}

	public class JSBeautify
	{
		StringBuilder output;
		string indentString;
		int indentLevel;
		string tokenText;
		Stack<string> modes;
		string currentMode;
		int optIndentSize;
		char optIndentChar;
		int optIndentLevel;
		bool optPreserveNewlines;
		bool ifLineFlag;
		bool doBlockJustClosed;
		string input;

		void trimOutput ()
		{
			while ((output.Length > 0) && ((output [output.Length - 1] == ' ') || (output [output.Length - 1].ToString () == indentString))) {
				output.Remove (output.Length - 1, 1);
			}
		}

		void printNewline (bool? ignore_repeated)
		{
			ignore_repeated = ignore_repeated ?? true;

			ifLineFlag = false;
			trimOutput ();

			if (output.Length == 0)
				return;

			if ((output [output.Length - 1] != '\n') || !ignore_repeated.Value) {
				output.Append (Environment.NewLine);
			}

			for (var i = 0; i < indentLevel; i++) {
				output.Append (indentString);
			}
		}

		void printSpace ()
		{
			var last_output = " ";
			if (output.Length > 0)
				last_output = output [output.Length - 1].ToString ();
			if ((last_output != " ") && (last_output != "\n") && (last_output != indentString)) {
				output.Append (' ');
			}
		}

		void printToken ()
		{
			output.Append (tokenText);
		}

		void indent ()
		{
			indentLevel++;
		}

		void unIndent ()
		{
			if (indentLevel > 0)
				indentLevel--;
		}

		void removeIndent ()
		{
			if ((output.Length > 0) && (output [output.Length - 1].ToString () == indentString)) {
				output.Remove (output.Length - 1, 1);
			}
		}

		void setMode (string mode)
		{
			modes.Push (currentMode);
			currentMode = mode;
		}

		void restoreMode ()
		{
			doBlockJustClosed = (currentMode == "DO_BLOCK");
			currentMode = modes.Pop ();
		}

		bool inArray (object what, ArrayList arr)
		{
			return arr.Contains (what);

		}

		bool isTernaryOperator ()
		{
			int level = 0;
			int colon_count = 0;
			for (var i = output.Length - 1; i >= 0; i--) {
				switch (output [i]) {
				case ':':
					if (level == 0)
						colon_count++;
					break;
				case '?':
					if (level == 0) {
						if (colon_count == 0) {
							return true;
						} else {
							colon_count--;
						}
					}
					break;
				case '{':
					if (level == 0)
						return false;
					level--;
					break;
				case '(':
				case '[':
					level--;
					break;
				case ')':
				case ']':
				case '}':
					level++;
					break;
				}
			}
			return false;
		}

		string whitespace;
		string wordchar;
		int parserPosition;
		string lastType;
		string lastText;
		string digits;
		string[] punct;
		string prefix;

		string[] getNextToken (ref int parserPosition)
		{
			var nNewlines = 0;

			if (parserPosition >= input.Length) {
				return new string[] { "", "TK_EOF" };
			}

			string c = input [parserPosition].ToString ();
			parserPosition++;

			while (whitespace.Contains (c)) {
				if (parserPosition >= input.Length) {
					return new string[] { "", "TK_EOF" };
				}

				if (c == "\n")
					nNewlines++;

				c = input [parserPosition].ToString ();
				parserPosition++;
			}

			var wantedNewline = false;

			if (optPreserveNewlines) {
				if (nNewlines > 1) {
					for (var i = 0; i < 2; i++) {
						printNewline (i == 0);
					}
				}
				wantedNewline = (nNewlines == 1);

			}

			if (wordchar.Contains (c)) {
				if (parserPosition < input.Length) {
					while (wordchar.Contains (input [parserPosition].ToString ())) {
						c += input [parserPosition];
						parserPosition++;
						if (parserPosition == input.Length)
							break;
					}
				}


				if ((parserPosition != input.Length) && (Regex.IsMatch (c, "^[0-9]+[Ee]$")) && ((input [parserPosition] == '-') || (input [parserPosition] == '+'))) {
					var sign = input [parserPosition];
					parserPosition++;

					var t = getNextToken (ref parserPosition);
					c += sign + t [0];
					return new string[] { c, "TK_WORD" };
				}

				if (c == "in") {
					return new string[] { c, "TK_OPERATOR" };
				}

				if (wantedNewline && lastType != "TK_OPERATOR" && !ifLineFlag) {
					printNewline (null);
				}
				return new string[] { c, "TK_WORD" };

			}

			if ((c == "(") || (c == "["))
				return new string[] { c, "TK_START_EXPR" };

			if (c == ")" || c == "]") {
				return new string[] { c, "TK_END_EXPR" };
			}

			if (c == "{") {
				return new string[] { c, "TK_START_BLOCK" };
			}

			if (c == "}") {
				return new string[] { c, "TK_END_BLOCK" };
			}

			if (c == ";") {
				return new string[] { c, "TK_SEMICOLON" };
			}

			if (c == "/") {
				var comment = "";
				if (input [parserPosition] == '*') {
					parserPosition++;
					if (parserPosition < input.Length) {
						while (!((input [parserPosition] == '*') && (input [parserPosition + 1] > '\0') && (input [parserPosition + 1] == '/') && (parserPosition < input.Length))) {
							comment += input [parserPosition];
							parserPosition++;
							if (parserPosition >= input.Length) {
								break;
							}
						}
					}

					parserPosition += 2;
					return new string[] { "/*" + comment + "*/", "TK_BLOCK_COMMENT" };
				}   

				if (input [parserPosition] == '/') {
					comment = c;
					while ((input [parserPosition] != '\x0d') && (input [parserPosition] != '\x0a')) {
						comment += input [parserPosition];
						parserPosition++;
						if (parserPosition >= input.Length) {
							break;
						}
					}

					parserPosition++;
					if (wantedNewline) {
						printNewline (null);
					}
					return new string[] { comment, "TK_COMMENT" };

				}
			}

			if ((c == "'") || (c == "\"") || ((c == "/")
			    && ((lastType == "TK_WORD" && lastText == "return") || ((lastType == "TK_START_EXPR") || (lastType == "TK_START_BLOCK") || (lastType == "TK_END_BLOCK")
			    || (lastType == "TK_OPERATOR") || (lastType == "TK_EOF") || (lastType == "TK_SEMICOLON"))))) {
				var sep = c;
				var esc = false;
				var resultingString = c;

				if (parserPosition < input.Length) {
					if (sep == "/") {
						var inCharClass = false;
						while ((esc) || (inCharClass) || (input [parserPosition].ToString () != sep)) {
							resultingString += input [parserPosition];
							if (!esc) {
								esc = input [parserPosition] == '\\';
								if (input [parserPosition] == '[') {
									inCharClass = true;
								} else if (input [parserPosition] == ']') {
									inCharClass = false;
								}
							} else {
								esc = false;
							}
							parserPosition++;
							if (parserPosition >= input.Length) {
								return new string[] { resultingString, "TK_STRING" };
							}
						} 
					} else {
						while ((esc) || (input [parserPosition].ToString () != sep)) {
							resultingString += input [parserPosition];
							if (!esc) {
								esc = input [parserPosition] == '\\';
							} else {
								esc = false;
							}
							parserPosition++;
							if (parserPosition >= input.Length) {
								return new string[] { resultingString, "TK_STRING" };
							}
						}
					}
				}

				parserPosition += 1;

				resultingString += sep;

				if (sep == "/") {
					// regexps may have modifiers /regexp/MOD , so fetch those, too
					while ((parserPosition < input.Length) && (wordchar.Contains (input [parserPosition].ToString ()))) {
						resultingString += input [parserPosition];
						parserPosition += 1;
					}
				}
				return new string []  { resultingString, "TK_STRING" };


			}

			if (c == "#") {
				var sharp = "#";
				if ((parserPosition < input.Length) && (digits.Contains (input [parserPosition].ToString ()))) {
					do {
						c = input [parserPosition].ToString ();
						sharp += c;
						parserPosition += 1;
					} while ((parserPosition < input.Length) && (c != "#") && (c != "="));
					if (c == "#") {
						return new string[] { sharp, "TK_WORD" };
						;
					} else {
						return new string[] { sharp, "TK_OPERATOR" };
						;
					}
				}
			}


			if ((c == "<") && (input.Substring (parserPosition - 1, 3) == "<!--")) {
				parserPosition += 3;
				return new string[] { "<!--", "TK_COMMENT" };
				;
			}

			if ((c == "-") && (input.Substring (parserPosition - 1, 2) == "-->")) {
				parserPosition += 2;
				if (wantedNewline) {
					printNewline (null);
				}
				return new string[] { "-->", "TK_COMMENT" };
			}

			if (punct.Contains (c.ToString ())) {
				while ((parserPosition < input.Length) && (punct.Contains (c + input [parserPosition].ToString ()))) {
					c += input [parserPosition];
					parserPosition += 1;
					if (parserPosition >= input.Length) {
						break;
					}
				}

				return new string[] { c, "TK_OPERATOR" };
			}

			return new string[] { c, "TK_UNKNOWN" };


		}

		string lastWord;
		bool varLine;
		bool varLineTainted;
		string[] lineStarters;
		bool inCase;
		string tokenType;

		public string GetResult ()
		{
			if (addScriptTags) {
				output.AppendLine ().AppendLine ("</script>");
			}

			return output.ToString ();
		}

		bool addScriptTags;

		public JSBeautify (string jsSourceText, JSBeautifyOptions options)
		{
			optIndentSize = options.IndentSize;
			optIndentChar = options.IndentChar;;
			optIndentLevel = options.IndentLevel;
			optPreserveNewlines = options.PreserveNewlines;
			output = new StringBuilder ();
			modes = new Stack<string> ();

			indentString = "";

			while (optIndentSize > 0) {
				indentString += optIndentChar;
				optIndentSize -= 1;
			}

			for (var i = 0; i < indentLevel; i++) {
				output.Append (indentString);
			}

			indentLevel = optIndentLevel;

			input = jsSourceText.Replace ("<script type=\"text/javascript\">", "").Replace ("</script>", "");
			if (input.Length != jsSourceText.Length) {
				output.AppendLine ("<script type=\"text/javascript\">");
				addScriptTags = true;
			}

			lastWord = ""; // last 'TK_WORD' passed
			lastType = "TK_START_EXPR"; // last token type
			lastText = ""; // last token text

			doBlockJustClosed = false;
			varLine = false;         // currently drawing var .... ;
			varLineTainted = false; // false: var a = 5; true: var a = 5, b = 6

			whitespace = "\n\r\t ";
			wordchar = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_$";
			digits = "0123456789";

			// <!-- is a special case (ok, it's a minor hack actually)
			punct = "+ - * / % & ++ -- = += -= *= /= %= == === != !== > < >= <= >> << >>> >>>= >>= <<= && &= | || ! !! , : ? ^ ^= |= ::".Split (' ');

			// words which should always start on new line.
			lineStarters = "continue,try,throw,return,var,if,switch,case,default,for,while,break,function".Split (',');

			// states showing if we are currently in expression (i.e. "if" case) - 'EXPRESSION', or in usual block (like, procedure), 'BLOCK'.
			// some formatting depends on that.
			currentMode = "BLOCK";
			modes.Push (currentMode);

			parserPosition = 0;
			inCase = false;

			while (true) {
				var t = getNextToken (ref parserPosition);
				tokenText = t [0];
				tokenType = t [1];
				if (tokenType == "TK_EOF") {
					break;
				}

				switch (tokenType) {

				case "TK_START_EXPR":
					varLine = false;
					setMode ("EXPRESSION");
					if ((lastText == ";") || (lastType == "TK_START_BLOCK")) {
						printNewline (null);
					} else if ((lastType == "TK_END_EXPR") || (lastType == "TK_START_EXPR")) {
						// do nothing on (( and )( and ][ and ]( ..
					} else if ((lastType != "TK_WORD") && (lastType != "TK_OPERATOR")) {
						printSpace ();
					} else if (lineStarters.Contains (lastWord)) {
						printSpace ();
					}
					printToken ();
					break;

				case "TK_END_EXPR":
					printToken ();
					restoreMode ();
					break;

				case "TK_START_BLOCK":

					if (lastWord == "do") {
						setMode ("DO_BLOCK");
					} else {
						setMode ("BLOCK");
					}
					if ((lastType != "TK_OPERATOR") && (lastType != "TK_START_EXPR")) {
						if (lastType == "TK_START_BLOCK") {
							printNewline (null);
						} else {
							printSpace ();
						}
					}
					printToken ();
					indent ();
					break;

				case "TK_END_BLOCK":
					if (lastType == "TK_START_BLOCK") {
						// nothing
						trimOutput ();
						unIndent ();
					} else {
						unIndent ();
						printNewline (null);
					}
					printToken ();
					restoreMode ();
					break;

				case "TK_WORD":

					if (doBlockJustClosed) {
						// do {} ## while ()
						printSpace ();
						printToken ();
						printSpace ();
						doBlockJustClosed = false;
						break;
					}

					if ((tokenText == "case") || (tokenText == "default")) {
						if (lastText == ":") {
							// switch cases following one another
							removeIndent ();
						} else {
							// case statement starts in the same line where switch
							unIndent ();
							printNewline (null);
							indent ();
						}
						printToken ();
						inCase = true;
						break;
					}

					prefix = "NONE";

					if (lastType == "TK_END_BLOCK") {
						if (!(new string[] { "else", "catch", "finally" }).Contains (tokenText.ToLower ())) {
							prefix = "NEWLINE";
						} else {
							prefix = "SPACE";
							printSpace ();
						}
					} else if ((lastType == "TK_SEMICOLON") && ((currentMode == "BLOCK") || (currentMode == "DO_BLOCK"))) {
						prefix = "NEWLINE";
					} else if ((lastType == "TK_SEMICOLON") && (currentMode == "EXPRESSION")) {
						prefix = "SPACE";
					} else if (lastType == "TK_STRING") {
						prefix = "NEWLINE";
					} else if (lastType == "TK_WORD") {
						prefix = "SPACE";
					} else if (lastType == "TK_START_BLOCK") {
						prefix = "NEWLINE";
					} else if (lastType == "TK_END_EXPR") {
						printSpace ();
						prefix = "NEWLINE";
					}

					if ((lastType != "TK_END_BLOCK") && ((new string[] {
						"else",
						"catch",
						"finally"
					}).Contains (tokenText.ToLower ()))) {
						printNewline (null);
					} else if ((lineStarters.Contains (tokenText)) || (prefix == "NEWLINE")) {
						if (lastText == "else") {
							// no need to force newline on else break
							printSpace ();
						} else if (((lastType == "TK_START_EXPR") || (lastText == "=") || (lastText == ",")) && (tokenText == "function")) {
							// no need to force newline on "function": (function
							// DONOTHING
						} else if ((lastType == "TK_WORD") && ((lastText == "return") || (lastText == "throw"))) {
							// no newline between "return nnn"
							printSpace ();
						} else if (lastType != "TK_END_EXPR") {
							if (((lastType != "TK_START_EXPR") || (tokenText != "var")) && (lastText != ":")) {
								// no need to force newline on "var": for (var x = 0...)
								if ((tokenText == "if") && (lastType == "TK_WORD") && (lastWord == "else")) {
									// no newline for } else if {
									printSpace ();
								} else {
									printNewline (null);
								}
							}
						} else {
							if ((lineStarters.Contains (tokenText)) && (lastText != ")")) {
								printNewline (null);
							}
						}
					} else if (prefix == "SPACE") {
						printSpace ();
					}
					printToken ();
					lastWord = tokenText;

					if (tokenText == "var") {
						varLine = true;
						varLineTainted = false;
					}

					if (tokenText == "if" || tokenText == "else") {
						ifLineFlag = true;
					}

					break;

				case "TK_SEMICOLON":

					printToken ();
					varLine = false;
					break;

				case "TK_STRING":

					if ((lastType == "TK_START_BLOCK") || (lastType == "TK_END_BLOCK") || (lastType == "TK_SEMICOLON")) {
						printNewline (null);
					} else if (lastType == "TK_WORD") {
						printSpace ();
					}
					printToken ();
					break;

				case "TK_OPERATOR":

					var startDelim = true;
					var endDelim = true;
					if (varLine && (tokenText != ",")) {
						varLineTainted = true;
						if (tokenText == ":") {
							varLine = false;
						}
					}
					if (varLine && (tokenText == ",") && (currentMode == "EXPRESSION")) {
						// do not break on comma, for(var a = 1, b = 2)
						varLineTainted = false;
					}

					if (tokenText == ":" && inCase) {
						printToken (); // colon really asks for separate treatment
						printNewline (null);
						inCase = false; 
						break;
					}

					if (tokenText == "::") {
						// no spaces around exotic namespacing syntax operator
						printToken ();
						break;
					}

					if (tokenText == ",") {
						if (varLine) {
							if (varLineTainted) {
								printToken ();
								printNewline (null);
								varLineTainted = false;
							} else {
								printToken ();
								printSpace ();
							}
						} else if (lastType == "TK_END_BLOCK") {
							printToken ();
							printNewline (null);
						} else {
							if (currentMode == "BLOCK") {
								printToken ();
								printNewline (null);
							} else {
								// EXPR od DO_BLOCK
								printToken ();
								printSpace ();
							}
						}
						break;
					} else if ((tokenText == "--") || (tokenText == "++")) { // unary operators special case
						if (lastText == ";") {
							if (currentMode == "BLOCK") {
								// { foo; --i }
								printNewline (null);
								startDelim = true;
								endDelim = false;
							} else {
								// space for (;; ++i)
								startDelim = true;
								endDelim = false;
							}
						} else {
							if (lastText == "{") {
								// {--i
								printNewline (null);
							}
							startDelim = false;
							endDelim = false;
						}
					} else if (((tokenText == "!") || (tokenText == "+") || (tokenText == "-")) && ((lastText == "return") || (lastText == "case"))) {
						startDelim = true;
						endDelim = false;
					} else if (((tokenText == "!") || (tokenText == "+") || (tokenText == "-")) && (lastType == "TK_START_EXPR")) {
						// special case handling: if (!a)
						startDelim = false;
						endDelim = false;
					} else if (lastType == "TK_OPERATOR") {
						startDelim = false;
						endDelim = false;
					} else if (lastType == "TK_END_EXPR") {
						startDelim = true;
						endDelim = true;
					} else if (tokenText == ".") {
						// decimal digits or object.property
						startDelim = false;
						endDelim = false;

					} else if (tokenText == ":") {
						if (isTernaryOperator ()) {
							startDelim = true;
						} else {
							startDelim = false;
						}
					}
					if (startDelim) {
						printSpace ();
					}

					printToken ();

					if (endDelim) {
						printSpace ();
					}
					break;

				case "TK_BLOCK_COMMENT":

					printNewline (null);
					printToken ();
					printNewline (null);
					break;

				case "TK_COMMENT":

					// print_newline();
					printSpace ();
					printToken ();
					printNewline (null);
					break;

				case "TK_UNKNOWN":
					printToken ();
					break;
				}

				lastType = tokenType;
				lastText = tokenText;
			}


		}
	}
}

