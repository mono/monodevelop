//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11PipelineStage.h"
#include "D3D11ShaderAndClasses.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

/// <summary>
/// Pixel Shader pipeline stage. 
/// </summary>
public ref class PixelShaderPipelineStage : PipelineStage
{
public:
    /// <summary>
    /// Get the constant buffers used by the pixel shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::PSGetConstantBuffers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin retrieving constant buffers from (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1).</param>
    /// <param name="bufferCount">Number of buffers to retrieve (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - startSlot).</param>
    /// <returns>Collection of constant buffer objects (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> to be returned by the method.</returns>
    ReadOnlyCollection<D3DBuffer^>^ GetConstantBuffers(UInt32 startSlot, UInt32 bufferCount);

    /// <summary>
    /// Get an array of sampler states from the pixel shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::PSGetSamplers)</para>
    /// </summary>
    /// <param name="startSlot">Index into a zero-based array to begin getting samplers from (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - 1).</param>
    /// <param name="samplerCount">Number of samplers to get from a device context. Each pipeline stage has a total of 16 sampler slots available (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - startSlot).</param>
    /// <returns>Collection of sampler-state objects (see <see cref="SamplerState"/>)<seealso cref="SamplerState"/> to be returned by the device.</returns>
    ReadOnlyCollection<SamplerState^>^ GetSamplers(UInt32 startSlot, UInt32 samplerCount);

    /// <summary>
    /// Get the pixel shader currently set on the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::PSGetShader)</para>
    /// </summary>
    /// <param name="classInstanceCount">The number of class-instance elements in the array.</param>
    /// <returns>
    /// A PixelShader object and its classes (see <see cref="PixelShader" />, <see cref="ShaderAndClasses" />)
    /// <seealso cref="PixelShader" />, <seealso cref="ShaderAndClasses" />.
    /// </returns>
    ShaderAndClasses<PixelShader^> GetShaderAndClasses(UInt32 classInstanceCount);

    /// <summary>
    /// Gets or sets the pixel shader currently set on the device. (See <see cref="PixelShader"/>)<seealso cref="PixelShader"/>.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::PSGetShader, ID3D11DeviceContext::PSSetShader)</para>
    /// Passing in null disables the shader for this pipeline stage.
    /// </summary>
    property PixelShader^ Shader
    {
        PixelShader^ get(void);
        void set(PixelShader^ pixelShader);
    }

    /// <summary>
    /// Get the pixel shader resources.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::PSGetShaderResources)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin getting shader resources from (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="viewCount">The number of resources to get from the device. Up to a maximum of 128 slots are available for shader resources (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - startSlot).</param>
    /// <returns>Collection of shader resource view objects to be returned by the device.</returns>
    ReadOnlyCollection<ShaderResourceView^>^ GetShaderResources(UInt32 startSlot, UInt32 viewCount);

    /// <summary>
    /// Set the constant buffers used by the pixel shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::PSSetConstantBuffers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting constant buffers to (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1).</param>
    /// <param name="constantBuffers">Collection of constant buffers (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> being given to the device.</param>
    void SetConstantBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ constantBuffers);

    /// <summary>
    /// Set an array of sampler states to the pixel shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::PSSetSamplers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting samplers to (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - 1).</param>
    /// <param name="samplers">A collection of sampler-state objects (see <see cref="SamplerState"/>)<seealso cref="SamplerState"/>. See Remarks.</param>
    void SetSamplers(UInt32 startSlot, IEnumerable<SamplerState^>^ samplers);

    /// <summary>
    /// Sets a pixel shader to the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::PSSetShader)</para>
    /// </summary>
    /// <param name="pixelShader">A pixel shader (see <see cref="PixelShader"/>)<seealso cref="PixelShader"/>. 
    /// Passing in null disables the shader for this pipeline stage.</param>
    /// <param name="classInstances">A collection of class-instance objects (see <see cref="ClassInstance"/>)<seealso cref="ClassInstance"/>. 
    /// Each interface used by a shader must have a corresponding class instance or the shader will get disabled. 
    /// Set to null if the shader does not use any interfaces.</param>
    void SetShader(PixelShader^ pixelShader, IEnumerable<ClassInstance^>^ classInstances);

    /// <summary>
    /// Sets a pixel shader to the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::PSSetShader)</para>
    /// </summary>
    /// <param name="shaderAndClasses">
    /// A PixelShader object and its classes (see <see cref="PixelShader" />, <see cref="ShaderAndClasses" />)
    /// <seealso cref="PixelShader" />, <seealso cref="ShaderAndClasses" />.
    /// Passing in null for the shader disables the shader for this pipeline stage.
    /// </param>
    void SetShader(ShaderAndClasses<PixelShader^> shaderAndClasses);

    /// <summary>
    /// Bind an array of shader resources to the pixel shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::PSSetShaderResources)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting shader resources to (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="shaderResourceViews">Collection of shader resource view objects to set to the device.</param>
    void SetShaderResources(UInt32 startSlot, IEnumerable<ShaderResourceView^>^ shaderResourceViews);

protected:

    // REVIEW: why 'protected'?
    PixelShaderPipelineStage(void)
    { }

internal:

    PixelShaderPipelineStage(DeviceContext^ parent) : PipelineStage(parent)
    { }
};
} } } }
