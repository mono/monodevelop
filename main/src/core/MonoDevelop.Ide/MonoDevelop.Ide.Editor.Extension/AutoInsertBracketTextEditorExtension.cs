//
// AutoInsertBracketTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using Mono.Addins;
using MonoDevelop.Ide.Extensions;

namespace MonoDevelop.Ide.Editor.Extension
{
	public abstract class AutoInsertBracketHandler
	{
		public virtual bool CanHandle (TextEditor editor) => true;
		public abstract bool Handle (TextEditor editor, DocumentContext ctx, KeyDescriptor descriptor);
	}

	class AutoInsertBracketTextEditorExtension : TextEditorExtension
	{
		const string extensionPoint = "/MonoDevelop/Ide/AutoInsertBracketHandler";
		bool isEnabled;
		List<AutoInsertBracketHandler> handlers;

		protected override void Initialize ()
		{
			base.Initialize ();

			SetEnabled (DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket);
		}

		void SetEnabled (bool enable)
		{
			if (isEnabled == enable) {
				return;
			}
			if (enable) {
				Editor.MimeTypeChanged += UpdateHandlers;
				Editor.ExtensionContext.ExtensionChanged += UpdateHandlers;
				UpdateHandlers (null, null);
			} else {
				Editor.MimeTypeChanged -= UpdateHandlers;
				Editor.ExtensionContext.ExtensionChanged -= UpdateHandlers;
				handlers = null;
			}
			isEnabled = enable;
		}

		void UpdateHandlers (object sender, EventArgs e)
		{
			//whenever the mimetype or the extension context has changed, refilter handlers
			handlers = new List<AutoInsertBracketHandler> (
				GetEditorExtensions (Editor, extensionPoint)
				.Select (ext => (AutoInsertBracketHandler) ext.CreateInstance ())
			);
		}

		public override void Dispose ()
		{
			SetEnabled (false);
			base.Dispose ();
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			var result = base.KeyPress (descriptor);

			// if the AutoInsertMatchingBracket option was changed since initialization
			if (DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket != isEnabled) {
				SetEnabled (DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket);
			}

			if (isEnabled && !Editor.IsSomethingSelected) {
				foreach (var handler in handlers) {
					if (handler.CanHandle (Editor)) {
						handler.Handle (Editor, DocumentContext, descriptor);
						break;
					}
				}
			}

			return result;
		}

		//returns all valid extensions in order of most to least specific
		static IEnumerable<TextEditorExtensionNode> GetEditorExtensions (TextEditor editor, string extensionPoint)
		{
			var returned = new HashSet<TextEditorExtensionNode> ();

			//get the nodes from the extensioncontext rather than the addinmanager
			//so that custom conditions supported by the editor are respected
			var nodes = editor.ExtensionContext.GetExtensionNodes<TextEditorExtensionNode> (extensionPoint);

			//file extensions are cheaper to check than mimetypes, check them first
			//this means exact file extensions take precedence over mimetypes regardless
			//of node ordering but it's not a big deal
			var extension = string.IsNullOrEmpty (editor.FileName) ? null : Path.GetExtension (editor.FileName);
			foreach (var node in nodes) {
				if (MatchAny (extension, node.FileExtensions)) {
					returned.Add (node);
					yield return node;
				}
			}

			//check mimetypes, from most to least specific
			var mimeChain = DesktopService.GetMimeTypeInheritanceChain (editor.MimeType);
			foreach (var mime in mimeChain) {
				foreach (var node in nodes) {
					if (!returned.Contains (node) && MatchAny (mime, node.MimeTypes)) {
						returned.Add (node);
						yield return node;
					}
				}
			}

			//finally, return any remaining nodes that don't have restrictions at all
			foreach (var node in nodes) {
				if (!returned.Contains (node) && NullOrEmpty (node.MimeTypes) && NullOrEmpty (node.FileExtensions)) {
					yield return node;
				}
			}

			bool MatchAny (string value, string [] matches) =>
				matches != null && matches.Length > 0
					&& matches.Any (m => string.Equals (m, value, StringComparison.OrdinalIgnoreCase));

			bool NullOrEmpty (string [] arr) => arr == null || arr.Length == 0;
		}
	}
}

