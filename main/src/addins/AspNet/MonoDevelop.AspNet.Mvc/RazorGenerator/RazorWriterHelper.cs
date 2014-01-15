//
// RazorWriterHelper.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

using System.Text;
using System;
using System.Globalization;
using System.Web.Razor.Generator;
using System.Reflection;

namespace MonoDevelop.RazorGenerator
{
	static class RazorWriterHelper
	{
		// System.Web.Razor.Generator.CSharpCodeWriter.WriteCStyleStringLiteral
		// Copyright (c) Microsoft Open Technologies, Inc.
		// Licensed under the Apache License, Version 2.0
		public static void WriteCStyleStringLiteral (this StringBuilder sb, string literal)
		{
			// From CSharpCodeGenerator.QuoteSnippetStringCStyle in CodeDOM
			sb.Append ("\"");
			for (int i = 0; i < literal.Length; i++) {
				switch (literal [i]) {
				case '\r':
					sb.Append ("\\r");
					break;
				case '\t':
					sb.Append ("\\t");
					break;
				case '\"':
					sb.Append ("\\\"");
					break;
				case '\'':
					sb.Append ("\\\'");
					break;
				case '\\':
					sb.Append ("\\\\");
					break;
				case '\0':
					sb.Append ("\\\0");
					break;
				case '\n':
					sb.Append ("\\n");
					break;
				case '\u2028':
				case '\u2029':
				// Inlined CSharpCodeGenerator.AppendEscapedChar
					sb.Append ("\\u");
					sb.Append (((int)literal [i]).ToString ("X4", CultureInfo.InvariantCulture));
					break;
				default:
					sb.Append (literal [i]);
					break;
				}
				if (i > 0 && i % 80 == 0) {
					// If current character is a high surrogate and the following 
					// character is a low surrogate, don't break them. 
					// Otherwise when we write the string to a file, we might lose 
					// the characters.
					if (Char.IsHighSurrogate (literal [i])
					    && (i < literal.Length - 1)
					    && Char.IsLowSurrogate (literal [i + 1])) {
						sb.Append (literal [++i]);
					}

					sb.Append ("\" +");
					sb.Append (Environment.NewLine);
					sb.Append ('\"');
				}
			}
			sb.Append ("\"");
		}

		public static void SetExpressionRenderingMode ( this CodeGeneratorContext context, ExpressionRenderingMode mode)
		{
			context.GetType ().InvokeMember (
				"ExpressionRenderingMode",
				BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				context,
				new object[] { mode }
			);
		}

		public static ExpressionRenderingMode GetExpressionRenderingMode (this CodeGeneratorContext context)
		{
			return (ExpressionRenderingMode) context.GetType ().InvokeMember (
				"ExpressionRenderingMode",
				BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				context,
				null
			);
		}
	}
}