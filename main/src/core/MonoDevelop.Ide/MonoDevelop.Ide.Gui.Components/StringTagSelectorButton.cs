// 
// StringTagSelectorButton.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.StringParsing;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.Gui.Components
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class StringTagSelectorButton : Gtk.Bin
	{
		bool isOpen;
		
		public StringTagSelectorButton ()
		{
			this.Build ();

			Accessible.SetShouldIgnore (true);
		}

		public StringTagModelDescription TagModel { get; set; }
		
		public Entry TargetEntry { get; set; }
		
		void InsertTag (string tag)
		{
			if (TargetEntry != null) {
				int tempInt = TargetEntry.Position;
				TargetEntry.DeleteSelection();
				TargetEntry.InsertText ("${" + tag + "}", ref tempInt);
			}
		}
		
		Menu CreateMenu (bool importantOnly)
		{
			if (TagModel != null) {
				Menu menu = new Menu ();
				
				bool itemsAdded = false;
				
				foreach (StringTagDescription[] tags in TagModel.GetTagsGrouped ()) {
					if (itemsAdded) {
						SeparatorMenuItem sep = new SeparatorMenuItem ();
						menu.Add (sep);
					}
					foreach (StringTagDescription tag in tags) {
						if (tag.Important != importantOnly)
							continue;
						MenuItem item = new MenuItem (tag.Description);
						string tagString = tag.Name;
						item.ButtonPressEvent += delegate {
							InsertTag (tagString);
						};
						menu.Add (item);
						itemsAdded = true;
					}
				}
				if (importantOnly) {
					Menu subMenu = CreateMenu (false);
					if (subMenu.Children.Length > 0) {
						SeparatorMenuItem sep = new SeparatorMenuItem ();
						menu.Add (sep);
						MenuItem item = new MenuItem (GettextCatalog.GetString ("More"));
						item.Submenu = subMenu;
						menu.Add (item);
					}
				}
				menu.ShowAll ();
				return menu;
			}
			return null;
		}
		
		protected virtual void OnButtonClicked (object sender, System.EventArgs e)
		{
			Menu menu = CreateMenu (true);
			
			if (menu != null) {
				isOpen = true;

				//make sure the button looks depressed
				ReliefStyle oldRelief = button.Relief;
				button.Relief = ReliefStyle.Normal;
				
				//clean up after the menu's done
				menu.Hidden += delegate {
					button.Relief = oldRelief ;
					isOpen = false;
					button.State = StateType.Normal;
					
					//FIXME: for some reason the menu's children don't get activated if we destroy 
					//directly here, so use a timeout to delay it
					GLib.Timeout.Add (100, delegate {
						menu.Destroy ();
						return false;
					});
				};
				menu.Popup (null, null, PositionFunc, 0, Gtk.Global.CurrentEventTime);
			}
		}
		
		protected override void OnStateChanged(StateType previous_state)
		{
			base.OnStateChanged (previous_state);
			
			//while the menu's open, make sure the button looks depressed
			if (isOpen && button.State != StateType.Active)
				button.State = StateType.Active;
		}
		
		void PositionFunc (Menu mn, out int x, out int y, out bool push_in)
		{
			button.GdkWindow.GetOrigin (out x, out y);
			Gdk.Rectangle rect = button.Allocation;
			x += rect.X;
			y += rect.Y + rect.Height;
			
			//if the menu would be off the bottom of the screen, "drop" it upwards
			if (y + mn.Requisition.Height > button.Screen.Height) {
				y -= mn.Requisition.Height;
				y -= rect.Height;
			}
			
			//let GTK reposition the button if it still doesn't fit on the screen
			push_in = true;
		}

		public Atk.Object ButtonAccessible {
			get {
				return button.Accessible;
			}
		}
	}
}

