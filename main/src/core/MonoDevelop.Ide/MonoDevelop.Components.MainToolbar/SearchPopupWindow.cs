// 
// SearchPopupWindow.cs
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Collections.Generic;
using Gtk;
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using Mono.Addins;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Text;
using System.Collections.Immutable;

namespace MonoDevelop.Components.MainToolbar
{
	class SearchPopupWindow : PopoverWindow, ISearchResultsDisplay
	{
		const int yMargin = 0;
		const int xMargin = 6;
		const int itemSeparatorHeight = 0;
		const int marginIconSpacing = 4;
		const int iconTextSpacing = 6;
		const int categorySeparatorHeight = 8;
		const int headerMarginSize = 100;
		const int itemPadding = 4;

		SearchService searchService = new SearchService ();
		List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> Results {
			get {
				return searchService.Results;
			}
		}

		Pango.Layout layout, headerLayout;
		Cairo.Color headerColor;
		Cairo.Color separatorLine;
		Cairo.Color darkSearchBackground;
		Cairo.Color lightSearchBackground;

		Cairo.Color selectionBackgroundColor;

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
		public SearchPopupWindow ()
		{
			headerColor = Styles.GlobalSearch.HeaderTextColor.ToCairoColor ();
			separatorLine = Styles.GlobalSearch.SeparatorLineColor.ToCairoColor ();
			lightSearchBackground = Styles.GlobalSearch.HeaderBackgroundColor.ToCairoColor ();
			darkSearchBackground = Styles.GlobalSearch.BackgroundColor.ToCairoColor ();
			selectionBackgroundColor = Styles.GlobalSearch.SelectionBackgroundColor.ToCairoColor ();
			TypeHint = Gdk.WindowTypeHint.Combo;
			this.SkipTaskbarHint = true;
			this.SkipPagerHint = true;
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.AllowShrink = false;
			this.AllowGrow = false;

			searchService.ResultsUpdated += ShowResults;

			layout = new Pango.Layout (PangoContext);
			headerLayout = new Pango.Layout (PangoContext);

			layout.Ellipsize = Pango.EllipsizeMode.End;
			headerLayout.Ellipsize = Pango.EllipsizeMode.End;

			Events = Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonMotionMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.ExposureMask | Gdk.EventMask.PointerMotionMask;
			ItemActivated += (sender, e) => OpenFile ();
		}

		bool inResize = false;

		public bool SearchForMembers {
			get;
			set;
		}

		protected override void OnDestroyed ()
		{
			searchService.Dispose ();
			searchService = null;

			HideTooltip ();
			this.declarationviewwindow.Destroy ();
			selectedItem = topItem = null;
			currentTooltip = null;
			base.OnDestroyed ();
		}

