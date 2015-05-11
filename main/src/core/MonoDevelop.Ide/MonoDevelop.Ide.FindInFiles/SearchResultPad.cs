//
// SearchResultPad.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using System.Threading;

namespace MonoDevelop.Ide.FindInFiles
{
	public class SearchResultPad : AbstractPadContent
	{
		readonly SearchResultWidget widget = new SearchResultWidget ();
		
		public string DefaultPlacement {
			get {
				return "Bottom"; 
			}
		}
		
		public override Gtk.Widget Control {
			get {
				return widget;
			}
		}
		
		public CancellationTokenSource CancellationTokenSource {
			get {
				return widget.CancellationTokenSource;
			}
			set {
				widget.CancellationTokenSource = value;
			}
		}
		public bool FocusPad {
			get;
			set;
		}
		public int InstanceNum {
			get;
			set;
		}
		
		public bool AllowReuse {
			get { 
				return widget.AllowReuse; 
			}
		}

		internal PathMode PathMode {
			set {
				widget.PathMode = value;
			}
		}
		
		public SearchResultPad (int instanceNum)
		{
			InstanceNum = instanceNum;
		}
		
		public void ReportResult (SearchResult result)
		{
			widget.Add (result);
		}

		public void ReportResults (IEnumerable<SearchResult> results)
		{
			widget.AddRange (results);
		}
		
		public override void Initialize (IPadWindow window)
		{
			window.Icon = Stock.FindIcon;
			base.Initialize (window);
		}
		
		public void BeginProgress (string title)
		{
			Window.IsWorking = true;
			widget.ShowStatus (GettextCatalog.GetString ("Searching..."));
			widget.BeginProgress ();
		}
		
		public void EndProgress ()
		{
			Window.IsWorking = false;
			widget.ShowStatus (" " + GettextCatalog.GetString("Search completed") + " - " + 
				string.Format (GettextCatalog.GetPluralString("{0} match.", "{0} matches.", widget.ResultCount), widget.ResultCount));
			widget.EndProgress ();
			if (FocusPad) 
				widget.FocusPad ();
		}
		
		public void WriteText (string text)
		{
			widget.WriteText (text);
		}
		
		public void ReportStatus (string statusText)
		{
			widget.ShowStatus (statusText);
		}
		
		#region CommandHandler
		[CommandHandler (ViewCommands.Open)]
		protected void OnOpen ()
		{
			widget.OpenSelectedMatches ();
		}
		
		[CommandHandler (EditCommands.SelectAll)]
		protected void OnSelectAll ()
		{
			widget.SelectAll ();
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			widget.CopySelection ();
		}

		#endregion
	}
}
