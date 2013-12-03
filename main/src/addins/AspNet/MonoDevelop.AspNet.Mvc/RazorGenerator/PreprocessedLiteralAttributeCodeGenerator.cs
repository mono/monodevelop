//
// PreprocessedLiteralAttributeCodeGenerator.cs
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

using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Text;
using System.Web.Razor.Generator;
using System.Text;

namespace MonoDevelop.RazorGenerator
{
	// based on base implementation
	// Copyright (c) Microsoft Open Technologies, Inc.
	// Licensed under the Apache License, Version 2.0
	class PreprocessedLiteralAttributeCodeGenerator : LiteralAttributeCodeGenerator
	{
		public PreprocessedLiteralAttributeCodeGenerator (LocationTagged<string> prefix, LocationTagged<string> value)
				: base (prefix, value)
		{
		}

		public PreprocessedLiteralAttributeCodeGenerator (LocationTagged<string> prefix, LocationTagged<SpanCodeGenerator> valueGenerator)
				: base (prefix, valueGenerator)
		{
		}

		public override void GenerateCode (Span target, CodeGeneratorContext context)
		{
			if (context.Host.DesignTimeMode)
				return;

			ExpressionRenderingMode oldMode = context.GetExpressionRenderingMode ();

			var sb = new StringBuilder ();
			sb.Append (", Tuple.Create<string,object,bool> (");
			sb.WriteCStyleStringLiteral (Prefix.Value);
			sb.Append (", ");

			if (ValueGenerator != null) {
				context.SetExpressionRenderingMode (ExpressionRenderingMode.InjectCode);
			} else {
				sb.WriteCStyleStringLiteral (Value);
				sb.Append (", true)");
			}
			context.BufferStatementFragment (sb.ToString ());

			if (ValueGenerator != null) {
				ValueGenerator.Value.GenerateCode (target, context);
				context.FlushBufferedStatement ();
				context.SetExpressionRenderingMode (oldMode);
				context.AddStatement (", false)");
			} else {
				context.FlushBufferedStatement ();
			}
		}
	}
}