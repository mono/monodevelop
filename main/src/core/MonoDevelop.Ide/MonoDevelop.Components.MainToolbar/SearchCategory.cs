// 
// SearchCategory.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Components.MainToolbar
{
	public abstract class SearchCategory : IComparable<SearchCategory>
	{
		protected const int FirstCategory = -1000;
		protected int sortOrder = 0;

		internal class DataItemComparer : IComparer<SearchResult>
		{
			public int Compare (SearchResult o1, SearchResult o2)
			{
				var r = o2.Rank.CompareTo (o1.Rank);
				if (r == 0)
					r = o1.SearchResultType.CompareTo (o2.SearchResultType);
				if (r == 0)
					return string.CompareOrdinal (o1.MatchedString, o2.MatchedString);
				return r;
			}
		}

		protected struct MatchResult
		{
			public bool Match;
			public int Rank;

			public MatchResult (bool match, int rank)
			{
				this.Match = match;
				this.Rank = rank;
			}
		}

		public string Name  {
			get;
			set;
		}

		public abstract string[] Tags {
			get;
		}

		public SearchCategory (string name)
		{
			this.Name = name;
		}

		public abstract bool IsValidTag (string tag);

		public abstract Task GetResults (ISearchResultCallback searchResultCallback, SearchPopupSearchPattern pattern, CancellationToken token);

		public virtual void Initialize (XwtPopup popupWindow)
		{
			
		}

		public int CompareTo (SearchCategory other)
		{
			return sortOrder.CompareTo (other.sortOrder);
		}
	}
}
