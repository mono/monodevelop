// 
// StringEscaping.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Globalization;
using System.Text;

namespace MonoDevelop.Gettext
{
	static class StringEscaping
	{
		//based on http://www-ccs.ucsd.edu/c/charset.html
		//with modififications, as Gettext dosn't follow all C escaping
		public static string ToGettextFormat (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				switch (c) {
				case '"':
					sb.Append ("\\\"");
					continue;
				
				//Gettext doesn't like escaping this
				//case '\'':
				//	sb.Append ("\\'");
				//	continue;
				
				//These shouldn't be in translations... but will be caught by IsControl
				//case '\a':
				//case '\b':
				//case '\f':
				//case '\v':
					
				//this doesn't matter since we're not dealing with trigraphs
				//case "?":
				//	sb.Append ("\\?");
				//	continue;
					
				case '\\':
					sb.Append ("\\\\");
					continue;
				case '\n':
					sb.Append ("\\n");
					continue;
				case '\r':
					sb.Append ("\\r");
					continue;
				case '\t':
					sb.Append ("\\t");
					continue;
				}
				
				if (c != '_' && char.IsControl (c))
					throw new FormatException (String.Format (MonoDevelop.Core.GettextCatalog.GetString ("Invalid character '{0}' in translatable string: '{1}'"),
					                                          c,
					                                          text));
				
				sb.Append (c);
			}
			return sb.ToString ();
		}
		
		public static void AppendFromGettextFormat (this StringBuilder sb, string text)
		{
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				switch (c) {
				case '\\':
					if (i + 1 < text.Length) {
						char nextChar = text [i + 1];
						if (nextChar == '\\' || nextChar == '"') {
							sb.Append (nextChar);
							i++;
							continue;
						}
						if (nextChar == 'n') {
							sb.Append ('\n');
							i++;
							continue;
						}
						if (nextChar == 't') {
							sb.Append ('\t');
							i++;
							continue;
						}
						if (nextChar == 'r') {
							sb.Append ('\r');
							i++;
							continue;
						}
						throw new FormatException (MonoDevelop.Core.GettextCatalog.GetString (
							"Invalid escape sequence '{0}' in string: '{1}'", nextChar, text));
					}
					break;
				}
				
				sb.Append (c);
			}
		}
		
		public static string FromGettextFormat (string text)
		{
			var sb = new StringBuilder ();
			AppendFromGettextFormat(sb, text);
			return sb.ToString ();
		}
		
		public static string UnEscape (EscapeMode mode, string text)
		{
			switch (mode) {
			case EscapeMode.None:
				return text;
			case EscapeMode.CSharp:
				return FromCSharpFormat (text);
			case EscapeMode.CSharpVerbatim:
				return FromCSharpVerbatimFormat (text);
			case EscapeMode.Xml:
				return FromXml (text);
			default:
				throw new Exception ("Unknown string escaping mode '" + mode.ToString () + "'");
			}
		}
		
		/*
		//based on http://www-ccs.ucsd.edu/c/charset.html 
		public static string FromCFormat (string text)
		{
		}
		*/
		
		//based on the C# 2.0 spec
		static string FromCSharpVerbatimFormat (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c1 = text[i];
				if (c1 == '"') {
					i++;
					char c2 = text[i];
					if (c2 != '"')
						throw new FormatException ("Unescaped '\"' character in C# verbatim string.");
				}
				sb.Append (c1);
			}
			return sb.ToString ();	
		}
		
		static string FromXml (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c1 = text[i];
				if (c1 == '&') {
					int end = text.IndexOf (';', i);
					if (end == -1)
						throw new FormatException ("Unterminated XML entity.");
					string entity = text.Substring (i+1, end - i - 1);
					switch (entity) {
					case "lt":
						sb.Append ('<');
						break;
					case "gt":
						sb.Append ('>');
						break;
					case "amp":
						sb.Append ('&');
						break;
					case "apos":
						sb.Append ('\'');
						break;
					case "quot":
						sb.Append ('"');
						break;
					default:
						throw new FormatException ("Unrecogised XML entity '&" + entity + ";'.");
					}
					i = end;
				} else
					sb.Append (c1);
			}
			return sb.ToString ();	
		}
		
		//based on the C# 2.0 spec
		static string FromCSharpFormat (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c1 = text[i];
				if (c1 != '\\') {
					sb.Append (c1);
					continue;
				}
				
				i++;
				char c2 = text[i];
				
				switch (c2) {
				case '\'':
				case '"':
				case '\\':
					sb.Append (c2);
					break;
				case 'n':
					sb.Append ('\n');
					break;
				case 'r':
					sb.Append ('\r');
					break;
				case 't':
					sb.Append ('\t');
					break;
				case 'U':
					uint Uc;
					if (!TryParseHex (text, i + 1, 8, out Uc) || NeedsEscaping (Uc)) {
						throw new FormatException ("Invalid escape '\\" + text.Substring (i, 9) + "' in translatable string.");
					}
					sb.Append (char.ConvertFromUtf32 ((int)Uc));
					i += 8;
					break;
				case 'u':
					uint uc;
					if (!TryParseHex (text, i + 1, 4, out uc) || NeedsEscaping (uc)) {
						throw new FormatException ("Invalid escape '\\" + text.Substring (i, 5) + "' in translatable string.");
					}
					sb.Append ((char)uc);
					i += 4;
					break;
				case 'x':
					//FIXME hex unicode
					//break;
					//if (char.IsControl (c);
					
				//case '0':
				//case 'a':
				//case 'b':
				//case 'f':
				//case 'v':
				default:
					throw new FormatException ("Invalid escape '\\" + c2 + "' in translatable string.");
				}
				
			}
			return sb.ToString ();
		}

		static bool NeedsEscaping (uint c)
		{
			//TODO: there are other chars we should error on?
			if (c > char.MaxValue)
				return false;
			return char.IsControl ((char)c);
		}

		static bool TryParseHex (string str, int offset, int length, out uint val)
		{
			val = 0x0;

			for (int i = offset; i < offset + length; i++) {
				uint bits;
				switch (str[i]) {
					case '0': bits = 0; break;
					case '1': bits = 1; break;
					case '2': bits = 2; break;
					case '3': bits = 3; break;
					case '4': bits = 4; break;
					case '5': bits = 5; break;
					case '6': bits = 6; break;
					case '7': bits = 7; break;
					case '8': bits = 8; break;
					case '9': bits = 9; break;
					case 'A': case 'a': bits = 10; break;
					case 'B': case 'b': bits = 11; break;
					case 'C': case 'c': bits = 12; break;
					case 'D': case 'd': bits = 13; break;
					case 'E': case 'e': bits = 14; break;
					case 'F': case 'f': bits = 15; break;
					default: return false;
				}
				val = (val << 4) | bits;
			}
			return true;
		}
		
		public enum EscapeMode
		{
			None,
			CSharp,
			CSharpVerbatim,
			Xml
		}
	}
}
