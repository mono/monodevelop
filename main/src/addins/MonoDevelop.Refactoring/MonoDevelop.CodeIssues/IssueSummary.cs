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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Refactoring;

namespace MonoDevelop.CodeIssues
{

	public class IssueSummary
	{
		/// <summary>
		/// The description of the issue.
		/// </summary>
		public string IssueDescription { get; set; }
		
		/// <summary>
		/// The region.
		/// </summary>
		public DomRegion Region { get; set;	}
		
		/// <summary>
		/// Gets or sets the category of the issue provider.
		/// </summary>
		public string ProviderCategory { get; set; }

		/// <summary>
		/// Gets or sets the title of the issue provider.
		/// </summary>
		public string ProviderTitle { get; set; }

		/// <summary>
		/// Gets or sets the description of the issue provider.
		/// </summary>
		public string ProviderDescription { get; set; }

		/// <summary>
		/// Gets or sets the severity.
		/// </summary>
		public Severity Severity { get; set; }

		/// <summary>
		/// Gets or sets a value indicating how this issue should be marked inside the text editor.
		/// Note: There is only one code issue provider generated therfore providers need to be state less.
		/// </summary>
		public IssueMarker IssueMarker { get; set; }

	}
}

