// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Projection.Implementation;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Factory for TextBuffers and ProjectionBuffers.
    /// </summary>

    [Export(typeof(ITextBufferFactoryService))]
    [Export(typeof(IProjectionBufferFactoryService))]
    internal partial class BufferFactoryService : ITextBufferFactoryService2, IProjectionBufferFactoryService, IInternalTextBufferFactory
    {
        #region Standard Content Type Definitions
        [Export]
        [Name("any")]
        public ContentTypeDefinition anyContentTypeDefinition;

        [Export]
        [Name("text")]
        [BaseDefinition("any")]
        public ContentTypeDefinition textContentTypeDefinition;

        [Export]
        [Name("projection")]
        [BaseDefinition("any")]
        public ContentTypeDefinition projectionContentTypeDefinition;

        [Export]
        [Name("plaintext")]
        [BaseDefinition("text")]
        public ContentTypeDefinition plaintextContentTypeDefinition;

        [Export]
        [Name("code")]
        [BaseDefinition("text")]
        public ContentTypeDefinition codeContentType;

        [Export]
        [Name("inert")]
        // N.B.: This ContentType does NOT inherit from anything
        public ContentTypeDefinition inertContentTypeDefinition;
        #endregion

        #region Service Consumptions

        [Import]
        internal IContentTypeRegistryService _contentTypeRegistryService { get; set; }

        [Import]
        internal IDifferenceService _differenceService { get; set; }
        
        [Import]
        internal ITextDifferencingSelectorService _textDifferencingSelectorService { get; set; }

        [Import]
        internal GuardedOperations _guardedOperations { get; set; }
    
        #endregion

        #region Private state
        private IContentType textContentType;
        private IContentType plaintextContentType;
        private IContentType inertContentType;
        private IContentType projectionContentType;
        #endregion

        #region ContentType accessors
        public IContentType TextContentType
        {
            get
            {
                if (this.textContentType == null)
                {
                    // it's OK to evaluate this more than once, and the assignment is atomic, so we don't protect this with a lock
                    this.textContentType = _contentTypeRegistryService.GetContentType("text");
                }
                return this.textContentType;
            }
        }

        public IContentType PlaintextContentType
        {
            get
            {
                if (this.plaintextContentType == null)
                {
                    // it's OK to evaluate this more than once, and the assignment is atomic, so we don't protect this with a lock
                    this.plaintextContentType = _contentTypeRegistryService.GetContentType("plaintext");
                }
                return this.plaintextContentType;
            }
        }

        public IContentType InertContentType
        {
            get
            {
                if (this.inertContentType == null)
                {
                    // it's OK to evaluate this more than once, and the assignment is atomic, so we don't protect this with a lock
                    this.inertContentType = _contentTypeRegistryService.GetContentType("inert");
                }
                return this.inertContentType;
            }
        }

        public IContentType ProjectionContentType
        {
            get
            {
                if (this.projectionContentType == null)
                {
                    // it's OK to evaluate this more than once, and the assignment is atomic, so we don't protect this with a lock
                    this.projectionContentType = _contentTypeRegistryService.GetContentType("projection");
                }
                return this.projectionContentType;
            }
        }
        #endregion

        public ITextBuffer CreateTextBuffer()
        {
            return Make(TextContentType, SimpleStringRebuilder.Create(String.Empty), false);
        }

        public ITextBuffer CreateTextBuffer(IContentType contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }
            return Make(contentType, SimpleStringRebuilder.Create(String.Empty), false);
        }

        public ITextBuffer CreateTextBuffer(string text, IContentType contentType)
        {
            return CreateTextBuffer(text, contentType, false);
        }

        public ITextBuffer CreateTextBuffer(SnapshotSpan span, IContentType contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            IStringRebuilder content = StringRebuilderFromSnapshotSpan(span);

            return Make(contentType, content, false);
        }

        public ITextBuffer CreateTextBuffer(string text, IContentType contentType, bool spurnGroup)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }
            return Make(contentType, SimpleStringRebuilder.Create(text), spurnGroup);
        }

        public ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType, long length, string traceId)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }
            if (length > int.MaxValue)
            {
                throw new InvalidOperationException(Strings.FileTooLarge);
            }

            ITextStorageLoader loader;
            if (length < TextModelOptions.CompressedStorageFileSizeThreshold)
            {
                loader = new SimpleTextStorageLoader(reader, (int)length);
            }
            else
            {
                loader = new CompressedTextStorageLoader(reader, (int)length, traceId);
            }
            IStringRebuilder content = SimpleStringRebuilder.Create(loader);

            ITextBuffer buffer = Make(contentType, content, false);
            if (!loader.HasConsistentLineEndings)
            {
                // leave a sign that line endings are inconsistent. This is rather nasty but for now
                // we don't want to pollute the API with this factoid
                buffer.Properties.AddProperty("InconsistentLineEndings", true);
            }
            // leave a similar sign about the longest line in the buffer.
            buffer.Properties.AddProperty("LongestLineLength", loader.LongestLineLength);
            return buffer;
        }

        public ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType)
        {
            return CreateTextBuffer(reader, contentType, -1, "legacy");
        }

        internal static IStringRebuilder StringRebuilderFromSnapshotSpan(SnapshotSpan span)
        {
            TextSnapshot snapshot = span.Snapshot as TextSnapshot;
            if (snapshot != null)
            {
                return snapshot.Content.Substring(span);
            }

            IProjectionSnapshot projectionSnapshot = span.Snapshot as IProjectionSnapshot;
            if (projectionSnapshot != null)
            {
                IStringRebuilder content = SimpleStringRebuilder.Create(string.Empty);

                foreach (var childSpan in projectionSnapshot.MapToSourceSnapshots(span))
                {
                    content = content.Append(StringRebuilderFromSnapshotSpan(childSpan));
                }

                return content;
            }

            //The we don't know what to do fallback. This should never be called unless someone provides a new snapshot
            //implementation.
            return SimpleStringRebuilder.Create(span.GetText());
        }

        private TextBuffer Make(IContentType contentType, IStringRebuilder content, bool spurnGroup)
        {
            TextBuffer buffer = new TextBuffer(contentType, content, _textDifferencingSelectorService.DefaultTextDifferencingService, _guardedOperations, spurnGroup);
            RaiseTextBufferCreatedEvent(buffer);
            return buffer;
        }

        public IProjectionBuffer CreateProjectionBuffer(IProjectionEditResolver projectionEditResolver, 
                                                        IList<object> trackingSpans,
                                                        ProjectionBufferOptions options,
                                                        IContentType contentType)
        {
            // projectionEditResolver is allowed to be null.
            if (trackingSpans == null)
            {
                throw new ArgumentNullException("trackingSpans");
            }
            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }
            IProjectionBuffer buffer = 
                new ProjectionBuffer(this, projectionEditResolver, contentType, trackingSpans, _differenceService, _textDifferencingSelectorService.DefaultTextDifferencingService, options, _guardedOperations);
            RaiseProjectionBufferCreatedEvent(buffer);
            return buffer;
        }

        public IProjectionBuffer CreateProjectionBuffer(IProjectionEditResolver projectionEditResolver,
                                                        IList<object> trackingSpans,
                                                        ProjectionBufferOptions options)
        {
            // projectionEditResolver is allowed to be null.
            if (trackingSpans == null)
            {
                throw new ArgumentNullException("trackingSpans");
            }

            IProjectionBuffer buffer =
                new ProjectionBuffer(this, projectionEditResolver, ProjectionContentType, trackingSpans, _differenceService, _textDifferencingSelectorService.DefaultTextDifferencingService, options, _guardedOperations);
            RaiseProjectionBufferCreatedEvent(buffer);
            return buffer;
        }

        public IElisionBuffer CreateElisionBuffer(IProjectionEditResolver projectionEditResolver,
                                                  NormalizedSnapshotSpanCollection exposedSpans,
                                                  ElisionBufferOptions options,
                                                  IContentType contentType)
        {
            // projectionEditResolver is allowed to be null.
            if (exposedSpans == null)
            {
                throw new ArgumentNullException("exposedSpans");
            }
            if (exposedSpans.Count == 0)
            {
                throw new ArgumentOutOfRangeException("exposedSpans");  // really?
            }
            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (exposedSpans[0].Snapshot != exposedSpans[0].Snapshot.TextBuffer.CurrentSnapshot)
            {
                // TODO:
                // build against given snapshot and then move forward if necessary?
                throw new ArgumentException("Elision buffer must be created against the current snapshot of its source buffer");
            }

            IElisionBuffer buffer = new ElisionBuffer(projectionEditResolver, contentType, exposedSpans[0].Snapshot.TextBuffer,
                                                      exposedSpans, options, _textDifferencingSelectorService.DefaultTextDifferencingService, _guardedOperations);
            RaiseProjectionBufferCreatedEvent(buffer);
            return buffer;
        }

        public IElisionBuffer CreateElisionBuffer(IProjectionEditResolver projectionEditResolver,
                                                  NormalizedSnapshotSpanCollection exposedSpans,
                                                  ElisionBufferOptions options)
        {
            return CreateElisionBuffer(projectionEditResolver, exposedSpans, options, ProjectionContentType);
        }

        public event EventHandler<TextBufferCreatedEventArgs> TextBufferCreated;
        public event EventHandler<TextBufferCreatedEventArgs> ProjectionBufferCreated;

        private void RaiseTextBufferCreatedEvent(ITextBuffer buffer)
        {
            EventHandler<TextBufferCreatedEventArgs> handler = TextBufferCreated;
            if (handler != null)
            {
                handler(this, new TextBufferCreatedEventArgs(buffer));
            }
        }

        private void RaiseProjectionBufferCreatedEvent(IProjectionBufferBase buffer)
        {
            EventHandler<TextBufferCreatedEventArgs> handler = ProjectionBufferCreated;
            if (handler != null)
            {
                handler(this, new TextBufferCreatedEventArgs(buffer));
            }
        }
    }
}
