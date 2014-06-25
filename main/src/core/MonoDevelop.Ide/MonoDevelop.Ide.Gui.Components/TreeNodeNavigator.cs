// TreeNodeNavigator.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Core;
using System.Collections;

namespace MonoDevelop.Ide.Gui.Components.Internal
{
	internal abstract class TreeNodeNavigator: ITreeNavigator, ITreeBuilder
	{
		NodePosition[] currentNodePositions;
		int currentNodePositionIndex;

		protected IExtensibleTreeViewFrontend Frontend { get; private set; }

		public TreeNodeNavigator (IExtensibleTreeViewFrontend frontend)
		{
			this.Frontend = frontend;
		}

		protected void OnPositionChanged ()
		{
			currentNodePositions = null;
		}
		
		public abstract ITreeNavigator Clone ();

		public abstract object DataItem { get; }

		public TypeNodeBuilder TypeNodeBuilder {
			get {
				NodeBuilder[] chain = NodeBuilderChain;
				if (chain != null && chain.Length > 0)
					return chain[0] as TypeNodeBuilder;
				return null;
			}
		}

		protected virtual void AssertIsValid ()
		{
		}
		
		public abstract bool Selected { get; set; }
		
		public abstract NodePosition CurrentPosition { get; }

		public abstract bool MoveToPosition (NodePosition position);

		public void ScrollToNode ()
		{
			Frontend.Backend.ScrollToCell (CurrentPosition);
		}

		public abstract bool MoveToRoot ();
	
		public bool MoveToObject (object dataObject)
		{
			var positions = Frontend.NodeHash.GetNodePositions (dataObject);
			if (positions.Length == 0)
				return false;
			MoveToPosition (positions [0]);
			currentNodePositionIndex = 0;
			currentNodePositions = positions;
			return true;
		}
	
		public bool MoveToNextObject ()
		{
			if (currentNodePositions != null) {
				if (++currentNodePositionIndex < currentNodePositions.Length) {
					var pos = currentNodePositions;
					if (MoveToPosition (pos [currentNodePositionIndex])) {
						currentNodePositions = pos;
						return true;
					}
				}
			} else {
				var positions = Frontend.NodeHash.GetNodePositions (DataItem);
				int i = Array.IndexOf (positions, CurrentPosition) + 1;
				if (i < positions.Length && MoveToPosition (positions [i])) {
					currentNodePositionIndex = i;
					currentNodePositions = positions;
					return true;
				}
			}
			currentNodePositions = null;
			return false;
		}
	
		public abstract bool MoveToParent ();

		public virtual bool MoveToParent (Type dataType)
		{
			AssertIsValid ();
			var oldPos = CurrentPosition;
			while (MoveToParent ()) {
				if (dataType.IsInstanceOfType (DataItem))
					return true;
			}
			MoveToPosition (oldPos);
			return false;
		}

		public abstract bool MoveToFirstChild ();

		public abstract bool MoveNext ();

		public bool HasChild (string name, Type dataType = null)
		{
			if (MoveToChild (name, dataType)) {
				MoveToParent ();
				return true;
			} else
				return false;
		}

		public abstract bool HasChildren ();
	
		public bool FindChild (object dataObject)
		{
			return FindChild (dataObject, false);
		}
		
		public bool FindChild (object dataObject, bool recursive)
		{
			AssertIsValid ();

			NodePosition[] poss = Frontend.NodeHash.GetNodePositions (dataObject);
			if (poss.Length == 0)
				return false;

			foreach (NodePosition pos in poss) {
				if (Frontend.Backend.IsChildPosition (CurrentPosition, pos, recursive)) {
					MoveToPosition (pos);
					return true;
				}
			}
			return false;
		}

		public bool MoveToChild (string name, Type dataType = null)
		{
			EnsureFilled ();
			var oldPos = CurrentPosition;

			if (!MoveToFirstChild ()) {
				MoveToPosition (oldPos);
				return false;
			}

			do {
				if (name == NodeName && (dataType == null || (dataType.IsInstanceOfType (DataItem))))
					return true;
			} while (MoveNext ());

			MoveToPosition (oldPos);
			return false;
		}

		public abstract bool Expanded { get; set; }

		public ITreeOptions Options {
			get { return Frontend.GlobalOptions; }
		}
		
		public NodeState SaveState ()
		{
			return NodeState.SaveState (this);
		}
		
		public void RestoreState (NodeState state)
		{
			NodeState.RestoreState (this, state);
		}
	
		public void Refresh ()
		{
			UpdateAll ();
		}
		
		public abstract void ExpandToNode ();

		public string NodeName {
			get {
				NodeBuilder[] chain = NodeBuilderChain;
				if (chain != null && chain.Length > 0)
					return ((TypeNodeBuilder)chain [0]).GetNodeName (this, DataItem);
				else
					return StoredNodeName;
			}
		}

		/// <summary>
		/// Returns the name stored in the tree node
		/// </summary>
		protected abstract string StoredNodeName { get; }

		public abstract NodeBuilder[] NodeBuilderChain { get; }

		public abstract object GetParentDataItem (Type type, bool includeCurrent);
	
		public void EnsureFilled ()
		{
			if (!Filled) {
				try {
					Frontend.BeginTreeUpdate ();
					FillNode ();
				} finally {
					Frontend.EndTreeUpdate ();
				}
			}
		}

		public abstract void FillNode ();

		public abstract bool Filled { get; }


		public void UpdateAll ()
		{
			try {
				Frontend.BeginTreeUpdate ();
				Update ();
				UpdateChildren ();
			} finally {
				Frontend.EndTreeUpdate ();
			}
		}

