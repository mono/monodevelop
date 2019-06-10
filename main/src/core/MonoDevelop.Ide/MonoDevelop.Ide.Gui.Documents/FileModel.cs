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
using System.Text;
using Roslyn.Utilities;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// A document model for files
	/// </summary>
	public class FileModel: DocumentModel
	{
		public FileModel ()
		{
		}

		public string MimeType => IsLoaded ? GetRepresentation<FileModelRepresentation> ().MimeType : null;

		public FilePath FilePath => Id != null ? (FilePath) Id : (IsLoaded ? GetRepresentation<FileModelRepresentation> ().FilePath : FilePath.Null);

		public bool CanWrite {
			get {
				if (IsNew)
					return false;
				if (File.Exists (FilePath)) {
					var attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;
					return (File.GetAttributes (FilePath) & attr) == 0;
				} else
					return true;
			}
		}

		public void CreateNew (string fileName, string mimeType)
		{
			InitializeNew ();
			var rep = GetRepresentation<FileModelRepresentation> ();
			rep.InitUnsaved (fileName, mimeType);
			rep.CreateNew ();
		}

		public Task LinkToFile (FilePath filePath)
		{
			if (FilePath != filePath)
				return Relink (filePath);

			return Task.CompletedTask;
		}

		public async Task ConvertToUnsaved ()
		{
			await UnlinkFromId ();
		}

		public async Task SaveAs (FilePath filePath)
		{
			await LinkToFile (filePath);
			await Save ();
		}

		/// <summary>
		/// Returns the content of the file in a stream
		/// </summary>
		/// <returns>The read.</returns>
		public Stream GetContent () 
		{
			var rep = GetRepresentation<FileModelRepresentation> ();
			return rep.GetContent ();
		}

		/// <summary>
		/// Sets the content of the file
		/// </summary>
		public Task SetContent (Stream content)
		{
			var rep = GetRepresentation<FileModelRepresentation> ();
			return rep.SetContent (content);
		}

		internal protected override Type RepresentationType => typeof (InternalFileModelRepresentation);

		protected abstract class FileModelRepresentation : ModelRepresentation
		{
			FilePath unsavedFilePath;

			public FilePath FilePath => Id != null ? (FilePath)Id : unsavedFilePath;
			public string MimeType { get; set; }

			internal void InitUnsaved (FilePath filePath, string mimeType)
			{
				unsavedFilePath = filePath;
				if (mimeType != null)
					MimeType = mimeType;
				else if (!filePath.IsNullOrEmpty)
					MimeType = Runtime.PeekService<DesktopService> ()?.GetMimeTypeForUri (filePath);
			}

			public async Task SetContent (Stream content)
			{
				try {
					FreezeChangeEvent ();
					await OnSetContent (content);
				} finally {
					ThawChangeEvent ();
				}
			}

			public Stream GetContent ()
			{
				return OnGetContent ();
			}

			protected abstract Task OnSetContent (Stream content);

			protected abstract Stream OnGetContent ();

			protected override async Task OnCopyFrom (ModelRepresentation other)
			{
				if (other is FileModelRepresentation file) {
					await SetContent (file.GetContent ());
				} else
					throw new InvalidOperationException ($"Can't copy data from model of type {other.GetType ()} into a model of type {GetType ()}");
			}

			protected override async Task OnIdChanged ()
			{
				MimeType = (await Runtime.GetService<DesktopService> ()).GetMimeTypeForUri (FilePath);
			}
		}

		class InternalFileModelRepresentation : FileModelRepresentation
		{
			byte [] content;

			protected override Stream OnGetContent ()
			{
				return new MemoryStream (content);
			}

			protected override Task OnSetContent (Stream content)
			{
				this.content = content.ReadAllBytes ();
				HasUnsavedChanges = true;
				NotifyChanged ();
				return Task.CompletedTask;
			}

			protected override Task OnLoad ()
			{
				bool hadContent = content != null;
				content = File.ReadAllBytes (FilePath);
				if (hadContent)
					NotifyChanged ();
				return Task.CompletedTask;
			}

			protected override void OnCreateNew ()
			{
				content = Array.Empty<byte> ();
			}

			protected override Task OnSave ()
			{
				File.WriteAllBytes (FilePath, content);
				return Task.CompletedTask;
			}
		}
	}
}
