// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using Microsoft.VisualStudio.Utilities;
    using System.ComponentModel.Composition;

    /// <summary>
    /// Represents metadata for an <see cref="ISpaceReservationManager"/>.  
    /// </summary>
    /// <remarks> 
    /// Because you cannot subclass this type, you can simply use the [Export] attribute.
    /// </remarks>
    /// <example>
    /// internal sealed class Components
    /// {
    ///    [Export]
    ///    [Name("SampleSpaceReservationManager")]
    ///    [Order(After = "Selection", Before = "Text")]
    ///    internal SpaceReservationManagerDefinition sampleManagerDefinition;
    ///    
    ///     { other components }
    /// }
    /// </example>
    public sealed class SpaceReservationManagerDefinition
    {
    }
}
