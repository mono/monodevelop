// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// Sensor Property Key identifiers. This class will be removed once wrappers are developed.
    /// </summary>
    public static class SensorPropertyKeys
    {
        /// <summary>
        /// The common Guid used by the property keys.
        /// </summary>
        public static readonly Guid SensorPropertyCommonGuid = new Guid(0X7F8383EC, 0XD3EC, 0X495C, 0XA8, 0XCF, 0XB8, 0XBB, 0XE8, 0X5C, 0X29, 0X20);
        
        /// <summary>
        /// The sensor type property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyType = new PropertyKey(SensorPropertyCommonGuid, 2);
        
        /// <summary>
        /// The sensor state property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyState = new PropertyKey(SensorPropertyCommonGuid, 3);
        
        /// <summary>
        /// The sensor sampling rate property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertySamplingRate = new PropertyKey(SensorPropertyCommonGuid, 4);
        
        /// <summary>
        /// The sensor persistent unique id property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyPersistentUniqueId = new PropertyKey(SensorPropertyCommonGuid, 5);
        
        /// <summary>
        /// The sensor manufacturer property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyManufacturer = new PropertyKey(SensorPropertyCommonGuid, 6);
        
        /// <summary>
        /// The sensor model property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyModel = new PropertyKey(SensorPropertyCommonGuid, 7);
        
        /// <summary>
        /// The sensor serial number property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertySerialNumber = new PropertyKey(SensorPropertyCommonGuid, 8);
        
        /// <summary>
        /// The sensor friendly name property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyFriendlyName = new PropertyKey(SensorPropertyCommonGuid, 9);
        
        /// <summary>
        /// The sensor description property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyDescription = new PropertyKey(SensorPropertyCommonGuid, 10);
        
        /// <summary>
        /// The sensor connection type property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyConnectionType = new PropertyKey(SensorPropertyCommonGuid, 11);
        
        /// <summary>
        /// The sensor min report interval property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyMinReportInterval = new PropertyKey(SensorPropertyCommonGuid, 12);
        
        /// <summary>
        /// The sensor current report interval property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyCurrentReportInterval = new PropertyKey(SensorPropertyCommonGuid, 13);
        
        /// <summary>
        /// The sensor change sensitivity property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyChangeSensitivity = new PropertyKey(SensorPropertyCommonGuid, 14);
        
        /// <summary>
        /// The sensor device id property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyDeviceId = new PropertyKey(SensorPropertyCommonGuid, 15);
        
        /// <summary>
        /// The sensor accuracy property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyAccuracy = new PropertyKey(SensorPropertyCommonGuid, 16);
        
        /// <summary>
        /// The sensor resolution property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyResolution = new PropertyKey(SensorPropertyCommonGuid, 17);
        
        /// <summary>
        /// The sensor light response curve property key.
        /// </summary>
        public static readonly PropertyKey SensorPropertyLightResponseCurve = new PropertyKey(SensorPropertyCommonGuid, 18);

        /// <summary>
        /// The sensor date time property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeTimestamp = new PropertyKey(new Guid(0XDB5E0CF2, 0XCF1F, 0X4C18, 0XB4, 0X6C, 0XD8, 0X60, 0X11, 0XD6, 0X21, 0X50), 2);
        
        /// <summary>
        /// The sensor latitude in degrees property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeLatitudeDegrees = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 2);
       
        /// <summary>
        /// The sensor longitude in degrees property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeLongitudeDegrees = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 3);
        
        /// <summary>
        /// The sensor altitude from sea level in meters property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAltitudeSeaLevelMeters = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 4);
       
        /// <summary>
        /// The sensor altitude in meters property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAltitudeEllipsoidMeters = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 5);
       
        /// <summary>
        /// The sensor speed in knots property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeSpeedKnots = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 6);
       
        /// <summary>
        /// The sensor true heading in degrees property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeTrueHeadingDegrees = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 7);
        
        /// <summary>
        /// The sensor magnetic heading in degrees property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeMagneticHeadingDegrees = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 8);
        
        /// <summary>
        /// The sensor magnetic variation property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeMagneticVariation = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 9);
        
        /// <summary>
        /// The sensor data fix quality property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeFixQuality = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 10);
        
        /// <summary>
        /// The sensor data fix type property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeFixType = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 11);
        
        /// <summary>
        /// The sensor position dilution of precision property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypePositionDilutionOfPrecision = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 12);
        
        /// <summary>
        /// The sensor horizontal dilution of precision property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeHorizontalDilutionOfPrecision = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 13);
       
        /// <summary>
        /// The sensor vertical dilution of precision property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeVerticalDilutionOfPrecision = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 14);
        
        /// <summary>
        /// The sensor number of satelites used property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeSatellitesUsedCount = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 15);
        
        /// <summary>
        /// The sensor number of satelites used property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeSatellitesUsedPrns = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 16);
        
        /// <summary>
        /// The sensor view property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeSatellitesInView = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 17);
        
        /// <summary>
        /// The sensor view property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeSatellitesInViewPrns = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 18);
        
        /// <summary>
        /// The sensor elevation property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeSatellitesInViewElevation = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 19);
        
        /// <summary>
        /// The sensor azimuth value for satelites in view property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeSatellitesInViewAzimuth = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 20);
        
        /// <summary>
        /// The sensor signal to noise ratio for satelites in view property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeSatellitesInViewStnRatio = new PropertyKey(new Guid(0X055C74D8, 0XCA6F, 0X47D6, 0X95, 0XC6, 0X1E, 0XD3, 0X63, 0X7A, 0X0F, 0XF4), 21);
       
        /// <summary>
        /// The sensor temperature in celsius property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeTemperatureCelsius = new PropertyKey(new Guid(0X8B0AA2F1, 0X2D57, 0X42EE, 0X8C, 0XC0, 0X4D, 0X27, 0X62, 0X2B, 0X46, 0XC4), 2);
        
        /// <summary>
        /// The sensor gravitational acceleration (X-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAccelerationXG = new PropertyKey(new Guid(0X3F8A69A2, 0X7C5, 0X4E48, 0XA9, 0X65, 0XCD, 0X79, 0X7A, 0XAB, 0X56, 0XD5), 2);
        
        /// <summary>
        /// The sensor gravitational acceleration (Y-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAccelerationYG = new PropertyKey(new Guid(0X3F8A69A2, 0X7C5, 0X4E48, 0XA9, 0X65, 0XCD, 0X79, 0X7A, 0XAB, 0X56, 0XD5), 3);
        
        /// <summary>
        /// The sensor gravitational acceleration (Z-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAccelerationZG = new PropertyKey(new Guid(0X3F8A69A2, 0X7C5, 0X4E48, 0XA9, 0X65, 0XCD, 0X79, 0X7A, 0XAB, 0X56, 0XD5), 4);
        
        /// <summary>
        /// The sensor angular acceleration per second (X-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAngularAccelerationXDegreesPerSecond = new PropertyKey(new Guid(0X3F8A69A2, 0X7C5, 0X4E48, 0XA9, 0X65, 0XCD, 0X79, 0X7A, 0XAB, 0X56, 0XD5), 5);
        
        /// <summary>
        /// The sensor angular acceleration per second (Y-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAngularAccelerationYDegreesPerSecond = new PropertyKey(new Guid(0X3F8A69A2, 0X7C5, 0X4E48, 0XA9, 0X65, 0XCD, 0X79, 0X7A, 0XAB, 0X56, 0XD5), 6);
        
        /// <summary>
        /// The sensor angular acceleration per second (Z-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAngularAccelerationZDegreesPerSecond = new PropertyKey(new Guid(0X3F8A69A2, 0X7C5, 0X4E48, 0XA9, 0X65, 0XCD, 0X79, 0X7A, 0XAB, 0X56, 0XD5), 7);
        
        /// <summary>
        /// The sensor angle in degrees (X-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAngleXDegrees = new PropertyKey(new Guid(0XC2FB0F5F, 0XE2D2, 0X4C78, 0XBC, 0XD0, 0X35, 0X2A, 0X95, 0X82, 0X81, 0X9D), 2);
        
        /// <summary>
        /// The sensor angle in degrees (Y-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAngleYDegrees = new PropertyKey(new Guid(0XC2FB0F5F, 0XE2D2, 0X4C78, 0XBC, 0XD0, 0X35, 0X2A, 0X95, 0X82, 0X81, 0X9D), 3);
       
        /// <summary>
        /// The sensor angle in degrees (Z-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeAngleZDegrees = new PropertyKey(new Guid(0XC2FB0F5F, 0XE2D2, 0X4C78, 0XBC, 0XD0, 0X35, 0X2A, 0X95, 0X82, 0X81, 0X9D), 4);
        
        /// <summary>
        /// The sensor magnetic heading (X-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeMagneticHeadingXDegrees = new PropertyKey(new Guid(0XC2FB0F5F, 0XE2D2, 0X4C78, 0XBC, 0XD0, 0X35, 0X2A, 0X95, 0X82, 0X81, 0X9D), 5);
        
        /// <summary>
        /// The sensor magnetic heading (Y-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeMagneticHeadingYDegrees = new PropertyKey(new Guid(0XC2FB0F5F, 0XE2D2, 0X4C78, 0XBC, 0XD0, 0X35, 0X2A, 0X95, 0X82, 0X81, 0X9D), 6);
        
        /// <summary>
        /// The sensor magnetic heading (Z-axis) property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeMagneticHeadingZDegrees = new PropertyKey(new Guid(0XC2FB0F5F, 0XE2D2, 0X4C78, 0XBC, 0XD0, 0X35, 0X2A, 0X95, 0X82, 0X81, 0X9D), 7);
        
        /// <summary>
        /// The sensor distance (X-axis) data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeDistanceXMeters = new PropertyKey(new Guid(0XC2FB0F5F, 0XE2D2, 0X4C78, 0XBC, 0XD0, 0X35, 0X2A, 0X95, 0X82, 0X81, 0X9D), 8);
        
        /// <summary>
        /// The sensor distance (Y-axis) data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeDistanceYMeters = new PropertyKey(new Guid(0XC2FB0F5F, 0XE2D2, 0X4C78, 0XBC, 0XD0, 0X35, 0X2A, 0X95, 0X82, 0X81, 0X9D), 9);
        
        /// <summary>
        /// The sensor distance (Z-axis) data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeDistanceZMeters = new PropertyKey(new Guid(0XC2FB0F5F, 0XE2D2, 0X4C78, 0XBC, 0XD0, 0X35, 0X2A, 0X95, 0X82, 0X81, 0X9D), 10);
        
        /// <summary>
        /// The sensor boolean switch data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeBooleanSwitchState = new PropertyKey(new Guid(0X38564A7C, 0XF2F2, 0X49BB, 0X9B, 0X2B, 0XBA, 0X60, 0XF6, 0X6A, 0X58, 0XDF), 2);
        
        /// <summary>
        /// The sensor multi-value switch data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeMultivalueSwitchState = new PropertyKey(new Guid(0X38564A7C, 0XF2F2, 0X49BB, 0X9B, 0X2B, 0XBA, 0X60, 0XF6, 0X6A, 0X58, 0XDF), 3);
        
        /// <summary>
        /// The sensor boolean switch array state data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeBooleanSwitchArrayState = new PropertyKey(new Guid(0X38564A7C, 0XF2F2, 0X49BB, 0X9B, 0X2B, 0XBA, 0X60, 0XF6, 0X6A, 0X58, 0XDF), 10);
        
        /// <summary>
        /// The sensor force (in newtons) data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeForceNewtons = new PropertyKey(new Guid(0X38564A7C, 0XF2F2, 0X49BB, 0X9B, 0X2B, 0XBA, 0X60, 0XF6, 0X6A, 0X58, 0XDF), 4);
        
        /// <summary>
        /// The sensor weight (in kilograms) data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeWeightKilograms = new PropertyKey(new Guid(0X38564A7C, 0XF2F2, 0X49BB, 0X9B, 0X2B, 0XBA, 0X60, 0XF6, 0X6A, 0X58, 0XDF), 5);
        
        /// <summary>
        /// The sensor pressure data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypePressurePascal = new PropertyKey(new Guid(0X38564A7C, 0XF2F2, 0X49BB, 0X9B, 0X2B, 0XBA, 0X60, 0XF6, 0X6A, 0X58, 0XDF), 6);
       
        /// <summary>
        /// The sensor strain data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeStrain = new PropertyKey(new Guid(0X38564A7C, 0XF2F2, 0X49BB, 0X9B, 0X2B, 0XBA, 0X60, 0XF6, 0X6A, 0X58, 0XDF), 7);
       
        /// <summary>
        /// The sensor human presence data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeHumanPresence = new PropertyKey(new Guid(0X2299288A, 0X6D9E, 0X4B0B, 0XB7, 0XEC, 0X35, 0X28, 0XF8, 0X9E, 0X40, 0XAF), 2);
       
        /// <summary>
        /// The sensor human proximity data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeHumanProximity = new PropertyKey(new Guid(0X2299288A, 0X6D9E, 0X4B0B, 0XB7, 0XEC, 0X35, 0X28, 0XF8, 0X9E, 0X40, 0XAF), 3);
        
        /// <summary>
        /// The sensor light data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeLightLux = new PropertyKey(new Guid(0XE4C77CE2, 0XDCB7, 0X46E9, 0X84, 0X39, 0X4F, 0XEC, 0X54, 0X88, 0X33, 0XA6), 2);
       
        /// <summary>
        /// The sensor 40-bit RFID tag data property key.
        /// </summary>
        public static readonly PropertyKey SensorDataTypeRfidTag40Bit = new PropertyKey(new Guid(0XD7A59A3C, 0X3421, 0X44AB, 0X8D, 0X3A, 0X9D, 0XE8, 0XAB, 0X6C, 0X4C, 0XAE), 2);
    }
}
