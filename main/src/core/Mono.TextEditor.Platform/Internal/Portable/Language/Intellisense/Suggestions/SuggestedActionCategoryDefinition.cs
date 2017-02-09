// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a suggested action category.
    /// </summary>
    /// <remarks> 
    /// Because you cannot subclass this type, you should use the [Export] attribute with no type.
    /// </remarks>
    /// <example>
    /// internal sealed class Components
    /// {
    ///     [Export]
    ///     [Name(PredefinedSuggestedActionCategoryNames.ErrorFix)]             // required
    ///     [BaseDefinition(PredefinedSuggestedActionCategoryNames.CodeFix)]    // zero or more BaseDefinitions are allowed
    ///     private SuggestedActionCategoryDefinition errorFixSuggestedActionCategoryDefinition;
    ///    
    ///    { other components }
    /// }
    /// </example>
    public sealed class SuggestedActionCategoryDefinition
    {
    }
}
