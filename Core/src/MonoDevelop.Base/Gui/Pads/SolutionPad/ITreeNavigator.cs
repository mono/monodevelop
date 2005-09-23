//
// ITreeNavigator.cs
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
using System.Collections;

namespace MonoDevelop.Gui.Pads
{
	public interface ITreeNavigator
	{
		object DataItem { get; }
		string NodeName { get; }

		object GetParentDataItem (Type type, bool includeCurrent);
		bool Selected { get; set; }
		bool Expanded { get; set; }
		void ExpandToNode ();
		ITreeOptions Options { get; }
		
		NodeState SaveState ();
		void RestoreState (NodeState state);

		NodePosition CurrentPosition { get; }
		bool MoveToPosition (NodePosition position);
		
		bool MoveToParent ();
		bool MoveToParent (Type type);
		bool MoveToRoot ();
		bool MoveToFirstChild ();
		bool MoveToChild (string name, Type dataType);
		bool HasChild (string name, Type dataType);
		bool HasChildren ();
		bool MoveNext ();
		
		// The following methods only look through nodes already created
		// (the tree is lazily created)
		bool MoveToObject (object dataObject);
		bool FindChild (object dataObject);
		bool FindChild (object dataObject, bool recursive);
		
		// True if the node has been filled with child data.
		bool Filled { get; }
		
		ITreeNavigator Clone ();
	}
	
	public struct NodePosition {
		internal Gtk.TreeIter _iter;
	}
}
