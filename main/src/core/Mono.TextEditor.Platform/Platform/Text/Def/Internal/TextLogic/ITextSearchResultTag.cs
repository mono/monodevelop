//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Represents search results that are provided by a search tagger.
    /// </summary>
    /// <remarks>
    /// The <see cref="ITextSearchResultTag"/> is present such that all consumers of search matches have a common way of obtaining the matches.
    /// </remarks>
    public interface ITextSearchResultTag : ITag
    {
    }
}
