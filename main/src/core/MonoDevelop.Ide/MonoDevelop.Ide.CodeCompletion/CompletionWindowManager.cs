// 
// CompletionListWindow.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class CompletionWindowManager
	{
		static CompletionListWindow wnd;
		
		public static bool IsVisible {
			get {
				return wnd != null /*&& wnd.Visible*/;
			}
		}
		
		public static CompletionListWindow Wnd {
			get { return wnd; }
		}
		
		public static int X {
			get {
				return wnd.X;
			}
		}
		
		public static int Y {
			get {
				return wnd.Y;
			}
		}
		
		public static CodeCompletionContext CodeCompletionContext {
			get {
				return wnd.CodeCompletionContext;
			}
		}
		
		static bool forceSuggestionMode;
		public static bool ForceSuggestionMode {
			get { return forceSuggestionMode; }
			set {
				forceSuggestionMode = value; 
				if (wnd != null) {
					wnd.AutoCompleteEmptyMatch = wnd.AutoSelect = !forceSuggestionMode;
				}
			}
		}
		
		static CompletionWindowManager ()
		{
			if (IdeApp.Workbench != null)
				IdeApp.Workbench.RootWindow.Destroyed += (sender, e) => DestroyWindow ();
		}
		
		// ext may be null, but then parameter completion don't work
		public static bool ShowWindow (CompletionTextEditorExtension ext, char firstChar, ICompletionDataList list, ICompletionWidget completionWidget, CodeCompletionContext completionContext)
		{
			try {
				if (ext != null) {
					int inserted = ext.document.Editor.EnsureCaretIsNotVirtual ();
					if (inserted > 0)
						completionContext.TriggerOffset = ext.document.Editor.Caret.Offset;
				}
				if (wnd == null) {
					wnd = new CompletionListWindow ();
					wnd.WordCompleted += HandleWndWordCompleted;
				}
				wnd.Extension = ext;
				try {
					if (!wnd.ShowListWindow (firstChar, list, completionWidget, completionContext)) {
						if (list is IDisposable)
							((IDisposable)list).Dispose ();
						HideWindow ();
						return false;
					}
					
					if (ForceSuggestionMode)
						wnd.AutoSelect = false;
					wnd.Show ();
					DesktopService.RemoveWindowShadow (wnd);
					OnWindowShown (EventArgs.Empty);
					return true;
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
					return false;
				}
			} finally {
				ParameterInformationWindowManager.UpdateWindow (ext, completionWidget);
			}
		}

		static void HandleWndWordCompleted (object sender, CodeCompletionContextEventArgs e)
		{
			EventHandler<CodeCompletionContextEventArgs> handler = WordCompleted;
			if (handler != null)
				handler (sender, e);
		}
		
		public static event EventHandler<CodeCompletionContextEventArgs> WordCompleted;

		static void DestroyWindow ()
		{
			if (wnd != null) {
				wnd.Destroy ();
				wnd = null;
			}
			OnWindowClosed (EventArgs.Empty);
		}
		
		public static bool PreProcessKeyEvent (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			if (!IsVisible)
				return false;
			return wnd.PreProcessKeyEvent (key, keyChar, modifier);
		}

		public static void UpdateCursorPosition ()
		{
			if (!IsVisible) 
				return;
			if (wnd.CompletionWidget.CaretOffset < wnd.StartOffset)
				DestroyWindow ();

		}

		public static void UpdateWordSelection (string text)
		{
			if (IsVisible) {
				wnd.List.CompletionString = text;
				wnd.UpdateWordSelection ();
			}
		}

		public static void PostProcessKeyEvent (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			if (!IsVisible)
				return;
			wnd.PostProcessKeyEvent (key, keyChar, modifier);
		}
		
		public static void HideWindow ()
		{
			if (!IsVisible)
				return;
			ParameterInformationWindowManager.UpdateWindow (wnd.Extension, wnd.CompletionWidget);
			if (wnd.Extension != null)
				wnd.Extension.document.Editor.FixVirtualIndentation ();
//			wnd.HideWindow ();
//			OnWindowClosed (EventArgs.Empty);
			DestroyWindow ();
		}
		
		
		static void OnWindowClosed (EventArgs e)
		{
			var handler = WindowClosed;
			if (handler != null)
				handler (null, e);
		}

		public static event EventHandler WindowClosed;
		
		static void OnWindowShown (EventArgs e)
		{
			var handler = WindowShown;
			if (handler != null)
				handler (null, e);
		}
		
		public static event EventHandler WindowShown;
	}
}
