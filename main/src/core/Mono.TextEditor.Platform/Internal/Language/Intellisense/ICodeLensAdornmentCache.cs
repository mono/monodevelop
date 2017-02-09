//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Service which caches CodeLensAdornment instances.  Cached adornments can either
    /// be connected or disconnected (with cached UI data).
    /// </summary>
    [CLSCompliant(false)]
    public interface ICodeLensAdornmentCache
    {
        /// <summary>
        /// Tries to get a cached adornment, if one exists.
        /// </summary>
        /// <param name="tag">The tag to get the cached adornment for.</param>
        /// <param name="adornment">The adornment, if one exists.</param>
        /// <returns>True if the cached adornment existed, otherwise false.</returns>
        bool TryGetAdornment(ICodeLensTag tag, out CodeLensAdornment adornment);

        /// <summary>
        /// Gets or creates and caches an adornment.
        /// </summary>
        /// <param name="tag">The tag to get or create the cached adornment for.</param>
        /// <returns>The existing or new adornment.</returns>
        Task<CodeLensAdornment> GetOrCreateAdornmentAsync(ICodeLensTag tag);

        /// <summary>
        /// Either updates the cache position or caches an existing CodeLensAdornment.
        /// </summary>
        /// <param name="tag">The tag to update or cache the adornment for.</param>
        /// <param name="adornment">The existing adornment associated with the tag.</param>
        void UpdateOrCacheAdornment(ICodeLensTag tag, CodeLensAdornment adornment);

        /// <summary>
        /// Requests for the lifetime of an adornment to be extended.  While the lifetime is extended,
        /// the adornment should not become disconnected from its underlying data even if the adornment
        /// is otherwise removed from the UI.
        /// </summary>
        /// <param name="tag">The tag whose adornment should have its lifetime extended.</param>
        /// <param name="extensionEndedHandler">A callback which is invoked when the lifetime
        /// extension can no longer be maintained (for example, when the cache is full, or
        /// if the underlying tag is disconnected).</param>
        Task ExtendLifetimeAsync(ICodeLensTag tag, Action extensionEndedHandler);

        /// <summary>
        /// Determines if the lifetime of an adornment is currently extended.
        /// </summary>
        /// <param name="tag">The tag to check the lifetime extension state of.</param>
        /// <returns>True if the adornment's lifetime is being extended by request, otherwise false.</returns>
        bool IsLifetimeExtended(ICodeLensTag tag);
    }
}
