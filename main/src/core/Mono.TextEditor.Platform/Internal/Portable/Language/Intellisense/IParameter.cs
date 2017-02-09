////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an individual parameter description inside the description of a signature.
    /// </summary>
    public interface IParameter
    {
        /// <summary>
        /// Gets the signature of which this parameter is a part.
        /// </summary>
        ISignature Signature { get; }

        /// <summary>
        /// Gets the name of this parameter. 
        /// </summary>
        /// <remarks>
        /// This is displayed to identify the parameter.
        /// </remarks>
        string Name { get; }

        /// <summary>
        /// Gets the documentation associated with the parameter.  
        /// </summary>
        /// <remarks>
        /// This is displayed to describe
        /// the parameter.
        /// </remarks>
        string Documentation { get; }

        /// <summary>
        /// Gets the text location of this parameter relative to the signature's content.
        /// </summary>
        Span Locus { get; }

        /// <summary>
        /// Gets the text location of this parameter relative to the signature's pretty-printed content.
        /// </summary>
        Span PrettyPrintedLocus { get; }
    }
}
