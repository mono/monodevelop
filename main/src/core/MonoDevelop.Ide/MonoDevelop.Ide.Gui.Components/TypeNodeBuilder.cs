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

namespace MonoDevelop.Ide.Gui.Components
{
	public abstract class TypeNodeBuilder: NodeBuilder
	{
		public abstract Type NodeDataType { get; }
		
		public abstract string GetNodeName (ITreeNavigator thisNode, object dataObject);
		
		// Optional override that should return the parent object of the given
		// object.
		public virtual object GetParentObject (object dataObject)
		{
			return null;
		}
		
		public virtual string ContextMenuAddinPath {
			get { return null; }
		}

		/// <summary>
		/// Gets a value indicating whether reference equality should be used to compare objects of this type
		/// </summary>
		public virtual bool UseReferenceEquality {
			get { return false; }
		}
	}
}
