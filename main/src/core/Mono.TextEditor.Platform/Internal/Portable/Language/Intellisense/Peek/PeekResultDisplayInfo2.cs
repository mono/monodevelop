// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public class PeekResultDisplayInfo2 : PeekResultDisplayInfo, IPeekResultDisplayInfo2
    {
        /// <summary>
        /// Defines the first character's index of the "interesting" token in the label.
        /// For instance, if peek references was triggered on a method,
        /// this would be the first character's index in the label that contains the method
        /// name.
        /// </summary>
        /// <remarks>
        /// This index is bound to <see cref="IPeekResultDisplayInfo.Label"/>.
        /// It has nothing to do with the span of text that initialized peek.
        /// </remarks>
        public int StartIndexOfTokenInLabel { get; set; }

        /// <summary>
        /// Defines the length of the "interesting" token in the label.
        /// For instance, if peek references was triggered on a method,
        /// this would be the length of the method name.
        /// </summary>
        /// <remarks>
        /// This length is bound to <see cref="IPeekResultDisplayInfo.Label"/>.
        /// It has nothing to do with the span of text that initialized peek.
        /// </remarks>
        public int LengthOfTokenInLabel { get; set; }

        /// <summary>
        /// Creates new instance of the <see cref="PeekResultDisplayInfo2"/> class.
        /// </summary>
        public PeekResultDisplayInfo2(string label, object labelTooltip, string title, string titleTooltip, int startIndexOfTokenInLabel, int lengthOfTokenInLabel) :
            base(label, labelTooltip, title, titleTooltip)
        {
            if (startIndexOfTokenInLabel < 0 || startIndexOfTokenInLabel >= label.Length)
            {
                throw new ArgumentOutOfRangeException("startIndexOfTokenInLabel");
            }
            if (lengthOfTokenInLabel < 0 || startIndexOfTokenInLabel + lengthOfTokenInLabel > label.Length)
            {
                throw new ArgumentOutOfRangeException("lengthOfTokenInLabel");
            }

            StartIndexOfTokenInLabel = startIndexOfTokenInLabel;
            LengthOfTokenInLabel = lengthOfTokenInLabel;
        }
    }
}
