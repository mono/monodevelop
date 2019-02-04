//
// FileDocumentModel.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Core.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Documents
{
	public class TextBufferFileModel : TextFileModel
	{
		ITextDocument textDocument;

		public event EventHandler TextBufferInstanceChanged;

		public override async Task<string> GetText ()
		{
			var buffer = await GetTextBuffer ();
			return buffer.CurrentSnapshot.GetText ();
		}

		public override void SetText (string text)
		{
			if (textDocument != null) {
				var edit = textDocument.TextBuffer.CreateEdit ();
				edit.Replace (0, textDocument.TextBuffer.CurrentSnapshot.Length, text);
				edit.Apply ();
			} else {
				textDocument = CreateTextDocument (text);
			}
		}

		public async Task<ITextBuffer> GetTextBuffer ()
		{
			return (await GetTextDocument ()).TextBuffer;
		}

		public async Task<ITextDocument> GetTextDocument ()
		{
			if (textDocument == null) {
				// If a linked document has a text document, reuse it
				textDocument = LinkedModels.OfType<TextBufferFileModel> ().FirstOrDefault (m => m.textDocument != null)?.textDocument;
				if (textDocument != null)
					return textDocument;
				// If linked to another model, take the text from it
				var someFile = LinkedModels.FirstOrDefault ();
				if (someFile != null) {
					await CopyFromLinkedModel (someFile);
					return textDocument;
				}
			}

			var contentType = (MimeType == null) ? PlatformCatalog.Instance.TextBufferFactoryService.InertContentType : GetContentTypeFromMimeType (FilePath, MimeType);
			if (IsNewFile) {
				textDocument = CreateTextDocument ("");
			} else {
				var text = await TextFileUtility.GetTextAsync (FilePath, CancellationToken.None);
				var buffer = PlatformCatalog.Instance.TextBufferFactoryService.CreateTextBuffer (text.Text, contentType);
				textDocument = PlatformCatalog.Instance.TextDocumentFactoryService.CreateTextDocument (buffer, FilePath);
				textDocument.Encoding = text.Encoding;
			}
			return textDocument;
		}

		ITextDocument CreateTextDocument (string text)
		{
			var buffer = PlatformCatalog.Instance.TextBufferFactoryService.CreateTextBuffer (text, contentType);
			var textDocument = PlatformCatalog.Instance.TextDocumentFactoryService.CreateTextDocument (buffer, FilePath.ToString () ?? "");
			textDocument.Encoding = TextFileUtility.DefaultEncoding;
			return textDocument;
		}

		protected static IContentType GetContentTypeFromMimeType (string filePath, string mimeType)
		{
			if (filePath != null) {
				var fileToContentTypeService = CompositionManager.Instance.GetExportedValue<IFileToContentTypeService> ();
				var contentTypeFromPath = fileToContentTypeService.GetContentTypeForFilePath (filePath);
				if (contentTypeFromPath != null &&
					contentTypeFromPath != PlatformCatalog.Instance.ContentTypeRegistryService.UnknownContentType) {
					return contentTypeFromPath;
				}
			}

			var contentType = PlatformCatalog.Instance.MimeToContentTypeRegistryService.GetContentType (mimeType);
			if (contentType == null) {
				// fallback 1: see if there is a content tyhpe with the same name
				contentType = PlatformCatalog.Instance.ContentTypeRegistryService.GetContentType (mimeType);
				if (contentType == null) {
					// No joy, create a content type that, by default, derives from text. This is strictly an error
					// (there should be mappings between any mime type and any content type).
					contentType = PlatformCatalog.Instance.ContentTypeRegistryService.AddContentType (mimeType, new string [] { "text" });
				}
			}

			return contentType;
		}

		void BindTextBuffer (ITextBuffer textBuffer)
		{
			textBuffer.Changed += TextBuffer_Changed;
		}

		void TextBuffer_Changed (object sender, TextContentChangedEventArgs e)
		{
			NotifyChanged ();
		}

		protected override void OnLinkedModelChanged (DocumentModel model)
		{
			base.OnLinkedModelChanged (model);
		}

		void ReplaceTextDocument (ITextDocument textDocument)
		{
			bool hadDocument = this.textDocument != null && this.textDocument != textDocument;
			this.textDocument = textDocument;
			OnTextBufferInstanceChanged ();
		}

		protected override Task CopyFromLinkedModel (DocumentModel model)
		{
			if (model is TextBufferFileModel textBufferFile) {
				if (textDocument != textBufferFile.textDocument) {
					ReplaceTextDocument (textBufferFile.textDocument);
				}
				return Task.CompletedTask;
			} else
				return base.CopyFromLinkedModel (model);
		}

		internal protected override void LinkToModels (IEnumerable<DocumentModel> documentModels, bool keepCurrentData)
		{
			if (textDocument == null)
				return;

			var fileModels = documentModels.OfType<TextBufferFileModel> ().ToList ();
			if (keepCurrentData) {
				foreach (var model in fileModels)
					model.ReplaceTextDocument (textDocument);
			} else if (fileModels.Count > 0)
				// Reset the document, a new one will be reused from the linked model
				ReplaceTextDocument (null);
		}

		internal protected override void UnlinkFromModels (IEnumerable<DocumentModel> documentModels)
		{
			if (textDocument == null)
				return;

			var fileModels = documentModels.OfType<TextBufferFileModel> ().Where (m => m.textDocument == textDocument).ToList ();
			if (fileModels.Count == 0)
				return;

			var contentType = (MimeType == null) ? PlatformCatalog.Instance.TextBufferFactoryService.InertContentType : GetContentTypeFromMimeType (FilePath, MimeType);
			var buffer = PlatformCatalog.Instance.TextBufferFactoryService.CreateTextBuffer (GetText (), contentType);
			var newDocument = PlatformCatalog.Instance.TextDocumentFactoryService.CreateTextDocument (buffer, FilePath.ToString () ?? "");
			newDocument.Encoding = TextFileUtility.DefaultEncoding;

			foreach (var fileModel in fileModels)
				fileModel.ReplaceTextDocument (newDocument);
		}

		protected virtual void OnTextBufferInstanceChanged ()
		{
			TextBufferInstanceChanged?.Invoke (this, EventArgs.Empty);
		}
	}
}
