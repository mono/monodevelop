// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{

    /// <summary>
    /// Contains options used to enumerate ELS services.
    /// </summary>
    public class MappingEnumOptions
    {
        internal Nullable<Guid> _guid;
        internal Win32EnumOptions _win32EnumOption;

        /// <summary>
        /// Public constructor. Initializes an empty instance of <see cref="MappingEnumOptions">MappingEnumOptions</see>.
        /// </summary>
        public MappingEnumOptions()
        {
            _win32EnumOption._size = InteropTools.SizeOfWin32EnumOptions;
        }

        /// <summary>
        /// Optional. A service category, for example, "Transliteration". The application must set this member to null
        /// if the service category is not a search criterion.
        /// </summary>
        public string Category
        {
            get
            {
                return _win32EnumOption._category;
            }
            set
            {
                _win32EnumOption._category = value;
            }
        }

        /// <summary>
        /// Optional. An input language string, following the IETF naming convention, that identifies the input language
        /// that services should accept. The application can set this member to null if the supported input language is
        /// not a search criterion.
        /// </summary>
        public string InputLanguage
        {
            get
            {
                return _win32EnumOption._inputLanguage;
            }
            set
            {
                _win32EnumOption._inputLanguage = value;
            }
        }

        /// <summary>
        /// Optional. An output language string, following the IETF naming convention, that identifies the output language
        /// that services use to retrieve results. The application can set this member to null if the output language is
        /// not a search criterion.
        /// </summary>
        public string OutputLanguage
        {
            get
            {
                return _win32EnumOption._outputLanguage;
            }
            set
            {
                _win32EnumOption._outputLanguage = value;
            }
        }

        /// <summary>
        /// Optional. A standard Unicode script name that can be accepted by services. The application set this member to
        /// null if the input script is not a search criterion.
        /// </summary>
        public string InputScript
        {
            get
            {
                return _win32EnumOption._inputScript;
            }
            set
            {
                _win32EnumOption._inputScript = value;
            }
        }

        /// <summary>
        /// Optional. A standard Unicode script name used by services. The application can set this member to
        /// null if the output script is not a search criterion.
        /// </summary>
        public string OutputScript
        {
            get
            {
                return _win32EnumOption._outputScript;
            }
            set
            {
                _win32EnumOption._outputScript = value;
            }
        }

        /// <summary>
        /// Optional. A string, following the format of the MIME content types, that identifies the format that the
        /// services should be able to interpret when the application passes data. Examples of content types are
        /// "text/plain", "text/html", and "text/css". The application can set this member to null if the input content
        /// type is not a search criterion.
        ///
        /// <note>In Windows 7, the ELS services support only the content type "text/plain". A content type specification
        /// can be found at the IANA website: http://www.iana.org/assignments/media-types/text/ </note>
        /// </summary>
        public string InputContentType
        {
            get
            {
                return _win32EnumOption._inputContentType;
            }
            set
            {
                _win32EnumOption._inputContentType = value;
            }
        }

        /// <summary>
        /// Optional. A string, following the format of the MIME content types, that identifies the format in which the
        /// services retrieve data. The application can set this member to null if the output content type is not a search
        /// criterion.
        /// </summary>
        public string OutputContentType
        {
            get
            {
                return _win32EnumOption._outputContentType;
            }
            set
            {
                _win32EnumOption._outputContentType = value;
            }
        }

        /// <summary>
        /// Optional. A globally unique identifier (guid) structure for a specific service. The application must
        /// avoid setting this member at all if the guid is not a search criterion.
        /// </summary>
        public Nullable<Guid> Guid
        {
            get
            {
                return _guid;
            }
            set
            {
                _guid = value;
            }
        }
    }

    /// <summary>
    /// Contains options for text recognition. The values stored in this structure affect the behavior and results
    /// of MappingRecognizeText.
    /// </summary>
    public class MappingOptions
    {
        internal Win32Options _win32Options;

        /// <summary>
        /// Public constructor. Initializes an empty instance of MappingOptions.
        /// </summary>
        public MappingOptions()
        {
            _win32Options._size = InteropTools.SizeOfWin32Options;
        }

        /// <summary>
        /// Optional. An input language string, following the IETF naming convention, that identifies the input language
        /// that the service should be able to accept. The application can set this member to null to indicate that
        /// the service is free to interpret the input as any input language it supports.
        /// </summary>
        public string InputLanguage
        {
            get
            {
                return _win32Options._inputLanguage;
            }
            set
            {
                _win32Options._inputLanguage = value;
            }
        }

        /// <summary>
        /// Optional. An output language string, following the IETF naming convention, that identifies the output language
        /// that the service should be able to use to produce results. The application can set this member to null if
        /// the service should decide the output language.
        /// </summary>
        public string OutputLanguage
        {
            get
            {
                return _win32Options._outputLanguage;
            }
            set
            {
                _win32Options._outputLanguage = value;
            }
        }

        /// <summary>
        /// Optional. A standard Unicode script name that should be accepted by the service. The application can set this
        /// member to null to let the service decide how handle the input.
        /// </summary>
        public string InputScript
        {
            get
            {
                return _win32Options._inputScript;
            }
            set
            {
                _win32Options._inputScript = value;
            }
        }

        /// <summary>
        /// Optional. A standard Unicode script name that the service should use to retrieve results. The application can
        /// set this member to null to let the service decide the output script.
        /// </summary>
        public string OutputScript
        {
            get
            {
                return _win32Options._outputScript;
            }
            set
            {
                _win32Options._outputScript = value;
            }
        }

        /// <summary>
        /// Optional. A string, following the format of the MIME content types, that identifies the format that the service
        /// should be able to interpret when the application passes data. Examples of content types are "text/plain",
        /// "text/html", and "text/css". The application can set this member to null to indicate the "text/plain"
        /// content type.
        ///
        /// <note>In Windows 7, the ELS services support only the content type "text/plain". A content type specification
        /// can be found at the IANA website: http://www.iana.org/assignments/media-types/text/ </note>
        /// </summary>
        public string InputContentType
        {
            get
            {
                return _win32Options._inputContentType;
            }
            set
            {
                _win32Options._inputContentType = value;
            }
        }

        /// <summary>
        /// Optional. A string, following the format of the MIME content types, that identifies the format in which the
        /// service should retrieve data. The application can set this member to NULL to let the service decide the output
        /// content type.
        /// </summary>
        public string OutputContentType
        {
            get
            {
                return _win32Options._outputContentType;
            }
            set
            {
                _win32Options._outputContentType = value;
            }
        }

        /// <summary>
        /// Optional. Private flag that a service provider defines to affect service behavior. Services can interpret this
        /// flag as they require.
        ///
        /// <note>For Windows 7, none of the available ELS services support flags.</note>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flag")]
        public int ServiceFlag
        {
            get
            {
                return (int)_win32Options._serviceFlag;
            }
            set
            {
                _win32Options._serviceFlag = (uint)value;
            }
        }

    }

}
