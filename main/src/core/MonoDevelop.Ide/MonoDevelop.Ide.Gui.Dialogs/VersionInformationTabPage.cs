// VersionInformationTabPage.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//   Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2009 RemObjects Software
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
//
//

using System;
using Gtk;
using MonoDevelop.Core;
using System.Reflection;
using System.Text;
using System.IO;
using MonoDevelop.Ide.Fonts;
using Mono.Addins;


namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class VersionInformationTabPage: VBox
	{
		bool destroyed;
		
		public VersionInformationTabPage ()
		{
			SetLabel (GettextCatalog.GetString ("Loading..."));
			
			new System.Threading.Thread (() => {
				try {
					var text = SystemInformation.ToText ();
					Gtk.Application.Invoke (delegate {
						if (destroyed)
							return;
						SetText (text);
					});
				} catch (Exception ex) {
					LoggingService.LogError ("Failed to load version information", ex);
					Gtk.Application.Invoke (delegate {
						if (destroyed)
							return;
						SetLabel (GettextCatalog.GetString ("Failed to load version information."));
					});
				}
			}).Start ();
		}
		
		void Clear ()
		{
			foreach (var c in this.Children) {
				this.Remove (c);
			}
		}
		
		void SetLabel (string text)
		{
			Clear ();
			var label = new Gtk.Label (text);
			PackStart (label, true, true, 0);
			ShowAll ();
		}

		void SetText (string text)
		{
			Clear ();
			var buf = new TextBuffer (null);
			buf.Text = text;
			
			var sw = new MonoDevelop.Components.CompactScrolledWindow () {
				ShowBorderLine = true,
				BorderWidth = 2,
				Child = new TextView (buf) {
					Editable = false,
					LeftMargin = 4,
					RightMargin = 4,
					PixelsAboveLines = 4,
					PixelsBelowLines = 4
				}
			};
			
			sw.Child.ModifyFont (Pango.FontDescription.FromString (DesktopService.DefaultMonospaceFont));
			PackStart (sw, true, true, 0);
			var hb = new HBox (false, 0) {
				BorderWidth = 2,
			};
			var copyButton = new Button () { Label = GettextCatalog.GetString ("Copy Version Information") };
			copyButton.Clicked += (sender, e) => CopyBufferToClipboard (buf);
			hb.PackStart (copyButton, true, true, 0);
			PackEnd (hb, false, false, 0);
			ShowAll ();
		}

		static void CopyBufferToClipboard (TextBuffer buf)
		{
			//get current cursor state
			TextIter s, e;
			TextIter cursorIter = TextIter.Zero;
			var hadSel = buf.GetSelectionBounds (out s, out e);
			if (!hadSel) {
				cursorIter = buf.GetIterAtOffset (buf.CursorPosition);
			}

			//copy text to clipboard, let the buffer handle the details
			buf.SelectRange (buf.StartIter, buf.EndIter);
			Clipboard clipboard = Clipboard.Get (Mono.TextEditor.ClipboardActions.CopyOperation.CLIPBOARD_ATOM);
			buf.CopyClipboard (clipboard);

			//restore cursor state
			if (hadSel) {
				buf.SelectRange (s, e);
			} else {
				buf.PlaceCursor (cursorIter);
			}
		}
		
		public override void Destroy ()
		{
			base.Destroy ();
			destroyed = true;
		}
	}
}