		public void Update ()
		{
			try {
				Frontend.BeginTreeUpdate ();
				var data = DataItem;
				var chain = NodeBuilderChain;
				NodeAttributes ats = Frontend.GetAttributes (this, chain, data);
				var nodeInfo = Frontend.GetNodeInfo (this, chain, data);
				OnUpdate (ats, nodeInfo);
			} finally {
				Frontend.EndTreeUpdate ();
			}
		}

		public void Update (NodeInfo nodeInfo)
		{
			try {
				Frontend.BeginTreeUpdate ();
				var data = DataItem;
				var chain = NodeBuilderChain;
				NodeAttributes ats = Frontend.GetAttributes (this, chain, data);
				OnUpdate (ats, nodeInfo);
			} finally {
				Frontend.EndTreeUpdate ();
			}
		}

		protected abstract void OnUpdate (NodeAttributes ats, NodeInfo nodeInfo);

		public void ResetState ()
		{
			try {
				Frontend.BeginTreeUpdate ();
				Update ();

				if (!Frontend.HasChildNodes (this, NodeBuilderChain, DataItem))
					FillNode ();
				else
					ResetChildren ();
			} finally {
				Frontend.EndTreeUpdate ();
			}
		}

		public abstract void ResetChildren ();

		public void UpdateChildren ()
		{
			try {
				Frontend.BeginTreeUpdate ();
				object data = DataItem;
				NodeBuilder[] chain = NodeBuilderChain;

				if (!Filled) {
					if (!Frontend.HasChildNodes (this, chain, data))
						FillNode ();
					return;
				}

				NodeState ns = SaveState ();
				RestoreState (ns);
			} finally {
				Frontend.EndTreeUpdate ();
			}
		}

		public void Remove ()
		{
			try {
				Frontend.BeginTreeUpdate ();
				Remove (false);
			} finally {
				Frontend.EndTreeUpdate ();
			}
		}

		public abstract void Remove (bool moveToParent);

		public void AddRoot (object dataObject)
		{
			AddChild (dataObject, true, true);
		}

		public void AddChild (object dataObject)
		{
			AddChild (dataObject, false);
		}

		public void AddChild (object dataObject, bool moveToChild)
		{
			AddChild (dataObject, false, moveToChild);
		}

		public void AddChild (object dataObject, NodeBuilder[] chain, NodeInfo ninfo, bool filled, bool moveToChild)
		{
			AddChild (dataObject, chain, ninfo, filled, false, moveToChild);
		}

		void AddChild (object dataObject, bool isRoot, bool moveToChild)
		{
			NodeBuilder[] chain = Frontend.GetBuilderChain (dataObject.GetType ());
			if (chain == null) return;

			var ni = Frontend.GetNodeInfo (this, chain, dataObject);
			var filled = !Frontend.HasChildNodes (this, chain, dataObject);
			AddChild (dataObject, chain, ni, filled, isRoot, moveToChild);
		}

		void AddChild (object dataObject, NodeBuilder[] chain, NodeInfo ni, bool filled, bool isRoot, bool moveToChild)
		{
			if (dataObject == null) throw new ArgumentNullException ("dataObject");

			if (!isRoot && !Filled)
				return;

			var oldIter = CurrentPosition;
			NodeAttributes ats = Frontend.GetAttributes (this, chain, dataObject);
			if ((ats & NodeAttributes.Hidden) != 0)
				return;

			try {
				Frontend.BeginTreeUpdate ();
				OnAddChild (dataObject, chain, ats, ni, filled, isRoot);

				Frontend.RegisterNode (CurrentPosition, dataObject, chain, true);
				Frontend.NotifyInserted (this, dataObject);

				if (!moveToChild)
					MoveToPosition (oldIter);
			} finally {
				Frontend.EndTreeUpdate ();
			}
		}

		public void AddChildren (IEnumerable dataObjects)
		{
			NodeBuilder[] chain = null;
			Type oldType = null;
			var oldIter = CurrentPosition;
			int items = 0;
			try {
				Frontend.BeginTreeUpdate ();
				PrepareBulkChildrenAdd ();
				foreach (object dataObject in dataObjects) {
					items++;
					if (chain == null || dataObject.GetType () != oldType) {
						oldType = dataObject.GetType ();
						chain = Frontend.GetBuilderChain (oldType);
						if (chain == null)
							continue;
					}

					NodeAttributes ats = Frontend.GetAttributes (this, chain, dataObject);
					if ((ats & NodeAttributes.Hidden) != 0)
						continue;

					var ni = Frontend.GetNodeInfo (this, chain, dataObject);
					var filled = !Frontend.HasChildNodes (this, chain, dataObject);
					OnAddChild (dataObject, chain, ats, ni, filled, false);

					Frontend.RegisterNode (CurrentPosition, dataObject, chain, true);
					Frontend.NotifyInserted (this, dataObject);

					MoveToPosition (oldIter);
				}
			} finally {
				FinishBulkChildrenAdd ();
				Frontend.EndTreeUpdate ();
			}
		}

		protected abstract void OnAddChild (object dataObject, NodeBuilder[] chain, NodeAttributes ats, NodeInfo nodeInfo, bool filled, bool isRoot);

		protected virtual void PrepareBulkChildrenAdd ()
		{
		}

		protected virtual void FinishBulkChildrenAdd ()
		{
		}

		protected void CreateChildren (object dataObject)
		{
			var chain = NodeBuilderChain;
			var pos = CurrentPosition;
			foreach (NodeBuilder builder in chain) {
				try {
					builder.PrepareChildNodes (dataObject);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
				MoveToPosition (pos);
			}
			foreach (NodeBuilder builder in chain) {
				try {
					builder.BuildChildNodes (this, dataObject);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
				MoveToPosition (pos);
			}
		}
	}
}
