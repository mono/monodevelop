//
// AppResult.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.Runtime.InteropServices;

namespace MonoDevelop.Components.AutoTest
{
	public abstract class AppResult : MarshalByRefObject
	{
		//public Gtk.Widget ResultWidget { get; private set; }

		public AppResult ParentNode { get; set; }
		public AppResult FirstChild { get; set; }
		public AppResult PreviousSibling { get; set; }
		public AppResult NextSibling { get; set; }

		// Operations
		public abstract AppResult Marked (string mark);
		public abstract AppResult CheckType (Type desiredType);
		public abstract AppResult Text (string text, bool exact);
		public abstract AppResult Model (string column);
		public abstract AppResult Property (string propertyName, object value);
		public abstract List<AppResult> NextSiblings ();

		// Actions
		public abstract bool Select ();
		public abstract bool Click ();
		public abstract bool TypeKey (char key, string state);
		public abstract bool EnterText (string text);
		public abstract bool Toggle (bool active);

		void AddChildrenToList (List<AppResult> children, AppResult child)
		{
			AppResult node = child.FirstChild;
			children.Add (child);

			while (node != null) {
				AddChildrenToList (children, node);
				node = node.NextSibling;
			}
		}

		public List<AppResult> FlattenChildren ()
		{
			List<AppResult> children = new List<AppResult> ();
			AddChildrenToList (children, FirstChild);

			return children;
		}
	}
}

