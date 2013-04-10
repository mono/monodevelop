//
// MDServicesMenuItem.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

using MonoMac.AppKit;
using MonoDevelop.Core;

namespace MonoDevelop.MacIntegration.MacMenu
{
	class MDServicesMenuItem : NSMenuItem, IUpdatableMenuItem
	{
		public MDServicesMenuItem ()
		{
			Title = GettextCatalog.GetString ("Services");
			var sub = NSApplication.SharedApplication.ServicesMenu;
			if (sub == null) {
				sub = new NSMenu ();
				NSApplication.SharedApplication.ServicesMenu = sub;
			} else {
				foreach (var m in sub.Supermenu.ItemArray ()) {
					if (m.Submenu == sub) {
						m.Submenu = new NSMenu ();
						break;
					}
				}
			}
			Submenu = sub;
		}

		public void Update (MDMenu parent, ref NSMenuItem lastSeparator, ref int index)
		{
			Enabled = true;
			Hidden = Submenu != NSApplication.SharedApplication.ServicesMenu;
			if (!Hidden)
				MDMenu.ShowLastSeparator (ref lastSeparator);
		}
	}
}
