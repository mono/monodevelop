// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// Contains a list of well known sensor categories.
    /// </summary>
    public static class SensorCategories
    {
        /// <summary>
        /// The sensor categoryId for all categories.
        /// </summary>
        public static readonly Guid All = new Guid(0xC317C286, 0xC468, 0x4288, 0x99, 0x75, 0xD4, 0xC4, 0x58, 0x7C, 0x44, 0x2C);
        
        /// <summary>
        /// The sensor location categoryId property key.
        /// </summary>
        public static readonly Guid Location = new Guid(0xBFA794E4, 0xF964, 0x4FDB, 0x90, 0xF6, 0x51, 0x5, 0x6B, 0xFE, 0x4B, 0x44);
       
        /// <summary>
        /// The environmental sensor cagetory property key.
        /// </summary>
        public static readonly Guid Environmental = new Guid(0x323439AA, 0x7F66, 0x492B, 0xBA, 0xC, 0x73, 0xE9, 0xAA, 0xA, 0x65, 0xD5);
       
        /// <summary>
        /// The motion sensor cagetory property key.
        /// </summary>
        public static readonly Guid Motion = new Guid(0xCD09DAF1, 0x3B2E, 0x4C3D, 0xB5, 0x98, 0xB5, 0xE5, 0xFF, 0x93, 0xFD, 0x46);
       
        /// <summary>
        /// The orientation sensor cagetory property key.
        /// </summary>
        public static readonly Guid Orientation = new Guid(0x9E6C04B6, 0x96FE, 0x4954, 0xB7, 0x26, 0x68, 0x68, 0x2A, 0x47, 0x3F, 0x69);
        
        /// <summary>
        /// The mechanical sensor cagetory property key.
        /// </summary>
        public static readonly Guid Mechanical = new Guid(0x8D131D68, 0x8EF7, 0x4656, 0x80, 0xB5, 0xCC, 0xCB, 0xD9, 0x37, 0x91, 0xC5);
        
        /// <summary>
        /// The electrical sensor cagetory property key.
        /// </summary>
        public static readonly Guid Electrical = new Guid(0xFB73FCD8, 0xFC4A, 0x483C, 0xAC, 0x58, 0x27, 0xB6, 0x91, 0xC6, 0xBE, 0xFF);
        
        /// <summary>
        /// The bio-metric sensor cagetory property key.
        /// </summary>        
        public static readonly Guid Biometric = new Guid(0xCA19690F, 0xA2C7, 0x477D, 0xA9, 0x9E, 0x99, 0xEC, 0x6E, 0x2B, 0x56, 0x48);
        
        /// <summary>
        /// The light sensor cagetory property key.
        /// </summary>
        public static readonly Guid Light = new Guid(0x17A665C0, 0x9063, 0x4216, 0xB2, 0x02, 0x5C, 0x7A, 0x25, 0x5E, 0x18, 0xCE);
       
        /// <summary>
        /// The scanner sensor cagetory property key.
        /// </summary>
        public static readonly Guid Scanner = new Guid(0xB000E77E, 0xF5B5, 0x420F, 0x81, 0x5D, 0x2, 0x70, 0xA7, 0x26, 0xF2, 0x70);
    }
}
