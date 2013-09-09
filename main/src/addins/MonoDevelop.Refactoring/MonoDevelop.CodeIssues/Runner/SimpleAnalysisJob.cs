//
// AbstractAnalysisJob.cs
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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;

namespace MonoDevelop.CodeIssues
{
	/// <summary>
	/// A simple analysis job.
	/// </summary>
	public class SimpleAnalysisJob : IAnalysisJob
	{
		readonly IList<ProjectFile> files;

		/// <summary>
		/// Initializes a new instance of the <see cref="MonoDevelop.CodeIssues.SimpleAnalysisJob"/> class.
		/// </summary>
		/// <param name="files">The files to analyze.</param>
		public SimpleAnalysisJob (IList<ProjectFile> files)
		{
			if (files == null)
				throw new ArgumentNullException ("files");
			this.files = files;
		}

		#region IAnalysisJob implementation

		public event EventHandler<CodeIssueEventArgs> CodeIssueAdded;

		protected virtual void OnCodeIssueAdded (CodeIssueEventArgs args)
		{
			var handler = CodeIssueAdded;
			if (handler != null)
				handler(this, args);
		}

		public IEnumerable<ProjectFile> GetFiles ()
		{
			return files;
		}

		public IEnumerable<BaseCodeIssueProvider> GetIssueProviders (ProjectFile file)
		{
			return RefactoringService.GetInspectors (DesktopService.GetMimeTypeForUri (file.Name));
		}

		public void AddResult (ProjectFile file, BaseCodeIssueProvider provider, IEnumerable<CodeIssue> issues)
		{
			OnCodeIssueAdded (new CodeIssueEventArgs(file, provider, issues));
		}

		public void NotifyCancelled ()
		{
			// empty for now
		}

		#endregion
	}
}

