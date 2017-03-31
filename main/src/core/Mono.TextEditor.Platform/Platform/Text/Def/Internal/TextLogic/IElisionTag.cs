//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// Tag indicating spans of text to be excluded from a view.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IViewTaggerProviders are querried by the editor implementation with this tag type for views having
    /// the <see cref="F:Microsoft.VisualStudio.Text.Editor.PredefinedTextViewRoles.Structured"/> view role.
    /// </para>
    /// <para>
    /// These tags cause text to be hidden but do not result in any outlining UI.
    /// IOutliningRegionTags are used to provide data to the outlining manager.
    /// </para>
    /// </remarks>
    public interface IElisionTag : ITag
    {
    }
}
