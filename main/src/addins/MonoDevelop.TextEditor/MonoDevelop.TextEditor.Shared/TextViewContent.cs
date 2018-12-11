using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppKit;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.Ide.Text
{
#if WINDOWS
	class TextViewContent : AbstractXwtViewContent
#elif MAC
	class TextViewContent : ViewContent
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
			this.xwtWidget = GetXwtWidget (widget);
			xwtWidget.Show ();
#elif MAC
			control = new EmbeddedNSViewControl (CreateControl ());
#endif
			ContentName = fileName;
		}

		protected override IEnumerable<object> OnGetContents (Type type)
		{
			if (type == typeof(ITextBuffer)) {
				return new[] { TextBuffer };
			} else if (type == typeof(ITextDocument)) {
				return new[] { TextDocument };
			}

			return Array.Empty<object> ();
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