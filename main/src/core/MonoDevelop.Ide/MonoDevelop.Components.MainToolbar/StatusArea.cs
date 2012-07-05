// 
// StatusArea.cs
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
using Gtk;
using MonoDevelop.Components;
using Cairo;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Components.MainToolbar
{
	class StatusArea : EventBox
	{
		HBox contentBox = new HBox (false, 0);

		Color borderColor = Styles.WidgetBorderColor;
		Color fill1Color = CairoExtensions.ParseColor ("eff5f7");
		Color fill2Color = CairoExtensions.ParseColor ("d0d9db");
		Color innerColor = CairoExtensions.ParseColor ("c4cdcf", 0.5);
		Color textColor = CairoExtensions.ParseColor ("3a4029");

		public StatusArea ()
		{
			VisibleWindow = false;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			contentBox.PackStart (MonoDevelopStatusBar.messageBox, true, true, 6);
			contentBox.PackEnd (MonoDevelopStatusBar.statusIconBox, false, false, 6);
			Add (contentBox);

			this.ButtonPressEvent += delegate {
				MonoDevelopStatusBar.HandleEventMessageBoxButtonPressEvent (null, null);
			};

			SetSizeRequest (32, 22);
			Show ();
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			ModifyText (StateType.Normal, textColor.ToGdkColor ());
			ModifyFg (StateType.Normal, textColor.ToGdkColor ());
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Height = 22;
			base.OnSizeRequested (ref requisition);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				CairoExtensions.RoundedRectangle (context, Allocation.X + 0.5, Allocation.Y + 0.5, Allocation.Width - 1, Allocation.Height - 1, 3);
				using (LinearGradient lg = new LinearGradient (Allocation.X, Allocation.Y, Allocation.X, Allocation.Height)) {
					lg.AddColorStop (0, fill1Color);
					lg.AddColorStop (1, fill2Color);
					context.Pattern = lg;
				}
				context.Fill ();

				CairoExtensions.RoundedRectangle (context, Allocation.X + 1.5, Allocation.Y + 1.5, Allocation.Width - 2.5, Allocation.Height - 2.5, 3);
				context.LineWidth = 1;
				context.Color = innerColor;
				context.Stroke ();

				CairoExtensions.RoundedRectangle (context, Allocation.X + 0.5, Allocation.Y + 0.5, Allocation.Width - 1, Allocation.Height - 1, 3);
				context.LineWidth = 1;
				context.Color = borderColor;
				context.Stroke ();
			}
			return base.OnExposeEvent (evnt);
		}

	}
}

