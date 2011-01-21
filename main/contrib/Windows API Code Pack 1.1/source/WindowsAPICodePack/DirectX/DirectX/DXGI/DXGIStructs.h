//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System;
using namespace System::Globalization;
using namespace Microsoft::WindowsAPICodePack::DirectX;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct2D1;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {

    value struct ColorF;

}}}}

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics { 

/// <summary>
/// A Luid is a 64-bit value guaranteed to be unique only on the system on which it was generated. The uniqueness of a locally unique identifier (LUID) is guaranteed only until the system is restarted. An LUID is not for direct manipulation. 
/// Applications are to use functions and structures to manipulate Luid values.
/// </summary>
public value struct Luid 
{
public:
    /// <summary>
    /// Low order bits.
    /// <para>(Also see DirectX SDK: LUID.LowPart)</para>
    /// </summary>
    property Int32 LowPart
    {
        Int32 get()
        {
            return lowPart;
        }

        void set(Int32 value)
        {
            lowPart = value;
        }
    }
    /// <summary>
    /// High order bits.
    /// <para>(Also see DirectX SDK: LUID.HighPart)</para>
    /// </summary>
    property Int32 HighPart
    {
        Int32 get()
        {
            return highPart;
        }

        void set(Int32 value)
        {
            highPart = value;
        }
    }

private:

    Int32 lowPart;
    Int32 highPart;

internal:
    Luid(const LUID& luid)
    {
        LowPart = luid.LowPart;
        HighPart = luid.HighPart;
    }
public:

    static Boolean operator == (Luid luid1, Luid luid2)
    {
        return (luid1.lowPart == luid2.lowPart) &&
            (luid1.highPart == luid2.highPart);
    }

    static Boolean operator != (Luid luid1, Luid luid2)
    {
        return !(luid1 == luid2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Luid::typeid)
        {
            return false;
        }

        return *this == safe_cast<Luid>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + lowPart.GetHashCode();
        hashCode = hashCode * 31 + highPart.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// Describes a monitor.
/// <para>(Also see Windows SDK: MONITORINFOEX)</para>
/// </summary>
public value struct MonitorInfo 
{
public:
    /// <summary>
    /// Specifies the display monitor rectangle, expressed in virtual-screen coordinates. 
    /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
    /// </summary>
    property D3DRect MonitorCoordinates
    {
        D3DRect get()
        {
            return monitorCoordinates;
        }

        void set(D3DRect value)
        {
            monitorCoordinates = value;
        }
    }

    /// <summary>
    /// Specifies the work area rectangle of the display monitor 
    /// that can be used by applications, expressed in virtual-screen coordinates. 
    /// Windows uses this rectangle to maximize an application on the monitor. 
    /// The rest of the area in MonitorRect contains system windows such as the task bar 
    /// and side bars. Note that if the monitor is not the primary display monitor, 
    /// some of the rectangle's coordinates may be negative values.
    /// </summary>
    property D3DRect WorkCoordinates
    {
        D3DRect get()
        {
            return workCoordinates;
        }

        void set(D3DRect value)
        {
            workCoordinates = value;
        }
    }

    /// <summary>
    /// Indicates if this is the primary monitor
    /// </summary>
    property Boolean IsPrimaryMonitor
    {
        Boolean get()
        {
            return isPrimaryMonitor;
        }

        void set(Boolean value)
        {
            isPrimaryMonitor = value;
        }
    }

    /// <summary>
    /// Handle to this monitor
    /// </summary>
    property IntPtr MonitorHandle
    {
        IntPtr get()
        {
            return monitorHandle;
        }

        void set(IntPtr value)
        {
            monitorHandle = value;
        }
    }

private:

    D3DRect monitorCoordinates;
    D3DRect workCoordinates;
    Boolean isPrimaryMonitor;
    IntPtr monitorHandle;

internal:
    MonitorInfo(HMONITOR hMon)
    {
        MONITORINFOEX pInfo;
        pInfo.cbSize = sizeof(MONITORINFOEXW);

        if (!GetMonitorInfo(hMon, &pInfo))
        {
            int hr = static_cast<int>(GetLastError());

            throw gcnew GraphicsException(
                String::Format(CultureInfo::CurrentCulture,
                "Unable to obtain monitor info. Last Error = 0x{0:X}.", hr), hr);
        }

        MonitorHandle = IntPtr(hMon);
        MonitorCoordinates = D3DRect(pInfo.rcMonitor);
        WorkCoordinates = D3DRect(pInfo.rcWork);
        IsPrimaryMonitor = (pInfo.dwFlags & MONITORINFOF_PRIMARY) == MONITORINFOF_PRIMARY;
    }
};


/// <summary>
/// Represents a rational number.
/// <para>(Also see DirectX SDK: DXGI_RATIONAL)</para>
/// </summary>
public value struct Rational 
{
public:
    /// <summary>
    /// An unsigned integer value representing the top of the rational number.
    /// <para>(Also see DirectX SDK: DXGI_RATIONAL.Numerator)</para>
    /// </summary>
    property UInt32 Numerator
    {
        UInt32 get()
        {
            return numerator;
        }

        void set(UInt32 value)
        {
            numerator = value;
        }
    }

    /// <summary>
    /// An unsigned integer value representing the bottom of the rational number.
    /// <para>(Also see DirectX SDK: DXGI_RATIONAL.Denominator)</para>
    /// </summary>
    property UInt32 Denominator
    {
        UInt32 get()
        {
            return denominator;
        }

        void set(UInt32 value)
        {
            denominator = value;
        }
    }
private:

    UInt32 numerator;
    UInt32 denominator;

internal:
    Rational (const DXGI_RATIONAL& rational)
    {
        Numerator = rational.Numerator;
        Denominator = rational.Denominator;
    }
public:

    Rational(UInt32 numerator, UInt32 denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    static Boolean operator == (Rational rational1, Rational rational2)
    {
        return (rational1.numerator == rational2.numerator) &&
            (rational1.denominator == rational2.denominator);
    }

    static Boolean operator != (Rational rational1, Rational rational2)
    {
        return !(rational1 == rational2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Rational::typeid)
        {
            return false;
        }

        return *this == safe_cast<Rational>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + numerator.GetHashCode();
        hashCode = hashCode * 31 + denominator.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Represents an RGB color.
/// <para>(Also see DirectX SDK: DXGI_RGB)</para>
/// </summary>
public value struct ColorRgb
{
public:
    /// <summary>
    /// A value representing the color of the red component. The range of this value is between 0 and 1.
    /// <para>(Also see DirectX SDK: DXGI_RGB.Red)</para>
    /// </summary>
    property Single Red
    {
        Single get()
        {
            return red;
        }

        void set(Single value)
        {
            red = value;
        }
    }

    /// <summary>
    /// A value representing the color of the green component. The range of this value is between 0 and 1.
    /// <para>(Also see DirectX SDK: DXGI_RGB.Green)</para>
    /// </summary>
    property Single Green
    {
        Single get()
        {
            return green;
        }

        void set(Single value)
        {
            green = value;
        }
    }

    /// <summary>
    /// A value representing the color of the blue component. The range of this value is between 0 and 1.
    /// <para>(Also see DirectX SDK: DXGI_RGB.Blue)</para>
    /// </summary>
    property Single Blue
    {
        Single get()
        {
            return blue;
        }

        void set(Single value)
        {
            blue = value;
        }
    }

private:

    Single red;
    Single green;
    Single blue;

public:
    ColorRgb(Single red, Single green, Single blue)
    {
        Red = red;
        Green = green;
        Blue = blue;
    }

    static Boolean operator == (ColorRgb color1, ColorRgb color2)
    {
        return 
            (color1.Red == color2.Red)  &&
            (color1.Green == color2.Green)  &&
            (color1.Blue == color2.Blue);
    }

    static Boolean operator != (ColorRgb color1, ColorRgb color2)
    {
        return !(color1 == color2);
    }

internal:
    ColorRgb(const DXGI_RGB& rgb)
    {
        Red = rgb.Red;
        Green = rgb.Green;
        Blue = rgb.Blue;
    }

   void CopyTo(DXGI_RGB* rgb)
    {
        rgb->Red = Red;
        rgb->Green = Green;
        rgb->Blue = Blue;
    }

public:

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != ColorRgb::typeid)
        {
            return false;
        }

        return *this == safe_cast<ColorRgb>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + red.GetHashCode();
        hashCode = hashCode * 31 + green.GetHashCode();
        hashCode = hashCode * 31 + blue.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// Represents an RGBA color.
/// </summary>
public value struct ColorRgba
{
public:
    /// <summary>
    /// A value representing the color of the red component. The range of this value is between 0 and 1.
    /// </summary>
    property Single Red
    {
        Single get()
        {
            return red;
        }

        void set(Single value)
        {
            red = value;
        }
    }

    /// <summary>
    /// A value representing the color of the green component. The range of this value is between 0 and 1.
    /// </summary>
    property Single Green
    {
        Single get()
        {
            return green;
        }

        void set(Single value)
        {
            green = value;
        }
    }

    /// <summary>
    /// A value representing the color of the blue component. The range of this value is between 0 and 1.
    /// </summary>
    property Single Blue
    {
        Single get()
        {
            return blue;
        }

        void set(Single value)
        {
            blue = value;
        }
    }

    /// <summary>
    /// A value representing the alpha channel component. The range of this value is between 0 and 1.
    /// </summary>
    property Single Alpha
    {
        Single get()
        {
            return alpha;
        }

        void set(Single value)
        {
            alpha = value;
        }
    }

private:

    Single red;
    Single green;
    Single blue;
    Single alpha;

public:
    ColorRgba(array<float>^ rgbaColors)
    {
        if (rgbaColors == nullptr)
        {
            throw gcnew ArgumentNullException("rgbaColors");
        }

        if (rgbaColors->Length != 4)
        {
            throw gcnew ArgumentOutOfRangeException("rgbaColors","Length of input array must be exactly \"4\".");
        }

        Red = rgbaColors[0];
        Green = rgbaColors[1];
        Blue = rgbaColors[2];
        Alpha = rgbaColors[3];
    }

    ColorRgba(Single red, Single green, Single blue, Single alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

	ColorRgba(ColorF color);

    ColorRgba(ColorF color, Single alpha);

    static Boolean operator == (ColorRgba color1, ColorRgba color2)
    {
        return 
            (color1.Red == color2.Red)  &&
            (color1.Green == color2.Green)  &&
            (color1.Blue == color2.Blue)  &&
            (color1.Alpha == color2.Alpha);
    }

    static Boolean operator != (ColorRgba color1, ColorRgba color2)
    {
        return !(color1 == color2);
    }

internal:

    ColorRgba(const FLOAT rgbaColors[4])
    {
        Red = rgbaColors[0];
        Green = rgbaColors[1];
        Blue = rgbaColors[2];
        Alpha = rgbaColors[3];
    }

private:
    static const UINT32 sc_redShift   = 16;
    static const UINT32 sc_greenShift = 8;
    static const UINT32 sc_blueShift  = 0;    

    static const UINT32 sc_redMask = 0xff << sc_redShift;
    static const UINT32 sc_greenMask = 0xff << sc_greenShift;
    static const UINT32 sc_blueMask = 0xff << sc_blueShift;      

public:

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != ColorRgba::typeid)
        {
            return false;
        }

        return *this == safe_cast<ColorRgba>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + red.GetHashCode();
        hashCode = hashCode * 31 + green.GetHashCode();
        hashCode = hashCode * 31 + blue.GetHashCode();
        hashCode = hashCode * 31 + alpha.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// Describes an adapter (or video card) by using Graphics 1.0.
/// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC)</para>
/// </summary>
public value struct AdapterDescription 
{
public:
    /// <summary>
    /// A string that contains the adapter description.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC.Description)</para>
    /// </summary>
    property String^ Description
    {
        String^ get()
        {
            return description;
        }

        void set(String^ value)
        {
            description = value;
        }
    }
    /// <summary>
    /// The PCI ID of the hardware vendor.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC.VendorId)</para>
    /// </summary>
    property UInt32 VendorId
    {
        UInt32 get()
        {
            return vendorId;
        }

        void set(UInt32 value)
        {
            vendorId = value;
        }
    }
    /// <summary>
    /// The PCI ID of the hardware device.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC.DeviceId)</para>
    /// </summary>
    property UInt32 DeviceId
    {
        UInt32 get()
        {
            return deviceId;
        }

        void set(UInt32 value)
        {
            deviceId = value;
        }
    }
    /// <summary>
    /// The PCI ID of the sub system.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC.SubSysId)</para>
    /// </summary>
    property UInt32 SubSysId
    {
        UInt32 get()
        {
            return subSysId;
        }

        void set(UInt32 value)
        {
            subSysId = value;
        }
    }
    /// <summary>
    /// The PCI ID of the revision number of the adapter.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC.Revision)</para>
    /// </summary>
    property UInt32 Revision
    {
        UInt32 get()
        {
            return revision;
        }

        void set(UInt32 value)
        {
            revision = value;
        }
    }
    /// <summary>
    /// The number of bytes of dedicated video memory that are not shared with the CPU.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC.DedicatedVideoMemory)</para>
    /// </summary>
    property UInt32 DedicatedVideoMemory
    {
        UInt32 get()
        {
            return dedicatedVideoMemory;
        }

        void set(UInt32 value)
        {
            dedicatedVideoMemory = value;
        }
    }
    /// <summary>
    /// The number of bytes of dedicated system memory that are not shared with the GPU. This memory is allocated from available system memory at boot time.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC.DedicatedSystemMemory)</para>
    /// </summary>
    property UInt32 DedicatedSystemMemory
    {
        UInt32 get()
        {
            return dedicatedSystemMemory;
        }

        void set(UInt32 value)
        {
            dedicatedSystemMemory = value;
        }
    }
    /// <summary>
    /// The number of bytes of shared system memory. This is the maximum value of system memory that may be consumed by the adapter during operation. Any incidental memory consumed by the driver as it manages and uses video memory is additional.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC.SharedSystemMemory)</para>
    /// </summary>
    property UInt32 SharedSystemMemory
    {
        UInt32 get()
        {
            return sharedSystemMemory;
        }

        void set(UInt32 value)
        {
            sharedSystemMemory = value;
        }
    }
    /// <summary>
    /// A unique value that identifies the adapter. See LUID for a definition of the structure. LUID is defined in dxgi.h.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC.AdapterLuid)</para>
    /// </summary>
    property Luid AdapterLuid
    {
        Luid get()
        {
            return adapterLuid;
        }

        void set(Luid value)
        {
            adapterLuid = value;
        }
    }

private:

    String^ description;
    UInt32 vendorId;
    UInt32 deviceId;
    UInt32 subSysId;
    UInt32 revision;
    UInt32 dedicatedVideoMemory;
    UInt32 dedicatedSystemMemory;
    UInt32 sharedSystemMemory;
    Luid adapterLuid;

internal:
    AdapterDescription(const DXGI_ADAPTER_DESC& adapterDescription)
    {
        VendorId = adapterDescription.VendorId;
        DeviceId = adapterDescription.DeviceId;
        SubSysId = adapterDescription.SubSysId;
        Revision = adapterDescription.Revision;
        DedicatedVideoMemory = static_cast<UInt32>(adapterDescription.DedicatedVideoMemory);
        DedicatedSystemMemory = static_cast<UInt32>(adapterDescription.DedicatedSystemMemory);
        AdapterLuid = Luid(adapterDescription.AdapterLuid);
        Description = gcnew String(adapterDescription.Description);
    }

public:

    static Boolean operator == (AdapterDescription adapterDescription1, AdapterDescription adapterDescription2)
    {
        return (adapterDescription1.description == adapterDescription2.description) &&
            (adapterDescription1.vendorId == adapterDescription2.vendorId) &&
            (adapterDescription1.deviceId == adapterDescription2.deviceId) &&
            (adapterDescription1.subSysId == adapterDescription2.subSysId) &&
            (adapterDescription1.revision == adapterDescription2.revision) &&
            (adapterDescription1.dedicatedVideoMemory == adapterDescription2.dedicatedVideoMemory) &&
            (adapterDescription1.dedicatedSystemMemory == adapterDescription2.dedicatedSystemMemory) &&
            (adapterDescription1.sharedSystemMemory == adapterDescription2.sharedSystemMemory) &&
            (adapterDescription1.adapterLuid == adapterDescription2.adapterLuid);
    }

    static Boolean operator != (AdapterDescription adapterDescription1, AdapterDescription adapterDescription2)
    {
        return !(adapterDescription1 == adapterDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != AdapterDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<AdapterDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + description->GetHashCode();
        hashCode = hashCode * 31 + vendorId.GetHashCode();
        hashCode = hashCode * 31 + deviceId.GetHashCode();
        hashCode = hashCode * 31 + subSysId.GetHashCode();
        hashCode = hashCode * 31 + revision.GetHashCode();
        hashCode = hashCode * 31 + dedicatedVideoMemory.GetHashCode();
        hashCode = hashCode * 31 + dedicatedSystemMemory.GetHashCode();
        hashCode = hashCode * 31 + sharedSystemMemory.GetHashCode();
        hashCode = hashCode * 31 + adapterLuid.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes an adapter (or video card) using Graphics 1.1.
/// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1)</para>
/// </summary>
public value struct AdapterDescription1 
{
public:
    /// <summary>
    /// A string that contains the adapter description.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1.Description)</para>
    /// </summary>
    property String^ Description
    {
        String^ get()
        {
            return description;
        }

        void set(String^ value)
        {
            description = value;
        }
    }

    /// <summary>
    /// The PCI ID of the hardware vendor.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1.VendorId)</para>
    /// </summary>
    property UInt32 VendorId
    {
        UInt32 get()
        {
            return vendorId;
        }

        void set(UInt32 value)
        {
            vendorId = value;
        }
    }
    /// <summary>
    /// The PCI ID of the hardware device.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1.DeviceId)</para>
    /// </summary>
    property UInt32 DeviceId
    {
        UInt32 get()
        {
            return deviceId;
        }

        void set(UInt32 value)
        {
            deviceId = value;
        }
    }
    /// <summary>
    /// The PCI ID of the sub system.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1.SubSysId)</para>
    /// </summary>
    property UInt32 SubSysId
    {
        UInt32 get()
        {
            return subSysId;
        }

        void set(UInt32 value)
        {
            subSysId = value;
        }
    }
    /// <summary>
    /// The PCI ID of the revision number of the adapter.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1.Revision)</para>
    /// </summary>
    property UInt32 Revision
    {
        UInt32 get()
        {
            return revision;
        }

        void set(UInt32 value)
        {
            revision = value;
        }
    }
    /// <summary>
    /// The number of bytes of dedicated video memory that are not shared with the CPU.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1.DedicatedVideoMemory)</para>
    /// </summary>
    property UInt32 DedicatedVideoMemory
    {
        UInt32 get()
        {
            return dedicatedVideoMemory;
        }

        void set(UInt32 value)
        {
            dedicatedVideoMemory = value;
        }
    }
    /// <summary>
    /// The number of bytes of dedicated system memory that are not shared with the GPU. This memory is allocated from available system memory at boot time.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1.DedicatedSystemMemory)</para>
    /// </summary>
    property UInt32 DedicatedSystemMemory
    {
        UInt32 get()
        {
            return dedicatedSystemMemory;
        }

        void set(UInt32 value)
        {
            dedicatedSystemMemory = value;
        }
    }
    /// <summary>
    /// The number of bytes of shared system memory. This is the maximum value of system memory that may be consumed by the adapter during operation. Any incidental memory consumed by the driver as it manages and uses video memory is additional.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1.SharedSystemMemory)</para>
    /// </summary>
    property UInt32 SharedSystemMemory
    {
        UInt32 get()
        {
            return sharedSystemMemory;
        }

        void set(UInt32 value)
        {
            sharedSystemMemory = value;
        }
    }
    /// <summary>
    /// A unique value that identifies the adapter. See LUID for a definition of the structure. LUID is defined in dxgi.h.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1.AdapterLuid)</para>
    /// </summary>
    property Luid AdapterLuid
    {
        Luid get()
        {
            return adapterLuid;
        }

        void set(Luid value)
        {
            adapterLuid = value;
        }
    }
    /// <summary>
    /// A member of the AdapterOptions enumerated type that describes the adapter type.  
    /// The AdapterOptions Remote flag specifies that the adapter is a remote adapter.
    /// <para>(Also see DirectX SDK: DXGI_ADAPTER_DESC1.Flags)</para>
    /// </summary>
    property AdapterOptions Options
    {
        AdapterOptions get()
        {
            return flags;
        }

        void set(AdapterOptions value)
        {
            flags = value;
        }
    }
private:

    String^ description;
    UInt32 vendorId;
    UInt32 deviceId;
    UInt32 subSysId;
    UInt32 revision;
    UInt32 dedicatedVideoMemory;
    UInt32 dedicatedSystemMemory;
    UInt32 sharedSystemMemory;
    Luid adapterLuid;
    AdapterOptions flags;

internal:
    AdapterDescription1(const DXGI_ADAPTER_DESC1& adapterDescription)
    {
        VendorId = adapterDescription.VendorId;
        DeviceId = adapterDescription.DeviceId;
        SubSysId = adapterDescription.SubSysId;
        Revision = adapterDescription.Revision;
        DedicatedVideoMemory = static_cast<UInt32>(adapterDescription.DedicatedVideoMemory);
        DedicatedSystemMemory = static_cast<UInt32>(adapterDescription.DedicatedSystemMemory);
        AdapterLuid = Luid(adapterDescription.AdapterLuid);
        Description = gcnew String(adapterDescription.Description);
        // For Graphics 1.1
        Options = static_cast<AdapterOptions>(adapterDescription.Flags);
    }
public:

    static Boolean operator == (AdapterDescription1 adapterDescription1, AdapterDescription1 adapterDescription2)
    {
        return (adapterDescription1.description == adapterDescription2.description) &&
            (adapterDescription1.vendorId == adapterDescription2.vendorId) &&
            (adapterDescription1.deviceId == adapterDescription2.deviceId) &&
            (adapterDescription1.subSysId == adapterDescription2.subSysId) &&
            (adapterDescription1.revision == adapterDescription2.revision) &&
            (adapterDescription1.dedicatedVideoMemory == adapterDescription2.dedicatedVideoMemory) &&
            (adapterDescription1.dedicatedSystemMemory == adapterDescription2.dedicatedSystemMemory) &&
            (adapterDescription1.sharedSystemMemory == adapterDescription2.sharedSystemMemory) &&
            (adapterDescription1.adapterLuid == adapterDescription2.adapterLuid) &&
            (adapterDescription1.flags == adapterDescription2.flags);
    }

    static Boolean operator != (AdapterDescription1 adapterDescription1, AdapterDescription1 adapterDescription2)
    {
        return !(adapterDescription1 == adapterDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != AdapterDescription1::typeid)
        {
            return false;
        }

        return *this == safe_cast<AdapterDescription1>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + description->GetHashCode();
        hashCode = hashCode * 31 + vendorId.GetHashCode();
        hashCode = hashCode * 31 + deviceId.GetHashCode();
        hashCode = hashCode * 31 + subSysId.GetHashCode();
        hashCode = hashCode * 31 + revision.GetHashCode();
        hashCode = hashCode * 31 + dedicatedVideoMemory.GetHashCode();
        hashCode = hashCode * 31 + dedicatedSystemMemory.GetHashCode();
        hashCode = hashCode * 31 + sharedSystemMemory.GetHashCode();
        hashCode = hashCode * 31 + adapterLuid.GetHashCode();
        hashCode = hashCode * 31 + flags.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// Describes timing and presentation statistics for a frame.
/// <para>(Also see DirectX SDK: DXGI_FRAME_STATISTICS)</para>
/// </summary>
public value struct FrameStatistics 
{
public:
    /// <summary>
    /// A value representing the running total count of times that an image has been presented to the monitor since the computer booted. Note that the number of times that an image has been presented to the monitor is not necessarily the same as the number of times that SwapChain.Present has been called.
    /// <para>(Also see DirectX SDK: DXGI_FRAME_STATISTICS.PresentCount)</para>
    /// </summary>
    property UInt32 PresentCount
    {
        UInt32 get()
        {
            return presentCount;
        }

        void set(UInt32 value)
        {
            presentCount = value;
        }
    }
    /// <summary>
    /// A value representing the running total count of v-blanks that have happened since the computer booted.
    /// <para>(Also see DirectX SDK: DXGI_FRAME_STATISTICS.PresentRefreshCount)</para>
    /// </summary>
    property UInt32 PresentRefreshCount
    {
        UInt32 get()
        {
            return presentRefreshCount;
        }

        void set(UInt32 value)
        {
            presentRefreshCount = value;
        }
    }
    /// <summary>
    /// A value representing the running total count of v-blanks that have happened since the computer booted.
    /// <para>(Also see DirectX SDK: DXGI_FRAME_STATISTICS.SyncRefreshCount)</para>
    /// </summary>
    property UInt32 SyncRefreshCount
    {
        UInt32 get()
        {
            return syncRefreshCount;
        }

        void set(UInt32 value)
        {
            syncRefreshCount = value;
        }
    }
    /// <summary>
    /// A value representing the high-resolution performance counter timer. 
    /// This value is that same as the value returned by the QueryPerformanceCounter function.
    /// <para>(Also see DirectX SDK: DXGI_FRAME_STATISTICS.SyncQPCTime)</para>
    /// </summary>
    property Int64 SyncQueryPerformanceCounterTime
    {
        Int64 get()
        {
            return syncQueryPerformanceCounterTime;
        }

        void set(Int64 value)
        {
            syncQueryPerformanceCounterTime = value;
        }
    }
    /// <summary>
    /// Reserved. Always returns 0.
    /// <para>(Also see DirectX SDK: DXGI_FRAME_STATISTICS.SyncGPUTime)</para>
    /// </summary>
    property Int64 SyncGpuTime
    {
        Int64 get()
        {
            return syncGPUTime;
        }

        void set(Int64 value)
        {
            syncGPUTime = value;
        }
    }

private:

    UInt32 presentCount;
    UInt32 presentRefreshCount;
    UInt32 syncRefreshCount;
    Int64 syncQueryPerformanceCounterTime;
    Int64 syncGPUTime;

internal:
    FrameStatistics(const DXGI_FRAME_STATISTICS & frameStatistics)
    {
        PresentCount = frameStatistics.PresentCount;
        PresentRefreshCount = frameStatistics.PresentRefreshCount;
        SyncRefreshCount = frameStatistics.SyncRefreshCount;
        SyncQueryPerformanceCounterTime = frameStatistics.SyncQPCTime.QuadPart;
        SyncGpuTime = frameStatistics.SyncGPUTime.QuadPart;
    }
public:

    static Boolean operator == (FrameStatistics frameStatistics1, FrameStatistics frameStatistics2)
    {
        return (frameStatistics1.presentCount == frameStatistics2.presentCount) &&
            (frameStatistics1.presentRefreshCount == frameStatistics2.presentRefreshCount) &&
            (frameStatistics1.syncRefreshCount == frameStatistics2.syncRefreshCount) &&
            (frameStatistics1.syncQueryPerformanceCounterTime == frameStatistics2.syncQueryPerformanceCounterTime) &&
            (frameStatistics1.syncGPUTime == frameStatistics2.syncGPUTime);
    }

    static Boolean operator != (FrameStatistics frameStatistics1, FrameStatistics frameStatistics2)
    {
        return !(frameStatistics1 == frameStatistics2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != FrameStatistics::typeid)
        {
            return false;
        }

        return *this == safe_cast<FrameStatistics>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + presentCount.GetHashCode();
        hashCode = hashCode * 31 + presentRefreshCount.GetHashCode();
        hashCode = hashCode * 31 + syncRefreshCount.GetHashCode();
        hashCode = hashCode * 31 + syncQueryPerformanceCounterTime.GetHashCode();
        hashCode = hashCode * 31 + syncGPUTime.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Controls the settings of a gamma curve.
/// <para>(Also see DirectX SDK: DXGI_GAMMA_CONTROL)</para>
/// </summary>
public value struct GammaControl 
{
public:
    /// <summary>
    /// A ColorRGB structure with scalar values that are applied to rgb values before being sent to the gamma look up table.
    /// <para>(Also see DirectX SDK: DXGI_GAMMA_CONTROL.Scale)</para>
    /// </summary>
    property ColorRgb Scale
    {
        ColorRgb get()
        {
            return scale;
        }

        void set(ColorRgb value)
        {
            scale = value;
        }
    }
    /// <summary>
    /// A ColorRGB structure with offset values that are applied to the rgb values before being sent to the gamma look up table.
    /// <para>(Also see DirectX SDK: DXGI_GAMMA_CONTROL.offset)</para>
    /// </summary>
    property ColorRgb Offset
    {
        ColorRgb get()
        {
            return offset;
        }

        void set(ColorRgb value)
        {
            offset = value;
        }
    }
    /// <summary>
    /// A collection of ColorRGB structures that control the points of a gamma curve.
    /// <para>(Also see DirectX SDK: DXGI_GAMMA_CONTROL.GammaCurve)</para>
    /// </summary>
    property IEnumerable<ColorRgb>^ GammaCurve
    {
        IEnumerable<ColorRgb>^ get()
        {
            if (gammaCurveArray == nullptr)
            {
                gammaCurveArray = gcnew array<ColorRgb>(GammaCurveArrayLength);
            }
            return Array::AsReadOnly(gammaCurveArray);
        }
    }
private:

    ColorRgb scale;
    ColorRgb offset;

internal:
    literal int GammaCurveArrayLength = 1025;
    GammaControl(const DXGI_GAMMA_CONTROL & gammaControl)
    {
        Scale = ColorRgb(gammaControl.Scale);
        Offset = ColorRgb(gammaControl.Offset);
        gammaCurveArray = gcnew array<ColorRgb>(GammaCurveArrayLength); 

        pin_ptr<ColorRgb> gammaCurveArrayPtr = &gammaCurveArray[0];
        memcpy(gammaCurveArrayPtr, gammaControl.GammaCurve, sizeof (DXGI_RGB) * GammaCurveArrayLength);
    }

    void CopyTo(DXGI_GAMMA_CONTROL * gammaControl)
    {
        Scale.CopyTo(&gammaControl->Scale);
        Offset.CopyTo(&gammaControl->Offset);

        if (gammaCurveArray != nullptr)
        {
            pin_ptr<ColorRgb> gammaCurveArrayPtr = &gammaCurveArray[0];
            memcpy(gammaControl->GammaCurve, gammaCurveArrayPtr, sizeof (DXGI_RGB) * GammaCurveArrayLength);
        }
        else
        {
            ZeroMemory(gammaControl->GammaCurve, sizeof (DXGI_RGB) * GammaCurveArrayLength);
        }
    }

private:
    array<ColorRgb>^ gammaCurveArray;

};

/// <summary>
/// Controls the gamma capabilities of an adapter.
/// <para>(Also see DirectX SDK: DXGI_GAMMA_CONTROL_CAPABILITIES)</para>
/// </summary>
public value struct GammaControlCapabilities 
{
public:
    /// <summary>
    /// True if scaling and offset operations are supported during gamma correction; otherwise, false.
    /// <para>(Also see DirectX SDK: DXGI_GAMMA_CONTROL_CAPABILITIES.ScaleAndOffsetSupported)</para>
    /// </summary>
    property Boolean ScaleAndOffsetSupported
    {
        Boolean get()
        {
            return scaleAndOffsetSupported;
        }

        void set(Boolean value)
        {
            scaleAndOffsetSupported = value;
        }
    }

    /// <summary>
    /// A value describing the maximum range of the control-point positions.
    /// <para>(Also see DirectX SDK: DXGI_GAMMA_CONTROL_CAPABILITIES.MaxConvertedValue)</para>
    /// </summary>
    property Single MaxConvertedValue
    {
        Single get()
        {
            return maxConvertedValue;
        }

        void set(Single value)
        {
            maxConvertedValue = value;
        }
    }

    /// <summary>
    /// A value describing the minimum range of the control-point positions.
    /// <para>(Also see DirectX SDK: DXGI_GAMMA_CONTROL_CAPABILITIES.MinConvertedValue)</para>
    /// </summary>
    property Single MinConvertedValue
    {
        Single get()
        {
            return minConvertedValue;
        }

        void set(Single value)
        {
            minConvertedValue = value;
        }
    }

    /// <summary>
    /// A value describing the number of control points in the array.
    /// <para>(Also see DirectX SDK: DXGI_GAMMA_CONTROL_CAPABILITIES.NumGammaControlPoints)</para>
    /// </summary>
    property UInt32 GammaControlPointCount
    {
        UInt32 get()
        {
            return numGammaControlPoints;
        }

        void set(UInt32 value)
        {
            numGammaControlPoints = value;
        }
    }

    /// <summary>
    /// A collection of values describing control points; the maximum length of control points is 1025.
    /// <para>(Also see DirectX SDK: DXGI_GAMMA_CONTROL_CAPABILITIES.ControlPointPositions)</para>
    /// </summary>
    property IEnumerable<Single>^ ControlPointPositions
    {
        IEnumerable<Single>^ get()
        {
            if (controlPointsPositions == nullptr)
            {
                controlPointsPositions = gcnew array<Single>(ControlPointPositionsArrayLength);
            }
            return Array::AsReadOnly(controlPointsPositions);
        }
    }
private:

    Boolean scaleAndOffsetSupported;
    Single maxConvertedValue;
    Single minConvertedValue;
    UInt32 numGammaControlPoints;

internal:
    literal int ControlPointPositionsArrayLength = 1025;
    GammaControlCapabilities(const DXGI_GAMMA_CONTROL_CAPABILITIES & gammaControlCapabilities)
    {
        MaxConvertedValue = gammaControlCapabilities.MaxConvertedValue;
        MinConvertedValue = gammaControlCapabilities.MinConvertedValue;
        GammaControlPointCount = gammaControlCapabilities.NumGammaControlPoints;
        ScaleAndOffsetSupported = gammaControlCapabilities.ScaleAndOffsetSupported != 0;
        
        controlPointsPositions = gcnew array<Single>(ControlPointPositionsArrayLength); 

        pin_ptr<Single> controlPointsPositionsPtr = &controlPointsPositions[0];
        memcpy(controlPointsPositionsPtr, gammaControlCapabilities.ControlPointPositions, sizeof (FLOAT) * ControlPointPositionsArrayLength);
    }

private:
    array<Single>^ controlPointsPositions;

};

/// <summary>
/// A mapped rectangle used for accessing a surface.
/// <para>(Also see DirectX SDK: DXGI_MAPPED_RECT)</para>
/// </summary>
public value struct MappedRect 
{
public:
    /// <summary>
    /// A value describing the width of the surface.
    /// <para>(Also see DirectX SDK: DXGI_MAPPED_RECT.Pitch)</para>
    /// </summary>
    property Int32 Pitch
    {
        Int32 get()
        {
            return pitch;
        }

        void set(Int32 value)
        {
            pitch = value;
        }
    }
    /// <summary>
    /// The image buffer of the surface.
    /// <para>(Also see DirectX SDK: DXGI_MAPPED_RECT.pBits)</para>
    /// </summary>
    property IntPtr Bits
    {
        IntPtr get()
        {
            return bits;
        }

        void set(IntPtr value)
        {
            bits = value;
        }
    }

private:

    Int32 pitch;
    IntPtr bits;

internal:
    MappedRect(const DXGI_MAPPED_RECT& mappedRect)
    {
        Pitch = mappedRect.Pitch;
        Bits = IntPtr(mappedRect.pBits);
    }
};

/// <summary>
/// Describes a display mode.
/// <para>(Also see DirectX SDK: DXGI_MODE_DESC)</para>
/// </summary>
public value struct ModeDescription 
{
public:
    /// <summary>
    /// A value describing the resolution width.
    /// <para>(Also see DirectX SDK: DXGI_MODE_DESC.Width)</para>
    /// </summary>
    property UInt32 Width
    {
        UInt32 get()
        {
            return width;
        }

        void set(UInt32 value)
        {
            width = value;
        }
    }
    /// <summary>
    /// A value describing the resolution height.
    /// <para>(Also see DirectX SDK: DXGI_MODE_DESC.Height)</para>
    /// </summary>
    property UInt32 Height
    {
        UInt32 get()
        {
            return height;
        }

        void set(UInt32 value)
        {
            height = value;
        }
    }
    /// <summary>
    /// A Rational structure describing the refresh rate in hertz
    /// <para>(Also see DirectX SDK: DXGI_MODE_DESC.RefreshRate)</para>
    /// </summary>
    property Rational RefreshRate
    {
        Rational get()
        {
            return refreshRate;
        }

        void set(Rational value)
        {
            refreshRate = value;
        }
    }
    /// <summary>
    /// A Format structure describing the display format.
    /// <para>(Also see DirectX SDK: DXGI_MODE_DESC.Format)</para>
    /// </summary>
    property Graphics::Format Format
    {
        Graphics::Format get()
        {
            return format;
        }

        void set(Graphics::Format value)
        {
            format = value;
        }
    }
    /// <summary>
    /// A member of the ModeScanlineOrder enumerated type describing the scanline drawing mode.
    /// <para>(Also see DirectX SDK: DXGI_MODE_DESC.ScanlineOrdering)</para>
    /// </summary>
    property ModeScanlineOrder ScanlineOrdering
    {
        ModeScanlineOrder get()
        {
            return scanlineOrdering;
        }

        void set(ModeScanlineOrder value)
        {
            scanlineOrdering = value;
        }
    }
    /// <summary>
    /// A member of the ModeScaling enumerated type describing the scaling mode.
    /// <para>(Also see DirectX SDK: DXGI_MODE_DESC.Scaling)</para>
    /// </summary>
    property ModeScaling Scaling
    {
        ModeScaling get()
        {
            return scaling;
        }

        void set(ModeScaling value)
        {
            scaling = value;
        }
    }

private:

    UInt32 width;
    UInt32 height;
    Rational refreshRate;
    Graphics::Format format;
    ModeScanlineOrder scanlineOrdering;
    ModeScaling scaling;

internal:
    ModeDescription(const DXGI_MODE_DESC& modeDescription)
    {
        Width = modeDescription.Width;
        Height = modeDescription.Height;
        RefreshRate = Rational(modeDescription.RefreshRate);
        Format = static_cast<Graphics::Format>(modeDescription.Format);
        ScanlineOrdering = static_cast<ModeScanlineOrder>(modeDescription.ScanlineOrdering);
        Scaling = static_cast<ModeScaling>(modeDescription.Scaling);
    }

    void CopyTo(DXGI_MODE_DESC* modeDescription)
    {
        modeDescription->Width = Width;
        modeDescription->Height = Height;
        // REVIEW: would be convenient to have a CopyTo() method here
        modeDescription->RefreshRate.Numerator = RefreshRate.Numerator;
        modeDescription->RefreshRate.Denominator = RefreshRate.Denominator;
        modeDescription->Format = static_cast<DXGI_FORMAT>(Format);
        modeDescription->ScanlineOrdering = static_cast<DXGI_MODE_SCANLINE_ORDER>(ScanlineOrdering);
        modeDescription->Scaling = static_cast<DXGI_MODE_SCALING>(Scaling);
    }
public:

    ModeDescription(UInt32 width, UInt32 height, Graphics::Format format, Rational refreshRate)
    {
        Width = width;
        Height = height;
        Format = format;
        RefreshRate = refreshRate;
        ScanlineOrdering = ModeScanlineOrder::Unspecified;
        Scaling = ModeScaling::Unspecified;
    }

    ModeDescription(UInt32 width, UInt32 height, Graphics::Format format,
        Rational refreshRate, ModeScanlineOrder scanlineOrder, ModeScaling scaling)
    {
        Width = width;
        Height = height;
        Format = format;
        RefreshRate = refreshRate;
        ScanlineOrdering = scanlineOrder;
        Scaling = scaling;
    }

    static Boolean operator == (ModeDescription modeDescription1, ModeDescription modeDescription2)
    {
        return (modeDescription1.width == modeDescription2.width) &&
            (modeDescription1.height == modeDescription2.height) &&
            (modeDescription1.refreshRate == modeDescription2.refreshRate) &&
            (modeDescription1.format == modeDescription2.format) &&
            (modeDescription1.scanlineOrdering == modeDescription2.scanlineOrdering) &&
            (modeDescription1.scaling == modeDescription2.scaling);
    }

    static Boolean operator != (ModeDescription modeDescription1, ModeDescription modeDescription2)
    {
        return !(modeDescription1 == modeDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != ModeDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<ModeDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + width.GetHashCode();
        hashCode = hashCode * 31 + height.GetHashCode();
        hashCode = hashCode * 31 + refreshRate.GetHashCode();
        hashCode = hashCode * 31 + format.GetHashCode();
        hashCode = hashCode * 31 + scanlineOrdering.GetHashCode();
        hashCode = hashCode * 31 + scaling.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes an output or physical connection between the adapter (video card) and a device.
/// <para>(Also see DirectX SDK: DXGI_OUTPUT_DESC)</para>
/// </summary>
public value struct OutputDescription 
{
public:
    /// <summary>
    /// A string that contains the name of the output device.
    /// <para>(Also see DirectX SDK: DXGI_OUTPUT_DESC.DeviceName)</para>
    /// </summary>
    property String^ DeviceName
    {
        String^ get()
        {
            return deviceName;
        }

        void set(String^ value)
        {
            deviceName = value;
        }
    }
    /// <summary>
    /// A RECT structure containing the bounds of the output in desktop coordinates.
    /// <para>(Also see DirectX SDK: DXGI_OUTPUT_DESC.DesktopCoordinates)</para>
    /// </summary>
    property D3DRect DesktopCoordinates
    {
        D3DRect get()
        {
            return desktopCoordinates;
        }

        void set(D3DRect value)
        {
            desktopCoordinates = value;
        }
    }
    /// <summary>
    /// True if the output is attached to the desktop; otherwise, false.
    /// <para>(Also see DirectX SDK: DXGI_OUTPUT_DESC.AttachedToDesktop)</para>
    /// </summary>
    property Boolean AttachedToDesktop
    {
        Boolean get()
        {
            return attachedToDesktop;
        }

        void set(Boolean value)
        {
            attachedToDesktop = value;
        }
    }
    /// <summary>
    /// A member of the ModeRotation enumerated type describing on how an image is rotated by the output.
    /// <para>(Also see DirectX SDK: DXGI_OUTPUT_DESC.Rotation)</para>
    /// </summary>
    property ModeRotation Rotation
    {
        ModeRotation get()
        {
            return rotation;
        }

        void set(ModeRotation value)
        {
            rotation = value;
        }
    }
    /// <summary>
    /// An HMONITOR handle that represents the display monitor. For more information, see HMONITOR and the Device Context.
    /// <para>(Also see DirectX SDK: DXGI_OUTPUT_DESC.Monitor)</para>
    /// </summary>
    property MonitorInfo Monitor
    {
        MonitorInfo get()
        {
            return monitor;
        }

        void set(MonitorInfo value)
        {
            monitor = value;
        }
    }
private:

    String^ deviceName;
    D3DRect desktopCoordinates;
    Boolean attachedToDesktop;
    ModeRotation rotation;
    MonitorInfo monitor;

internal:
    OutputDescription(const DXGI_OUTPUT_DESC& outputDescription)
    {
        DeviceName = gcnew String(outputDescription.DeviceName);
        DesktopCoordinates = D3DRect(outputDescription.DesktopCoordinates);
        AttachedToDesktop = outputDescription.AttachedToDesktop != 0;
        Rotation = static_cast<ModeRotation>(outputDescription.Rotation);
        Monitor = MonitorInfo(outputDescription.Monitor);
    }
};

/// <summary>
/// Describes multi-sampling parameters for a resource.
/// <para>(Also see DirectX SDK: DXGI_SAMPLE_DESC)</para>
/// </summary>
public value struct SampleDescription
{
public:
    /// <summary>
    /// The number of multisamples per pixel.
    /// <para>(Also see DirectX SDK: DXGI_SAMPLE_DESC.Count)</para>
    /// </summary>
    property UInt32 Count
    {
        UInt32 get()
        {
            return count;
        }

        void set(UInt32 value)
        {
            count = value;
        }
    }
    /// <summary>
    /// The image quality level. The higher the quality, the lower the performance. The valid range is between zero and one less than the level returned         by Device.CheckMultisampleQualityLevels.
    /// <para>(Also see DirectX SDK: DXGI_SAMPLE_DESC.Quality)</para>
    /// </summary>
    property UInt32 Quality
    {
        UInt32 get()
        {
            return quality;
        }

        void set(UInt32 value)
        {
            quality = value;
        }
    }
private:

    UInt32 count;
    UInt32 quality;

internal:

    SampleDescription(const DXGI_SAMPLE_DESC& sampleDescription)
    {
        Count = sampleDescription.Count;
        Quality = sampleDescription.Quality;
    }

    void CopyTo(DXGI_SAMPLE_DESC &desc)
    {
        desc.Count = Count;
        desc.Quality = Quality;
    }

public:

    SampleDescription(UInt32 count, UInt32 quality)
    {
        Count = count;
        Quality = quality;
    }

    static Boolean operator == (SampleDescription sampleDescription1, SampleDescription sampleDescription2)
    {
        return (sampleDescription1.count == sampleDescription2.count) &&
            (sampleDescription1.quality == sampleDescription2.quality);
    }

    static Boolean operator != (SampleDescription sampleDescription1, SampleDescription sampleDescription2)
    {
        return !(sampleDescription1 == sampleDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != SampleDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<SampleDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + count.GetHashCode();
        hashCode = hashCode * 31 + quality.GetHashCode();

        return hashCode;
    }

};

// REVIEW: how is this ever used?

/// <summary>
/// Represents a handle to a shared resource.
/// <para>(Also see DirectX SDK: DXGI_SHARED_RESOURCE)</para>
/// </summary>
public value struct SharedResource 
{
public:
    /// <summary>
    /// A handle to a shared resource.
    /// <para>(Also see DirectX SDK: DXGI_SHARED_RESOURCE.Handle)</para>
    /// </summary>
    property IntPtr Handle
    {
        IntPtr get()
        {
            return handle;
        }

        void set(IntPtr value)
        {
            handle = value;
        }
    }
private:

    IntPtr handle;

internal:
    CA_SUPPRESS_MESSAGE("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")
    SharedResource(const DXGI_SHARED_RESOURCE & sharedResource)
    {
        Handle = IntPtr(sharedResource.Handle);
    }
};

/// <summary>
/// Describes a surface.
/// <para>(Also see DirectX SDK: DXGI_SURFACE_DESC)</para>
/// </summary>
public value struct SurfaceDescription 
{
public:
    /// <summary>
    /// A value describing the surface width.
    /// <para>(Also see DirectX SDK: DXGI_SURFACE_DESC.Width)</para>
    /// </summary>
    property UInt32 Width
    {
        UInt32 get()
        {
            return width;
        }

        void set(UInt32 value)
        {
            width = value;
        }
    }
    /// <summary>
    /// A value describing the surface height.
    /// <para>(Also see DirectX SDK: DXGI_SURFACE_DESC.Height)</para>
    /// </summary>
    property UInt32 Height
    {
        UInt32 get()
        {
            return height;
        }

        void set(UInt32 value)
        {
            height = value;
        }
    }
    /// <summary>
    /// A member of the Format enumerated type that describes the surface format.
    /// <para>(Also see DirectX SDK: DXGI_SURFACE_DESC.Format)</para>
    /// </summary>
    property Graphics::Format Format
    {
        Graphics::Format get()
        {
            return format;
        }

        void set(Graphics::Format value)
        {
            format = value;
        }
    }
    /// <summary>
    /// A member of the SampleDescription structure that describes multi-sampling parameters for the surface.
    /// <para>(Also see DirectX SDK: DXGI_SURFACE_DESC.SampleDesc)</para>
    /// </summary>
    property Graphics::SampleDescription SampleDescription
    {
        Graphics::SampleDescription get()
        {
            return sampleDescription;
        }

        void set(Graphics::SampleDescription value)
        {
            sampleDescription = value;
        }
    }

private:

    UInt32 width;
    UInt32 height;
    Graphics::Format format;
    Graphics::SampleDescription sampleDescription;

internal:
    SurfaceDescription(const DXGI_SURFACE_DESC & surfaceDescription)
    {
        Width = surfaceDescription.Width;
        Height = surfaceDescription.Height;
        Format = static_cast<Graphics::Format>(surfaceDescription.Format);
        SampleDescription = Graphics::SampleDescription(surfaceDescription.SampleDesc);
    }
public:

    static Boolean operator == (SurfaceDescription surfaceDescription1, SurfaceDescription surfaceDescription2)
    {
        return (surfaceDescription1.width == surfaceDescription2.width) &&
            (surfaceDescription1.height == surfaceDescription2.height) &&
            (surfaceDescription1.format == surfaceDescription2.format) &&
            (surfaceDescription1.sampleDescription == surfaceDescription2.sampleDescription);
    }

    static Boolean operator != (SurfaceDescription surfaceDescription1, SurfaceDescription surfaceDescription2)
    {
        return !(surfaceDescription1 == surfaceDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != SurfaceDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<SurfaceDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + width.GetHashCode();
        hashCode = hashCode * 31 + height.GetHashCode();
        hashCode = hashCode * 31 + format.GetHashCode();
        hashCode = hashCode * 31 + sampleDescription.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a swap chain.
/// <para>(Also see DirectX SDK: DXGI_SWAP_CHAIN_DESC)</para>
/// </summary>
public value struct SwapChainDescription 
{
public:
    /// <summary>
    /// A ModeDescription structure describing the backbuffer display mode.
    /// <para>(Also see DirectX SDK: DXGI_SWAP_CHAIN_DESC.BufferDesc)</para>
    /// </summary>
    property ModeDescription BufferDescription
    {
        ModeDescription get()
        {
            return bufferDescription;
        }

        void set(ModeDescription value)
        {
            bufferDescription = value;
        }
    }
    /// <summary>
    /// A SampleDescription structure describing multi-sampling parameters.
    /// <para>(Also see DirectX SDK: DXGI_SWAP_CHAIN_DESC.SampleDesc)</para>
    /// </summary>
    property Graphics::SampleDescription SampleDescription
    {
        Graphics::SampleDescription get()
        {
            return sampleDescription;
        }

        void set(Graphics::SampleDescription value)
        {
            sampleDescription = value;
        }
    }
    /// <summary>
    /// A member of the UsageOptions enumerated type describing the surface usage and CPU access options for the back buffer. The back buffer can be used for shader input or render-target output.
    /// <para>(Also see DirectX SDK: DXGI_SWAP_CHAIN_DESC.BufferUsage)</para>
    /// </summary>
    property UsageOptions BufferUsage
    {
        UsageOptions get()
        {
            return bufferUsage;
        }

        void set(UsageOptions value)
        {
            bufferUsage = value;
        }
    }
    /// <summary>
    /// A value that describes the number of buffers in the swap chain, including the front buffer.
    /// <para>(Also see DirectX SDK: DXGI_SWAP_CHAIN_DESC.BufferCount)</para>
    /// </summary>
    property UInt32 BufferCount
    {
        UInt32 get()
        {
            return bufferCount;
        }

        void set(UInt32 value)
        {
            bufferCount = value;
        }
    }
    /// <summary>
    /// An HWND handle to the output window. This member must a valid window handle.
    /// <para>(Also see DirectX SDK: DXGI_SWAP_CHAIN_DESC.OutputWindow)</para>
    /// </summary>
    property IntPtr OutputWindowHandle
    {
        IntPtr get()
        {
            return outputWindowHandle;
        }

        void set(IntPtr value)
        {
            outputWindowHandle = value;
        }
    }
    /// <summary>
    /// True if the output is in windowed mode; otherwise, false. For more information, see Factory.CreateSwapChain.
    /// <para>(Also see DirectX SDK: DXGI_SWAP_CHAIN_DESC.Windowed)</para>
    /// </summary>
    property Boolean Windowed
    {
        Boolean get()
        {
            return windowed;
        }

        void set(Boolean value)
        {
            windowed = value;
        }
    }
    /// <summary>
    /// A member of the SwapEffect enumerated type that describes options for handling the contents of the presentation buffer after         presenting a surface.
    /// <para>(Also see DirectX SDK: DXGI_SWAP_CHAIN_DESC.SwapEffect)</para>
    /// </summary>
    property Graphics::SwapEffect SwapEffect
    {
        Graphics::SwapEffect get()
        {
            return swapEffect;
        }

        void set(Graphics::SwapEffect value)
        {
            swapEffect = value;
        }
    }
    /// <summary>
    /// A member of the SwapChainOptions enumerated type that describes options for swap-chain behavior.
    /// <para>(Also see DirectX SDK: DXGI_SWAP_CHAIN_DESC.Flags)</para>
    /// </summary>
    property SwapChainOptions Options
    {
        SwapChainOptions get()
        {
            return flags;
        }

        void set(SwapChainOptions value)
        {
            flags = value;
        }
    }

private:

    ModeDescription bufferDescription;
    Graphics::SampleDescription sampleDescription;
    UsageOptions bufferUsage;
    UInt32 bufferCount;
    IntPtr outputWindowHandle;
    Boolean windowed;
    Graphics::SwapEffect swapEffect;
    SwapChainOptions flags;

internal:
    SwapChainDescription(const DXGI_SWAP_CHAIN_DESC& swapChainDescription)
    {
        BufferDescription = ModeDescription(swapChainDescription.BufferDesc);
        SampleDescription = Graphics::SampleDescription(swapChainDescription.SampleDesc);
        BufferUsage = static_cast<UsageOptions>(swapChainDescription.BufferUsage);
        OutputWindowHandle = IntPtr(swapChainDescription.OutputWindow);
        Windowed = swapChainDescription.Windowed != 0;
        SwapEffect = static_cast<Graphics::SwapEffect>(swapChainDescription.SwapEffect);
        Options = static_cast<SwapChainOptions>(swapChainDescription.Flags);
        BufferCount = swapChainDescription.BufferCount;
    }

    void CopyTo(DXGI_SWAP_CHAIN_DESC* pSwapChainDescription)
    {
        pSwapChainDescription->BufferDesc.Format = static_cast<DXGI_FORMAT>(BufferDescription.Format);
        pSwapChainDescription->BufferDesc.Height = BufferDescription.Height;
        pSwapChainDescription->BufferDesc.Width = BufferDescription.Width;
        pSwapChainDescription->BufferDesc.RefreshRate.Denominator  = BufferDescription.RefreshRate.Denominator;
        pSwapChainDescription->BufferDesc.RefreshRate.Numerator = BufferDescription.RefreshRate.Numerator;
        pSwapChainDescription->BufferDesc.Scaling = static_cast<DXGI_MODE_SCALING>(BufferDescription.Scaling);
        pSwapChainDescription->BufferDesc.ScanlineOrdering = static_cast<DXGI_MODE_SCANLINE_ORDER>(BufferDescription.ScanlineOrdering);

        pSwapChainDescription->SampleDesc.Count = SampleDescription.Count;
        pSwapChainDescription->SampleDesc.Quality = SampleDescription.Quality;
        
        pSwapChainDescription->BufferUsage = static_cast<DXGI_USAGE>(BufferUsage);
        pSwapChainDescription->BufferCount = BufferCount;
        pSwapChainDescription->OutputWindow = static_cast<HWND>(OutputWindowHandle.ToPointer());
        pSwapChainDescription->Windowed = Windowed ? 1 : 0;
        pSwapChainDescription->SwapEffect = static_cast<DXGI_SWAP_EFFECT>(SwapEffect);
        pSwapChainDescription->Flags = static_cast<UINT>(Options);
    }
};

public value class AdapterDriverVersion
{
public:

    property Int32 Major
    {
        Int32 get(void) { return major; }
    }

    property Int32 Minor
    {
        Int32 get(void) { return minor; }
    }

    static bool operator ==(AdapterDriverVersion version1, AdapterDriverVersion version2)
    {
        return version1.major == version2.major &&
            version1.minor == version2.minor;
    }

    static bool operator !=(AdapterDriverVersion version1, AdapterDriverVersion version2)
    {
        return !(version1 == version2);
    }

    virtual bool Equals(Object^ obj) override
    {
        if (obj->GetType() != AdapterDriverVersion::typeid)
        {
            return false;
        }

        return *this == safe_cast<AdapterDriverVersion>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + major.GetHashCode();
        hashCode = hashCode * 31 + minor.GetHashCode();

        return hashCode;
    }

internal:

    AdapterDriverVersion(Int32 major, Int32 minor)
        : major(major), minor(minor)
    { }

private:

    Int32 major;
    Int32 minor;
};

} } } }
