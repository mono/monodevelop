//
// MdMouseProcessorProvider.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.ComponentModel.Composition;

using AppKit;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.TextEditor.Cocoa
{
	[Export (typeof (ICocoaMouseProcessorProvider))]
	[Name ("VisualStudioMouseProcessor")]
	[Order (Before = "WordSelection")]
	[ContentType ("Text")]
	[TextViewRole ("INTERACTIVE")]
	sealed class MdMouseProcessorProvider : ICocoaMouseProcessorProvider
	{
		[Import]
		public IEditorCommandHandlerServiceFactory CommandServiceFactory { get; private set; }

		public ICocoaMouseProcessor GetAssociatedProcessor (ICocoaTextView cocoaTextView)
			=> new MdMouseProcessor (
				CommandServiceFactory.GetService (cocoaTextView),
				cocoaTextView);
	}

	sealed class MdMouseProcessor : CocoaMouseProcessorBase
	{
		const string menuPath = "/MonoDevelop/TextEditor/ContextMenu/Editor";

		readonly IEditorCommandHandlerService commandServiceFactory;
		readonly ICocoaTextView textView;

		public MdMouseProcessor (
			IEditorCommandHandlerService commandServiceFactory,
			ICocoaTextView textView)
		{
			this.commandServiceFactory = commandServiceFactory
				?? throw new ArgumentNullException (nameof (commandServiceFactory));

			this.textView = textView
				?? throw new ArgumentNullException (nameof (textView));
		}

		public override void PreprocessMouseRightButtonDown (MouseEventArgs e)
			=> textView.MoveCaretToPosition (e.Event);

		public override void PreprocessMouseRightButtonUp (MouseEventArgs e)
		{
			var controller = (DocumentController)textView.Properties [typeof (DocumentController)];
			var extensionContext = controller.ExtensionContext;
			var commandEntrySet = IdeApp.CommandService.CreateCommandEntrySet (extensionContext, menuPath);

			var menuPosition = textView.GetViewRelativeMousePosition (e.Event);

			IdeApp.CommandService.ShowContextMenu (
				textView.VisualElement,
				(int)menuPosition.X,
				(int)menuPosition.Y,
				commandEntrySet,
				controller);
		}
	}
}