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
using Mono.TextEditor;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor.Wrappers
{
	class TooltipProviderWrapper : TooltipProvider, IDisposable
	{
		readonly MonoDevelop.Ide.Editor.TooltipProvider provider;
		TooltipItem lastWrappedItem;
		Ide.Editor.TooltipItem lastUnwrappedItem;

		public MonoDevelop.Ide.Editor.TooltipProvider OriginalProvider {
			get {
				return provider;
			}
		}

		public TooltipProviderWrapper (MonoDevelop.Ide.Editor.TooltipProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException ("provider");
			this.provider = provider;
		}

		#region implemented abstract members of TooltipProvider

		static MonoDevelop.Ide.Editor.TextEditor WrapEditor (MonoTextEditor editor)
		{
			foreach (var doc in IdeApp.Workbench.Documents) {
				if (doc.FileName == editor.FileName)
					return doc.Editor;
			}
			return null;
		}

		public override TooltipItem GetItem (MonoTextEditor editor, int offset)
		{
			var item = provider.GetItem (WrapEditor (editor), IdeApp.Workbench.ActiveDocument, offset);
			if (item == null)
				return null;
			if (lastUnwrappedItem != null) {
				if (lastUnwrappedItem.Offset == item.Offset &&
					lastUnwrappedItem.Length == item.Length &&
					lastUnwrappedItem.Item.Equals (item.Item)) {
					return lastWrappedItem;
				}
			}
			lastUnwrappedItem = item;
			return lastWrappedItem = new TooltipItem (item.Item, item.Offset, item.Length);
		}

		public override bool IsInteractive (MonoTextEditor editor, Gtk.Window tipWindow)
		{
			var wrappedEditor = WrapEditor (editor);
			if (wrappedEditor == null)
				return false;
			return provider.IsInteractive (wrappedEditor, tipWindow);
		}

		public override Gtk.Window CreateTooltipWindow (MonoTextEditor editor, int offset, Gdk.ModifierType modifierState, TooltipItem item)
		{
			var wrappedEditor = WrapEditor (editor);
			if (wrappedEditor == null)
				return null;
			var control = provider.CreateTooltipWindow (wrappedEditor, IdeApp.Workbench.ActiveDocument, new MonoDevelop.Ide.Editor.TooltipItem (item.Item, item.ItemSegment.Offset, item.ItemSegment.Length), offset, modifierState);
			if (control == null)
				return null;
			return (Gtk.Window)control;
		}

		protected override void GetRequiredPosition (MonoTextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			var wrappedEditor = WrapEditor (editor);
			if (wrappedEditor == null) {
				requiredWidth = 0;
				xalign = 0;
				return;
			}
			provider.GetRequiredPosition (wrappedEditor, tipWindow, out requiredWidth, out xalign);
		}

		public override Gtk.Window ShowTooltipWindow (MonoTextEditor editor, Gtk.Window tipWindow, int offset, Gdk.ModifierType modifierState, int mouseX, int mouseY, TooltipItem item)
		{
			var wrappedEditor = WrapEditor (editor);
			if (wrappedEditor == null) {
				return tipWindow;
			}
			provider.ShowTooltipWindow (wrappedEditor, tipWindow, new MonoDevelop.Ide.Editor.TooltipItem (item.Item, item.ItemSegment.Offset, item.ItemSegment.Length), modifierState, mouseX, mouseY);
			return tipWindow;
		}

		public void Dispose ()
		{
			var disposableProvider = provider as IDisposable;
			if (disposableProvider != null) {
				disposableProvider.Dispose ();
			}
			lastWrappedItem = null;
			lastUnwrappedItem = null;
		}
		#endregion
	}
}