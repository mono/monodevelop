//
// TypeNodeBuilder.cs
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
	public abstract class TypeNodeBuilder: NodeBuilder
	{
		// Return this const in CompareToObject to instruct the tree view to
		// use the default sorting rules for the compared objects.
		public const int DefaultSort = int.MinValue;
		
		public abstract Type NodeDataType { get; }
		
		public abstract string GetNodeName (ITreeNavigator thisNode, object dataObject);
		
		// Optional override that should return the parent object of the given
		// object.
		public virtual object GetParentObject (object dataObject)
		{
			return null;
		}
		
		// Return -1 if thisDataObject is less than otherDataObject, 0 if equal, 1 if greater
		// Return DefaultSort is sort is undefined or you want to use default sorting rules
		// (by default, it compares the node name).
		// The thisDataObject parameter is an instance valid for this node builder.
		// otherDataObject may not be an instance valid for this builder.
		public virtual int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			return DefaultSort;
		}
		
		public virtual string ContextMenuAddinPath {
			get { return null; }
		}
	}
}
