//
// PreprocessedAttributeBlockCodeGenerator.cs
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

namespace MonoDevelop.RazorGenerator
{
	// based on base implementation
	// Copyright (c) Microsoft Open Technologies, Inc.
	// Licensed under the Apache License, Version 2.0
	class PreprocessedAttributeBlockCodeGenerator : AttributeBlockCodeGenerator
	{
		public PreprocessedAttributeBlockCodeGenerator (AttributeBlockCodeGenerator old)
				: base (old.Name, old.Prefix, old.Suffix)
		{
		}

		public override void GenerateStartBlockCode (Block target, CodeGeneratorContext context)
		{
			if (context.Host.DesignTimeMode)
				return;

			context.FlushBufferedStatement();

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
			sb.WriteCStyleStringLiteral (Name);
			sb.Append (", ");
			sb.WriteCStyleStringLiteral (Prefix);
			sb.Append (", ");
			sb.WriteCStyleStringLiteral (Suffix);

			context.AddStatement (sb.ToString ());
		}

		public override void GenerateEndBlockCode (Block target, CodeGeneratorContext context)
		{
			if (context.Host.DesignTimeMode)
				return;

			context.FlushBufferedStatement ();
			context.AddStatement (");");
		}
	}
}