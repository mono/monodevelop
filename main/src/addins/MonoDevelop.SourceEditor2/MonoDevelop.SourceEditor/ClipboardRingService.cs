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

using Gtk;

using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor
{
	static class ClipboardRingService
	{
		const int clipboardRingSize = 20;
		const int toolboxNameMaxLength = 250;
		static readonly List<TextToolboxNode> clipboardRing = new List<TextToolboxNode> ();

		public static event EventHandler Updated;

		static ClipboardRingService ()
		{
			ClipboardActions.CopyOperation.Copy += AddToClipboardRing;
		}

		static void AddToClipboardRing (string text)
		{
			if (string.IsNullOrEmpty (text))
				return;

			//if already in ring, grab the existing node
			TextToolboxNode newNode = null;
			foreach (var node in clipboardRing) {
				if (node.Text == text) {
					clipboardRing.Remove (node);
					newNode = node;
					break;
				}
			}

			clipboardRing.Add (newNode ?? CreateClipboardToolboxItem (text));

			while (clipboardRing.Count > clipboardRingSize) {
				clipboardRing.RemoveAt (0);
			}

			Updated?.Invoke (null, EventArgs.Empty);
		}

		static TextToolboxNode CreateClipboardToolboxItem (string text)
		{
			var item = new TextToolboxNode (text) {
				Category = GettextCatalog.GetString ("Clipboard Ring"),
				Icon = DesktopService.GetIconForFile ("a.txt", IconSize.Menu),
				Name = EscapeAndTruncateName (text)
			};

			string [] lines = text.Split ('\n');
			for (int i = 0; i < 3 && i < lines.Length; i++) {
				if (i > 0)
					item.Description += Environment.NewLine;
				string line = lines [i];
				if (line.Length > 16)
					line = line.Substring (0, 16) + "...";
				item.Description += line;
			}

			return item;
		}

		static string EscapeAndTruncateName (string text)
		{
			var sb = StringBuilderCache.Allocate ();
			foreach (char ch in text) {
				switch (ch) {
				case '\t': sb.Append ("\\t"); break;
				case '\r': sb.Append ("\\r"); break;
				case '\n': sb.Append ("\\n"); break;
				default: sb.Append (ch); break;
				}
				if (sb.Length >= toolboxNameMaxLength) {
					break;
				}
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}

		public static IEnumerable<ItemToolboxNode> GetToolboxItems ()
		{
			return clipboardRing;
		}
	}
}
