// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using Microsoft.WindowsAPICodePack.DirectX;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using System.Windows.Media.Media3D;


namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    /// <summary>
    /// A Mesh that allows for changing textures within the scene
    /// </summary>
    public class Texturizer : XMesh
    {
        /// <summary>
        /// If true shows one texture at a time
        /// </summary>
        public bool ShowOneTexture
        {
            get
            {
                return showOneTexture;
            }
            set
            {
                showOneTexture = value;
            }
        }
        bool showOneTexture = true;

        /// <summary>
        /// This method sets which part to texture during rendering.
        /// </summary>
        /// <param name="partName"></param>
        public void PartToTexture(string partName)
        {
            if (string.IsNullOrEmpty(partName))
            {
                throw new ArgumentException("Must be a non-empty string", "partName");
            }

            partEmphasis.Clear();

            foreach (Part part in rootParts)
            {
                if (BuildEmphasisDictionary(part, partName, false))
                {
                    break;
                }
            }
        }

        private HashSet<Part> partEmphasis = new HashSet<Part>();

        /// <summary>
        /// Builds a dictionary of parts to be emphasized if displaying wireframe.
        /// </summary>
        /// <remarks>
        /// During rendering, as the mesh tree is traversed each part is checked to
        /// see whether it should be displayed as wireframe or not. A part in the dictionary
        /// built by this method will be rendered as a solid part, otherwise the part
        /// will be rendered as wireframe.
        /// This method traverses the mesh tree looking for the named part. Once that
        /// named part is found, that part and all its children are added to the dictionary.
        /// The traversal is terminated once the named part has been found and all of its
        /// children have also been traversed (and added to the dictionary).
        /// </remarks>
        /// <param name="part">The current part to inspect</param>
        /// <param name="partName">The name of the root part to emphasize during rendering</param>
        /// <param name="fEmphasizeParent">True if the parent of this part will be emphasized, false otherwise</param>
        /// <returns>True if this part has been emphasized, false otherwise</returns>
        private bool BuildEmphasisDictionary(Part part, string partName, bool fEmphasizeParent)
        {
            if (fEmphasizeParent || (!string.IsNullOrEmpty(part.name) && part.name == partName))
            {
                partEmphasis.Add(part);
                fEmphasizeParent = true;
            }

            foreach (Part childPart in part.parts)
            {
                if (BuildEmphasisDictionary(childPart, partName, fEmphasizeParent) && !fEmphasizeParent)
                {
                    break;
                }
            }

            return fEmphasizeParent;
        }

        /// <summary>
        /// Clears the alternate texture list (restoring the model's textures)
        /// </summary>
        public void RevertTextures()
        {
            alternateTextures.Clear();
        }

        /// <summary>
        /// Gets a list of the names of the parts in the mesh
        /// </summary>
        /// <returns></returns>
        public List<string> GetParts()
        {
            List<string> partNames = new List<string>();

            if (rootParts != null)
            {
                foreach (Part part in rootParts)
                {
                    GetParts(part, partNames);
                }
            }

            return partNames;
        }

        private void GetParts(Part part, List<string> names)
        {
            if (!string.IsNullOrEmpty(part.name))
            {
                names.Add(part.name);
            }

            foreach (Part childPart in part.parts)
            {
                GetParts(childPart, names);
            }
        }


        /// <summary>
        /// Creates an alternate texture for a part
        /// </summary>
        /// <param name="partName">The name of the part to create the texture for.</param>
        /// <param name="imagePath">The path to the image to be used for the texture.</param>
        public void SwapTexture(string partName, string imagePath)
        {
            if (partName != null)
            {
                if (File.Exists(imagePath))
                {
                    FileStream stream = File.OpenRead(imagePath);

                    try
                    {
                        ShaderResourceView srv = TextureLoader.LoadTexture(this.manager.device, stream);
                        if (srv != null)
                            alternateTextures[partName] = srv;
                    }
                    catch (COMException)
                    {
                        System.Windows.MessageBox.Show("Not a valid image.");
                    }

                }
                else
                {
                    alternateTextures[partName] = null;
                }
            }
        }

        Dictionary<string, ShaderResourceView> alternateTextures = new Dictionary<string, ShaderResourceView>();

        private RasterizerState solidRasterizerState;
        private RasterizerState wireframeRasterizerState;
        private RasterizerState currentRasterizerState;

        internal override ShaderResourceView UpdateRasterizerStateForPart(Part part)
        {
            RasterizerState state = 
                showOneTexture && !partEmphasis.Contains(part) ? wireframeRasterizerState : solidRasterizerState;

            if (state != currentRasterizerState)
            {
                this.manager.device.RS.State = currentRasterizerState = state;
            }

            ShaderResourceView textureOverride;

            if (!alternateTextures.TryGetValue(part.name, out textureOverride))
            {
                textureOverride = null;
            }

            return textureOverride;
        }

        /// <summary>
        /// Renders the mesh with the specified transformation. This alternate render method
        /// supplements the base class rendering to provide part-by-part texturing support.
        /// </summary>
        /// <param name="modelTransform"></param>
        public void Render(Matrix3D modelTransform)
        {
            // setup rasterization
            RasterizerDescription rasterizerDescription = new RasterizerDescription()
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
                AntiAliasedLineEnable = true
            };

            try
            {
                solidRasterizerState = this.manager.device.CreateRasterizerState(rasterizerDescription);

                rasterizerDescription.FillMode = FillMode.Wireframe;
                wireframeRasterizerState = this.manager.device.CreateRasterizerState(rasterizerDescription);

                base.Render(modelTransform.ToMatrix4x4F());
            }
            finally
            {
                if (solidRasterizerState != null)
                {
                    solidRasterizerState.Dispose();
                    solidRasterizerState = null;
                }

                if (wireframeRasterizerState != null)
                {
                    wireframeRasterizerState.Dispose();
                    wireframeRasterizerState = null;
                }

                currentRasterizerState = null;
            }
        }
    }
}
