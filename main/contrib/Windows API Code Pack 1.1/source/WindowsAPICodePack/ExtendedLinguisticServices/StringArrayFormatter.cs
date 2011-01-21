// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Text;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{

    /// <summary>
    /// Converts registry-formatted data into string arrays.
    /// </summary>
    public class StringArrayFormatter : IMappingFormatter<string[]>
    {
        private static readonly char[] Separator = new char[] { '\0' };

        /// <summary>
        /// Converts a single <see cref="MappingDataRange">MappingDataRange</see> into a string array by splitting on each null character and
        /// removing the empty entries.
        /// </summary>
        /// <param name="dataRange">The <see cref="MappingDataRange">MappingDataRange</see> to convert</param>
        /// <returns>The resulting string array</returns>
        public string[] Format(MappingDataRange dataRange)
        {
            if (dataRange == null) { throw new ArgumentNullException("dataRange"); }

            byte[] data = dataRange.GetData();
            string resultText = Encoding.Unicode.GetString(data);
            return resultText.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Uses <see cref="Format(MappingDataRange)">Format</see> to format all the ranges of the supplied
        /// <see cref="MappingPropertyBag">MappingPropertyBag</see>.
        /// </summary>
        /// <param name="bag">The property bag to convert.</param>
        /// <returns>An array of string arrays, one per <see cref="MappingDataRange">MappingDataRange</see>.</returns>
        public string[][] FormatAll(MappingPropertyBag bag)
        {
            if (bag == null) { throw new ArgumentNullException("bag"); }

            MappingDataRange[] dataRanges = bag.GetResultRanges();
            string[][] results = new string[dataRanges.Length][];
            for (int i = 0; i < results.Length; ++i)
            {
                results[i] = Format(dataRanges[i]);
            }
            return results;
        }
    }

}
