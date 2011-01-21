// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{

    /// <summary>
    /// This class contains constants describing the existing ELS services for Windows 7.
    /// </summary>
    public static class MappingAvailableServices
    {
        /// <summary>
        /// The guid of the Microsoft Language Detection service.
        /// </summary>
        public static readonly Guid LanguageDetection =
            new Guid("{CF7E00B1-909B-4d95-A8F4-611F7C377702}");

        /// <summary>
        /// The guid of the Microsoft Script Detection service.
        /// </summary>
        public static readonly Guid ScriptDetection =
            new Guid("{2D64B439-6CAF-4f6b-B688-E5D0F4FAA7D7}");

        /// <summary>
        /// The guid of the Microsoft Traditional Chinese to Simplified Chinese Transliteration service.
        /// </summary>        
        public static readonly Guid TransliterationHantToHans =
            new Guid("{A3A8333B-F4FC-42f6-A0C4-0462FE7317CB}");

        /// <summary>
        /// The guid of the Microsoft Simplified Chinese to Traditional Chinese Transliteration service.
        /// </summary>
        public static readonly Guid TransliterationHansToHant =
            new Guid("{3CACCDC8-5590-42dc-9A7B-B5A6B5B3B63B}");

        /// <summary>
        /// The guid of the Microsoft Malayalam to Latin Transliteration service.
        /// </summary>
        public static readonly Guid TransliterationMalayalamToLatin =
            new Guid("{D8B983B1-F8BF-4a2b-BCD5-5B5EA20613E1}");

        /// <summary>
        /// The guid of the Microsoft Devanagari to Latin Transliteration service.
        /// </summary>        
        public static readonly Guid TransliterationDevanagariToLatin =
            new Guid("{C4A4DCFE-2661-4d02-9835-F48187109803}");

        /// <summary>
        /// The guid of the Microsoft Cyrillic to Latin Transliteration service.
        /// </summary>
        public static readonly Guid TransliterationCyrillicToLatin =
            new Guid("{3DD12A98-5AFD-4903-A13F-E17E6C0BFE01}");

        /// <summary>
        /// The guid of the Microsoft Bengali to Latin Transliteration service.
        /// </summary>
        public static readonly Guid TransliterationBengaliToLatin =
            new Guid("{F4DFD825-91A4-489f-855E-9AD9BEE55727}");
    }

}
