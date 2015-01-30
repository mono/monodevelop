//
// CSharpProjector.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Editor;
using MonoDevelop.AspNet.WebForms.Dom;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor.Projection;
using MonoDevelop.Core.Text;

namespace MonoDevelop.AspNet.WebForms.CSharp
{
	public class CSharpProjector
	{
		public Task<Projection> CreateProjection (DocumentInfo info, IReadonlyTextDocument data, bool buildExpressions)
		{
			if (info == null)
				throw new ArgumentNullException ("info");
			if (data == null)
				throw new ArgumentNullException ("data");
			var document = new StringBuilder ();

			WriteUsings (info.Imports, document);
			var segBuilder = System.Collections.Immutable.ImmutableList<ProjectedSegment>.Empty.ToBuilder ();

			foreach (var node in info.XScriptBlocks) {
				var start = data.LocationToOffset (node.Region.Begin.Line, node.Region.Begin.Column) + 2;
				var end = data.LocationToOffset (node.Region.End.Line, node.Region.End.Column) - 2;

				segBuilder.Add (new ProjectedSegment (start, document.Length, end - start));

				document.AppendLine (data.GetTextBetween (start, end));
			}
			if (buildExpressions) {
				WriteClassDeclaration (info, document);
				document.AppendLine ("{");
				document.AppendLine ("void Generated ()");
				document.AppendLine ("{");
				//Console.WriteLine ("start:" + location.BeginLine  +"/" +location.BeginColumn);

				foreach (var node in info.XExpressions) {
					bool isBlock = node is WebFormsRenderBlock;

					var start = data.LocationToOffset (node.Region.Begin.Line, node.Region.Begin.Column) + 2;
					var end = data.LocationToOffset (node.Region.End.Line, node.Region.End.Column) - 2;

					if (!isBlock) {
						document.Append ("WriteLine (");
						start += 1;
					}

					string expr = data.GetTextBetween (start, end);
					segBuilder.Add (new ProjectedSegment (start, document.Length, expr.Length));
					document.Append (expr);
					if (!isBlock)
						document.Append (");");
				}
				document.AppendLine ("}");
				document.AppendLine ("}");
			}
			return Task.FromResult(new Projection (
				TextEditorFactory.CreateNewDocument (new StringTextSource (document.ToString ()), info.AspNetDocument.FileName + ".g.cs", "text/x-csharp"),
				segBuilder.ToImmutable ()
			));
		}

		static void WriteUsings (IEnumerable<string> usings, StringBuilder builder)
		{
			foreach (var u in usings) {
				builder.Append ("using ");
				builder.Append (u);
				builder.AppendLine (";");
			}
		}

		static void WriteClassDeclaration (DocumentInfo info, StringBuilder builder)
		{
			builder.Append ("partial class ");
			builder.Append (info.ClassName);
			builder.Append (" : ");
			builder.AppendLine (info.BaseType);
		}
	}
}

