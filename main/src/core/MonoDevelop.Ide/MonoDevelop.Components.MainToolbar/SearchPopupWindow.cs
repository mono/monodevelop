// 
// SearchPopupWindow.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//       Vsevolod Kukol <sevoku@microsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using Mono.Addins;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Text;
using System.Collections.Immutable;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.Components.MainToolbar
{
	class SearchPopupWindow : XwtThemedPopup
	{
		public new SearchPopupWidget Content {
			get { return base.Content as SearchPopupWidget; }
			private set { base.Content = value; }
		}

		internal event EventHandler SelectedItemChanged {
			add => Content.SelectedItemChanged += value;
			remove => Content.SelectedItemChanged -= value;
		}

		public SearchPopupWindow () : base (PopupType.Tooltip)
		{
			var native = BackendHost.Backend.Window as Gtk.Window;
			if (native != null)
				native.TypeHint = Gdk.WindowTypeHint.Combo;
			this.TransientFor = Xwt.MessageDialog.RootWindow;
			Content = new SearchPopupWidget ();
			Theme.BackgroundColor = Styles.GlobalSearch.BackgroundColor;
			Content.MarginTop = Content.MarginBottom = 2;
			Content.MarginLeft = Content.MarginRight = 0;
			Theme.Padding = 0;
		}

		public bool SearchForMembers {
			get { return Content.SearchForMembers; }
			set { Content.SearchForMembers = value; }
		}

		public void Update (SearchPopupSearchPattern pattern)
		{
			Content.Update (pattern);
		}

		internal void OpenFile ()
		{
			Content.OpenFile ();
		}

		internal bool ProcessKey (Key key, ModifierKeys state)
		{
			return Content.ProcessKey (key, state);
		}

		protected override void OnDrawContent (Context context, Rectangle contentBounds, Rectangle backgroundBounds)
		{
			base.OnDrawContent (context, contentBounds, backgroundBounds);
			var headerMargin = Content.GetHeaderMargin () + contentBounds.X;

			if (headerMargin > 0) {
				context.SetColor (Styles.GlobalSearch.HeaderBackgroundColor);
				context.Rectangle (backgroundBounds.X, backgroundBounds.Y, headerMargin, backgroundBounds.Height);
				context.Fill ();
				context.MoveTo (0.5 + backgroundBounds.X + headerMargin, 0);
				context.LineTo (0.5 + backgroundBounds.X + headerMargin, backgroundBounds.Height);
				context.SetColor (Styles.GlobalSearch.SeparatorLineColor);
				context.Stroke ();
			}
		}
	}

	class SearchPopupWidget : Canvas
	{
		const int yMargin = 0;
		const int xMargin = 6;
		const int itemSeparatorHeight = 0;
		const int marginIconSpacing = 4;
		const int iconTextSpacing = 6;
		const int categorySeparatorHeight = 8;
		const int headerMarginSize = 100;
		const int itemPadding = 4;

		List<SearchCategory> categories = new List<SearchCategory> ();
		List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> results = new List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> ();
		TextLayout layout, headerLayout;
		CancellationTokenSource src;
		Color headerColor;
		Color selectionBackgroundColor;

		bool isInSearch;
		class NullDataSource : ISearchDataSource
		{
			#region ISearchDataSource implementation
			Xwt.Drawing.Image ISearchDataSource.GetIcon (int item)
			{
				throw new NotImplementedException ();
			}
			string ISearchDataSource.GetMarkup (int item, bool isSelected)
			{
				throw new NotImplementedException ();
			}
			string ISearchDataSource.GetDescriptionMarkup (int item, bool isSelected)
			{
				throw new NotImplementedException ();
			}
			Task<TooltipInformation> ISearchDataSource.GetTooltip (CancellationToken token, int item)
			{
				throw new NotImplementedException ();
			}
			double ISearchDataSource.GetWeight (int item)
			{
				throw new NotImplementedException ();
			}
			
			ISegment ISearchDataSource.GetRegion (int item)
			{
				throw new NotImplementedException ();
			}
			
			string ISearchDataSource.GetFileName (int item)
			{
				throw new NotImplementedException ();
			}
			bool ISearchDataSource.CanActivate (int item)
			{
				throw new NotImplementedException ();
			}
			void ISearchDataSource.Activate (int item)
			{
				throw new NotImplementedException ();
			}
			int ISearchDataSource.ItemCount {
				get {
					return 0;
				}
			}
			#endregion
		}

		public SearchPopupWidget ()
		{
			headerColor = Styles.GlobalSearch.HeaderTextColor;
			selectionBackgroundColor = Styles.GlobalSearch.SelectionBackgroundColor;

			Toolkit.Load (ToolkitType.Gtk).Invoke (() => declarationviewwindow = new TooltipInformationWindow ());

			categories.Add (new RoslynSearchCategory ());
			categories.Add (new FileSearchCategory ());
			categories.Add (new CommandSearchCategory ());
			categories.Add (new SearchInSolutionSearchCategory ());
			foreach (var cat in AddinManager.GetExtensionObjects<SearchCategory> ("/MonoDevelop/Ide/SearchCategories")) {
				categories.Add (cat);
				cat.Initialize (ParentWindow as XwtPopup);
			}

			categories.Sort ();

			layout = new TextLayout ();
			headerLayout = new TextLayout ();

			layout.Trimming = TextTrimming.WordElipsis;
			headerLayout.Trimming = TextTrimming.WordElipsis;

			ItemActivated += (sender, e) => OpenFile ();
		}

		public bool SearchForMembers {
			get;
			set;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				if (src != null)
					src.Cancel ();
				HideTooltip ();
				if (layout != null) {
					layout.Dispose ();
					headerLayout.Dispose ();
				}
				this.declarationviewwindow.Destroy ();
			}
			layout = null;
			headerLayout = null;
			SelectedItem = topItem = null;
			currentTooltip = null;
			base.Dispose (disposing);
		}

		internal async void OpenFile ()
		{
			if (SelectedItem == null || SelectedItem.Item < 0 || SelectedItem.Item >= SelectedItem.DataSource.Count)
				return;

			if (SelectedItem.DataSource[SelectedItem.Item].CanActivate) {
				SelectedItem.DataSource[SelectedItem.Item].Activate ();
				ParentWindow.Dispose ();
			}
			else {
				var region = SelectedItemRegion;
				if (string.IsNullOrEmpty (SelectedItemFileName)) {
					ParentWindow.Dispose ();
					return;
				}

				if (region.Length <= 0) {
					if (Pattern.LineNumber == 0) {
						await IdeApp.Workbench.OpenDocument (SelectedItemFileName, project: null);
					} else {
						await IdeApp.Workbench.OpenDocument (SelectedItemFileName, null, Pattern.LineNumber, Pattern.HasColumn ? Pattern.Column : 1);
					}
				} else {
					await IdeApp.Workbench.OpenDocument (new FileOpenInformation (SelectedItemFileName, null) {
						Offset = region.Offset
					});
				}
				ParentWindow.Dispose ();
			}
		}
		SearchPopupSearchPattern pattern;

		public SearchPopupSearchPattern Pattern {
			get {
				return pattern;
			}
		}
		static readonly SearchCategory.DataItemComparer cmp = new SearchCategory.DataItemComparer ();

		class SearchResultCollector : ISearchResultCallback
		{
			List<SearchResult> searchResults = new List<SearchResult> (maxItems);

			public IReadOnlyList<SearchResult> Results {
				get {
					return searchResults;
				}	
			}

			public SearchCategory Category { get; private set;}

			public SearchResultCollector (SearchCategory cat)
			{
				this.Category = cat;
			}

			public Task Task { get; set; }

			#region ISearchResultCallback implementation
			void ISearchResultCallback.ReportResult (SearchResult result)
			{
				if (maxItems == searchResults.Count) {
					int i = searchResults.Count;
					while (i > 0) {
						if (cmp.Compare (result, searchResults [i - 1]) > 0)
							break;
						i--;
					}
					if (i == maxItems) {
						return;//this means it's worse then current worst
					} else {
						if (!result.IsValid)
							return;
						searchResults.RemoveAt (maxItems - 1);
						searchResults.Insert (i, result);
					}
				} else {
					if (!result.IsValid)
						return;
					int i = searchResults.Count;
					while (i > 0) {
						if (cmp.Compare (result, searchResults [i - 1]) > 0)
							break;
						i--;
					}
					searchResults.Insert (i, result);
				}
			}

			#endregion
		}

		public void Update (SearchPopupSearchPattern pattern)
		{
			// in case of 'string:' it's not clear if the user ment 'tag:pattern'  or 'pattern:line' therefore guess
			// 'tag:', if no valid tag is found guess 'pattern:'
			if (!string.IsNullOrEmpty (pattern.Tag) && string.IsNullOrEmpty (pattern.Pattern) && !categories.Any (c => c.IsValidTag (pattern.Tag))) {
				pattern = new SearchPopupSearchPattern (null, pattern.Tag, pattern.LineNumber, pattern.Column, pattern.UnparsedPattern);
			}

			this.pattern = pattern;
			if (src != null)
				src.Cancel ();
			HideTooltip ();
			src = new CancellationTokenSource ();
			isInSearch = true;
			if (results.Count == 0) {
				QueueDraw ();
			}
			var collectors = new List<SearchResultCollector> ();
			var token = src.Token;
			foreach (var _cat in categories) {
				var cat = _cat;
				if (!string.IsNullOrEmpty (pattern.Tag) && !cat.IsValidTag (pattern.Tag))
					continue;
				var col = new SearchResultCollector (_cat);
				collectors.Add (col);
				col.Task = cat.GetResults (col, pattern, token);
			}

			Task.WhenAll (collectors.Select (c => c.Task)).ContinueWith (t => {
				if (token.IsCancellationRequested)
					return;
				var newResults = new List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> (collectors.Count);
				foreach (var col in collectors) {
					if (col.Task.IsCanceled) {
						continue;
					} else if (col.Task.IsFaulted) {
						LoggingService.LogError ($"Error getting search results for {col.Category}", col.Task.Exception);
					} else {
						newResults.Add (Tuple.Create (col.Category, col.Results));
					}
				}

				List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> failedResults = null;
				ItemIdentifier topResult = null;
				for (int i = 0; i < newResults.Count; i++) {
					var tuple = newResults [i];
					try {
						if (tuple.Item2.Count == 0)
							continue;
						if (topResult == null || topResult.DataSource [topResult.Item].Weight < tuple.Item2 [0].Weight)
							topResult = new ItemIdentifier (tuple.Item1, tuple.Item2, 0);
					} catch (Exception e) {
						LoggingService.LogError ("Error while showing result " + i, e);
						if (failedResults == null)
							failedResults = new List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> ();
						failedResults.Add (newResults [i]);
						continue;
					}
				}

				if (failedResults != null)
					failedResults.ForEach (failedResult => newResults.Remove (failedResult));

				InvokeAsync (delegate {
					if (token.IsCancellationRequested)
						return;
					ShowResults (newResults, topResult);
					isInSearch = false;

					OnPreferredSizeChanged ();
				});
			}, token);
		}

		void ShowResults (List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> newResults, ItemIdentifier topResult)
		{
			results = newResults;
			SelectedItem = topItem = topResult;
			ShowTooltip ();
		}

		int calculatedItems;
		Size GetIdealSize ()
		{
			Size retVal = new Size ();
			//var location = ParentWindow.Location;
			Rectangle geometry = Desktop.PrimaryScreen.VisibleBounds;
			try {
				if (ParentWindow?.TransientFor != null)
					//geometry = ParentWindow.Visible? ParentWindow.Screen.VisibleBounds : ParentWindow.TransientFor.Screen.VisibleBounds;
					geometry = ParentWindow.TransientFor.Screen.VisibleBounds;
			} catch (Exception e) {
				LoggingService.LogError ("Can not get SearchPopupWindow Screen bounds", e);
			}
			int maxHeight = (int)geometry.Height * 4 / 5;
			double startY = yMargin + ParentBounds.Y;
			double y = startY;
			calculatedItems = 0;
			foreach (var result in results) {
				var dataSrc = result.Item2;
				if (dataSrc.Count == 0)
					continue;
				
				for (int i = 0; i < maxItems && i < dataSrc.Count; i++) {
					layout.Markup = GetRowMarkup (dataSrc[i]);
					var ls = layout.GetSize ();
					if (y + ls.Height + itemSeparatorHeight + itemPadding * 2 > maxHeight)
						break;
					y += ls.Height + itemSeparatorHeight + itemPadding * 2;
					calculatedItems++;
				}
			}
			retVal.Width = Math.Min ((int)geometry.Width * 4 / 5, 480);
			if (Math.Abs (y - startY) < 1) {
				layout.Markup = GettextCatalog.GetString ("No matches");
				var ls = layout.GetSize ();
				var realHeight = ls.Height + itemSeparatorHeight + 4 + itemPadding * 2;
				y += realHeight;
			} else {
				y -= itemSeparatorHeight;
			}

			var calculatedHeight = Math.Min (
				maxHeight, 
				(int)y + yMargin + results.Count (res => res.Item2.Count > 0) * categorySeparatorHeight
			);
			retVal.Height = calculatedHeight;
			return retVal;
		}

		const int maxItems = 8;

		protected override Size OnGetPreferredSize (SizeConstraint widthConstraint, SizeConstraint heightConstraint)
		{
			return GetIdealSize ();
		}

		ItemIdentifier GetItemAt (double px, double py)
		{
			double y = ParentBounds.Y + yMargin;
			if (topItem != null){
				layout.Markup = GetRowMarkup (topItem.DataSource[topItem.Item]);
				var ls = layout.GetSize ();
				y += ls.Height + itemSeparatorHeight + itemPadding * 2;
				if (y > py)
					return new ItemIdentifier (topItem.Category, topItem.DataSource, topItem.Item);
			}
			foreach (var result in results) {
				var category = result.Item1;
				var dataSrc = result.Item2;
				int itemsAdded = 0;
				for (int i = 0; i < maxItems && i < dataSrc.Count; i++) {
					if (topItem != null && topItem.DataSource == dataSrc && topItem.Item == i)
						continue;
					layout.Markup = GetRowMarkup (dataSrc[i]);

					var ls = layout.GetSize ();
					y += ls.Height + itemSeparatorHeight + itemPadding * 2;
					if (y > py){
						return new ItemIdentifier (category, dataSrc, i);
					}

//					var region = dataSrc.GetRegion (i);
//					if (!region.IsEmpty) {
//						layout.SetMarkup (region.BeginLine.ToString ());
//						int w2, h2;
//						layout.GetPixelSize (out w2, out h2);
//						w += w2;
//					}
					itemsAdded++;
				}
				if (itemsAdded > 0)
					y += categorySeparatorHeight;
			}
			return null;
		}

		protected override void OnMouseMoved (MouseMovedEventArgs args)
		{
			var item = GetItemAt (args.X, args.Y);
			if (item == null && SelectedItem != null || 
			    item != null && SelectedItem == null || item != null && !item.Equals (SelectedItem)) {
				SelectedItem = item;
				ShowTooltip ();
				QueueDraw ();
			}
			base.OnMouseMoved (args);
		}

		protected override void OnButtonPressed (ButtonEventArgs args)
		{
			if (args.Button == PointerButton.Left) {
				if (SelectedItem != null)
					OnItemActivated (EventArgs.Empty);
			}
			base.OnButtonPressed (args);
		}

		int SelectedCategoryIndex {
			get {
				for (int i = 0; i < results.Count; i++) {
					if (results [i].Item1 == SelectedItem.Category) {
						return i;
					}
				}
				return -1;
			}
		}

		void SelectItemUp ()
		{
			if (SelectedItem == null || SelectedItem == topItem)
				return;
			int i = SelectedCategoryIndex;
			if (SelectedItem.Item > 0) {
				SelectedItem = new ItemIdentifier (SelectedItem.Category, SelectedItem.DataSource, SelectedItem.Item - 1);
				if (i > 0 && SelectedItem.Equals (topItem)) {
					SelectItemUp ();
					return;
				}
			} else {
				if (i == 0) {
					SelectedItem = topItem;
				} else {
					do {
						i--;
						SelectedItem = new ItemIdentifier (
							results [i].Item1,
							results [i].Item2,
							Math.Min (maxItems, results [i].Item2.Count) - 1
						);
						if (SelectedItem.Category == topItem.Category && SelectedItem.Item == topItem.Item && i > 0) {
							i--;
							SelectedItem = new ItemIdentifier (
								results [i].Item1,
								results [i].Item2,
								Math.Min (maxItems, results [i].Item2.Count) - 1
							);
						}
							
					} while (i > 0 && SelectedItem.DataSource.Count <= 0);

					if (SelectedItem.DataSource.Count <= 0) {
						SelectedItem = topItem;
					}
				}
			}
			ShowTooltip ();
			QueueDraw ();
		}

		void SelectItemDown ()
		{
			if (SelectedItem == null)
				return;

			if (SelectedItem.Equals (topItem)) {
				for (int j = 0; j < results.Count; j++) {
					if (results[j].Item2.Count == 0 || results[j].Item2.Count == 1 && topItem.DataSource == results[j].Item2)
						continue;
					SelectedItem = new ItemIdentifier (
						results [j].Item1,
						results [j].Item2,
						0
						);
					if (SelectedItem.Equals (topItem))
						goto normalDown;
					break;
				}
				ShowTooltip ();
				QueueDraw ();
				return;
			}
		normalDown:
			var i = SelectedCategoryIndex;

			// check real upper bound
			if (SelectedItem != null) {
				var curAbsoluteIndex = SelectedItem == topItem ? 1 : 0;
				for (int j = 0; j < i; j++) {
					curAbsoluteIndex += Math.Min (maxItems, results [j].Item2.Count);
				}
				curAbsoluteIndex += SelectedItem.Item + 1;
				if (curAbsoluteIndex + 1 > calculatedItems)
					return;
			}

			var upperBound = Math.Min (maxItems, SelectedItem.DataSource.Count);
			if (SelectedItem.Item + 1 < upperBound) {
				if (topItem.DataSource == SelectedItem.DataSource && SelectedItem.Item == upperBound - 1)
					return;
				SelectedItem = new ItemIdentifier (SelectedItem.Category, SelectedItem.DataSource, SelectedItem.Item + 1);
			} else {
				for (int j = i + 1; j < results.Count; j++) {
					if (results[j].Item2.Count == 0 || results[j].Item2.Count == 1 && topItem.DataSource == results[j].Item2)
						continue;
					SelectedItem = new ItemIdentifier (
						results [j].Item1,
						results [j].Item2,
						0
						);
					if (SelectedItem.Equals (topItem)) {
						SelectedItem = new ItemIdentifier (
							results [j].Item1,
							results [j].Item2,
							1
							);
					}

					break;
				}
			}
			ShowTooltip ();
			QueueDraw ();
		}

		TooltipInformationWindow declarationviewwindow;
		TooltipInformation currentTooltip;

		void HideTooltip ()
		{
			if (declarationviewwindow != null) {
				declarationviewwindow.Hide ();
			}
			if (tooltipSrc != null)
				tooltipSrc.Cancel ();
		}

		CancellationTokenSource tooltipSrc = null;
		async void ShowTooltip ()
		{
			var currentSelectedItem = SelectedItem;
			if (currentSelectedItem == null || currentSelectedItem.DataSource == null) {
				HideTooltip ();
				return;
			}
			var i = currentSelectedItem.Item;
			if (i < 0 || i >= currentSelectedItem.DataSource.Count)
				return;

			if (tooltipSrc != null)
				tooltipSrc.Cancel ();
			tooltipSrc = new CancellationTokenSource ();
			var token = tooltipSrc.Token;

			try {
				currentTooltip = await currentSelectedItem.DataSource [i].GetTooltipInformation (token);
			} catch (OperationCanceledException) {
				HideTooltip ();
				return;
			} catch (Exception e) {
				LoggingService.LogError ("Error while creating search popup window tooltip", e);
				HideTooltip ();
				return;
			}
			if (currentTooltip == null || string.IsNullOrEmpty (currentTooltip.SignatureMarkup) || token.IsCancellationRequested) {
				HideTooltip ();
				return;
			}

			declarationviewwindow.Hide ();
			declarationviewwindow.Clear ();
			declarationviewwindow.AddOverload (currentTooltip);
			declarationviewwindow.CurrentOverload = 0;
			declarationviewwindow.ShowArrow = true;
			var rect = SelectedItemRectangle;
			declarationviewwindow.ShowPopup (this, new Rectangle (0, (int)rect.Y - 5, Bounds.Width, (int)rect.Height), PopupPosition.Right);
		}

		void SelectNextCategory ()
		{
			if (SelectedItem == null)
				return;
			var i = SelectedCategoryIndex;
			if (SelectedItem.Equals (topItem)) {
				if (i > 0) {
					SelectedItem = new ItemIdentifier (
						results [0].Item1,
						results [0].Item2,
						0
					);

				} else {
					if (topItem.DataSource.Count > 1) {
						SelectedItem = new ItemIdentifier (
							results [0].Item1,
							results [0].Item2,
							1
						);
					} else if (i < results.Count - 1) {
						SelectedItem = new ItemIdentifier (
							results [i + 1].Item1,
							results [i + 1].Item2,
							0
						);
					}
				}
			} else {
				while (i < results.Count - 1 && results [i + 1].Item2.Count == 0)
					i++;
				if (i < results.Count - 1) {
					SelectedItem = new ItemIdentifier (
						results [i + 1].Item1,
						results [i + 1].Item2,
						0
					);
				}
			}
			ShowTooltip ();
			QueueDraw ();	
		}

		void SelectPrevCategory ()
		{
			if (SelectedItem == null)
				return;
			var i = SelectedCategoryIndex;
			if (i > 0) {
				SelectedItem = new ItemIdentifier (
					results [i - 1].Item1,
					results [i - 1].Item2,
					0
				);
				if (SelectedItem.Equals (topItem)) {
					if (topItem.DataSource.Count> 1) {
						SelectedItem = new ItemIdentifier (
							results [i - 1].Item1,
							results [i - 1].Item2,
							1
						);
					} else if (i > 1) {
						SelectedItem = new ItemIdentifier (
							results [i - 2].Item1,
							results [i - 2].Item2,
							0
						);
					}
				}
			} else {
				SelectedItem = topItem;
			}
			ShowTooltip ();
			QueueDraw ();
		}

		void SelectFirstCategory ()
		{
			SelectedItem = topItem;
			ShowTooltip ();
			QueueDraw ();
		}

		void SelectLastCatgory ()
		{
			var r = results.LastOrDefault (r2 => r2.Item2.Count > 0 && !(r2.Item2.Count == 1 && topItem.Category == r2.Item1));
			if (r == null)
				return;
			SelectedItem = new ItemIdentifier (
				r.Item1,
				r.Item2,
				r.Item2.Count - 1
			);
			ShowTooltip ();
			QueueDraw ();
		}

		internal bool ProcessKey (Xwt.Key key, Xwt.ModifierKeys state)
		{
			switch (key) {
			case Xwt.Key.Up:
				if (state.HasFlag (Xwt.ModifierKeys.Command))
					goto case Xwt.Key.PageUp;
				if (state.HasFlag (Xwt.ModifierKeys.Control))
					SelectFirstCategory ();
				else
					SelectItemUp ();
				return true;
			case Xwt.Key.Down:
				if (state.HasFlag (Xwt.ModifierKeys.Command))
					goto case Xwt.Key.PageDown;
				if (state.HasFlag (Xwt.ModifierKeys.Control))
					SelectLastCatgory ();
				else
					SelectItemDown ();
				return true;
			case (Xwt.Key)Gdk.Key.KP_Page_Down:
			case Xwt.Key.PageDown:
				SelectNextCategory ();
				return true;
			case (Xwt.Key)Gdk.Key.KP_Page_Up:
			case Xwt.Key.PageUp:
				SelectPrevCategory ();
				return true;
			case Xwt.Key.Return:
				OnItemActivated (EventArgs.Empty);
				return true;
			}
			return false;
		}

		public event EventHandler ItemActivated;

		protected virtual void OnItemActivated (EventArgs e)
		{
			EventHandler handler = this.ItemActivated;
			if (handler != null)
				handler (this, e);
		}

		public ISegment SelectedItemRegion {
			get {
				if (SelectedItem == null || SelectedItem.Item < 0 || SelectedItem.Item >= SelectedItem.DataSource.Count)
					return TextSegment.Invalid;
				return SelectedItem.DataSource[SelectedItem.Item].Segment;
			}
		}

		public string SelectedItemFileName {
			get {
				if (SelectedItem == null || SelectedItem.Item < 0 || SelectedItem.Item >= SelectedItem.DataSource.Count)
					return null;
				return SelectedItem.DataSource[SelectedItem.Item].File;
			}
		}

		internal class ItemIdentifier {
			public SearchCategory Category { get; private set; }
			public IReadOnlyList<SearchResult> DataSource { get; private set; }
			public int Item { get; private set; }

			public ItemIdentifier (SearchCategory category, IReadOnlyList<SearchResult> dataSource, int item)
			{
				this.Category = category;
				this.DataSource = dataSource;
				this.Item = item;
			}

			public override bool Equals (object obj)
			{
				if (obj == null)
					return false;
				if (ReferenceEquals (this, obj))
					return true;
				if (obj.GetType () != typeof(ItemIdentifier))
					return false;
				ItemIdentifier other = (ItemIdentifier)obj;
				return Category == other.Category && DataSource == other.DataSource && Item == other.Item;
			}
			
			public override int GetHashCode ()
			{
				unchecked {
					return (Category != null ? Category.GetHashCode () : 0) ^ (DataSource != null ? DataSource.GetHashCode () : 0) ^ Item.GetHashCode ();
				}
			}

			public override string ToString ()
			{
				return string.Format ("[ItemIdentifier: Category={0}, DataSource=#{1}, Item={2}]", Category.Name, DataSource.Count, Item);
			}
		}
		ItemIdentifier topItem;
		ItemIdentifier selectedItem;

		internal ItemIdentifier SelectedItem {
			get => selectedItem; 
			set {
				selectedItem = value;
				SelectedItemChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		internal event EventHandler SelectedItemChanged;

		Rectangle SelectedItemRectangle {
			get {
				if (SelectedItem == null)
					return new Rectangle (0, 0, Bounds.Width, 16);

				double y = Bounds.Y + yMargin;
				if (topItem != null){
					layout.Markup = GetRowMarkup (topItem.DataSource[topItem.Item]);
					var ls = layout.GetSize ();
					if (topItem.Category == SelectedItem.Category && topItem.Item == SelectedItem.Item)
						return new Rectangle (0, y, Bounds.Width, ls.Height + itemSeparatorHeight + itemPadding * 2);
					y += ls.Height + itemSeparatorHeight + itemPadding * 2;
				}
				foreach (var result in results) {
					var category = result.Item1;
					var dataSrc = result.Item2;
					int itemsAdded = 0;
					for (int i = 0; i < maxItems && i < dataSrc.Count; i++) {
						if (topItem != null && topItem.DataSource == dataSrc && topItem.Item == i)
							continue;
						layout.Markup = GetRowMarkup (dataSrc[i]);

						var ls = layout.GetSize ();

						if (SelectedItem.Category == category && SelectedItem.DataSource == dataSrc && SelectedItem.Item == i)
							return new Rectangle (0, y, Bounds.Width, ls.Height + itemSeparatorHeight + itemPadding * 2);
						y += ls.Height + itemSeparatorHeight + itemPadding * 2;

//						var region = dataSrc.GetRegion (i);
//						if (!region.IsEmpty) {
//							layout.SetMarkup (region.BeginLine.ToString ());
//							int w2, h2;
//							layout.GetPixelSize (out w2, out h2);
//							w += w2;
//						}
						itemsAdded++;
					}
					if (itemsAdded > 0)
						y += categorySeparatorHeight;
				}
				return new Rectangle (0, 0, Bounds.Width, 16);
			}
		}

		internal int GetHeaderMargin ()
		{
			var r = results.Where (res => res.Item2.Count > 0).ToArray ();
			if (r.Length > 0)
				return (int)(ParentBounds.X - Bounds.X + headerMarginSize);
			return -1;
		}

		protected override void OnDraw (Context context, Rectangle dirtyRect)
		{
			context.SetLineWidth (1);
			var alloc = ParentBounds;
			var adjustedMarginSize = alloc.X - Bounds.X  + headerMarginSize;

			var r = results.Where (res => res.Item2.Count > 0).ToArray ();
			var length = r.Length;

			double y = alloc.Y + yMargin;
			Size ls;
			if (topItem != null) {
				headerLayout.Text = GettextCatalog.GetString ("Top Result");
				ls = headerLayout.GetSize ();
				context.SetColor (headerColor);
				context.DrawTextLayout (headerLayout, alloc.Left + headerMarginSize - ls.Width - xMargin, y + itemPadding);

				var category = topItem.Category;
				var dataSrc = topItem.DataSource;
				var i = topItem.Item;
				var isSelected = SelectedItem != null && SelectedItem.Category == category && SelectedItem.Item == i;

				double x = alloc.X + xMargin + headerMarginSize;
				context.SetColor (Xwt.Drawing.Colors.Black);
				layout.Markup = GetRowMarkup (dataSrc[i], isSelected);
				ls = layout.GetSize ();
				if (isSelected) {
					context.SetColor (selectionBackgroundColor);
					context.Rectangle (alloc.X + headerMarginSize + 1, y, Bounds.Width - adjustedMarginSize - 1, ls.Height + itemPadding * 2);
					context.Fill ();
					context.SetColor (Xwt.Drawing.Colors.White);
				}

				var px = dataSrc[i].Icon;
				if (px != null) {
					if (isSelected)
						px = px.WithStyles ("sel");
					context.DrawImage (px, (int)x + marginIconSpacing, (int)(y + itemPadding));
					x += px.Width + iconTextSpacing + marginIconSpacing;
				}

				context.SetColor (Xwt.Drawing.Colors.Black);
				context.DrawTextLayout (layout, x, y + itemPadding);

				y += ls.Height + itemSeparatorHeight + itemPadding * 2;

			}

			for (int n = 0; n < length; ++n) {
				var result = r [n];
				var category = result.Item1;
				var dataSrc = result.Item2;
				if (dataSrc.Count == 0)
					continue;
				if (dataSrc.Count == 1 && topItem != null && topItem.DataSource == dataSrc)
					continue;
				headerLayout.Text = category.Name;
				ls = headerLayout.GetSize ();

				if (y + ls.Height + itemPadding * 2 > Bounds.Height)
					break;

				context.SetColor (headerColor);
				context.DrawTextLayout (headerLayout, alloc.X + headerMarginSize - ls.Width - xMargin, y + itemPadding);

				layout.Width = Bounds.Width - adjustedMarginSize - 35;

				for (int i = 0; i < maxItems && i < dataSrc.Count; i++) {
					if (topItem != null && topItem.Category == category && topItem.Item == i)
						continue;
					var isSelected = SelectedItem != null && SelectedItem.Category == category && SelectedItem.Item == i;
					double x = alloc.X + xMargin + headerMarginSize;
					context.SetColor (Xwt.Drawing.Colors.Black);
					layout.Markup = GetRowMarkup (dataSrc[i], isSelected);
					ls = layout.GetSize ();
					if (y + ls.Height + itemSeparatorHeight + itemPadding * 2 > Bounds.Height)
						break;
					if (isSelected) {
						context.SetColor (selectionBackgroundColor);
						context.Rectangle (alloc.X + headerMarginSize + 1, y, Bounds.Width - adjustedMarginSize - 1, ls.Height + itemPadding * 2);
						context.Fill ();
						context.SetColor (Xwt.Drawing.Colors.White);
					}

					var px = dataSrc[i].Icon;
					if (px != null) {
						if (isSelected)
							px = px.WithStyles ("sel");
						context.DrawImage (px, (int)x + marginIconSpacing, (int)(y + itemPadding));
						x += px.Width + iconTextSpacing + marginIconSpacing;
					}

					context.SetColor (Xwt.Drawing.Colors.Black);
					context.DrawTextLayout (layout, x, y + itemPadding);

					y += ls.Height + itemSeparatorHeight + itemPadding * 2;
				}
				if (n != length - 1) {
					y += categorySeparatorHeight;
				}
			}
			if (y == alloc.Y + yMargin) {
				context.SetColor (Styles.GlobalSearch.ResultTextColor);
				layout.Markup = isInSearch ? GettextCatalog.GetString ("Searching...") : GettextCatalog.GetString ("No matches");
				context.DrawTextLayout (layout, alloc.X + xMargin, y);
			}
		}

		static string selectedResultTextColor = Styles.ColorGetHex (Styles.GlobalSearch.SelectedResultTextColor);
		static string resultTextColor = Styles.ColorGetHex (Styles.GlobalSearch.ResultTextColor);
		static string selectedResultDescriptionTextColor = Styles.ColorGetHex (Styles.GlobalSearch.SelectedResultDescriptionTextColor);
		static string resultDescriptionTextColor = Styles.ColorGetHex (Styles.GlobalSearch.ResultDescriptionTextColor);
		string GetRowMarkup (SearchResult result, bool selected = false)
		{
			var resultFgColor = selected ? selectedResultTextColor : resultTextColor;
			var descFgColor = selected ? selectedResultDescriptionTextColor : resultDescriptionTextColor;
			string text = Ide.TypeSystem.MarkupUtilities.UnescapeString (result.GetMarkupText (selected));
			string desc = Ide.TypeSystem.MarkupUtilities.UnescapeString (result.GetDescriptionMarkupText ());

			int descLength = desc != null ? desc.Length : 0;

			var sb = new System.Text.StringBuilder (text.Length + resultFgColor.Length + descLength + descFgColor.Length + 68);
			sb.Append ("<span foreground=\"");
			sb.Append (resultFgColor);
			sb.Append ("\">");
			sb.Append (text);
			sb.Append ("</span>");
			if (descLength > 0) {
				sb.Append ("<span foreground=\"");
				sb.Append (descFgColor);
				sb.Append ("\" font=\"" + ParentWindow.Theme.Font.WithScaledSize (0.7).ToString () + "\">\n");
				sb.Append (desc);
				sb.Append ("</span>");
			}
			return sb.ToString ();
		}

		public new SearchPopupWindow ParentWindow {
			get { return base.ParentWindow as SearchPopupWindow; }
		}

	}
}

