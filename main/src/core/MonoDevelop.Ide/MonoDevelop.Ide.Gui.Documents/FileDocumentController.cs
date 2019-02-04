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

		public FileModel FileModel { get => Model as FileModel; set => Model = value; }

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
				return;

			if (FileModelType != null) {
				var modelRegistry = await Runtime.GetService<DocumentModelRegistry> ();

				if (fileDescriptor.Content != null) {
					// It is a new file
					if (!typeof (FileModel).IsAssignableFrom (FileModelType))
						throw new InvalidOperationException ("Invalid file model type: " + FileModelType);
					var fileModel = (FileModel) Activator.CreateInstance (FileModelType);
					await fileModel.SetContent (fileDescriptor.Content);
					Model = fileModel;
				} else {
					// Existing file, get a model from the registry
					Model = await modelRegistry.GetSharedModel(FileModelType, fileDescriptor.FilePath);
				}
			}
			FilePath = fileDescriptor.FilePath;
			Owner = fileDescriptor.Owner;
		}

		protected virtual Type FileModelType => null;

		protected override bool OnCanAssignModel (Type type)
		{
			return false;
		}

		protected override bool OnTryReuseDocument (ModelDescriptor modelDescriptor)
		{
			return modelDescriptor is FileDescriptor file && file.FilePath.CanonicalPath == filePath;
		}

		protected virtual void OnFileNameChanged ()
		{
			if (FileModelType != null)
				FileModel?.LinkToFile (FilePath);
			UpdateIcon (DocumentIcon).Ignore ();
		}

		protected override void OnModelChanged (DocumentModel oldModel, DocumentModel newModel)
		{
			if (FileModelType != null)
				IsNewDocument = FileModel.IsNewFile;
			UpdateIcon (DocumentIcon).Ignore ();
			base.OnModelChanged (oldModel, newModel);
		}

		async Task UpdateIcon (Xwt.Drawing.Image iconToReplace)
		{
			if (!FilePath.IsNullOrEmpty) {
				var desktopService = await ServiceProvider.GetService<DesktopService> ();
				if (DocumentIcon != iconToReplace) // If the icon has changed since the update was requested, keep the new one
					return;
				try {
					DocumentIcon = desktopService.GetIconForFile (FilePath);
				} catch (Exception ex) {
					LoggingService.LogError ("Icon retrieval failed", ex);
					DocumentIcon = desktopService.GetIconForType ("gnome-fs-regular", Gtk.IconSize.Menu);
				}
			}
		}

		public async Task SaveAs (FilePath filePath, Encoding encoding)
		{
			FilePath = filePath;
			Encoding = encoding;
			if (IsNewDocument && FileModelType != null) {
				// Register the file model with the new id
				var modelRegistry = await Runtime.GetService<DocumentModelRegistry> ();
				await modelRegistry.RegisterSharedModel (FileModel, filePath);
			}
			IsNewDocument = false;
			await Save ();
		}

		public bool CanActivateFile (FilePath file)
		{
			return false;
		}
	}
}
