// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Utilities;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.IO;

    [Export(typeof(IPersistentSpanFactory))]
    internal class PersistentSpanFactory : IPersistentSpanFactory
    {
        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService;

        private readonly Dictionary<object, FrugalList<PersistentSpan>> _spansOnDocuments = new Dictionary<object, FrugalList<PersistentSpan>>();   //Used for lock

        private bool _eventsHooked;

        #region IPersistentSpanFactory members
        public bool CanCreate(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            ITextDocument document;
            return this.TextDocumentFactoryService.TryGetTextDocument(buffer, out document);
        }

        public IPersistentSpan Create(SnapshotSpan span, SpanTrackingMode trackingMode)
        {
            ITextDocument document;
            if (this.TextDocumentFactoryService.TryGetTextDocument(span.Snapshot.TextBuffer, out document))
            {
                PersistentSpan persistentSpan = new PersistentSpan(document, span, trackingMode, this);

                this.AddSpan(document, persistentSpan);

                return persistentSpan;
            }

            return null;
        }

        public IPersistentSpan Create(ITextSnapshot snapshot, int startLine, int startIndex, int endLine, int endIndex, SpanTrackingMode trackingMode)
        {
            ITextDocument document;
            if (this.TextDocumentFactoryService.TryGetTextDocument(snapshot.TextBuffer, out document))
            {
                var start = PersistentSpan.LineIndexToSnapshotPoint(startLine, startIndex, snapshot);
                var end = PersistentSpan.LineIndexToSnapshotPoint(endLine, endIndex, snapshot);
                if (end < start)
                {
                    end = start;
                }

                PersistentSpan persistentSpan = new PersistentSpan(document, new SnapshotSpan(start, end), trackingMode, this);

                this.AddSpan(document, persistentSpan);

                return persistentSpan;
            }

            return null;
        }

        public IPersistentSpan Create(string filePath, int startLine, int startIndex, int endLine, int endIndex, SpanTrackingMode trackingMode)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("filePath");
            }
            if (startLine < 0)
            {
                throw new ArgumentOutOfRangeException("startLine", "Must be non-negative.");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", "Must be non-negative.");
            }
            if (endLine < startLine)
            {
                throw new ArgumentOutOfRangeException("endLine", "Must be >= startLine.");
            }
            if ((endIndex < 0) || ((startLine == endLine) && (endIndex < startIndex)))
            {
                throw new ArgumentOutOfRangeException("endIndex", "Must be non-negative and (endLine,endIndex) may not be before (startLine,startIndex).");
            }
            if (((int)trackingMode < (int)SpanTrackingMode.EdgeExclusive) || ((int)trackingMode > (int)(SpanTrackingMode.EdgeNegative)))
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }

            PersistentSpan persistentSpan = new PersistentSpan(filePath, startLine, startIndex, endLine, endIndex, trackingMode, this);

            this.AddSpan(new FileNameKey(filePath), persistentSpan);

            return persistentSpan;
        }

        public IPersistentSpan Create(string filePath, Span span, SpanTrackingMode trackingMode)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("filePath");
            }
            if (((int)trackingMode < (int)SpanTrackingMode.EdgeExclusive) || ((int)trackingMode > (int)(SpanTrackingMode.EdgeNegative)))
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }

            PersistentSpan persistentSpan = new PersistentSpan(filePath, span, trackingMode, this);

            this.AddSpan(new FileNameKey(filePath), persistentSpan);

            return persistentSpan;
        }
        #endregion

        internal bool IsEmpty {  get { return _spansOnDocuments.Count == 0; } } //For unit tests

        private void AddSpan(object key, PersistentSpan persistentSpan)
        {
            lock (_spansOnDocuments)
            {
                FrugalList<PersistentSpan> spans;
                if (!_spansOnDocuments.TryGetValue(key, out spans))
                {
                    this.EnsureEventsHooked();

                    spans = new FrugalList<PersistentSpan>();
                    _spansOnDocuments.Add(key, spans);
                }

                spans.Add(persistentSpan);
            }
        }

        private void EnsureEventsHooked()
        {
            if (!_eventsHooked)
            {
                _eventsHooked = true;

                this.TextDocumentFactoryService.TextDocumentCreated += OnTextDocumentCreated;
                this.TextDocumentFactoryService.TextDocumentDisposed += OnTextDocumentDisposed;
            }
        }

        private void OnTextDocumentCreated(object sender, TextDocumentEventArgs e)
        {
            var path = new FileNameKey(e.TextDocument.FilePath);
            FrugalList<PersistentSpan> spans;
            lock (_spansOnDocuments)
            {
                if (_spansOnDocuments.TryGetValue(path, out spans))
                {
                    foreach (var span in spans)
                    {
                        span.DocumentReopened(e.TextDocument);
                    }

                    _spansOnDocuments.Remove(path);
                    _spansOnDocuments.Add(e.TextDocument, spans);
                }
            }
        }

        private void OnTextDocumentDisposed(object sender, TextDocumentEventArgs e)
        {
            FrugalList<PersistentSpan> spans;
            lock (_spansOnDocuments)
            {
                if (_spansOnDocuments.TryGetValue(e.TextDocument, out spans))
                {
                    foreach (var span in spans)
                    {
                        span.DocumentClosed();
                    }

                    _spansOnDocuments.Remove(e.TextDocument);

                    var path = new FileNameKey(e.TextDocument.FilePath);
                    FrugalList<PersistentSpan> existingSpansOnPath;
                    if (_spansOnDocuments.TryGetValue(path, out existingSpansOnPath))
                    {
                        //Handle (badly) the case where a document is renamed to an existing closed document & then closed.
                        existingSpansOnPath.AddRange(spans);
                    }
                    else
                    {
                        _spansOnDocuments.Add(path, spans);
                    }
                }
            }
        }

        internal void Delete(PersistentSpan span)
        {
            lock (_spansOnDocuments)
            {
                ITextDocument document = span.Document;
                if (document != null)
                {
                    FrugalList<PersistentSpan> spans;
                    if (_spansOnDocuments.TryGetValue(document, out spans))
                    {
                        spans.Remove(span);

                        if (spans.Count == 0)
                        {
                            //Last one ... remove all references to document.
                            _spansOnDocuments.Remove(document);
                        }
                    }
                    else
                    {
                        Debug.Fail("There should have been an entry in SpanOnDocuments.");
                    }
                }
                else
                {
                    var path = new FileNameKey(span.FilePath);
                    FrugalList<PersistentSpan> spans;
                    if (_spansOnDocuments.TryGetValue(path, out spans))
                    {
                        spans.Remove(span);

                        if (spans.Count == 0)
                        {
                            //Last one ... remove all references to path.
                            _spansOnDocuments.Remove(path);
                        }
                    }
                    else
                    {
                        Debug.Fail("There should have been an entry in SpanOnDocuments.");
                    }
                }
            }
        }

        private class FileNameKey
        {
            private readonly string _fileName;
            private readonly int _hashCode;

            public FileNameKey(string fileName)
            {
                //Gracefully catch errors getting the full path (which can happen if the file name is on a protected share).
                try
                {
                    _fileName = Path.GetFullPath(fileName);
                }
                catch
                {
                    //This shouldn't happen (we are generally passed names associated with documents that we are expecting to open so
                    //we should have access). If we fail, we will, at worst not get the same underlying document when people create
                    //persistent spans using unnormalized names.
                    _fileName = fileName;
                }

                _hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(_fileName);
            }

            //Override equality and hash code
            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override bool Equals(object obj)
            {
                var other = obj as FileNameKey;
                return (other != null) && string.Equals(_fileName, other._fileName, StringComparison.OrdinalIgnoreCase);
            }

            public override string ToString()
            {
                return _fileName;
            }
        }
    }
}
