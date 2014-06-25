//
// SelectionGroup.cs
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
using System;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Gui.Components.Internal
{
	internal class SelectionGroup
	{
		public NodeBuilder[] BuilderChain;
		public List<ITreeNavigator> Nodes;

		NodePosition[] savedPos;
		object[] dataItems;

		public object[] DataItems {
			get {
				if (dataItems == null) {
					dataItems = new object [Nodes.Count];
					for (int n=0; n<Nodes.Count; n++)
						dataItems [n] = Nodes [n].DataItem;
				}
				return dataItems;
			}
		}

		public void SavePositions ()
		{
			savedPos = new NodePosition [Nodes.Count];
			for (int n=0; n<Nodes.Count; n++)
				savedPos [n] = Nodes [n].CurrentPosition;
		}

		public bool RestorePositions (ExtensibleTreeView tree)
		{
			for (int n=0; n<Nodes.Count; n++) {
				if (savedPos[n].IsValid)
					Nodes [n].MoveToPosition (savedPos [n]);
				else
					return false;
			}
			return true;
		}
	}
}

