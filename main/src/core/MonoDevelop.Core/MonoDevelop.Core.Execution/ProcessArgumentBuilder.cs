// 
// ProcessArgumentBuilder.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Text;

namespace MonoDevelop.Core.Execution
{
	/// <summary>
	/// Builds a process argument string.
	/// </summary>
	public class ProcessArgumentBuilder
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder ();
		
		// Mono 2.8.x doesn't like it if we escape both  " and ' within ""
		static string escapeDoubleQuoteCharsStr = "\\\"";
		
		/// <summary>
		/// Adds an argument without escaping or quoting.
		/// </summary>
		public void Add (string argument)
		{
			if (sb.Length > 0)
				sb.Append (' ');
			sb.Append (argument);
		}
		
		/// <summary>
		/// Adds multiple arguments without escaping or quoting.
		/// </summary>
		public void Add (params string[] args)
		{
			foreach (var a in args)
				Add (a);
		}
		
		/// <summary>
		/// Adds a formatted argument, quoting and escaping as necessary.
		/// </summary>
		public void AddQuotedFormat (string argumentFormat, params object[] values)
		{
			AddQuoted (string.Format (argumentFormat, values));
		}
		
		public void AddQuotedFormat (string argumentFormat, object val0)
		{
			AddQuoted (string.Format (argumentFormat, val0));
		}
		
		/// <summary>
		/// Adds an argument, quoting and escaping as necessary.
		/// </summary>
		public void AddQuoted (string argument)
		{
			if (sb.Length > 0)
				sb.Append (' ');
			
			sb.Append ('"');
			AppendEscaped (sb, escapeDoubleQuoteCharsStr, argument);
			sb.Append ('"');
		}
		
		/// <summary>
		/// Adds multiple arguments, quoting and escaping each as necessary.
		/// </summary>
		public void AddQuoted (params string[] args)
		{
			foreach (var a in args)
				AddQuoted (a);
		}
		
		/// <summary>
		/// Quotes a string, escaping if necessary.
		/// </summary>
		public static string Quote (string s)
		{
			var sb = new StringBuilder ();
			sb.Append ('"');
			AppendEscaped (sb, escapeDoubleQuoteCharsStr, s);
			sb.Append ('"');
			return sb.ToString ();
		}
		
		//FIXME: this doesn't work in Mono 2.8.1 (and 2.8?); escaped spaces get discarded
		/*
		
		static string escapeAllChars = "\\\"' ()";
		
		/// <summary>
		/// Escapes all special characters.
		/// </summary>
		public static string Escape (string s)
		{
			var sb = new StringBuilder ();
			AppendEscaped (sb, escapeAllChars, s);
			return sb.ToString ();
		}
		*/
		
		public override string ToString ()
		{
			return sb.ToString ();
		}
		
		static void AppendEscaped (StringBuilder sb, string escapeChars, string s)
		{
			for (int i = 0; i < s.Length; i++) {
				char c = s[i];
				if (escapeChars.IndexOf (c) > -1)
					sb.Append ('\\');
				sb.Append (c);
			}
		}
	}
}

