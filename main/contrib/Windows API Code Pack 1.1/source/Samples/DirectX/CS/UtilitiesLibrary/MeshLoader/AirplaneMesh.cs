// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows.Media.Media3D;

namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    /// <summary>
    /// A specialization of an XMesh that rotates the propeller of "airplane 2.x".
    /// </summary>
    public class AirplaneXMesh : XMesh
    {
        private double propRotation = 0;
        private double hubOffsetX = .05;
        private double hubOffsetY = .43;
        private double propZOffset = -3.7;
        private double propAngle = 20;

        protected override Matrix3D PartAnimation( string partName )
        {
            if( partName == "propeller" )
            {
                Transform3DGroup group = new Transform3DGroup( );

                group.Children.Add(
                    new TranslateTransform3D( -hubOffsetX, -hubOffsetY, -propZOffset ) );
                group.Children.Add(
                    new RotateTransform3D(
                        new AxisAngleRotation3D( new Vector3D( 1, 0, 0 ), -propAngle ), 0, 0, 0 ) );
                group.Children.Add(
                    new RotateTransform3D(
                        new AxisAngleRotation3D( new Vector3D( 0, 0, 1 ), propRotation ), 0, 0, 0 ) );
                group.Children.Add(
                    new RotateTransform3D(
                        new AxisAngleRotation3D( new Vector3D( 1, 0, 0 ), +propAngle ), 0, 0, 0 ) );
                group.Children.Add(
                    new TranslateTransform3D( +hubOffsetX, +hubOffsetY, +propZOffset ) );

                propRotation += 11;

                return group.Value;
            }
            else
            {
                return Matrix3D.Identity;
            }
        }
    }
}
