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
using System.Collections.Generic;

namespace MonoDevelop.Core.Execution
{
	/// <summary>
	/// Builds a process argument string.
	/// </summary>
	public class ProcessArgumentBuilder
	{
		StringBuilder sb = new StringBuilder ();
		
		public string ProcessPath {
			get; private set;
		}
		
		// .NET doesn't allow escaping chars other than " and \ inside " quotes
		const string escapeDoubleQuoteCharsStr = "\\\"";
		
		public ProcessArgumentBuilder ()
		{
			
		}
		
		public ProcessArgumentBuilder (string processPath)
		{
			ProcessPath = processPath;
		}
		
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
		
		/// <summary>Adds an argument, quoting and escaping as necessary.</summary>
		/// <remarks>The .NET process class does not support escaped 
		/// arguments, only quoted arguments with escaped quotes.</remarks>
		public void AddQuoted (string argument)
		{
			if (argument == null)
				return;
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
		
		/// <summary>Quotes a string, escaping if necessary.</summary>
		/// <remarks>The .NET process class does not support escaped 
		/// arguments, only quoted arguments with escaped quotes.</remarks>
		public static string Quote (string s)
		{
			var sb = StringBuilderCache.Allocate ();
			sb.Append ('"');
			AppendEscaped (sb, escapeDoubleQuoteCharsStr, s);
			sb.Append ('"');
			return StringBuilderCache.ReturnAndFree (sb);
		}
		
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
		
		static string GetArgument (StringBuilder builder, string buf, int startIndex, out int endIndex, out Exception ex)
		{
			bool escaped = false;
			char qchar, c = '\0';
			int i = startIndex;
			
			builder.Clear ();
			switch (buf[startIndex]) {
			case '\'': qchar = '\''; i++; break;
			case '"': qchar = '"'; i++; break;
			default: qchar = '\0'; break;
			}
			
			while (i < buf.Length) {
				c = buf[i];
				
				if (c == qchar && !escaped) {
					// unescaped qchar means we've reached the end of the argument
					i++;
					break;
				}
				
				if (c == '\\') {
					escaped = true;
				} else if (escaped) {
					builder.Append (c);
					escaped = false;
				} else if (qchar == '\0' && (c == ' ' || c == '\t')) {
					break;
				} else if (qchar == '\0' && (c == '\'' || c == '"')) {
					string sofar = builder.ToString ();
					string embedded;
					
					if ((embedded = GetArgument (builder, buf, i, out endIndex, out ex)) == null)
						return null;
					
					i = endIndex;
					builder.Clear ();
					builder.Append (sofar);
					builder.Append (embedded);
					continue;
				} else {
					builder.Append (c);
				}
				
				i++;
			}
			
			if (escaped || (qchar != '\0' && c != qchar)) {
				ex = new FormatException (escaped ? "Incomplete escape sequence." : "No matching quote found.");
				endIndex = -1;
				return null;
			}
			
			endIndex = i;
			ex = null;
			
			return builder.ToString ();
		}
		
		static bool TryParse (string commandline, out string[] argv, out Exception ex)
		{
			StringBuilder builder = new StringBuilder ();
			List<string> args = new List<string> ();
			string argument;
			int i = 0, j;
			char c;
			
			while (i < commandline.Length) {
				c = commandline[i];
				if (c != ' ' && c != '\t') {
					if ((argument = GetArgument (builder, commandline, i, out j, out ex)) == null) {
						argv =  null;
						return false;
					}
					
					args.Add (argument);
					i = j;
				}
				
				i++;
			}
			
			argv = args.ToArray ();
			ex = null;
			
			return true;
		}
		
		public static bool TryParse (string commandline, out string[] argv)
		{
			Exception ex;
			
			return TryParse (commandline, out argv, out ex);
		}
		
		public static string[] Parse (string commandline)
		{
			string[] argv;
			Exception ex;
			
			if (!TryParse (commandline, out argv, out ex))
				throw ex;
			
			return argv;
		}
	}
}

