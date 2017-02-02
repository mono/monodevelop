// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;

    internal partial class TextDocument : ITextDocument
    {
        #region Private Members

        private readonly TextDocumentFactoryService _textDocumentFactoryService;
        private ITextBuffer _textBuffer;
        private Encoding _encoding;
        private string _filePath;

        private DateTime _lastSavedTimeUtc;
        private DateTime _lastModifiedTimeUtc;
        private int _cleanReiteratedVersion; // The ReiteratedVersionNumber at which the document was last clean (saved, opened, or created)
        private bool _isDirty;
        private bool _isDisposed;
        private bool _raisingDirtyStateChangedEvent;
        private bool _raisingFileActionChangedEvent;
        private bool _reloadingFile;

        #endregion

        #region Construction
        internal TextDocument(ITextBuffer textBuffer, string filePath, DateTime lastModifiedTime, TextDocumentFactoryService textDocumentFactoryService)
            : this(textBuffer, filePath, lastModifiedTime, textDocumentFactoryService, Encoding.UTF8) { }
        
        internal TextDocument(ITextBuffer textBuffer, string filePath, DateTime lastModifiedTime, TextDocumentFactoryService textDocumentFactoryService, Encoding encoding)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }
            if (textDocumentFactoryService == null)
            {
                throw new ArgumentNullException("textDocumentFactoryService");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            _textBuffer = textBuffer;
            _filePath = filePath;
            _lastModifiedTimeUtc = lastModifiedTime;
            _textDocumentFactoryService = textDocumentFactoryService;
            _cleanReiteratedVersion = _textBuffer.CurrentSnapshot.Version.ReiteratedVersionNumber;
            _isDisposed = false;
            _isDirty = false;
            _reloadingFile = false;
            _raisingDirtyStateChangedEvent = false;
            _raisingFileActionChangedEvent = false;
            _encoding = encoding;

            // Keep track of when the text buffer has been changed so that we can update the LastContentModifiedTime
            _textBuffer.ChangedHighPriority += TextBufferChangedHandler;

            _textBuffer.Properties.AddProperty(typeof(ITextDocument), this);
        }
        #endregion

        #region ITextDocument Members

        public string FilePath
        {
            get { return _filePath; }
        }

        public ITextBuffer TextBuffer
        {
            get { return _textBuffer; }
        }

        public bool IsDirty
        {
            get { return _isDirty; }
        }

        public DateTime LastSavedTime
        {
            get { return _lastSavedTimeUtc; }
        }

        public DateTime LastContentModifiedTime
        {
            get { return _lastModifiedTimeUtc; }
        }

        public void Rename(string newFilePath)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("ITextDocument");
            }
            if (_raisingDirtyStateChangedEvent || _raisingFileActionChangedEvent)
            {
                throw new InvalidOperationException();
            }
            if (newFilePath == null)
            {
                throw new ArgumentNullException("newFilePath");
            }

            _filePath = newFilePath;

            RaiseFileActionChangedEvent(_lastModifiedTimeUtc, FileActionTypes.DocumentRenamed, _filePath);
        }

        public ReloadResult Reload()
        {
            return Reload(EditOptions.None);
        }

        public ReloadResult Reload(EditOptions options)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("ITextDocument");
            }
            if (_raisingDirtyStateChangedEvent || _raisingFileActionChangedEvent)
            {
                throw new InvalidOperationException();
            }

            var beforeSnapshot = _textBuffer.CurrentSnapshot;
            Encoding newEncoding = null;
            FallbackDetector fallbackDetector;

            try
            {
                _reloadingFile = true;

                // Load the file and read the contents to the text buffer

                long fileSize;
                using (var stream = TextDocumentFactoryService.OpenFileGuts(_filePath, out _lastModifiedTimeUtc, out fileSize))
                {
                    // We want to use the encoding indicated by a BoM if one is present because
                    // VS9's editor did so. We can't let the StreamReader below detect
                    // the byte order marks because we still want to be able to detect the
                    // fallback condition.
                    bool unused;
                    newEncoding = EncodedStreamReader.CheckForBoM(stream, isStreamEmpty: out unused);

                    Debug.Assert(newEncoding == null || newEncoding.GetPreamble().Length > 0);

                    // TODO: Consider using the encoder detector extensions as well.

                    if (newEncoding == null)
                        newEncoding = this.Encoding;

                    fallbackDetector = new FallbackDetector(newEncoding.DecoderFallback);
                    var modifiedEncoding = (Encoding)newEncoding.Clone();
                    modifiedEncoding.DecoderFallback = fallbackDetector;

                    using (var streamReader = new StreamReader(stream, modifiedEncoding, detectEncodingFromByteOrderMarks: false))
                    {
                        TextBuffer concreteBuffer = _textBuffer as TextBuffer;
                        if (concreteBuffer != null)
                        {
                            ITextStorageLoader loader;
                            if (fileSize < TextModelOptions.CompressedStorageFileSizeThreshold)
                            {
                                loader = new SimpleTextStorageLoader(streamReader, (int)fileSize);
                            }
                            else
                            {
                                loader = new CompressedTextStorageLoader(streamReader, (int)fileSize, _filePath);
                            }
                            IStringRebuilder newContent = SimpleStringRebuilder.Create(loader);
                            if (!loader.HasConsistentLineEndings)
                            {
                                // leave a sign that line endings are inconsistent. This is rather nasty but for now
                                // we don't want to pollute the API with this factoid.
                                concreteBuffer.Properties["InconsistentLineEndings"] = true;
                            }
                            else
                            {
                                // this covers a really obscure case where on initial load the file had inconsistent line
                                // endings, but the UI settings were such that it was ignored, and since then the file has
                                // acquired consistent line endings and the UI settings have also changed.
                                concreteBuffer.Properties.RemoveProperty("InconsistentLineEndings");
                            }
                            // leave a similar sign about the longest line in the buffer.
                            concreteBuffer.Properties["LongestLineLength"] = loader.LongestLineLength;

                            concreteBuffer.ReloadContent(newContent, options, editTag: this);
                        }
                        else
                        {
                            // we may hit this path if somebody mocks the text buffer in a test.
                            using (var edit = _textBuffer.CreateEdit(options, null, editTag: this))
                            {
                                if (edit.Replace(new Span(0, edit.Snapshot.Length), streamReader.ReadToEnd()))
                                {
                                    edit.Apply();
                                }
                                else
                                {
                                    edit.Cancel();
                                }
                            }
                        }
                        Debug.Assert(streamReader.CurrentEncoding.CodePage == newEncoding.CodePage);
                        Debug.Assert(streamReader.CurrentEncoding.GetPreamble().Length == newEncoding.GetPreamble().Length);
                    }
                }
            }
            finally
            {
                _reloadingFile = false;
            }

            //The snapshot on a reload will change even if the contents of the before & after files are identical (differences will simply find an
            //empty set of changes) so this test is a measure of whether of not the reload succeeded.
            if (beforeSnapshot.Version.Next != null)
            {
                // Update status
                // set the "clean" reiterated version number to the reiterated version number of the version immediately
                // after the before snapshot (which is the state of the buffer after loading the document but before any
                // subsequent edits made in the text buffer changed events).
                _cleanReiteratedVersion = beforeSnapshot.Version.Next.ReiteratedVersionNumber;

                // TODO: the following event really should be queued up through the buffer group so that it comes before
                // the text changed event (and any subsequent text changed event invoked from an event handler)
                RaiseFileActionChangedEvent(_lastModifiedTimeUtc, FileActionTypes.ContentLoadedFromDisk, _filePath);
                this.Encoding = newEncoding;
                return fallbackDetector.FallbackOccurred ? ReloadResult.SucceededWithCharacterSubstitutions : ReloadResult.Succeeded;
            }
            else
            {
                return ReloadResult.Aborted;
            }
        }

        public bool IsReloading
        {
            get { return _reloadingFile; }
        }

        public void Save()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("ITextDocument");
            }
            if (_raisingDirtyStateChangedEvent || _raisingFileActionChangedEvent)
            {
                throw new InvalidOperationException();
            }

            PerformSave(FileMode.Create, _filePath, false);
            UpdateSaveStatus(_filePath, false);
        }

        public void SaveAs(string filePath, bool overwrite)
        {
            SaveAs(filePath, overwrite, false);
        }

        public void SaveAs(string filePath, bool overwrite, IContentType newContentType)
        {
            SaveAs(filePath, overwrite, false, newContentType);
        }

        public void SaveCopy(string filePath, bool overwrite)
        {
            SaveCopy(filePath, overwrite, false);
        }

        public void SaveAs(string filePath, bool overwrite, bool createFolder)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("ITextDocument");
            }
            if (_raisingDirtyStateChangedEvent || _raisingFileActionChangedEvent)
            {
                throw new InvalidOperationException();
            }
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            PerformSave(overwrite ? FileMode.Create : FileMode.CreateNew, filePath, createFolder);
            UpdateSaveStatus(filePath, _filePath != filePath);

            // file path won't be updated if the save fails (in which case PerformSave will throw an exception)

            _filePath = filePath;
        }

        public void SaveAs(string filePath, bool overwrite, bool createFolder, IContentType newContentType)
        {
             if (newContentType == null)
            {
                throw new ArgumentNullException("newContentType");
            }
             SaveAs(filePath, overwrite, createFolder);
            // content type won't be changed if the save fails (in which case SaveAs will throw an exception)
            _textBuffer.ChangeContentType(newContentType, null);
        }

        public void SaveCopy(string filePath, bool overwrite, bool createFolder)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("ITextDocument");
            }
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            PerformSave(overwrite ? FileMode.Create : FileMode.CreateNew, filePath, createFolder);
            // Don't update save status
        }

        private void PerformSave(FileMode fileMode, string filePath, bool createFolder)
        {
            // check whether directory of the path exists
            if (createFolder)
            {
                string fileDirectoryName = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(fileDirectoryName) && !Directory.Exists(fileDirectoryName))
                {
                    Directory.CreateDirectory(fileDirectoryName);
                }
            }

            NewFileUtilities.SaveSnapshot(_textBuffer.CurrentSnapshot, fileMode, _encoding, filePath);
        }

        private void UpdateSaveStatus(string filePath, bool renamed)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            _lastSavedTimeUtc = fileInfo.LastWriteTimeUtc;
            _cleanReiteratedVersion = _textBuffer.CurrentSnapshot.Version.ReiteratedVersionNumber;

            FileActionTypes actionType = FileActionTypes.ContentSavedToDisk;
            if (renamed)
            {
                actionType |= FileActionTypes.DocumentRenamed;
            }
            RaiseFileActionChangedEvent(_lastSavedTimeUtc, actionType, filePath);
        }

        public void UpdateDirtyState(bool isDirtied, DateTime lastContentModifiedTimeUtc)
        {
            if (_raisingDirtyStateChangedEvent || _raisingFileActionChangedEvent)
            {
                throw new InvalidOperationException();
            }

            if (_isDisposed)
            {
                throw new ObjectDisposedException("ITextDocument");
            }

            _lastModifiedTimeUtc = lastContentModifiedTimeUtc;

            RaiseDirtyStateChangedEvent(isDirtied);
        }

        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                Encoding oldEncoding = _encoding;

                _encoding = value;
                
                if (!_encoding.Equals(oldEncoding))
                {
                    _textDocumentFactoryService._guardedOperations.RaiseEvent(this, EncodingChanged, new EncodingChangedEventArgs(oldEncoding, _encoding));
                }
            }
        }

        public void SetEncoderFallback(EncoderFallback fallback)
        {
            _encoding = Encoding.GetEncoding(_encoding.CodePage, fallback, _encoding.DecoderFallback);
            // no event here!
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_raisingDirtyStateChangedEvent || _raisingFileActionChangedEvent)
            {
                throw new InvalidOperationException();
            }

            if (!_isDisposed)
            {
                _textBuffer.ChangedHighPriority -= TextBufferChangedHandler;
                _textBuffer.Properties.RemoveProperty(typeof(ITextDocument));

                _isDisposed = true;

                _textDocumentFactoryService.RaiseTextDocumentDisposed(this);
                GC.SuppressFinalize(this);

                _textBuffer = null; // why?
            }
        }

        #endregion

        #region Private helpers

        private void TextBufferChangedHandler(object sender, TextContentChangedEventArgs e)
        {
            // We don't want to process textbuffer changes that were caused by ourselves
            if (e.EditTag != this)
            {
                _lastModifiedTimeUtc = DateTime.UtcNow;

                // If the edit was the result of an undo/redo action that took us back to the clean ReiteratedVersionNumber,
                // the document is no longer dirty
                if (e.AfterVersion.ReiteratedVersionNumber == _cleanReiteratedVersion)
                {
                    RaiseDirtyStateChangedEvent(false);
                }
                else
                {
                    RaiseDirtyStateChangedEvent(true);
                }
            }
        }

        /// <summary>
        /// Raises events for when the dirty state changes
        /// </summary>
        private void RaiseDirtyStateChangedEvent(bool newDirtyState)
        {
            _raisingDirtyStateChangedEvent = true;

            try
            {
                bool dirtyStateChanged = (_isDirty != newDirtyState);

                if (dirtyStateChanged)
                {
                    _isDirty = newDirtyState;

                    _textDocumentFactoryService._guardedOperations.RaiseEvent(this, DirtyStateChanged);
                }
            }
            finally
            {
                _raisingDirtyStateChangedEvent = false;
            }
        }

        /// <summary>
        /// Raises events for when a file load/save occurs.
        /// </summary>
        private void RaiseFileActionChangedEvent(DateTime actionTime, FileActionTypes actionType, string filePath)
        {
            _raisingFileActionChangedEvent = true;

            try
            {
                if ((actionType & FileActionTypes.ContentLoadedFromDisk) == FileActionTypes.ContentLoadedFromDisk ||
                    (actionType & FileActionTypes.ContentSavedToDisk) == FileActionTypes.ContentSavedToDisk)
                {
                    // We did a reload or a save so we probably want to clear the dirty flag unless someone modified
                    // the buffer -- changing the reiterated version number -- between the reload and when this call was made
                    // (for example, modifying the buffer in the text buffer changed event).
                    if (_cleanReiteratedVersion == _textBuffer.CurrentSnapshot.Version.ReiteratedVersionNumber)
                    {
                        RaiseDirtyStateChangedEvent(false);
                    }
                }

               _textDocumentFactoryService._guardedOperations.RaiseEvent(this, FileActionOccurred, new TextDocumentFileActionEventArgs(filePath, actionTime, actionType));

            }
            finally
            {
               _raisingFileActionChangedEvent = false;
            }
        }

        #endregion

        public event EventHandler<TextDocumentFileActionEventArgs> FileActionOccurred;
        public event EventHandler DirtyStateChanged;
        public event EventHandler<EncodingChangedEventArgs> EncodingChanged;

        /// <summary>
        /// An accessor for isDisposed to be used by <see cref="TextDocumentFactoryService"/>.
        /// </summary>
        internal bool IsDisposed
        {
            get { return _isDisposed; }
        }

        // This class is actually defined in Text.Dat
        // HACK: move SaveSnapshot to a method on the ITextSnapshot2 & keep the implementation in Text\TextModel
        internal class NewFileUtilities
        {
            public static void SaveSnapshot(ITextSnapshot snapshot,
                                            FileMode fileMode,
                                            Encoding encoding,
                                            string filePath)
            {
                Debug.Assert((fileMode == FileMode.Create) || (fileMode == FileMode.CreateNew));

                //Save the contents of the text buffer to disk.

                string temporaryFilePath = null;
                try
                {
                    FileStream originalFileStream = null;
                    FileStream temporaryFileStream = NewFileUtilities.CreateFileStream(filePath, fileMode, out temporaryFilePath, out originalFileStream);
                    if (originalFileStream == null)
                    {
                        //The "normal" scenario: save the snapshot directly to disk. Either:
                        // there are no hard links to the target file so we can write the snapshot to the temporary and use File.Replace.
                        // we're creating a new file (in which case, temporaryFileStream is a misnomer: it is the stream for the file we are creating).
                        try
                        {
                            using (StreamWriter streamWriter = new StreamWriter(temporaryFileStream, encoding))
                            {
                                snapshot.Write(streamWriter);
                            }
                        }
                        finally
                        {
                            //This is somewhat redundant: disposing of streamWriter had the side-effect of disposing of temporaryFileStream
                            temporaryFileStream.Dispose();
                            temporaryFileStream = null;
                        }

                        if (temporaryFilePath != null)
                        {
                            //We were saving to the original file and already have a copy of the file on disk.
                            int remainingAttempts = 3;
                            do
                            {
                                try
                                {
                                    //Replace the contents of filePath with the contents of the temporary using File.Replace to
                                    //preserve the various attributes of the original file.
                                    File.Replace(temporaryFilePath, filePath, null, true);
                                    temporaryFilePath = null;

                                    return;
                                }
                                catch (FileNotFoundException)
                                {
                                    // The target file doesn't exist (someone deleted it after we detected it earlier).
                                    // This is an acceptable condition so don't throw.
                                    File.Move(temporaryFilePath, filePath);
                                    temporaryFilePath = null;

                                    return;
                                }
                                catch (IOException)
                                {
                                    //There was some other exception when trying to replace the contents of the file
                                    //(probably because some other process had the file locked).
                                    //Wait a few ms and try again.
                                    System.Threading.Thread.Sleep(5);
                                }
                            }
                            while (--remainingAttempts > 0);

                            //We're giving up on replacing the file. Try overwriting it directly (this is essentially the old Dev11 behavior).
                            //Do not try approach we are using for hard links (copying the original & restoring it if there is a failure) since
                            //getting here implies something strange is going on with the file system (Git or the like locking files) so we
                            //want the simplest possible fallback.

                            //Failing here causes the exception to be passed to the calling code.
                            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                            {
                                using (StreamWriter streamWriter = new StreamWriter(stream, encoding))
                                {
                                    snapshot.Write(streamWriter);
                                }
                            }
                        }
                    }
                    else
                    {
                        //filePath has hard links so we need to use a different approach to save the file:
                        // copy the original file to the temporary
                        // write directly to the original
                        // restore the original in the event of errors (which could be encoding errors and not disk issues) if there's a problem.
                        try
                        {
                            // Copy the contents of the original file to the temporary.
                            originalFileStream.CopyTo(temporaryFileStream);

                            //We've got a clean copy, try writing the snapshot directly to the original file
                            try
                            {
                                originalFileStream.Seek(0, SeekOrigin.Begin);
                                originalFileStream.SetLength(0);

                                //Make sure the StreamWriter is flagged leaveOpen == true. Otherwise disposing of the StreamWriter will dispose of originalFileStream and we need to
                                //leave originalFileStream open so we can use it to restore the original from the temporary copy we made.
                                using (var streamWriter = new StreamWriter(originalFileStream, encoding, bufferSize: 1024, leaveOpen: true))        //1024 == the default buffer size for a StreamWriter.
                                {
                                    snapshot.Write(streamWriter);
                                }
                            }
                            catch
                            {
                                //Restore the original from the temporary copy we made (but rethrow the original exception since we didn't save the file).
                                temporaryFileStream.Seek(0, SeekOrigin.Begin);

                                originalFileStream.Seek(0, SeekOrigin.Begin);
                                originalFileStream.SetLength(0);

                                temporaryFileStream.CopyTo(originalFileStream);

                                throw;
                            }
                        }
                        finally
                        {
                            originalFileStream.Dispose();
                            originalFileStream = null;

                            temporaryFileStream.Dispose();
                            temporaryFileStream = null;
                        }
                    }
                }
                finally
                {
                    if (temporaryFilePath != null)
                    {
                        try
                        {
                            //We do not need the temporary any longer.
                            if (File.Exists(temporaryFilePath))
                            {
                                File.Delete(temporaryFilePath);
                            }
                        }
                        catch
                        {
                            //Failing to clean up the temporary is an ignorable exception.
                        }
                    }
                }
            }

            private static FileStream CreateFileStream(string filePath, FileMode fileMode, out string temporaryPath, out FileStream originalFileStream)
            {
                originalFileStream = null;

                if (File.Exists(filePath))
                {
                    // We're writing to a file that already exists. This is an error if we're trying to do a CreateNew.
                    if (fileMode == FileMode.CreateNew)
                    {
                        throw new IOException(filePath + " exists");
                    }

                    try
                    {
                        originalFileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                        //Even thoug SafeFileHandle is an IDisposable, we don't dispose of it since that closes the strem.
                        var safeHandle = originalFileStream.SafeFileHandle;
                        if (!(safeHandle.IsClosed || safeHandle.IsInvalid))
                        {
                            //BY_HANDLE_FILE_INFORMATION fi;
                            //if (GetFileInformationByHandle(safeHandle, out fi))
                            {
                                //if (fi.NumberOfLinks <= 1)
                                {
                                    // The file we're trying to write to doesn't have any hard links ... clear out the originalFileStream
                                    // as a clue.
                                    originalFileStream.Dispose();
                                    originalFileStream = null;
                                }
                            }
                        }
                    }
                    catch
                    {
                        if (originalFileStream != null)
                        {
                            originalFileStream.Dispose();
                            originalFileStream = null;
                        }

                        //We were not able to determine whether or not the file had hard links so throw here (aborting the save)
                        //since we don't know how to do it safely.
                        throw;
                    }

                    string root = Path.GetDirectoryName(filePath);

                    int count = 0;
                    while (++count < 20)
                    {
                        try
                        {
                            temporaryPath = Path.Combine(root, Path.GetRandomFileName() + "~");   //The ~ suffix hides the temporary file from GIT.
                            return new FileStream(temporaryPath, FileMode.CreateNew, (originalFileStream != null) ? FileAccess.ReadWrite : FileAccess.Write, FileShare.None);
                        }
                        catch (IOException)
                        {
                            //Ignore IOExceptions ... GetRandomFileName() came up with a duplicate so we need to try again.
                        }
                    }

                    Debug.Fail("Unable to create a temporary file");
                }

                temporaryPath = null;
                return new FileStream(filePath, fileMode, FileAccess.Write, FileShare.Read);
            }
            /*
            [StructLayout(LayoutKind.Sequential)]
            struct BY_HANDLE_FILE_INFORMATION
            {
                public uint FileAttributes;
                public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
                public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
                public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
                public uint VolumeSerialNumber;
                public uint FileSizeHigh;
                public uint FileSizeLow;
                public uint NumberOfLinks;
                public uint FileIndexHigh;
                public uint FileIndexLow;
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool GetFileInformationByHandle(
                Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
                out BY_HANDLE_FILE_INFORMATION lpFileInformation
            );
            */
        }
    }
}
