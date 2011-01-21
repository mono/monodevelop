//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D {

	// REVIEW: for performance reasons, it might be better for the == and != overloads
	// to take by-ref parameters (e.g. "const Vector %vector"). In C++, this isn't so
	// bad because the parameters can be "const", advertising that they aren't actually
	// modified by the call.
	//
	// But, "const" is lost in the compiled code, so FxCop doesn't have any way to
	// detect that. And FxCop warns against by-reference parameters.
	//
	// For the smaller value types, the difference is probably negligible. But there is,
	// for example, stuff like the 4x4 matrix type.
	//
	// Alternatively, perhaps some of the larger types should be reference types.
	// Depends on how they are used; if they are passed in an array to DirectX, then
	// they're better off as value types. But if not, making them reference types
	// would be okay.

/// <summary>
/// This structure defines a 4 component float vector.
/// </summary>
[StructLayout(LayoutKind::Explicit)]
public value struct Vector4F
{
public:
    /// <summary>
    /// Specifies the first element of the vector.
    /// </summary>
    property float X
    {
        float get()
        {
            return x;
        }

        void set(float value)
        {
            x = value;
        }
    }

    /// <summary>
    /// Specifies the second element of the vector.
    /// </summary>
    property float Y
    {
        float get()
        {
            return y;
        }

        void set(float value)
        {
            y = value;
        }
    }

    /// <summary>
    /// Specifies the third element of the vector.
    /// </summary>
    property float Z
    {
        float get()
        {
            return z;
        }

        void set(float value)
        {
            z = value;
        }
    }

    /// <summary>
    /// Specifies the fourth element of the vector.
    /// </summary>
    property float W
    {
        float get()
        {
            return w;
        }

        void set(float value)
        {
            w = value;
        }
    }


private:

    [FieldOffset(0)]
    float x;
    [FieldOffset(4)]
    float y;
    [FieldOffset(8)]
    float z;
    [FieldOffset(12)]
    float w;

public:
    /// <summary>
    /// Initializes the vector from a set of values.
    /// </summary>
    /// <param name="x">Specifies value of x.</param>
    /// <param name="y">Specifies value of y.</param>
    /// <param name="z">Specifies value of z.</param>
    /// <param name="w">Specifies value of w.</param>
    Vector4F(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Initializes the vector from an array.
    /// </summary>
    /// <param name="dataSource">The vector data as an array of floats.</param>
    Vector4F(array<float>^ dataSource)
    {
        if(dataSource->Length != 4)
        {
            throw gcnew ArgumentException("Invalid array length", "dataSource");
        }

        this->X = dataSource[0];
        this->Y = dataSource[1];
        this->Z = dataSource[2];
        this->W = dataSource[3];
    }

    /// <summary>
    /// The vector equality operator.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if the vectors are equal.</returns>
    static Boolean operator ==(Vector4F vector1, Vector4F vector2 )
    {
        return 
            (vector1.X == vector2.X) && 
            (vector1.Y == vector2.Y) && 
            (vector1.Z == vector2.Z) && 
            (vector1.W == vector2.W);
    }

    /// <summary>
    /// The vector inequality operator.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if the vectors are not equal.</returns>
    static Boolean operator !=( Vector4F vector1, Vector4F vector2 )
    {
        return !(vector1 == vector2);
    }

internal:

    [FieldOffset(0)]
    float dangerousFirstField;

public:

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Vector4F::typeid)
        {
            return false;
        }

        return *this == safe_cast<Vector4F>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + x.GetHashCode();
        hashCode = hashCode * 31 + y.GetHashCode();
        hashCode = hashCode * 31 + z.GetHashCode();
        hashCode = hashCode * 31 + w.GetHashCode();

        return hashCode;
    }

};



