// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a set of predefined suggested action category names.
    /// </summary>
    public static class PredefinedSuggestedActionCategoryNames
    {
        /// <summary>
        /// A root category that include any suggested action.
        /// </summary>
        public const string Any = "Any";

        /// <summary>
        /// A category of suggested actions aimed to fix code issues.
        /// </summary>
        public const string CodeFix = "CodeFix";

        /// <summary>
        /// A category of suggested actions aimed to fix errors.
        /// </summary>
        public const string ErrorFix = "ErrorFix";

        /// <summary>
        /// A category of suggested actions aimed to fix style violations.
        /// </summary>
        public const string StyleFix = "StyleFix";

        /// <summary>
        /// A category of suggested actions aimed to suggest a code refactoring.
        /// </summary>
        public const string Refactoring = "Refactoring";
    }
}
