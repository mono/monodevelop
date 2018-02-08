// 
// CompletionListWindowTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;
using Xwt;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Gui
{
	class MockCompletionView : ICompletionView
	{
		IListDataProvider provider;
		ICompletionViewEventSink eventSink;
		int selectedItemIndex = -1;
		List<int> filteredItems = new List<int> ();
		List<int> filteredItemsWithCategories = new List<int> ();
		List<CategorizedCompletionItems> filteredCategories = new List<CategorizedCompletionItems> ();
		List<string> visibleItems = new List<string> ();

		public void Initialize (IListDataProvider provider, ICompletionViewEventSink eventSink)
		{
			this.provider = provider;
			this.eventSink = eventSink;
		}

		public List<string> GetVisibleItems ()
		{
			var result = new List<string> ();
			foreach (var i in filteredItems)
				result.Add (provider.GetCompletionData (i).DisplayText);
			return result;
		}

		public List<string> GetVisibleItemsAndCategories ()
		{
			return visibleItems;
		}

		void UpdateList ()
		{
			visibleItems.Clear ();
			if (filteredCategories.Count > 0 && InCategoryMode) {
				foreach (var c in filteredCategories) {
					if (c.Items.Count == 0)
						continue;
					if (c.CompletionCategory != null)
						visibleItems.Add (c.CompletionCategory.DisplayText);
					else if (filteredCategories.Count > 1)
						visibleItems.Add ("[]");
					foreach (var i in c.Items)
						visibleItems.Add (provider.GetCompletionData (i).DisplayText);
				}

			} else {
				foreach (var i in filteredItems)
					visibleItems.Add (provider.GetCompletionData (i).DisplayText);
			}
		}

		public void ShowFilteredItems (CompletionListFilterResult filterResult)
		{
			this.filteredItems = filterResult.FilteredItems;
			this.filteredCategories = filterResult.CategorizedItems;
			if (GetListIndexFromItemIndex (selectedItemIndex) == -1)
				SelectedItemIndex = -1;

			filteredItemsWithCategories.Clear ();
			foreach (var c in filteredCategories) {
				foreach (var i in c.Items)
					filteredItemsWithCategories.Add (i);
			}

			UpdateList ();
		}

		public bool LoadingMessageVisible { get; set; }

		public int SelectedItemIndex {
			get {
				return selectedItemIndex;
			}
			set {
				if (selectedItemIndex != value) {
					selectedItemIndex = value;
					eventSink.OnSelectedItemChanged ();
				}
			}
		}

		public int SelectedIndex {
			get {
				return GetListIndexFromItemIndex (selectedItemIndex);
			}
		}

		bool inCategoryMode;
		public bool InCategoryMode {
			get => inCategoryMode;
			set {
				inCategoryMode = value;
				UpdateList ();
			}
		}

		public bool SelectionEnabled { get; set; }

		public bool Visible { get; set; }
		public bool Destroyed { get; set; }

		public Rectangle Allocation => new Xwt.Rectangle (0, 0, 100, 100);

		public int X => 0;

		public int Y => 0;

		public Gtk.Window TransientFor { get; set; }

		public void Destroy ()
		{
			Visible = false;
			Destroyed = true;
		}

		public void Hide ()
		{
			Visible = false;
		}

		public void HideLoadingMessage ()
		{
			LoadingMessageVisible = false;
		}

		public void MoveCursor (int relative)
		{
			if (filteredItems.Count == 0)
				return;
			var index = GetListIndexFromItemIndex (selectedItemIndex);
			index += relative;
			if (index < 0)
				index = 0;
			else if (index >= filteredItems.Count)
				index = filteredItems.Count - 1;
			SelectedItemIndex = GetItemIndexFromListIndex (index);
		}

		public void PageDown ()
		{
			MoveCursor (5);
		}

		public void PageUp ()
		{
			MoveCursor (-5);
		}

		public void Reposition (int triggerX, int triggerY, int triggerHeight, bool force)
		{
		}

		public bool RepositionDeclarationViewWindow (TooltipInformationWindow declarationviewwindow, int selectedItem)
		{
			return true;
		}

		public void RepositionWindow (Rectangle? r)
		{
		}

		public void ResetSizes ()
		{
		}

		public void ResetState ()
		{
			selectedItemIndex = -1;
		}

		public void Show ()
		{
			Visible = true;
		}

		public void ShowLoadingMessage ()
		{
			LoadingMessageVisible = true;
		}

		public void ShowPreviewCompletionEntry ()
		{
		}

		int GetListIndexFromItemIndex (int itemIndex)
		{
			if (InCategoryMode)
				return filteredItemsWithCategories.IndexOf (itemIndex);
			else
				return filteredItems.IndexOf (itemIndex);
		}

		int GetItemIndexFromListIndex (int index)
		{
			if (InCategoryMode)
				return filteredItemsWithCategories [index];
			else
				return filteredItems [index];
		}

		public void PerformDoubleClick (int row)
		{
			SelectedItemIndex = GetItemIndexFromListIndex (row);
			eventSink.OnDoubleClick ();
		}

		public void InputPreviewCompletionEntryText (string text)
		{
			var txt = "";
			foreach (var c in text) {
				txt += c;
				var key = KeyDescriptor.FromGtk ((Gdk.Key)c, c, Gdk.ModifierType.None);
				eventSink.OnPreProcessPreviewCompletionEntryKey (key);
				eventSink.OnPreviewCompletionEntryChanged (txt);
			}
		}

		public void PerformPreviewCompletionEntryActivated ()
		{
			eventSink.OnPreviewCompletionEntryActivated ();
		}

		public void PerformPreviewCompletionEntryLostFocus ()
		{
			eventSink.OnPreviewCompletionEntryLostFocus ();
		}
	}
}
