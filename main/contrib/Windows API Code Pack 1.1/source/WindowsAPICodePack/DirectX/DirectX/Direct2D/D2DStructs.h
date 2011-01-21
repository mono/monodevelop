// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {

    using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

    /// <summary>
    /// Represents an x-coordinate and y-coordinate pair, expressed as floating-point values, in two-dimensional space.
    /// <para>(Also see DirectX SDK: D2D1_POINT_2F)</para>
    /// </summary>
    public value struct Point2F
    {
    public:


        /// <summary>
        /// Constructor for the Point2F value type
        /// </summary>
        /// <param name="x">Initializes the X property.</param>
        /// <param name="y">Initializes the Y property.</param>
        Point2F(
            FLOAT x,
            FLOAT y
            );


        /// <summary>
        /// The x-coordinate of the point.
        /// </summary>
        property FLOAT X
        {
            FLOAT get()
            {
                return x;
            }

            void set(FLOAT value)
            {
                x = value;
            }
        }

        /// <summary>
        /// The y-coordinate of the point.
        /// </summary>
        property FLOAT Y
        {
            FLOAT get()
            {
                return y;
            }

            void set(FLOAT value)
            {
                y = value;
            }
        }

        static Boolean operator == ( Point2F point1, Point2F point2 )
        {
            return 
                (point1.X == point2.X)  &&
                (point1.Y == point2.Y);
        }

        static Boolean operator != ( Point2F point1, Point2F point2 )
        {
            return !(point1 == point2);
        }

    private:

        FLOAT x;
        FLOAT y;

    internal:

        void CopyFrom(
            __in const D2D1_POINT_2F &point_2f
            );

        void CopyTo(
            __out D2D1_POINT_2F *ppoint_2f
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != Point2F::typeid)
            {
                return false;
            }

            return *this == safe_cast<Point2F>(obj);
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
    /// Stores an ordered pair of integers, typically the width and height of a rectangle.
    /// <para>(Also see DirectX SDK: D2D1_SIZE_F)</para>
    /// </summary>
    public value struct SizeF
    {
    public:

        /// <summary>
        /// Constructor for the SizeF value type
        /// </summary>
        /// <param name="width">Initializes the Width property.</param>
        /// <param name="height">Initializes the Height property.</param>
        SizeF(
            FLOAT width,
            FLOAT height
            );


        /// <summary>
        /// The horizontal component of this size.
        /// </summary>
        property FLOAT Width
        {
            FLOAT get()
            {
                return width;
            }

            void set(FLOAT value)
            {
                width = value;
            }
        }

        /// <summary>
        /// The vertical component of this size.
        /// </summary>
        property FLOAT Height
        {
            FLOAT get()
            {
                return height;
            }

            void set(FLOAT value)
            {
                height = value;
            }
        }

        static Boolean operator == ( SizeF size1, SizeF size2 )
        {
            return 
                (size1.Width == size2.Width)  &&
                (size1.Height == size2.Height);
        }

        static Boolean operator != ( SizeF size1, SizeF size2 )
        {
            return !(size1 == size2);
        }

    private:

        FLOAT width;
        FLOAT height;

    internal:

        void CopyFrom(
            __in const D2D1_SIZE_F &size_f
            );

        void CopyTo(
            __out D2D1_SIZE_F *psize_f
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != SizeF::typeid)
            {
                return false;
            }

            return *this == safe_cast<SizeF>(obj);
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
    /// Describes an elliptical arc between two points.
    /// <para>(Also see DirectX SDK: D2D1_ARC_SEGMENT)</para>
    /// </summary>
    public value struct ArcSegment
    {
    public:


        /// <summary>
        /// Constructor for the ArcSegment value type
        /// </summary>
        /// <param name="point">Initializes the Point property.</param>
        /// <param name="size">Initializes the Size property.</param>
        /// <param name="rotationAngle">Initializes the RotationAngle property.</param>
        /// <param name="sweepDirection">Initializes the SweepDirection property.</param>
        /// <param name="arcSize">Initializes the ArcSize property.</param>
        ArcSegment(
            Point2F point,
            SizeF size,
            FLOAT rotationAngle,
            Direct2D1::SweepDirection sweepDirection,
            Direct2D1::ArcSize arcSize
            );


        /// <summary>
        /// The end point of the arc.
        /// </summary>
        property Point2F Point
        {
            Point2F get()
            {
                return point;
            }

            void set(Point2F value)
            {
                point = value;
            }
        }

        /// <summary>
        /// The x-radius and y-radius of the arc.
        /// </summary>
        property SizeF Size
        {
            SizeF get()
            {
                return size;
            }

            void set(SizeF value)
            {
                size = value;
            }
        }

        /// <summary>
        /// A value that specifies how many degrees in the clockwise direction the ellipse 
        /// is rotated relative to the current coordinate system.
        /// </summary>
        property FLOAT RotationAngle
        {
            FLOAT get()
            {
                return rotationAngle;
            }

            void set(FLOAT value)
            {
                rotationAngle = value;
            }
        }

        /// <summary>
        /// A value that specifies whether the arc sweep is clockwise or counterclockwise.
        /// </summary>
        property Direct2D1::SweepDirection SweepDirection
        {
            Direct2D1::SweepDirection get()
            {
                return sweepDirection;
            }

            void set(Direct2D1::SweepDirection value)
            {
                sweepDirection = value;
            }
        }

        /// <summary>
        /// A value that specifies whether the given arc is larger than 180 degrees.
        /// </summary>
        property Direct2D1::ArcSize ArcSize
        {
            Direct2D1::ArcSize get()
            {
                return arcSize;
            }

            void set(Direct2D1::ArcSize value)
            {
                arcSize = value;
            }
        }

        static Boolean operator == ( ArcSegment segment1, ArcSegment segment2 )
        {
            return 
                (segment1.Point == segment2.Point)  &&
                (segment1.Size == segment2.Size)  &&
                (segment1.RotationAngle == segment2.RotationAngle)  &&
                (segment1.SweepDirection == segment2.SweepDirection)  &&
                (segment1.ArcSize == segment2.ArcSize);
        }

        static Boolean operator != ( ArcSegment segment1, ArcSegment segment2 )
        {
            return !(segment1 == segment2);
        }

    private:

        Point2F point;
        SizeF size;
        FLOAT rotationAngle;
        Direct2D1::SweepDirection sweepDirection;
        Direct2D1::ArcSize arcSize;

    internal:

        void CopyFrom(
            __in const D2D1_ARC_SEGMENT &arc_segment
            );

        void CopyTo(
            __out D2D1_ARC_SEGMENT *parc_segment
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != ArcSegment::typeid)
            {
                return false;
            }

            return *this == safe_cast<ArcSegment>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + point.GetHashCode();
            hashCode = hashCode * 31 + size.GetHashCode();
            hashCode = hashCode * 31 + rotationAngle.GetHashCode();
            hashCode = hashCode * 31 + sweepDirection.GetHashCode();
            hashCode = hashCode * 31 + arcSize.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Represents a cubic bezier segment drawn between two points.
    /// <para>(Also see DirectX SDK: D2D1_BEZIER_SEGMENT)</para>
    /// </summary>
    public value struct BezierSegment
    {
    public:


        /// <summary>
        /// Constructor for the BezierSegment value type
        /// </summary>
        /// <param name="point1">Initializes the Point1 property.</param>
        /// <param name="point2">Initializes the Point2 property.</param>
        /// <param name="point3">Initializes the Point3 property.</param>
        BezierSegment(
            Point2F point1,
            Point2F point2,
            Point2F point3
            );


        /// <summary>
        /// The first control point for the Bezier segment.
        /// </summary>
        property Point2F Point1
        {
            Point2F get()
            {
                return point1;
            }

            void set(Point2F value)
            {
                point1 = value;
            }
        }

        /// <summary>
        /// The second control point for the Bezier segment.
        /// </summary>
        property Point2F Point2
        {
            Point2F get()
            {
                return point2;
            }

            void set(Point2F value)
            {
                point2 = value;
            }
        }

        /// <summary>
        /// The end point for the Bezier segment.
        /// </summary>
        property Point2F Point3
        {
            Point2F get()
            {
                return point3;
            }

            void set(Point2F value)
            {
                point3 = value;
            }
        }

        static Boolean operator == ( BezierSegment segment1, BezierSegment segment2 )
        {
            return 
                (segment1.Point1 == segment2.Point1)  &&
                (segment1.Point2 == segment2.Point2)  &&
                (segment1.Point3 == segment2.Point3);
        }

        static Boolean operator != ( BezierSegment segment1, BezierSegment segment2 )
        {
            return !(segment1 == segment2);
        }

    private:

        Point2F point1;
        Point2F point2;
        Point2F point3;

    internal:

        void CopyFrom(
            __in const D2D1_BEZIER_SEGMENT &bezier_segment
            );

        void CopyTo(
            __out D2D1_BEZIER_SEGMENT *pbezier_segment
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != BezierSegment::typeid)
            {
                return false;
            }

            return *this == safe_cast<BezierSegment>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + point1.GetHashCode();
            hashCode = hashCode * 31 + point2.GetHashCode();
            hashCode = hashCode * 31 + point3.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Contains the three vertices that describe a triangle.
    /// <para>(Also see DirectX SDK: D2D1_TRIANGLE)</para>
    /// </summary>
    public value struct Triangle
    {
    public:


        /// <summary>
        /// Constructor for the Triangle value type
        /// </summary>
        /// <param name="point1">Initializes the Point1 property.</param>
        /// <param name="point2">Initializes the Point2 property.</param>
        /// <param name="point3">Initializes the Point3 property.</param>
        Triangle(
            Point2F point1,
            Point2F point2,
            Point2F point3
            );

        /// <summary>
        /// The first vertex of a triangle.
        /// </summary>
        property Point2F Point1
        {
            Point2F get()
            {
                return point1;
            }

            void set(Point2F value)
            {
                point1 = value;
            }
        }

        /// <summary>
        /// The second vertex of a triangle.
        /// </summary>
        property Point2F Point2
        {
            Point2F get()
            {
                return point2;
            }

            void set(Point2F value)
            {
                point2 = value;
            }
        }

        /// <summary>
        /// The third vertex of a triangle.
        /// </summary>
        property Point2F Point3
        {
            Point2F get()
            {
                return point3;
            }

            void set(Point2F value)
            {
                point3 = value;
            }
        }

        static Boolean operator == ( Triangle triangle1, Triangle triangle2 )
        {
            return 
                (triangle1.Point1 == triangle2.Point1)  &&
                (triangle1.Point2 == triangle2.Point2)  &&
                (triangle1.Point3 == triangle2.Point3);
        }

        static Boolean operator != ( Triangle triangle1, Triangle triangle2 )
        {
            return !(triangle1 == triangle2);
        }

    private:

        Point2F point1;
        Point2F point2;
        Point2F point3;

    internal:

        void CopyFrom(
            __in const D2D1_TRIANGLE &triangle
            );

        void CopyTo(
            __out D2D1_TRIANGLE *ptriangle
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != Triangle::typeid)
            {
                return false;
            }

            return *this == safe_cast<Triangle>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + point1.GetHashCode();
            hashCode = hashCode * 31 + point2.GetHashCode();
            hashCode = hashCode * 31 + point3.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Represents a 3-by-2 matrix.
    /// <para>(Also see DirectX SDK: D2D1_MATRIX_3X2_F)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId="x")
    public value struct Matrix3x2F
    {
    public:

        /// <summary>
        /// Constructor for the Matrix3x2F value type
        /// </summary>
        /// <param name="m11">Initializes the _11 property.</param>
        /// <param name="m12">Initializes the _12 property.</param>
        /// <param name="m21">Initializes the _21 property.</param>
        /// <param name="m22">Initializes the _22 property.</param>
        /// <param name="m31">Initializes the _31 property.</param>
        /// <param name="m32">Initializes the _32 property.</param>
        Matrix3x2F(
            FLOAT m11,
            FLOAT m12,
            FLOAT m21,
            FLOAT m22,
            FLOAT m31,
            FLOAT m32
            );


        /// <summary>
        /// The value in the first row and first column of the matrix.
        /// </summary>
        property FLOAT M11
        {
            FLOAT get()
            {
                return m11;
            }

            void set(FLOAT value)
            {
                m11 = value;
            }
        }

        /// <summary>
        /// The value in the first row and second column of the matrix.
        /// </summary>
        property FLOAT M12
        {
            FLOAT get()
            {
                return m12;
            }

            void set(FLOAT value)
            {
                m12 = value;
            }
        }

        /// <summary>
        /// The value in the second row and first column of the matrix.
        /// </summary>
        property FLOAT M21
        {
            FLOAT get()
            {
                return m21;
            }

            void set(FLOAT value)
            {
                m21 = value;
            }
        }

        /// <summary>
        /// The value in the second row and second column of the matrix.
        /// </summary>
        property FLOAT M22
        {
            FLOAT get()
            {
                return m22;
            }

            void set(FLOAT value)
            {
                m22 = value;
            }
        }

        /// <summary>
        /// The value in the third row and first column of the matrix.
        /// </summary>
        property FLOAT M31
        {
            FLOAT get()
            {
                return m31;
            }

            void set(FLOAT value)
            {
                m31 = value;
            }
        }

        /// <summary>
        /// The value in the third row and second column of the matrix.
        /// </summary>
        property FLOAT M32
        {
            FLOAT get()
            {
                return m32;
            }

            void set(FLOAT value)
            {
                m32 = value;
            }
        }

    private:

        FLOAT m11;
        FLOAT m12;
        FLOAT m21;
        FLOAT m22;
        FLOAT m31;
        FLOAT m32;

    public:

        static Boolean operator == ( Matrix3x2F matrix1, Matrix3x2F matrix2 )
        {
            return 
                (matrix1.M11 == matrix2.M11)  &&
                (matrix1.M12 == matrix2.M12)  &&
                (matrix1.M21 == matrix2.M21)  &&
                (matrix1.M22 == matrix2.M22)  &&
                (matrix1.M31 == matrix2.M31)  &&
                (matrix1.M32 == matrix2.M32);
        }

        static Boolean operator != ( Matrix3x2F matrix1, Matrix3x2F matrix2 )
        {
            return !(matrix1 == matrix2);
        }

        /// <summary>
        /// Creates a translation transformation that has the specified displacement.
        /// </summary>
        /// <param name="size">Contains the x and y displacements.</param>
        /// <returns>A transformation matrix that translates an object the specified horizontal and vertical distance.</returns>
        static Matrix3x2F Translation( SizeF size );

        /// <summary>
        /// Creates a translation transformation that has the specified x and y displacements.
        /// </summary>
        /// <param name="x">The distance to translate along the x-axis.</param>
        /// <param name="y">The distance to translate along the y-axis.</param>
        /// <returns>A transformation matrix that translates an object the specified horizontal and vertical distance.</returns>
        static Matrix3x2F Translation( float x, float y);

        /// <summary>
        /// Creates a scale transformation that has the specified scale factors and center point. 
        /// </summary>
        /// <param name="size">Contains the x and y factors of scale transformation.</param>
        /// <param name="center">The point about which the scale is performed.</param>
        /// <returns>The new scale transformation.</returns>
        static Matrix3x2F Scale(SizeF size, Point2F center);

        /// <summary>
        /// Creates a scale transformation that has the specified scale factors at point (0, 0). 
        /// </summary>
        /// <param name="size">Contains the x and y factors of scale transformation.</param>
        /// <returns>The new scale transformation.</returns>
        static Matrix3x2F Scale(SizeF size);

        /// <summary>
        /// Creates a scale transformation that has the specified scale factors and center point. 
        /// </summary>
        /// <param name="x">The x-axis scale factor of the scale transformation.</param>
        /// <param name="y">The y-axis scale factor of the scale transformation.</param>
        /// <param name="center">The point about which the scale is performed.</param>
        /// <returns>The new scale transformation.</returns>
        static Matrix3x2F Scale(FLOAT x, FLOAT y, Point2F center);

        /// <summary>
        /// Creates a scale transformation that has the specified scale factors at point (0, 0). 
        /// </summary>
        /// <param name="x">The x-axis scale factor of the scale transformation.</param>
        /// <param name="y">The y-axis scale factor of the scale transformation.</param>
        /// <returns>The new scale transformation.</returns>
        static Matrix3x2F Scale(FLOAT x, FLOAT y);

        /// <summary>
        /// Creates a rotation transformation that has the specified angle and center point.
        /// </summary>
        /// <param name="angle">The rotation angle in degrees. 
        /// A positive angle creates a clockwise rotation, and a negative angle creates a counterclockwise rotation.</param>
        /// <param name="center">The point about which the rotation is performed.</param>
        /// <returns>The new rotation transformation.</returns>
        static Matrix3x2F Rotation(FLOAT angle, Point2F center);

        /// <summary>
        /// Creates a rotation transformation that has the specified angle and rotates around Point (0, 0).
        /// </summary>
        /// <param name="angle">The rotation angle in degrees. 
        /// A positive angle creates a clockwise rotation, and a negative angle creates a counterclockwise rotation.</param>
        /// <returns>The new rotation transformation.</returns>
        static Matrix3x2F Rotation(FLOAT angle);

        /// <summary>
        /// Creates a skew transformation that has the specified x-axis and y-axis values and center point.
        /// </summary>
        /// <param name="center">The point about which the skew is performed.</param>
        /// <param name="angleX">The x-axis skew angle, which is measured in degrees counterclockwise from the y-axis.</param>
        /// <param name="angleY">The y-axis skew angle, which is measured in degrees clockwise from the x-axis.</param>
        /// <returns>The new skew transformation.</returns>
        static Matrix3x2F Skew(FLOAT angleX, FLOAT angleY, Point2F center);

        /// <summary>
        /// Creates a skew transformation that has the specified x-axis and y-axis values at point (0, 0).
        /// </summary>
        /// <param name="angleX">The x-axis skew angle, which is measured in degrees counterclockwise from the y-axis.</param>
        /// <param name="angleY">The y-axis skew angle, which is measured in degrees clockwise from the x-axis.</param>
        /// <returns>The new skew transformation.</returns>
        static Matrix3x2F Skew(FLOAT angleX, FLOAT angleY);


        /// <summary>
        /// The Idenitity Matrix
        /// </summary>
        static property Matrix3x2F Identity
        {
            Matrix3x2F get();        
        }

        /// <summary>
        /// Indicates whether this matrix is the identity matrix.
        /// </summary>
        property bool IsIdentity
        {
            bool get();
        }

    internal:
        void CopyFrom(
            __in const D2D1_MATRIX_3X2_F &matrix_3x2_f
            );

        void CopyTo(
            __out D2D1_MATRIX_3X2_F *pmatrix_3x2_f
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != Matrix3x2F::typeid)
            {
                return false;
            }

            return *this == safe_cast<Matrix3x2F>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + m11.GetHashCode();
            hashCode = hashCode * 31 + m12.GetHashCode();
            hashCode = hashCode * 31 + m21.GetHashCode();
            hashCode = hashCode * 31 + m22.GetHashCode();
            hashCode = hashCode * 31 + m31.GetHashCode();
            hashCode = hashCode * 31 + m32.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Defines a two-tag element, which contains two application-defined 64-bit unsigned integer values used to mark a set of rendering operations. 
    /// </summary>
    public value struct Tags
    {
    public:

        /// <summary>
        /// Constructor for the Tags struct.
        /// </summary>
        /// <param name="tag1">Initializes the first tag element.</param>
        /// <param name="tag2">Initializes the second tag element.</param>
        Tags(
            UINT64 tag1,
            UINT64 tag2
            )
        {
            Tag1 = tag1;
            Tag2 = tag2;
        }

        /// <summary>
        /// First tag element.
        /// </summary>
        property UINT64 Tag1
        {
            UINT64 get()
            {
                return tag1;
            }

            void set(UINT64 value)
            {
                tag1 = value;
            }
        }

        /// <summary>
        /// Second tag element.
        /// </summary>
        property UINT64 Tag2
        {
            UINT64 get()
            {
                return tag2;
            }

            void set(UINT64 value)
            {
                tag2 = value;
            }
        }


        static Boolean operator == ( Tags tags1, Tags tags2 )
        {
            return 
                (tags1.Tag1 == tags2.Tag1)  &&
                (tags1.Tag2 == tags2.Tag2);
        }

        static Boolean operator != ( Tags tags1, Tags tags2 )
        {
            return !(tags1 == tags2);
        }

    private:

        UINT64 tag1;
        UINT64 tag2;

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != Tags::typeid)
            {
                return false;
            }

            return *this == safe_cast<Tags>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + tag1.GetHashCode();
            hashCode = hashCode * 31 + tag2.GetHashCode();

            return hashCode;
        }

    };
  


    /// <summary>
    /// Describes the drawing state of a render target. 
    /// This also specifies the drawing state that is saved into a DrawingStateBlock object.
    /// <para>(Also see DirectX SDK: D2D1_DRAWING_STATE_DESCRIPTION)</para>
    /// </summary>
    public value struct DrawingStateDescription
    {
    public:

        /// <summary>
        /// Constructor for the DrawingStateDescription value type
        /// </summary>
        /// <param name="antiAliasMode">Initializes the AntiAliasMode property.</param>
        /// <param name="textAntiAliasMode">Initializes the TextAntiAliasMode property.</param>
        /// <param name="tags">Initializes the Tags property.</param>
        /// <param name="transform">Initializes the Transform property.</param>
        DrawingStateDescription(
            Direct2D1::AntiAliasMode antiAliasMode,
            Direct2D1::TextAntiAliasMode textAntiAliasMode,
            Direct2D1::Tags tags,
            Matrix3x2F transform
            );


        /// <summary>
        /// The antialiasing mode for subsequent nontext drawing operations.
        /// </summary>
        property Direct2D1::AntiAliasMode AntiAliasMode
        {
            Direct2D1::AntiAliasMode get()
            {
                return antiAliasMode;
            }

            void set(Direct2D1::AntiAliasMode value)
            {
                antiAliasMode = value;
            }
        }

        /// <summary>
        /// The antialiasing mode for subsequent text and glyph drawing operations.
        /// </summary>
        property Direct2D1::TextAntiAliasMode TextAntiAliasMode
        {
            Direct2D1::TextAntiAliasMode get()
            {
                return textAntiAliasMode;
            }

            void set(Direct2D1::TextAntiAliasMode value)
            {
                textAntiAliasMode = value;
            }
        }

        /// <summary>
        /// The tags for subsequent drawing operations.
        /// </summary>
        property Direct2D1::Tags Tags
        {
            Direct2D1::Tags get()
            {
                return tags;
            }

            void set(Direct2D1::Tags value)
            {
                tags = value;
            }
        }

        /// <summary>
        /// The transformation to apply to subsequent drawing operations.
        /// </summary>
        property Matrix3x2F Transform
        {
            Matrix3x2F get()
            {
                return transform;
            }

            void set(Matrix3x2F value)
            {
                transform = value;
            }
        }

    private:

        Direct2D1::AntiAliasMode antiAliasMode;
        Direct2D1::TextAntiAliasMode textAntiAliasMode;
        Direct2D1::Tags tags;
        Matrix3x2F transform;

    internal:

        void CopyFrom(
            __in const D2D1_DRAWING_STATE_DESCRIPTION &drawing_state_description
            );

        void CopyTo(
            __out D2D1_DRAWING_STATE_DESCRIPTION *pdrawing_state_description
            );

    public:

        static Boolean operator == (DrawingStateDescription drawingStateDescription1, DrawingStateDescription drawingStateDescription2)
        {
            return (drawingStateDescription1.antiAliasMode == drawingStateDescription2.antiAliasMode) &&
                (drawingStateDescription1.textAntiAliasMode == drawingStateDescription2.textAntiAliasMode) &&
                (drawingStateDescription1.tags == drawingStateDescription2.tags) &&
                (drawingStateDescription1.transform == drawingStateDescription2.transform);
        }

        static Boolean operator != (DrawingStateDescription drawingStateDescription1, DrawingStateDescription drawingStateDescription2)
        {
            return !(drawingStateDescription1 == drawingStateDescription2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != DrawingStateDescription::typeid)
            {
                return false;
            }

            return *this == safe_cast<DrawingStateDescription>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + antiAliasMode.GetHashCode();
            hashCode = hashCode * 31 + textAntiAliasMode.GetHashCode();
            hashCode = hashCode * 31 + tags.GetHashCode();
            hashCode = hashCode * 31 + transform.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Contains the center point, x-radius, and y-radius of an ellipse.
    /// <para>(Also see DirectX SDK: D2D1_ELLIPSE)</para>
    /// </summary>
    public value struct Ellipse
    {
    public:

        /// <summary>
        /// Constructor for the Ellipse value type
        /// </summary>
        /// <param name="point">Initializes the Point property.</param>
        /// <param name="radiusX">Initializes the RadiusX property.</param>
        /// <param name="radiusY">Initializes the RadiusY property.</param>
        Ellipse(
            Point2F point,
            FLOAT radiusX,
            FLOAT radiusY
            );


        /// <summary>
        /// The center point of the ellipse.
        /// </summary>
        property Point2F Point
        {
            Point2F get()
            {
                return point;
            }

            void set(Point2F value)
            {
                point = value;
            }
        }

        /// <summary>
        /// The X-radius of the ellipse.
        /// </summary>
        property FLOAT RadiusX
        {
            FLOAT get()
            {
                return radiusX;
            }

            void set(FLOAT value)
            {
                radiusX = value;
            }
        }

        /// <summary>
        /// The Y-radius of the ellipse.
        /// </summary>
        property FLOAT RadiusY
        {
            FLOAT get()
            {
                return radiusY;
            }

            void set(FLOAT value)
            {
                radiusY = value;
            }
        }


        static Boolean operator == ( Ellipse ellipse1, Ellipse ellipse2 )
        {
            return 
                (ellipse1.Point == ellipse2.Point)  &&
                (ellipse1.RadiusX == ellipse2.RadiusX)  &&
                (ellipse1.RadiusY == ellipse2.RadiusY);
        }

        static Boolean operator != ( Ellipse ellipse1, Ellipse ellipse2 )
        {
            return !(ellipse1 == ellipse2);
        }


    private:

        Point2F point;
        FLOAT radiusX;
        FLOAT radiusY;

    internal:

        void CopyFrom(
            __in const D2D1_ELLIPSE &ellipse
            );

        void CopyTo(
            __out D2D1_ELLIPSE *pellipse
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != Ellipse::typeid)
            {
                return false;
            }

            return *this == safe_cast<Ellipse>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + point.GetHashCode();
            hashCode = hashCode * 31 + radiusX.GetHashCode();
            hashCode = hashCode * 31 + radiusY.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Allows additional parameters for factory creation.
    /// <para>(Also see DirectX SDK: D2D1_FACTORY_OPTIONS)</para>
    /// </summary>
    public value struct FactoryOptions
    {
    public:


        /// <summary>
        /// Constructor for the FactoryOptions value type
        /// </summary>
        /// <param name="debugLevel">Initializes the DebugLevel property.</param>
        FactoryOptions(
            Direct2D1::DebugLevel debugLevel
            );

        /// <summary>
        /// Requests a certain level of debugging information from the debug layer. This parameter
        /// is ignored if the debug layer DLL is not present.
        /// </summary>
        property Direct2D1::DebugLevel DebugLevel
        {
            Direct2D1::DebugLevel get()
            {
                return debugLevel;
            }

            void set(Direct2D1::DebugLevel value)
            {
                debugLevel = value;
            }
        }

    private:

        Direct2D1::DebugLevel debugLevel;

    internal:

        void CopyFrom(
            __in const D2D1_FACTORY_OPTIONS &factory_options
            );

        void CopyTo(
            __out D2D1_FACTORY_OPTIONS *pfactory_options
            );
    public:

        static Boolean operator == (FactoryOptions factoryOptions1, FactoryOptions factoryOptions2)
        {
            return (factoryOptions1.debugLevel == factoryOptions2.debugLevel);
        }

        static Boolean operator != (FactoryOptions factoryOptions1, FactoryOptions factoryOptions2)
        {
            return !(factoryOptions1 == factoryOptions2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != FactoryOptions::typeid)
            {
                return false;
            }

            return *this == safe_cast<FactoryOptions>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + debugLevel.GetHashCode();

            return hashCode;
        }

    };


    value struct ColorI;

    /// <summary>
    /// Describes the red, green, blue, and alpha components of a color.
    /// <para>(Also see DirectX SDK: D2D1_COLOR_F)</para>
    /// </summary>
    public value struct ColorF
    {
    public:
        /// <summary>
        /// Constructor for the ColorF value type
        /// </summary>
        /// <param name="red">Initializes the Red property.</param>
        /// <param name="green">Initializes the Green property.</param>
        /// <param name="blue">Initializes the Blue property.</param>
        ColorF(
            FLOAT red,
            FLOAT green,
            FLOAT blue
            );


        /// <summary>
        /// Constructor for the ColorF value type
        /// </summary>
        /// <param name="red">Initializes the Red property.</param>
        /// <param name="green">Initializes the Green property.</param>
        /// <param name="blue">Initializes the Blue property.</param>
        /// <param name="alpha">Initializes the Alpha property.</param>
        ColorF(
            FLOAT red,
            FLOAT green,
            FLOAT blue,
            FLOAT alpha
            );

        /// <summary>
        /// Constructor for the ColorF value type
        /// </summary>
        /// <param name="color">ColorI value used to initialize the new ColorF value</param>
        ColorF(ColorI color);

        /// <summary>
        /// Constructor for the ColorF value type
        /// </summary>
        /// <param name="argb">Packed 8-bit-per-channel color value; alpha channel is most-significant byte, blue is least-significant</param>
        ColorF(int argb);

        /// <summary>
        /// Constructor for the ColorF value type
        /// </summary>
        /// <param name="colorValues">An array containing red, green, blue, and (optionally) alpha values for the ColorF value</param>
        ColorF(array<Single>^ colorValues);


        /// <summary>
        /// Constructor for the ColorF value type
        /// </summary>
        /// <param name="colorValues">An array containing red, green, and blue values for the ColorF value</param>
        /// <param name="alpha">The alpha value for the ColorF value</param>
        ColorF(array<Single>^ colorValues, Single alpha);


        /// <summary>
        /// Floating-point value specifying the red component of a color. This value generally is in the range from 0.0 through 1.0, with 0.0 being black. 
        /// </summary>
        property FLOAT Red
        {
            FLOAT get()
            {
                return r;
            }

            void set(FLOAT value)
            {
                r = value;
            }
        }

        /// <summary>
        /// Floating-point value specifying the green component of a color. This value generally is in the range from 0.0 through 1.0, with 0.0 being black.
        /// </summary>
        property FLOAT Green
        {
            FLOAT get()
            {
                return g;
            }

            void set(FLOAT value)
            {
                g = value;
            }
        }

        /// <summary>
        /// Floating-point value specifying the blue component of a color. This value generally is in the range from 0.0 through 1.0, with 0.0 being black. 
        /// </summary>
        property FLOAT Blue
        {
            FLOAT get()
            {
                return b;
            }

            void set(FLOAT value)
            {
                b = value;
            }
        }

        /// <summary>
        /// Floating-point value specifying the alpha component of a color. This value generally is in the range from 0.0 through 1.0, with 0.0 being completely transparent.
        /// </summary>
        property FLOAT Alpha
        {
            FLOAT get()
            {
                return a;
            }

            void set(FLOAT value)
            {
                a = value;
            }
        }


        static Boolean operator == ( ColorF color1, ColorF color2 )
        {
            return 
                (color1.Red == color2.Red)  &&
                (color1.Green == color2.Green)  &&
                (color1.Blue == color2.Blue)  &&
                (color1.Alpha == color2.Alpha);
        }

        static Boolean operator != ( ColorF color1, ColorF color2 )
        {
            return !(color1 == color2);
        }

    private:

        FLOAT r;
        FLOAT g;
        FLOAT b;
        FLOAT a;

        void InitWithColorI(ColorI color);

    internal:

        void CopyFrom(
            __in const D2D1_COLOR_F &color_f
            );

        void CopyTo(
            __out D2D1_COLOR_F *pcolor_f
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != ColorF::typeid)
            {
                return false;
            }

            return *this == safe_cast<ColorF>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + r.GetHashCode();
            hashCode = hashCode * 31 + g.GetHashCode();
            hashCode = hashCode * 31 + b.GetHashCode();
            hashCode = hashCode * 31 + a.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Describes the red, green, blue, and alpha components of a color using
    /// integer values.
    /// </summary>
    public value struct ColorI
    {
    public:
        /// <summary>
        /// Constructor for the ColorI value type
        /// </summary>
        /// <param name="argb">32-bit integer composed of the alpha, red, green,
        /// and blue color components, most significant byte to least, respectively.</param>
        ColorI(Int32 argb);


        /// <summary>
        /// Constructor for the ColorI value type
        /// </summary>
        /// <param name="red">Initializes the Red property.</param>
        /// <param name="green">Initializes the Green property.</param>
        /// <param name="blue">Initializes the Blue property.</param>
        ColorI(int red, int green, int blue);


        /// <summary>
        /// Constructor for the ColorI value type
        /// </summary>
        /// <param name="red">Initializes the Red property.</param>
        /// <param name="green">Initializes the Green property.</param>
        /// <param name="blue">Initializes the Blue property.</param>
        /// <param name="alpha">Initializes the Alpha property.</param>
        ColorI(int red, int green, int blue, int alpha);


        /// <summary>
        /// Constructor for the ColorI value type
        /// </summary>
        /// <param name="color">ColorF value used to initialize the new ColorI value</param>
        ColorI(ColorF color);

        /// <summary>
        /// Integer value specifying the red component of a color.
        /// This value is in the range from 0 through 255, with 0 being black. 
        /// </summary>
        property int Red
        {
            int get()
            {
                return r;
            }

            void set(int value)
            {
                r = value;
            }
        }

        /// <summary>
        /// Integer value specifying the green component of a color.
        /// This value is in the range from 0 through 255, with 0 being black. 
        /// </summary>
        property int Green
        {
            int get()
            {
                return g;
            }

            void set(int value)
            {
                g = value;
            }
        }

        /// <summary>
        /// Integer value specifying the blue component of a color.
        /// This value is in the range from 0 through 255, with 0 being black. 
        /// </summary>
        property int Blue
        {
            int get()
            {
                return b;
            }

            void set(int value)
            {
                b = value;
            }
        }

        /// <summary>
        /// Integer value specifying the alpha (transparency) component of a color.
        /// This value is in the range from 0 through 255, with 0 being black. 
        /// </summary>
        property int Alpha
        {
            int get()
            {
                return a;
            }

            void set(int value)
            {
                a = value;
            }
        }


        static Boolean operator == ( ColorI color1, ColorI color2 )
        {
            return 
                (color1.Red == color2.Red)  &&
                (color1.Green == color2.Green)  &&
                (color1.Blue == color2.Blue)  &&
                (color1.Alpha == color2.Alpha);
        }

        static Boolean operator != ( ColorI color1, ColorI color2 )
        {
            return !(color1 == color2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != ColorI::typeid)
            {
                return false;
            }

            return *this == safe_cast<ColorI>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + r.GetHashCode();
            hashCode = hashCode * 31 + g.GetHashCode();
            hashCode = hashCode * 31 + b.GetHashCode();
            hashCode = hashCode * 31 + a.GetHashCode();

            return hashCode;
        }

    private:

        int r;
        int g;
        int b;
        int a;

        static const UINT32 sc_alphaShift = 24;
        static const UINT32 sc_redShift   = 16;
        static const UINT32 sc_greenShift = 8;
        static const UINT32 sc_blueShift  = 0;

    };



    /// <summary>
    /// Contains the position and color of a gradient stop.
    /// <para>(Also see DirectX SDK: D2D1_GRADIENT_STOP)</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Gradient stops can be specified in any order if they are at different positions. Two stops may share a position. 
    /// In this case, the first stop specified is treated as the "low" stop (nearer 0.0f) and subsequent stops are treated as "higher" (nearer 1.0f). 
    /// This behavior is useful if a caller wants an instant transition in the middle of a stop.
    /// </para>
    /// <para>
    /// Typically, there are at least two points in a collection, although creation with only one stop is permitted. 
    /// For example, one point is at position 0.0f, another point is at position 1.0f, and additional points 
    /// are distributed in the [0, 1] range. Where the gradient progression is beyond the range of [0, 1], 
    /// the stops are stored, but may affect the gradient. 
    /// </para>
    /// <para>
    /// When drawn, the [0, 1] range of positions is mapped to the brush, in a brush-dependent way. 
    /// </para>
    /// <para>
    /// Gradient stops with a position outside the [0, 1] range can not be seen explicitly, 
    /// but they can still affect the colors produced in the [0, 1] range. 
    /// For example, a two-stop gradient {{0.0f, Black}, {2.0f, White}} is indistinguishable visually from 
    /// {{0.0f, Black}, {1.0f, Mid-level gray}}. Also, the colors are clamped before interpolation.
    /// </para>
    /// </remarks>
    public value struct GradientStop
    {
    public:

        /// <summary>
        /// Constructor for the GradientStop value type
        /// </summary>
        /// <param name="position">Initializes the Position property.</param>
        /// <param name="color">Initializes the Color property.</param>
        GradientStop(
            FLOAT position,
            ColorF color
            );

        /// <summary>
        /// A value that indicates the relative position of the gradient stop in the brush. 
        /// This value must be in the [0.0f, 1.0f] range if the gradient stop is to be seen explicitly.
        /// </summary>
        property FLOAT Position
        {
            FLOAT get()
            {
                return position;
            }

            void set(FLOAT value)
            {
                position = value;
            }
        }

        /// <summary>
        /// The color of the gradient stop.
        /// </summary>
        property ColorF Color
        {
            ColorF get()
            {
                return color;
            }

            void set(ColorF value)
            {
                color = value;
            }
        }

        static Boolean operator == ( GradientStop gradientStop1, GradientStop gradientStop2 )
        {
            return 
                (gradientStop1.Position == gradientStop2.Position)  &&
                (gradientStop1.Color == gradientStop2.Color);
        }

        static Boolean operator != ( GradientStop gradientStop1, GradientStop gradientStop2 )
        {
            return !(gradientStop1 == gradientStop2);
        }

    private:

        FLOAT position;
        ColorF color;

    internal:

        void CopyFrom(
            __in const D2D1_GRADIENT_STOP &gradient_stop
            );

        void CopyTo(
            __out D2D1_GRADIENT_STOP *pgradient_stop
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != GradientStop::typeid)
            {
                return false;
            }

            return *this == safe_cast<GradientStop>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + position.GetHashCode();
            hashCode = hashCode * 31 + color.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Stores an ordered pair of integers, typically the width and height of a rectangle.
    /// <para>(Also see DirectX SDK: D2D1_SIZE_U)</para>
    /// </summary>
    public value struct SizeU
    {
    public:


        /// <summary>
        /// Constructor for the SizeU value type
        /// </summary>
        /// <param name="width">Initializes the Width property.</param>
        /// <param name="height">Initializes the Height property.</param>
        SizeU(
            UINT32 width,
            UINT32 height
            );


        /// <summary>
        /// The horizontal component of this size.
        /// </summary>
        property UINT32 Width
        {
            UINT32 get()
            {
                return width;
            }

            void set(UINT32 value)
            {
                width = value;
            }
        }

        /// <summary>
        /// The vertical component of this size.
        /// </summary>
        property UINT32 Height
        {
            UINT32 get()
            {
                return height;
            }

            void set(UINT32 value)
            {
                height = value;
            }
        }

        static Boolean operator == ( SizeU size1, SizeU size2 )
        {
            return 
                (size1.Width == size2.Width)  &&
                (size1.Height == size2.Height);
        }

        static Boolean operator != ( SizeU size1, SizeU size2 )
        {
            return !(size1 == size2);
        }

    private:

        UINT32 width;
        UINT32 height;

    internal:

        void CopyFrom(
            __in const D2D1_SIZE_U &size_u
            );

        void CopyTo(
            __out D2D1_SIZE_U *psize_u
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != SizeU::typeid)
            {
                return false;
            }

            return *this == safe_cast<SizeU>(obj);
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
    /// Contains the window handle, pixel size, and presentation options for an HwndRenderTarget.
    /// <para>(Also see DirectX SDK: D2D1_HWND_RENDER_TARGET_PROPERTIES)</para>
    /// </summary>
    public value struct HwndRenderTargetProperties
    {
    public:

        /// <summary>
        /// Constructor for the HwndRenderTargetProperties value type
        /// </summary>
        /// <param name="windowHandle">Initializes the WindowHandle property.</param>
        /// <param name="pixelSize">Initializes the PixelSize property.</param>
        /// <param name="presentOptions">Initializes the PresentOptions property.</param>
        HwndRenderTargetProperties(
            IntPtr windowHandle,
            SizeU pixelSize,
            Direct2D1::PresentOptions presentOptions
            );


        /// <summary>
        /// A handle to the windows to which the render target issues the output from its drawing commands.
        /// </summary>
        property IntPtr WindowHandle
        {
            IntPtr get()
            {
                return windowHandle;
            }

            void set(IntPtr value)
            {
                windowHandle = value;
            }
        }

        /// <summary>
        /// The size of the render target, in pixels.
        /// </summary>
        property SizeU PixelSize
        {
            SizeU get()
            {
                return pixelSize;
            }

            void set(SizeU value)
            {
                pixelSize = value;
            }
        }

        /// <summary>
        /// A value that specifies whether the render target retains the frame after it is presented and whether the render target waits for the device to refresh before presenting.
        /// </summary>
        property Direct2D1::PresentOptions PresentOptions
        {
            Direct2D1::PresentOptions get()
            {
                return presentOptions;
            }

            void set(Direct2D1::PresentOptions value)
            {
                presentOptions = value;
            }
        }

    private:

        IntPtr windowHandle;
        SizeU pixelSize;
        Direct2D1::PresentOptions presentOptions;

    internal:

        void CopyFrom(
            __in const D2D1_HWND_RENDER_TARGET_PROPERTIES &hwnd_render_target_properties
            );

        void CopyTo(
            __out D2D1_HWND_RENDER_TARGET_PROPERTIES *phwnd_render_target_properties
            );

    };



    /// <summary>
    /// Represents a rectangle defined by the coordinates of the upper-left corner (left, top) and the coordinates of the lower-right corner (right, bottom). 
    /// <para>(Also see DirectX SDK: D2D1_RECT_F)</para>
    /// </summary>
    public value struct RectF
    {
    public:

        /// <summary>
        /// Constructor for the RectF value type
        /// </summary>
        /// <param name="left">Initializes the Left property.</param>
        /// <param name="top">Initializes the Top property.</param>
        /// <param name="right">Initializes the Right property.</param>
        /// <param name="bottom">Initializes the Bottom property.</param>
        RectF(
            FLOAT left,
            FLOAT top,
            FLOAT right,
            FLOAT bottom
            );

        /// <summary>
        /// The x-coordinate of the upper-left corner of the rectangle.
        /// </summary>
        property FLOAT Left
        {
            FLOAT get()
            {
                return left;
            }

            void set(FLOAT value)
            {
                left = value;
            }
        }

        /// <summary>
        /// The y-coordinate of the upper-left corner of the rectangle. 
        /// </summary>
        property FLOAT Top
        {
            FLOAT get()
            {
                return top;
            }

            void set(FLOAT value)
            {
                top = value;
            }
        }

        /// <summary>
        /// The x-coordinate of the lower-right corner of the rectangle. 
        /// </summary>
        property FLOAT Right
        {
            FLOAT get()
            {
                return right;
            }

            void set(FLOAT value)
            {
                right = value;
            }
        }

        /// <summary>
        /// The y-coordinate of the lower-right corner of the rectangle. 
        /// </summary>
        property FLOAT Bottom
        {
            FLOAT get()
            {
                return bottom;
            }

            void set(FLOAT value)
            {
                bottom = value;
            }
        }

        /// <summary>
        /// The height of this rectangle. 
        /// </summary>
        /// <remarks>
        /// Changing the Height property will also cause a change in the Bottom value.
        /// </remarks>
        property FLOAT Height
        {
            FLOAT get()
            {
                return Math::Abs(Bottom - Top);
            }

            void set(FLOAT value)
            {
                Bottom = Top + value;
            }

        }

        /// <summary>
        /// Retrieve the width of this rectangle. 
        /// </summary>
        /// <remarks>
        /// Changing the Width property will also cause a change in the Right value.
        /// </remarks>
        property FLOAT Width
        {
            FLOAT get()
            {
                return Math::Abs(Left - Right);
            }

            void set(FLOAT value)
            {
                Right = Left + value;
            }
        }


        static Boolean operator == ( RectF rect1, RectF rect2 )
        {
            return 
                (rect1.Left == rect2.Left)  &&
                (rect1.Top == rect2.Top)  &&
                (rect1.Right == rect2.Right)  &&
                (rect1.Bottom == rect2.Bottom);
        }

        static Boolean operator != ( RectF rect1, RectF rect2 )
        {
            return !(rect1 == rect2);
        }


    private:

        FLOAT left;
        FLOAT top;
        FLOAT right;
        FLOAT bottom;

    internal:

        void CopyFrom(
            __in const D2D1_RECT_F &rect_f
            );

        void CopyTo(
            __out D2D1_RECT_F *prect_f
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != RectF::typeid)
            {
                return false;
            }

            return *this == safe_cast<RectF>(obj);
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



    /// <summary>
    /// Contains the content bounds, mask information, opacity settings, and other options for a layer resource. 
    /// <para>(Also see DirectX SDK: D2D1_LAYER_PARAMETERS)</para>
    /// </summary>
    public value struct LayerParameters
    {
    public:


        /// <summary>
        /// Constructor for the LayerParameters value type
        /// </summary>
        /// <param name="contentBounds">Initializes the ContentBounds property.</param>
        /// <param name="geometricMask">Initializes the GeometricMask property.</param>
        /// <param name="maskAntiAliasMode">Initializes the MaskAntiAliasMode property.</param>
        /// <param name="maskTransform">Initializes the MaskTransform property.</param>
        /// <param name="opacity">Initializes the Opacity property.</param>
        /// <param name="opacityBrush">Initializes the OpacityBrush property.</param>
        /// <param name="layerOptions">Initializes the LayerOptions property.</param>
        LayerParameters(
            RectF contentBounds,
            Geometry ^geometricMask,
            AntiAliasMode maskAntiAliasMode,
            Matrix3x2F maskTransform,
            FLOAT opacity,
            Brush ^opacityBrush,
            LayerOptions layerOptions
            );


        /// <summary>
        /// The rectangular clip that will be applied to the layer. The clip is affected by the
        /// world transform. Content outside these bounds is not guaranteed to render.
        /// </summary>
        property RectF ContentBounds
        {
            RectF get()
            {
                return contentBounds;
            }

            void set(RectF value)
            {
                contentBounds = value;
            }
        }

        /// <summary>
        /// The geometric mask specifies the area of the layer that is composited into the render target.
        /// </summary>
        property Geometry ^ GeometricMask
        {
            Geometry ^ get()
            {
                return geometricMask;
            }

            void set(Geometry ^ value)
            {
                geometricMask = value;
            }
        }

        /// <summary>
        /// A value that specifies the antialiasing mode for the geometricMask. 
        /// </summary>
        property AntiAliasMode MaskAntiAliasMode
        {
            AntiAliasMode get()
            {
                return maskAntiAliasMode;
            }

            void set(AntiAliasMode value)
            {
                maskAntiAliasMode = value;
            }
        }

        /// <summary>
        /// A value that specifies the transform that is applied to the geometric mask when composing the layer.
        /// </summary>
        property Matrix3x2F MaskTransform
        {
            Matrix3x2F get()
            {
                return maskTransform;
            }

            void set(Matrix3x2F value)
            {
                maskTransform = value;
            }
        }

        /// <summary>
        /// An opacity value that is applied uniformly to all resources in the layer when compositing to the target.
        /// </summary>
        property FLOAT Opacity
        {
            FLOAT get()
            {
                return opacity;
            }

            void set(FLOAT value)
            {
                opacity = value;
            }
        }

        /// <summary>
        /// A brush that is used to modify the opacity of the layer. The brush is mapped to the layer, 
        /// and the alpha channel of each mapped brush pixel is multiplied against the corresponding layer pixel.
        /// </summary>
        property Brush ^ OpacityBrush
        {
            Brush ^ get()
            {
                return opacityBrush;
            }

            void set(Brush ^ value)
            {
                opacityBrush = value;
            }
        }

        /// <summary>
        /// A value that specifies whether the layer intends to render text with ClearType antialiasing
        /// </summary>
        property LayerOptions Options
        {
            LayerOptions get()
            {
                return options;
            }

            void set(LayerOptions value)
            {
                options = value;
            }
        }

    private:

        RectF contentBounds;
        Geometry ^ geometricMask;
        AntiAliasMode maskAntiAliasMode;
        Matrix3x2F maskTransform;
        FLOAT opacity;
        Brush ^ opacityBrush;
        LayerOptions options;

    internal:

        void CopyFrom(
            __in const D2D1_LAYER_PARAMETERS &layer_parameters
            );

        void CopyTo(
            __out D2D1_LAYER_PARAMETERS *player_parameters
            );

    };


    /// <summary>
    /// Contains the control point and end point for a quadratic Bezier segment.
    /// <para>(Also see DirectX SDK: D2D1_QUADRATIC_BEZIER_SEGMENT)</para>
    /// </summary>
    public value struct QuadraticBezierSegment
    {
    public:


        /// <summary>
        /// Constructor for the QuadraticBezierSegment value type
        /// </summary>
        /// <param name="point1">Initializes the Point1 property.</param>
        /// <param name="point2">Initializes the Point2 property.</param>
        QuadraticBezierSegment(
            Point2F point1,
            Point2F point2
            );


        /// <summary>
        /// The control point of the quadratic Bezier segment.
        /// </summary>
        property Point2F Point1
        {
            Point2F get()
            {
                return point1;
            }

            void set(Point2F value)
            {
                point1 = value;
            }
        }

        /// <summary>
        /// The end point of the quadratic Bezier segment.
        /// </summary>
        property Point2F Point2
        {
            Point2F get()
            {
                return point2;
            }

            void set(Point2F value)
            {
                point2 = value;
            }
        }

        static Boolean operator == ( QuadraticBezierSegment segment1, QuadraticBezierSegment segment2 )
        {
            return 
                (segment1.Point1 == segment2.Point1)  &&
                (segment1.Point2 == segment2.Point2);
        }

        static Boolean operator != ( QuadraticBezierSegment segment1, QuadraticBezierSegment segment2 )
        {
            return !(segment1 == segment2);
        }

    private:

        Point2F point1;
        Point2F point2;

    internal:

        void CopyFrom(
            __in const D2D1_QUADRATIC_BEZIER_SEGMENT &quadratic_bezier_segment
            );

        void CopyTo(
            __out D2D1_QUADRATIC_BEZIER_SEGMENT *pquadratic_bezier_segment
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != QuadraticBezierSegment::typeid)
            {
                return false;
            }

            return *this == safe_cast<QuadraticBezierSegment>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + point1.GetHashCode();
            hashCode = hashCode * 31 + point2.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Contains the dimensions and corner radii of a rounded rectangle.
    /// <para>(Also see DirectX SDK: D2D1_ROUNDED_RECT)</para>
    /// </summary>
    public value struct RoundedRect
    {
    public:

        /// <summary>
        /// Constructor for the RoundedRect value type
        /// </summary>
        /// <param name="rect">Initializes the Rect property.</param>
        /// <param name="radiusX">Initializes the RadiusX property.</param>
        /// <param name="radiusY">Initializes the RadiusY property.</param>
        RoundedRect(
            RectF rect,
            FLOAT radiusX,
            FLOAT radiusY
            );


        /// <summary>
        /// The coordinates of the rectangle.
        /// </summary>
        property RectF Rect
        {
            RectF get()
            {
                return rect;
            }

            void set(RectF value)
            {
                rect = value;
            }
        }

        /// <summary>
        /// The x-radius for the quarter ellipse that is drawn to replace every corner of the rectangle.
        /// </summary>
        property FLOAT RadiusX
        {
            FLOAT get()
            {
                return radiusX;
            }

            void set(FLOAT value)
            {
                radiusX = value;
            }
        }

        /// <summary>
        /// The y-radius for the quarter ellipse that is drawn to replace every corner of the rectangle.
        /// </summary>
        property FLOAT RadiusY
        {
            FLOAT get()
            {
                return radiusY;
            }

            void set(FLOAT value)
            {
                radiusY = value;
            }
        }

        static Boolean operator == ( RoundedRect rect1, RoundedRect rect2 )
        {
            return 
                (rect1.Rect == rect2.Rect)  &&
                (rect1.RadiusX == rect2.RadiusX)  &&
                (rect1.RadiusY == rect2.RadiusY);
        }

        static Boolean operator != ( RoundedRect rect1, RoundedRect rect2 )
        {
            return !(rect1 == rect2);
        }

    private:

        RectF rect;
        FLOAT radiusX;
        FLOAT radiusY;

    internal:

        void CopyFrom(
            __in const D2D1_ROUNDED_RECT &rounded_rect
            );

        void CopyTo(
            __out D2D1_ROUNDED_RECT *prounded_rect
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != RoundedRect::typeid)
            {
                return false;
            }

            return *this == safe_cast<RoundedRect>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + rect.GetHashCode();
            hashCode = hashCode * 31 + radiusX.GetHashCode();
            hashCode = hashCode * 31 + radiusY.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Describes the opacity and transformation of a brush.
    /// <para>(Also see DirectX SDK: D2D1_BRUSH_PROPERTIES)</para>
    /// </summary>
    public value struct BrushProperties
    {
    public:


        /// <summary>
        /// Constructor for the BrushProperties value type
        /// </summary>
        /// <param name="opacity">Initializes the Opacity property.</param>
        /// <param name="transform">Initializes the Transform property.</param>
        BrushProperties(
            FLOAT opacity,
            Matrix3x2F transform
            );


        /// <summary>
        /// A value between 0.0f and 1.0f, inclusive, that specifies the degree of opacity of the brush.
        /// </summary>
        property FLOAT Opacity
        {
            FLOAT get()
            {
                return opacity;
            }

            void set(FLOAT value)
            {
                opacity = value;
            }
        }

        /// <summary>
        /// The transformation that is applied to the brush.
        /// </summary>
        property Matrix3x2F Transform
        {
            Matrix3x2F get()
            {
                return transform;
            }

            void set(Matrix3x2F value)
            {
                transform = value;
            }
        }

    private:

        FLOAT opacity;
        Matrix3x2F transform;

    internal:

        void CopyFrom(
            __in const D2D1_BRUSH_PROPERTIES &brush_properties
            );

        void CopyTo(
            __out D2D1_BRUSH_PROPERTIES *pbrush_properties
            );

    public:

        static Boolean operator == (BrushProperties brushProperties1, BrushProperties brushProperties2)
        {
            return (brushProperties1.opacity == brushProperties2.opacity) &&
                (brushProperties1.transform == brushProperties2.transform);
        }

        static Boolean operator != (BrushProperties brushProperties1, BrushProperties brushProperties2)
        {
            return !(brushProperties1 == brushProperties2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != BrushProperties::typeid)
            {
                return false;
            }

            return *this == safe_cast<BrushProperties>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + opacity.GetHashCode();
            hashCode = hashCode * 31 + transform.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Contains the starting point and endpoint of the gradient axis for a LinearGradientBrush.
    /// <para>(Also see DirectX SDK: D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES)</para>
    /// </summary>
    public value struct LinearGradientBrushProperties
    {
    public:


        /// <summary>
        /// Constructor for the LinearGradientBrushProperties value type
        /// </summary>
        /// <param name="startPoint">Initializes the StartPoint property.</param>
        /// <param name="endPoint">Initializes the EndPoint property.</param>
        LinearGradientBrushProperties(
            Point2F startPoint,
            Point2F endPoint
            );


        /// <summary>
        /// In the brush's coordinate space, the starting point of the gradient axis.
        /// </summary>
        property Point2F StartPoint
        {
            Point2F get()
            {
                return startPoint;
            }

            void set(Point2F value)
            {
                startPoint = value;
            }
        }

        /// <summary>
        /// In the brush's coordinate space, the endpoint of the gradient axis.
        /// </summary>
        property Point2F EndPoint
        {
            Point2F get()
            {
                return endPoint;
            }

            void set(Point2F value)
            {
                endPoint = value;
            }
        }

    private:

        Point2F startPoint;
        Point2F endPoint;

    internal:

        void CopyFrom(
            __in const D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES &linear_gradient_brush_properties
            );

        void CopyTo(
            __out D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES *plinear_gradient_brush_properties
            );

    public:

        static Boolean operator == (LinearGradientBrushProperties linearGradientBrushProperties1, LinearGradientBrushProperties linearGradientBrushProperties2)
        {
            return (linearGradientBrushProperties1.startPoint == linearGradientBrushProperties2.startPoint) &&
                (linearGradientBrushProperties1.endPoint == linearGradientBrushProperties2.endPoint);
        }

        static Boolean operator != (LinearGradientBrushProperties linearGradientBrushProperties1, LinearGradientBrushProperties linearGradientBrushProperties2)
        {
            return !(linearGradientBrushProperties1 == linearGradientBrushProperties2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != LinearGradientBrushProperties::typeid)
            {
                return false;
            }

            return *this == safe_cast<LinearGradientBrushProperties>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + startPoint.GetHashCode();
            hashCode = hashCode * 31 + endPoint.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Describes the extend modes and the interpolation mode of a BitmapBrush.
    /// <para>(Also see DirectX SDK: D2D1_BITMAP_BRUSH_PROPERTIES)</para>
    /// </summary>
    public value struct BitmapBrushProperties
    {
    public:


        /// <summary>
        /// Constructor for the BitmapBrushProperties value type
        /// </summary>
        /// <param name="extendModeX">Initializes the ExtendModeX property.</param>
        /// <param name="extendModeY">Initializes the ExtendModeY property.</param>
        /// <param name="interpolationMode">Initializes the InterpolationMode property.</param>
        BitmapBrushProperties(
            ExtendMode extendModeX,
            ExtendMode extendModeY,
            BitmapInterpolationMode interpolationMode
            );


        /// <summary>
        /// A value that describes how the brush horizontally tiles those areas that extend past its bitmap.
        /// </summary>
        property ExtendMode ExtendModeX
        {
            ExtendMode get()
            {
                return extendModeX;
            }

            void set(ExtendMode value)
            {
                extendModeX = value;
            }
        }

        /// <summary>
        /// A value that describes how the brush vertically tiles those areas that extend past its bitmap.
        /// </summary>
        property ExtendMode ExtendModeY
        {
            ExtendMode get()
            {
                return extendModeY;
            }

            void set(ExtendMode value)
            {
                extendModeY = value;
            }
        }

        /// <summary>
        /// A value that specifies how the bitmap is interpolated when it is scaled or rotated.
        /// </summary>
        property BitmapInterpolationMode InterpolationMode
        {
            BitmapInterpolationMode get()
            {
                return interpolationMode;
            }

            void set(BitmapInterpolationMode value)
            {
                interpolationMode = value;
            }
        }

    private:

        ExtendMode extendModeX;
        ExtendMode extendModeY;
        BitmapInterpolationMode interpolationMode;

    internal:

        void CopyFrom(
            __in const D2D1_BITMAP_BRUSH_PROPERTIES &bitmap_brush_properties
            );

        void CopyTo(
            __out D2D1_BITMAP_BRUSH_PROPERTIES *pbitmap_brush_properties
            );

    public:

        static Boolean operator == (BitmapBrushProperties bitmapBrushProperties1, BitmapBrushProperties bitmapBrushProperties2)
        {
            return (bitmapBrushProperties1.extendModeX == bitmapBrushProperties2.extendModeX) &&
                (bitmapBrushProperties1.extendModeY == bitmapBrushProperties2.extendModeY) &&
                (bitmapBrushProperties1.interpolationMode == bitmapBrushProperties2.interpolationMode);
        }

        static Boolean operator != (BitmapBrushProperties bitmapBrushProperties1, BitmapBrushProperties bitmapBrushProperties2)
        {
            return !(bitmapBrushProperties1 == bitmapBrushProperties2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != BitmapBrushProperties::typeid)
            {
                return false;
            }

            return *this == safe_cast<BitmapBrushProperties>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + extendModeX.GetHashCode();
            hashCode = hashCode * 31 + extendModeY.GetHashCode();
            hashCode = hashCode * 31 + interpolationMode.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Contains the data format and alpha mode for a bitmap or render target. 
    /// <para>(Also see DirectX SDK: D2D1_PIXEL_FORMAT)</para>
    /// </summary>
    public value struct PixelFormat
    {
    public:

        /// <summary>
        /// Constructor for the PixelFormat value type
        /// </summary>
        /// <param name="format">Initializes the Format property.</param>
        /// <param name="alphaMode">Initializes the AlphaMode property.</param>
        PixelFormat(
            Graphics::Format format,
            Direct2D1::AlphaMode alphaMode
            );


        /// <summary>
        /// A value that specifies the size and arrangement of channels in each pixel.
        /// </summary>
        property Format Format
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
        /// A value that specifies whether the alpha channel is using pre-multiplied alpha, straight alpha, whether it should be ignored and considered opaque, or whether it is unknown. 
        /// </summary>
        property Direct2D1::AlphaMode AlphaMode
        {
            Direct2D1::AlphaMode get()
            {
                return alphaMode;
            }

            void set(Direct2D1::AlphaMode value)
            {
                alphaMode = value;
            }
        }

        static Boolean operator == ( PixelFormat pixelFormat1, PixelFormat pixelFormat2 )
        {
            return 
                (pixelFormat1.Format == pixelFormat2.Format)  &&
                (pixelFormat1.AlphaMode == pixelFormat2.AlphaMode);
        }

        static Boolean operator != ( PixelFormat pixelFormat1, PixelFormat pixelFormat2 )
        {
            return !(pixelFormat1 == pixelFormat2);
        }

    private:

        Graphics::Format format;
        Direct2D1::AlphaMode alphaMode;

    internal:

        void CopyFrom(
            __in const D2D1_PIXEL_FORMAT &pixel_format
            );

        void CopyTo(
            __out D2D1_PIXEL_FORMAT *ppixel_format
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != PixelFormat::typeid)
            {
                return false;
            }

            return *this == safe_cast<PixelFormat>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + format.GetHashCode();
            hashCode = hashCode * 31 + alphaMode.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Properties, aside from the width, that allow geometric penning to be specified.
    /// <para>(Also see DirectX SDK: D2D1_STROKE_STYLE_PROPERTIES)</para>
    /// </summary>
    public value struct StrokeStyleProperties
    {
    public:


        /// <summary>
        /// Constructor for the StrokeStyleProperties value type
        /// </summary>
        /// <param name="startCap">Initializes the StartCap property.</param>
        /// <param name="endCap">Initializes the EndCap property.</param>
        /// <param name="dashCap">Initializes the DashCap property.</param>
        /// <param name="lineJoin">Initializes the LineJoin property.</param>
        /// <param name="miterLimit">Initializes the MiterLimit property.</param>
        /// <param name="dashStyle">Initializes the DashStyle property.</param>
        /// <param name="dashOffset">Initializes the DashOffset property.</param>
        StrokeStyleProperties(
            CapStyle startCap,
            CapStyle endCap,
            CapStyle dashCap,
            LineJoin lineJoin,
            FLOAT miterLimit,
            DashStyle dashStyle,
            FLOAT dashOffset
            );


        /// <summary>
        /// The cap applied to the start of all the open figures in a stroked geometry.
        /// </summary>
        property CapStyle StartCap
        {
            CapStyle get()
            {
                return startCap;
            }

            void set(CapStyle value)
            {
                startCap = value;
            }
        }

        /// <summary>
        /// The cap applied to the end of all the open figures in a stroked geometry.
        /// </summary>
        property CapStyle EndCap
        {
            CapStyle get()
            {
                return endCap;
            }

            void set(CapStyle value)
            {
                endCap = value;
            }
        }

        /// <summary>
        /// The shape at either end of each dash segment.
        /// </summary>
        property CapStyle DashCap
        {
            CapStyle get()
            {
                return dashCap;
            }

            void set(CapStyle value)
            {
                dashCap = value;
            }
        }

        /// <summary>
        /// A value that describes how segments are joined. This value is ignored for a vertex if the segment flags specify that the segment should have a smooth join.
        /// </summary>
        property LineJoin LineJoin
        {
            Direct2D1::LineJoin get()
            {
                return lineJoin;
            }

            void set(Direct2D1::LineJoin value)
            {
                lineJoin = value;
            }
        }

        /// <summary>
        /// The limit of the thickness of the join on a mitered corner. This value is always treated as though it is greater than or equal to 1.0f.
        /// </summary>
        property FLOAT MiterLimit
        {
            FLOAT get()
            {
                return miterLimit;
            }

            void set(FLOAT value)
            {
                miterLimit = value;
            }
        }

        /// <summary>
        /// A value that specifies whether the stroke has a dash pattern and, if so, the dash style.
        /// </summary>
        property DashStyle DashStyle
        {
            Direct2D1::DashStyle get()
            {
                return dashStyle;
            }

            void set(Direct2D1::DashStyle value)
            {
                dashStyle = value;
            }
        }

        /// <summary>
        /// A value that specifies an offset in the dash sequence. A positive dash offset value shifts the dash pattern, in units of stroke width, toward the start of the stroked geometry. A negative dash offset value shifts the dash pattern, in units of stroke width, toward the end of the stroked geometry.
        /// </summary>
        property FLOAT DashOffset
        {
            FLOAT get()
            {
                return dashOffset;
            }

            void set(FLOAT value)
            {
                dashOffset = value;
            }
        }

    private:

        CapStyle startCap;
        CapStyle endCap;
        CapStyle dashCap;
        Direct2D1::LineJoin lineJoin;
        FLOAT miterLimit;
        Direct2D1::DashStyle dashStyle;
        FLOAT dashOffset;

    internal:

        void CopyFrom(
            __in const D2D1_STROKE_STYLE_PROPERTIES &stroke_style_properties
            );

        void CopyTo(
            __out D2D1_STROKE_STYLE_PROPERTIES *pstroke_style_properties
            );

    public:

        static Boolean operator == (StrokeStyleProperties strokeStyleProperties1, StrokeStyleProperties strokeStyleProperties2)
        {
            return (strokeStyleProperties1.startCap == strokeStyleProperties2.startCap) &&
                (strokeStyleProperties1.endCap == strokeStyleProperties2.endCap) &&
                (strokeStyleProperties1.dashCap == strokeStyleProperties2.dashCap) &&
                (strokeStyleProperties1.lineJoin == strokeStyleProperties2.lineJoin) &&
                (strokeStyleProperties1.miterLimit == strokeStyleProperties2.miterLimit) &&
                (strokeStyleProperties1.dashStyle == strokeStyleProperties2.dashStyle) &&
                (strokeStyleProperties1.dashOffset == strokeStyleProperties2.dashOffset);
        }

        static Boolean operator != (StrokeStyleProperties strokeStyleProperties1, StrokeStyleProperties strokeStyleProperties2)
        {
            return !(strokeStyleProperties1 == strokeStyleProperties2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != StrokeStyleProperties::typeid)
            {
                return false;
            }

            return *this == safe_cast<StrokeStyleProperties>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + startCap.GetHashCode();
            hashCode = hashCode * 31 + endCap.GetHashCode();
            hashCode = hashCode * 31 + dashCap.GetHashCode();
            hashCode = hashCode * 31 + lineJoin.GetHashCode();
            hashCode = hashCode * 31 + miterLimit.GetHashCode();
            hashCode = hashCode * 31 + dashStyle.GetHashCode();
            hashCode = hashCode * 31 + dashOffset.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Contains the gradient origin offset and the size and position of the gradient ellipse for a RadialGradientBrush.
    /// <para>(Also see DirectX SDK: D2D1_RADIAL_GRADIENT_BRUSH_PROPERTIES)</para>
    /// </summary>
    public value struct RadialGradientBrushProperties
    {
    public:

        /// <summary>
        /// Constructor for the RadialGradientBrushProperties value type
        /// </summary>
        /// <param name="center">Initializes the Center property.</param>
        /// <param name="gradientOriginOffset">Initializes the GradientOriginOffset property.</param>
        /// <param name="radiusX">Initializes the RadiusX property.</param>
        /// <param name="radiusY">Initializes the RadiusY property.</param>
        RadialGradientBrushProperties(
            Point2F center,
            Point2F gradientOriginOffset,
            FLOAT radiusX,
            FLOAT radiusY
            );


        /// <summary>
        /// In the brush's coordinate space, the center of the gradient ellipse.
        /// </summary>
        property Point2F Center
        {
            Point2F get()
            {
                return center;
            }

            void set(Point2F value)
            {
                center = value;
            }
        }

        /// <summary>
        /// In the brush's coordinate space, the offset of the gradient origin relative to the gradient ellipse's center.
        /// </summary>
        property Point2F GradientOriginOffset
        {
            Point2F get()
            {
                return gradientOriginOffset;
            }

            void set(Point2F value)
            {
                gradientOriginOffset = value;
            }
        }

        /// <summary>
        /// In the brush's coordinate space, the x-radius of the gradient ellipse.
        /// </summary>
        property FLOAT RadiusX
        {
            FLOAT get()
            {
                return radiusX;
            }

            void set(FLOAT value)
            {
                radiusX = value;
            }
        }

        /// <summary>
        /// In the brush's coordinate space, the y-radius of the gradient ellipse.
        /// </summary>
        property FLOAT RadiusY
        {
            FLOAT get()
            {
                return radiusY;
            }

            void set(FLOAT value)
            {
                radiusY = value;
            }
        }

    private:

        Point2F center;
        Point2F gradientOriginOffset;
        FLOAT radiusX;
        FLOAT radiusY;

    internal:

        void CopyFrom(
            __in const D2D1_RADIAL_GRADIENT_BRUSH_PROPERTIES &radial_gradient_brush_properties
            );

        void CopyTo(
            __out D2D1_RADIAL_GRADIENT_BRUSH_PROPERTIES *pradial_gradient_brush_properties
            );

    public:

        static Boolean operator == (RadialGradientBrushProperties radialGradientBrushProperties1, RadialGradientBrushProperties radialGradientBrushProperties2)
        {
            return (radialGradientBrushProperties1.center == radialGradientBrushProperties2.center) &&
                (radialGradientBrushProperties1.gradientOriginOffset == radialGradientBrushProperties2.gradientOriginOffset) &&
                (radialGradientBrushProperties1.radiusX == radialGradientBrushProperties2.radiusX) &&
                (radialGradientBrushProperties1.radiusY == radialGradientBrushProperties2.radiusY);
        }

        static Boolean operator != (RadialGradientBrushProperties radialGradientBrushProperties1, RadialGradientBrushProperties radialGradientBrushProperties2)
        {
            return !(radialGradientBrushProperties1 == radialGradientBrushProperties2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != RadialGradientBrushProperties::typeid)
            {
                return false;
            }

            return *this == safe_cast<RadialGradientBrushProperties>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + center.GetHashCode();
            hashCode = hashCode * 31 + gradientOriginOffset.GetHashCode();
            hashCode = hashCode * 31 + radiusX.GetHashCode();
            hashCode = hashCode * 31 + radiusY.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Contains rendering options (hardware or software), pixel format, DPI information, remoting options, and Direct3D support requirements for a render target. 
    /// <para>(Also see DirectX SDK: D2D1_RENDER_TARGET_PROPERTIES)</para>
    /// </summary>
    public value struct RenderTargetProperties
    {
    public:

        /// <summary>
        /// Constructor for the RenderTargetProperties value type
        /// </summary>
        /// <param name="type">Initializes the Type property.</param>
        /// <param name="pixelFormat">Initializes the PixelFormat property.</param>
        /// <param name="dpiX">Initializes the DpiX property.</param>
        /// <param name="dpiY">Initializes the DpiY property.</param>
        /// <param name="usage">Initializes the Usage property.</param>
        /// <param name="minLevel">Initializes the MinLevel property.</param>
        RenderTargetProperties(
            RenderTargetType type,
            PixelFormat pixelFormat,
            FLOAT dpiX,
            FLOAT dpiY,
            RenderTargetUsages usage,
            FeatureLevel minLevel
            );


        /// <summary>
        /// A value that specifies whether the render target should force hardware or software rendering. 
        /// A value of Default specifies that the render target should use hardware rendering if it is available; 
        /// otherwise, it uses software rendering. Note that WIC bitmap render targets do not support hardware rendering.
        /// </summary>
        property RenderTargetType RenderTargetType
        {
            ::Microsoft::WindowsAPICodePack::DirectX::Direct2D1::RenderTargetType get()
            {
                return type;
            }

            void set(::Microsoft::WindowsAPICodePack::DirectX::Direct2D1::RenderTargetType value)
            {
                type = value;
            }
        }

        /// <summary>
        /// The pixel format and alpha mode of the render target.
        /// </summary>
        property PixelFormat PixelFormat
        {
            Direct2D1::PixelFormat get()
            {
                return pixelFormat;
            }

            void set(Direct2D1::PixelFormat value)
            {
                pixelFormat = value;
            }
        }

        /// <summary>
        /// The horizontal DPI of the render target. To use the default DPI, set dpiX and dpiY to 0.
        /// </summary>
        property FLOAT DpiX
        {
            FLOAT get()
            {
                return dpiX;
            }

            void set(FLOAT value)
            {
                dpiX = value;
            }
        }

        /// <summary>
        /// The verical DPI of the render target. To use the default DPI, set dpiX and dpiY to 0.
        /// </summary>
        property FLOAT DpiY
        {
            FLOAT get()
            {
                return dpiY;
            }

            void set(FLOAT value)
            {
                dpiY = value;
            }
        }

        /// <summary>
        /// A value that specifies how the render target is remoted and whether it should be GDI-compatible. 
        /// Set to None to create a render target that is not compatible with GDI and uses Direct3D command-stream 
        /// remoting if it is available.
        /// </summary>
        property RenderTargetUsages Usage
        {
            RenderTargetUsages get()
            {
                return usage;
            }

            void set(RenderTargetUsages value)
            {
                usage = value;
            }
        }

        /// <summary>
        /// A value that specifies the minimum Direct3D feature level required for hardware rendering. 
        /// If the specified minimum level is not available, the render target uses software rendering 
        /// if the type member is set to Default; if type is set to to Hardware, render target creation fails. 
        /// A value of Default indicates that Direct2D should determine whether the Direct3D feature level of 
        /// the device is adequate. 
        /// This property is used only when creating HwndRenderTarget and DCRenderTarget objects.
        /// </summary>
        property FeatureLevel MinLevel
        {
            FeatureLevel get()
            {
                return minLevel;
            }

            void set(FeatureLevel value)
            {
                minLevel = value;
            }
        }

    private:

        ::Microsoft::WindowsAPICodePack::DirectX::Direct2D1::RenderTargetType type;
        Direct2D1::PixelFormat pixelFormat;
        FLOAT dpiX;
        FLOAT dpiY;
        RenderTargetUsages usage;
        FeatureLevel minLevel;

    internal:

        void CopyFrom(
            __in const D2D1_RENDER_TARGET_PROPERTIES &render_target_properties
            );

        void CopyTo(
            __out D2D1_RENDER_TARGET_PROPERTIES *prender_target_properties
            );

    public:

        static Boolean operator == (RenderTargetProperties renderTargetProperties1, RenderTargetProperties renderTargetProperties2)
        {
            return (renderTargetProperties1.type == renderTargetProperties2.type) &&
                (renderTargetProperties1.pixelFormat == renderTargetProperties2.pixelFormat) &&
                (renderTargetProperties1.dpiX == renderTargetProperties2.dpiX) &&
                (renderTargetProperties1.dpiY == renderTargetProperties2.dpiY) &&
                (renderTargetProperties1.usage == renderTargetProperties2.usage) &&
                (renderTargetProperties1.minLevel == renderTargetProperties2.minLevel);
        }

        static Boolean operator != (RenderTargetProperties renderTargetProperties1, RenderTargetProperties renderTargetProperties2)
        {
            return !(renderTargetProperties1 == renderTargetProperties2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != RenderTargetProperties::typeid)
            {
                return false;
            }

            return *this == safe_cast<RenderTargetProperties>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + type.GetHashCode();
            hashCode = hashCode * 31 + pixelFormat.GetHashCode();
            hashCode = hashCode * 31 + dpiX.GetHashCode();
            hashCode = hashCode * 31 + dpiY.GetHashCode();
            hashCode = hashCode * 31 + usage.GetHashCode();
            hashCode = hashCode * 31 + minLevel.GetHashCode();

            return hashCode;
        }

    };


    /// <summary>
    /// Describes the pixel format and dpi of a bitmap.
    /// <para>(Also see DirectX SDK: D2D1_BITMAP_PROPERTIES)</para>
    /// </summary>
    public value struct BitmapProperties
    {
    public:


        /// <summary>
        /// Constructor for the BitmapProperties value type
        /// </summary>
        /// <param name="pixelFormat">Initializes the PixelFormat property.</param>
        /// <param name="dpiX">Initializes the DpiX property.</param>
        /// <param name="dpiY">Initializes the DpiY property.</param>
        BitmapProperties(
            PixelFormat pixelFormat,
            FLOAT dpiX,
            FLOAT dpiY
            );


        /// <summary>
        /// The bitmap's pixel format and alpha mode.
        /// </summary>
        property PixelFormat PixelFormat
        {
            Direct2D1::PixelFormat get()
            {
                return pixelFormat;
            }

            void set(Direct2D1::PixelFormat value)
            {
                pixelFormat = value;
            }
        }

        /// <summary>
        /// The horizontal dpi of the bitmap.
        /// </summary>
        property FLOAT DpiX
        {
            FLOAT get()
            {
                return dpiX;
            }

            void set(FLOAT value)
            {
                dpiX = value;
            }
        }

        /// <summary>
        /// The vertical dpi of the bitmap.
        /// </summary>
        property FLOAT DpiY
        {
            FLOAT get()
            {
                return dpiY;
            }

            void set(FLOAT value)
            {
                dpiY = value;
            }
        }

    private:

        Direct2D1::PixelFormat pixelFormat;
        FLOAT dpiX;
        FLOAT dpiY;

    internal:

        void CopyFrom(
            __in const D2D1_BITMAP_PROPERTIES &bitmap_properties
            );

        void CopyTo(
            __out D2D1_BITMAP_PROPERTIES *pbitmap_properties
            );

    public:

        static Boolean operator == (BitmapProperties bitmapProperties1, BitmapProperties bitmapProperties2)
        {
            return (bitmapProperties1.pixelFormat == bitmapProperties2.pixelFormat) &&
                (bitmapProperties1.dpiX == bitmapProperties2.dpiX) &&
                (bitmapProperties1.dpiY == bitmapProperties2.dpiY);
        }

        static Boolean operator != (BitmapProperties bitmapProperties1, BitmapProperties bitmapProperties2)
        {
            return !(bitmapProperties1 == bitmapProperties2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != BitmapProperties::typeid)
            {
                return false;
            }

            return *this == safe_cast<BitmapProperties>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + pixelFormat.GetHashCode();
            hashCode = hashCode * 31 + dpiX.GetHashCode();
            hashCode = hashCode * 31 + dpiY.GetHashCode();

            return hashCode;
        }

    };


    /// <summary>
    /// Represents an x-coordinate and y-coordinate pair, expressed as an unsigned 32-bit integer value, in two-dimensional space.
    /// <para>(Also see DirectX SDK: D2D1_POINT_2U)</para>
    /// </summary>
    public value struct Point2U
    {
    public:


        /// <summary>
        /// Constructor for the Point2U value type
        /// </summary>
        /// <param name="x">Initializes the X property.</param>
        /// <param name="y">Initializes the Y property.</param>
        Point2U(
            UINT32 x,
            UINT32 y
            );


        /// <summary>
        /// The x-coordinate value of the point. 
        /// </summary>
        property UINT32 X
        {
            UINT32 get()
            {
                return x;
            }

            void set(UINT32 value)
            {
                x = value;
            }
        }

        /// <summary>
        /// The y-coordinate value of the point. 
        /// </summary>
        property UINT32 Y
        {
            UINT32 get()
            {
                return y;
            }

            void set(UINT32 value)
            {
                y = value;
            }
        }

        static Boolean operator == ( Point2U point1, Point2U point2 )
        {
            return 
                (point1.X == point2.X)  &&
                (point1.Y == point2.Y);
        }

        static Boolean operator != ( Point2U point1, Point2U point2 )
        {
            return !(point1 == point2);
        }

    private:

        UINT32 x;
        UINT32 y;

    internal:

        void CopyFrom(
            __in const D2D1_POINT_2U &point_2u
            );

        void CopyTo(
            __out D2D1_POINT_2U *ppoint_2u
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != Point2U::typeid)
            {
                return false;
            }

            return *this == safe_cast<Point2U>(obj);
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
    /// Represents a rectangle defined by the upper-left corner pair of coordinates (left,top) 
    /// and the lower-right corner pair of coordinates (right, bottom). 
    /// These coordinates are expressed as a 32-bit unsigned integer values.
    /// <para>(Also see DirectX SDK: D2D1_RECT_U)</para>
    /// </summary>
    public value struct RectU
    {
    public:

        /// <summary>
        /// Constructor for the RectU value type
        /// </summary>
        /// <param name="left">Initializes the Left property.</param>
        /// <param name="top">Initializes the Top property.</param>
        /// <param name="right">Initializes the Right property.</param>
        /// <param name="bottom">Initializes the Bottom property.</param>
        RectU(
            UINT32 left,
            UINT32 top,
            UINT32 right,
            UINT32 bottom
            );


        /// <summary>
        /// The x-coordinate of the upper-left corner of the rectangle.
        /// </summary>
        property UINT32 Left
        {
            UINT32 get()
            {
                return left;
            }

            void set(UINT32 value)
            {
                left = value;
            }
        }

        /// <summary>
        /// The y-coordinate of the upper-left corner of the rectangle. 
        /// </summary>
        property UINT32 Top
        {
            UINT32 get()
            {
                return top;
            }

            void set(UINT32 value)
            {
                top = value;
            }
        }

        /// <summary>
        /// The x-coordinate of the lower-right corner of the rectangle. 
        /// </summary>
        property UINT32 Right
        {
            UINT32 get()
            {
                return right;
            }

            void set(UINT32 value)
            {
                right = value;
            }
        }

        /// <summary>
        /// The y-coordinate of the lower-right corner of the rectangle. 
        /// </summary>
        property UINT32 Bottom
        {
            UINT32 get()
            {
                return bottom;
            }

            void set(UINT32 value)
            {
                bottom = value;
            }
        }

        /// <summary>
        /// The height of this rectangle. 
        /// </summary>
        /// <remarks>
        /// Changing the Height property will also cause a change in the Bottom value.
        /// </remarks>
        property UINT32 Height
        {
            UINT32 get()
            {
                return static_cast<UINT32>(Math::Abs((int)(Bottom - Top)));
            }

            void set(UINT32 value)
            {
                Bottom = Top + value;
            }

        }

        /// <summary>
        /// Retrieve the width of this rectangle. 
        /// </summary>
        /// <remarks>
        /// Changing the Width property will also cause a change in the Right value.
        /// </remarks>
        property UINT32 Width
        {
            UINT32 get()
            {
                return static_cast<UINT32>(Math::Abs((int)(Left - Right)));
            }

            void set(UINT32 value)
            {
                Right = Left + value;
            }
        }

        static Boolean operator == ( RectU rect1, RectU rect2 )
        {
            return 
                (rect1.Left == rect2.Left)  &&
                (rect1.Top == rect2.Top)  &&
                (rect1.Right == rect2.Right)  &&
                (rect1.Bottom == rect2.Bottom);
        }

        static Boolean operator != ( RectU rect1, RectU rect2 )
        {
            return !(rect1 == rect2);
        }


    private:

        UINT32 left;
        UINT32 top;
        UINT32 right;
        UINT32 bottom;

    internal:

        void CopyFrom(
            __in const D2D1_RECT_U &rect_u
            );

        void CopyTo(
            __out D2D1_RECT_U *prect_u
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != RectU::typeid)
            {
                return false;
            }

            return *this == safe_cast<RectU>(obj);
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



    /// <summary>
    /// Represents a rectangle defined by the upper-left corner pair of coordinates (left,top) 
    /// and the lower-right corner pair of coordinates (right, bottom). 
    /// These coordinates are expressed as a 32-bit signed integer values.
    /// </summary>

    // REVIEW: why don't the other rectangle structs inherit from this one?
    // The dimensional properties are all the same.
    public value struct Rect
    {
    public:

        /// <summary>
        /// Constructor for the Rect value type
        /// </summary>
        /// <param name="left">Initializes the Left property.</param>
        /// <param name="top">Initializes the Top property.</param>
        /// <param name="right">Initializes the Right property.</param>
        /// <param name="bottom">Initializes the Bottom property.</param>
        Rect(
            int left,
            int top,
            int right,
            int bottom
            );


        /// <summary>
        /// The x-coordinate of the upper-left corner of the rectangle.
        /// </summary>
        property int Left
        {
            int get()
            {
                return left;
            }

            void set(int value)
            {
                left = value;
            }
        }

        /// <summary>
        /// The y-coordinate of the upper-left corner of the rectangle. 
        /// </summary>
        property int Top
        {
            int get()
            {
                return top;
            }

            void set(int value)
            {
                top = value;
            }
        }

        /// <summary>
        /// The x-coordinate of the lower-right corner of the rectangle. 
        /// </summary>
        property int Right
        {
            int get()
            {
                return right;
            }

            void set(int value)
            {
                right = value;
            }
        }

        /// <summary>
        /// The y-coordinate of the lower-right corner of the rectangle. 
        /// </summary>
        property int Bottom
        {
            int get()
            {
                return bottom;
            }

            void set(int value)
            {
                bottom = value;
            }
        }

        /// <summary>
        /// The height of this rectangle. 
        /// </summary>
        /// <remarks>
        /// Changing the Height property will also cause a change in the Bottom value.
        /// </remarks>
        property int Height
        {
            int get()
            {
                return Math::Abs(Bottom - Top);
            }

            void set(int value)
            {
                Bottom = Top + value;
            }

        }

        /// <summary>
        /// Retrieve the width of this rectangle. 
        /// </summary>
        /// <remarks>
        /// Changing the Width property will also cause a change in the Right value.
        /// </remarks>
        property int Width
        {
            int get()
            {
                return Math::Abs(Left - Right);
            }

            void set(int value)
            {
                Right = Left + value;
            }
        }


        static Boolean operator == ( Rect rect1, Rect rect2 )
        {
            return 
                (rect1.Left == rect2.Left)  &&
                (rect1.Top == rect2.Top)  &&
                (rect1.Right == rect2.Right)  &&
                (rect1.Bottom == rect2.Bottom);
        }

        static Boolean operator != ( Rect rect1, Rect rect2 )
        {
            return !(rect1 == rect2);
        }


    private:

        int left;
        int top;
        int right;
        int bottom;

    internal:

        void CopyFrom(
            __in const ::RECT &rect
            );

        void CopyTo(
            __out ::RECT *prect
            );

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != Rect::typeid)
            {
                return false;
            }

            return *this == safe_cast<Rect>(obj);
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



    /// <summary>
    /// Define horizontal and vertical Dpi settings.
    /// </summary>
    public value struct DpiF
    {
    public:


        /// <summary>
        /// Constructor for the DpiF value type
        /// </summary>
        /// <param name="x">Initializes the X property.</param>
        /// <param name="y">Initializes the Y property.</param>
        DpiF(
            FLOAT x,
            FLOAT y
            );


        /// <summary>
        /// The horizontal Dpi setting.
        /// </summary>
        property FLOAT X
        {
            FLOAT get()
            {
                return x;
            }

            void set(FLOAT value)
            {
                x = value;
            }
        }

        /// <summary>
        /// The vertical Dpi setting.
        /// </summary>
        property FLOAT Y
        {
            FLOAT get()
            {
                return y;
            }

            void set(FLOAT value)
            {
                y = value;
            }
        }


        static Boolean operator == ( DpiF dpi1, DpiF dpi2 )
        {
            return 
                (dpi1.X == dpi2.X)  &&
                (dpi1.Y == dpi2.Y);
        }

        static Boolean operator != ( DpiF dpi1, DpiF dpi2 )
        {
            return !(dpi1 == dpi2);
        }

    private:

        FLOAT x;
        FLOAT y;

    public:

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != DpiF::typeid)
            {
                return false;
            }

            return *this == safe_cast<DpiF>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + x.GetHashCode();
            hashCode = hashCode * 31 + y.GetHashCode();

            return hashCode;
        }

    };

} } } }
