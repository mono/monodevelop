// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines extensions to <see cref="IPeekResultDisplayInfo"/> to provide
    /// more information about an <see cref="IPeekResult"/>.
    /// </summary>
    public interface IPeekResultDisplayInfo2 : IPeekResultDisplayInfo
    {
        /// <summary>
        /// Defines the first character's index of the "interesting" token in the label.
        /// For instance, if Peek references was triggered on a method,
        /// this would be the first character's index in the label that contains the method
        /// name.
        /// </summary>
        /// <remarks>
        /// This index is bound to <see cref="IPeekResultDisplayInfo.Label"/>.
        /// It has nothing to do with the span of text that initialized peek.
        /// </remarks>
        int StartIndexOfTokenInLabel { get; set; }

        /// <summary>
        /// Defines the length of the "interesting" token in the label.
        /// For instance, if Peek references was triggered on a method,
        /// this would be the length of the method name.
        /// </summary>
        /// <remarks>
        /// This length is bound to <see cref="IPeekResultDisplayInfo.Label"/>.
        /// It has nothing to do with the span of text that initialized Peek session.
        /// </remarks>
        int LengthOfTokenInLabel { get; set; }
    }
}
