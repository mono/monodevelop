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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Components.MainToolbar;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.Ide.NavigateToDialog
{
	class ResultsDataSource: List<SearchResult>, ISearchDataSource
	{
		Gtk.Widget widget;
		SearchResult bestResult;
		int bestRank = int.MinValue;
		Dictionary<string,bool> names = new Dictionary<string,bool> ();
		
		public ResultsDataSource (Gtk.Widget widget)
		{
			this.widget = widget;
		}

		#region ISearchDataSource implementation

		Gdk.Pixbuf ISearchDataSource.GetIcon (int item)
		{
			return this[item].Icon;
		}

		string ISearchDataSource.GetMarkup (int item, bool isSelected)
		{
			if (isSelected)
				return GLib.Markup.EscapeText (this[item].PlainText);
			return this[item].GetMarkupText (widget);
		}

		string ISearchDataSource.GetDescriptionMarkup (int item, bool isSelected)
		{
			if (isSelected)
				return GLib.Markup.EscapeText (this[item].Description);
			return this[item].GetDescriptionMarkupText (widget);
		}

		ICSharpCode.NRefactory.TypeSystem.DomRegion ISearchDataSource.GetRegion (int item)
		{
			var result = this [item];
			return new DomRegion (result.File, result.Row, result.Column, result.Row, result.Column);
		}

		bool ISearchDataSource.CanActivate (int item)
		{
			var result = this [item];
			return result.CanActivate;
		}

		void ISearchDataSource.Activate (int item)
		{
			var result = this [item];
			result.Activate ();
		}
		
		double ISearchDataSource.GetWeight (int item)
		{
			return this [item].Rank;
		}


		int ISearchDataSource.ItemCount {
			get {
				return this.Count;
			}
		}
		#endregion		
/*		public string GetText (int n)
		{

		}
		
		public string GetSelectedText (int n)
		{
			string descr = this[n].Description;
			if (string.IsNullOrEmpty (descr))
return GLib.Markup.EscapeText (this[n].PlainText);
			return GLib.Markup.EscapeText (this[n].PlainText) + " [" + descr + "]";
		}*/

		public SearchResult BestResult {
			get {
				return bestResult;
			}
		}

		public void AddResult (SearchResult res)
		{
			Add (res);
			if (names.ContainsKey (res.MatchedString))
				names[res.MatchedString] = true;
			else
				names.Add (res.MatchedString, false);
			
			if (res.Rank > bestRank) {
				bestResult = res;
				bestRank = res.Rank;
			}
		}
	}
}
