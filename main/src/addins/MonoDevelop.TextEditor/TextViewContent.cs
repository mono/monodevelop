using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.Ide.Text
{
	class TextViewContent : AbstractXwtViewContent
	{
		TextViewImports imports;
		FilePath fileName;
		string mimeType;
		Project ownerProject;
		RootWpfWidget widget;
		Xwt.Widget xwtWidget;

		public ITextDocument TextDocument { get; }
		public ITextBuffer TextBuffer { get; }
		public ITextView TextView { get; private set; }

		private IWpfTextViewHost textViewHost;

		public TextViewContent (TextViewImports imports, FilePath fileName, string mimeType, Project ownerProject)
		{
			this.imports = imports;
			this.fileName = fileName;
			this.mimeType = mimeType;
			this.ownerProject = ownerProject;

			var contentType = (mimeType == null) ? imports.TextBufferFactoryService.InertContentType : GetContentTypeFromMimeType (fileName, mimeType);

			TextDocument = imports.TextDocumentFactoryService.CreateAndLoadTextDocument (fileName, contentType);
			TextBuffer = TextDocument.TextBuffer;
			TextDocument.DirtyStateChanged += OnTextDocumentDirtyStateChanged;

			var control = CreateControl (imports);
			this.widget = new RootWpfWidget (control);
			widget.HeightRequest = 50;
			widget.WidthRequest = 100;
			this.xwtWidget = GetXwtWidget (widget);
			xwtWidget.Show ();
			ContentName = fileName;
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

		private Widget GetXwtWidget (RootWpfWidget widget)
		{
			return Xwt.Toolkit.CurrentEngine.WrapWidget (widget, NativeWidgetSizing.External);
		}

		private System.Windows.Controls.Control CreateControl (TextViewImports imports)
		{
			TextView = imports.TextEditorFactoryService.CreateTextView (TextBuffer);
			textViewHost = imports.TextEditorFactoryService.CreateTextViewHost ((IWpfTextView)TextView, setFocus: true);
			return textViewHost.HostControl;
		}

		public override Widget Widget => xwtWidget;

		public override void Dispose ()
		{
			TextDocument.Dispose ();
			base.Dispose ();
		}
	}
}