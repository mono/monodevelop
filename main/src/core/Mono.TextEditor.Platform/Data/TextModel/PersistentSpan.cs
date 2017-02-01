// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Utilities;
    using System;
    using System.Diagnostics;

    internal sealed class PersistentSpan : IPersistentSpan
    {
        #region members
        private PersistentSpanFactory _factory;

        private ITrackingSpan _span;            //null for spans on closed documents or disposed spans
        private ITextDocument _document;        //null for spans on closed documents or disposed spans
        private string _filePath;               //null for spans on opened documents or disposed spans
        private int _startLine;                 //these parameters are valid whether or not the document is open (but _start*,_end* may be stale).
        private int _startIndex;
        private int _endLine;
        private int _endIndex;
        private Span _nonTrackingSpan;
        private bool _useLineIndex;

        private readonly SpanTrackingMode _trackingMode;
        #endregion

        internal PersistentSpan(ITextDocument document, SnapshotSpan span, SpanTrackingMode trackingMode, PersistentSpanFactory factory)
        {
            //Arguments verified in factory
            _document = document;

            _span = span.Snapshot.CreateTrackingSpan(span, trackingMode);
            _trackingMode = trackingMode;

            _factory = factory;
        }

        internal PersistentSpan(string filePath, int startLine, int startIndex, int endLine, int endIndex, SpanTrackingMode trackingMode, PersistentSpanFactory factory)
        {
            //Arguments verified in factory
            _filePath = filePath;

            _useLineIndex = true;
            _startLine = startLine;
            _startIndex = startIndex;
            _endLine = endLine;
            _endIndex = endIndex;

            _trackingMode = trackingMode;

            _factory = factory;
        }

        internal PersistentSpan(string filePath, Span span, SpanTrackingMode trackingMode, PersistentSpanFactory factory)
        {
            //Arguments verified in factory
            _filePath = filePath;

            _useLineIndex = false;
            _nonTrackingSpan = span;

            _trackingMode = trackingMode;

            _factory = factory;
        }

        #region IPersistentSpan members
        public bool IsDocumentOpen { get { return _document != null; } }

        public ITextDocument Document { get { return _document; } }

        public ITrackingSpan Span { get { return _span; } }

        public string FilePath
        {
            get
            {
                return (_document != null) ? _document.FilePath : _filePath;
            }
        }

        public bool TryGetStartLineIndex(out int startLine, out int startIndex)
        {
            if ((_document == null) && (_filePath == null))
                throw new ObjectDisposedException("PersistentSpan");

            if (_span != null)
                this.UpdateStartEnd();

            startLine = _startLine;
            startIndex = _startIndex;

            return ((_span != null) || _useLineIndex);
        }

        public bool TryGetEndLineIndex(out int endLine, out int endIndex)
        {
            if ((_document == null) && (_filePath == null))
                throw new ObjectDisposedException("PersistentSpan");

            if (_span != null)
                this.UpdateStartEnd();

            endLine = _endLine;
            endIndex = _endIndex;
            return ((_span != null) || _useLineIndex);
        }

        public bool TryGetSpan(out Span span)
        {
            if ((_document == null) && (_filePath == null))
                throw new ObjectDisposedException("PersistentSpan");

            if (_span != null)
                this.UpdateStartEnd();

            span = _nonTrackingSpan;
            return ((_span != null) || !_useLineIndex);
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
            if ((_document != null) || (_filePath != null))
            {
                _factory.Delete(this);

                _span = null;
                _document = null;
                _filePath = null;
            }
        }
        #endregion

        #region private helpers
        internal void DocumentClosed()
        {
            this.UpdateStartEnd();

            //We set this to false when the document is closed because we have an accurate line/index and that is more stable
            //than a simple offset.
            _useLineIndex = true;
            _nonTrackingSpan = new Span(0, 0);

            _filePath = _document.FilePath;
            _document = null;
            _span = null;
        }

        internal void DocumentReopened(ITextDocument document)
        {
            _document = document;

            ITextSnapshot snapshot = document.TextBuffer.CurrentSnapshot;

            SnapshotPoint start;
            SnapshotPoint end;
            if (_useLineIndex)
            {
                start = PersistentSpan.LineIndexToSnapshotPoint(_startLine, _startIndex, snapshot);
                end = PersistentSpan.LineIndexToSnapshotPoint(_endLine, _endIndex, snapshot);

                if (end < start)
                {
                    //Guard against the case where _start & _end are something like (100,2) & (101, 1).
                    //Those points would pass the argument validation (since _endLine > _startLine) but
                    //would cause problems if the document has only 5 lines since they would map to
                    //(5, 2) & (5, 1).
                    end = start;
                }
            }
            else
            {
                start = new SnapshotPoint(snapshot, Math.Min(_nonTrackingSpan.Start, snapshot.Length));
                end = new SnapshotPoint(snapshot, Math.Min(_nonTrackingSpan.End, snapshot.Length));
            }

            _span = snapshot.CreateTrackingSpan(new SnapshotSpan(start, end), _trackingMode);

            _filePath = null;
        }

        private void UpdateStartEnd()
        {
            SnapshotSpan span = _span.GetSpan(_span.TextBuffer.CurrentSnapshot);

            _nonTrackingSpan = span;

            PersistentSpan.SnapshotPointToLineIndex(span.Start, out _startLine, out _startIndex);
            PersistentSpan.SnapshotPointToLineIndex(span.End, out _endLine, out _endIndex);
        }

        private static void SnapshotPointToLineIndex(SnapshotPoint p, out int line, out int index)
        {
            ITextSnapshotLine l = p.GetContainingLine();

            line = l.LineNumber;
            index = Math.Min(l.Length, p - l.Start);
        }

        internal static SnapshotPoint LineIndexToSnapshotPoint(int line, int index, ITextSnapshot snapshot)
        {
            ITextSnapshotLine l = snapshot.GetLineFromLineNumber(Math.Min(line, snapshot.LineCount - 1));

            return l.Start + Math.Min(index, l.Length);
        }
        #endregion
    }
}