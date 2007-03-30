//   Jeffrey Stedfast
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;

using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui {
	public abstract class NavigationHistory : Alignment {
		protected MenuToolButton button;
		protected Hashtable navpoints;
		
		public NavigationHistory (string stock_id) : base (0.5f, 0.5f, 1.0f, 0.0f)
		{
			LeftPadding = 3;
			RightPadding = 3;
			
			navpoints = new Hashtable ();
			
			button = new MenuToolButton (stock_id);
			button.Clicked += new EventHandler (ButtonClicked);
			button.Show ();
			Add (button);
			
			NavigationService.HistoryChanged += new EventHandler (HistoryChanged);
		}
		
		protected virtual void Go ()
		{
			throw new NotImplementedException ();
		}
		
		void ButtonClicked (object sender, EventArgs e)
		{
			Go ();
		}
		
		protected void GoTo (object sender, EventArgs e)
		{
			if (!navpoints.Contains (sender))
				return;
			
			INavigationPoint point = (INavigationPoint) navpoints[sender];
			NavigationService.Go (point);
		}
		
		void ClearHistory (object sender, EventArgs e)
		{
			NavigationService.ClearHistory (false);
		}
		
		protected virtual void UpdateHistory ()
		{
			MenuItem item;
			
			item = new SeparatorMenuItem ();
			item.Show ();
			
			((MenuShell) button.Menu).Append (item);
			
			item = new MenuItem ("Clear History");
			item.Activated += new EventHandler (ClearHistory);
			item.Show ();
			
			((MenuShell) button.Menu).Append (item);
		}
		
		void HistoryChanged (object sender, EventArgs e)
		{
			UpdateHistory ();
		}
	}
	
	public class NavigationHistoryBack : NavigationHistory {
		public NavigationHistoryBack () : base ("gtk-go-back")
		{
			UpdateHistory ();
		}
		
		protected override void Go ()
		{
			NavigationService.Go (-1);
		}
		
		protected override void UpdateHistory ()
		{
			Menu menu = new Menu ();
			
			navpoints.Clear ();
			
			if (NavigationService.CanNavigateBack) {
				List<INavigationPoint> points;
				INavigationPoint point;
				MenuItem item;
				int i;
				
				points = new List<INavigationPoint> (NavigationService.Points);
				
				for (i = 0; i < points.Count; i++) {
					point = points[i];
					if (point == NavigationService.CurrentPosition) {
						i--;
						break;
					}
				}
				
				while (i >= 0) {
					point = points[i--];
					item = new MenuItem (point.FullDescription);
					item.Activated += new EventHandler (GoTo);
					navpoints.Add (item, point);
					item.Show ();
					
					((MenuShell) menu).Append (item);
				}
				
				this.Sensitive = true;
			} else {
				this.Sensitive = false;
			}
			
			menu.Show ();
			button.Menu = menu;
			base.UpdateHistory ();
		}
	}
}
