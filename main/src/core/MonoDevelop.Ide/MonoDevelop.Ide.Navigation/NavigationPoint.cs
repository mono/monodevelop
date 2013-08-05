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
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Navigation
{
	public abstract class NavigationPoint: IDisposable
	{
		public abstract string DisplayName { get; }
		//public abstract string Tooltip { get; }

		[Obsolete ("Will be removed. Please use ShowDocument.")]
		public abstract void Show ();

		public virtual Document ShowDocument ()
		{
#pragma warning disable 618
			Show ();
#pragma warning restore 618
			return null;
		}
		
		// used for fuzzy matching to decide whether to replace an existing nav point
		// e.g if user just moves around a little, we don't want to add too many points
		public virtual bool ShouldReplace (NavigationPoint oldPoint)
		{
			return this.Equals (oldPoint);
		}
		
		// To be called by subclass when the navigation point is not valid anymore
		public virtual void Dispose ()
		{
			if (ParentItem != null)
				ParentItem.NotifyDestroyed ();
			if (Destroyed != null)
				Destroyed (this, EventArgs.Empty);
		}
		
		public event EventHandler Destroyed;
		
		public override string ToString ()
		{
			return string.Format ("[NavigationPoint {0}]", DisplayName);
		}
		
		internal NavigationHistoryItem ParentItem;
	}
}
