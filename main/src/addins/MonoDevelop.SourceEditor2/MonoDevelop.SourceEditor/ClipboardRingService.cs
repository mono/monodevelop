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
using System.ComponentModel;

using Gtk;

using Mono.TextEditor;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.SourceEditor
{
	static class ClipboardRingService
	{
		const int clipboardRingItemMaxChars = 1024 * 1024 / 2; // 1MB
		const int clipboardRingSize = 20;
		const int toolboxNameMaxLength = 250;

		static readonly List<ClipboardToolboxNode> clipboardRing = new List<ClipboardToolboxNode> ();

		public static event EventHandler Updated;

		static ClipboardRingService ()
		{
			ClipboardActions.CopyOperation.Copy += AddToClipboardRing;
		}

		static void AddToClipboardRing (string text)
		{
			if (string.IsNullOrEmpty (text))
				return;

			// if too big, just reject it, truncation is unlikely to be useful
			if (text.Length > clipboardRingItemMaxChars) {
				LoggingService.LogWarning ($"Item '{EscapeAndTruncateName(text, 20)}...' exceeeds clipboard ring size limit");
				return;
			}

			//if already in ring, grab the existing node
			ClipboardToolboxNode newNode = null;
			foreach (var node in clipboardRing) {
				if (node.Text == text) {
					clipboardRing.Remove (node);
					newNode = node;
					break;
				}
			}

			clipboardRing.Add (newNode ?? new ClipboardToolboxNode (text));

			int overflow = clipboardRing.Count - clipboardRingSize;
			if (overflow > 0) {
				clipboardRing.RemoveRange (0, overflow);
			}

			Updated?.Invoke (null, EventArgs.Empty);
		}

		static string EscapeAndTruncateName (string text, int truncateAt)
		{
			var sb = StringBuilderCache.Allocate ();
			foreach (char ch in text) {
				switch (ch) {
				case '\t': sb.Append ("\\t"); break;
				case '\r': sb.Append ("\\r"); break;
				case '\n': sb.Append ("\\n"); break;
				default: sb.Append (ch); break;
				}
				if (sb.Length >= truncateAt) {
					break;
				}
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}

		public static IEnumerable<ItemToolboxNode> GetToolboxItems ()
		{
			return clipboardRing;
		}

		class ClipboardToolboxNode : ItemToolboxNode, ITextToolboxNode, ICustomTooltipToolboxNode
		{
			static readonly ToolboxItemFilterAttribute filterAtt = new ToolboxItemFilterAttribute ("text/plain", ToolboxItemFilterType.Allow);
			static readonly string category = GettextCatalog.GetString ("Clipboard Ring");
			static readonly Xwt.Drawing.Image icon = DesktopService.GetIconForFile ("a.txt", IconSize.Menu);

			public ClipboardToolboxNode (string text)
			{
				Text = text;

				ItemFilters.Add (filterAtt);
				Category = category;
				Icon = icon;
			}

			public string Text { get; private set; }

			public override string Name {
				get {
					return base.Name.Length > 0 ? base.Name : (base.Name = EscapeAndTruncateName (Text, toolboxNameMaxLength));
				}
				set => base.Name = value;
			}

			public override bool Filter (string keyword)
			{
				return Text.IndexOf (keyword, StringComparison.InvariantCultureIgnoreCase) >= 0;
			}

			public string GetDragPreview (Document document)
			{
				return Text;
			}

			public void InsertAtCaret (Document document)
			{
				document.Editor.InsertAtCaret (Text);
			}

			public bool IsCompatibleWith (Document document)
			{
				return true;
			}

			public bool HasTooltip => true;

			public Components.Window CreateTooltipWindow (Control parent)
			{
				//large amounts of text are expensive to lay out, and the preview window
				//doesn't need all that much anyway
				var txt = Text;
				if (txt.Length > 4096) {
					txt = txt.Substring (0, 4096) + "...";
				}

				var w = parent.GetNativeWidget<Widget> ()?.GdkWindow;
				return new CodePreviewWindow (w) { Text = Text };
			}
		}
	}
}
