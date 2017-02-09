// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{

    /// <summary>
    /// Provides information for an IAdornmentLayer export.  
    /// </summary>
    /// <remarks> 
    /// Because you cannot subclass this type, you can use the [Export] attribute with no type.
    /// </remarks>
    /// <example>
    /// internal sealed class Components
    /// {
    ///    [Export]
    ///    [Name("ExampleAdornmentLayer")]
    ///    [Order(After = "Selection", Before = "Text")]
    ///    internal AdornmentLayerDefinition viewLayerDefinition;
    ///    
    ///    { other components }
    /// }
    /// </example>
    public sealed class AdornmentLayerDefinition
    {
    }
}
