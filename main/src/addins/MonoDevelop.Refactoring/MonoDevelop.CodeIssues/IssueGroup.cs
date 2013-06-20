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
		IList<IssueSummary> leaves = new List<IssueSummary>();
		ISet<IssueSummary> allIssues = new HashSet<IssueSummary>();

		/// <summary>
		/// Initializes a new instance of the <see cref="MonoDevelop.CodeIssues.IssueGroup"/> class.
		/// </summary>
		/// <param name="sourceProvider">The <see cref="IGroupingProvider"/> that created this group.</param>
		/// <param name="nextProvider">
		/// The <see cref="IGroupingProvider"/> to use when grouping <see cref="IssueSummary"/> instances.
		/// </param>
		/// <param name="description">A string describing the contents of this group.</param>
		public IssueGroup (IGroupingProvider sourceProvider, IGroupingProvider nextProvider, string description)
		{
			groupingProvider = nextProvider;
			Description = description;
			ProcessingEnabled = false;
			if (sourceProvider != null) {
				sourceProvider.NextChanged += HandleNextChanged;
			}
		}

		void HandleNextChanged (IGroupingProvider obj)
		{
			lock(_lock) {
				ProcessingEnabled = false;
				groupingProvider = obj.Next;
				groups.Clear ();
				leaves.Clear ();
			}
			// By disabling processing, no events will be raised until EnableGrouping() has been
			// called. There is a slight possibility of a race between two different grouping provider
			// changes but all such changes should originate in the ui and thus it should not happen
			// TODO: Fix the race described above.
			OnChildrenInvalidated ();
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

		event Action<IssueGroup> childrenInvalidated;
		
		public event Action<IssueGroup> ChildrenInvalidated {
			add {
				childrenInvalidated += value;
			}
			remove {
				childrenInvalidated -= value;
			}
		}

		protected virtual void OnChildrenInvalidated ()
		{
			var handler = childrenInvalidated;
			if (handler != null) {
				handler (this);
			}
		}

		event Action<IssueGroup> childGroupAdded;
		/// <summary>
		/// Called when a child is added to this IssueGrouping.
		/// </summary>
		public event Action<IssueGroup> ChildGroupAdded
		{
			add {
				childGroupAdded += value;
			}
			remove {
				childGroupAdded -= value;
			}
		}

		void DoChildGroupAdded (IssueGroup group)
		{
			var handler = childGroupAdded;
			if (handler != null) {
				handler (group);
			}
		}

		event Action<IssueSummary> issueSummaryAdded;
		/// <summary>
		/// Called when an <see cref="IssueSummary"/> is added to this IssueGrouping.
		/// </summary>
		public event Action<IssueSummary> IssueSummaryAdded
		{
			add {
				issueSummaryAdded += value;
			}
			remove {
				issueSummaryAdded -= value;
			}
		}

		void DoIssueAdded (IssueSummary summary)
		{
			var handler = issueSummaryAdded;
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
		
		public bool ProcessingEnabled {
			get;
			private set;
		}
		
		/// <summary>
		/// Makes this instance start processing issues.
		/// </summary>
		public void EnableProcessing ()
		{
			lock (_lock) {
				ProcessingEnabled = true;
			}
			foreach (var issue in allIssues) {
				Push (issue);
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
				if (!allIssues.Contains (issue)) {
					IssueCount++;
					allIssues.Add (issue);
				}
				if (!ProcessingEnabled) {
					return;
				}
				if (groupingProvider != null) {
					group = groupingProvider.GetIssueGroup (issue);
				}
				if (group == null) {
					leaves.Add (issue);
				} else if (!groups.Contains (group)) {
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

