//
// IAnalysisJob.cs
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
using System.Collections.Generic;
using MonoDevelop.Projects;
using System;

namespace MonoDevelop.CodeIssues
{
	/// <summary>
	/// Represents an analysis job. Implementations must be thread safe.
	/// </summary>
	public interface IAnalysisJob
	{
		/// <summary>
		/// Gets the file names affected by this job.
		/// </summary>
		/// <remarks>
		/// The return value of this method may not change after the initial invocation.
		/// </remarks>
		/// <returns>The file names.</returns>
		IEnumerable<ProjectFile> GetFiles ();

		/// <summary>
		/// Gets the issue providers for the file specified in <paramref name="file"/>.
		/// </summary>
		/// <returns>The issue providers to run on the specified file.</returns>
		/// <param name="file">The file.</param>
		IEnumerable<BaseCodeIssueProvider> GetIssueProviders (ProjectFile file);

		/// <summary>
		/// Adds the results to this job.
		/// </summary>
		/// <param name="file">The file that the results apply to.</param>
		/// <param name="provider">The provider that provided the issues.</param>
		/// <param name="issues">The issues detected in the specified file.</param>
		void AddResult (ProjectFile file, BaseCodeIssueProvider provider, IEnumerable<CodeIssue> issues);

		/// <summary>
		/// Notifies the job that there was an error running the specified provider on the specified file.
		/// </summary>
		/// <param name="file">The file.</param>
		/// <param name="provider">The provider.</param>
		void AddError (ProjectFile file, BaseCodeIssueProvider provider);

		/// <summary>
		/// Occurs when new code issues are added.
		/// </summary>
		event EventHandler<CodeIssueEventArgs> CodeIssueAdded;

		/// <summary>
		/// Called when this job is cancelled to notify the instance about that change.
		/// </summary>
		/// <remarks>
		/// The caller does NOT have to ensure that this is the last method called.
		/// Specifically, <see cref="AddResult"/> can be called after this method has been invoked.
		/// </remarks>
		void NotifyCancelled ();

		/// <summary>
		/// Notifies the job that all files have been processed.
		/// </summary>
		void SetCompleted ();

		/// <summary>
		/// Occurs when the job is completed.
		/// </summary>
		event EventHandler<EventArgs> Completed;
	}
}

