// Copyright (c) Microsoft Corporation.  All rights reserved.
#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {

public value class PointAndTangent
{
private:

    Point2F point;
    Point2F tangent;

public:

    PointAndTangent(Point2F point, Point2F tangent)
        : point(point), tangent(tangent)
    { }

    property Point2F Point
    {
        Point2F get(void) { return point; }
    }

    property Point2F Tangent
    {
        Point2F get(void) { return tangent; }
    }

    static bool operator ==(PointAndTangent pointAndTangent1, PointAndTangent pointAndTangent2)
    {
        return pointAndTangent1.point == pointAndTangent2.point &&
            pointAndTangent1.tangent == pointAndTangent2.tangent;
    }

    static bool operator !=(PointAndTangent pointAndTangent1, PointAndTangent pointAndTangent2)
    {
        return !(pointAndTangent1 == pointAndTangent2);
    }

    virtual bool Equals(Object^ obj) override
    {
        if (obj->GetType() != PointAndTangent::typeid)
        {
            return false;
        }

        return *this == safe_cast<PointAndTangent>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + point.GetHashCode();
        hashCode = hashCode * 31 + tangent.GetHashCode();

        return hashCode;
    }
};

} } } }