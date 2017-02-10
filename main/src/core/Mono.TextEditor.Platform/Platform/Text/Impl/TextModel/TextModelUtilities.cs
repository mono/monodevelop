// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using Microsoft.VisualStudio.Text.Utilities;

    internal static class TextModelUtilities
    {
       /// <summary>
        /// Compute the impact of a change that substitutes <paramref name="newText"/> for <paramref name="oldText"/> in the
        /// context described by the <paramref name="boundaryConditions"/>.
        /// </summary>
        /// <param name="boundaryConditions">Immediate surroundings of the change with respect to compound line breaks.</param>
        /// <param name="oldText">The replaced text.</param>
        /// <param name="newText">The newly inserted text.</param>
        /// <returns></returns>
        static public int ComputeLineCountDelta(LineBreakBoundaryConditions boundaryConditions, ChangeString oldText, ChangeString newText)
        {
            int delta = 0;
            delta -= oldText.ComputeLineBreakCount();
            delta += newText.ComputeLineBreakCount();
            if ((boundaryConditions & LineBreakBoundaryConditions.PrecedingReturn) != 0)
            {
                if (oldText.Length > 0 && oldText[0] == '\n')
                {
                    delta++;
                }
                if (newText.Length > 0 && newText[0] == '\n')
                {
                    delta--;
                }
            }

            if ((boundaryConditions & LineBreakBoundaryConditions.SucceedingNewline) != 0)
            {
                if (oldText.Length > 0 && oldText[oldText.Length - 1] == '\r')
                {
                    delta++;
                }
                if (newText.Length > 0 && newText[newText.Length - 1] == '\r')
                {
                    delta--;
                }
            }

            if ((oldText.Length == 0) &&
                ((boundaryConditions & LineBreakBoundaryConditions.PrecedingReturn) != 0) &&
                ((boundaryConditions & LineBreakBoundaryConditions.SucceedingNewline) != 0))
            {
                // return and newline were adjacent before and were separated by the insertion
                delta++;
            }

            if ((newText.Length == 0) &&
                ((boundaryConditions & LineBreakBoundaryConditions.PrecedingReturn) != 0) &&
                ((boundaryConditions & LineBreakBoundaryConditions.SucceedingNewline) != 0))
            {
                // return and newline were separated before and were made adjacent by the deletion
                delta--;
            }
            return delta;
        }
    }
}