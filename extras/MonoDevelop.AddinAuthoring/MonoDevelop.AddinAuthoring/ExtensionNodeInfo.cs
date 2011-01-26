// 
// ExtensionNodeInfo.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Addins.Description;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.AddinAuthoring
{
	public class ExtensionNodeInfo
	{
		public int Order { get; set; }
		public ExtensionNodeDescription Node { get; set; }
		public List<ExtensionNodeInfo> Children { get; set; }
		
		public ExtensionNodeInfo (ExtensionNodeDescription node, bool canModify)
		{
			this.Node = node;
			this.CanModify = canModify;
		}
		
		public ExtensionNodeInfo (ExtensionNodeDescription node, bool canModify, int order)
		{
			this.Node = node;
			this.CanModify = canModify;
			this.Order = order;
		}
		
		public bool CanModify {
			get; set;
		}
		
		public void Add (ExtensionNodeInfo child)
		{
			if (Children == null)
				Children = new List<ExtensionNodeInfo> ();
			Children.Add (child);
		}
		
		public ExtensionNodeInfo GetChild (string name)
		{
			if (Children == null)
				return null;
			return Children.FirstOrDefault (n => n.Node.Id == name);
		}
		
		public List<ExtensionNodeInfo> Expand ()
		{
			if (Children != null)
				return Children;
			Children = new List<ExtensionNodeInfo> ();
			int i = 0;
			foreach (ExtensionNodeDescription n in Node.ChildNodes)
				Children.Add (new ExtensionNodeInfo (n, CanModify) { Order = i++ });
			return Children;
		}
		
		public bool HasChildren {
			get { return Node.ChildNodes.Count > 0; }
		}
		
		public void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public override bool Equals (object obj)
		{
			ExtensionNodeInfo en = obj as ExtensionNodeInfo;
			return en != null && Node == en.Node;
		}
		
		public override int GetHashCode ()
		{
			return Node.GetHashCode ();
		}


		
		public event EventHandler Changed;
	}
	
	public class ExtensionNodeInfoList: System.Collections.ObjectModel.Collection<ExtensionNodeInfo>
	{
		protected override void InsertItem (int index, ExtensionNodeInfo item)
		{
			base.InsertItem (index, item);
			UpdateOrder (index);
		}
		
		protected override void SetItem (int index, ExtensionNodeInfo item)
		{
			base.SetItem (index, item);
			item.Order = index;
		}
		
		protected override void RemoveItem (int index)
		{
			base.RemoveItem (index);
			UpdateOrder (index);
		}
		
		void UpdateOrder (int index)
		{
			for (int n=index; n<Count; n++)
				this[n].Order = n;
		}


	}
}

