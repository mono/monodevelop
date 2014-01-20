//
// WorkspaceItemExtension.cs
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
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	public class WorkspaceItemExtension: WorkspaceObjectExtension
	{
		WorkspaceItemExtension next;

		internal protected override bool SupportsObject (WorkspaceObject item)
		{
			return item is WorkspaceItem;
		}

		internal protected override void InitializeChain (ChainedExtension next)
		{
			this.next = FindNextImplementation<WorkspaceItemExtension> (next);
		}

		protected WorkspaceItem Item {
			get { return (WorkspaceItem) base.Owner; }
		}

		internal protected virtual bool SupportsItem (WorkspaceItem item)
		{
			return next.SupportsItem (item);
		}

		internal protected virtual Task Save (ProgressMonitor monitor)
		{
			return next.Save (monitor);
		}

		internal protected virtual IEnumerable<FilePath> GetItemFiles (bool includeReferencedFiles)
		{
			return next.GetItemFiles (includeReferencedFiles);
		}
	}
}

