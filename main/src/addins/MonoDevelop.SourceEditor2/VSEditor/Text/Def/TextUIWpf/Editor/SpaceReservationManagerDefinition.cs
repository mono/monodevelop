//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
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
