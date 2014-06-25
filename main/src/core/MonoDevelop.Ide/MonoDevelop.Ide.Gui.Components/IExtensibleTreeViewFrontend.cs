//
// IExtensibleTreeFrontend.cs
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
using MonoDevelop.Ide.Gui.Components.Internal;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Components
{

	internal interface IExtensibleTreeViewFrontend
	{
		bool CheckAndDrop (TreeNodeNavigator nav, DragOperation oper, DropPosition dropPos, bool drop, object[] obj);

		bool HasChildNodes (ITreeBuilder tb, NodeBuilder[] chain, object dataObject);

		void RenameNode (TreeNodeNavigator nav, string newText);

		void CollapseCurrentItem ();

		void ExpandCurrentItem ();

		CommandEntrySet CreateContextMenu ();

		// Events
		void OnCurrentItemDeleted ();
		void OnSelectionChanged ();
		void OnCurrentItemActivated ();
		void NotifySelectionChanged ();

		int CompareObjects (NodeBuilder[] chain, ITreeNavigator thisNode, ITreeNavigator otherNode);

		void BeginTreeUpdate ();
		void EndTreeUpdate ();
		void CancelTreeUpdate ();

		// For private use
		NodeInfo GetNodeInfo (ITreeBuilder tb, NodeBuilder[] chain, object dataObject);
		NodeAttributes GetAttributes (ITreeBuilder tb, NodeBuilder[] chain, object dataObject);
		ExtensibleTreeView.TreeOptions GlobalOptions { get; }
		ExtensibleTreeViewBackend Backend { get; }
		NodeBuilder[] GetBuilderChain (Type type);
		void NotifyInserted (ITreeNavigator nav, object dataObject);
		void RegisterNode (NodePosition it, object dataObject, NodeBuilder[] chain = null, bool fireAddedEvent = true);
		void UnregisterNode (object dataObject, NodePosition pos, NodeBuilder[] chain = null, bool fireRemovedEvent = true);
		NodePositionDictionary NodeHash { get; }
	}
}
