//
// PreprocessedDynamicAttributeBlockCodeGenerator.cs
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
using System.Web.Razor.Generator;
using System.Text;
using System.Linq;

namespace MonoDevelop.RazorGenerator
{
	// based on base implementation
	// Copyright (c) Microsoft Open Technologies, Inc.
	// Licensed under the Apache License, Version 2.0
	class PreprocessedDynamicAttributeBlockCodeGenerator : DynamicAttributeBlockCodeGenerator
	{
		const string ValueWriterName = "__razor_attribute_value_writer";

		ExpressionRenderingMode oldRenderingMode;
		string oldTargetWriter;
		bool isExpression;

		public PreprocessedDynamicAttributeBlockCodeGenerator (DynamicAttributeBlockCodeGenerator old)
				: base (old.Prefix, old.ValueStart)
		{
		}

		public override void GenerateStartBlockCode (Block target, CodeGeneratorContext context)
		{
			if (context.Host.DesignTimeMode)
				return;

			Block child = target.Children.Where (n => n.IsBlock).Cast<Block> ().FirstOrDefault ();
			isExpression = child != null && child.Type == BlockType.Expression;

			var sb = new StringBuilder ();
			sb.Append (", Tuple.Create<string,object,bool> (");
			sb.WriteCStyleStringLiteral (Prefix.Value);
			sb.Append (", ");

			if (isExpression) {
				oldRenderingMode = context.GetExpressionRenderingMode ();
				context.SetExpressionRenderingMode (ExpressionRenderingMode.InjectCode);
			} else {
				sb.AppendFormat (
					"new {0} ({1} => {{",
					context.Host.GeneratedClassContext.TemplateTypeName,
					ValueWriterName);
			}

			context.MarkEndOfGeneratedCode ();
			context.BufferStatementFragment (sb.ToString ());

			oldTargetWriter = context.TargetWriterName;
			context.TargetWriterName = ValueWriterName;
		}

		public override void GenerateEndBlockCode (Block target, CodeGeneratorContext context)
		{
			if (context.Host.DesignTimeMode)
				return;

			var sb = new StringBuilder ();
			if (isExpression) {
				sb.Append (", false)");
				context.SetExpressionRenderingMode (oldRenderingMode);
			} else {
				sb.Append ("}), false)");
			}

			context.AddStatement (sb.ToString ());
			context.TargetWriterName = oldTargetWriter;
		}
	}
}