//
// PreprocessedCSharpRazorCodeGenerator.cs
//
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
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

using System.Web.Razor.Generator;
using System.Web.Razor.Parser.SyntaxTree;
using System.Text;
using System.Globalization;
using System;
using System.Linq;
using System.Reflection;

namespace MonoDevelop.RazorGenerator
{
	class PreprocessedCSharpRazorCodeGenerator : CSharpRazorCodeGenerator
	{
		public PreprocessedCSharpRazorCodeGenerator (RazorCodeGenerator old)
			: base (old.ClassName, old.RootNamespaceName, old.SourceFileName, old.Host)
		{
		}

		public override void VisitSpan (Span span)
		{
			var attGen = span.CodeGenerator as LiteralAttributeCodeGenerator;
			if (attGen != null) {
				GenerateLiteralAttributeCode (attGen, span, Context);
				return;
			}
			base.VisitSpan (span);
		}

		//from aspnetwebstack/master/src/System.Web.Razor/Generator/CSharpCodeWriter.cs
		static void WriteCStyleStringLiteral (StringBuilder sb, string literal)
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

		//based on LiteralAttributeCodeGenerator.GenerateCode
		static void GenerateLiteralAttributeCode (LiteralAttributeCodeGenerator gen, Span target, CodeGeneratorContext context)
		{
			if (context.Host.DesignTimeMode)
				return;

			var sb = new StringBuilder ();
			sb.Append (", Tuple.Create<object,bool> (");
			if (gen.ValueGenerator != null) {
				context.FlushBufferedStatement ();
				context.AddStatement (sb.ToString ());
				sb.Length = 0;
				gen.ValueGenerator.Value.GenerateCode (target, context);
				sb.Append (", false)");
			} else {
				WriteCStyleStringLiteral (sb, gen.Value);
				sb.Append (", true)");
			}

			context.FlushBufferedStatement ();
			context.AddStatement (sb.ToString ());
		}

		public override void VisitStartBlock (Block block)
		{
			var attGen = block.CodeGenerator as AttributeBlockCodeGenerator;
			if (attGen != null) {
				GenerateAttributeStartBlockCode (attGen, block, Context);
				return;
			}
			var dynAttGen = block.CodeGenerator as DynamicAttributeBlockCodeGenerator;
			if (dynAttGen != null) {
				GenerateDynamicAttributeStartBlockCode (dynAttGen, block, Context);
				return;
			}
			base.VisitStartBlock (block);
		}

		//based on AttributeBlockCodeGenerator.GenerateStartBlockCode
		static void GenerateAttributeStartBlockCode (AttributeBlockCodeGenerator gen, Block target, CodeGeneratorContext context)
		{
			if (context.Host.DesignTimeMode)
				return;

			var sb = new StringBuilder ();
			if (!string.IsNullOrEmpty (context.TargetWriterName)) {
				sb.AppendFormat (
					"{0} ({1}, ",
					context.Host.GeneratedClassContext.WriteAttributeToMethodName,
					context.TargetWriterName
				);
			} else {
				sb.AppendFormat (
					"{0} (",
					context.Host.GeneratedClassContext.WriteAttributeMethodName
				);
			}
			WriteCStyleStringLiteral (sb, gen.Name);
			sb.Append (", ");
			WriteCStyleStringLiteral (sb, gen.Prefix);
			sb.Append (", ");
			WriteCStyleStringLiteral (sb, gen.Suffix);

			context.FlushBufferedStatement ();
			context.AddStatement (sb.ToString ());
		}

		const string ValueWriterName = "__razor_attribute_value_writer";

		//based on DynamicAttributeBlockCodeGenerator.GenerateStartBlockCode
		static void GenerateDynamicAttributeStartBlockCode (DynamicAttributeBlockCodeGenerator gen, Block target, CodeGeneratorContext context)
		{
			if (context.Host.DesignTimeMode)
				return;

			var sb = new StringBuilder ();
			Block child = target.Children.Where (n => n.IsBlock).Cast<Block> ().FirstOrDefault ();
			bool isExpresssion = child != null && child.Type == BlockType.Expression;
			if (isExpresssion) {
				sb.Append (", Tuple.Create<object,bool> (");
				//TODO: switcharoo rendering mode
				//_oldRenderingMode = context.ExpressionRenderingMode;
				//context.ExpressionRenderingMode = ExpressionRenderingMode.InjectCode
				context.GetType ().InvokeMember ("ExpressionRenderingMode", BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance, null, context, new object[] { ExpressionRenderingMode.InjectCode });
			} else {
				sb.AppendFormat (
					", Tuple.Create<object,bool> (new {0} ({1} => {{",
					context.Host.GeneratedClassContext.TemplateTypeName,
					ValueWriterName);
			}

			context.MarkEndOfGeneratedCode ();
			context.BufferStatementFragment (sb.ToString ());

			//TODO: switcharoo writer
			//oldTargetWriter = context.TargetWriterName;
			//context.TargetWriterName = ValueWriterName;
		}

		public override void VisitEndBlock (Block block)
		{
			var attGen = block.CodeGenerator as AttributeBlockCodeGenerator;
			if (attGen != null) {
				GenerateAttributeEndBlockCode (attGen, block, Context);
				return;
			}
			var dynAttGen = block.CodeGenerator as DynamicAttributeBlockCodeGenerator;
			if (dynAttGen != null) {
				GenerateDynamicAttributeEndBlockCode (dynAttGen, block, Context);
				return;
			}
			base.VisitEndBlock (block);
		}

		//based on AttributeBlockCodeGenerator.GenerateEndBlockCode
		static void GenerateAttributeEndBlockCode (AttributeBlockCodeGenerator gen, Block target, CodeGeneratorContext context)
		{
			if (context.Host.DesignTimeMode)
				return;

			context.FlushBufferedStatement();
			context.AddStatement (");");
		}

		//based on DynamicAttributeBlockCodeGenerator.GenerateEndBlockCode
		static void GenerateDynamicAttributeEndBlockCode (DynamicAttributeBlockCodeGenerator gen, Block target, CodeGeneratorContext context)
		{
			if (context.Host.DesignTimeMode)
				return;

			var sb = new StringBuilder ();
			Block child = target.Children.Where (n => n.IsBlock).Cast<Block> ().FirstOrDefault ();
			bool isExpression = child != null && child.Type == BlockType.Expression;
			if (isExpression) {
				sb.Append (", false)");
				//TODO: switcharoo rendering mode
				//context.ExpressionRenderingMode = _oldRenderingMode;
				context.GetType ().InvokeMember ("ExpressionRenderingMode", BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance, null, context, new object[] { ExpressionRenderingMode.WriteToOutput });
			} else {
				sb.Append ("}), false)");
			}

			context.AddStatement (sb.ToString ());

			//TODO: switcharoo writer
			//context.TargetWriterName = _oldTargetWriter;
		}
	}
}