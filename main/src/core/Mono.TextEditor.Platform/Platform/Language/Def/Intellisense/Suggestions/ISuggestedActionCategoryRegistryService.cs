// Copyright (c) Microsoft Corporation
// All rights reserved

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// The service that maintains the collection of suggested action categories.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ISuggestedActionCategoryRegistryService))]
    /// </remarks>
    public interface ISuggestedActionCategoryRegistryService
    {
        /// <summary>
        /// Gets the <see cref="ISuggestedActionCategory"></see> object with the specified <paramref name="categoryName"/>.
        /// </summary>
        /// <param name="categoryName">The name of the category. Name comparisons are case-insensitive.</param>
        /// <returns>The category, or null if no category is found.</returns>
        ISuggestedActionCategory GetCategory(string categoryName);

        /// <summary>
        /// Gets an enumeration of all categories, including the "unknown" category.
        /// </summary>
        IEnumerable<ISuggestedActionCategory> Categories { get; }

        /// <summary>
        /// Creates a new <see cref="ISuggestedActionCategorySet"/> containing given categories.
        /// </summary>
        /// <param name="categories">A list of categories to be included into the set.</param>
        /// <returns>An instance of <see cref="ISuggestedActionCategorySet"/> containing given categories.</returns>
        ISuggestedActionCategorySet CreateSuggestedActionCategorySet(IEnumerable<string> categories);

        /// <summary>
        /// Creates a new <see cref="ISuggestedActionCategorySet"/> containing given categories.
        /// </summary>
        /// <param name="categories">A list of categories to be included into the set.</param>
        /// <returns>An instance of <see cref="ISuggestedActionCategorySet"/> containing given categories.</returns>
        ISuggestedActionCategorySet CreateSuggestedActionCategorySet(params string[] categories);

        /// <summary>
        /// A predefined <see cref="ISuggestedActionCategorySet"/> containing any category.
        /// </summary>
        ISuggestedActionCategorySet Any { get; }

        /// <summary>
        /// A predefined <see cref="ISuggestedActionCategorySet"/> containing code fixes category.
        /// </summary>
        ISuggestedActionCategorySet AllCodeFixes { get; }

        /// <summary>
        /// A predefined <see cref="ISuggestedActionCategorySet"/> containing all refactorings category.
        /// </summary>
        ISuggestedActionCategorySet AllRefactorings { get; }
        
        /// <summary>
        /// A predefined <see cref="ISuggestedActionCategorySet"/> containing all code fixes and refactorings category.
        /// </summary>
        ISuggestedActionCategorySet AllCodeFixesAndRefactorings { get; }
    }
}
