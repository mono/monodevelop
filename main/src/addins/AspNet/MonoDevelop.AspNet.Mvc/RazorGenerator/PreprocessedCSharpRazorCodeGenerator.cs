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
				// TODO: do stuff with attGen.Prefix, attGen.ValueGenerator, attGen.Value
			} else {
				base.VisitSpan (span);
			}
		}

		public override void VisitBlock (Block block)
		{
			var attGen = block.CodeGenerator as AttributeBlockCodeGenerator;
			if (attGen != null) {
				// TODO: do stuff with attGen.Name, attGen.Prefix, attGen.Suffix
				return;
			}
			var dynAttGen = block.CodeGenerator as DynamicAttributeBlockCodeGenerator;
			if (dynAttGen != null) {
				// TODO: do stuff with attGen.Prefix, attGen.ValueStart
				return;
			}
			base.VisitBlock (block);
		}

		public override void VisitStartBlock (Block block)
		{
			var attGen = block.CodeGenerator as AttributeBlockCodeGenerator;
			if (attGen != null) {
				// TODO: do stuff with attGen.Name, attGen.Prefix, attGen.Suffix
				return;
			}
			var dynAttGen = block.CodeGenerator as DynamicAttributeBlockCodeGenerator;
			if (dynAttGen != null) {
				// TODO: do stuff with attGen.Prefix, attGen.ValueStart
				return;
			}
			base.VisitStartBlock (block);
		}

		public override void VisitEndBlock (Block block)
		{
			var attGen = block.CodeGenerator as AttributeBlockCodeGenerator;
			if (attGen != null) {
				// TODO: do stuff with attGen.Name, attGen.Prefix, attGen.Suffix
				return;
			}
			var dynAttGen = block.CodeGenerator as DynamicAttributeBlockCodeGenerator;
			if (dynAttGen != null) {
				// TODO: do stuff with attGen.Prefix, attGen.ValueStart
				return;
			}
			base.VisitEndBlock (block);
		}
	}
}