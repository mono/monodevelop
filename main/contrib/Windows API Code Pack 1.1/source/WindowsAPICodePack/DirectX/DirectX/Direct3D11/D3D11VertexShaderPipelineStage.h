//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11PipelineStage.h"
#include "D3D11ShaderAndClasses.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

/// <summary>
/// Vertex Shader pipeline stage. 
/// </summary>
public ref class VertexShaderPipelineStage : PipelineStage
{

public:
    /// <summary>
    /// Get the constant buffers used by the vertex shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::VSGetConstantBuffers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin retrieving constant buffers from (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1).</param>
    /// <param name="bufferCount">Number of buffers to retrieve (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - startSlot).</param>
    /// <returns>Collection of constant buffer objects (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> to be returned by the method.</returns>
    ReadOnlyCollection<D3DBuffer^>^ GetConstantBuffers(UInt32 startSlot, UInt32 bufferCount);

    /// <summary>
    /// Get an array of sampler states from the vertex shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::VSGetSamplers)</para>
    /// </summary>
    /// <param name="startSlot">Index into a zero-based array to begin getting samplers from (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - 1).</param>
    /// <param name="samplerCount">Number of samplers to get from a device context. Each pipeline stage has a total of 16 sampler slots available (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - startSlot).</param>
    /// <returns>Collection of sampler-state objects (see <see cref="SamplerState"/>)<seealso cref="SamplerState"/> to be returned by the device.</returns>
    ReadOnlyCollection<SamplerState^>^ GetSamplers(UInt32 startSlot, UInt32 samplerCount);

    /// <summary>
    /// Get the vertex shader currently set on the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::VSGetShader)</para>
    /// </summary>
    /// <param name="classInstanceCount">The number of class-instance elements requested.</param>
    /// <returns>
    /// A VertexShader object and its classes (see <see cref="VertexShader" />, <see cref="ShaderAndClasses" />)
    /// <seealso cref="VertexShader" />, <seealso cref="ShaderAndClasses" />.
    /// </returns>
    ShaderAndClasses<VertexShader^> GetShaderAndClasses(UInt32 classInstanceCount);

    /// <summary>
    /// Get the vertex shader currently set on the device. (See <see cref="VertexShader"/>)<seealso cref="VertexShader"/>.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::VSGetShader, ID3D11DeviceContext::VSSetShader)</para>
    /// Passing in null disables the shader for this pipeline stage.
    /// </summary>
    property VertexShader^ Shader
    {
        VertexShader^ get(void);
        void set(VertexShader^ vertexShader);
    }

    /// <summary>
    /// Get the vertex shader resources.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::VSGetShaderResources)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin getting shader resources from (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="viewCount">The number of resources to get from the device. Up to a maximum of 128 slots are available for shader resources (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - startSlot).</param>
    /// <returns>Collection of shader resource views to be returned by the device.</returns>
    ReadOnlyCollection<ShaderResourceView^>^ GetShaderResources(UInt32 startSlot, UInt32 viewCount);

    /// <summary>
    /// Set the constant buffers used by the vertex shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::VSSetConstantBuffers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting constant buffers to (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1).</param>
    /// <param name="constantBuffers">Collection of constant buffers (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> being given to the device.</param>
    void SetConstantBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ constantBuffers);

    /// <summary>
    /// Set an array of sampler states to the vertex shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::VSSetSamplers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting samplers to (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - 1).</param>
    /// <param name="samplers">A collection of sampler-state objects (see <see cref="SamplerState"/>)<seealso cref="SamplerState"/>. See Remarks.</param>
    void SetSamplers(UInt32 startSlot, IEnumerable<SamplerState^>^ samplers);

    /// <summary>
    /// Set a vertex shader to the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::VSSetShader)</para>
    /// </summary>
    /// <param name="vertexShader">A vertex shader (see <see cref="VertexShader"/>)<seealso cref="VertexShader"/>. 
    /// Passing in null disables the shader for this pipeline stage.</param>
    /// <param name="classInstances">A collection of class-instance objects (see <see cref="ClassInstance"/>)<seealso cref="ClassInstance"/>. 
    /// Each interface used by a shader must have a corresponding class instance or the shader will get disabled. 
    /// Set this parameter to null if the shader does not use any interfaces.</param>
    void SetShader(VertexShader^ vertexShader, IEnumerable<ClassInstance^>^ classInstances);

    /// <summary>
    /// Set a vertex shader to the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::VSSetShader)</para>
    /// </summary>
    /// <param name="shaderAndClasses">
    /// A VertexShader object and its classes (see <see cref="VertexShader" />, <see cref="ShaderAndClasses" />)
    /// <seealso cref="VertexShader" />, <seealso cref="ShaderAndClasses" />.
    /// Passing in null for the shader disables the shader for this pipeline stage.
    /// </param>
    void SetShader(ShaderAndClasses<VertexShader^> shaderAndClasses);

    /// <summary>
    /// Bind an array of shader resources to the vertex-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::VSSetShaderResources)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting shader resources to (range is from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="shaderResourceViews">Collection of shader resource view objects to set to the device.
    /// Up to a maximum of 128 slots are available for shader resources (range is from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - startSlot).</param>
    void SetShaderResources(UInt32 startSlot, IEnumerable<ShaderResourceView^>^ shaderResourceViews);

protected:

    // REVIEW: why 'protected'?
    VertexShaderPipelineStage(void)
    { }

internal:

    VertexShaderPipelineStage(DeviceContext^ parent) : PipelineStage(parent)
    { }
};
} } } }
