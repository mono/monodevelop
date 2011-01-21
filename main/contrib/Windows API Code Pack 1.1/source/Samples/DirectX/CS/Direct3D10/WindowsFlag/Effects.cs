// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using System.Reflection;

namespace WindowsFlag
{
    /// <summary>
    /// This class wraps the shaders
    /// </summary>
    internal class Effects
    {
        #region Private fields
        Effect effect;
        EffectTechnique technique;
        EffectMatrixVariable worldVariable;
        EffectMatrixVariable viewVariable;
        EffectMatrixVariable projectionVariable;

        EffectVectorVariable lightDirVariable;
        EffectVectorVariable lightColorVariable;
        EffectVectorVariable baseColorVariable;

        Vector4F[] vLightDirs =
        {
            new Vector4F ( 0.0f, 1.0f, 3.0f, 1.0f ),
            new Vector4F ( 0.0f, 1.0f, -3.0f, 1.0f),
        };

        Vector4F[] vLightColors = 
        {
            new Vector4F ( 0.25f, 0.25f, 0.25f, 0.25f ),
            new Vector4F ( 0.25f, 0.25f, 0.25f, 0.25f )
        };
        
        #endregion

        #region Internal properties
        internal Matrix4x4F WorldMatrix
        {
            set
            {
                worldVariable.Matrix = value;
            }
        }

        internal Matrix4x4F ViewMatrix
        {
            set
            {
                viewVariable.Matrix = value;
            }
        }

        internal Matrix4x4F ProjectionMatrix
        {
            set
            {
                projectionVariable.Matrix = value;
            }
        }

        internal Vector4F BaseColor
        {
            set
            {
                baseColorVariable.FloatVector = value;
            }
        }

        internal EffectTechnique Technique
        {
            get
            {
                return technique;
            }
        } 
        #endregion

        public Effects(D3DDevice device)
        {
            // File compiled using the following command:
            // "$(DXSDK_DIR)\utilities\bin\x86\fxc" "WindowsFlag.fx" /T fx_4_0 /Fo "WindowsFlag.fxo"
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowsFlag.WindowsFlag.fxo"))
            {
                effect = device.CreateEffectFromCompiledBinary(stream);
            }
            // Obtain the technique
            technique = effect.GetTechniqueByName("Render");

            // Obtain the variables
            worldVariable = effect.GetVariableByName("World").AsMatrix;
            viewVariable = effect.GetVariableByName("View").AsMatrix;
            projectionVariable = effect.GetVariableByName("Projection").AsMatrix;

            lightDirVariable = effect.GetVariableByName("vLightDir").AsVector;
            lightColorVariable = effect.GetVariableByName("vLightColor").AsVector;
            baseColorVariable = effect.GetVariableByName("vBaseColor").AsVector;

            // Set constants
            lightColorVariable.SetFloatVectorArray(vLightColors);
            lightDirVariable.SetFloatVectorArray(vLightDirs);
        }
    }
}
