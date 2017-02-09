// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an <see cref="IPeekResult"/> that is not based on a location in a document, but can
    /// be browsed externally, for example a metadata class that can only be browsed in Object Browser.
    /// </summary>
    public interface IExternallyBrowsablePeekResult : IPeekResult
    {
    }
}
