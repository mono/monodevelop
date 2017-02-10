// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Data.Utilities
{

    using System;
    using System.Windows;
    using System.IO;
    using System.Text;

    /// <summary>
    /// This class helps with the general tasks related to data objects that are used for copy/paste and drag/drop operations.
    /// </summary>
    static class DataObjectManager
    {
        /// <summary>
        /// Extracts the text from a given IDataObject
        /// </summary>
        public static string ExtractText(IDataObject data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (data.GetDataPresent(DataFormats.UnicodeText))
            {
                return (string)data.GetData(DataFormats.UnicodeText);
            }
            else if (data.GetDataPresent(DataFormats.Text))
            {
                return (string)data.GetData(DataFormats.Text);
            }
            else if (data.GetDataPresent(DataFormats.Html))
            {
                return ExtractHTMLText(data).Trim();
            }
            else if (data.GetDataPresent(DataFormats.CommaSeparatedValue))
            {
                return (string)data.GetData(DataFormats.Text, true);
            }
            else
            {
                //unsupported format
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns true if the data contained within the <see cref="IDataObject"/> can be converted to text.
        /// </summary>
        public static bool ContainsText(IDataObject data)
        {
            return data.GetDataPresent(DataFormats.Text, true) || data.GetDataPresent(DataFormats.Html) || data.GetDataPresent(DataFormats.CommaSeparatedValue);
        }

        #region Private Helpers

        /// <summary>
        /// Extract HTML data from the supplied IDataObject based on the standard HTML clipboard format
        /// </summary>
        internal static string ExtractHTMLText(IDataObject data)
        {
            string html = string.Empty;

            //check to see if the data was stored as a string, if so then just use the string. This seems to be done in some places
            //instead of correctly storing the data as a stream of bytes
            string stringTest = data.GetData(DataFormats.Html) as string;
            if (stringTest != null)
            {
                html = stringTest;
            }
            else
            {
                //decode the data into a string based on UTF8 enconding. the HTML format has a header and a body part
                //the header is encoded as ASCII and the body is encoded as UTF8; UTF8 shares the same
                //starting indecies of ASCII, everything can be decoded as UTF8
                MemoryStream dataMemoryStream = null;
                try
                {
                    dataMemoryStream = (MemoryStream)data.GetData(DataFormats.Html);
                }
                catch
                {
                    throw new InvalidOperationException("Can't examine data in IDataObject object, MemoryStream expected but not found");
                }
                Decoder decoder = Encoding.UTF8.GetDecoder();
                byte[] bufferData = dataMemoryStream.ToArray();
                int charCount = decoder.GetCharCount(bufferData, 0, bufferData.Length);
                char[] convertedData = new char[charCount];
                int charsUsed, bytesUsed;
                bool completed;
                decoder.Convert(bufferData, 0, bufferData.Length, convertedData, 0, charCount, true, out bytesUsed, out charsUsed, out completed);
                html = new string(convertedData);
            }

            //now extract the relevant piece of data
            const string startFragment = "<!--StartFragment-->";
            const string endFragment = "<!--EndFragment-->";
            int startIndex = html.IndexOf(startFragment, StringComparison.CurrentCultureIgnoreCase);
            int endIndex = html.IndexOf(endFragment, StringComparison.CurrentCultureIgnoreCase);
            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
            {
                return html.Substring(startIndex + startFragment.Length, endIndex - startIndex - startFragment.Length);
            }
            else
            {
                //there's invalid HTML format data in the IDataObject
                return string.Empty;
            }
        }

        #endregion //Private Helpers
    }
}
