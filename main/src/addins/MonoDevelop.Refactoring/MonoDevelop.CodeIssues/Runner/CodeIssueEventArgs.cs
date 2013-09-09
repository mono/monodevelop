//
// CodeIssueEventArgs.cs
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
using System;
using MonoDevelop.CodeIssues;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.CodeIssues
{
	/// <summary>
	/// Code issue event arguments.
	/// </summary>
	public class CodeIssueEventArgs : EventArgs
	{

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeIssueEventArgs"/> class.
		/// </summary>
		/// <param name="codeIssues">The code issues.</param>
		public CodeIssueEventArgs (ProjectFile file, BaseCodeIssueProvider provider, IEnumerable<CodeIssue> codeIssues)
		{
			File = file;
			Provider = provider;
			CodeIssues = codeIssues as IList<CodeIssue> ?? codeIssues.ToList ();
		}

		/// <summary>
		/// Gets the analyzed file.
		/// </summary>
		/// <value>The analyzed file.</value>
		public ProjectFile File { get; private set; }

		/// <summary>
		/// Gets the provider.
		/// </summary>
		/// <value>The code issue provider that provided the issues.</value>
		public BaseCodeIssueProvider Provider { get; private set; }

		/// <summary>
		/// Gets the code issues.
		/// </summary>
		/// <value>The new code issues.</value>
		public IList<CodeIssue> CodeIssues { get; private set; }
	}
}

