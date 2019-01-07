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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if MAC
using AppKit;
#endif
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Text
{
#if WINDOWS
	partial class TextViewContent : AbstractXwtViewContent
#elif MAC
	partial class TextViewContent : ViewContent
#endif
	{
		TextViewImports imports;
		FilePath fileName;
		string mimeType;
		Project ownerProject;
#if WINDOWS
		RootWpfWidget widget;
		Xwt.Widget xwtWidget;
		public IWpfTextView TextView { get; private set; }
		private IWpfTextViewHost textViewHost;
#elif MAC
		private ICocoaTextViewHost textViewHost;
		public ICocoaTextView TextView { get; private set; }
#endif
		public ITextDocument TextDocument { get; }
		public ITextBuffer TextBuffer { get; }
		private IEditorCommandHandlerService _editorCommandHandlerService;
		List<IEditorContentProvider> contentProviders;

		public TextViewContent (TextViewImports imports, FilePath fileName, string mimeType, Project ownerProject)
		{
			this.imports = imports;
			this.fileName = fileName;
			this.mimeType = mimeType;
			this.ownerProject = ownerProject;

			//TODO: HACK, this needs to be moved elsewhere and updated when MonoDevelop settings change.
			imports.EditorOptionsFactoryService.GlobalOptions.SetOptionValue (
				DefaultTextViewHostOptions.LineNumberMarginId,
				Editor.DefaultSourceEditorOptions.Instance.ShowLineNumberMargin);

			var contentType = (mimeType == null) ? imports.TextBufferFactoryService.InertContentType : GetContentTypeFromMimeType (fileName, mimeType);

			TextDocument = imports.TextDocumentFactoryService.CreateAndLoadTextDocument (fileName, contentType);
			TextBuffer = TextDocument.TextBuffer;
			TextDocument.DirtyStateChanged += OnTextDocumentDirtyStateChanged;
#if WINDOWS
			var control = CreateControl (imports);
			this.widget = new RootWpfWidget (control);
			widget.HeightRequest = 50;
			widget.WidthRequest = 100;

			TextView.VisualElement.Tag = widget;
			TextView.VisualElement.LostKeyboardFocus += (s, e) => {
				Components.Commands.CommandManager.LastFocusedWpfElement = TextView.VisualElement;
			};

			this.xwtWidget = GetXwtWidget (widget);
			xwtWidget.Show ();
#elif MAC
			control = new EmbeddedNSViewControl (CreateControl ());
#endif
			ContentName = fileName;
			TextView.Properties [typeof (Ide.Text.TextViewContent)] = this;
			_editorCommandHandlerService = imports.EditorCommandHandlerServiceFactory.GetService (TextView);

			contentProviders = imports.EditorContentProviderService.GetContentProvidersForView (TextView).ToList ();
		}

		protected override object OnGetContent (Type type)
		{
			foreach (var provider in contentProviders) {
				var content = provider.GetContent (TextView, type);
				if (content != null) {
					return content;
				}
			}
			return GetIntrinsicType (type);
		}

		protected override IEnumerable<object> OnGetContents (Type type)
		{
			foreach (var provider in contentProviders) {
				var contents = provider.GetContents (TextView, type);
				if (contents != null) {
					foreach (var content in contents) {
						yield return content;

					}
				}
			}
			var intrinsicType = GetIntrinsicType (type);
			if (intrinsicType != null) {
				yield return intrinsicType;
			}
		}

		object GetIntrinsicType (Type type)
		{
			if (type.IsInstanceOfType (TextBuffer))
				return TextBuffer;
			if (type.IsInstanceOfType (TextDocument))
				return TextDocument;
			if (type.IsInstanceOfType (TextView))
				return TextView;
			if (type.IsInstanceOfType (this))
				return this;
			return null;
		}

		public override Task Save ()
		{
			TextDocument.Save ();
			return Task.CompletedTask;
		}

		public override Task Save (FileSaveInformation fileSaveInformation)
		{
			TextDocument.SaveAs (fileSaveInformation.FileName, overwrite: true);
			return Task.CompletedTask;
		}

		public override bool IsDirty 
		{
			get => TextDocument.IsDirty;
		}

		private void OnTextDocumentDirtyStateChanged (object sender, EventArgs e)
		{
			OnDirtyChanged ();
		}

		static readonly string[] textContentType = { "text" };

		private IContentType GetContentTypeFromMimeType (string filePath, string mimeType)
		{
			if (filePath != null) {
				var contentTypeFromPath = imports.FileToContentTypeService.GetContentTypeForFilePath (filePath);
				if (contentTypeFromPath != null &&
					contentTypeFromPath != imports.ContentTypeRegistryService.UnknownContentType) {
					return contentTypeFromPath;
				}
			}

			IContentType contentType = imports.MimeToContentTypeRegistryService.GetContentType (mimeType);
			if (contentType == null) {
				// fallback 1: see if there is a content tyhpe with the same name
				contentType = imports.ContentTypeRegistryService.GetContentType (mimeType);
				if (contentType == null) {
					// No joy, create a content type that, by default, derives from text. This is strictly an error
					// (there should be mappings between any mime type and any content type).
					contentType = imports.ContentTypeRegistryService.AddContentType (mimeType, textContentType);
				}
			}

			return contentType;
		}

#if WINDOWS
		private Widget GetXwtWidget (RootWpfWidget widget)
		{
			return Xwt.Toolkit.CurrentEngine.WrapWidget (widget, NativeWidgetSizing.External);
		}

		private System.Windows.Controls.Control CreateControl (TextViewImports imports)
		{
			var roles = imports.TextEditorFactoryService.AllPredefinedRoles;
			ITextDataModel dataModel = new VacuousTextDataModel (TextBuffer);
			ITextViewModel viewModel = UIExtensionSelector.InvokeBestMatchingFactory(
				imports.TextViewModelProviders,
				dataModel.ContentType,
				roles,
				(provider) => (provider.CreateTextViewModel (dataModel, roles)),
				imports.ContentTypeRegistryService,
				imports.GuardedOperations,
				this) ?? new VacuousTextViewModel (dataModel);
			TextView = imports.TextEditorFactoryService.CreateTextView (viewModel, roles, imports.EditorOptionsFactoryService.GlobalOptions);
			textViewHost = imports.TextEditorFactoryService.CreateTextViewHost ((IWpfTextView)TextView, setFocus: true);
			return textViewHost.HostControl;
		}

		public override Widget Widget => xwtWidget;
#elif MAC
		class EmbeddedNSViewControl : Control
		{
			private AppKit.NSView nSView;

			public EmbeddedNSViewControl (AppKit.NSView nSView)
			{
				this.nSView = nSView;
			}

			protected override object CreateNativeWidget<T> ()
			{
				return nSView;
			}
		}

		private AppKit.NSView CreateControl ()
		{
			var roles = imports.TextEditorFactoryService.AllPredefinedRoles;
			ITextDataModel dataModel = new VacuousTextDataModel (TextBuffer);
			ITextViewModel viewModel = UIExtensionSelector.InvokeBestMatchingFactory (
				imports.TextViewModelProviders,
				dataModel.ContentType,
				roles,
				(provider) => (provider.CreateTextViewModel (dataModel, roles)),
				imports.ContentTypeRegistryService,
				imports.GuardedOperations,
				this) ?? new VacuousTextViewModel (dataModel);
			TextView = imports.TextEditorFactoryService.CreateTextView (viewModel, roles, imports.EditorOptionsFactoryService.GlobalOptions);
			textViewHost = imports.TextEditorFactoryService.CreateTextViewHost (TextView, setFocus: true);
			return textViewHost.HostControl;
		}
		Control control;
		public override Control Control { get => control; }
#endif
		public override void Dispose ()
		{
			TextDocument.Dispose ();
			base.Dispose ();
		}
	}
}