// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Classification
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Formatting;
    using System.Windows;

    /// <summary>
    /// Maps from arbitrary keys to a <see cref="ResourceDictionary"/>.
    /// </summary>
    public interface IEditorFormatMap
    {
        /// <summary>
        /// Gets a <see cref="ResourceDictionary"/> for the specified key.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="ResourceDictionary"/> object that represents the set of property
        /// contributions from the provided <see cref="EditorFormatDefinition"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is empty or null.</exception>
        ResourceDictionary GetProperties(string key);

        /// <summary>
        /// Adds a <see cref="ResourceDictionary"/> for a new key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="properties">The new properties.</param>
        /// <remarks>
        /// <para>
        /// Adding properties will cause the FormatMappingChanged event to be raised.
        /// </para>
        /// <para>If <paramref name="key"/> already exists in the map, then this is equivalent to <see cref="SetProperties"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null or empty.</exception>
        void AddProperties(string key, ResourceDictionary properties);

        /// <summary>
        /// Sets the <see cref="ResourceDictionary"/> of a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="properties">The new <see cref="ResourceDictionary"/> of properties.</param>
        /// <remarks>
        /// <para>
        /// Setting properties will cause the FormatMappingChanged event to be raised.
        /// </para>
        /// <para>
        /// If the <see cref="ResourceDictionary"/> set does not contain the expected properties, the consumer
        /// of the properties may throw an exception.
        /// </para>
        /// </remarks>
        void SetProperties(string key, ResourceDictionary properties);

        /// <summary>
        /// Begins a batch update on this <see cref="IEditorFormatMap"/>. Events
        /// will not be raised until <see cref="EndBatchUpdate"/> is called.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="BeginBatchUpdate"/> was called for a second time 
        /// without calling <see cref="EndBatchUpdate"/>.</exception>
        /// <remarks>You must call <see cref="EndBatchUpdate"/> in order to re-enable FormatMappingChanged events.</remarks>
        void BeginBatchUpdate();

        /// <summary>
        /// Ends a batch update on this <see cref="IEditorFormatMap"/> and raises an event if any changes were made during
        /// the batch update.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="EndBatchUpdate"/> was called without calling <see cref="BeginBatchUpdate"/> first.</exception>
        /// <remarks>You must call <see cref="EndBatchUpdate"/> in order to re-enable FormatMappingChanged events if <see cref="BeginBatchUpdate"/> was called.</remarks>
        void EndBatchUpdate();

        /// <summary>
        /// Determines whether this <see cref="IEditorFormatMap"/> is in the middle of a batch update.
        /// </summary>
        bool IsInBatchUpdate { get; }

        /// <summary>
        /// Occurs when this <see cref="IEditorFormatMap"/> changes.
        /// </summary>
        event EventHandler<FormatItemsEventArgs> FormatMappingChanged;
    }
}
