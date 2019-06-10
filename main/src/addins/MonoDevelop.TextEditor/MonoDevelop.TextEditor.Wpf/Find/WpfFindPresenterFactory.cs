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
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Find;
using Microsoft.VisualStudio.Text.Find.Implementation;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.TextEditor.Wpf.Find
{
	[Export (typeof (IFindPresenterFactory))]
	class WpfFindPresenterFactory : IFindPresenterFactory
	{
		[Import]
		public FindBroker FindBroker { get; set; }

		[Import]
		public IEditorCommandHandlerServiceFactory EditorCommandHandlerServiceFactory { get; set; }

		public IFindPresenter TryGetFindPresenter (ITextView textView)
		{
			return textView.Properties.GetOrCreateSingletonProperty<IFindPresenter> (() => 
			{
				var presenter = new WpfFindPresenter (this, textView);
				return presenter;
			});
		}

		[Export]
		[Name (FindUI.FindUIAdornmentLayer)]
		[Order (After = PredefinedAdornmentLayers.Caret)]
		[IsOverlayLayer (true)]
		private AdornmentLayerDefinition adornmentLayerDefinition;
	}
}