//
// NodeCommandTargetChain.cs
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

	class NodeCommandTargetChain: ICommandDelegatorRouter
	{
		NodeCommandHandler target;
		ITreeNavigator[] nodes;
		internal NodeCommandTargetChain Next;

		public NodeCommandTargetChain (NodeCommandHandler target, ITreeNavigator[] nodes)
		{
			this.nodes = nodes;
			this.target = target;
		}

		public object GetNextCommandTarget ()
		{
			target.SetCurrentNodes (null);
			return Next;
		}

		public object GetDelegatedCommandTarget ()
		{
			target.SetCurrentNodes (nodes);
			return target;
		}
	}

}
