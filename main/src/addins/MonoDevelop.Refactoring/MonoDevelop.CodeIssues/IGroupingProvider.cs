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
using System;

namespace MonoDevelop.CodeIssues
{
	public interface IGroupingProvider
	{
		/// <summary>
		/// Gets the issue group for the <see cref="IssueGroup"/> specified in <paramref name="issue"/>.
		/// </summary>
		/// <returns>The issue group.</returns>
		/// <param name="issue">The <see cref="IssueSummary"/> to return a group for.</param>
		IssueGroup GetIssueGroup(IssueSummary issue);

		/// <summary>
		/// Removes the set of cached groups.
		/// </summary>
		void Reset ();
		
		/// <summary>
		/// The <see cref="IGroupingProvider"/> to be applied after the current instance. Never returns null.
		/// </summary>
		/// <value>The next.</value>
		/// <exception cref="InvalidOperationException">
		/// Thrown by both accessors if <see cref="SupportsNext"/> is false.
		/// </exception>
		IGroupingProvider Next { get; set; }
		
		/// <summary>
		/// Occurs when <see cref="Next"/> changes.
		/// </summary>
		event EventHandler<GroupingProviderEventArgs> NextChanged;
		
		/// <summary>
		/// Gets a value indicating whether this <see cref="MonoDevelop.CodeIssues.IGroupingProvider"/>
		/// supports the usage of <see cref="Next"/> .
		/// </summary>
		/// <value>True if <see cref="Next"/> can be used.</value>
		bool SupportsNext { get; }
	}
}

