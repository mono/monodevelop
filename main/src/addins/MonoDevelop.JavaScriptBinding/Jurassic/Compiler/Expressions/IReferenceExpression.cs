using System;

namespace Jurassic.Compiler
{
    
    /// <summary>
    /// Represents a reference - an expression that is valid on the left-hand-side of an assignment
    /// operation.
    /// </summary>
    public interface IReferenceExpression
    {
        /// <summary>
        /// Gets the static type of the reference.
        /// </summary>
        PrimitiveType Type { get; }
    }
}