//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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
//

using Gtk;
using System;
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Database.Components
{
	public partial class WaitDialog : Window
	{
		private static WaitDialog dlg;
		private static bool isRunning;
		
		protected WaitDialog () : 
			base (WindowType.Popup)
		{
			this.KeepAbove = true;
			this.Build();
		}
		
		public static void ShowDialog ()
		{
			ShowDialog (null);
		}
		
		public static void ShowDialog (string text)
		{
			if (dlg == null)
				dlg = new WaitDialog ();
			
			if (text == null)
				dlg.label.Markup = GettextCatalog.GetString ("Please Wait");
			else
				dlg.label.Markup = text;

			dlg.ShowAll ();
			if (!isRunning) {
				isRunning = true;
				ThreadPool.QueueUserWorkItem (new WaitCallback (ProgressUpdate));
			} else {
				dlg.Present ();
			}
		}
		
		private static void ProgressUpdate (object state)
		{
			while (isRunning) {
			DispatchService.GuiDispatch (delegate () {
					dlg.progressbar.Pulse ();
				});
				Thread.Sleep (1000);
			}
		}
		
		public static void HideDialog ()
		{
			if (dlg == null)
				return;
			
			isRunning = false;
			dlg.Destroy ();
			dlg = null;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);
			Requisition req = SizeRequest ();
			Style.PaintFlatBox (this.Style, this.GdkWindow, StateType.Normal, ShadowType.Out, Gdk.Rectangle.Zero, this, "tooltip", 0, 0, req.Width, req.Height);
			return true;
		}
	}
}
