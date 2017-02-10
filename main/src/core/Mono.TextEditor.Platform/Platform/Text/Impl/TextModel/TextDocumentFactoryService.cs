// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;
    using System.Diagnostics;

    [Export(typeof(ITextDocumentFactoryService))]
    internal sealed partial class TextDocumentFactoryService : ITextDocumentFactoryService
    {
        #region Internal Consumptions
        
        [Import]
        internal ITextBufferFactoryService _bufferFactoryService { get; set; }

        [ImportMany]
        internal List<Lazy<IEncodingDetector, IEncodingDetectorMetadata>> _unorderedEncodingDetectors { get; set; }

        [Import]
        internal GuardedOperations _guardedOperations { get; set; }

        #endregion

        internal static Encoding DefaultEncoding = Encoding.Default; // Exposed for unit tests.

        #region ITextDocumentFactoryService Members

        public ITextDocument CreateAndLoadTextDocument(string filePath, IContentType contentType)
        {
            bool unused;
            return CreateAndLoadTextDocument(filePath, contentType, attemptUtf8Detection: true, characterSubstitutionsOccurred: out unused);
        }

        public ITextDocument CreateAndLoadTextDocument(string filePath, IContentType contentType, Encoding encoding, out bool characterSubstitutionsOccurred)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            var fallbackDetector = new FallbackDetector(encoding.DecoderFallback);
            var modifiedEncoding = (Encoding)encoding.Clone();
            modifiedEncoding.DecoderFallback = fallbackDetector;

            ITextBuffer buffer;
            DateTime lastModified;
            long fileSize;
            using (Stream stream = OpenFile(filePath, out lastModified, out fileSize))
            {
                // Caller knows best, so don't use byte order marks.
                using (StreamReader reader = new StreamReader(stream, modifiedEncoding, detectEncodingFromByteOrderMarks: false))
                {
                    System.Diagnostics.Debug.Assert(encoding.CodePage == reader.CurrentEncoding.CodePage);
                    buffer = ((ITextBufferFactoryService2)_bufferFactoryService).CreateTextBuffer(reader, contentType, fileSize, filePath);
                }
            }

            characterSubstitutionsOccurred = fallbackDetector.FallbackOccurred;

#if _DEBUG
            TextUtilities.TagBuffer(buffer, filePath);
#endif
            TextDocument textDocument = new TextDocument(buffer, filePath, lastModified, this, encoding);

            RaiseTextDocumentCreated(textDocument);

            return textDocument;
        }

        public ITextDocument CreateAndLoadTextDocument(string filePath, IContentType contentType, bool attemptUtf8Detection, out bool characterSubstitutionsOccurred)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            characterSubstitutionsOccurred = false;

            Encoding chosenEncoding = null;
            ITextBuffer buffer = null;
            DateTime lastModified;
            long fileSize;

            // select matching detectors without instantiating any
            var detectors = ExtensionSelector.SelectMatchingExtensions(OrderedEncodingDetectors, contentType);

            using (Stream stream = OpenFile(filePath, out lastModified, out fileSize))
            {
                // First, look for a byte order marker and let the encoding detecters
                // suggest encodings.
                chosenEncoding = EncodedStreamReader.DetectEncoding(stream, detectors, _guardedOperations);

                // If that didn't produce a result, tentatively try to open as UTF 8.
                if (chosenEncoding == null && attemptUtf8Detection)
                {
                    try
                    {
                        var detectorEncoding = new ExtendedCharacterDetector();

                        using (StreamReader reader = new EncodedStreamReader.NonStreamClosingStreamReader(stream, detectorEncoding, false))
                        {
                            buffer = ((ITextBufferFactoryService2)_bufferFactoryService).CreateTextBuffer(reader, contentType, fileSize, filePath);
                            characterSubstitutionsOccurred = false;
                        }

                        if (detectorEncoding.DecodedExtendedCharacters)
                        {
                            // Valid UTF-8 but has bytes that are not merely ASCII.
                            chosenEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                        }
                        else
                        {
                            // Valid UTF8 but no extended characters, so it's valid ASCII.

                            // We don't use ASCII here because of the following scenario:
                            // The user with a non-ENU system encoding opens a code file with ASCII-only contents,
                            // 

                            chosenEncoding = DefaultEncoding;
                        }
                    }
                    catch (DecoderFallbackException)
                    {
                        // Not valid UTF-8.
                        // Proceed to the next if block to try the system's default codepage.
                        Debug.Assert(buffer == null);
                        buffer = null;
                        stream.Position = 0;
                    }
                }

                Debug.Assert(buffer == null || chosenEncoding != null);

                // If all else didn't work, use system's default encoding.
                if (chosenEncoding == null)
                    chosenEncoding = DefaultEncoding;

                if (buffer == null)
                {
                    var fallbackDetector = new FallbackDetector(chosenEncoding.DecoderFallback);
                    var modifiedEncoding = (Encoding)chosenEncoding.Clone();
                    modifiedEncoding.DecoderFallback = fallbackDetector;

                    Debug.Assert(stream.Position == 0);

                    using (StreamReader reader = new EncodedStreamReader.NonStreamClosingStreamReader(stream, modifiedEncoding, detectEncodingFromByteOrderMarks: false))
                    {
                        Debug.Assert(chosenEncoding.CodePage == reader.CurrentEncoding.CodePage);
                        buffer = ((ITextBufferFactoryService2)_bufferFactoryService).CreateTextBuffer(reader, contentType, fileSize, filePath);
                    }

                    characterSubstitutionsOccurred = fallbackDetector.FallbackOccurred;
                }
            }

            TextDocument textDocument = new TextDocument(buffer, filePath, lastModified, this, chosenEncoding);

            RaiseTextDocumentCreated(textDocument);

            return textDocument;
        }

        public ITextDocument CreateTextDocument(ITextBuffer textBuffer, string filePath)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }

            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            TextDocument textDocument = new TextDocument(textBuffer, filePath, DateTime.UtcNow, this, Encoding.UTF8);
            RaiseTextDocumentCreated(textDocument);

            return textDocument;
        }

        public bool TryGetTextDocument(ITextBuffer textBuffer, out ITextDocument textDocument)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }

            textDocument = null;

            TextDocument document;
            if (textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document))
            {
                if(document != null && !document.IsDisposed)
                {
                    textDocument = document;
                    return true;
                }
                else
                {
                    Debug.Fail("There shouldn't be a null or disposed document in the buffer's property bag.  Did someone else put it there?");
                }
            }

            return false;
        }

        public event EventHandler<TextDocumentEventArgs> TextDocumentCreated;

        public event EventHandler<TextDocumentEventArgs> TextDocumentDisposed;

        #endregion

        #region helpers

        /// <summary>
        /// Helper method to raise the <see cref="ITextDocumentFactoryService.TextDocumentCreated"/> event.
        /// </summary>
        /// <param name="textDocument">The <see cref="ITextDocument"/> that was created.</param>
        private void RaiseTextDocumentCreated(ITextDocument textDocument)
        {
            EventHandler<TextDocumentEventArgs> documentCreated = this.TextDocumentCreated;
            if (documentCreated != null)
            {
                documentCreated.Invoke(this, new TextDocumentEventArgs(textDocument));
            }
        }

        /// <summary>
        /// Helper method to raise the <see cref="ITextDocumentFactoryService.TextDocumentDisposed"/> event.
        /// </summary>
        /// <param name="textDocument">The <see cref="ITextDocument"/> that was disposed.</param>
        internal void RaiseTextDocumentDisposed(ITextDocument textDocument)
        {
            EventHandler<TextDocumentEventArgs> documentDisposed = this.TextDocumentDisposed;
            if (documentDisposed != null)
            {
                documentDisposed.Invoke(this, new TextDocumentEventArgs(textDocument));
            }
        }

        private IList<Lazy<IEncodingDetector, IEncodingDetectorMetadata>> _orderedEncodingDetectors;

        internal IEnumerable<Lazy<IEncodingDetector, IEncodingDetectorMetadata>> OrderedEncodingDetectors
        {
            get
            {
                if (_orderedEncodingDetectors == null)
                {
                    if (_unorderedEncodingDetectors != null)
                    {
                        _orderedEncodingDetectors = Orderer.Order(_unorderedEncodingDetectors);
                    }
                    else
                    {
                        _orderedEncodingDetectors = new List<Lazy<IEncodingDetector, IEncodingDetectorMetadata>>();
                    }
                }
                return _orderedEncodingDetectors;
            }
            set // for unit test helper.
            {
                _orderedEncodingDetectors = new List<Lazy<IEncodingDetector, IEncodingDetectorMetadata>>(value);
            }
        }

        // Exposed for testing.
        internal Func<string, Stream> StreamCreator;

        private Stream OpenFile(string filePath, out DateTime lastModifiedTimeUtc, out long fileSize)
        {
            if (StreamCreator != null)
            {
                lastModifiedTimeUtc = DateTime.UtcNow;
                fileSize = -1;  // a signal that the file size is not known
                return StreamCreator(filePath);
            }
            else
            {
                return OpenFileGuts(filePath, out lastModifiedTimeUtc, out fileSize);
            }
        }

        internal static Stream OpenFileGuts(string filePath, out DateTime lastModifiedTimeUtc, out long fileSize)
        {
            // Sometimes files are held open with FILE_FLAG_DELETE_ON_CLOSE before the editor
            // is asked to open them. We should support that by allowing FileShare.Delete.
            Stream result = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            FileInfo fileInfo = new FileInfo(filePath);
            lastModifiedTimeUtc = fileInfo.LastWriteTimeUtc;
            fileSize = fileInfo.Length;
            if (fileSize > int.MaxValue)
            {
                throw new InvalidOperationException(Strings.FileTooLarge);
            }

            return result;
        }

        // For unit testing purposes
        internal void Initialize(ITextBufferFactoryService bufferFactoryService)
        {
            Initialize(bufferFactoryService, null);
        }

        internal void Initialize(ITextBufferFactoryService bufferFactoryService, List<Lazy<IEncodingDetector, IEncodingDetectorMetadata>> detectors)
        {
            _bufferFactoryService = bufferFactoryService;
            _unorderedEncodingDetectors = detectors ?? new List<Lazy<IEncodingDetector, IEncodingDetectorMetadata>>();
        }

        #endregion
    }
}
