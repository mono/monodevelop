// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;
    using Microsoft.VisualStudio.Text.Projection;

    /// <summary>
    /// Tag Aggregator options.
    /// </summary>
    [Flags]
    public enum TagAggregatorOptions2
    {
        /// <summary>
        /// Default behavior. The tag aggregator will map up and down through all projection buffers.
        /// </summary>
        None = TagAggregatorOptions.None,

        /// <summary>
        /// Only map through projection buffers that have the "projection" content type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Normally, a tag aggregator will map up and down through all projection buffers (buffers
        /// that implement <see cref="IProjectionBufferBase"/>).  This flag will cause the projection buffer
        /// to not map through buffers that are projection buffers but do not have a projection content type.
        /// </para>
        /// </remarks>
        /// <comment>This is used by the classifier aggregator, as classification depends on content type.</comment>
        MapByContentType = TagAggregatorOptions.MapByContentType,

        /// <summary>
        /// Delay creating the taggers for the tag aggregator.
        /// </summary>
        /// <remarks>
        /// <para>A tag aggregator will, normally, create all of its taggers when it is created. This option
        /// will cause the tagger to defer the creation until idle time tasks are done.</para>
        /// <para>If this option is set, a TagsChanged event will be raised after the taggers have been created.</para>
        /// </remarks>
        DeferTaggerCreation = 0x02
    }
}