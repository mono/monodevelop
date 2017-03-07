//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Tagging.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.Composition;
    using System.Linq;

    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Utilities;

    /// <summary>
    /// Exports the TagAggregator provider, both the buffer and view version.
    /// </summary>
    [Export(typeof(IBufferTagAggregatorFactoryService))]
    [Export(typeof(IViewTagAggregatorFactoryService))]
    internal sealed class TagAggregatorFactoryService : IBufferTagAggregatorFactoryService, IViewTagAggregatorFactoryService
    {
        [ImportMany(typeof(ITaggerProvider))]
        internal List<Lazy<ITaggerProvider, INamedTaggerMetadata>> BufferTaggerProviders { get; set; }

        [ImportMany(typeof(IViewTaggerProvider))]
        internal List<Lazy<IViewTaggerProvider, IViewTaggerMetadata>> ViewTaggerProviders { get; set; }

        [Import]
        internal IBufferGraphFactoryService BufferGraphFactoryService { get; set; }

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        [Import]
        internal IThreadHelper ThreadHelper { get; set; }

        [Import]
        internal GuardedOperations GuardedOperations { get; set; }

        internal ImmutableDictionary<ContentAndTypeData, IEnumerable<Lazy<ITaggerProvider, INamedTaggerMetadata>>> _bufferTaggerProviderMap = ImmutableDictionary<ContentAndTypeData, IEnumerable<Lazy<ITaggerProvider, INamedTaggerMetadata>>>.Empty;
        internal ImmutableDictionary<ContentAndTypeData, IEnumerable<Lazy<IViewTaggerProvider, IViewTaggerMetadata>>> _viewTaggerProviderMap = ImmutableDictionary<ContentAndTypeData, IEnumerable<Lazy<IViewTaggerProvider, IViewTaggerMetadata>>>.Empty;

        #region IBufferTagAggregatorFactoryService Members

        public ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer) where T : ITag
        {
            return CreateTagAggregator<T>(textBuffer, TagAggregatorOptions.None);
        }

        public ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer, TagAggregatorOptions options) where T : ITag
        {
            if (textBuffer == null)
                throw new ArgumentNullException("textBuffer");

            return new TagAggregator<T>(this, null, this.BufferGraphFactoryService.CreateBufferGraph(textBuffer), options);

        }

        #endregion

        #region IViewTagAggregatorFactoryService Members

        public ITagAggregator<T> CreateTagAggregator<T>(ITextView textView) where T : ITag
        {
            return CreateTagAggregator<T>(textView, TagAggregatorOptions.None);
        }

        public ITagAggregator<T> CreateTagAggregator<T>(ITextView textView, TagAggregatorOptions options) where T : ITag
        {
            if (textView == null)
                throw new ArgumentNullException("textView");

            return new TagAggregator<T>(this, textView, textView.BufferGraph, options);
        }

        #endregion

        internal IEnumerable<Lazy<ITaggerProvider, INamedTaggerMetadata>> GetBufferTaggersForType(IContentType type, Type taggerType)
        {
            var key = new ContentAndTypeData(type, taggerType);

            IEnumerable<Lazy<ITaggerProvider, INamedTaggerMetadata>> taggers;
            if (!_bufferTaggerProviderMap.TryGetValue(key, out taggers))
            {
                taggers = new List<Lazy<ITaggerProvider, INamedTaggerMetadata>>(this.BufferTaggerProviders.Where(f => Match(type, taggerType, f.Metadata)));

                ImmutableInterlocked.Update(ref _bufferTaggerProviderMap, (s) => s.Add(key, taggers));
            }

            return taggers;
        }

        internal IEnumerable<Lazy<IViewTaggerProvider, IViewTaggerMetadata>> GetViewTaggersForType(IContentType type, Type taggerType)
        {
            var key = new ContentAndTypeData(type, taggerType);

            IEnumerable<Lazy<IViewTaggerProvider, IViewTaggerMetadata>> taggers;
            if (!_viewTaggerProviderMap.TryGetValue(key, out taggers))
            {
                taggers = new List<Lazy<IViewTaggerProvider, IViewTaggerMetadata>>(this.ViewTaggerProviders.Where(f => Match(type, taggerType, f.Metadata)));

                ImmutableInterlocked.Update(ref _viewTaggerProviderMap, (s) => s.Add(key, taggers));
            }

            return taggers;
        }

        private static bool Match(IContentType bufferContentType, Type taggerType, INamedTaggerMetadata tagMetadata)
        {
            bool contentTypeMatch = false;

            foreach (string contentType in tagMetadata.ContentTypes)
            {
                if (bufferContentType.IsOfType(contentType))
                {
                    contentTypeMatch = true;
                    break;
                }
            }

            if (contentTypeMatch)
            {
                // Now find out if it can provide tags of the type we want
                foreach (Type type in tagMetadata.TagTypes)
                {
                    // This producer is used if it claims to produce a tag
                    // that this type is assignable from.
                    if (taggerType.IsAssignableFrom(type))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal class ContentAndTypeData
        {
            public readonly IContentType ContentType;
            public readonly Type TaggerType;

            public ContentAndTypeData(IContentType contentType, Type taggerType)
            {
                this.ContentType = contentType;
                this.TaggerType = taggerType;
            }

            public override bool Equals(object obj)
            {
                var other = obj as ContentAndTypeData;
                return (other != null) && (other.ContentType == this.ContentType) && (other.TaggerType == this.TaggerType);
            }

            public override int GetHashCode()
            {
                return this.ContentType.GetHashCode() ^ this.TaggerType.GetHashCode();
            }
        }
    }
}
