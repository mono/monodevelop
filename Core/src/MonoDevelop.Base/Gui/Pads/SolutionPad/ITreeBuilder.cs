//
// ITreeBuilder.cs
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

namespace MonoDevelop.Gui.Pads
{
	public interface ITreeBuilder: ITreeNavigator
	{
		// Updates the current node and its children
		void UpdateAll ();
		
		// Updates the label and icon of the current node
		void Update ();
		
		// Updates the children of the current node
		void UpdateChildren ();
		
		// Removes the current node
		void Remove ();
		
		// Removes de current node and if moveToParent is true, it moves
		// to the parent node.
		void Remove (bool moveToParent);
		
		// Adds a child to the current node
		void AddChild (object dataObject);
		
		// Adds a child to the current node and if moveToChild is true, it
		// moves to the new child.
		void AddChild (object dataObject, bool moveToChild);
	}
}
