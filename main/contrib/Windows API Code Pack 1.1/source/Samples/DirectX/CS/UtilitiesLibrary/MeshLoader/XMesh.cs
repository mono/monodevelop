// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.WindowsAPICodePack.DirectX;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    /// <summary>
    /// 
    /// </summary>
    public class XMesh : IDisposable
    {
        RasterizerDescription rDescription = new RasterizerDescription()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.Back,
            FrontCounterclockwise = false,
            DepthBias = 0,
            DepthBiasClamp = 0,
            SlopeScaledDepthBias = 0,
            DepthClipEnable = true,
            ScissorEnable = false,
            MultisampleEnable = true,
            AntiAliasedLineEnable = true,
        };

        #region public methods
        /// <summary>
        /// Renders the mesh with the specified transformation
        /// </summary>
        /// <param name="modelTransform"></param>
        public void Render(Matrix4x4F modelTransform)
        {
            rDescription.FillMode = wireFrame ? FillMode.Wireframe : FillMode.Solid;
            // setup rasterization
            using (RasterizerState rState = this.manager.device.CreateRasterizerState(rDescription))
            {
                this.manager.device.RS.State = rState;
                this.manager.brightnessVariable.AsSingle = this.lightIntensity;

                if (rootParts != null)
                {
                    Matrix3D transform = modelTransform.ToMatrix3D();

                    foreach (Part part in rootParts)
                    {
                        RenderPart(part, transform, null);
                    }
                }

                // Note: see comment regarding input layout in RenderPart()
                // method; the same thing applies to the render state of the
                // rasterizer stage of the pipeline.
                this.manager.device.RS.State = null;
            }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Displays the unshaded wireframe if true
        /// </summary>
        public bool ShowWireFrame
        {
            get { return wireFrame; }
            set { wireFrame = value; }
        }
        private bool wireFrame = false;

        /// <summary>
        /// Sets the intensity of the light used in rendering.
        /// </summary>
        public float LightIntensity
        {
            get { return lightIntensity; }
            set { lightIntensity = value; }
        }
        private float lightIntensity = 1.0f;
        #endregion

        #region virtual methods
        protected virtual Matrix3D PartAnimation(string partName)
        {
            return Matrix3D.Identity;
        }

        internal virtual ShaderResourceView UpdateRasterizerStateForPart(Part part)
        {
            return null;
        }

        #endregion

        #region implementation
        internal XMesh()
        {
        }

        internal void Load(string path, XMeshManager manager)
        {
            this.manager = manager;
            XMeshTextLoader loader = new XMeshTextLoader(this.manager.device);
            rootParts = loader.XMeshFromFile(path);
        }

        private void RenderPart(Part part, Matrix3D parentMatrix, ShaderResourceView parentTextureOverride)
        {
            // set part transform
            Transform3DGroup partGroup = new Transform3DGroup();
            partGroup.Children.Add(new MatrixTransform3D(PartAnimation(part.name)));
            partGroup.Children.Add(new MatrixTransform3D(part.partTransform.ToMatrix3D()));
            partGroup.Children.Add(new MatrixTransform3D(parentMatrix));

            parentMatrix = partGroup.Value;

            ShaderResourceView textureOverride = UpdateRasterizerStateForPart(part);

            if (textureOverride == null)
            {
                textureOverride = parentTextureOverride;
            }
            else
            {
                parentTextureOverride = textureOverride;
            }

            if (part.vertexBuffer != null)
            {
                EffectTechnique technique;

                if (textureOverride != null)
                {
                    technique = this.manager.techniqueRenderTexture;
                    this.manager.diffuseVariable.Resource = textureOverride;
                }
                else if (part.material == null)
                {
                    technique = this.manager.techniqueRenderVertexColor;
                }
                else
                {
                    if (part.material.textureResource != null)
                    {
                        technique = this.manager.techniqueRenderTexture;
                        this.manager.diffuseVariable.Resource = part.material.textureResource;
                    }
                    else
                    {
                        technique = this.manager.techniqueRenderMaterialColor;
                        this.manager.materialColorVariable.FloatVector = part.material.materialColor;
                    }
                }

                this.manager.worldVariable.Matrix = parentMatrix.ToMatrix4x4F();

                //set up vertex buffer and index buffer
                uint stride = (uint)Marshal.SizeOf(typeof(XMeshVertex));
                uint offset = 0;
                this.manager.device.IA.SetVertexBuffers(0, new D3DBuffer[]
                    { part.vertexBuffer },
                    new uint[] { stride },
                    new uint[] { offset });

                //Set primitive topology
                this.manager.device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList;

                TechniqueDescription techDesc = technique.Description;
                for (uint p = 0; p < techDesc.Passes; ++p)
                {
                    technique.GetPassByIndex(p).Apply();
                    PassDescription passDescription = technique.GetPassByIndex(p).Description;

                    using (InputLayout inputLayout = this.manager.device.CreateInputLayout(
                            part.dataDescription,
                            passDescription.InputAssemblerInputSignature,
                            passDescription.InputAssemblerInputSignatureSize))
                    {
                        // set vertex layout
                        this.manager.device.IA.InputLayout = inputLayout;

                        // draw part
                        this.manager.device.Draw((uint)part.vertexCount, 0);

                        // Note: In Direct3D 10, the device will not retain a reference
                        // to the input layout, so it's important to reset the device's
                        // input layout before disposing the object.  Were this code
                        // using Direct3D 11, the device would in fact retain a reference
                        // and so it would be safe to go ahead and dispose the input
                        // layout without resetting it; in that case, there could be just
                        // a single assignment to null outside the 'for' loop, or even
                        // no assignment at all.
                        this.manager.device.IA.InputLayout = null;
                    }
                }
            }

            foreach (Part childPart in part.parts)
            {
                RenderPart(childPart, parentMatrix, parentTextureOverride);
            }
        }

        /// <summary>
        /// The root part of this mesh
        /// </summary>
        internal IEnumerable<Part> rootParts;

        /// <summary>
        /// The object that manages the XMeshes
        /// </summary>
        internal XMeshManager manager;

        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed;

        private void DisposePart(Part part)
        {
            if (part.vertexBuffer != null)
            {
                part.vertexBuffer.Dispose();
                part.vertexBuffer = null;
            }
            if ((part.material != null) && (part.material.textureResource != null))
            {
                part.material.textureResource.Dispose();
                part.material.textureResource = null;
            }

            foreach (Part childPart in part.parts)
            {
                DisposePart(childPart);
            }

            part.parts = null;
        }

        /// <summary>
        /// Releases resources no longer needed.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && rootParts != null)
            {
                foreach (Part part in rootParts)
                {
                    DisposePart(part);
                }
                rootParts = null;
                disposed = true;
            }
        }
        #endregion
    }
}