		public async void OpenFile ()
		{
			if (selectedItem == null || selectedItem.Item < 0 || selectedItem.Item >= selectedItem.DataSource.Count)
				return;

			if (selectedItem.DataSource[selectedItem.Item].CanActivate) {
				selectedItem.DataSource[selectedItem.Item].Activate ();
				Destroy ();
			}
			else {
				var region = SelectedItemRegion;
				if (string.IsNullOrEmpty (SelectedItemFileName)) {
					Destroy ();
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
				Destroy ();
			}
		}

		public SearchPopupSearchPattern Pattern {
			get {
				return searchService.Pattern;
			}
		}

		public void Update (SearchPopupSearchPattern pattern)
		{
			HideTooltip ();
			isInSearch = true;
			if (Results.Count == 0) {
				QueueDraw ();
			}

			searchService.Update (pattern);
		}

		void ShowResults (object sender, EventArgs args)
		{
			Application.Invoke (delegate {
				List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> failedResults = null;
				topItem = null;

				for (int i = 0; i < Results.Count; i++) {
					var tuple = Results [i];
					try {
						if (tuple.Item2.Count == 0)
							continue;
						if (topItem == null || topItem.DataSource [topItem.Item].Weight < tuple.Item2 [0].Weight)
							topItem = new ItemIdentifier (tuple.Item1, tuple.Item2, 0);
					} catch (Exception e) {
						LoggingService.LogError ("Error while showing result " + i, e);
						if (failedResults == null)
							failedResults = new List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> ();
						failedResults.Add (Results [i]);
						continue;
					}
				}
				selectedItem = topItem;

				if (failedResults != null)
					failedResults.ForEach (failedResult => Results.Remove (failedResult));

				ShowTooltip ();

				isInSearch = false;
				AnimatedResize ();
			});
		}

		int calculatedItems;
		Gdk.Size GetIdealSize ()
		{
			Gdk.Size retVal = new Gdk.Size ();
			int ox, oy;
			GetPosition (out ox, out oy);
			Xwt.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen.Number, Screen.GetMonitorAtPoint (ox, oy));
			int maxHeight = (int)geometry.Height * 4 / 5;
			double startY = yMargin + ChildAllocation.Y;
			double y = startY;
			calculatedItems = 0;
			foreach (var result in Results) {
				var dataSrc = result.Item2;
				if (dataSrc.Count == 0)
					continue;
				
				for (int i = 0; i < maxItems && i < dataSrc.Count; i++) {
					layout.SetMarkup (GetRowMarkup (dataSrc[i]));
					int w, h;
					layout.GetPixelSize (out w, out h);
					if (y + h + itemSeparatorHeight + itemPadding * 2 > maxHeight)
						break;
					y += h + itemSeparatorHeight + itemPadding * 2;
					calculatedItems++;
				}
			}
			retVal.Width = Math.Min ((int)geometry.Width * 4 / 5, 480);
			if (Math.Abs (y - startY) < 1) {
				layout.SetMarkup (GettextCatalog.GetString ("No matches"));
				int w, h;
				layout.GetPixelSize (out w, out h);
				var realHeight = h + itemSeparatorHeight + 4 + itemPadding * 2;
				y += realHeight;
			} else {
				y -= itemSeparatorHeight;
			}

			var calculatedHeight = Math.Min (
				maxHeight, 
				(int)y + yMargin + Results.Count (res => res.Item2.Count > 0) * categorySeparatorHeight
			);
			retVal.Height = calculatedHeight;
			return retVal;
		}

		const int maxItems = 8;

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			if (!inResize) {
				Gdk.Size idealSize = GetIdealSize ();
				requisition.Width = idealSize.Width;
				requisition.Height = idealSize.Height;
			}
		}

