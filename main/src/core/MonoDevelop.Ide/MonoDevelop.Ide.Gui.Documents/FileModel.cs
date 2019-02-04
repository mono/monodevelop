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
	public class FileDocumentModel: DocumentModel
	{
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

		public bool IsNewFile { get; private set; }

		public bool CanWrite {
			get {
				var attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;
				return (File.GetAttributes (FilePath) & attr) != 0;
			}
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
		public async Task<Stream> GetContent () 
		{
			var rep = await GetRepresentation<FileModelRepresentation> ();
			return await rep.GetContent ();
		}

		/// <summary>
		/// Sets the content of the file
		/// </summary>
		public async Task SetContent (Stream content)
		{
			var rep = await GetRepresentation<FileModelRepresentation> ();
			await rep.SetContent (content);
		}

		internal protected override Type RepresentationType => typeof (InternalFileModelRepresentation);

		protected abstract class FileModelRepresentation : ModelRepresentation
		{
			public FilePath FilePath => (FilePath)Id;
			public string MimeType { get; set; }

			public async Task SetContent (Stream content)
			{
				try {
					await WaitHandle.WaitAsync ();
					await OnSetContent (content);
					SetLoaded ();
				} finally {
					WaitHandle.Release ();
				}
				NotifyChanged ();
			}

			public async Task<Stream> GetContent ()
			{
				await Load ();
				try {
					await WaitHandle.WaitAsync ();
					return await OnGetContent ();
				} finally {
					WaitHandle.Release ();
				}
			}

			protected abstract Task OnSetContent (Stream content);

			protected abstract Task<Stream> OnGetContent ();

			protected override async Task OnCopyFrom (ModelRepresentation other)
			{
				if (other is FileModelRepresentation file) {
					await SetContent (await file.GetContent ());
				} else
					throw new InvalidOperationException ($"Can't copy data from model of type {other.GetType ()} into a model of type {GetType ()}");
			}
		}

		class InternalFileModelRepresentation : FileModelRepresentation
		{
			byte [] content;

			protected override Task<Stream> OnGetContent ()
			{
				return Task.FromResult<Stream> (new MemoryStream (content));
			}

			protected override Task OnSetContent (Stream content)
			{
				this.content = content.ReadAllBytes ();
				return Task.CompletedTask;
			}

			protected override Task OnLoad ()
			{
				content = File.ReadAllBytes (FilePath);
				return Task.CompletedTask;
			}

			protected override Task OnSave ()
			{
				File.WriteAllBytes (FilePath, content);
				return Task.CompletedTask;
			}
		}
	}
}
