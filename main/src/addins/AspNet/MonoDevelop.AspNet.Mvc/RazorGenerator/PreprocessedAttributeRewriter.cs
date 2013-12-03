//
// PreprocessedAttributeRewriter.cs
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

using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Generator;

namespace MonoDevelop.RazorGenerator
{
	class PreprocessedAttributeRewriter : MarkupRewriter
	{
		protected override bool CanRewrite (Span span)
		{
			return span.CodeGenerator is LiteralAttributeCodeGenerator;
		}

		protected override SyntaxTreeNode RewriteSpan (BlockBuilder parent, Span span)
		{
			var b = new SpanBuilder (span);
			var old = (LiteralAttributeCodeGenerator)span.CodeGenerator;
			b.CodeGenerator = old.ValueGenerator != null
				? new PreprocessedLiteralAttributeCodeGenerator (old.Prefix, old.ValueGenerator)
				: new PreprocessedLiteralAttributeCodeGenerator (old.Prefix, old.Value);
			return b.Build ();
		}

		protected override bool CanRewrite (Block block)
		{
			return block.CodeGenerator is AttributeBlockCodeGenerator
				|| block.CodeGenerator is DynamicAttributeBlockCodeGenerator;
		}

		protected override SyntaxTreeNode RewriteBlock (BlockBuilder parent, Block block)
		{
			var b = new BlockBuilder (block);
			var abGen = block.CodeGenerator as AttributeBlockCodeGenerator;
			if (abGen != null) {
				b.CodeGenerator = new PreprocessedAttributeBlockCodeGenerator (abGen);
			} else {
				b.CodeGenerator = new PreprocessedDynamicAttributeBlockCodeGenerator ((DynamicAttributeBlockCodeGenerator)b.CodeGenerator);
			}
			return b.Build ();
		}
	}
}