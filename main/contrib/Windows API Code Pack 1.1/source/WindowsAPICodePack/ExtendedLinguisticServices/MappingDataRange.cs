// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{

    /// <summary>
    /// Contains text recognition results for a recognized text subrange. An array of structures of this type
    /// is retrieved by an ELS service in a <see cref="MappingPropertyBag">MappingPropertyBag</see> structure.
    /// </summary>
    public class MappingDataRange
    {
        internal Win32DataRange _win32DataRange;

        internal MappingDataRange()
        {
        }

        /// <summary>
        /// Index of the beginning of the subrange in the text, where 0 indicates the first character of the string
        /// passed to MappingService.RecognizeText or
        /// MappingService.BeginRecognizeText, instead of an offset to the
        /// index passed to the function in the index parameter. The value should be less than the entire length
        /// of the text.
        /// </summary>
        public int StartIndex
        {
            get
            {
                return (int)_win32DataRange._startIndex;
            }
        }

        /// <summary>
        /// Index of the end of the subrange in the text, where 0 indicates the first character at of the string
        /// passed to MappingService.RecognizeText or
        /// MappingService.BeginRecognizeText, instead of an offset to the
        /// index passed to the function in the index parameter. The value should be less than the entire length
        /// of the text.
        /// </summary>
        public int EndIndex
        {
            get
            {
                return (int)_win32DataRange._endIndex;
            }
        }

        /// <summary>
        /// The data retrieved as service output associated with the subrange. This data must be of the format indicated
        /// by the content type supplied in the <see cref="ContentType">ContentType</see> property.
        /// </summary>
        public byte[] GetData()
        {
            byte[] data = new byte[(int)_win32DataRange._dataSize];
            if (_win32DataRange._dataSize == 0)
            {
                return data;
            }
            if (_win32DataRange._data == IntPtr.Zero)
            {
                throw new LinguisticException(LinguisticException.InvalidData);
            }
            Marshal.Copy(_win32DataRange._data, data, 0, (int)_win32DataRange._dataSize);
            return data;
        }

        /// <summary>
        /// A string specifying the MIME content type of the data returned by <see cref="GetData()">GetData()</see>. Examples of
        /// content types are "text/plain", "text/html", and "text/css".
        ///
        /// <note>In Windows 7, the ELS services support only the content type "text/plain". A content type specification
        /// can be found at the IANA website: http://www.iana.org/assignments/media-types/text/ </note>
        /// </summary>
        public string ContentType
        {
            get
            {
                return _win32DataRange._contentType;
            }
        }

        /// <summary>
        /// Available action IDs for this data range. Usable for calling <see cref="MappingService.DoAction">MappingService.DoAction</see> or
        /// <see cref="MappingService.BeginDoAction">MappingService.BeginDoAction</see>.
        ///
        /// <note>In Windows 7, the ELS services do not expose any actions.</note>
        /// </summary>
        public IEnumerable<string> ActionIds
        {
            get
            {
                string[] actionIDs = InteropTools.UnpackStringArray(
                    _win32DataRange._actionIDs, _win32DataRange._actionsCount);
                return actionIDs;
            }
        }

        /// <summary>
        /// Available action display names for this data range. These strings can be localized.
        ///
        /// <note>In Windows 7, the ELS services do not expose any actions.</note>
        /// </summary>
        public IEnumerable<string> ActionDisplayNames
        {
            get
            {
                string[] actionDisplayNames = InteropTools.UnpackStringArray(
                    _win32DataRange._actionDisplayNames, _win32DataRange._actionsCount);
                return actionDisplayNames;
            }
        }

        /// <summary>
        /// Formats the low-level data contained in this <see cref="MappingDataRange">MappingDataRange</see> using an implementation of the
        /// <see cref="IMappingFormatter{T}">IMappingFormatter</see> interface.
        /// </summary>
        /// <typeparam name="T">The type with which <see cref="IMappingFormatter{T}">IMappingFormatter</see> is parameterized.</typeparam>
        /// <param name="formatter">The formatter to be used in the formatting.</param>
        /// <returns>A formatted version of this <see cref="MappingDataRange">MappingDataRange</see>.</returns>
        public T FormatData<T>(IMappingFormatter<T> formatter)
        {
            if (formatter == null) { throw new ArgumentNullException("formatter"); }
            return formatter.Format(this);
        }
    }

}
