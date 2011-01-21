// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.DirectX;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{

    /// <summary>
    /// Manages the XMesh file loading
    /// </summary>
    public class XMeshManager : IDisposable
    {
        internal D3DDevice device;

        internal Effect effect;
        internal EffectTechnique techniqueRenderTexture;
        internal EffectTechnique techniqueRenderVertexColor;
        internal EffectTechnique techniqueRenderMaterialColor;

        internal EffectScalarVariable brightnessVariable;
        internal EffectVectorVariable materialColorVariable;
        internal EffectMatrixVariable worldVariable;
        internal EffectMatrixVariable viewVariable;
        internal EffectMatrixVariable projectionVariable;
        internal EffectShaderResourceVariable diffuseVariable;

        /// <summary>
        /// Creates the mesh manager
        /// </summary>
        /// <param name="device"></param>
        public XMeshManager(D3DDevice device)
        {
            this.device = device;

            // Create the effect
            //XMesh.fxo was compiled from XMesh.fx using:
            // "$(DXSDK_DIR)utilities\bin\x86\fxc" "$(ProjectDir)Mesh\MeshLoaders\XMesh.fx" /T fx_4_0 /Fo"$(ProjectDir)Mesh\MeshLoaders\XMesh.fxo"
            using (Stream effectStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.XMesh.fxo"))
            {
                effect = device.CreateEffectFromCompiledBinary( new BinaryReader( effectStream ) );
            }

            // Obtain the techniques
            techniqueRenderTexture = effect.GetTechniqueByName("RenderTextured");
            techniqueRenderVertexColor = effect.GetTechniqueByName("RenderVertexColor");
            techniqueRenderMaterialColor = effect.GetTechniqueByName("RenderMaterialColor");

            // Obtain the variables
            brightnessVariable = effect.GetVariableByName("Brightness").AsScalar;
            materialColorVariable = effect.GetVariableByName("MaterialColor").AsVector;
            worldVariable = effect.GetVariableByName("World").AsMatrix;
            viewVariable = effect.GetVariableByName("View").AsMatrix;
            projectionVariable = effect.GetVariableByName("Projection").AsMatrix;
            diffuseVariable = effect.GetVariableByName("tex2D").AsShaderResource;
        }

        public void SetViewAndProjection(Matrix4x4F view, Matrix4x4F projection)
        {
            viewVariable.Matrix = view;
            projectionVariable.Matrix = projection;
        }

        /// <summary>
        /// Returns an XMesh object that contains the data from a specified .X file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public XMesh Open(string path)
        {
            XMesh mesh = new XMesh();
            mesh.Load(path, this);
            return mesh;
        }

        /// <summary>
        /// Reutrns a specialization of an XMesh object that contains the data from a specified .X file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public T Open<T>(string path) where T : XMesh, new()
        {
            T mesh = new T();
            mesh.Load(path, this);
            return mesh;
        }

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed;
        /// <summary>
        /// Cleans up the memory allocated by the manager.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                if (effect != null)
                    effect.Dispose();
                effect = null;

                if (techniqueRenderTexture != null)
                    techniqueRenderTexture.Dispose();
                techniqueRenderTexture = null;

                if (techniqueRenderVertexColor != null)
                    techniqueRenderVertexColor.Dispose();
                techniqueRenderVertexColor = null;

                if (techniqueRenderMaterialColor != null)
                    techniqueRenderMaterialColor.Dispose();
                techniqueRenderMaterialColor = null;

                if (brightnessVariable != null)
                    brightnessVariable.Dispose();
                brightnessVariable = null;

                if (materialColorVariable != null)
                    materialColorVariable.Dispose();
                materialColorVariable = null;

                if (worldVariable != null)
                    worldVariable.Dispose();
                worldVariable = null;

                if (viewVariable != null)
                    viewVariable.Dispose();
                viewVariable = null;

                if (projectionVariable != null)
                    projectionVariable.Dispose();
                projectionVariable = null;

                if (diffuseVariable != null)
                    diffuseVariable.Dispose();
                diffuseVariable = null;
            }
        }
        #endregion
    }
}
