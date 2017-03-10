//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Editor;

    internal partial class TextDocument : ITextDocument
    {
        #region Private Members

        private readonly TextDocumentFactoryService _textDocumentFactoryService;
        private ITextBuffer _textBuffer;
        private Encoding _encoding;
        private string _filePath;
        //If the user explicitly chooses the encoding, we want to respect their chosen encoding
        private bool _explicitEncoding;

        //This corresponds to the tools option "Auto-detect UTF-8 encoding"
        //Unfortunately, we cannot dynamically read the option value without adding a dependency on TextLogic which results in a layering violation.
        //Therefore we're going to cache this value on creation of the text document and it will persist the lifetime of the document.
        private bool _attemptUtf8Detection = true;

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

        internal TextDocument(ITextBuffer textBuffer, string filePath, DateTime lastModifiedTime, TextDocumentFactoryService textDocumentFactoryService, Encoding encoding, bool explicitEncoding = false, bool attemptUtf8Detection = true)
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
            _explicitEncoding = explicitEncoding;
            _attemptUtf8Detection = attemptUtf8Detection;

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

        private void ReloadBufferFromStream(Stream stream, long fileSize, EditOptions options, Encoding encoding)
        {
            using (var streamReader = new EncodedStreamReader.NonStreamClosingStreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false))
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

                    StringRebuilder newContent = SimpleStringRebuilder.Create(loader);
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
            }
        }

        public ReloadResult Reload(EditOptions options)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ITextDocument));
            }
            if (_raisingDirtyStateChangedEvent || _raisingFileActionChangedEvent)
            {
                throw new InvalidOperationException();
            }

            Encoding newEncoding;
            var beforeSnapshot = _textBuffer.CurrentSnapshot;
            bool characterSubstitutionsOccurred = false;

            try
            {
                _reloadingFile = true;

                // Load the file and read the contents to the text buffer
                long fileSize;

                using (var stream = TextDocumentFactoryService.OpenFileGuts(_filePath, out _lastModifiedTimeUtc, out fileSize))
                {
                    var detectors = ExtensionSelector.SelectMatchingExtensions(_textDocumentFactoryService.OrderedEncodingDetectors, _textBuffer.ContentType);
                    
                    if(_explicitEncoding)
                    {
                        // If the user explicitly chose their encoding, we want to respect it.
                        newEncoding = this.Encoding;
                    }
                    else
                    {
                        newEncoding = EncodedStreamReader.DetectEncoding(stream, detectors, _textDocumentFactoryService.GuardedOperations);
                    }

                    if (newEncoding == null && _attemptUtf8Detection)
                    {
                        try
                        {
                            var detectorEncoding = new ExtendedCharacterDetector();

                            ReloadBufferFromStream(stream, fileSize, options, detectorEncoding);

                            if (detectorEncoding.DecodedExtendedCharacters)
                            {
                                // Valid UTF-8 but has bytes that are not merely ASCII.
                                newEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                            }
                            else
                            {
                                // Valid UTF8 but no extended characters, so it looks like valid ASCII.
                                // However, we don't use ASCII here because of the following scenario:
                                // The user with a non-English US system encoding opens a code file that happens to contain ASCII-only contents
                                // Therefore we'll just use their system encoding.
                                newEncoding = Encoding.Default;
                            }
                        }
                        catch (DecoderFallbackException)
                        {
                            // Not valid UTF-8.
                            // Proceed to the next if block to try the system's default codepage.
                            // For example, this occurs when you have extended characters like € in a UTF-8 file or ANSI file.
                            // We reset the stream so we can continue loading with the default system encoding.
                            Debug.Assert(newEncoding == null);
                            Debug.Assert(beforeSnapshot.Version.Next == null);
                            stream.Position = 0;
                        }
                    }

                    // If all else didn't work, use system's default encoding.
                    if (newEncoding == null)
                    {
                        newEncoding = Encoding.Default;
                    }

                    //If there is no "Next" version of the original snapshot, we have not successfully reloaded the document
                    if(beforeSnapshot.Version.Next == null)
                    {
                        //We use this fall back detector to observe whether or not character substitutions 
                        //occur while we're reading the stream
                        var fallbackDetector = new FallbackDetector(newEncoding.DecoderFallback);
                        var modifiedEncoding = (Encoding)newEncoding.Clone();
                        modifiedEncoding.DecoderFallback = fallbackDetector;

                        Debug.Assert(stream.Position == 0);
                        ReloadBufferFromStream(stream, fileSize, options, modifiedEncoding);

                        if(fallbackDetector.FallbackOccurred)
                        {
                            characterSubstitutionsOccurred = fallbackDetector.FallbackOccurred;
                        }
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
                return characterSubstitutionsOccurred ? ReloadResult.SucceededWithCharacterSubstitutions : ReloadResult.Succeeded;
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

            FileUtilities.SaveSnapshot(_textBuffer.CurrentSnapshot, fileMode, _encoding, filePath);
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
                    _textDocumentFactoryService.GuardedOperations.RaiseEvent(this, EncodingChanged, new EncodingChangedEventArgs(oldEncoding, _encoding));
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

                    _textDocumentFactoryService.GuardedOperations.RaiseEvent(this, DirtyStateChanged);
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

               _textDocumentFactoryService.GuardedOperations.RaiseEvent(this, FileActionOccurred, new TextDocumentFileActionEventArgs(filePath, actionTime, actionType));

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
    }
}
