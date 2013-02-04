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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.Components.MainToolbar
{
	class SearchPopupWindow : PopoverWindow
	{
		const int yMargin = 0;
		const int xMargin = 6;
		const int itemSeparatorHeight = 2;
		const int marginIconSpacing = 4;
		const int iconTextSpacing = 6;
		const int categorySeparatorHeight = 8;
		const int headerMarginSize = 100;

		List<SearchCategory> categories = new List<SearchCategory> ();
		List<Tuple<SearchCategory, ISearchDataSource>> results = new List<Tuple<SearchCategory, ISearchDataSource>> ();
		List<Tuple<SearchCategory, ISearchDataSource>> incompleteResults = new List<Tuple<SearchCategory, ISearchDataSource>> ();
		Pango.Layout layout, headerLayout;
		CancellationTokenSource src;
		Cairo.Color headerColor;
		Cairo.Color separatorLine;
		Cairo.Color darkSearchBackground;
		Cairo.Color lightSearchBackground;

		Cairo.Color selectionBackgroundColor;

		bool isInSearch;
		class NullDataSource : ISearchDataSource
		{
			#region ISearchDataSource implementation
			Gdk.Pixbuf ISearchDataSource.GetIcon (int item)
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
			TooltipInformation ISearchDataSource.GetTooltip (int item)
			{
				throw new NotImplementedException ();
			}
			double ISearchDataSource.GetWeight (int item)
			{
				throw new NotImplementedException ();
			}
			DomRegion ISearchDataSource.GetRegion (int item)
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
			headerColor = CairoExtensions.ParseColor ("8c8c8c");
			separatorLine = CairoExtensions.ParseColor ("dedede");
			lightSearchBackground = CairoExtensions.ParseColor ("ffffff");
			darkSearchBackground = CairoExtensions.ParseColor ("f7f7f7");
			selectionBackgroundColor = CairoExtensions.ParseColor ("cccccc");
			TypeHint = Gdk.WindowTypeHint.Combo;
			this.SkipTaskbarHint = true;
			this.SkipPagerHint = true;
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.AllowShrink = false;
			this.AllowGrow = false;

			categories.Add (new ProjectSearchCategory (this));
			categories.Add (new FileSearchCategory (this));
			categories.Add (new CommandSearchCategory (this));
			layout = new Pango.Layout (PangoContext);
			headerLayout = new Pango.Layout (PangoContext);

			layout.Ellipsize = Pango.EllipsizeMode.End;
			headerLayout.Ellipsize = Pango.EllipsizeMode.End;

			Events = Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonMotionMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.ExposureMask | Gdk.EventMask.PointerMotionMask;
			ItemActivated += (sender, e) => OpenFile ();
			SizeRequested += delegate(object o, SizeRequestedArgs args) {
				if (inResize)
					return;
				if (args.Requisition.Width != Allocation.Width || args.Requisition.Height != Allocation.Height) {
					inResize = true;
//					Visible = false;
					Resize (args.Requisition.Width, args.Requisition.Height);
//					Visible = true;
					if (!Visible)
						Visible = true;
					inResize = false;
				}
			};
		}
		bool inResize = false;

		public bool SearchForMembers {
			get;
			set;
		}

		protected override void OnDestroyed ()
		{
			if (src != null)
				src.Cancel ();
			HideTooltip ();
			this.declarationviewwindow.Destroy ();
			base.OnDestroyed ();
		}

		internal void OpenFile ()
		{
			if (selectedItem == null || selectedItem.Item < 0 || selectedItem.Item >= selectedItem.DataSource.ItemCount)
				return;

			if (selectedItem.DataSource.CanActivate (selectedItem.Item)) {
				Destroy ();
				selectedItem.DataSource.Activate (selectedItem.Item);
			}
			else {
				var region = SelectedItemRegion;
				Destroy ();

				if (string.IsNullOrEmpty (region.FileName))
					return;
				if (region.Begin.IsEmpty) {
					if (Pattern.LineNumber == 0) {
						IdeApp.Workbench.OpenDocument (region.FileName);
					} else {
						IdeApp.Workbench.OpenDocument (region.FileName, Pattern.LineNumber, 1);
					}
				} else {
					IdeApp.Workbench.OpenDocument (region.FileName, region.BeginLine, region.BeginColumn);
				}
			}
		}
		SearchPopupSearchPattern pattern;

		public SearchPopupSearchPattern Pattern {
			get {
				return pattern;
			}
		}

		public void Update (string searchPattern)
		{
			if (src != null)
				src.Cancel ();

			HideTooltip ();
			src = new CancellationTokenSource ();
			isInSearch = true;
			if (results.Count == 0) {
				QueueDraw ();
			}
			pattern = SearchPopupSearchPattern.ParsePattern (searchPattern);
			incompleteResults.Clear ();
			foreach (var _cat in categories) {
				var cat = _cat;
				var token = src.Token;
				cat.GetResults (pattern, maxItems, token).ContinueWith (t => {
					if (t.IsFaulted) {
						LoggingService.LogError ("Error getting search results", t.Exception);
					} else {
						Application.Invoke (delegate {
							ShowResult (cat, t.Result ?? new NullDataSource ());
						});
					}
				}, token);
			}
		}

		void ShowResult (SearchCategory cat, ISearchDataSource result)
		{
			incompleteResults.Add (Tuple.Create (cat, result));

			incompleteResults.Sort ((x, y) => {
				return categories.IndexOf (x.Item1).CompareTo (categories.IndexOf (y.Item1));
			}
			);

			if (incompleteResults.Count == categories.Count) {
				results.Clear ();
				results.AddRange (incompleteResults);
				topItem = null;
				for (int i = 0; i < results.Count; i++) {
					if (results[i].Item2.ItemCount == 0)
						continue;
					if (topItem == null || topItem.DataSource.GetWeight (topItem.Item) <  results[i].Item2.GetWeight (0)) 
						topItem = new ItemIdentifier (results[i].Item1, results[i].Item2, 0);
				}
				selectedItem = topItem;

				ShowTooltip ();
				isInSearch = false;
				AnimatedResize ();
			}
		}

		Gdk.Size GetIdealSize ()
		{
			Gdk.Size retVal = new Gdk.Size ();
			int ox, oy;
			GetPosition (out ox, out oy);
			Gdk.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtPoint (ox, oy));

			double startY = yMargin + ChildAllocation.Y;
			double y = startY;
			
			foreach (var result in results) {
				//				var category = result.Item1;
				var dataSrc = result.Item2;
				if (dataSrc.ItemCount == 0)
					continue;
				
				for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
					layout.SetMarkup (GetRowMarkup (dataSrc, i));
					int w, h;
					layout.GetPixelSize (out w, out h);
					y += h + itemSeparatorHeight;
				}
			}
			retVal.Width = Math.Min (geometry.Width * 4 / 5, 480);
			if (y == startY) {
				layout.SetMarkup (GettextCatalog.GetString ("No matches"));
				int w, h;
				layout.GetPixelSize (out w, out h);
				y += h + itemSeparatorHeight + 4;
			} else {
				y -= itemSeparatorHeight;
			}
			
			var calculedHeight = Math.Min (geometry.Height * 4 / 5, (int)y + yMargin + results.Count (res => res.Item2.ItemCount > 0) * categorySeparatorHeight);
			retVal.Height = calculedHeight;
			return retVal;
		}

		const int maxItems = 8;

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			Gdk.Size idealSize = GetIdealSize ();
			requisition.Width += idealSize.Width;
			requisition.Height += idealSize.Height;
		}

		ItemIdentifier GetItemAt (double px, double py)
		{
			double y = ChildAllocation.Y + yMargin;
			if (topItem != null){
				layout.SetMarkup (GetRowMarkup (topItem.DataSource, topItem.Item));
				int w, h;
				layout.GetPixelSize (out w, out h);
				y += h + itemSeparatorHeight;
				if (y > py)
					return new ItemIdentifier (topItem.Category, topItem.DataSource, topItem.Item);
			}
			foreach (var result in results) {
				var category = result.Item1;
				var dataSrc = result.Item2;
				int itemsAdded = 0;
				for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
					if (topItem != null && topItem.DataSource == dataSrc && topItem.Item == i)
						continue;
					layout.SetMarkup (GetRowMarkup (dataSrc, i));

					int w, h;
					layout.GetPixelSize (out w, out h);
					y += h + itemSeparatorHeight;
					if (y > py){
						return new ItemIdentifier (category, dataSrc, i);
					}

					var region = dataSrc.GetRegion (i);
					if (!region.Begin.IsEmpty) {
						layout.SetMarkup (region.BeginLine.ToString ());
						int w2, h2;
						layout.GetPixelSize (out w2, out h2);
						w += w2;
					}
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
				for (int i = 0; i < results.Count; i++) {
					if (results [i].Item1 == selectedItem.Category) {
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
							results [i].Item1,
							results [i].Item2,
							Math.Min (maxItems, results [i].Item2.ItemCount) - 1
						);
						if (selectedItem.Category == topItem.Category && selectedItem.Item == topItem.Item && i > 0) {
							i--;
							selectedItem = new ItemIdentifier (
								results [i].Item1,
								results [i].Item2,
								Math.Min (maxItems, results [i].Item2.ItemCount) - 1
							);
						}
							
					} while (i > 0 && selectedItem.DataSource.ItemCount <= 0);

					if (selectedItem.DataSource.ItemCount <= 0) {
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
				for (int j = 0; j < results.Count; j++) {
					if (results[j].Item2.ItemCount == 0 || results[j].Item2.ItemCount == 1 && topItem.DataSource == results[j].Item2)
						continue;
					selectedItem = new ItemIdentifier (
						results [j].Item1,
						results [j].Item2,
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
			var upperBound = Math.Min (maxItems, selectedItem.DataSource.ItemCount);
			if (selectedItem.Item + 1 < upperBound) {
				if (topItem.DataSource == selectedItem.DataSource && selectedItem.Item == upperBound - 1)
					return;
				selectedItem = new ItemIdentifier (selectedItem.Category, selectedItem.DataSource, selectedItem.Item + 1);
			} else {
				for (int j = i + 1; j < results.Count; j++) {
					if (results[j].Item2.ItemCount == 0 || results[j].Item2.ItemCount == 1 && topItem.DataSource == results[j].Item2)
						continue;
					selectedItem = new ItemIdentifier (
						results [j].Item1,
						results [j].Item2,
						0
						);
					if (selectedItem.Equals (topItem)) {
						selectedItem = new ItemIdentifier (
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

		TooltipInformationWindow declarationviewwindow = new TooltipInformationWindow ();
		uint declarationViewTimer, declarationViewWindowOpacityTimer;
		void RemoveDeclarationViewTimer ()
		{
			if (declarationViewWindowOpacityTimer != 0) {
				GLib.Source.Remove (declarationViewWindowOpacityTimer);
				declarationViewWindowOpacityTimer = 0;
			}
			if (declarationViewTimer != 0) {
				GLib.Source.Remove (declarationViewTimer);
				declarationViewTimer = 0;
			}
		}

		void HideTooltip ()
		{
			RemoveDeclarationViewTimer ();
			if (declarationviewwindow != null) {
				declarationviewwindow.Hide ();
				declarationviewwindow.Opacity = 0;
			}
			if (tooltipSrc != null)
				tooltipSrc.Cancel ();
		}

		CancellationTokenSource tooltipSrc = null;
		void ShowTooltip ()
		{
			HideTooltip ();
			var currentSelectedItem = selectedItem;
			if (currentSelectedItem == null || currentSelectedItem.DataSource == null)
				return;
			var i = currentSelectedItem.Item;
			if (i < 0 || i >= currentSelectedItem.DataSource.ItemCount)
				return;

			if (tooltipSrc != null)
				tooltipSrc.Cancel ();
			tooltipSrc = new CancellationTokenSource ();
			var token = tooltipSrc.Token;

			ThreadPool.QueueUserWorkItem (delegate {
				var tooltip = currentSelectedItem.DataSource.GetTooltip (i);
				if (tooltip == null || string.IsNullOrEmpty (tooltip.SignatureMarkup) || token.IsCancellationRequested)
					return;
				Application.Invoke (delegate {
					if (token.IsCancellationRequested)
						return;
					declarationviewwindow.Clear ();
					declarationviewwindow.AddOverload (tooltip);
					declarationviewwindow.CurrentOverload = 0;
					declarationViewTimer = GLib.Timeout.Add (250, DelayedTooltipShow);
				});
			});

		}

		bool DelayedTooltipShow ()
		{
			declarationviewwindow.ShowArrow = true;
			var rect = SelectedItemRectangle;

			declarationviewwindow.ShowPopup (this, new Gdk.Rectangle (0, (int)rect.Y, Allocation.Width, (int)rect.Height), PopupPosition.Right);
			if (declarationViewWindowOpacityTimer != 0) 
				GLib.Source.Remove (declarationViewWindowOpacityTimer);
			declarationViewWindowOpacityTimer = GLib.Timeout.Add (50, new OpacityTimer (this).Timer);
			declarationViewTimer = 0;
			return false;
		}

		class OpacityTimer
		{
			public double Opacity { get; private set; }
			
			SearchPopupWindow window;
			//			static int num = 0;
			//			int id;
			public OpacityTimer (SearchPopupWindow window)
			{
				//				id = num++;
				this.window = window;
				Opacity = 0.0;
				window.declarationviewwindow.Opacity = Opacity;
			}
			
			public bool Timer ()
			{
				Opacity = System.Math.Min (1.0, Opacity + 0.33);
				window.declarationviewwindow.Opacity = Opacity;
				bool result = Math.Round (Opacity * 10.0) < 10;
				if (!result)
					window.declarationViewWindowOpacityTimer = 0;
				return result;
			}
		}



		void SelectNextCategory ()
		{
			if (selectedItem == null)
				return;
			var i = SelectedCategoryIndex;
			if (selectedItem.Equals (topItem)) {
				if (i > 0) {
					selectedItem = new ItemIdentifier (
						results [0].Item1,
						results [0].Item2,
						0
					);

				} else {
					if (topItem.DataSource.ItemCount > 1) {
						selectedItem = new ItemIdentifier (
							results [0].Item1,
							results [0].Item2,
							1
						);
					} else if (i < results.Count - 1) {
						selectedItem = new ItemIdentifier (
							results [i + 1].Item1,
							results [i + 1].Item2,
							0
						);
					}
				}
			} else {
				while (i < results.Count - 1 && results [i + 1].Item2.ItemCount == 0)
					i++;
				if (i < results.Count - 1) {
					selectedItem = new ItemIdentifier (
						results [i + 1].Item1,
						results [i + 1].Item2,
						0
					);
				}
			}
			QueueDraw ();	
		}

		void SelectPrevCategory ()
		{
			if (selectedItem == null)
				return;
			var i = SelectedCategoryIndex;
			if (i > 0) {
				selectedItem = new ItemIdentifier (
					results [i - 1].Item1,
					results [i - 1].Item2,
					0
				);
				if (selectedItem.Equals (topItem)) {
					if (topItem.DataSource.ItemCount > 1) {
						selectedItem = new ItemIdentifier (
							results [i - 1].Item1,
							results [i - 1].Item2,
							1
						);
					} else if (i > 1) {
						selectedItem = new ItemIdentifier (
							results [i - 2].Item1,
							results [i - 2].Item2,
							0
						);
					}
				}
			} else {
				selectedItem = topItem;
			}
			QueueDraw ();
		}

		void SelectFirstCategory ()
		{
			selectedItem = topItem;
			QueueDraw ();
		}

		void SelectLastCatgory ()
		{
			var r = results.LastOrDefault (r2 => r2.Item2.ItemCount > 0 && !(r2.Item2.ItemCount == 1 && topItem.Category == r2.Item1));
			if (r == null)
				return;
			selectedItem = new ItemIdentifier (
				r.Item1,
				r.Item2,
				r.Item2.ItemCount - 1
			);
			QueueDraw ();
		}

		internal bool ProcessKey (Gdk.Key key, Gdk.ModifierType state)
		{
			switch (key) {
			case Gdk.Key.Up:
				if (state.HasFlag (Gdk.ModifierType.Mod2Mask))
					goto case Gdk.Key.Page_Up;
				if (state.HasFlag (Gdk.ModifierType.ControlMask))
					goto case Gdk.Key.Home;
				SelectItemUp ();
				return true;
			case Gdk.Key.Down:
				if (state.HasFlag (Gdk.ModifierType.Mod2Mask))
					goto case Gdk.Key.Page_Down;
				if (state.HasFlag (Gdk.ModifierType.ControlMask))
					goto case Gdk.Key.End;
				SelectItemDown ();
				return true;
			case Gdk.Key.KP_Page_Down:
			case Gdk.Key.Page_Down:
				SelectNextCategory ();
				return true;
			case Gdk.Key.KP_Page_Up:
			case Gdk.Key.Page_Up:
				SelectPrevCategory ();
				return true;
			case Gdk.Key.Home:
				SelectFirstCategory ();
				return true;
			case Gdk.Key.End:
				SelectLastCatgory ();
				return true;
			
			case Gdk.Key.Return:
				OnItemActivated (EventArgs.Empty);
				return true;
			}
			return true;
		}

		public event EventHandler ItemActivated;

		protected virtual void OnItemActivated (EventArgs e)
		{
			EventHandler handler = this.ItemActivated;
			if (handler != null)
				handler (this, e);
		}

		public DomRegion SelectedItemRegion {
			get {
				if (selectedItem == null || selectedItem.Item < 0 || selectedItem.Item >= selectedItem.DataSource.ItemCount)
					return DomRegion.Empty;
				return selectedItem.DataSource.GetRegion (selectedItem.Item);
			}
		}

		class ItemIdentifier {
			public SearchCategory Category { get; private set; }
			public ISearchDataSource DataSource { get; private set; }
			public int Item { get; private set; }

			public ItemIdentifier (SearchCategory category, ISearchDataSource dataSource, int item)
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
				return string.Format ("[ItemIdentifier: Category={0}, DataSource=#{1}, Item={2}]", Category.Name, DataSource.ItemCount, Item);
			}
		}

		ItemIdentifier selectedItem = null, topItem = null;

		Cairo.Rectangle SelectedItemRectangle {
			get {
				if (selectedItem == null)
					return new Cairo.Rectangle (0, 0, Allocation.Width, 16);

				double y = ChildAllocation.Y + yMargin;
				if (topItem != null){
					layout.SetMarkup (GetRowMarkup (topItem.DataSource, topItem.Item));
					int w, h;
					layout.GetPixelSize (out w, out h);
					if (topItem.Category == selectedItem.Category && topItem.Item == selectedItem.Item)
						return new Cairo.Rectangle (0, y, Allocation.Width, h + itemSeparatorHeight);
					y += h + itemSeparatorHeight;
				}
				foreach (var result in results) {
					var category = result.Item1;
					var dataSrc = result.Item2;
					int itemsAdded = 0;
					for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
						if (topItem != null && topItem.DataSource == dataSrc && topItem.Item == i)
							continue;
						layout.SetMarkup (GetRowMarkup (dataSrc, i));

						int w, h;
						layout.GetPixelSize (out w, out h);

						if (selectedItem.Category == category && selectedItem.DataSource == dataSrc && selectedItem.Item == i)
							return new Cairo.Rectangle (0, y, Allocation.Width, h + itemSeparatorHeight);
						y += h + itemSeparatorHeight;

						var region = dataSrc.GetRegion (i);
						if (!region.Begin.IsEmpty) {
							layout.SetMarkup (region.BeginLine.ToString ());
							int w2, h2;
							layout.GetPixelSize (out w2, out h2);
							w += w2;
						}
						itemsAdded++;
					}
					if (itemsAdded > 0)
						y += categorySeparatorHeight;
				}
				return new Cairo.Rectangle (0, 0, Allocation.Width, 16);
			}
		}

		protected override void OnDrawContent (Gdk.EventExpose evnt, Cairo.Context context)
		{
			context.LineWidth = 1;
			var alloc = ChildAllocation;
			var adjustedMarginSize = alloc.X - Allocation.X  + headerMarginSize;

			var r = results.Where (res => res.Item2.ItemCount > 0).ToArray ();
			if (r.Any ()) {
				context.Color = lightSearchBackground;
				context.Rectangle (Allocation.X, Allocation.Y, adjustedMarginSize, Allocation.Height);
				context.Fill ();

				context.Color = darkSearchBackground;
				context.Rectangle (Allocation.X + adjustedMarginSize, Allocation.Y, Allocation.Width - adjustedMarginSize, Allocation.Height);
				context.Fill ();
				context.MoveTo (0.5 + Allocation.X + adjustedMarginSize, 0);
				context.LineTo (0.5 + Allocation.X + adjustedMarginSize, Allocation.Height);
				context.Color = separatorLine;
				context.Stroke ();
			} else {
				context.Color = new Cairo.Color (1, 1, 1);
				context.Rectangle (Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
				context.Fill ();
			}

			double y = alloc.Y + yMargin;
			int w, h;
			if (topItem != null) {
				headerLayout.SetText (GettextCatalog.GetString ("Top Result"));
				headerLayout.GetPixelSize (out w, out h);
				context.MoveTo (alloc.Left + headerMarginSize - w - xMargin, y);
				context.Color = headerColor;
				Pango.CairoHelper.ShowLayout (context, headerLayout);

				var category = topItem.Category;
				var dataSrc = topItem.DataSource;
				var i = topItem.Item;

				double x = alloc.X + xMargin + headerMarginSize;
				context.Color = new Cairo.Color (0, 0, 0);
				layout.SetMarkup (GetRowMarkup (dataSrc, i));
				layout.GetPixelSize (out w, out h);
				if (selectedItem != null && selectedItem.Category == category && selectedItem.Item == i) {
					context.Color = selectionBackgroundColor;
					context.Rectangle (alloc.X + headerMarginSize, y, Allocation.Width - adjustedMarginSize, h);
					context.Fill ();
					context.Color = new Cairo.Color (1, 1, 1);
				}

				var px = dataSrc.GetIcon (i);
				if (px != null) {
					evnt.Window.DrawPixbuf (Style.WhiteGC, px, 0, 0, (int)x + marginIconSpacing, (int)y + (h - px.Height) / 2, px.Width, px.Height, Gdk.RgbDither.None, 0, 0);
					x += px.Width + iconTextSpacing + marginIconSpacing;
				}

				context.MoveTo (x, y);
				Pango.CairoHelper.ShowLayout (context, layout);

				y += h + itemSeparatorHeight;

			}

			foreach (var result in r) {
				var category = result.Item1;
				var dataSrc = result.Item2;
				if (dataSrc.ItemCount == 0)
					continue;
				if (dataSrc.ItemCount == 1 && topItem != null && topItem.DataSource == dataSrc)
					continue;
				headerLayout.SetText (category.Name);
				headerLayout.GetPixelSize (out w, out h);
				context.MoveTo (alloc.X + headerMarginSize - w - xMargin, y);
				context.Color = headerColor;
				Pango.CairoHelper.ShowLayout (context, headerLayout);

				layout.Width = Pango.Units.FromPixels (Allocation.Width - adjustedMarginSize - 35);

				for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
					if (topItem != null && topItem.Category == category && topItem.Item == i)
						continue;
					double x = alloc.X + xMargin + headerMarginSize;
					context.Color = new Cairo.Color (0, 0, 0);
					layout.SetMarkup (GetRowMarkup (dataSrc, i));
					layout.GetPixelSize (out w, out h);
					if (selectedItem != null && selectedItem.Category == category && selectedItem.Item == i) {
						context.Color = selectionBackgroundColor;
						context.Rectangle (alloc.X + headerMarginSize, y, Allocation.Width - adjustedMarginSize, h);
						context.Fill ();
						context.Color = new Cairo.Color (1, 1, 1);
					}

					var px = dataSrc.GetIcon (i);
					if (px != null) {
						evnt.Window.DrawPixbuf (Style.WhiteGC, px, 0, 0, (int)x + marginIconSpacing, (int)y + (h - px.Height) / 2, px.Width, px.Height, Gdk.RgbDither.None, 0, 0);
						x += px.Width + iconTextSpacing + marginIconSpacing;
					}

					context.MoveTo (x, y);
					Pango.CairoHelper.ShowLayout (context, layout);

					y += h + itemSeparatorHeight;
				}
				if (result != r.Last ()) {
/*						context.MoveTo (alloc.X, y + categorySeparatorHeight / 2 + 0.5);
					context.LineTo (alloc.X + alloc.Width, y + categorySeparatorHeight / 2 + 0.5);
					context.Color = (HslColor)Style.Mid (StateType.Normal);
					context.Stroke ();*/
					y += categorySeparatorHeight;
				}
			}
			if (y == alloc.Y + yMargin) {
				context.Color = new Cairo.Color (0, 0, 0);
				layout.SetMarkup (isInSearch ? GettextCatalog.GetString ("Searching...") : GettextCatalog.GetString ("No matches"));
				context.MoveTo (alloc.X + xMargin, y);
				Pango.CairoHelper.ShowLayout (context, layout);
			}
		}

		string GetRowMarkup (ISearchDataSource dataSrc, int i)
		{
			string txt = "<span foreground=\"#606060\">" + dataSrc.GetMarkup (i, false) +"</span>";
			string desc = dataSrc.GetDescriptionMarkup (i, false);
			if (!string.IsNullOrEmpty (desc))
				txt += "<span foreground=\"#8F8F8F\" size=\"small\">\n" + desc + "</span>";
			return txt;
		}
	}
}

