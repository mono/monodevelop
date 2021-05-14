//
// TestTreeBuilder.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Projects
{
	class TestTreeBuilder : ITreeBuilder
	{
		public object DataItem { get; set; }
		public string NodeName { get; set; }
		public bool Selected { get; set; }
		public bool Expanded { get; set; }
		public ITreeOptions Options { get; }
		public TypeNodeBuilder TypeNodeBuilder { get; }
		public NodePosition CurrentPosition { get; }
		public bool Filled { get; }

		public List<object> ChildNodes = new List<object> ();

		public void AddChild (object dataObject)
		{
			ChildNodes.Add (dataObject);
		}

		public void AddChild (object dataObject, bool moveToChild)
		{
			ChildNodes.Add (dataObject);
		}

		public void AddChildren (IEnumerable dataObjects)
		{
			foreach (object dataObject in dataObjects) {
				ChildNodes.Add (dataObject);
			}
		}

		public ITreeNavigator Clone ()
		{
			throw new NotImplementedException ();
		}

		public void ExpandToNode ()
		{
		}

		public bool FindChild (object dataObject)
		{
			throw new NotImplementedException ();
		}

		public bool FindChild (object dataObject, bool recursive)
		{
			throw new NotImplementedException ();
		}

		public Dictionary<Type, object> ParentDataItem = new Dictionary<Type, object> ();

		public object GetParentDataItem (Type type, bool includeCurrent)
		{
			if (ParentDataItem.TryGetValue (type, out object parent)) {
				return parent;
			}

			return null;
		}

		public T GetParentDataItem<T> (bool includeCurrent)
		{
			throw new NotImplementedException ();
		}

		public bool HasChild (string name, Type dataType)
		{
			throw new NotImplementedException ();
		}

		public bool HasChildren ()
		{
			throw new NotImplementedException ();
		}

		public bool MoveNext ()
		{
			throw new NotImplementedException ();
		}

		public bool MoveToChild (string name, Type dataType)
		{
			throw new NotImplementedException ();
		}

		public bool MoveToFirstChild ()
		{
			throw new NotImplementedException ();
		}

		public bool MoveToNextObject ()
		{
			throw new NotImplementedException ();
		}

		public bool MoveToObject (object dataObject)
		{
			throw new NotImplementedException ();
		}

		public bool MoveToParent ()
		{
			throw new NotImplementedException ();
		}

		public bool MoveToParent (Type type)
		{
			throw new NotImplementedException ();
		}

		public bool MoveToPosition (NodePosition position)
		{
			throw new NotImplementedException ();
		}

		public bool MoveToRoot ()
		{
			throw new NotImplementedException ();
		}

		public void Remove ()
		{
		}

		public void Remove (bool moveToParent)
		{
		}

		public void RestoreState (NodeState state)
		{
		}

		public NodeState SaveState ()
		{
			throw new NotImplementedException ();
		}

		public void ScrollToNode ()
		{
		}

		public void Update ()
		{
		}

		public void UpdateAll ()
		{
		}

		public void UpdateChildren ()
		{
		}
	}
}
