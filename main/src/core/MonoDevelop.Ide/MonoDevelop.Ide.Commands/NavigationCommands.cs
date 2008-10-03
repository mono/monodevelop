// NavigationCommands.cs
//
// Author:
//   Jeffrey Stedfast  <fejj@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//


using System;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Commands
{
	public enum NavigationCommands
	{
		NavigateBack,
		NavigateForward,
		NavigateHistory,
		ClearNavigationHistory
	}
	
	internal class NavigateBack : CommandHandler
	{
		protected override void Run ()
		{
			NavigationHistoryService.MoveBack ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = NavigationHistoryService.CanMoveBack;
		}
	}
	
	internal class NavigateForward : CommandHandler
	{
		protected override void Run ()
		{
			NavigationHistoryService.MoveForward ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = NavigationHistoryService.CanMoveForward;
		}
	}
	
	internal class NavigateHistory : CommandHandler
	{
		protected override void Run (object ob)
		{
			NavigationPoint nav = ob as NavigationPoint;
			if (nav != null)
				NavigationHistoryService.MoveTo (nav);
		}
		
		protected override void Update (CommandArrayInfo info)
		{
			int currentIndex;
			IList<NavigationPoint> points = NavigationHistoryService.GetNavigationList (15, out currentIndex);
			
			if (points.Count < 1) {
				Document doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) {
					CommandInfo item = info.Add (doc.Window.Title, null);
					item.Checked = true;
				}
				return;
			}
			
			for (int i = points.Count - 1; i >= 0; i--) {
				CommandInfo item = info.Add (points[i].DisplayName, points[i]);
				item.Checked = (i == currentIndex);
			}
		}
	}
	
	internal class ClearNavigationHistory : CommandHandler
	{
		protected override void Run ()
		{
			NavigationHistoryService.Clear ();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = NavigationHistoryService.CanMoveForward || NavigationHistoryService.CanMoveBack;
		}
	}
}