		ItemIdentifier GetItemAt (double px, double py)
		{
			double y = ChildAllocation.Y + yMargin;
			if (topItem != null){
				layout.SetMarkup (GetRowMarkup (topItem.DataSource[topItem.Item]));
				int w, h;
				layout.GetPixelSize (out w, out h);
				y += h + itemSeparatorHeight + itemPadding * 2;
				if (y > py)
					return new ItemIdentifier (topItem.Category, topItem.DataSource, topItem.Item);
			}
			foreach (var result in Results) {
				var category = result.Item1;
				var dataSrc = result.Item2;
				int itemsAdded = 0;
				for (int i = 0; i < maxItems && i < dataSrc.Count; i++) {
					if (topItem != null && topItem.DataSource == dataSrc && topItem.Item == i)
						continue;
					layout.SetMarkup (GetRowMarkup (dataSrc[i]));

					int w, h;
					layout.GetPixelSize (out w, out h);
					y += h + itemSeparatorHeight + itemPadding * 2;
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

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			var item = GetItemAt (evnt.X, evnt.Y);
			if (item == null && selectedItem != null || 
			    item != null && selectedItem == null || item != null && !item.Equals (selectedItem)) {
				selectedItem = item;
				ShowTooltip ();
				QueueDraw ();
			}
			return base.OnMotionNotifyEvent (evnt);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) {
				if (selectedItem != null)
					OnItemActivated (EventArgs.Empty);
			}

			return base.OnButtonPressEvent (evnt);
		}

		int SelectedCategoryIndex {
			get {
				for (int i = 0; i < Results.Count; i++) {
					if (Results [i].Item1 == selectedItem.Category) {
						return i;
					}
				}
				return -1;
			}
		}

		void SelectItemUp ()
		{
			if (selectedItem == null || selectedItem == topItem)
				return;
			int i = SelectedCategoryIndex;
			if (selectedItem.Item > 0) {
				selectedItem = new ItemIdentifier (selectedItem.Category, selectedItem.DataSource, selectedItem.Item - 1);
				if (i > 0 && selectedItem.Equals (topItem)) {
					SelectItemUp ();
					return;
				}
			} else {
				if (i == 0) {
					selectedItem = topItem;
				} else {
					do {
						i--;
						selectedItem = new ItemIdentifier (
							Results [i].Item1,
							Results [i].Item2,
							Math.Min (maxItems, Results [i].Item2.Count) - 1
						);
						if (selectedItem.Category == topItem.Category && selectedItem.Item == topItem.Item && i > 0) {
							i--;
							selectedItem = new ItemIdentifier (
								Results [i].Item1,
								Results [i].Item2,
								Math.Min (maxItems, Results [i].Item2.Count) - 1
							);
						}
							
					} while (i > 0 && selectedItem.DataSource.Count <= 0);

					if (selectedItem.DataSource.Count <= 0) {
						selectedItem = topItem;
					}
				}
			}
			ShowTooltip ();
			QueueDraw ();
		}

		void SelectItemDown ()
		{
			if (selectedItem == null)
				return;

			if (selectedItem.Equals (topItem)) {
				for (int j = 0; j < Results.Count; j++) {
					if (Results[j].Item2.Count == 0 || Results[j].Item2.Count == 1 && topItem.DataSource == Results[j].Item2)
						continue;
					selectedItem = new ItemIdentifier (
						Results [j].Item1,
						Results [j].Item2,
						0
						);
					if (selectedItem.Equals (topItem))
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
			if (selectedItem != null) {
				var curAbsoluteIndex = selectedItem == topItem ? 1 : 0;
				for (int j = 0; j < i; j++) {
					curAbsoluteIndex += Math.Min (maxItems, Results [j].Item2.Count);
				}
				curAbsoluteIndex += selectedItem.Item + 1;
				if (curAbsoluteIndex + 1 > calculatedItems)
					return;
			}

			var upperBound = Math.Min (maxItems, selectedItem.DataSource.Count);
			if (selectedItem.Item + 1 < upperBound) {
				if (topItem.DataSource == selectedItem.DataSource && selectedItem.Item == upperBound - 1)
					return;
				selectedItem = new ItemIdentifier (selectedItem.Category, selectedItem.DataSource, selectedItem.Item + 1);
			} else {
				for (int j = i + 1; j < Results.Count; j++) {
					if (Results[j].Item2.Count == 0 || Results[j].Item2.Count == 1 && topItem.DataSource == Results[j].Item2)
						continue;
					selectedItem = new ItemIdentifier (
						Results [j].Item1,
						Results [j].Item2,
						0
						);
					if (selectedItem.Equals (topItem)) {
						selectedItem = new ItemIdentifier (
							Results [j].Item1,
							Results [j].Item2,
							1
							);
					}

					break;
				}
			}
			ShowTooltip ();
			QueueDraw ();
		}

		TooltipInformationWindow declarationviewwindow = new TooltipInformationWindow ();
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
			var currentSelectedItem = selectedItem;
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
			
			declarationviewwindow.Clear ();
			declarationviewwindow.AddOverload (currentTooltip);
			declarationviewwindow.CurrentOverload = 0;
			declarationviewwindow.ShowArrow = true;
			var rect = SelectedItemRectangle;
			declarationviewwindow.ShowPopup (this, new Gdk.Rectangle (0, (int)rect.Y - 5, Allocation.Width, (int)rect.Height), PopupPosition.Right);
		}

		void SelectNextCategory ()
		{
			if (selectedItem == null)
				return;
			var i = SelectedCategoryIndex;
			if (selectedItem.Equals (topItem)) {
				if (i > 0) {
					selectedItem = new ItemIdentifier (
						Results [0].Item1,
						Results [0].Item2,
						0
					);

				} else {
					if (topItem.DataSource.Count > 1) {
						selectedItem = new ItemIdentifier (
							Results [0].Item1,
							Results [0].Item2,
							1
						);
					} else if (i < Results.Count - 1) {
						selectedItem = new ItemIdentifier (
							Results [i + 1].Item1,
							Results [i + 1].Item2,
							0
						);
					}
				}
			} else {
				while (i < Results.Count - 1 && Results [i + 1].Item2.Count == 0)
					i++;
				if (i < Results.Count - 1) {
					selectedItem = new ItemIdentifier (
						Results [i + 1].Item1,
						Results [i + 1].Item2,
						0
					);
				}
			}
			ShowTooltip ();
			QueueDraw ();	
		}

		void SelectPrevCategory ()
		{
			if (selectedItem == null)
				return;
			var i = SelectedCategoryIndex;
			if (i > 0) {
				selectedItem = new ItemIdentifier (
					Results [i - 1].Item1,
					Results [i - 1].Item2,
					0
				);
				if (selectedItem.Equals (topItem)) {
					if (topItem.DataSource.Count> 1) {
						selectedItem = new ItemIdentifier (
							Results [i - 1].Item1,
							Results [i - 1].Item2,
							1
						);
					} else if (i > 1) {
						selectedItem = new ItemIdentifier (
							Results [i - 2].Item1,
							Results [i - 2].Item2,
							0
						);
					}
				}
			} else {
				selectedItem = topItem;
			}
			ShowTooltip ();
			QueueDraw ();
		}

		void SelectFirstCategory ()
		{
			selectedItem = topItem;
			ShowTooltip ();
			QueueDraw ();
		}

		void SelectLastCatgory ()
		{
			var r = Results.LastOrDefault (r2 => r2.Item2.Count > 0 && !(r2.Item2.Count == 1 && topItem.Category == r2.Item1));
			if (r == null)
				return;
			selectedItem = new ItemIdentifier (
				r.Item1,
				r.Item2,
				r.Item2.Count - 1
			);
			ShowTooltip ();
			QueueDraw ();
		}

		public bool ProcessKey (Xwt.Key key, Xwt.ModifierKeys state)
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
				if (selectedItem == null || selectedItem.Item < 0 || selectedItem.Item >= selectedItem.DataSource.Count)
					return TextSegment.Invalid;
				return selectedItem.DataSource[selectedItem.Item].Segment;
			}
		}

