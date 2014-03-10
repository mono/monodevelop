// 
// Pages.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.ObjectModel;

namespace ICSharpCode.PackageManagement
{
	public class Pages : ObservableCollection<Page>
	{
		public const int DefaultPageSize = 29;
		public const int DefaultMaximumSelectablePages = 5;
		
		int pageSize = DefaultPageSize;
		int selectedPageNumber = 1;
		int maximumSelectablePages = DefaultMaximumSelectablePages;
		int totalItems = 0;
		int itemsOnSelectedPage = 0;
		
		public int TotalItems {
			get { return totalItems; }
			set {
				if (totalItems != value) {
					totalItems = value;
					UpdatePages();
				}
			}
		}
		
		public int SelectedPageNumber {
			get { return selectedPageNumber; }
			set {
				if (selectedPageNumber != value) {
					selectedPageNumber = value;
					UpdatePages();
				}
			}
		}
		
		public int MaximumSelectablePages {
			get { return maximumSelectablePages; }
			set {
				if (maximumSelectablePages != value) {
					maximumSelectablePages = value;
					UpdatePages();
				}
			}
		}
		
		public int ItemsBeforeFirstPage {
			get {
				return (selectedPageNumber - 1) * pageSize;
			}
		}

		public bool IsPaged {
			get { return totalItems > pageSize; }
		}
		
		public bool HasPreviousPage {
			get { return IsPaged && !IsFirstPageSelected; }
		}
		
		bool IsFirstPageSelected {
			get { return selectedPageNumber == 1; }
		}
		
		public bool HasNextPage {
			get { return IsPaged && !IsLastPageSelected; }
		}
		
		bool IsLastPageSelected {
			get { return selectedPageNumber == TotalPages; }
		}
		
		public int TotalPages {
			get { return (totalItems + pageSize - 1) / pageSize; }
		}
		
		public int PageSize {
			get { return pageSize; }
			set {
				if (pageSize != value) {
					pageSize = value;
					UpdatePages();
				}
			}
		}
		
		void UpdatePages()
		{
			Clear();
			
			int startPage = GetStartPage();
			for (int pageNumber = startPage; pageNumber <= TotalPages; ++pageNumber) {
				if (Count >= maximumSelectablePages) {
					break;
				}
				Page page = CreatePage(pageNumber);
				Add(page);
			}
		}
		
		int GetStartPage()
		{
			// Less pages than can be selected?
			int totalPages = TotalPages;
			if (totalPages <= maximumSelectablePages) {
				return 1;
			}
			
			// First choice for start page.
			int startPage = selectedPageNumber - (maximumSelectablePages / 2);
			if (startPage <= 0) {
				return 1;
			}
			
			// Do we have enough pages?
			int totalPagesBasedOnStartPage = totalPages - startPage + 1;
			if (totalPagesBasedOnStartPage >= maximumSelectablePages) {
				return startPage;
			}
			
			// Ensure we have enough pages.
			startPage -= maximumSelectablePages - totalPagesBasedOnStartPage;
			if (startPage > 0) {
				return startPage;
			}
			return 1;
		}
		
		Page CreatePage(int pageNumber)
		{
			var page = new Page();
			page.Number = pageNumber;
			page.IsSelected = IsSelectedPage(pageNumber);
			return page;
		}
		
		bool IsSelectedPage(int pageNumber)
		{
			return pageNumber == selectedPageNumber;
		}
		
		public int TotalItemsOnSelectedPage {
			get { return itemsOnSelectedPage; }
			set {
				itemsOnSelectedPage = value;
				if (itemsOnSelectedPage < pageSize) {
					TotalItems = (selectedPageNumber - 1) * pageSize + itemsOnSelectedPage;
				}
			}
		}
	}
}
