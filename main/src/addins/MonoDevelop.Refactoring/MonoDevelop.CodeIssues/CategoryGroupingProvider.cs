//
// ProjectGroupingProvider.cs
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

namespace MonoDevelop.CodeIssues
{
	// TODO: should this be a threadsafe class?
	// Our current usage is fine, but could be nice anyway (and might avoid future bugs)
	[GroupingDescription("Category")]
	public class CategoryGroupingProvider: IGroupingProvider
	{
		Dictionary<string, IssueGroup> groups = new Dictionary<string, IssueGroup> ();
		
		public CategoryGroupingProvider()
		{
			Next = NullGroupingProvider.Instance;
		}
		
		#region IGroupingProvider implementation

		public IssueGroup GetIssueGroup (IssueSummary issue)
		{
			IssueGroup group;
			if (!groups.TryGetValue(issue.ProviderCategory, out group)) {
				group = new IssueGroup (null, null, issue.ProviderCategory);
				groups.Add (issue.ProviderCategory, group);
			}
			return group;
		}

		public void Reset ()
		{
			groups.Clear ();
		}

		IGroupingProvider next;
		public IGroupingProvider Next
		{
			get {
				return next;
			}
			set {
				next = value;
				OnNextChanged (this);
			}
		}

		protected virtual void OnNextChanged (CategoryGroupingProvider categoryGroupingProvider)
		{
			var handler = nextChanged;
			if (handler != null) {
				handler (categoryGroupingProvider);
			}
		}
		
		event Action<IGroupingProvider> nextChanged;
		
		event Action<IGroupingProvider> IGroupingProvider.NextChanged
		{
			add {
				nextChanged += value;
			}
			remove {
				nextChanged -= value;
			}
		}

		public bool SupportsNext
		{
			get {
				return true;
			}
		}

		#endregion
	}
}

