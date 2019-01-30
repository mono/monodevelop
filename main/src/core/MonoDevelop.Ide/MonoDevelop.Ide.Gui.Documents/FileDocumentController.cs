//
// FileDocumentController.cs
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
using System.Text;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// A document controller specialized in showing the content of files
	/// </summary>
	public class FileDocumentController : DocumentController
	{
		FilePath filePath;

		public FileDocumentModel FileModel { get => Model as FileDocumentModel; set => Model = value; }

		public FilePath FilePath {
			get { return filePath; }
			set {
				if (value != filePath) {
					filePath = value.CanonicalPath;
					OnFileNameChanged ();
				}
			}
		}

		public string MimeType { get; set; }

		public Encoding Encoding { get; set; }

		public bool SupportsSaveAs { get; set; }

		public bool SupportsEncoding { get; set; }

		protected override async Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			if (!(modelDescriptor is FileDescriptor fileDescriptor))
				throw new NotSupportedException ("Controllers of type FileDocumentController can only handle model descriptors of type FileDescriptor");

			var modelRegistry = await Runtime.GetService<DocumentModelRegistry> ();

			if (fileDescriptor.Content != null) {
				// It is a new file
				var fileModel = new FileDocumentModel ();
				await fileModel.SetContent (fileDescriptor.Content);
				Model = fileModel;
			} else {
				// Existing file, get a model from the registry
				Model = modelRegistry.CreateSharedModel<FileDocumentModel> (fileDescriptor.FilePath);
			}
			FilePath = fileDescriptor.FilePath;
			Owner = fileDescriptor.Owner;
		}

		protected override bool OnCanAssignModel (Type type)
		{
			return typeof (FileDocumentModel).IsAssignableFrom (type);
		}

		protected override bool OnTryReuseDocument (ModelDescriptor modelDescriptor)
		{
			return modelDescriptor is FileDescriptor file && file.FilePath.CanonicalPath == filePath;
		}

		protected virtual void OnFileNameChanged ()
		{
			FileModel?.LinkToFile (FilePath);
			UpdateIcon (DocumentIcon).Ignore ();
		}

		protected override void OnModelChanged ()
		{
			IsNewDocument = FileModel.IsNewFile;
			UpdateIcon (DocumentIcon).Ignore ();
			base.OnModelChanged ();
		}

		async Task UpdateIcon (Xwt.Drawing.Image iconToReplace)
		{
			if (Model is FileDocumentModel fileModel) {
				var desktopService = await ServiceProvider.GetService<DesktopService> ();
				if (DocumentIcon != iconToReplace) // If the icon has changed since the update was requested, keep the new one
					return;
				try {
					DocumentIcon = desktopService.GetIconForFile (fileModel.FilePath);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
					DocumentIcon = desktopService.GetIconForType ("gnome-fs-regular", Gtk.IconSize.Menu);
				}
			}
		}

		protected override async Task OnSave ()
		{
			var modelRegistry = await Runtime.GetService<DocumentModelRegistry> ();
			modelRegistry.RegisterSharedModel (FileModel);
			await base.OnSave ();
		}

		public Task SaveAs (FilePath filePath, Encoding encoding)
		{
			FilePath = filePath;
			Encoding = encoding;
			IsNewDocument = false;
			return Save ();
		}

		public bool CanActivateFile (FilePath file)
		{
			return false;
		}
	}
}
