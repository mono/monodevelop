//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Internal options for text storage tuning.
    /// </summary>
    public static class TextModelEditorOptions
    {
        /// <summary>
        /// The default option that determines the file size above which compressed storage will be used.
        /// </summary>
        public static readonly EditorOptionKey<int> CompressedStorageFileSizeThresholdOptionId = new EditorOptionKey<int>(CompressedStorageFileSizeThresholdOptionName);
        public const string CompressedStorageFileSizeThresholdOptionName = "CompressedStorageFileSizeThreshold";

        /// <summary>
        /// The default option that determines the page size when compressed storage is in use.
        /// </summary>
        public static readonly EditorOptionKey<int> CompressedStoragePageSizeOptionId = new EditorOptionKey<int>(CompressedStoragePageSizeOptionName);
        public const string CompressedStoragePageSizeOptionName = "CompressedStoragePageSize";

        /// <summary>
        /// The default option that specifies how many pages will be retained in memory when compressed storage is in use.
        /// </summary>
        public static readonly EditorOptionKey<int> CompressedStorageMaxLoadedPagesOptionId = new EditorOptionKey<int>(CompressedStorageMaxLoadedPagesOptionName);
        public const string CompressedStorageMaxLoadedPagesOptionName = "CompressedStorageMaxLoadedPages";

        /// <summary>
        /// The default option that determines whether weak references to discarded pages will be retained.
        /// </summary>
        public static readonly EditorOptionKey<bool> CompressedStorageRetainWeakReferencesOptionId = new EditorOptionKey<bool>(CompressedStorageRetainWeakReferencesOptionName);
        public const string CompressedStorageRetainWeakReferencesOptionName = "CompressedStorageRetainWeakReferences";

        /// <summary>
        /// The default option that determines the size above which differencing of text changes will not be attempted even when requested.
        /// </summary>
        public static readonly EditorOptionKey<int> DiffSizeThresholdOptionId = new EditorOptionKey<int>(DiffSizeThresholdOptionName);
        public const string DiffSizeThresholdOptionName = "DiffSizeThreshold";
    }

    /// <summary>
    /// The option definition that determines the file size above which compressed storage will be used.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(TextModelEditorOptions.CompressedStorageFileSizeThresholdOptionName)]
    public sealed class CompressedStorageFileSizeThreshold : EditorOptionDefinition<int>
    {
        /// <summary>
        /// Gets the default value (5 megabytes)
        /// </summary>
        public override int Default { get { return 1024 * 1024 * 5; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return TextModelEditorOptions.CompressedStorageFileSizeThresholdOptionId; } }
    }

    /// <summary>
    /// The option definition that determines the page size when compressed storage is in use.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(TextModelEditorOptions.CompressedStoragePageSizeOptionName)]
    public sealed class CompressedStoragePageSize : EditorOptionDefinition<int>
    {
        /// <summary>
        /// Gets the default value (1 megabyte)
        /// </summary>
        public override int Default { get { return 1024 * 1024; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return TextModelEditorOptions.CompressedStoragePageSizeOptionId; } }
    }

    /// <summary>
    /// The default option that specifies how many pages will be retained in memory when compressed storage is in use.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(TextModelEditorOptions.CompressedStorageMaxLoadedPagesOptionName)]
    public sealed class CompressedStorageMaxLoadedPages : EditorOptionDefinition<int>
    {
        /// <summary>
        /// Gets the default value (3)
        /// </summary>
        public override int Default { get { return 3; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return TextModelEditorOptions.CompressedStorageMaxLoadedPagesOptionId; } }
    }

    /// <summary>
    /// The option definition that determines whether weak references to discarded pages will be retained.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(TextModelEditorOptions.CompressedStorageRetainWeakReferencesOptionName)]
    public sealed class CompressedStorageRetainWeakReferences : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value (true)
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return TextModelEditorOptions.CompressedStorageRetainWeakReferencesOptionId; } }
    }

    /// <summary>
    /// The default option that determines the size above which differencing of text changes will not be attempted even when requested.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(TextModelEditorOptions.DiffSizeThresholdOptionName)]
    public sealed class DiffSizeThreshold : EditorOptionDefinition<int>
    {
        /// <summary>
        /// Gets the default value (10 MB)
        /// </summary>
        public override int Default { get { return 25 * 1024 * 1024; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return TextModelEditorOptions.DiffSizeThresholdOptionId; } }
    }
}