// 
// SearchPopupWidget.cs
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

namespace MonoDevelop.Components.MainToolbar
{
	public class SearchPopupWidget : Gtk.DrawingArea
	{
		List<SearchCategory> categories = new List<SearchCategory> ();

		List<Tuple<SearchCategory, ISearchDataSource>> results = new List<Tuple<SearchCategory, ISearchDataSource>> ();
		Pango.Layout layout, headerLayout;
		CancellationTokenSource src;
		Cairo.Color headerColor;

		public SearchPopupWidget ()
		{
			headerColor = CairoExtensions.ParseColor ("d4d4d4");


			categories.Add (new ProjectSearchCategory (this));
			layout = new Pango.Layout (PangoContext);
			headerLayout = new Pango.Layout (PangoContext);
		}

		public void Update (string searchPattern)
		{
			if (src != null)
				src.Cancel ();
			src = new CancellationTokenSource ();
			results.Clear ();
			QueueDraw ();
			foreach (var _cat in categories) {
				var cat = _cat;
				var token = src.Token;
				var task = cat.GetResults (searchPattern, token);
				task.ContinueWith (delegate {
					if (token.IsCancellationRequested)
						return;
					Application.Invoke (delegate {
						if (token.IsCancellationRequested)
							return;
						ShowResult (cat, task.Result);
					});
				});
			}
		}

		void ShowResult (SearchCategory cat, ISearchDataSource result)
		{
			results.Add (Tuple.Create (cat, result));
			QueueDraw ();
			QueueResize ();
		}

		const int maxItems = 8;

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			double maxX = 0, y = 0;
				
			foreach (var result in results) {
				var category = result.Item1;
				var dataSrc = result.Item2;
				if (dataSrc.ItemCount == 0)
					continue;
				
				for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
					layout.SetMarkup (dataSrc.GetMarkup (i, false));

					int w, h;
					layout.GetPixelSize (out w, out h);
					y += h;
					maxX = Math.Max (maxX, w);
				}
			}
			requisition.Width = (int)maxX + 100;
			requisition.Height = (int)y + 4;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Color = new Cairo.Color (1, 1, 1);

				context.Rectangle (evnt.Area.X, evnt.Area.Y, evnt.Area.Width, evnt.Area.Height);
				context.Fill ();

				double x = 0, y = 0;
				foreach (var result in results) {
					var category = result.Item1;
					var dataSrc = result.Item2;
					if (dataSrc.ItemCount == 0)
						continue;
					headerLayout.SetText (category.Name);
					context.MoveTo (x, y);
					context.Color = headerColor;
					PangoCairoHelper.ShowLayout (context, headerLayout);

					for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
						context.MoveTo (x + 100, y);
						context.Color = new Cairo.Color (0, 0, 0);
						layout.SetMarkup (dataSrc.GetMarkup (i, false));
						PangoCairoHelper.ShowLayout (context, layout);

						int w, h;
						layout.GetPixelSize (out w, out h);
						y += h;
					}
				}
			}
			return base.OnExposeEvent (evnt);
		}
	}
}

