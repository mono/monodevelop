//
// GtkNodePosition.cs
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

namespace MonoDevelop.Ide.Gui.Components.Internal
{
	internal class GtkNodePosition: NodePosition
	{
		public readonly Gtk.TreeIter Iter;
		Gtk.TreeStore store;

		public GtkNodePosition (Gtk.TreeStore store, Gtk.TreeIter iter)
		{
			this.store = store;
			Iter = iter;
		}

		public override bool IsValid {
			get { return !Iter.Equals (Gtk.TreeIter.Zero) && store.IterIsValid (Iter); }
		}

		public override bool Equals (object obj)
		{
			var other = obj as GtkNodePosition;
			if (other == null)
				return false;
			return store.GetPath (Iter).Equals (store.GetPath (other.Iter));
		}

		public override int GetHashCode ()
		{
			var p = store.GetPath (Iter);
			int c = 0;
			unchecked {
				for (int n = 0; n < p.Indices.Length; n++)
					c ^= p.Indices [n];
			}
			return c;
		}
	}

	internal static class GtkNodePositionHelper
	{
		public static Gtk.TreeIter GetIter (this NodePosition pos)
		{
			return ((GtkNodePosition)pos).Iter;
		}
	}
}

