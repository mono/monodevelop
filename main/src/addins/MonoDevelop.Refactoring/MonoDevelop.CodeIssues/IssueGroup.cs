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
using System.Linq;
using System.Collections.Generic;
using Xwt;
using MonoDevelop.CodeIssues;

namespace MonoDevelop.CodeIssues
{
	/// <summary>
	/// Represents the cached information saved for each issue detected in a source file.
	/// </summary>
	/// <remarks>
	/// This class is thread safe.
	/// </remarks>
	public class IssueGroup : IIssueTreeNode, IIssueSummarySink
	{
		object _lock = new object ();
		bool processingEnabled;
		/// <summary>
		/// A list of groups produced by the <see cref="groupingProvider"/>.
		/// </summary>
		ISet<IssueGroup> groups = new HashSet<IssueGroup>();
		IList<IIssueTreeNode> children = new List<IIssueTreeNode>();
		ISet<IssueSummary> allIssues = new HashSet<IssueSummary>();

		/// <summary>
		/// Initializes a new instance of the <see cref="MonoDevelop.CodeIssues.IssueGroup"/> class.
		/// </summary>
		/// <param name="sourceProvider">The <see cref="IGroupingProvider"/> that created this group.</param>
		/// <param name="nextProvider">
		/// The <see cref="IGroupingProvider"/> to use when grouping <see cref="IssueSummary"/> instances.
		/// </param>
		/// <param name="description">A string describing the contents of this group.</param>
		public IssueGroup (IGroupingProvider nextProvider, string description)
		{
			groupingProvider = nextProvider;
			Description = description;
			processingEnabled = false;
		}

		IGroupingProvider groupingProvider;

		public IGroupingProvider GroupingProvider {
			get {
				return groupingProvider;
			}
			set {
				lock(_lock) {
					processingEnabled = false;
					groupingProvider = value;
					groups.Clear ();
					children.Clear ();
				}
				OnChildrenInvalidated (new IssueGroupEventArgs(this));
			}
		}

		/*void HandleNextChanged (object sender, GroupingProviderEventArgs eventArgs)
		{
			lock(_lock) {
				processingEnabled = false;
				groupingProvider = eventArgs.GroupingProvider.Next;
				groups.Clear ();
				children.Clear ();
			}
			// By disabling processing, no events will be raised until EnableGrouping() has been
			// called. There is a slight possibility of a race between two different grouping provider
			// changes but all such changes should originate in the ui and thus it should not happen
			// TODO: Fix the race described above.
			OnChildrenInvalidated (new IssueGroupEventArgs(this));
		}*/

		#region IIssueTreeNode implementation

		string IIssueTreeNode.Text {
			get {
				return string.Format ("{0} ({1})", Description, IssueCount);
			}
		}

		ICollection<IIssueTreeNode> IIssueTreeNode.Children {
			get {
				EnableProcessing ();
				lock (_lock) {
					return new List<IIssueTreeNode> (children);
				}
			}
		}
		
		bool IIssueTreeNode.HasChildren {
			get {
				return HasChildren;
			}
		}

		event EventHandler<IssueGroupEventArgs> childrenInvalidated;
		event EventHandler<IssueGroupEventArgs> IIssueTreeNode.ChildrenInvalidated {
			add {
				childrenInvalidated += value;
			}
			remove {
				childrenInvalidated -= value;
			}
		}

		protected virtual void OnChildrenInvalidated (IssueGroupEventArgs eventArgs)
		{
			var handler = childrenInvalidated;
			if (handler != null) {
				handler (this, eventArgs);
			}
		}

		event EventHandler<IssueTreeNodeEventArgs> childAdded;
		event EventHandler<IssueTreeNodeEventArgs> IIssueTreeNode.ChildAdded {
			add {
				childAdded += value;
			}
			remove {
				childAdded -= value;
			}
		}

		protected virtual void OnChildAdded (IssueTreeNodeEventArgs eventArgs)
		{
			var handler = childAdded;
			if (handler != null) {
				handler (this, eventArgs);
			}
		}

		event EventHandler<IssueGroupEventArgs> textChanged;
		event EventHandler<IssueGroupEventArgs> IIssueTreeNode.TextChanged {
			add {
				textChanged += value;
			}
			remove {
				textChanged -= value;
			}
		}

		protected virtual void OnTextChanged (IssueGroupEventArgs eventArgs)
		{
			var handler = textChanged;
			if (handler != null) {
				handler (this, eventArgs);
			}
		}

		#endregion

		/// <summary>
		/// Gets the description.
		/// </summary>
		/// <value>The description.</value>
		public string Description {
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether this instance issue count.
		/// </summary>
		/// <value><c>true</c> if this instance issue count; otherwise, <c>false</c>.</value>
		public int IssueCount {
			get;
			private set;
		}
		
		public bool HasChildren {
			get {
				lock (_lock) {
					return allIssues.Count > 0;
				}
			}
		}
		
		public void ClearStatistics ()
		{
			lock (_lock) {
				groups.Clear ();
				children.Clear ();
				allIssues.Clear ();
				groupingProvider.Reset ();
				IssueCount = 0;
				processingEnabled = false;
			}
		}
		
		/// <summary>
		/// Makes this instance start processing issues.
		/// </summary>
		public void EnableProcessing ()
		{
			lock (_lock) {
				if (!processingEnabled) {
					processingEnabled = true;
				
					// Now process the existing issues
					foreach (var issue in allIssues) {
						IssueGroup group;
						ProcessIssue (issue, out group);
						
						// TODO: Could this be run without holding the lock and is it worth it?
						if (group != null) {
							group.AddIssue (issue);	
						}
					}
				}
			}
		}

		#region IIssueSummarySink implementation
		
		public void AddIssue (IssueSummary issue)
		{
			IssueGroup group = null;
			bool groupAdded = false;
			bool issueAdded = false;
			lock (_lock) {
				if (!allIssues.Contains (issue)) {
					IssueCount++;
					allIssues.Add (issue);
					issueAdded = true;
				}
				if (processingEnabled) {
					groupAdded = ProcessIssue (issue, out group);
				}
			}
			if (issueAdded) {
				OnTextChanged (new IssueGroupEventArgs (this));
			}
			if (!processingEnabled)
				return;
			if (groupAdded) {
				OnChildAdded (new IssueTreeNodeEventArgs (this, group));
			} else if (group == null) {
				OnChildAdded (new IssueTreeNodeEventArgs (this, issue));
			}
			if (group != null) {
				group.AddIssue (issue);
			}
			
		}
		
		#endregion

		bool ProcessIssue (IssueSummary issue, out IssueGroup group)
		{
			bool groupAdded = false;
			group = null;
			if (groupingProvider != null) {
				group = groupingProvider.GetIssueGroup (issue);
			}
			if (group == null) {
				children.Add (issue);
			} else if (!groups.Contains (group)) {
				groupAdded = true;
				groups.Add (group);
				children.Add (group);
			}
			return groupAdded;
		}
	}
}

