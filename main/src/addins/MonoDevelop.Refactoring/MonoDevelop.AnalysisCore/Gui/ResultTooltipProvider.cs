// 
// ResultTooltipProvider.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.AnalysisCore.Gui
{
	class ResultTooltipProvider : TooltipProvider
	{
		public ResultTooltipProvider ()
		{
		}

		public override Task<TooltipItem> GetItem (TextEditor editor, DocumentContext ctx, int offset, CancellationToken token = default(CancellationToken))
		{
//			//get the ResultsEditorExtension from the editor
//			var ed =  editor as ExtensibleTextEditor;
//			if (ed == null)
//				return null;
//			var ext = ed.Extension;
//			while (ext != null && !(ext is ResultsEditorExtension))
//				ext = ext.Next;
//			if (ext == null)
//				return null;
//			var resExt = (ResultsEditorExtension) ext;
//			
//			//get the results from the extension
//			var results = resExt.GetResultsAtOffset (offset);
//			if (results == null || results.Count == 0)
//				return null;
//			
//			return new TooltipItem (results, editor.GetLineByOffset (offset));
			return Task.FromResult<TooltipItem> (null);
		}

		public override MonoDevelop.Components.Control CreateTooltipWindow (TextEditor editor, DocumentContext ctx, TooltipItem item, int offset, Gdk.ModifierType modifierState)
		{
//			//create a message string from all the results
//			var results = (IList<Result>)item.Item;
//			var sb = new StringBuilder ();
//			bool first = false;
//			foreach (var r in results) {
//				if (!first)
//					first = true;
//				else
//					sb.AppendLine ();
//				sb.Append (r.Level.ToString ());
//				sb.Append (": ");
//				sb.Append (AmbienceService.EscapeText (r.Message));
//			}
//
//			//FIXME: use a nicer, more specialized tooltip window, with results formatting and hints about 
//			// commands and stuff
//			var win = new LanguageItemWindow ((ExtensibleTextEditor) editor, modifierState, null, sb.ToString (), null);
//			if (win.IsEmpty)
//				return null;
//			return win;
			return null;
		}

//		protected override void GetRequiredPosition (ITextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
//		{
//			var win = (LanguageItemWindow) tipWindow;
//			requiredWidth = win.SetMaxWidth (win.Screen.Width);
//			xalign = 0.5;
//		}
	}
}

