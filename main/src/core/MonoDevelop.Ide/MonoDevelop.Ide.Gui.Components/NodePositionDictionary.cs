//
// NodePositionDictionary.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

//#define TREE_VERIFY_INTEGRITY

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Extensions;
using System.Linq;
using MonoDevelop.Ide.Gui.Components.Internal;

namespace MonoDevelop.Ide.Gui.Components
{

	class NodePositionDictionary
	{
		// This dictionary can be configured to use object reference equality
		// instead of regular object equality for a specific set of types

		NodeComparer nodeComparer;
		Dictionary<object,object> dictionary = new Dictionary<object, object> (new NodeComparer ());
		static NodePosition[] NoPositions = new NodePosition[0];

		public NodePositionDictionary (): base ()
		{
			nodeComparer = (NodeComparer)dictionary.Comparer;
		}

		public IEnumerable<object> AllObjects {
			get { return dictionary.Keys; }
		}

		public void Clear ()
		{
			dictionary.Clear ();
		}

		public bool IsRegistered (object dataObject)
		{
			return dictionary.ContainsKey (dataObject);
		}

		public NodePosition[] GetNodePositions (object dataObject)
		{
			object posInfo;
			if (dictionary.TryGetValue (dataObject, out posInfo)) {
				if (posInfo is NodePosition)
					return new [] { (NodePosition)posInfo };
				else
					return (NodePosition[])posInfo;
			} else
				return NoPositions;
		}

		/// <summary>
		/// Registers a new node. Returns true if the node has been registered for the first time
		/// </summary>
		public bool RegisterNode (object dataObject, NodePosition pos)
		{
			object currentIt;
			if (!dictionary.TryGetValue (dataObject, out currentIt)) {
				dictionary [dataObject] = pos;
				return true;
			} else {
				if (currentIt is NodePosition[]) {
					var arr = (NodePosition[]) currentIt;
					Array.Resize (ref arr, arr.Length + 1);
					arr [arr.Length - 1] = pos;
					dictionary [dataObject] = arr;
				} else {
					dictionary [dataObject] = new [] { pos, (NodePosition) currentIt};
				}
				return false;
			}
		}

		public bool UnregisterNode (object dataObject, NodePosition pos)
		{
			object poss;
			if (!dictionary.TryGetValue (dataObject, out poss))
				return false;
			if (poss is NodePosition[]) {
				var arr = ((NodePosition[])poss).ToList ();
				arr.Remove (pos);
				if (arr.Count == 1)
					dictionary [dataObject] = arr [0];
				else
					dictionary [dataObject] = arr.ToArray ();
				return false;
			} else {
				dictionary.Remove (dataObject);
				return true;
			}
		}

		/// <summary>
		/// Sets that the objects of the specified type have to be compared
		/// using object reference equality
		/// </summary>
		public void RegisterByRefType (Type type)
		{
			nodeComparer.byRefTypes.Add (type);
		}

		class NodeComparer: IEqualityComparer<object>
		{
			public HashSet<Type> byRefTypes = new HashSet<Type> ();
			public Dictionary<Type,bool> typeData = new Dictionary<Type, bool> ();

			bool IEqualityComparer<object>.Equals (object x, object y)
			{
				if (CompareByRef (x.GetType ()))
					return x == y;
				else
					return x.Equals (y);
			}

			int IEqualityComparer<object>.GetHashCode (object obj)
			{
				if (CompareByRef (obj.GetType ()))
					return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode (obj);
				else
					return obj.GetHashCode ();
			}

			bool CompareByRef (Type type)
			{
				if (byRefTypes.Count == 0)
					return false;

				bool compareRef;
				if (!typeData.TryGetValue (type, out compareRef)) {
					compareRef = false;
					var t = type;
					while (t != null) {
						if (byRefTypes.Contains (t)) {
							compareRef = true;
							break;
						}
						t = t.BaseType;
					}
					typeData [type] = compareRef;
				}
				return compareRef;
			}
		}
	}
}
