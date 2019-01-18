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

using System.ComponentModel.Composition;
using AppKit;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Ide;

namespace MonoDevelop.TextEditor.Cocoa
{
	[Export (typeof (ICocoaMouseProcessorProvider))]
	[Name ("VisualStudioMouseProcessor")]
	[Order (Before = "WordSelection")]
	[ContentType ("Text")]
	[TextViewRole ("INTERACTIVE")]
	class MdMouseProcessorProvider : ICocoaMouseProcessorProvider
	{
		[Import]
		public ITextDocumentFactoryService TextDocumentFactory { get; private set; }

		[Import]
		public IEditorCommandHandlerServiceFactory CommandServiceFactory { get; private set; }

		public ICocoaMouseProcessor GetAssociatedProcessor (ICocoaTextView cocoaTextView)
		{
			return new MdMouseProcessor (CommandServiceFactory.GetService(cocoaTextView), cocoaTextView);
		}
	}

	class MdMouseProcessor : CocoaMouseProcessorBase
	{
		ICocoaTextView cocoaTextView;
		private readonly IEditorCommandHandlerService commandServiceFactory;
		readonly string menuPath = "/MonoDevelop/TextEditor/ContextMenu/Editor";

		public MdMouseProcessor (IEditorCommandHandlerService commandServiceFactory, ICocoaTextView cocoaTextView)
		{
			this.cocoaTextView = cocoaTextView;
			this.commandServiceFactory = commandServiceFactory;
		}

		public override void PreprocessMouseRightButtonUp (MouseEvent e)
		{
			var view = (CocoaTextViewContent)cocoaTextView.Properties [typeof (CocoaTextViewContent)];
			var ctx = view.WorkbenchWindow?.ExtensionContext ?? Mono.Addins.AddinManager.AddinEngine;
			var cset = IdeApp.CommandService.CreateCommandEntrySet (ctx, menuPath);
			var pt = ((NSEvent)e.Event).LocationInWindow;
			pt = cocoaTextView.VisualElement.ConvertPointFromView (pt, null);
			IdeApp.CommandService.ShowContextMenu (cocoaTextView.VisualElement, (int)pt.X, (int)pt.Y, cset, view);
		}
	}
}
