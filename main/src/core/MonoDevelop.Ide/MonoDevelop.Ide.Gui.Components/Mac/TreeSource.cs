//
// TreeSource.cs
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

#if MAC
using System;
using AppKit;
using System.Collections.Generic;
using Foundation;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.Ide.Gui.Components.Internal
{
	class TreeSource: NSOutlineViewDataSource
	{
		NodeList rootNodes = new NodeList ();
		IExtensibleTreeViewFrontend frontend;
		int version;
		int nextNodeId;

		public TreeSource (IExtensibleTreeViewFrontend frontend)
		{
			this.frontend = frontend;
		}

		public TreeItem GetItem (NodePosition pos)
		{
			var np = (MacNodePosition)pos;
			ValidatePosition (np);
			TreeItem node = np.ParentList [np.NodeIndex];
			if (!node.Connected) {
				node.Connected = true;
				np.ParentList [np.NodeIndex] = node;
			}
			return node;
		}

		public int Version {
			get {
				return version;
			}
		}

		public NodeList RootNodes {
			get { return rootNodes; }
		}

		public NodePosition GetItemPosition (TreeItem treeItem)
		{
			return treeItem.Position;
		}

		void ValidatePosition (MacNodePosition np)
		{
			if (np == null || !np.IsValid)
				throw new InvalidOperationException ("Invalid node position");

			if (np.StoreVersion != version) {
				np.NodeIndex = -1;
				for (int i=0; i<np.ParentList.Count; i++) {
					if (np.ParentList [i].NodeId == np.NodeId) {
						np.NodeIndex = i;
						break;
					}
				}
				if (np.NodeIndex == -1)
					throw new InvalidOperationException ("Invalid node position");
				np.StoreVersion = version;
			}
		}


		public override bool AcceptDrop (NSOutlineView outlineView, NSDraggingInfo info, NSObject item, nint index)
		{
			return false;
		}

		public override string[] FilesDropped (NSOutlineView outlineView, NSUrl dropDestination, NSArray items)
		{
			throw new NotImplementedException ();
		}

		public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject ofItem)
		{
			var item = (TreeItem) ofItem;

			if (ofItem == null) {
				if (childIndex >= rootNodes.Count)
					return null;
				return rootNodes[(int)childIndex];
			} else {
				if (!item.Filled) {
					var nav = new MacTreeNodeNavigator (frontend, outlineView, this, item.Position);
					nav.EnsureFilled ();
				}
				if (item.Children == null || childIndex >= item.Children.Count)
					return null;
				return item.Children [(int)childIndex];
			}
		}

		public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
		{
			if (item == null)
				return rootNodes.Count;
			else {
				var n = (TreeItem)item;
				if (!n.Filled) {
					try {
						frontend.BeginTreeUpdate ();
						var tb = new MacTreeNodeNavigator (frontend, null, this, n.Position);
						tb.EnsureFilled ();
					} finally {
						frontend.CancelTreeUpdate ();
					}
				}
				return n.Children != null ? n.Children.Count : 0;
			}
		}

		public override NSObject GetObjectValue (NSOutlineView outlineView, NSTableColumn forTableColumn, NSObject byItem)
		{
			return byItem;
		}

		public override void SetObjectValue (NSOutlineView outlineView, NSObject theObject, NSTableColumn tableColumn, NSObject item)
		{
		}

		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			return GetChildrenCount (outlineView, item) > 0;
		}

		public override NSObject ItemForPersistentObject (NSOutlineView outlineView, NSObject theObject)
		{
			return null;
		}

		public override bool OutlineViewwriteItemstoPasteboard (NSOutlineView outlineView, NSArray items, NSPasteboard pboard)
		{
			return false;
		}

		public override NSObject PersistentObjectForItem (NSOutlineView outlineView, NSObject item)
		{
			return null;
		}

		public override void SortDescriptorsChanged (NSOutlineView outlineView, NSSortDescriptor[] oldDescriptors)
		{
		}

		public override NSDragOperation ValidateDrop (NSOutlineView outlineView, NSDraggingInfo info, NSObject item, nint index)
		{
			return NSDragOperation.None;
		}

		public bool MoveNext (ref MacNodePosition pos)
		{
			ValidatePosition (pos);
			if (pos.NodeIndex + 1 >= pos.ParentList.Count)
				return false;
			TreeItem n = pos.ParentList[pos.NodeIndex + 1];
			pos = pos.MakeMutable ();
			pos.NodeId = n.NodeId;
			pos.NodeIndex++;
			pos.StoreVersion = version;
			return true;
		}

		public bool MoveToRoot (ref MacNodePosition pos)
		{
			if (rootNodes.Count == 0)
				return false;
			TreeItem n = rootNodes[0];
			pos = pos.MakeMutable ();
			pos.ParentList = rootNodes;
			pos.NodeId = n.NodeId;
			pos.NodeIndex = 0;
			pos.StoreVersion = version;
			return true;
		}

		public bool MoveToParent (ref MacNodePosition pos)
		{
			ValidatePosition (pos);
			if (pos.ParentList == rootNodes)
				return false;
			var parent = pos.ParentList.Parent;
			pos = pos.MakeMutable ();
			pos.ParentList = parent.ParentList;
			pos.NodeId = parent.NodeId;
			pos.NodeIndex = parent.NodeIndex;
			pos.StoreVersion = version;
			return true;
		}

		public bool MoveToFirstChild (ref MacNodePosition pos)
		{
			ValidatePosition (pos);
			TreeItem n = pos.ParentList[pos.NodeIndex];
			if (n.Children == null || n.Children.Count == 0)
				return false;
			pos = pos.MakeMutable ();
			pos.ParentList = n.Children;
			pos.NodeId = n.Children [0].NodeId;
			pos.NodeIndex = 0;
			pos.StoreVersion = version;
			return true;
		}

		public bool HasChildren (MacNodePosition pos)
		{
			ValidatePosition (pos);
			TreeItem n = pos.ParentList[pos.NodeIndex];
			return n.Children != null && n.Children.Count > 0;
		}

		public void Remove (ref MacNodePosition pos)
		{
			ValidatePosition (pos);
			pos.ParentList.RemoveAt (pos.NodeIndex);
			version++;
			pos.Invalidate ();
		}


		public TreeItem AddChild (ref MacNodePosition pos)
		{
			if (pos == null || !pos.IsUndefined)
				ValidatePosition (pos);

			TreeItem nn = new TreeItem ();
			nn.NodeId = nextNodeId++;

			NodeList list;

			if (pos.IsUndefined) {
				list = rootNodes;
			} else {
				TreeItem n = pos.ParentList [pos.NodeIndex];
				if (n.Children == null) {
					n.Children = new NodeList ();
					n.Children.Parent = new MacNodePosition () { ParentList = pos.ParentList, NodeId = n.NodeId, NodeIndex = pos.NodeIndex, StoreVersion = version };
					pos.ParentList [pos.NodeIndex] = n;
				}
				list = n.Children;
			}
			list.Add (nn);
			version++;

			pos = pos.MakeMutable ();
			pos.ParentList = list;
			pos.NodeId = nn.NodeId;
			pos.NodeIndex = list.Count - 1;
			pos.StoreVersion = version;

			nn.Position = pos.Clone ();

			return nn;
		}

		public NodePosition GetRootPosition ()
		{
			if (rootNodes.Count == 0)
				return null;
			TreeItem n = rootNodes[0];
			return new MacNodePosition {
				ParentList = rootNodes,
				NodeId = n.NodeId,
				NodeIndex = 0,
				StoreVersion = version
			};
		}

		public void ResetChildren (MacNodePosition pos)
		{
			ValidatePosition (pos);
			TreeItem n = pos.ParentList[pos.NodeIndex];
			n.Filled = false;
			n.Children = null;
			version++;
		}

		public TreeItem ClearChildren (MacNodePosition pos)
		{
			ValidatePosition (pos);
			TreeItem n = pos.ParentList[pos.NodeIndex];
			n.Filled = true;
			if (n.Children != null)
				n.Children.Clear ();
			version++;
			return n;
		}
	}

	enum ItemStatus: byte {
		NotChecked,
		Filled,
		HasChildren
	}

	class TreeItem: NSObject
	{
		public TreeItem ()
		{
		}

		public TreeItem (IntPtr p): base (p)
		{
		}

		public MacNodePosition Position;
		public object DataItem;
		public NodeBuilder[] NodeBuilderChain;
		public bool Connected;
		public NodeList Children;
		public int NodeId;
		public bool Filled;

		public string Label { get; set; }
		public NSAttributedString Markup { get; set; }
		public Xwt.Drawing.Image Icon { get; set; }
		public Xwt.Drawing.Image ClosedIcon { get; set; }
		public Xwt.Drawing.Image OverlayBottomLeft { get; set; }
		public Xwt.Drawing.Image OverlayBottomRight { get; set; }
		public Xwt.Drawing.Image OverlayTopLeft { get; set; }
		public Xwt.Drawing.Image OverlayTopRight { get; set; }

		public void LoadFrom (NodeInfo nodeInfo)
		{
			Label = nodeInfo.Label;
			var ft = Xwt.FormattedText.FromMarkup (Label);
			Markup = ft.ToAttributedString ();
			Icon = nodeInfo.Icon;
			ClosedIcon = nodeInfo.ClosedIcon;
			OverlayBottomLeft = nodeInfo.OverlayBottomLeft;
			OverlayBottomRight = nodeInfo.OverlayBottomRight;
			OverlayTopLeft = nodeInfo.OverlayTopLeft;
			OverlayTopRight = nodeInfo.OverlayTopRight;
		}
	}

	class NodeList: List<TreeItem>
	{
		public MacNodePosition Parent;
	}

	class MacNodePosition: NodePosition
	{
		public NodeList ParentList;
		public int NodeIndex;
		public int NodeId;
		public int StoreVersion = -2;
		public bool Frozen;

		public MacNodePosition MakeMutable ()
		{
			if (Frozen)
				return Clone ();
			else
				return this;
		}

		public override bool Equals (object obj)
		{
			MacNodePosition other = (MacNodePosition) obj;
			if (other == null)
				return false;
			return ParentList == other.ParentList && NodeId == other.NodeId;
		}

		public override int GetHashCode ()
		{
			return ParentList.GetHashCode () ^ NodeId;
		}

		public MacNodePosition Clone ()
		{
			return new MacNodePosition {
				ParentList = ParentList,
				NodeId = NodeId,
				NodeIndex = NodeIndex,
				StoreVersion = StoreVersion
			};
		}

		public void Invalidate ()
		{
			ParentList = null;
			StoreVersion = -1;
		}

		public void SetUndefined ()
		{
			ParentList = null;
			StoreVersion = -2;
		}

		public bool IsValid {
			get { return StoreVersion >= 0; }
		}

		public bool IsUndefined {
			get { return StoreVersion == -2; }
		}
	}
}

#endif
