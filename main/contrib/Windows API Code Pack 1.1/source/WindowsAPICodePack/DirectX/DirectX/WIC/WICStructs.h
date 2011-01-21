//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent {

using namespace System;

/// <summary>
/// Describes size of the bitmap.
/// </summary>
public value struct BitmapSize
{
public:
    /// <summary>
    /// The width of the bitmap in pixels.
    /// </summary>
    property unsigned int Width
    {
        unsigned int get()
        {
            return width;
        }

        void set(unsigned int value)
        {
            width = value;
        }
    }

    /// <summary>
    /// The height of the bitmap in pixels.
    /// </summary>
    property unsigned int Height
    {
        unsigned int get()
        {
            return height;
        }

        void set(unsigned int value)
        {
            height = value;
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    BitmapSize( unsigned int width, unsigned int height )
    {
        Width = width;
        Height = height;
    }

private:

    unsigned int width;
    unsigned int height;

public:

    static Boolean operator == (BitmapSize bitmapSize1, BitmapSize bitmapSize2)
    {
        return (bitmapSize1.width == bitmapSize2.width) &&
            (bitmapSize1.height == bitmapSize2.height);
    }

    static Boolean operator != (BitmapSize bitmapSize1, BitmapSize bitmapSize2)
    {
        return !(bitmapSize1 == bitmapSize2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != BitmapSize::typeid)
        {
            return false;
        }

        return *this == safe_cast<BitmapSize>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + width.GetHashCode();
        hashCode = hashCode * 31 + height.GetHashCode();

        return hashCode;
    }

};



/// <summary>
/// Describes dots per inch resolution of the bitmap.
/// </summary>
public value struct BitmapResolution
{
public:
    /// <summary>
    /// The horizontal resolution of the bitmap.
    /// </summary>
    property double DpiX
    {
        double get()
        {
            return dpiX;
        }

        void set(double value)
        {
            dpiX = value;
        }
    }

    /// <summary>
    /// The vertical resolution of the bitmap.
    /// </summary>
    property double DpiY
    {
        double get()
        {
            return dpiY;
        }

        void set(double value)
        {
            dpiY = value;
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    BitmapResolution( double dpiX, double dpiY )
    {
        DpiX = dpiX;
        DpiY = dpiY;
    }
    
private:

    double dpiX;
    double dpiY;

public:

    static Boolean operator == (BitmapResolution bitmapResolution1, BitmapResolution bitmapResolution2)
    {
        return (bitmapResolution1.dpiX == bitmapResolution2.dpiX) &&
            (bitmapResolution1.dpiY == bitmapResolution2.dpiY);
    }

    static Boolean operator != (BitmapResolution bitmapResolution1, BitmapResolution bitmapResolution2)
    {
        return !(bitmapResolution1 == bitmapResolution2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != BitmapResolution::typeid)
        {
            return false;
        }

        return *this == safe_cast<BitmapResolution>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + dpiX.GetHashCode();
        hashCode = hashCode * 31 + dpiY.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// Describes a rectangle of bitmap data.
/// </summary>
public value struct BitmapRectangle
{
public:
    /// <summary>
    /// The horizontal component of the top left corner of the rectangle.
    /// </summary>
    property int X
    {
        int get()
        {
            return x;
        }

        void set(int value)
        {
            x = value;
        }
    }

    /// <summary>
    /// The vertical component of the top left corner of the rectangle.
    /// </summary>
    property int Y
    {
        int get()
        {
            return y;
        }

        void set(int value)
        {
            y = value;
        }
    }

    /// <summary>
    /// The width of the rectangle.
    /// </summary>
    property int Width
    {
        int get()
        {
            return width;
        }

        void set(int value)
        {
            width = value;
        }
    }

    /// <summary>
    /// The height of the rectangle.
    /// </summary>
    property int Height
    {
        int get()
        {
            return height;
        }

        void set(int value)
        {
            height = value;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    BitmapRectangle( int x, int y, int width, int height )
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

private:

    int x;
    int y;
    int width;
    int height;

public:

    static Boolean operator == (BitmapRectangle bitmapRectangle1, BitmapRectangle bitmapRectangle2)
    {
        return (bitmapRectangle1.x == bitmapRectangle2.x) &&
            (bitmapRectangle1.y == bitmapRectangle2.y) &&
            (bitmapRectangle1.width == bitmapRectangle2.width) &&
            (bitmapRectangle1.height == bitmapRectangle2.height);
    }

    static Boolean operator != (BitmapRectangle bitmapRectangle1, BitmapRectangle bitmapRectangle2)
    {
        return !(bitmapRectangle1 == bitmapRectangle2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != BitmapRectangle::typeid)
        {
            return false;
        }

        return *this == safe_cast<BitmapRectangle>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + x.GetHashCode();
        hashCode = hashCode * 31 + y.GetHashCode();
        hashCode = hashCode * 31 + width.GetHashCode();
        hashCode = hashCode * 31 + height.GetHashCode();

        return hashCode;
    }

};


} } } }
