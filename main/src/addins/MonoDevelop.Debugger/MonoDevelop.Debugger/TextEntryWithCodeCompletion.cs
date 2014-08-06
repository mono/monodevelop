//
// TextEntryWithCodeCompletion.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
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
using Xwt;
using MonoDevelop.Ide.CodeCompletion;
using System.Collections.Generic;

namespace MonoDevelop.Debugger
{
	class TextEntryWithCodeCompletion : TextEntry, ICompletionWidget
	{
		CodeCompletionContext ctx;
		Gtk.Entry gtkEntry;

		Gdk.ModifierType modifier;
		bool keyHandled = false;
		uint keyValue;
		char keyChar;
		Gdk.Key key;

		public TextEntryWithCodeCompletion ()
		{
			gtkEntry = Xwt.Toolkit.CurrentEngine.GetNativeWidget (this) as Gtk.Entry;
			if (gtkEntry == null)
				throw new NotImplementedException ();
			gtkEntry.KeyReleaseEvent += HandleKeyReleaseEvent;
			gtkEntry.KeyPressEvent += HandleKeyPressEvent;
			CompletionWindowManager.WindowClosed += HandleWindowClosed;
		}

		void HandleWindowClosed (object sender, EventArgs e)
		{
			ctx = null;
			if (CompletionContextChanged != null)
				CompletionContextChanged (this, EventArgs.Empty);
		}

		[GLib.ConnectBeforeAttribute]
		void HandleKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			keyHandled = false;

			keyChar = (char)args.Event.Key;
			keyValue = args.Event.KeyValue;
			modifier = args.Event.State;
			key = args.Event.Key;

			if ((args.Event.Key == Gdk.Key.Down || args.Event.Key == Gdk.Key.Up)) {
				keyChar = '\0';
			}

			if (list != null)
				args.RetVal = keyHandled = CompletionWindowManager.PreProcessKeyEvent (key, keyChar, modifier);
		}

		void HandleKeyReleaseEvent (object o, Gtk.KeyReleaseEventArgs args)
		{
			if (keyHandled)
				return;

			string text = ctx == null ? Text : Text.Substring (Math.Max (0, Math.Min (ctx.TriggerOffset, Text.Length)));
			CompletionWindowManager.UpdateWordSelection (text);
			CompletionWindowManager.PostProcessKeyEvent (key, keyChar, modifier);
			PopupCompletion ();
		}

		void PopupCompletion ()
		{
			char c = (char)Gdk.Keyval.ToUnicode (keyValue);
			if (ctx == null) {
				ctx = ((ICompletionWidget)this).CreateCodeCompletionContext (0);
				CompletionWindowManager.ShowWindow (null, c, list, this, ctx);
				if (CompletionContextChanged != null)
					CompletionContextChanged (this, EventArgs.Empty);
			}
		}

		CompletionDataList list;

		public void SetCodeCompletionList (IList<string> list)
		{
			this.list = new CompletionDataList ();
			foreach (var l in list)
				this.list.Add (l);
			this.list.DefaultCompletionString = "System.Exception";
			this.list.AddKeyHandler (new NullDotKeyHandler ());
		}

		/// <summary>
		/// Prevents typing '.' inserting(confirming/finishing) code completion
		/// </summary>
		class NullDotKeyHandler : ICompletionKeyHandler
		{
			#region ICompletionKeyHandler implementation

			public bool PreProcessKey (CompletionListWindow listWindow, Gdk.Key key, char keyChar, Gdk.ModifierType modifier, out KeyActions keyAction)
			{
				keyAction = KeyActions.None;
				if (keyChar == '.') {
					return true;
				}
				return false;
			}

			public bool PostProcessKey (CompletionListWindow listWindow, Gdk.Key key, char keyChar, Gdk.ModifierType modifier, out KeyActions keyAction)
			{
				keyAction = KeyActions.None;
				if (keyChar == '.') {
					return true;
				}
				return false;
			}

			#endregion
		}

		#region ICompletionWidget implementation

		public event EventHandler CompletionContextChanged;

		public string GetText (int startOffset, int endOffset)
		{
			if (startOffset < 0 || startOffset > Text.Length)
				startOffset = 0;
			if (endOffset > Text.Length)
				endOffset = Text.Length;
			return Text.Substring (startOffset, endOffset - startOffset);
		}

		public char GetChar (int offset)
		{
			if (offset >= Text.Length)
				return (char)0;
			else
				return Text [offset];
		}

		protected override void OnLostFocus (EventArgs args)
		{
			base.OnLostFocus (args);
			CompletionWindowManager.HideWindow ();
		}

		public void Replace (int offset, int count, string text)
		{
			if (count > 0)
				Text = Text.Remove (offset, count);
			if (!string.IsNullOrEmpty (text))
				Text = Text.Insert (offset, text);
		}

		public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset)
		{
			CodeCompletionContext c = new CodeCompletionContext ();
			c.TriggerLine = 0;
			c.TriggerOffset = triggerOffset;
			c.TriggerLineOffset = c.TriggerOffset;
			c.TriggerTextHeight = gtkEntry.SizeRequest ().Height;
			c.TriggerWordLength = CaretOffset;

			var location = this.ConvertToScreenCoordinates (new Point (0, c.TriggerTextHeight));
			c.TriggerXCoord = (int)location.X;
			c.TriggerYCoord = (int)location.Y;
			return c;
		}

		public string GetCompletionText (CodeCompletionContext ctx)
		{
			return Text.Substring (ctx.TriggerOffset, ctx.TriggerWordLength);
		}

		public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
		{
			Text = complete_word;
			gtkEntry.Position = complete_word.Length;
		}

		public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int completeWordOffset)
		{
			Text = complete_word;
			gtkEntry.Position = complete_word.Length;
		}

		public CodeCompletionContext CurrentCodeCompletionContext {
			get {
				return CreateCodeCompletionContext (CaretOffset);
			}
		}

		public int CaretOffset {
			get {
				return gtkEntry.Position;
			}
		}

		public int TextLength {
			get {
				return Text.Length;
			}
		}

		public int SelectedLength {
			get {
				return 0;
			}
		}

		public Gtk.Style GtkStyle {
			get {
				return gtkEntry.Style;
			}
		}

		#endregion
	}
}

