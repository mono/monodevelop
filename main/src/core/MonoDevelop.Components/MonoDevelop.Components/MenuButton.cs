// 
// MenuButton.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using Gtk;

namespace MonoDevelop.Components
{
	
	
	public class MenuButton : Button
	{
		MenuCreator creator;
		Label label;
		
		public MenuButton ()
			: base ()
		{
			HBox box = new HBox();
			box.Spacing = 6;
			Add (box);
			
			label = new Label ();
			box.PackStart (label, false, false, 0);
			Arrow arrow = new Arrow (ArrowType.Down, ShadowType.Out);
			box.PackEnd (arrow, false, false, 0);
			base.Label = null;
		}
		
		protected MenuButton (IntPtr raw)
			: base (raw)
		{
			
		}
		
		public MenuCreator MenuCreator {
			get { return creator; }
			set { creator = value; }
		}
		
		protected override void OnClicked ()
		{
			base.OnClicked ();
			
			if (creator != null) {
				Menu menu = creator (this);
				
				if (menu != null) {
					
					//make sure the button looks depressed
					ReliefStyle oldRelief = this.Relief;
					this.Relief = ReliefStyle.Normal;
					StateChangedHandler h;
					h = delegate {
						if (this.State != StateType.Active)
							this.State = StateType.Active;
					};
					this.StateChanged += h;
					
					//clean up after the menu's done
					menu.Hidden += delegate {
						this.Relief = oldRelief ;
						this.StateChanged -= h;
						this.State = StateType.Normal;
						menu.Destroy ();
					};
					
					menu.Popup (null, null, PositionFunc, 0, Gtk.Global.CurrentEventTime);
				}
			}
			
		}
		
		void PositionFunc (Menu mn, out int x, out int y, out bool push_in)
		{
			this.GdkWindow.GetOrigin (out x, out y);
			Gdk.Rectangle rect = this.Allocation;
			x += rect.X;
			y += rect.Y + rect.Height;
			
			//if the menu would be off the bottom of the screen, "drop" it upwards
			if (y + mn.Requisition.Height > this.Screen.Height) {
				y -= mn.Requisition.Height;
				y -= rect.Height;
			}
			
			//let GTK reposition the button if it still doesn't fit on the screen
			push_in = true;
		}
		
		protected override void OnDestroyed ()
		{
			creator = null;
			base.OnDestroyed ();
		}
		
		public new string Label {
			get { return label.Text; }
			set { label.Text = value; }
		}
	}
	
	public delegate Menu MenuCreator (MenuButton button);
}
