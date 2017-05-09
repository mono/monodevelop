//
// MDMenuItem.cs
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

#if MAC
using AppKit;
using MonoDevelop.Components.Commands;
using System.Linq;

namespace MonoDevelop.Components.Mac
{
	class MDSubMenuItem : NSMenuItem, IUpdatableMenuItem
	{
		CommandEntrySet ces;

		public MDSubMenuItem (CommandManager manager, CommandEntrySet ces, CommandSource commandSource = CommandSource.MainMenu, object initialCommandTarget = null)
		{
			this.ces = ces;

			this.Submenu = new MDMenu (manager, ces, commandSource, initialCommandTarget);
			this.Title = this.Submenu.Title;
		}

		public void Update (MDMenu parent, ref int index)
		{
			((MDMenu)Submenu).UpdateCommands ();
			if (ces.AutoHide)
				Hidden = Submenu.ItemArray ().All (item => item.Hidden);
			else
				Enabled = Submenu.ItemArray ().Any (item => !item.Hidden);
		}
	}
}
#endif