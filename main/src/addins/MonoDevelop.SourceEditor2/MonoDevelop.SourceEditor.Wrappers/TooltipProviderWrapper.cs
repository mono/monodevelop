//
// TooltipProviderWrapper.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using Mono.TextEditor;
using MonoDevelop.Ide;
using Xwt.GtkBackend;
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor.Wrappers
{
	class TooltipProviderWrapper : TooltipProvider, IDisposable
	{
		readonly MonoDevelop.Ide.Editor.TooltipProvider provider;
		Ide.Editor.TooltipItem lastItem;

		public MonoDevelop.Ide.Editor.TooltipProvider OriginalProvider {
			get {
				return provider;
			}
		}

		public TooltipProviderWrapper (MonoDevelop.Ide.Editor.TooltipProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException (nameof (provider));
			this.provider = provider;
		}

		#region implemented abstract members of TooltipProvider

		static MonoDevelop.Ide.Editor.TextEditor WrapEditor (MonoTextEditor editor)
		{
			foreach (var doc in IdeApp.Workbench.Documents) {
				var textEditor = doc.Editor;
				if (textEditor == null)
					continue;
				if (textEditor.FileName == editor.FileName)
					return textEditor;
			}
			return null;
		}

		public override async Task<MonoDevelop.Ide.Editor.TooltipItem> GetItem (MonoTextEditor editor, int offset, CancellationToken token = default(CancellationToken))
		{
			var wrappedEditor = WrapEditor (editor);
			if (wrappedEditor == null)
				return null;
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return null;
			var task = provider.GetItem (wrappedEditor, doc, offset, token);
			if (task == null) {
				LoggingService.LogWarning ("Tooltip provider " + provider + " gave back null on GetItem (should always return a non null task).");
				return null;
			}
			var item = await task;
			if (item == null)
				return null;
			if (lastItem != null) {
				if (lastItem.Offset == item.Offset &&
					lastItem.Length == item.Length &&
					lastItem.Item.Equals (item.Item)) {
					return lastItem;
				}
			}
			lastItem = item;
			return item;
		}

		public override bool IsInteractive (MonoTextEditor editor, Xwt.WindowFrame tipWindow)
		{
			var wrappedEditor = WrapEditor (editor);
			if (wrappedEditor == null)
				return false;
			return provider.IsInteractive (wrappedEditor, tipWindow);
		}

		static bool IsMouseOver (Xwt.WindowFrame tipWidget)
		{
			var mousePosition = Xwt.Desktop.MouseLocation;
			return tipWidget.ScreenBounds.Contains (mousePosition);
		}

		public override void TakeMouseControl (MonoTextEditor editor, Xwt.WindowFrame tipWindow)
		{
			if (!IsMouseOver (tipWindow))
				editor.TextArea.HideTooltip ();
			var wrappedEditor = WrapEditor (editor);
			if (wrappedEditor == null)
				return;
			provider.TakeMouseControl (wrappedEditor, tipWindow);
		}

		public override Xwt.WindowFrame CreateTooltipWindow (MonoTextEditor editor, int offset, Gdk.ModifierType modifierState, Ide.Editor.TooltipItem item)
		{
			var wrappedEditor = WrapEditor (editor);
			if (wrappedEditor == null)
				return null;
			var control = provider.CreateTooltipWindow (wrappedEditor, IdeApp.Workbench.ActiveDocument, item, offset, modifierState.ToXwtValue ());
			if (control == null)
				return null;
			return control;
		}

		protected override void GetRequiredPosition (MonoTextEditor editor, Xwt.WindowFrame tipWindow, out int requiredWidth, out double xalign)
		{
			var wrappedEditor = WrapEditor (editor);
			if (wrappedEditor == null) {
				requiredWidth = 0;
				xalign = 0;
				return;
			}
			provider.GetRequiredPosition (wrappedEditor, tipWindow, out requiredWidth, out xalign);
		}

		public override Xwt.WindowFrame ShowTooltipWindow (MonoTextEditor editor, Xwt.WindowFrame tipWindow, int offset, Gdk.ModifierType modifierState, int mouseX, int mouseY, Ide.Editor.TooltipItem item)
		{
			var wrappedEditor = WrapEditor (editor);
			if (wrappedEditor == null) {
				return tipWindow;
			}
			provider.ShowTooltipWindow (wrappedEditor, tipWindow, item, modifierState.ToXwtValue (), mouseX, mouseY);
			return tipWindow;
		}

		public void Dispose ()
		{
			var disposableProvider = provider as IDisposable;
			if (disposableProvider != null) {
				disposableProvider.Dispose ();
			}
			lastItem = null;
		}

		public override bool TryCloseTooltipWindow (Xwt.WindowFrame tipWindow, Ide.Editor.TooltipCloseReason reason)
		{
			return provider.TryCloseTooltipWindow (tipWindow, reason);
		}
		#endregion
	}
}