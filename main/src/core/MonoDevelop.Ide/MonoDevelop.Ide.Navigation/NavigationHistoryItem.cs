// 
// NavigationHistoryService.cs
//  
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//   Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Navigation
{
	public class NavigationHistoryItem: IDisposable
	{
		DateTime created = DateTime.Now;
		NavigationPoint navPoint;
		
		internal NavigationHistoryItem (NavigationPoint navPoint)
		{
			this.navPoint = navPoint;
		}
		
		public void Dispose ()
		{
			navPoint.Dispose ();
		}

		public void Show ()
		{
			if (!NavigationHistoryService.IsCurrent (this))
				NavigationHistoryService.MoveTo (this);
			NavigationPoint.Show ();
		}
		
		public string DisplayName {
			get { return navPoint.DisplayName; }
		}
		
		public DateTime Created {
			get { return created; }
		}
		
		internal DateTime Visited { get; private set; }
		
		internal void SetVisited ()
		{
			Visited = DateTime.Now;
		}
		
		internal NavigationPoint NavigationPoint {
			get { return navPoint; }
		}
		
		public event EventHandler Destroyed {
			add { navPoint.Destroyed += value; }
			remove { navPoint.Destroyed -= value; }
		}
	}
}
