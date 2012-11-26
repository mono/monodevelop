// 
// NavigateToDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Gdk;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Components.MainToolbar;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.Components.MainToolbar
{
	class ResultsDataSource: List<SearchResult>, ISearchDataSource
	{

		Gtk.Widget widget;
		SearchResult bestResult;
		int bestRank = int.MinValue;

		public ResultsDataSource (Gtk.Widget widget)
		{
			this.widget = widget;
		}

		public void SortUpToN (MonoDevelop.Components.MainToolbar.SearchCategory.DataItemComparer comparison, int n)
		{
			// use built-in sorting for small lists since the fast algorithm is only correct for lists larger than n * 2
			if (Count < n * 2) {
				this.Sort (comparison);
				return;
			}
			int offset = 0;
			// build binary heap from all items
			for (int i = 0; i < Count; i++) {
				int index = i;
				var item = this [offset + i]; // use next item
				
				// and move it on top, if greater than parent
				while (index > 0 &&
				       comparison.Compare (this[offset + (index - 1) / 2], item) > 0) {
					int top = (index - 1) / 2;
					this [offset + index] = this [offset + top];
					index = top;
				}
				this [offset + index] = item;
			}
			
			var bound = Math.Max (0, Count - 1 - n);
			for (int i = Count - 1; i > bound; i--) {
				// delete max and place it as last
				var last = this [offset + i];
				this [offset + i] = this [offset];

				int index = 0;
				// the last one positioned in the heap
				while (index * 2 + 1 < i) {
					int left = index * 2 + 1, right = left + 1;
					
					if (right < i && comparison.Compare (this [offset + left], this [offset + right]) > 0) {
						if (comparison.Compare (last, this [offset + right]) < 0)
							break;
						
						this [offset + index] = this [offset + right];
						index = right;
					} else {
						if (comparison.Compare (last, this [offset + left]) < 0)
							break;
						
						this [offset + index] = this [offset + left];
						index = left;
					}
				}
				this [offset + index] = last;
			}

			// switch the lasts elements with the first ones (the last n elements are sorted)
			for (int i = 0; i < n; i++) {
				var tmp = this [Count - 1 - i];
				this [Count - 1 - i] = this [i];
				this [i] = tmp;
			}
		}

		#region ISearchDataSource implementation

		Gdk.Pixbuf ISearchDataSource.GetIcon (int item)
		{
			return this [item].Icon;
		}

		string ISearchDataSource.GetMarkup (int item, bool isSelected)
		{
			if (isSelected)
				return GLib.Markup.EscapeText (this [item].PlainText);
			return this [item].GetMarkupText (widget);
		}

		string ISearchDataSource.GetDescriptionMarkup (int item, bool isSelected)
		{
			if (isSelected)
				return GLib.Markup.EscapeText (this [item].Description);
			return this [item].GetDescriptionMarkupText (widget);
		}

		ICSharpCode.NRefactory.TypeSystem.DomRegion ISearchDataSource.GetRegion (int item)
		{
			var result = this [item];
			return new DomRegion (result.File, result.Row, result.Column, result.Row, result.Column);
		}

		bool ISearchDataSource.CanActivate (int item)
		{
			var result = this [item];
			return result.CanActivate;
		}

		void ISearchDataSource.Activate (int item)
		{
			var result = this [item];
			result.Activate ();
		}
		
		double ISearchDataSource.GetWeight (int item)
		{
			return this [item].Rank;
		}

		int ISearchDataSource.ItemCount {
			get {
				return this.Count;
			}
		}

		TooltipInformation ISearchDataSource.GetTooltip (int item)
		{
			return this [item].TooltipInformation;
		}
		#endregion

		public SearchResult BestResult {
			get {
				return bestResult;
			}
		}

		public void AddResult (SearchResult res)
		{
			Add (res);

			if (res.Rank > bestRank) {
				bestResult = res;
				bestRank = res.Rank;
			}
		}
	}
}
