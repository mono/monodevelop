//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Subset of ITextBufferFactoryService. Used to avoid having to mock the asset system in unit tests.
    /// </summary>
    internal interface IInternalTextBufferFactory
    {
        ITextBuffer CreateTextBuffer(string text, IContentType contentType);

        ITextBuffer CreateTextBuffer(string text, IContentType contentType, bool spurnGroup);

        IContentType TextContentType { get; }
        IContentType InertContentType { get; }
        IContentType ProjectionContentType { get; }
    }
}