//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10Resource.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A 1D texture interface accesses texel data, which is structured memory.
    /// <para>(Also see DirectX SDK: ID3D10Texture1D)</para>
    /// </summary>
    public ref class Texture1D :
        public D3DResource
    {
    public: 
        /// <summary>
        /// Get the properties of the texture resource.
        /// <para>(Also see DirectX SDK: ID3D10Texture1D::GetDesc)</para>
        /// </summary>
        property Texture1DDescription Description
        {
            Texture1DDescription get();
        }

        /// <summary>
        /// Get the data contained in a subresource, and deny the GPU access to that subresource.
        /// <para>(Also see DirectX SDK: ID3D10Texture1D::Map)</para>
        /// </summary>
        /// <param name="subresourceIndex">Index number of the subresource. See D3D10CalcSubresource for more details.</param>
        /// <param name="type">Specifies the CPU's read and write permissions for a resource. For possible values, see Map.</param>
        /// <param name="options">Flag that specifies what the CPU should do when the GPU is busy. This flag is optional.</param>
        /// <returns>Pointer to the texture resource data.</returns>
        IntPtr Map(UInt32 subresourceIndex, Direct3D10::Map type, Direct3D10::MapOptions options);

        /// <summary>
        /// Invalidate the resource that was retrieved by Texture1D.Map, and re-enable the GPU's access to that resource.
        /// <para>(Also see DirectX SDK: ID3D10Texture1D::Unmap)</para>
        /// </summary>
        /// <param name="subresourceIndex">Subresource to be unmapped. See D3D10CalcSubresource for more details.</param>
        void Unmap(UInt32 subresourceIndex);
    internal:
        Texture1D()
        { }

        Texture1D(ID3D10Texture1D* pNativeID3D10Texture1D) : D3DResource(pNativeID3D10Texture1D)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10Texture1D)); }
        }
    };
} } } }
