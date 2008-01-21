// ExtendibleTextEditor.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Text;

using Mono.TextEditor;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.SourceEditor
{
	public class ExtendibleTextEditor : TextEditor
	{
		ITextEditorExtension extension = null;
		LanguageItemWindow languageItemWindow;
		SourceEditorView view;
		
		const int LanguageItemTipTimer = 800;
		ILanguageItem tipItem;
		bool showTipScheduled;
		int langTipX, langTipY;
		uint tipTimeoutId;
		Dictionary<int, ErrorInfo> errors = new Dictionary<int,ErrorInfo> ();
		
		public ITextEditorExtension Extension {
			get {
				return extension;
			}
			set {
				extension = value;
			}
		}
		
		public ExtendibleTextEditor (SourceEditorView view)
		{
			this.view = view;
			base.TextEditorData.Caret.PositionChanged += delegate {
				if (extension != null)
					extension.CursorPositionChanged ();
			};
			base.TextEditorData.Document.Buffer.TextReplaced += delegate (object sender, ReplaceEventArgs args) {
				if (extension != null)
					extension.TextChanged (args.Offset, args.Offset + Math.Max (args.Count, args.Value != null ? args.Value.Length : 0));
			};
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (extension != null) {
				if (extension.KeyPress (evnt.Key, evnt.State))
					return true;
			}
			bool result = base.OnKeyPressEvent (evnt);
			return result;
		}
		
		double mx, my;
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			mx = evnt.X - this.XOffset;
			my = evnt.Y;
			bool result = base.OnMotionNotifyEvent (evnt);
			UpdateLanguageItemWindow ();
			return result;
		}
		
		void UpdateLanguageItemWindow ()
		{
			if (languageItemWindow != null) {
				// Tip already being shown. Update it.
				ShowTooltip ();
			}
			else if (showTipScheduled) {
				// Tip already scheduled. Reset the timer.
				GLib.Source.Remove (tipTimeoutId);
				tipTimeoutId = GLib.Timeout.Add (LanguageItemTipTimer, ShowTooltip);
			}
			else {
				// Start a timer to show the tip
				showTipScheduled = true;
				tipTimeoutId = GLib.Timeout.Add (LanguageItemTipTimer, ShowTooltip);
			}
		}
		
		bool ShowTooltip ()
		{
			string errorInfo;

			showTipScheduled = false;
			int xloc = (int)mx;
			int yloc = (int)my;
			ILanguageItem item = GetLanguageItem (Document.LocationToOffset (base.VisualToDocumentLocation ((int)mx, (int)my)));
			if (item != null) {
				// Tip already being shown for this language item?
				if (languageItemWindow != null && tipItem != null && tipItem.Equals (item))
					return false;
				
				langTipX = xloc;
				langTipY = yloc;
				tipItem = item;

				HideLanguageItemWindow ();
				
				IParserContext pctx = view.GetParserContext ();
				if (pctx == null)
					return false;

				DoShowTooltip (new LanguageItemWindow (tipItem, pctx, view.GetAmbience (), 
				                                        GetErrorInformationAt (Caret.Offset)), langTipX, langTipY);
				
				
			} else if (!string.IsNullOrEmpty ((errorInfo = GetErrorInformationAt(Caret.Offset)))) {
				// Error tooltip already shown
				if (languageItemWindow != null /*&& tiItem == ti.Line*/)
					return false;
				//tiItem = ti.Line;
				
				HideLanguageItemWindow ();
				DoShowTooltip (new LanguageItemWindow (null, null, null, errorInfo), xloc, yloc);
			} else
				HideLanguageItemWindow ();
			
			return false;
		}
		
		void DoShowTooltip (LanguageItemWindow liw, int xloc, int yloc)
		{
			languageItemWindow = liw;
			
			int ox = 0, oy = 0;
			
			this.GdkWindow.GetOrigin (out ox, out oy);
			int w = languageItemWindow.Child.SizeRequest ().Width;
			languageItemWindow.Move (xloc + ox - (w/2), yloc + oy + 20);
			languageItemWindow.ShowAll ();
		}
		
		protected override void OnUnrealized ()
		{
			if (showTipScheduled) {
				GLib.Source.Remove (tipTimeoutId);
				showTipScheduled = false;
			}
			base.OnUnrealized ();
		}
		string GetErrorInformationAt (int offset)
		{
//			ErrorInfo info;
//			if (errors.TryGetValue (iter.Line, out info))
//				return "<b>" + GettextCatalog.GetString ("Parser Error:") + "</b> " + info.Message;
//			else
				return null;
		}
		
		ILanguageItem GetLanguageItem (int offset)
		{
			string txt = this.Document.Buffer.Text;
			string fileName = view.ContentName;
			if (fileName == null)
				fileName = view.UntitledName;

			IParserContext ctx = view.GetParserContext ();
			if (ctx == null)
				return null;

			IExpressionFinder expressionFinder = null;
			if (fileName != null)
				expressionFinder = ctx.GetExpressionFinder (fileName);

			string expression = expressionFinder == null ? TextUtilities.GetExpressionBeforeOffset (view, offset) : expressionFinder.FindFullExpression (txt, offset).Expression;
			if (expression == null)
				return null;
			
			int lineNumber = this.Document.Splitter.OffsetToLineNumber (offset);
			LineSegment line = this.Document.GetLine (lineNumber);

			return ctx.ResolveIdentifier (expression, lineNumber + 1, line.Offset + 1, fileName, txt);
		}		
		

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)		
		{
			HideLanguageItemWindow ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			HideLanguageItemWindow ();
			return base.OnScrollEvent (evnt);
		}
		
		public void HideLanguageItemWindow ()
		{
			if (showTipScheduled) {
				GLib.Source.Remove (tipTimeoutId);
				showTipScheduled = false;
			}
			if (languageItemWindow != null) {
				languageItemWindow.Destroy ();
				languageItemWindow = null;
			}
		}
	}
}
