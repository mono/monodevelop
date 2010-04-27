// 
// NavigateToDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Gdk;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.CodeCompletion;
namespace MonoDevelop.Ide.NavigateToDialog
{
class ResultsDataSource: List<SearchResult>, IListViewDataSource
	{
		SearchResult bestResult;
		int bestRank = int.MinValue;
		Dictionary<string,bool> names = new Dictionary<string,bool> ();

		public string GetText (int n)
		{
			string descr = this[n].Description;
			if (string.IsNullOrEmpty (descr))
				return this[n].MarkupText;
			return this[n].MarkupText + " <span foreground=\"darkgray\">[" + descr + "]</span>";
		}
		
		public string GetSelectedText (int n)
		{
			string descr = this[n].Description;
			if (string.IsNullOrEmpty (descr))
				return GLib.Markup.EscapeText (this[n].PlainText);
			return GLib.Markup.EscapeText (this[n].PlainText) + " [" + descr + "]";
		}

		public Pixbuf GetIcon (int n)
		{
			return this[n].Icon;
		}
		
		public bool UseMarkup (int n)
		{
			return true;
		}
				
		public int ItemCount {
			get {
				return Count;
			}
		}

		public SearchResult BestResult {
			get {
				return bestResult;
			}
		}

		public void AddResult (SearchResult res)
		{
			Add (res);
			if (names.ContainsKey (res.PlainText))
				names[res.PlainText] = true;
			else
				names.Add (res.PlainText, false);
			if (res.Rank > bestRank) {
				bestResult = res;
				bestRank = res.Rank;
			}
		}
	}
}
