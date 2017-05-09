//
// RazorCSharpParsedDocument.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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

using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;

namespace MonoDevelop.AspNet.Razor
{
	public class RazorCSharpParsedDocument : DefaultParsedDocument
	{
		public RazorCSharpPageInfo PageInfo { get; set; }

		public RazorCSharpParsedDocument (string fileName, RazorCSharpPageInfo pageInfo) : base (fileName)
		{
			PageInfo = pageInfo;
			Flags |= ParsedDocumentFlags.NonSerializable | ParsedDocumentFlags.HasCustomCompletionExtension;
			if (PageInfo.Errors != null)
				AddRange (PageInfo.Errors);
		}

		public override System.Threading.Tasks.Task<IReadOnlyList<FoldingRegion>> GetFoldingsAsync (System.Threading.CancellationToken cancellationToken)
		{
			return System.Threading.Tasks.Task.FromResult((IReadOnlyList<FoldingRegion>)Foldings.ToList ());
		}

		public IEnumerable<FoldingRegion> Foldings	{
			get	{
				if (PageInfo.FoldingRegions != null) {
					foreach (var region in PageInfo.FoldingRegions) {
						yield return region;
					}
				}
				if (PageInfo.Comments != null) {
					foreach (var comment in PageInfo.Comments) {
						if (comment.Region.BeginLine != comment.Region.EndLine)
							yield return new FoldingRegion ("@* comment *@", comment.Region);
					}
				}
			}
		}

	}
}