/// <summary>
/// This structure defines a 3 component float vector.
/// </summary>
public value struct Vector3F
{
public:
    /// <summary>
    /// Specifies the first element of the vector.
    /// </summary>
    property float X
    {
        float get()
        {
            return x;
        }

        void set(float value)
        {
            x = value;
        }
    }

    /// <summary>
    /// Specifies the second element of the vector.
    /// </summary>
    property float Y
    {
        float get()
        {
            return y;
        }

        void set(float value)
        {
            y = value;
        }
    }

    /// <summary>
    /// Specifies the third element of the vector.
    /// </summary>
    property float Z
    {
        float get()
        {
            return z;
        }

        void set(float value)
        {
            z = value;
        }
    }

private:

    float x;
    float y;
    float z;

public:
    /// <summary>
    /// Normalize the vector, returning a new one.
    /// </summary>
    /// <returns>The normalized vector</returns>
    Vector3F Normalize()
    {
        float length = (float)Math::Sqrt(X * X + Y * Y + Z * Z);

        if (length == 0)
        {
            return Vector3F(0, 0, 0);
        }

        return Vector3F(X / length, Y / length, Z / length);
    }

    /// <summary>
    /// Normalize the vector.
    /// </summary>
    void NormalizeInPlace()
    {
        float length = (float)Math::Sqrt(X * X + Y * Y + Z * Z);
        
        if (length == 0)
        {
            X = 0;
            Y = 0;
            Z = 0;
        }
        else
        {
            X /= length;
            Y /= length;
            Z /= length;
        }
    }

    /// <summary>
    /// Compute the dot (scalar) product of the given vectors.
    /// </summary>
    /// <param name="vector1">The first vector for the dot product.</param>
    /// <param name="vector2">The second vector for the dot product.</param>
    /// <returns>The dot prodcut.</returns>
    static float Dot(Vector3F vector1, Vector3F vector2)
    {
        return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
    }

    /// <summary>
    /// Compute the cross product of the given vectors.
    /// </summary>
    /// <param name="vector1">The first vector for the cross product.</param>
    /// <param name="vector2">The second vector for the cross product.</param>
    /// <returns>The cross prodcut vector.</returns>
    static Vector3F Cross(Vector3F vector1, Vector3F vector2)
    {
        return Vector3F(
            vector1.Y * vector2.Z - vector1.Z * vector2.Y,
            vector1.Z * vector2.X - vector1.X * vector2.Z,
            vector1.X * vector2.Y - vector1.Y * vector2.X);
    }

    /// <summary>
    /// Adds vector2 to vector1.
    /// </summary>
    /// <param name="vector1">The first vector to add.</param>
    /// <param name="vector2">The second vector to add.</param>
    /// <returns>The addition resultant vector.</returns>
    static Vector3F operator +(Vector3F vector1, Vector3F vector2)
    {
        return Vector3F(vector1.X + vector2.X, vector1.Y + vector2.Y, vector1.Z + vector2.Z);
    }

    /// <summary>
    /// Adds vector2 to vector1.
    /// </summary>
    /// <param name="vector1">The first vector to add.</param>
    /// <param name="vector2">The second vector to add.</param>
    /// <returns>The addition resultant vector.</returns>
	static Vector3F Add(Vector3F vector1, Vector3F vector2)
	{
		return vector1 + vector2;
	}

    /// <summary>
    /// Subtracts vector2 from vector1.
    /// </summary>
    /// <param name="vector1">The vector from which to subtract.</param>
    /// <param name="vector2">The vector to subtract.</param>
    /// <returns>The subtraction resultant vector.</returns>
    static Vector3F operator -(Vector3F vector1, Vector3F vector2)
    {
        return Vector3F(vector1.X - vector2.X, vector1.Y - vector2.Y, vector1.Z - vector2.Z);
    }

    /// <summary>
    /// Subtracts vector2 from vector1.
    /// </summary>
    /// <param name="vector1">The vector from which to subtract.</param>
    /// <param name="vector2">The vector to subtract.</param>
    /// <returns>The subtraction resultant vector.</returns>
    static Vector3F Subtract(Vector3F vector1, Vector3F vector2)
    {
		return vector1 - vector2;
	}

public:
    /// <summary>
    /// Initializes the vector from a set of values.
    /// </summary>
    /// <param name="x">Specifies value of x.</param>
    /// <param name="y">Specifies value of y.</param>
    /// <param name="z">Specifies value of z.</param>
    Vector3F(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Initializes the vector from an array.
    /// </summary>
    /// <param name="dataSource">The vector data as an array of floats.</param>
    Vector3F(array<float>^ dataSource)
    {
        if(dataSource->Length != 3)
        {
            throw gcnew ArgumentException("Invalid array length", "dataSource");
        }

        this->X = dataSource[0];
        this->Y = dataSource[1];
        this->Z = dataSource[2];
    }

    /// <summary>
    /// The vector equality operator.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if the vectors are equal.</returns>
    static Boolean operator ==( Vector3F vector1, Vector3F vector2 )
    {
        return 
            (vector1.X == vector2.X) && 
            (vector1.Y == vector2.Y) && 
            (vector1.Z == vector2.Z);
    }

    /// <summary>
    /// The vector inequality operator.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if the vectors are not equal.</returns>
    static Boolean operator !=( Vector3F vector1, Vector3F vector2 )
    {
        return !(vector1 == vector2);
    }
    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Vector3F::typeid)
        {
            return false;
        }

        return *this == safe_cast<Vector3F>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + x.GetHashCode();
        hashCode = hashCode * 31 + y.GetHashCode();
        hashCode = hashCode * 31 + z.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// This structure defines a 2 component float vector.
/// </summary>
public value struct Vector2F
{
public:
    /// <summary>
    /// Specifies the first element of the vector.
    /// </summary>
    property float X
    {
        float get()
        {
            return x;
        }

        void set(float value)
        {
            x = value;
        }
    }

    /// <summary>
    /// Specifies the second element of the vector.
    /// </summary>
    property float Y
    {
        float get()
        {
            return y;
        }

        void set(float value)
        {
            y = value;
        }
    }


private:

    float x;
    float y;

public:
    /// <summary>
    /// Initializes the vector from a set of values.
    /// </summary>
    /// <param name="x">Specifies value of x.</param>
    /// <param name="y">Specifies value of y.</param>
    Vector2F(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Initializes the vector from an array.
    /// </summary>
    /// <param name="dataSource">The vector data as an array of floats.</param>
    Vector2F(array<float>^ dataSource)
    {
        if(dataSource->Length != 2)
        {
            throw gcnew ArgumentException("Invalid array length", "dataSource");
        }

        this->X = dataSource[0];
        this->Y = dataSource[1];
    }

    /// <summary>
    /// The vector equality operator.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if the vectors are equal.</returns>
    static Boolean operator ==( Vector2F vector1, Vector2F vector2 )
    {
        return 
            (vector1.X == vector2.X) && 
            (vector1.Y == vector2.Y);
    }

    /// <summary>
    /// The vector inequality operator.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if the vectors are not equal.</returns>
    static Boolean operator !=( Vector2F vector1, Vector2F vector2 )
    {
        return !(vector1 == vector2);
    }
    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Vector2F::typeid)
        {
            return false;
        }

        return *this == safe_cast<Vector2F>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + x.GetHashCode();
        hashCode = hashCode * 31 + y.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// This structure defines a 4 component int vector.
/// </summary>
public value struct Vector4I
{
public:
    /// <summary>
    /// Specifies the first element of the vector.
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
    /// Specifies the second element of the vector.
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
    /// Specifies the third element of the vector.
    /// </summary>
    property int Z
    {
        int get()
        {
            return z;
        }

        void set(int value)
        {
            z = value;
        }
    }

    /// <summary>
    /// Specifies the fourth element of the vector.
    /// </summary>
    property int W
    {
        int get()
        {
            return w;
        }

        void set(int value)
        {
            w = value;
        }
    }


private:

    int x;
    int y;
    int z;
    int w;

public:
    /// <summary>
    /// Initializes the vector from a set of values.
    /// </summary>
    /// <param name="x">Specifies value of x.</param>
    /// <param name="y">Specifies value of y.</param>
    /// <param name="z">Specifies value of z.</param>
    /// <param name="w">Specifies value of w.</param>
    Vector4I( int x, int y, int z, int w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Initializes the vector from an array.
    /// </summary>
    /// <param name="dataSource">The vector data as an array of floats.</param>
    Vector4I(array<int>^ dataSource)
    {
        if(dataSource->Length != 4)
        {
            throw gcnew ArgumentException("Invalid array length", "dataSource");
        }

        this->X = dataSource[0];
        this->Y = dataSource[1];
        this->Z = dataSource[2];
        this->W = dataSource[3];
    }

    /// <summary>
    /// The vector equality operator.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if the vectors are equal.</returns>
    static Boolean operator ==( Vector4I vector1, Vector4I vector2 )
    {
        return 
            (vector1.X == vector2.X) && 
            (vector1.Y == vector2.Y) && 
            (vector1.Z == vector2.Z) && 
            (vector1.W == vector2.W);
    }

    /// <summary>
    /// The vector inequality operator.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if the vectors are not equal.</returns>
    static Boolean operator !=( Vector4I vector1, Vector4I vector2 )
    {
        return !(vector1 == vector2);
    }
    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Vector4I::typeid)
        {
            return false;
        }

        return *this == safe_cast<Vector4I>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + x.GetHashCode();
        hashCode = hashCode * 31 + y.GetHashCode();
        hashCode = hashCode * 31 + z.GetHashCode();
        hashCode = hashCode * 31 + w.GetHashCode();

        return hashCode;
    }

};



/// <summary>
/// This structure defines a 4 component Boolean vector.
/// </summary>
public value struct Vector4B
{
public:
    /// <summary>
    /// Specifies the first element of the vector.
    /// </summary>
    property Boolean X
    {
        Boolean get()
        {
            return x;
        }

        void set(Boolean value)
        {
            x = value;
        }
    }

    /// <summary>
    /// Specifies the second element of the vector.
    /// </summary>
    property Boolean Y
    {
        Boolean get()
        {
            return y;
        }

        void set(Boolean value)
        {
            y = value;
        }
    }

    /// <summary>
    /// Specifies the third element of the vector.
    /// </summary>
    property Boolean Z
    {
        Boolean get()
        {
            return z;
        }

        void set(Boolean value)
        {
            z = value;
        }
    }

    /// <summary>
    /// Specifies the fourth element of the vector.
    /// </summary>
    property Boolean W
    {
        Boolean get()
        {
            return w;
        }

        void set(Boolean value)
        {
            w = value;
        }
    }

private:

    Boolean x;
    Boolean y;
    Boolean z;
    Boolean w;

public:
    /// <summary>
    /// Initializes the vector from a set of values.
    /// </summary>
    /// <param name="x">Specifies value of x.</param>
    /// <param name="y">Specifies value of y.</param>
    /// <param name="z">Specifies value of z.</param>
    /// <param name="w">Specifies value of w.</param>
    Vector4B(Boolean x, Boolean y, Boolean z, Boolean w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Initializes the vector from an array.
    /// </summary>
    /// <param name="dataSource">The vector data as an array of floats.</param>
    Vector4B(array<Boolean>^ dataSource)
    {
        if(dataSource->Length != 4)
        {
            throw gcnew ArgumentException("Invalid array length", "dataSource");
        }

        this->X = dataSource[0];
        this->Y = dataSource[1];
        this->Z = dataSource[2];
        this->W = dataSource[3];
    }

    /// <summary>
    /// The vector equality operator.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if the vectors are equal.</returns>
    static Boolean operator ==(Vector4B vector1, Vector4B vector2)
    {
        return 
            (vector1.x == vector2.x) && 
            (vector1.y == vector2.y) && 
            (vector1.z == vector2.z) && 
            (vector1.w == vector2.w);
    }

    /// <summary>
    /// The vector inequality operator.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if the vectors are not equal.</returns>
    static Boolean operator !=(Vector4B vector1, Vector4B vector2)
    {
        return !(vector1 == vector2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Vector4B::typeid)
        {
            return false;
        }

        return *this == safe_cast<Vector4B>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + x.GetHashCode();
        hashCode = hashCode * 31 + y.GetHashCode();
        hashCode = hashCode * 31 + z.GetHashCode();
        hashCode = hashCode * 31 + w.GetHashCode();

        return hashCode;
    }

};



/// <summary>
/// This structure defines a 4 x 4 matrix using floats.
/// </summary>
CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId="x")
[StructLayout(LayoutKind::Explicit)]
public value struct Matrix4x4F
{
public:
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M11
    {
        float get()
        {
            return m11;
        }

        void set(float value)
        {
            m11 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M12
    {
        float get()
        {
            return m12;
        }

        void set(float value)
        {
            m12 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M13
    {
        float get()
        {
            return m13;
        }

        void set(float value)
        {
            m13 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M14
    {
        float get()
        {
            return m14;
        }

        void set(float value)
        {
            m14 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M21
    {
        float get()
        {
            return m21;
        }

        void set(float value)
        {
            m21 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M22
    {
        float get()
        {
            return m22;
        }

        void set(float value)
        {
            m22 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M23
    {
        float get()
        {
            return m23;
        }

        void set(float value)
        {
            m23 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M24
    {
        float get()
        {
            return m24;
        }

        void set(float value)
        {
            m24 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M31
    {
        float get()
        {
            return m31;
        }

        void set(float value)
        {
            m31 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M32
    {
        float get()
        {
            return m32;
        }

        void set(float value)
        {
            m32 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M33
    {
        float get()
        {
            return m33;
        }

        void set(float value)
        {
            m33 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M34
    {
        float get()
        {
            return m34;
        }

        void set(float value)
        {
            m34 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M41
    {
        float get()
        {
            return m41;
        }

        void set(float value)
        {
            m41 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M42
    {
        float get()
        {
            return m42;
        }

        void set(float value)
        {
            m42 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M43
    {
        float get()
        {
            return m43;
        }

        void set(float value)
        {
            m43 = value;
        }
    }
    /// <summary>
    /// Specifies the value of the matrix at a particular row and column.
    /// </summary>
    property float M44
    {
        float get()
        {
            return m44;
        }

        void set(float value)
        {
            m44 = value;
        }
    }

private:

    [FieldOffset(0)]
    float m11;
    [FieldOffset(4)]
    float m12;
    [FieldOffset(8)]
    float m13;
    [FieldOffset(12)]
    float m14;
    [FieldOffset(16)]
    float m21;
    [FieldOffset(20)]
    float m22;
    [FieldOffset(24)]
    float m23;
    [FieldOffset(28)]
    float m24;
    [FieldOffset(32)]
    float m31;
    [FieldOffset(36)]
    float m32;
    [FieldOffset(40)]
    float m33;
    [FieldOffset(44)]
    float m34;
    [FieldOffset(48)]
    float m41;
    [FieldOffset(52)]
    float m42;
    [FieldOffset(56)]
    float m43;
    [FieldOffset(60)]
    float m44;

internal:

    [FieldOffset(0)]
    float dangerousFirstField;

public:
    /// <summary>
    /// Initializes the matrix from a set of values.
    /// </summary>
    /// <param name="m11">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m12">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m13">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m14">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m21">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m22">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m23">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m24">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m31">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m32">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m33">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m34">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m41">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m42">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m43">Specifies the value of the matrix at a particular row and column.</param>
    /// <param name="m44">Specifies the value of the matrix at a particular row and column.</param>
    Matrix4x4F( 
            float m11, float m12, float m13, float m14,
            float m21, float m22, float m23, float m24,
            float m31, float m32, float m33, float m34,
            float m41, float m42, float m43, float m44 )

    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M14 = m14;
        M21 = m21;
        M22 = m22;
        M23 = m23;
        M24 = m24;
        M31 = m31;
        M32 = m32;
        M33 = m33;
        M34 = m34;
        M41 = m41;
        M42 = m42;
        M43 = m43;
        M44 = m44;
    }

    /// <summary>
    /// Initializes the matrix from an array.
    /// </summary>
    /// <param name="dataSource">The matrix data as an array of floats.</param>
    Matrix4x4F(array<float>^ dataSource)
    {
        if(dataSource->Length != 16)
        {
            throw gcnew ArgumentException("Invalid array length", "dataSource");
        }

        M11 = dataSource[0];
        M12 = dataSource[1];
        M13 = dataSource[2];
        M14 = dataSource[3];
        M21 = dataSource[4];
        M22 = dataSource[5];
        M23 = dataSource[6];
        M24 = dataSource[7];
        M31 = dataSource[8];
        M32 = dataSource[9];
        M33 = dataSource[10];
        M34 = dataSource[11];
        M41 = dataSource[12];
        M42 = dataSource[13];
        M43 = dataSource[14];
        M44 = dataSource[15];
    }

    /// <summary>
    /// Returns an identity matrix.
    /// </summary>
    static property Matrix4x4F Identity
    {
        Matrix4x4F get()
        {
            return Matrix4x4F( 
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
        }
    }

    /// <summary>
    /// The matrix multiplication operator (linear transformation composition).
    /// </summary>
    /// <param name="matrix1">The first factor.</param>
    /// <param name="matrix2">The second factor.</param>
    /// <returns>The product of the matricies.</returns>
    static Matrix4x4F operator *( Matrix4x4F matrix1, Matrix4x4F matrix2 )
    {
        return Matrix4x4F(
            matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41,
            matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42,
            matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43,
            matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44,

            matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41,
            matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42,
            matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43,
            matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44,

            matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41,
            matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42,
            matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43,
            matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44,

            matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41,
            matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42,
            matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43,
            matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44
            );
    }

    /// <summary>
    /// The matrix multiplication operator (linear transformation composition).
    /// </summary>
    /// <param name="matrix1">The first factor.</param>
    /// <param name="matrix2">The second factor.</param>
    /// <returns>The product of the matricies.</returns>
    static Matrix4x4F Multiply( Matrix4x4F matrix1, Matrix4x4F matrix2 )
    {
		return matrix1 * matrix2;
	}

    /// <summary>
    /// The matrix equality operator.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <returns>True if the matricies are equal.</returns>
    static Boolean operator ==(Matrix4x4F matrix1, Matrix4x4F matrix2)
    {
        return 
            (matrix1.M11 == matrix2.M11) && 
            (matrix1.M12 == matrix2.M12) && 
            (matrix1.M13 == matrix2.M13) && 
            (matrix1.M14 == matrix2.M14) && 

            (matrix1.M21 == matrix2.M21) && 
            (matrix1.M22 == matrix2.M22) && 
            (matrix1.M23 == matrix2.M23) && 
            (matrix1.M24 == matrix2.M24) && 

            (matrix1.M31 == matrix2.M31) && 
            (matrix1.M32 == matrix2.M32) && 
            (matrix1.M33 == matrix2.M33) && 
            (matrix1.M34 == matrix2.M34) && 

            (matrix1.M41 == matrix2.M41) && 
            (matrix1.M42 == matrix2.M42) && 
            (matrix1.M43 == matrix2.M43) && 
            (matrix1.M44 == matrix2.M44);
    }

    /// <summary>
    /// The matrix inequality operator.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second second matrix.</param>
    /// <returns>True if the matricies are not equal.</returns>
    static Boolean operator !=(Matrix4x4F matrix1, Matrix4x4F matrix2)
    {
        return !(matrix1 == matrix2);
    }

internal:
    Matrix4x4F( float* matrixArray )
    {
        // REVIEW: this was always incorrect, because no explicit layout
        // was defined for this type. It's still incorrect.
        pin_ptr<Matrix4x4F> self = this;
        memcpy( self, matrixArray, sizeof(Matrix4x4F) );
    }
public:

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Matrix4x4F::typeid)
        {
            return false;
        }

        return *this == safe_cast<Matrix4x4F>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + m11.GetHashCode();
        hashCode = hashCode * 31 + m12.GetHashCode();
        hashCode = hashCode * 31 + m13.GetHashCode();
        hashCode = hashCode * 31 + m14.GetHashCode();
        hashCode = hashCode * 31 + m21.GetHashCode();
        hashCode = hashCode * 31 + m22.GetHashCode();
        hashCode = hashCode * 31 + m23.GetHashCode();
        hashCode = hashCode * 31 + m24.GetHashCode();
        hashCode = hashCode * 31 + m31.GetHashCode();
        hashCode = hashCode * 31 + m32.GetHashCode();
        hashCode = hashCode * 31 + m33.GetHashCode();
        hashCode = hashCode * 31 + m34.GetHashCode();
        hashCode = hashCode * 31 + m41.GetHashCode();
        hashCode = hashCode * 31 + m42.GetHashCode();
        hashCode = hashCode * 31 + m43.GetHashCode();
        hashCode = hashCode * 31 + m44.GetHashCode();

        return hashCode;
    }

};



/// <summary>
/// This structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
/// </summary>
public value struct D3DRect 
{
public:
    /// <summary>
    /// Specifies the x-coordinate of the upper-left corner of the rectangle.
    /// </summary>
    property Int32 Left
    {
        Int32 get()
        {
            return left;
        }

        void set(Int32 value)
        {
            left = value;
        }
    }

    /// <summary>
    /// Specifies the y-coordinate of the upper-left corner of the rectangle.
    /// </summary>
    property Int32 Top
    {
        Int32 get()
        {
            return top;
        }

        void set(Int32 value)
        {
            top = value;
        }
    }

    /// <summary>
    /// Specifies the x-coordinate of the lower-right corner of the rectangle.
    /// </summary>
    property Int32 Right
    {
        Int32 get()
        {
            return right;
        }

        void set(Int32 value)
        {
            right = value;
        }
    }

    /// <summary>
    /// Specifies the y-coordinate of the lower-right corner of the rectangle.
    /// </summary>
    property Int32 Bottom
    {
        Int32 get()
        {
            return bottom;
        }

        void set(Int32 value)
        {
            bottom = value;
        }
    }

private:

    Int32 left;
    Int32 top;
    Int32 right;
    Int32 bottom;

public:
    /// <summary>
    /// Explicit constructor.
    /// </summary>
    /// <param name="left">Specifies the x-coordinate of the upper-left corner of the rectangle.</param>
    /// <param name="top"> Specifies the y-coordinate of the upper-left corner of the rectangle.</param>
    /// <param name="right">Specifies the x-coordinate of the lower-right corner of the rectangle.</param>
    /// <param name="bottom">Specifies the y-coordinate of the lower-right corner of the rectangle.</param>
    D3DRect( Int32 left, Int32 top, Int32 right, Int32 bottom )
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>
    /// The equality operator.
    /// </summary>
    /// <param name="rect1">The first rectangle.</param>
    /// <param name="rect2">The second second rectangle.</param>
    /// <returns>True if the rectangles are equal.</returns>
    static Boolean operator ==(D3DRect rect1, D3DRect rect2)
    {
        return
            (rect1.Left == rect2.Left) &&
            (rect1.Top == rect2.Top) &&
            (rect1.Bottom == rect2.Bottom) &&
            (rect1.Right == rect2.Right);
    }

    /// <summary>
    /// The inequality operator.
    /// </summary>
    /// <param name="rect1">The first rectangle.</param>
    /// <param name="rect2">The second second rectangle.</param>
    /// <returns>True if the rectangles are not equal.</returns>
    static Boolean operator !=(D3DRect rect1, D3DRect rect2)
    {
        return !(rect1 == rect2);
    }

internal:
    D3DRect(RECT* pD3dRect)
    {
        Left = pD3dRect->left;
        Top = pD3dRect->top;
        Right = pD3dRect->right;
        Bottom = pD3dRect->bottom;
    }
    D3DRect(const RECT &rect)
    {
        Left = rect.left;
        Top = rect.top;
        Right = rect.right;
        Bottom = rect.bottom;
    }
    
    operator const RECT ()
    {
        RECT nativeRect;

        nativeRect.left = Left;
        nativeRect.top = Top;
        nativeRect.right = Right;
        nativeRect.bottom = Bottom;

        return nativeRect;
    }

public:

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != D3DRect::typeid)
        {
            return false;
        }

        return *this == safe_cast<D3DRect>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + left.GetHashCode();
        hashCode = hashCode * 31 + top.GetHashCode();
        hashCode = hashCode * 31 + right.GetHashCode();
        hashCode = hashCode * 31 + bottom.GetHashCode();

        return hashCode;
    }

};

} } } }
