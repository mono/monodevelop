//
// CommandRouterContainer.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Components.Commands
{
	/// <summary>
	/// A container which can be used to redirect the command
	/// route to a different parent
	/// </summary>
	public class CommandRouterContainer: Gtk.HBox, ICommandDelegatorRouter
	{
		bool continueToParent;
		
		protected object Delegated {
			get;
			set;
		}
		
		protected CommandRouterContainer (bool continueToParent)
		{
			this.continueToParent = continueToParent;
		}

		public CommandRouterContainer (Control child, object target, bool continueToParent) : this (continueToParent)
		{
			Accessible.SetShouldIgnore (true);
			if (child != null) {
				PackStart (child, true, true, 0);
				child = null;
			}
			Delegated = target;
		}
		
		public virtual object GetNextCommandTarget ()
		{
			return continueToParent ? Parent : null;
		}
		
		public virtual object GetDelegatedCommandTarget ()
		{
			return Delegated;
		}
	}
}
