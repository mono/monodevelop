//
// TreePathReference.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

using Gtk;

namespace MonoDevelop.Debugger
{
	public sealed class TreePathReference : IDisposable
	{
		int[] indices;
		TreePath path;

		public TreePathReference (TreeModel model, TreePath path)
		{
			model.RowsReordered += HandleRowsReordered;
			model.RowInserted += HandleRowInserted;
			model.RowDeleted += HandleRowDeleted;

			indices = path.Indices;
			this.path = path;
			Model = model;
		}

		void HandleRowsReordered (object o, RowsReorderedArgs args)
		{
			int length = Model.IterNChildren (args.Iter);
			int depth = args.Path.Depth;

			if (length < 2 || !args.Path.IsAncestor (Path) || indices.Length <= depth)
				return;

			for (int i = 0; i < length; i++) {
				if (args.NewChildOrder[i] == indices[depth]) {
					indices[depth] = i;
					break;
				}
			}
		}

		void HandleRowInserted (object o, RowInsertedArgs args)
		{
			var inserted = args.Path.Indices;
			int i;

			for (i = 0; i < inserted.Length - 1 && i < indices.Length - 1; i++) {
				if (inserted[i] > indices[i]) {
					// the inserted node is listed below the node we are watching, ignore it
					return;
				}

				if (inserted[i] < indices[i])
					break;
			}

			if (inserted[i] <= indices[i]) {
				// the node was inserted above the node we are watching, update our position
				indices[i]++;
				path = null;
			}
		}

		void HandleRowDeleted (object o, RowDeletedArgs args)
		{
			var deleted = args.Path.Indices;
			int i;

			for (i = 0; i < deleted.Length && i < indices.Length; i++) {
				if (deleted[i] > indices[i]) {
					// the deleted node is listed below the node we are watching, ignore it
					return;
				}

				if (deleted[i] < indices[i]) {
					// the deleted node is listed above the node we are watching, update our position
					indices[i]--;
					path = null;
					return;
				}
			}

			if (deleted.Length <= indices.Length) {
				// the node we are watching (or its parent) has been deleted
				Invalidate ();
			}
		}

		public TreeModel Model {
			get; private set;
		}

		public TreePath Path {
			get {
				if (path == null && indices != null)
					path = new TreePath (indices);

				return path;
			}
		}

		public bool IsValid {
			get { return Model != null && indices != null; }
		}

		void Invalidate ()
		{
			if (Model != null) {
				Model.RowsReordered -= HandleRowsReordered;
				Model.RowInserted -= HandleRowInserted;
				Model.RowDeleted -= HandleRowDeleted;
				Model = null;
			}
			
			indices = null;
			path = null;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			Invalidate ();
		}
		#endregion
	}
}
