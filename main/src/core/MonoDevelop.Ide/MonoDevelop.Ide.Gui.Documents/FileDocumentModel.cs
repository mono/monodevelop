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
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Core.Text;
using System.Threading;
using System.Text;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// A document model for files
	/// </summary>
	public class FileDocumentModel: DocumentModel
	{
		class FileData : DocumentModelData
		{
			public ITextDocument TextDocument { get; set; }
		}

		public FileDocumentModel (FilePath filePath)
		{
			this.FilePath = filePath;
		}

		public FileDocumentModel ()
		{
			IsNewFile = true;
		}

		public string MimeType { get; set; }

		public FilePath FilePath { get; private set; }

		public Encoding Encoding { get; set; }

		public bool IsNewFile { get; private set; }

		public bool CanWrite {
			get {
				var attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;
				return (File.GetAttributes (FilePath) & attr) != 0;
			}
		}

		public event EventHandler TextBufferChanged;

		protected internal override DocumentModelData CreateDataObject ()
		{
			return new FileData ();
		}

		public void LinkToFile (FilePath filePath)
		{
			if (FilePath != filePath) {
				FilePath = filePath;
				Relink (filePath);
			}
		}

		public void ConvertToUnsaved ()
		{
			UnlinkFromId ();
			FilePath = FilePath.FileName; // Remove the path, keep only the file name
			IsNewFile = true;
		}

		public Task SaveAs (FilePath filePath)
		{
			LinkToFile (filePath);
			return Save ();
		}

		protected override Task OnSave ()
		{
			if (IsNewFile)
				throw new InvalidOperationException ("SaveAs() must be used to save new files");

			return base.OnSave ();
		}

		/// <summary>
		/// Returns the content of the file in a stream
		/// </summary>
		/// <returns>The read.</returns>
		public Task<Stream> GetContent () 
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Sets the content of the file
		/// </summary>
		public Task SetContent (Stream content)
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Sets the content of the file
		/// </summary>
		public Task SetContent (string text)
		{
			var ms = new MemoryStream ();
			var writer = new StreamWriter (ms);
			writer.Write (text);
			return SetContent (ms);
		}

		/// <summary>
		/// Sets the content of the file
		/// </summary>
		public Task SetTextContent (TextReader textReader)
		{
			var ms = new MemoryStream ();
			var writer = new StreamWriter (ms);
			writer.Write (textReader.ReadToEnd ());
			return SetContent (ms);
		}

		/// <summary>
		/// Returns the content of the file in a stream
		/// </summary>
		/// <returns>The read.</returns>
		public async Task<TextReader> GetTextContent ()
		{
			return new StreamReader (await GetContent ());
		}

		public async Task<ITextBuffer> GetTextBuffer ()
		{
			return (await GetTextDocument()).TextBuffer;
		}

		public async Task<ITextDocument> GetTextDocument ()
		{
			var data = (FileData)Data;
			if (data.TextDocument == null) {
				var contentType = (MimeType == null) ? PlatformCatalog.Instance.TextBufferFactoryService.InertContentType : await GetContentTypeFromMimeType (FilePath, MimeType);
				if (IsNewFile) {
					using (var reader = new StreamReader (await GetContent ())) {
						var buffer = PlatformCatalog.Instance.TextBufferFactoryService.CreateTextBuffer (reader, contentType);
						data.TextDocument = PlatformCatalog.Instance.TextDocumentFactoryService.CreateTextDocument (buffer, FilePath.ToString () ?? "");
						data.TextDocument.Encoding = TextFileUtility.DefaultEncoding;
					}
				} else {
					var text = await TextFileUtility.GetTextAsync (FilePath, CancellationToken.None);
					var buffer = PlatformCatalog.Instance.TextBufferFactoryService.CreateTextBuffer (text.Text, contentType);
					data.TextDocument = PlatformCatalog.Instance.TextDocumentFactoryService.CreateTextDocument (buffer, FilePath);
					data.TextDocument.Encoding = text.Encoding;
				}
			}
			return data.TextDocument;
		}

		private static async Task<IContentType> GetContentTypeFromMimeType (string filePath, string mimeType)
		{
			if (filePath != null) {
				var compositionManager = await Runtime.GetService<CompositionManager> ();
				var fileToContentTypeService = compositionManager.GetExportedValue<IFileToContentTypeService> ();
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
	}
}
