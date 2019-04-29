//
// DocumentViewContainer.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Collections.ObjectModel;

namespace MonoDevelop.Ide.Gui.Documents
{
	public sealed class DocumentViewContentCollection : Collection<DocumentView>
	{
		IDocumentViewContentCollectionListener listener;

		internal void AttachListener (IDocumentViewContentCollectionListener listener)
		{
			this.listener = listener;
		}

		internal void DetachListener ()
		{
			this.listener = null;
		}

		protected override void ClearItems ()
		{
			if (listener != null)
				listener.ClearItems (this);
			base.ClearItems ();
			if (listener != null)
				listener.ItemsCleared (this);
		}

		protected override void InsertItem (int index, DocumentView item)
		{
			if (listener != null)
				listener.InsertItem (this, index, item);
			base.InsertItem (index, item);
			if (listener != null)
				listener.ItemInserted (this, index, item);
		}

		protected override void RemoveItem (int index)
		{
			if (listener != null)
				listener.RemoveItem (this, index);
			base.RemoveItem (index);
			if (listener != null)
				listener.ItemRemoved (this, index);
		}

		protected override void SetItem (int index, DocumentView item)
		{
			if (listener != null)
				listener.SetItem (this, index, item);
			var oldItem = Items [index];
			base.SetItem (index, item);
			if (listener != null)
				listener.ItemSet (this, index, oldItem, item);
		}
	}

	interface IDocumentViewContentCollectionListener
	{
		void ClearItems (DocumentViewContentCollection list);
		void InsertItem (DocumentViewContentCollection list, int index, DocumentView item);
		void RemoveItem (DocumentViewContentCollection list, int index);
		void SetItem (DocumentViewContentCollection list, int index, DocumentView item);
		void ItemsCleared (DocumentViewContentCollection list);
		void ItemInserted (DocumentViewContentCollection list, int index, DocumentView item);
		void ItemRemoved (DocumentViewContentCollection list, int index);
		void ItemSet (DocumentViewContentCollection list, int index, DocumentView oldItem, DocumentView item);
	}
}
