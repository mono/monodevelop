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
using System.Collections.Generic;
using Xwt;

namespace MonoDevelop.CodeIssues
{
	/// <summary>
	/// Represents the cached information saved for each issue detected in a source file.
	/// </summary>
	/// <remarks>
	/// This class is thread safe.
	/// </remarks>
	public class IssueGroup
	{
		object _lock = new object ();
		IGroupingProvider groupingProvider;
		/// <summary>
		/// A list of groups produced by the <see cref="groupingProvider"/>.
		/// </summary>
		ISet<IssueGroup> groups = new HashSet<IssueGroup>();

		/// <summary>
		/// Initializes a new instance of the <see cref="MonoDevelop.CodeIssues.IssueGroup"/> class.
		/// </summary>
		/// <param name="provider">
		/// The <see cref="IGroupingProvider"/> to use when grouping <see cref="IssueSummary"/> instances.
		/// </param>
		public IssueGroup (IGroupingProvider provider, string description)
		{
			groupingProvider = provider;
			Description = description;
		}

		/// <summary>
		/// Gets or sets the position of this node inside an <see cref="TreeStore"/>.
		/// </summary>
		/// <value>The position.</value>
		public TreePosition Position {
			get;
			set;
		}

		public string Description {
			get;
			private set;
		}

		public int IssueCount {
			get;
			private set;
		}

		/// <summary>
		/// Called when a child is added to this IssueGrouping.
		/// </summary>
		public event Action<IssueGroup> ChildGroupAdded;

		void DoChildGroupAdded (IssueGroup group)
		{
			var handler = ChildGroupAdded;
			if (handler != null) {
				handler (group);
			}
		}

		/// <summary>
		/// Called when an <see cref="IssueSummary"/> is added to this IssueGrouping.
		/// </summary>
		public event Action<IssueSummary> IssueSummaryAdded;

		void DoIssueAdded (IssueSummary summary)
		{
			var handler = IssueSummaryAdded;
			if (handler != null) {
				handler (summary);
			}
		}

		public void ClearStatistics ()
		{
			lock (_lock) {
				groups.Clear ();
				groupingProvider.Reset ();
				IssueCount = 0;
			}
		}

		/// <summary>
		/// Push the specified issue through the grouped tree.
		/// </summary>
		/// <param name="issue">The <see cref="IssueSummary"/> to push through the tree.</param>
		public void Push (IssueSummary issue)
		{
			IssueGroup group = null;
			bool groupAdded = false;
			lock (_lock) {
				IssueCount++;
				if (groupingProvider != null) {
					group = groupingProvider.GetIssueGroup (issue);
				}
				if (group != null && !groups.Contains (group)) {
					groupAdded = true;
					groups.Add (group);
				}
			}
			// We don't need to hold the lock while calling the event delegates
			// It is possible that the IssueCount is incorrect, because it can
			// be updated by another call to Push before the value has been used by
			// the event handlers. Currently, this is not a problem, because the value
			// will be updated again when the second caller reaches the event calls.
			
			if (groupAdded) {
				DoChildGroupAdded (group);
				group.Push (issue);	
			} else if (group == null) {
				DoIssueAdded (issue);
			} else {
				group.Push (issue);	
			}
		}
	}
}

