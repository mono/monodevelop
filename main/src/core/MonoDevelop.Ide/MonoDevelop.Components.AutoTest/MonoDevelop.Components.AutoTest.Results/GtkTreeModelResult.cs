//
// GtkTreeModelResult.cs
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
using Gtk;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class GtkTreeModelResult : AppResult
	{
		TreeView TView;
		TreeModel TModel;
		int Column;

		public GtkTreeModelResult (TreeView treeView, TreeModel treeModel, int column)
		{
			TView = treeView;
			TModel = treeModel;
			Column = column;
		}

		public override AppResult Marked (string mark)
		{
			return null;
		}

		public override AppResult Button ()
		{
			return null;
		}

		public override AppResult TextField ()
		{
			return null;
		}

		public override AppResult Model (string column)
		{
			return null;
		}

		public override AppResult Text (string text)
		{
			throw new NotImplementedException ();
		}
	}
}

