﻿//
// GtkNotebookResult.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using System.Collections.Generic;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class GtkNotebookResult : GtkWidgetResult
	{
		Notebook noteBook;
		Label toBeSelectedLabel;
		int toBeSelected = -1;

		public GtkNotebookResult (Widget notebookWidget) : base (notebookWidget)
		{
			noteBook = notebookWidget as Notebook;
		}

		public override string ToString ()
		{
			return String.Format ("{0} - {1} - {2} - {3}, - {4} - Tab: {5}", noteBook, noteBook.Allocation, noteBook.Name, noteBook.GetType ().FullName, noteBook.Toplevel.Name, toBeSelected);
		}

		public override AppResult Text (string text, bool exact)
		{
			for (int i = 0; i < noteBook.NPages; i++) {
				var iTab = noteBook.GetNthPage (i);
				var label = noteBook.GetTabLabelText (iTab);
				if (CheckForText (label, text, exact)) {
					toBeSelectedLabel = noteBook.GetTabLabel (iTab) as Label;
					toBeSelected = i;
					return this;
				}
			}
			return null;
		}

		public override AppResult Selected ()
		{
			if (base.Selected () != null) {
				return noteBook.CurrentPage == toBeSelected ? this : null;
			}
			return null;
		}

		public override bool Select ()
		{
			if (toBeSelected >= 0) {
				noteBook.CurrentPage = toBeSelected;
				return true;
			}
			return false;
		}
	}
}

