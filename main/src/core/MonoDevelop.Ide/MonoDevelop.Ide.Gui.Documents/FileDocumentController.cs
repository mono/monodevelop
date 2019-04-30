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
		string originalFileTitle;
		string defaultMimeType;
		string mimeType;
		DesktopService desktopService;
		private Encoding encoding;

		/// <summary>
		/// Raised when the FilePath property changes
		/// </summary>
		public event EventHandler FilePathChanged;

		public FileModel FileModel { get => Model as FileModel; set => Model = value; }

		public FilePath FilePath {
			get {
				CheckInitialized ();
				return filePath;
			}
			set {
				if (value != filePath) {
					filePath = value.CanonicalPath;
					defaultMimeType = null;
					OnFileNameChanged ();

					// Extensions that apply to specific file types may need to be loaded or unloaded
					RefreshExtensions ().Ignore ();
				}
			}
		}

		public string MimeType {
			get {
				CheckInitialized ();
				if (mimeType != null)
					return mimeType;
				if (defaultMimeType == null)
					defaultMimeType = desktopService.GetMimeTypeForUri (FilePath);
				return defaultMimeType;
			}
			set {
				mimeType = value;
			}
		}

		public Encoding Encoding {
			get {
				return encoding;
			}
			set {
				if (encoding != value) {
					encoding = value;
					OnEncodingChanged ();
				}
			}
		}

		public bool SupportsSaveAs { get; set; } = true;

		public virtual bool SupportsEncoding => FileModelType != null && typeof(TextFileModel).IsAssignableFrom (FileModelType);

		protected override async Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			desktopService = await ServiceProvider.GetService<DesktopService> ();

			if (!(modelDescriptor is FileDescriptor fileDescriptor))
				return;

			if (FileModelType != null) {
				if (fileDescriptor.Content != null) {
					// It is a new file
					if (!typeof (FileModel).IsAssignableFrom (FileModelType))
						throw new InvalidOperationException ("Invalid file model type: " + FileModelType);
					var fileModel = (FileModel)Activator.CreateInstance (FileModelType);
					fileModel.CreateNew ();
					await fileModel.SetContent (fileDescriptor.Content);
					if (fileDescriptor.Encoding != null && fileModel is TextFileModel textFileModel)
						textFileModel.Encoding = fileDescriptor.Encoding;
					SetModel (fileModel);
				} else {
					// Existing file, get a model from the registry
					var modelRegistry = await Runtime.GetService<DocumentModelRegistry> ();
					SetModel (await modelRegistry.GetSharedModel (FileModelType, fileDescriptor.FilePath));
				}
			}
			IsNewDocument = HasUnsavedChanges = fileDescriptor.Content != null;
			FilePath = fileDescriptor.FilePath;
			MimeType = fileDescriptor.MimeType;
			Encoding = fileDescriptor.Encoding ?? Encoding.UTF8;
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
			// If name changed and the file model is linked to a file, relink it
			if (FileModelType != null && FileModel != null && FileModel.IsLinked && FileModel.FilePath != filePath)
				FileModel.LinkToFile (FilePath);

			UpdateIcon ();

			try {
				FilePathChanged?.Invoke (this, EventArgs.Empty);
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}

			if (DocumentTitle == null || DocumentTitle == originalFileTitle)
				DocumentTitle = originalFileTitle = FilePath.FileName;
		}

		protected virtual void OnEncodingChanged ()
		{
		}

		protected override void OnModelChanged (DocumentModel oldModel, DocumentModel newModel)
		{
			if (FileModelType != null && FileModel != null) {
				if (FileModel.Id != null)
					FilePath = FileModel.FilePath;
			}
			UpdateIcon ();
			base.OnModelChanged (oldModel, newModel);
		}

		void UpdateIcon ()
		{
			if (!FilePath.IsNullOrEmpty) {
				try {
					DocumentIcon = desktopService.GetIconForFile (FilePath);
				} catch (Exception ex) {
					LoggingService.LogError ("Icon retrieval failed", ex);
					DocumentIcon = desktopService.GetIconForType ("gnome-fs-regular", Gtk.IconSize.Menu);
				}
			} else
				DocumentIcon = desktopService.GetIconForType ("gnome-fs-regular", Gtk.IconSize.Menu);
		}

		public async Task SaveAs (FilePath filePath, Encoding encoding)
		{
			FilePath = filePath;
			Encoding = encoding;
			await Save ();
		}

		protected override async Task OnSave ()
		{
			if (FileModelType != null) {
				if (IsNewDocument) {
					// Register the file model with the new id
					var modelRegistry = await Runtime.GetService<DocumentModelRegistry> ();
					await FileModel.LinkToFile (FilePath);
					await modelRegistry.ShareModel (FileModel);
				}
			}
			await base.OnSave ();
		}

		public bool CanActivateFile (FilePath file)
		{
			return false;
		}
	}
}
