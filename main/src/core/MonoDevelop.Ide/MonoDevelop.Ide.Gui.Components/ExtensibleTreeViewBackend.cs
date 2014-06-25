//
// ExtensibleTreeViewBackend.cs
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
using MonoDevelop.Ide.Gui.Components.Internal;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Components
{
	internal abstract class ExtensibleTreeViewBackend
	{
		public virtual void Initialize (IExtensibleTreeViewFrontend frontend)
		{
		}

		public abstract object CreateWidget ();

		public virtual void UpdateFont ()
		{
		}

		public virtual void EnableDragUriSource (Func<object,string> nodeToUri)
		{
		}

		public abstract TreeNodeNavigator CreateNavigator ();

		public virtual void Clear ()
		{
		}

		public abstract NodePosition GetSelectedNode ();

		public abstract NodePosition[] GetSelectedNodes ();

		public abstract bool MultipleNodesSelected ();

		public abstract TreeNodeNavigator GetNodeAtPosition (NodePosition position);

		public abstract TreeNodeNavigator GetRootNode ();

		public abstract void StartLabelEdit ();

		public abstract void CollapseTree ();

		public abstract bool ShowSelectionPopupButton { get; set; }

		public abstract void ScrollToCell (NodePosition pos);

		public abstract bool IsChildPosition (NodePosition parent, NodePosition potentialChild, bool recursive);

		public virtual void OnNodeUnregistered (object dataObject)
		{
		}

		public virtual void EnableAutoTooltips ()
		{
		}

		public abstract bool AllowsMultipleSelection { get; set; }

		public virtual void BeginTreeUpdate ()
		{
		}

		public virtual void EndTreeUpdate ()
		{
		}
	}

}

