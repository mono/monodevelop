//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10Resource.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A 2D texture interface manages texel data, which is structured memory.
    /// <para>(Also see DirectX SDK: ID3D10Texture2D)</para>
    /// </summary>
    public ref class Texture2D :
        public D3DResource
    {
    public: 
        /// <summary>
        /// Get the properties of the texture resource.
        /// <para>(Also see DirectX SDK: ID3D10Texture2D::GetDesc)</para>
        /// </summary>
        property Texture2DDescription Description
        {
            Texture2DDescription get();
        }

        /// <summary>
        /// Get the data contained in a subresource, and deny the GPU access to that subresource.
        /// <para>(Also see DirectX SDK: ID3D10Texture2D::Map)</para>
        /// </summary>
        /// <param name="subresourceIndex">Index number of the subresource. See D3D10CalcSubresource for more details.</param>
        /// <param name="type">Specifies the CPU's read and write permissions for a resource. For possible values, see Map.</param>
        /// <param name="options">Flag that specifies what the CPU should do when the GPU is busy. This flag is optional.</param>
        /// <returns>Pointer to the texture resource data.</returns>
        MappedTexture2D Map(UInt32 subresourceIndex, Direct3D10::Map type, Direct3D10::MapOptions options);

        /// <summary>
        /// Invalidate the pointer to the resource that was retrieved by Texture2D.Map, and re-enable GPU access to the resource.
        /// <para>(Also see DirectX SDK: ID3D10Texture2D::Unmap)</para>
        /// </summary>
        /// <param name="subresourceIndex">Subresource to be unmapped. See D3D10CalcSubresource for more details.</param>
        void Unmap(UInt32 subresourceIndex);
    internal:
        Texture2D()
        { }

        Texture2D(ID3D10Texture2D* pNativeID3D10Texture2D) : D3DResource(pNativeID3D10Texture2D)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10Texture2D)); }
        }
    };
} } } }
