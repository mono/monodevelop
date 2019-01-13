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
	{
#elif MAC
	partial class TextViewContent : ViewContent
	{
		sealed class EmbeddedNSViewControl : Control
		{
			readonly NSView nsView;

			public EmbeddedNSViewControl (NSView nsView)
				=> this.nsView = nsView;

			protected override object CreateNativeWidget<T> ()
				=> nsView;
		}
#endif

		readonly TextViewImports imports;
		readonly FilePath fileName;
		readonly string mimeType;
		readonly Project ownerProject;
		readonly Control control;
		readonly IEditorCommandHandlerService commandService;
		readonly List<IEditorContentProvider> contentProviders;

#if WINDOWS
		readonly Xwt.Widget xwtWidget;
		public override Xwt.Widget Widget => xwtWidget;

		public IWpfTextView TextView { get; }
#elif MAC
		public ICocoaTextView TextView { get; }
#endif

		public ITextDocument TextDocument { get; }
		public ITextBuffer TextBuffer { get; }

		public override Control Control => control;

		public TextViewContent (
			TextViewImports imports,
			FilePath fileName,
			string mimeType,
			Project ownerProject)
		{
			this.imports = imports;
			this.fileName = fileName;
			this.mimeType = mimeType;
			this.ownerProject = ownerProject;

			//TODO: HACK, this needs to be moved elsewhere and updated when MonoDevelop settings change.
			imports.EditorOptionsFactoryService.GlobalOptions.SetOptionValue (
				DefaultTextViewHostOptions.LineNumberMarginId,
				Editor.DefaultSourceEditorOptions.Instance.ShowLineNumberMargin);

			//TODO: this can change when the file is renamed
			var contentType = mimeType == null
				? imports.TextBufferFactoryService.InertContentType
				: GetContentTypeFromMimeType (fileName, mimeType);

			TextDocument = imports.TextDocumentFactoryService.CreateAndLoadTextDocument (fileName, contentType);
			TextBuffer = TextDocument.TextBuffer;
			TextDocument.DirtyStateChanged += OnTextDocumentDirtyStateChanged;

			var roles = imports.TextEditorFactoryService.AllPredefinedRoles;
			var dataModel = new VacuousTextDataModel (TextBuffer);
			var viewModel = UIExtensionSelector.InvokeBestMatchingFactory (
				imports.TextViewModelProviders,
				dataModel.ContentType,
				roles,
				provider => provider.CreateTextViewModel (dataModel, roles),
				imports.ContentTypeRegistryService,
				imports.GuardedOperations,
				this) ?? new VacuousTextViewModel (dataModel);

#if MAC
			TextView = imports.TextEditorFactoryService.CreateTextView (viewModel, roles, imports.EditorOptionsFactoryService.GlobalOptions);
			control = new EmbeddedNSViewControl (imports.TextEditorFactoryService.CreateTextViewHost (TextView, setFocus: true).HostControl);
#elif WINDOWS
			TextView = imports.TextEditorFactoryService.CreateTextView (viewModel, roles, imports.EditorOptionsFactoryService.GlobalOptions);
			control = imports.TextEditorFactoryService.CreateTextViewHost (TextView, setFocus: true).HostControl;

			var widget = new RootWpfWidget (control);
			widget.HeightRequest = 50;
			widget.WidthRequest = 100;

			TextView.VisualElement.Tag = widget;
			TextView.VisualElement.LostKeyboardFocus += (s, e) => {
				Components.Commands.CommandManager.LastFocusedWpfElement = TextView.VisualElement;
			};

			xwtWidget = Xwt.Toolkit.CurrentEngine.WrapWidget (widget, Xwt.NativeWidgetSizing.External);
			xwtWidget.Show ();
#endif

			commandService = imports.EditorCommandHandlerServiceFactory.GetService (TextView);
			contentProviders = new List<IEditorContentProvider> (imports.EditorContentProviderService.GetContentProvidersForView (TextView));

			TextView.Properties [typeof (TextViewContent)] = this;
			ContentName = fileName;
		}

		public override void Dispose ()
		{
			TextDocument.Dispose ();
			base.Dispose ();
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
					foreach (var content in contents)
						yield return content;
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

		public override bool IsDirty => TextDocument.IsDirty;

		void OnTextDocumentDirtyStateChanged (object sender, EventArgs e)
			=> OnDirtyChanged ();

		static readonly string[] textContentType = { "text" };

		IContentType GetContentTypeFromMimeType (string filePath, string mimeType)
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
	}
}