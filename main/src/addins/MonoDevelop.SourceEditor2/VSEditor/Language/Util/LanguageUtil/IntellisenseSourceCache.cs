// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Language.Utilities
{
    internal static class IntellisenseSourceCache
    {
        public static IEnumerable<TSource> GetSources<TSource>(ITextView textView, ITextBuffer buffer, Func<ITextBuffer, IEnumerable<TSource>> sourceCreator) where TSource : IDisposable
        {
            var viewCache = textView.Properties.GetOrCreateSingletonProperty(() => new ViewSourceCache<TSource>(textView, sourceCreator));
            return viewCache.GetSources(new ITextBuffer[] { buffer });
        }

        public static IEnumerable<TSource> GetSources<TSource>(
            ITextView textView,
            IReadOnlyCollection<ITextBuffer> buffers,
            Func<ITextBuffer, IEnumerable<TSource>> sourceCreator) where TSource : IDisposable
        {
            var viewCache = textView.Properties.GetOrCreateSingletonProperty(() => new ViewSourceCache<TSource>(textView, sourceCreator));
            return viewCache.GetSources(buffers);
        }

        private class ViewSourceCache<TSource> where TSource : IDisposable
        {
            private readonly ITextView _textView;
            private readonly Func<ITextBuffer, IEnumerable<TSource>> _sourceCreator;
            private readonly HybridDictionary _bufferCache = new HybridDictionary();

            public ViewSourceCache(ITextView textView, Func<ITextBuffer, IEnumerable<TSource>> sourceCreator)
            {
                _textView = textView;
                _sourceCreator = sourceCreator;

                _textView.Closed += (sender, args) => this.HandleViewClosed();
            }

            public IEnumerable<TSource> GetSources(IEnumerable<ITextBuffer> buffers)
            {
                if (_textView.IsClosed)
                {
                    Debug.Fail("Intellisense is trying to operate after its view has been closed.");
                    return Enumerable.Empty<TSource>();
                }

                // For each buffer that should be involved, either return the cached source for that buffer, or create a source and
                // cache it for later use.
                IList<TSource> sources = new FrugalList<TSource>();
                foreach (var buffer in buffers)
                {
                    var bufferSources = (IEnumerable<TSource>) _bufferCache[buffer];

                    if (bufferSources == null)
                    {
                        bufferSources = _sourceCreator(buffer);
                        _bufferCache.Add(buffer, bufferSources);

                        buffer.ContentTypeChanged += OnContentTypeChanged;
                    }

                    foreach (var source in bufferSources)
                    {
                        sources.Add(source);
                    }
                }

                return sources;
            }

            private void OnContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
            {
                var buffer = (ITextBuffer)sender;
                buffer.ContentTypeChanged -= OnContentTypeChanged;

                var bufferSources = (IEnumerable<TSource>)_bufferCache[buffer];

                if (bufferSources != null)
                {
                    foreach (TSource source in bufferSources)
                    {
                        source.Dispose();
                    }
                }

                _bufferCache.Remove(buffer);
            }

            public void HandleViewClosed()
            {
                foreach (ITextBuffer buffer in _bufferCache.Keys)
                {
                    buffer.ContentTypeChanged -= OnContentTypeChanged;
                }

                foreach (IEnumerable<TSource> sourcesList in _bufferCache.Values)
                {
                    foreach (var source in sourcesList)
                    {
                        source.Dispose();
                    }
                }

                _bufferCache.Clear();
            }
        }
    }
}
