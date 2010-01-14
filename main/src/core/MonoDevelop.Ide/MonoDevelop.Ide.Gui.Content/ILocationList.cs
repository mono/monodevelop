// 
// ILocationList.cs
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

namespace MonoDevelop.Ide.Gui.Content
{
	/// <summary>
	/// This interface can be implemented to provide a list of locations through which the
	/// user can browse with F4. To set the active list, set IdeApp.Workbench.ActiveLocationList
	/// </summary>
	public interface ILocationList
	{
		/// <summary>
		/// Return the name of items that this list enumerates. For example 'error' or 'search result'
		/// </summary>
		string ItemName { get; }
		
		/// <summary>
		/// Returns the next location in the list. Null if there are no more locations.
		/// </summary>
		NavigationPoint GetNextLocation ();
		
		/// <summary>
		/// Returns the previous location in the list. Null if there are no more locations.
		/// </summary>
		NavigationPoint GetPreviousLocation ();
	}
}