		public string SelectedItemFileName {
			get {
				if (selectedItem == null || selectedItem.Item < 0 || selectedItem.Item >= selectedItem.DataSource.Count)
					return null;
				return selectedItem.DataSource[selectedItem.Item].File;
			}
		}

		class ItemIdentifier {
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

		ItemIdentifier selectedItem = null, topItem = null;

		Cairo.Rectangle SelectedItemRectangle {
			get {
				if (selectedItem == null)
					return new Cairo.Rectangle (0, 0, Allocation.Width, 16);

				double y = ChildAllocation.Y + yMargin;
				if (topItem != null){
					layout.SetMarkup (GetRowMarkup (topItem.DataSource[topItem.Item]));
					int w, h;
					layout.GetPixelSize (out w, out h);
					if (topItem.Category == selectedItem.Category && topItem.Item == selectedItem.Item)
						return new Cairo.Rectangle (0, y, Allocation.Width, h + itemSeparatorHeight + itemPadding * 2);
					y += h + itemSeparatorHeight + itemPadding * 2;
				}
				foreach (var result in Results) {
					var category = result.Item1;
					var dataSrc = result.Item2;
					int itemsAdded = 0;
					for (int i = 0; i < maxItems && i < dataSrc.Count; i++) {
						if (topItem != null && topItem.DataSource == dataSrc && topItem.Item == i)
							continue;
						layout.SetMarkup (GetRowMarkup (dataSrc[i]));

						int w, h;
						layout.GetPixelSize (out w, out h);

						if (selectedItem.Category == category && selectedItem.DataSource == dataSrc && selectedItem.Item == i)
							return new Cairo.Rectangle (0, y, Allocation.Width, h + itemSeparatorHeight + itemPadding * 2);
						y += h + itemSeparatorHeight + itemPadding * 2;

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
				return new Cairo.Rectangle (0, 0, Allocation.Width, 16);
			}
		}

		public bool IsVisible {
			get {
				return Visible;
			}
		}

		protected override void OnDrawContent (Gdk.EventExpose evnt, Cairo.Context context)
		{
			context.LineWidth = 1;
			var alloc = ChildAllocation;
			var adjustedMarginSize = alloc.X - Allocation.X  + headerMarginSize;

			var r = Results.Where (res => res.Item2.Count > 0).ToArray ();
			if (r.Any ()) {
				context.SetSourceColor (lightSearchBackground);
				context.Rectangle (Allocation.X, Allocation.Y, adjustedMarginSize, Allocation.Height);
				context.Fill ();

				context.SetSourceColor (darkSearchBackground);
				context.Rectangle (Allocation.X + adjustedMarginSize, Allocation.Y, Allocation.Width - adjustedMarginSize, Allocation.Height);
				context.Fill ();
				context.MoveTo (0.5 + Allocation.X + adjustedMarginSize, 0);
				context.LineTo (0.5 + Allocation.X + adjustedMarginSize, Allocation.Height);
				context.SetSourceColor (separatorLine);
				context.Stroke ();
			} else {
				context.SetSourceColor (darkSearchBackground);
				context.Rectangle (Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
				context.Fill ();
			}

			double y = alloc.Y + yMargin;
			int w, h;
			if (topItem != null) {
				headerLayout.SetText (GettextCatalog.GetString ("Top Result"));
				headerLayout.GetPixelSize (out w, out h);
				context.MoveTo (alloc.Left + headerMarginSize - w - xMargin, y + itemPadding);
				context.SetSourceColor (headerColor);
				Pango.CairoHelper.ShowLayout (context, headerLayout);

				var category = topItem.Category;
				var dataSrc = topItem.DataSource;
				var i = topItem.Item;
				var isSelected = selectedItem != null && selectedItem.Category == category && selectedItem.Item == i;

				double x = alloc.X + xMargin + headerMarginSize;
				context.SetSourceRGB (0, 0, 0);
				layout.SetMarkup (GetRowMarkup (dataSrc[i], isSelected));
				layout.GetPixelSize (out w, out h);
				if (isSelected) {
					context.SetSourceColor (selectionBackgroundColor);
					context.Rectangle (alloc.X + headerMarginSize + 1, y, Allocation.Width - adjustedMarginSize - 1, h + itemPadding * 2);
					context.Fill ();
					context.SetSourceRGB (1, 1, 1);
				}

				var px = dataSrc[i].Icon;
				if (px != null) {
					if (isSelected)
						px = px.WithStyles ("sel");
					context.DrawImage (this, px, (int)x + marginIconSpacing, (int)(y + itemPadding));
					x += px.Width + iconTextSpacing + marginIconSpacing;
				}

				context.MoveTo (x, y + itemPadding);
				context.SetSourceRGB (0, 0, 0);
				Pango.CairoHelper.ShowLayout (context, layout);

				y += h + itemSeparatorHeight + itemPadding * 2;

			}

			foreach (var result in r) {
				var category = result.Item1;
				var dataSrc = result.Item2;
				if (dataSrc.Count == 0)
					continue;
				if (dataSrc.Count == 1 && topItem != null && topItem.DataSource == dataSrc)
					continue;
				headerLayout.SetText (category.Name);
				headerLayout.GetPixelSize (out w, out h);

				if (y + h + itemPadding * 2 > Allocation.Height)
					break;

				context.MoveTo (alloc.X + headerMarginSize - w - xMargin, y + itemPadding);
				context.SetSourceColor (headerColor);
				Pango.CairoHelper.ShowLayout (context, headerLayout);

				layout.Width = Pango.Units.FromPixels (Allocation.Width - adjustedMarginSize - 35);

				for (int i = 0; i < maxItems && i < dataSrc.Count; i++) {
					if (topItem != null && topItem.Category == category && topItem.Item == i)
						continue;
					var isSelected = selectedItem != null && selectedItem.Category == category && selectedItem.Item == i;
					double x = alloc.X + xMargin + headerMarginSize;
					context.SetSourceRGB (0, 0, 0);
					layout.SetMarkup (GetRowMarkup (dataSrc[i], isSelected));
					layout.GetPixelSize (out w, out h);
					if (y + h + itemSeparatorHeight + itemPadding * 2 > Allocation.Height)
						break;
					if (isSelected) {
						context.SetSourceColor (selectionBackgroundColor);
						context.Rectangle (alloc.X + headerMarginSize + 1, y, Allocation.Width - adjustedMarginSize - 1, h + itemPadding * 2);
						context.Fill ();
						context.SetSourceRGB (1, 1, 1);
					}

					var px = dataSrc[i].Icon;
					if (px != null) {
						if (isSelected)
							px = px.WithStyles ("sel");
						context.DrawImage (this, px, (int)x + marginIconSpacing, (int)(y + itemPadding));
						x += px.Width + iconTextSpacing + marginIconSpacing;
					}

					context.MoveTo (x, y + itemPadding);
					context.SetSourceRGB (0, 0, 0);
					Pango.CairoHelper.ShowLayout (context, layout);

					y += h + itemSeparatorHeight + itemPadding * 2;
				}
				if (result != r.Last ()) {
					y += categorySeparatorHeight;
				}
			}
			if (y == alloc.Y + yMargin) {
				context.SetSourceColor (Styles.GlobalSearch.ResultTextColor.ToCairoColor ());
				layout.SetMarkup (isInSearch ? GettextCatalog.GetString ("Searching...") : GettextCatalog.GetString ("No matches"));
				context.MoveTo (alloc.X + xMargin, y);
				Pango.CairoHelper.ShowLayout (context, layout);
			}
		}

		string GetRowMarkup (SearchResult result, bool selected = false)
		{
			var resultFgColor = selected ? Styles.GlobalSearch.SelectedResultTextColor : Styles.GlobalSearch.ResultTextColor;
			var descFgColor = selected ? Styles.GlobalSearch.SelectedResultDescriptionTextColor : Styles.GlobalSearch.ResultDescriptionTextColor;
			string txt = "<span foreground=\"" + Styles.ColorGetHex (resultFgColor) + "\">" + result.GetMarkupText(selected) +"</span>";
			string desc = result.GetDescriptionMarkupText ();
			if (!string.IsNullOrEmpty (desc))
				txt += "<span foreground=\"" + Styles.ColorGetHex (descFgColor) + "\" size=\"small\">\n" + desc + "</span>";
			return txt;
		}

		public void ShowResultsDisplay ()
		{
			ShowAll ();
		}

		public void HideResultsDisplay ()
		{
			Hide ();
		}

		public void DestroyResultsDisplay ()
		{
			Destroy ();
		}

		public void UpdateResults (SearchPopupSearchPattern pattern)
		{
			Update (pattern);
		}

		public void PositionResultsDisplay (Widget anchor)
		{
			ShowPopup (anchor, PopupPosition.TopRight);

			var window = anchor.GdkWindow;
			if (window == null) {
				if (IsRealized) {
					Move (anchor.Allocation.Width - Allocation.Width, anchor.Allocation.Y);
				} else {
					Realized += (sender, e) =>
						Move (anchor.Allocation.Width - Allocation.Width, anchor.Allocation.Y);
				}
			}
		}
	}
}

