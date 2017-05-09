//
// WorkspaceObjectExtension.cs
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
using Mono.Addins;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	public class WorkspaceObjectExtension: ChainedExtension
	{
		WorkspaceObjectExtension next;
		internal ProjectModelExtensionNode SourceExtensionNode;

		internal protected override void InitializeChain (ChainedExtension next)
		{
			base.InitializeChain (next);
			this.next = FindNextImplementation<WorkspaceObjectExtension> (next);
		}

		protected WorkspaceObject Owner { get; private set; }

		internal protected virtual bool SupportsObject (WorkspaceObject item)
		{
			return SourceExtensionNode == null || SourceExtensionNode.CanHandleObject (item);
		}

		internal void Init (WorkspaceObject item)
		{
			Owner = item;
			Initialize ();
		}

		/// <summary>
		/// Invoked just after creation the extension chain of the object
		/// </summary>
		internal protected virtual void Initialize ()
		{
		}

		/// <summary>
		/// Invoked after all extensions have been initialized
		/// </summary>
		internal protected virtual void OnExtensionChainCreated ()
		{
		}

		internal protected virtual object GetService (Type t)
		{
			return t.IsInstanceOfType (this) ? this : next.GetService (t);
		}

		internal void NotifyShared ()
		{
			OnSetShared ();
			if (next != null)
				next.OnSetShared ();
		}

		protected virtual void OnSetShared ()
		{
		}

		internal protected virtual StringTagModelDescription OnGetStringTagModelDescription (ConfigurationSelector conf)
		{
			var m = next.OnGetStringTagModelDescription (conf);
			m.Add (GetType ());
			return m;
		}

		internal protected virtual StringTagModel OnGetStringTagModel (ConfigurationSelector conf)
		{
			var m = next.OnGetStringTagModel (conf);
			m.Add (this);
			return m;
		}
	}
}

