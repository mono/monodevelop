//
// IGroupingProvider.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Simon Lindgren
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
using Mono.TextEditor;

namespace MonoDevelop.CodeIssues
{

	public class ActionSummary
	{
		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		/// <value>The title.</value>
		public string Title { get; set; }
		
		/// <summary>
		/// Gets or sets the sibling key.
		/// </summary>
		/// <value>The sibling key.</value>
		public object SiblingKey { get; set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="MonoDevelop.CodeIssues.ActionSummary"/> is batchable.
		/// </summary>
		/// <value><c>true</c> if batchable; otherwise, <c>false</c>.</value>
		public bool Batchable { get; set; }
		
		public DocumentRegion Region { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="IssueSummary"/> representing the source of this action.
		/// </summary>
		/// <value>The issue summary.</value>
		public IssueSummary IssueSummary { get; set; }
		
		public override string ToString ()
		{
			return string.Format ("[ActionSummary: Title={0}, Batchable={1}, Region={2}]", Title, Batchable, Region);
		}
	}

}

