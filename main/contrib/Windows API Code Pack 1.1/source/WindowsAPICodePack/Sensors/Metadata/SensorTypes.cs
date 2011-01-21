// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// Contains a list of well known sensor types. This class will be removed once wrappers are developed.
    /// </summary>
    public static class SensorTypes
    {
        /// <summary>
        /// The GPS location sensor type property key.
        /// </summary>
        public static readonly Guid LocationGps = new Guid(0xED4CA589, 0x327A, 0x4FF9, 0xA5, 0x60, 0x91, 0xDA, 0x4B, 0x48, 0x27, 0x5E);
        
        /// <summary>
        /// The environmental temperature sensor type property key.
        /// </summary>
        public static readonly Guid EnvironmentalTemperature = new Guid(0x04FD0EC4, 0xD5DA, 0x45FA, 0x95, 0xA9, 0x5D, 0xB3, 0x8E, 0xE1, 0x93, 0x06);
        
        /// <summary>
        /// The environmental atmostpheric pressure sensor type property key.
        /// </summary>
        public static readonly Guid EnvironmentalAtmosphericPressure = new Guid(0xE903829, 0xFF8A, 0x4A93, 0x97, 0xDF, 0x3D, 0xCB, 0xDE, 0x40, 0x22, 0x88);
        
        /// <summary>
        /// The environmental humidity sensor type property key.
        /// </summary>
        public static readonly Guid EnvironmentalHumidity = new Guid(0x5C72BF67, 0xBD7E, 0x4257, 0x99, 0xB, 0x98, 0xA3, 0xBA, 0x3B, 0x40, 0xA);
        
        /// <summary>
        /// The environmental wind speed sensor type property key.
        /// </summary>
        public static readonly Guid EnvironmentalWindSpeed = new Guid(0xDD50607B, 0xA45F, 0x42CD, 0x8E, 0xFD, 0xEC, 0x61, 0x76, 0x1C, 0x42, 0x26);
       
        /// <summary>
        /// The environmental wind direction sensor type property key.
        /// </summary>
        public static readonly Guid EnvironmentalWindDirection = new Guid(0x9EF57A35, 0x9306, 0x434D, 0xAF, 0x9, 0x37, 0xFA, 0x5A, 0x9C, 0x0, 0xBD);
        
        /// <summary>
        /// The accelerometer sensor type property key.
        /// </summary>
        public static readonly Guid Accelerometer1D = new Guid(0xC04D2387, 0x7340, 0x4CC2, 0x99, 0x1E, 0x3B, 0x18, 0xCB, 0x8E, 0xF2, 0xF4);
        
        /// <summary>
        /// The 2D accelerometer sensor type property key.
        /// </summary>
        public static readonly Guid Accelerometer2D = new Guid(0xB2C517A8, 0xF6B5, 0x4BA6, 0xA4, 0x23, 0x5D, 0xF5, 0x60, 0xB4, 0xCC, 0x7);
        
        /// <summary>
        /// The 3D accelerometer sensor type property key.
        /// </summary>
        public static readonly Guid Accelerometer3D = new Guid(0xC2FB0F5F, 0xE2D2, 0x4C78, 0xBC, 0xD0, 0x35, 0x2A, 0x95, 0x82, 0x81, 0x9D);
        
        /// <summary>
        /// The motion sensor type property key.
        /// </summary>
        public static readonly Guid MotionDetector = new Guid(0x5C7C1A12, 0x30A5, 0x43B9, 0xA4, 0xB2, 0xCF, 0x9, 0xEC, 0x5B, 0x7B, 0xE8);
        
        /// <summary>
        /// The gyrometer sensor type property key.
        /// </summary>
        public static readonly Guid Gyrometer1D = new Guid(0xFA088734, 0xF552, 0x4584, 0x83, 0x24, 0xED, 0xFA, 0xF6, 0x49, 0x65, 0x2C);
        
        /// <summary>
        /// The 2D gyrometer sensor type property key.
        /// </summary>
        public static readonly Guid Gyrometer2D = new Guid(0x31EF4F83, 0x919B, 0x48BF, 0x8D, 0xE0, 0x5D, 0x7A, 0x9D, 0x24, 0x5, 0x56);
        
        /// <summary>
        /// The 3D gyrometer sensor type property key.
        /// </summary>
        public static readonly Guid Gyrometer3D = new Guid(0x9485F5A, 0x759E, 0x42C2, 0xBD, 0x4B, 0xA3, 0x49, 0xB7, 0x5C, 0x86, 0x43);
        
        /// <summary>
        /// The speedometer sensor type property key.
        /// </summary>
        public static readonly Guid Speedometer = new Guid(0x6BD73C1F, 0xBB4, 0x4310, 0x81, 0xB2, 0xDF, 0xC1, 0x8A, 0x52, 0xBF, 0x94);
        
        /// <summary>
        /// The compass sensor type property key.
        /// </summary>
        public static readonly Guid Compass1D = new Guid(0xA415F6C5, 0xCB50, 0x49D0, 0x8E, 0x62, 0xA8, 0x27, 0xB, 0xD7, 0xA2, 0x6C);
        
        /// <summary>
        /// The 2D compass sensor type property key.
        /// </summary>
        public static readonly Guid Compass2D = new Guid(0x15655CC0, 0x997A, 0x4D30, 0x84, 0xDB, 0x57, 0xCA, 0xBA, 0x36, 0x48, 0xBB);
        
        /// <summary>
        /// The 3D compass sensor type property key.
        /// </summary>
        public static readonly Guid Compass3D = new Guid(0x76B5CE0D, 0x17DD, 0x414D, 0x93, 0xA1, 0xE1, 0x27, 0xF4, 0xB, 0xDF, 0x6E);
        
        /// <summary>
        /// The inclinometer sensor type property key.
        /// </summary>
        public static readonly Guid Inclinometer1D = new Guid(0xB96F98C5, 0x7A75, 0x4BA7, 0x94, 0xE9, 0xAC, 0x86, 0x8C, 0x96, 0x6D, 0xD8);
       
        /// <summary>
        /// The 2D inclinometer sensor type property key.
        /// </summary>
        public static readonly Guid Inclinometer2D = new Guid(0xAB140F6D, 0x83EB, 0x4264, 0xB7, 0xB, 0xB1, 0x6A, 0x5B, 0x25, 0x6A, 0x1);
        
        /// <summary>
        /// The 3D inclinometer sensor type property key.
        /// </summary>
        public static readonly Guid Inclinometer3D = new Guid(0xB84919FB, 0xEA85, 0x4976, 0x84, 0x44, 0x6F, 0x6F, 0x5C, 0x6D, 0x31, 0xDB);
       
        /// <summary>
        /// The distance sensor type property key.
        /// </summary>
        public static readonly Guid Distance1D = new Guid(0x5F14AB2F, 0x1407, 0x4306, 0xA9, 0x3F, 0xB1, 0xDB, 0xAB, 0xE4, 0xF9, 0xC0);
        
        /// <summary>
        /// The 2D sensor type property key.
        /// </summary>
        public static readonly Guid Distance2D = new Guid(0x5CF9A46C, 0xA9A2, 0x4E55, 0xB6, 0xA1, 0xA0, 0x4A, 0xAF, 0xA9, 0x5A, 0x92);
        
        /// <summary>
        /// The 3D distance sensor type property key.
        /// </summary>
        public static readonly Guid Distance3D = new Guid(0xA20CAE31, 0xE25, 0x4772, 0x9F, 0xE5, 0x96, 0x60, 0x8A, 0x13, 0x54, 0xB2);
       
        /// <summary>
        /// The electrical voltage sensor type property key.
        /// </summary>
        public static readonly Guid Voltage = new Guid(0xC5484637, 0x4FB7, 0x4953, 0x98, 0xB8, 0xA5, 0x6D, 0x8A, 0xA1, 0xFB, 0x1E);
       
        /// <summary>
        /// The electrical current sensor type property key.
        /// </summary>
        public static readonly Guid Current = new Guid(0x5ADC9FCE, 0x15A0, 0x4BBE, 0xA1, 0xAD, 0x2D, 0x38, 0xA9, 0xAE, 0x83, 0x1C);
       
        /// <summary>
        /// The boolean switch sensor type property key.
        /// </summary>
        public static readonly Guid BooleanSwitch = new Guid(0x9C7E371F, 0x1041, 0x460B, 0x8D, 0x5C, 0x71, 0xE4, 0x75, 0x2E, 0x35, 0xC);
        
        /// <summary>
        /// The boolean switch array sensor property key.
        /// </summary>
        public static readonly Guid BooleanSwitchArray = new Guid(0X545C8BA5, 0XB143, 0X4545, 0X86, 0X8F, 0XCA, 0X7F, 0XD9, 0X86, 0XB4, 0XF6);
        
        /// <summary>
        /// The multiple value switch sensor type property key.
        /// </summary>       
        public static readonly Guid MultivalueSwitch = new Guid(0xB3EE4D76, 0x37A4, 0x4402, 0xB2, 0x5E, 0x99, 0xC6, 0xA, 0x77, 0x5F, 0xA1);
       
        /// <summary>
        /// The force sensor type property key.
        /// </summary>
        public static readonly Guid Force = new Guid(0xC2AB2B02, 0x1A1C, 0x4778, 0xA8, 0x1B, 0x95, 0x4A, 0x17, 0x88, 0xCC, 0x75);
        
        /// <summary>
        /// The scale sensor type property key.
        /// </summary>
        public static readonly Guid Scale = new Guid(0xC06DD92C, 0x7FEB, 0x438E, 0x9B, 0xF6, 0x82, 0x20, 0x7F, 0xFF, 0x5B, 0xB8);
        
        /// <summary>
        /// The pressure sensor type property key.
        /// </summary>
        public static readonly Guid Pressure = new Guid(0x26D31F34, 0x6352, 0x41CF, 0xB7, 0x93, 0xEA, 0x7, 0x13, 0xD5, 0x3D, 0x77);
        
        /// <summary>
        /// The strain sensor type property key.
        /// </summary>
        public static readonly Guid Strain = new Guid(0xC6D1EC0E, 0x6803, 0x4361, 0xAD, 0x3D, 0x85, 0xBC, 0xC5, 0x8C, 0x6D, 0x29);
        
        /// <summary>
        /// The Human presence sensor type property key.
        /// </summary>
        public static readonly Guid HumanPresence = new Guid(0xC138C12B, 0xAD52, 0x451C, 0x93, 0x75, 0x87, 0xF5, 0x18, 0xFF, 0x10, 0xC6);
       
        /// <summary>
        /// The human proximity sensor type property key.
        /// </summary>
        public static readonly Guid HumanProximity = new Guid(0x5220DAE9, 0x3179, 0x4430, 0x9F, 0x90, 0x6, 0x26, 0x6D, 0x2A, 0x34, 0xDE);
        
        /// <summary>
        /// The touch sensor type property key.
        /// </summary>
        public static readonly Guid Touch = new Guid(0x17DB3018, 0x6C4, 0x4F7D, 0x81, 0xAF, 0x92, 0x74, 0xB7, 0x59, 0x9C, 0x27);
        
        /// <summary>
        /// The ambient light sensor type property key.
        /// </summary>
        public static readonly Guid AmbientLight = new Guid(0x97F115C8, 0x599A, 0x4153, 0x88, 0x94, 0xD2, 0xD1, 0x28, 0x99, 0x91, 0x8A);
        
        /// <summary>
        /// The RFID sensor type property key.
        /// </summary>
        public static readonly Guid RfidScanner = new Guid(0x44328EF5, 0x2DD, 0x4E8D, 0xAD, 0x5D, 0x92, 0x49, 0x83, 0x2B, 0x2E, 0xCA);
        
        /// <summary>
        /// The bar code scanner sensor type property key.
        /// </summary>
        public static readonly Guid BarcodeScanner = new Guid(0x990B3D8F, 0x85BB, 0x45FF, 0x91, 0x4D, 0x99, 0x8C, 0x4, 0xF3, 0x72, 0xDF);
    }
}
