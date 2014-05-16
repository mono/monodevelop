//
// AbstractGroupingProvider.cs
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
	public abstract class AbstractGroupingProvider<T>: IGroupingProvider
	{
		/// <summary>
		/// Associates a parent group and grouping key to a child group.
		/// </summary>
		readonly Dictionary<Tuple<IssueGroup, T>, IssueGroup> groups = new Dictionary<Tuple<IssueGroup, T>, IssueGroup> ();
		
		protected AbstractGroupingProvider()
		{
			Next = NullGroupingProvider.Instance;
		}
		
		#region IGroupingProvider implementation

		protected abstract T GetGroupingKey (IssueSummary issue);
		
		protected abstract string GetGroupName (IssueSummary issue);

		public IssueGroup GetIssueGroup (IssueGroup parentGroup, IssueSummary issue)
		{
			IssueGroup group;
			var providerCategory = GetGroupingKey (issue);
			if (providerCategory == null)
				return null;
			var key = Tuple.Create (parentGroup, providerCategory);
			if (!groups.TryGetValue (key, out group)) {
				group = new IssueGroup (Next, GetGroupName(issue));
				groups.Add (key, group);
			}
			return group;
		}

		public void Reset ()
		{
			groups.Clear ();
			Next.Reset ();
		}

		IGroupingProvider next;
		public IGroupingProvider Next
		{
			get {
				return next;
			}
			set {
				var eventArgs = new GroupingProviderEventArgs (this, next);
				next = value;
				foreach (var group in groups.Values) {
					group.GroupingProvider = value;
				}
				OnNextChanged (eventArgs);
			}
		}

		protected virtual void OnNextChanged (GroupingProviderEventArgs eventArgs)
		{
			var handler = nextChanged;
			if (handler != null) {
				handler (this, eventArgs);
			}
		}
		
		event EventHandler<GroupingProviderEventArgs> nextChanged;
		
		event EventHandler<GroupingProviderEventArgs> IGroupingProvider.NextChanged
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

