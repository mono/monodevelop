// 
// State.cs
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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Xml.StateEngine
{
	public abstract class State
	{
		State parent;

		/// <summary>
		/// When the <see cref="Parser"/> advances by one character, it calls this method 
		/// on the currently active <see cref="State"/> to determine the next state.
		/// </summary>
		/// <param name="c">The current character.</param>
		/// <param name="rollback"> If set non-null, the parser will be rolled back that number 
		/// of characters (empty string means replay current char to the next state.
		/// Note that this will not change the DOM state.</param>
		/// <returns>
		/// The next state. A new or parent <see cref="State"/> will change the parser state; 
		/// the current state or <see cref="null"/> will not.
		/// </returns>
		public abstract State PushChar (char c, IParseContext context, ref string rollback);

		public State Parent { get { return parent; } }
		
		protected void Adopt (State child)
		{
			Debug.Assert (child.parent == null);
			child.parent = this;
		}
		
		public RootState RootState {
			get {
				return (this as RootState) ?? parent.RootState;
			}
		}
	}
	
	public abstract class RootState : State
	{
		public abstract XDocument CreateDocument ();
	}
}
