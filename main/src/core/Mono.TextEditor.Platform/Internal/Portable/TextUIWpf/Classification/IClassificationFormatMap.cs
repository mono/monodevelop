// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Classification
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// Maps from a <see cref="IClassificationType"/> to an <see cref="TextFormattingRunProperties"/> object.
    /// </summary>
    public interface IClassificationFormatMap
    {
        /// <summary>
        /// Gets the effective <see cref="TextFormattingRunProperties"/> for a given text classification type including the
        /// properties of <paramref name="classificationType"/> and any properties that it might inherit.
        /// </summary>
        /// <param name="classificationType">
        /// The <see cref="IClassificationType"/> whose merged text properties should be returned.
        /// </param>
        /// <returns>
        /// The <see cref="TextFormattingRunProperties"/> object that represents the merged set of text properties
        /// from the specified classification type.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="classificationType"/> is null.</exception>
        TextFormattingRunProperties GetTextProperties(IClassificationType classificationType);

        /// <summary>
        /// Gets the explicit <see cref="TextFormattingRunProperties"/> for <paramref name="classificationType"/>.
        /// </summary>
        /// <param name="classificationType">
        /// The <see cref="IClassificationType"/> whose text properties should be returned.
        /// </param>
        /// <returns>
        /// The <see cref="TextFormattingRunProperties"/> object that represents the properties of the <paramref name="classificationType"/>.
        /// These properties will include only properties defined for <paramref name="classificationType"/> excluding properties that might
        /// be inherited.
        /// </returns>
        TextFormattingRunProperties GetExplicitTextProperties(IClassificationType classificationType);

        /// <summary>
        /// Gets the key used to store the associated properties of <paramref name="classificationType"/> in the 
        /// underlying <see cref="IEditorFormatMap"/>.
        /// </summary>
        /// <param name="classificationType">
        /// The <see cref="IClassificationType"/> whose key is returned.</param>
        /// <returns>
        /// The key that's used to store the properties of <paramref name="classificationType"/> in the underlying <see cref="IEditorFormatMap"/>.
        /// </returns>
        string GetEditorFormatMapKey(IClassificationType classificationType);

        /// <summary>
        /// Adds a <see cref="TextFormattingRunProperties"/> to a new <see cref="IClassificationType"/>.
        /// </summary>
        /// <param name="classificationType">The <see cref="IClassificationType"/>.</param>
        /// <param name="properties">The new properties.</param>
        /// <remarks>
        /// <para>Adding the text properties will cause the <see cref="ClassificationFormatMappingChanged"/> event to be sent.</para>
        /// <para><paramref name="classificationType"/> has the highest priority.</para>
        /// <para>If <paramref name="classificationType"/> already exists in the map, then this is equivalent to <see cref="SetTextProperties"/>(classificationType, properties).</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="classificationType"/> is null.</exception>
        void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties);

        /// <summary>
        /// Adds a <see cref="TextFormattingRunProperties"/> to a new <see cref="IClassificationType"/>.
        /// </summary>
        /// <param name="classificationType">The <see cref="IClassificationType"/>.</param>
        /// <param name="properties">The new properties.</param>
        /// <param name="priority">The <see cref="IClassificationType"/> that defines the relative prority of <paramref name="classificationType"/>.</param>
        /// <remarks>
        /// <para>Adding the text properties will cause the <see cref="ClassificationFormatMappingChanged"/> event to be sent.</para>
        /// <para>The priority of <paramref name="classificationType"/> will be lower than that of <paramref name="priority"/>.</para>
        /// <para>If <paramref name="classificationType"/> already exists in the map, then this is equivalent to <see cref="SetTextProperties"/>(classificationType, properties).</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="classificationType"/>, <paramref name="properties"/> or
        /// <paramref name="priority"/> is null.</exception>   
        /// <exception cref="KeyNotFoundException"><paramref name="priority"/> does not exist in <see cref="CurrentPriorityOrder"/>.</exception>
        void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties, IClassificationType priority);

        /// <summary>
        /// Sets the merged <see cref="TextFormattingRunProperties"/> of an <see cref="IClassificationType"/>.
        /// </summary>
        /// <param name="classificationType">The <see cref="IClassificationType"/>.</param>
        /// <param name="properties">The new properties.</param>
        /// <remarks>
        /// <para>
        /// Setting the text properties will cause the <see cref="ClassificationFormatMappingChanged"/> event to be sent.
        /// </para>
        /// <para>
        /// Only parts of the <paramref name="properties"/> that are different than the inherited values of <paramref name="classificationType"/>'s
        /// properties are stored. If you wish to override all properties of <paramref name="classificationType"/> explicity, please use
        /// <see cref="SetExplicitTextProperties"/>.
        /// </para>
        /// </remarks>
        void SetTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties);

        /// <summary>
        /// Sets the explicit <see cref="TextFormattingRunProperties"/> of an <see cref="IClassificationType"/>.
        /// </summary>
        /// <param name="classificationType">The <see cref="IClassificationType"/>.</param>
        /// <param name="properties">The new properties.</param>
        /// <remarks>
        /// <para>
        /// Setting the text properties will cause the <see cref="ClassificationFormatMappingChanged"/> event to be sent.
        /// </para>
        /// <para>
        /// Provided values in <paramref name="properties"/> will be set for the provided <paramref name="classificationType"/> and override
        /// any inhertied values. If you wish to keep the inheritance structure and only override the set of varying properties, please use
        /// <see cref="SetTextProperties"/>.
        /// </para>
        /// </remarks>
        void SetExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties);

        /// <summary>
        /// Gets a read-only list of the <see cref="IClassificationType"/> objects supported by this format map, sorted by priority.
        /// </summary>
        ReadOnlyCollection<IClassificationType> CurrentPriorityOrder { get; }

        /// <summary>
        /// Gets or sets the default properties that are applied to all classification types. The default properties contain the set
        /// of minimal properties required to render text properly.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default text properties have the lowest priority. Properties associated with a <see cref="IClassificationType"/>
        /// will only inherit properties of the <see cref="DefaultTextProperties"/> if they don't provide the core necessary 
        /// properties such as <see cref="TextFormattingRunProperties.Typeface"/>.
        /// </para>
        /// <para>
        /// The <see cref="DefaultTextProperties"/> are guaranteed to contain a <see cref="TextFormattingRunProperties.Typeface"/>, 
        /// <see cref="TextFormattingRunProperties.ForegroundBrush"/> and <see cref="TextFormattingRunProperties.FontRenderingEmSize"/>
        /// </para>
        /// </remarks>
        TextFormattingRunProperties DefaultTextProperties { get; set; }

        /// <summary>
        /// Switches the priorities of two <see cref="IClassificationType"/> objects.
        /// </summary>
        /// <param name="firstType">The first type.</param>
        /// <param name="secondType">The second type.</param>
        /// <remarks>
        /// Changing the priority of an <see cref="IClassificationType"/> causes the <see cref="ClassificationFormatMappingChanged "/> event to be raised.
        /// </remarks>
        void SwapPriorities(IClassificationType firstType, IClassificationType secondType);

        /// <summary>
        /// Begins a batch update on this <see cref="IClassificationFormatMap"/>. Events
        /// will not be raised until <see cref="EndBatchUpdate"/> is called.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="BeginBatchUpdate"/> was called for a second time without calling <see cref="EndBatchUpdate"/>.</exception>
        /// <remarks>You must call <see cref="EndBatchUpdate"/> in order to re-enable <see cref="ClassificationFormatMappingChanged"/> events.</remarks>
        void BeginBatchUpdate();

        /// <summary>
        /// Ends a batch update on this <see cref="IClassificationFormatMap"/> and raises an event if any changes were made during
        /// the batch update.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="EndBatchUpdate"/> was called without calling <see cref="BeginBatchUpdate"/>.</exception>
        /// <remarks>You must call<see cref="EndBatchUpdate"/> in order to re-enable <see cref="ClassificationFormatMappingChanged"/> events if <see cref="BeginBatchUpdate"/> was called.</remarks>
        void EndBatchUpdate();

        /// <summary>
        /// Determines whether this <see cref="IClassificationFormatMap"/> is in the middle of a batch update.
        /// </summary>
        bool IsInBatchUpdate { get; }

        /// <summary>
        /// Occurs when this <see cref="IClassificationFormatMap"/> changes.
        /// </summary>
        event EventHandler<EventArgs> ClassificationFormatMappingChanged;
    }
}
