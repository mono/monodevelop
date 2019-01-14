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
using System.ComponentModel.Composition;
using System.Threading.Tasks;

#if MAC
using AppKit;
#elif WINDOWS
using System.Windows.Input;
#endif

using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
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

#if MAC
	[Export (typeof (EditorFormatDefinition))]
	[ClassificationType (ClassificationTypeNames = ClassificationTypeNames.Text)]
	[Name (ClassificationTypeNames.Text)]
	[Order (After = Priority.Default, Before = Priority.High)]
	[UserVisible (true)]
	internal class ClassificationFormatDefinitionFromPreferences : ClassificationFormatDefinition
	{
		internal ClassificationFormatDefinitionFromPreferences ()
		{
			nfloat fontSize = -1;
			var fontName = Editor.DefaultSourceEditorOptions.Instance.FontName;

			if (!string.IsNullOrEmpty (fontName)) {
				var sizeStartOffset = fontName.LastIndexOf (' ');
				if (sizeStartOffset >= 0) {
					nfloat.TryParse (fontName.Substring (sizeStartOffset + 1), out fontSize);
					fontName = fontName.Substring (0, sizeStartOffset);
				}
			}

			if (string.IsNullOrEmpty (fontName))
				fontName = "Menlo";

			if (fontSize <= 1)
				fontSize = 12;

			FontTypeface = NSFont.FromFontName (fontName, fontSize);
		}
	}
#endif

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
		readonly IEditorCommandHandlerService commandService;
		readonly List<IEditorContentProvider> contentProviders;
		readonly Editor.DefaultSourceEditorOptions sourceEditorOptions;

#if WINDOWS
		readonly Xwt.Widget xwtWidget;
		public override Xwt.Widget Widget => xwtWidget;

		public IWpfTextView TextView { get; }
#elif MAC
		readonly Control control;
		public ICocoaTextView TextView { get; }

		public override Control Control => control;
#endif

		public ITextDocument TextDocument { get; }
		public ITextBuffer TextBuffer { get; }

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
			this.sourceEditorOptions = Editor.DefaultSourceEditorOptions.Instance;

			// FIXME: move this to the end of the .ctor after fixing margin options responsiveness
			HandleSourceEditorOptionsChanged (this, EventArgs.Empty);

			//TODO: this can change when the file is renamed
			var contentType = mimeType == null
				? imports.TextBufferFactoryService.InertContentType
				: GetContentTypeFromMimeType (fileName, mimeType);

			TextDocument = imports.TextDocumentFactoryService.CreateAndLoadTextDocument (fileName, contentType);
			TextBuffer = TextDocument.TextBuffer;

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
			control.GetNativeWidget<Gtk.Widget> ().CanFocus = true;
			TextView.GotAggregateFocus += (sender, e) => {
				control.GetNativeWidget<Gtk.Widget> ().GrabFocus ();
			};
#elif WINDOWS
			TextView = imports.TextEditorFactoryService.CreateTextView (viewModel, roles, imports.EditorOptionsFactoryService.GlobalOptions);
			var wpfControl = imports.TextEditorFactoryService.CreateTextViewHost (TextView, setFocus: true).HostControl;

			var widget = new RootWpfWidget (wpfControl);
			widget.HeightRequest = 50;
			widget.WidthRequest = 100;

			TextView.VisualElement.Tag = widget;

			xwtWidget = Xwt.Toolkit.CurrentEngine.WrapWidget (widget, Xwt.NativeWidgetSizing.External);
			xwtWidget.Show ();
#endif

			commandService = imports.EditorCommandHandlerServiceFactory.GetService (TextView);
			contentProviders = new List<IEditorContentProvider> (imports.EditorContentProviderService.GetContentProvidersForView (TextView));

			TextView.Properties [typeof (TextViewContent)] = this;
			ContentName = fileName;

			SubscribeToEvents ();
		}

		public override void Dispose ()
		{
			UnsubscribeFromEvents ();
			TextDocument.Dispose ();
			base.Dispose ();
		}

		void SubscribeToEvents ()
		{
			sourceEditorOptions.Changed += HandleSourceEditorOptionsChanged;
			TextDocument.DirtyStateChanged += HandleTextDocumentDirtyStateChanged;

#if WINDOWS
			TextView.VisualElement.LostKeyboardFocus += HandleWpfLostKeyboardFocus;
#endif
		}

		void UnsubscribeFromEvents ()
		{
			sourceEditorOptions.Changed -= HandleSourceEditorOptionsChanged;
			TextDocument.DirtyStateChanged -= HandleTextDocumentDirtyStateChanged;

#if WINDOWS
			TextView.VisualElement.LostKeyboardFocus -= HandleWpfLostKeyboardFocus;
#endif
		}

#if WINDOWS
		void HandleWpfLostKeyboardFocus (object sender, KeyboardFocusChangedEventArgs e)
			=> Components.Commands.CommandManager.LastFocusedWpfElement = TextView.VisualElement;
#endif

		void HandleSourceEditorOptionsChanged (object sender, EventArgs e)
		{
			imports.EditorOptionsFactoryService.GlobalOptions.SetOptionValue (
				DefaultTextViewHostOptions.LineNumberMarginId,
				sourceEditorOptions.ShowLineNumberMargin);
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

		void HandleTextDocumentDirtyStateChanged (object sender, EventArgs e)
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