// Copyright (c) Microsoft Corporation
// All rights reserved

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// A suggested action that contains nested set of suggested actions 
    /// representing flavors of their parent action.
    /// </summary>
    [CLSCompliant(false)]
    public interface ISuggestedActionWithFlavors : ISuggestedAction
    {
    }
}