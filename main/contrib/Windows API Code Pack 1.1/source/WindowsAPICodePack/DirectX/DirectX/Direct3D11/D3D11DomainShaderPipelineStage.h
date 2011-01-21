//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11PipelineStage.h"
#include "D3D11ShaderAndClasses.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

/// <summary>
/// Domain Shader pipeline stage. 
/// </summary>
public ref class DomainShaderPipelineStage : PipelineStage
{
public:
    /// <summary>
    /// Get the constant buffers used by the domain-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DSGetConstantBuffers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin retrieving constant buffers from (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1).</param>
    /// <param name="bufferCount">Number of buffers to retrieve (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - startSlot).</param>
    /// <returns>Collection of constant buffer objects (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> to be returned by the method.</returns>
    ReadOnlyCollection<D3DBuffer^>^ GetConstantBuffers(UInt32 startSlot, UInt32 bufferCount);

    /// <summary>
    /// Get an array of sampler state objects from the domain-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DSGetSamplers)</para>
    /// </summary>
    /// <param name="startSlot">Index into a zero-based array to begin getting samplers from (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - 1).</param>
    /// <param name="samplerCount">Number of samplers to get from a device context. Each pipeline stage has a total of 16 sampler slots available (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - startSlot).</param>
    /// <returns>A collection of sampler-state objects (see <see cref="SamplerState"/>)<seealso cref="SamplerState"/>.</returns>
    ReadOnlyCollection<SamplerState^>^ GetSamplers(UInt32 startSlot, UInt32 samplerCount);

    /// <summary>
    /// Get the domain shader currently set on the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DSGetShader)</para>
    /// </summary>
    /// <param name="classInstanceCount">The number of class-instance elements requested.</param>
    /// <returns>
    /// A DomainShader object and its classes (see <see cref="DomainShader" />, <see cref="ShaderAndClasses" />)
    /// <seealso cref="DomainShader" />, <seealso cref="ShaderAndClasses" />.
    /// </returns>
    ShaderAndClasses<DomainShader^> GetShaderAndClasses(UInt32 classInstanceCount);

    /// <summary>
    /// Get the domain shader currently set on the device. (See <see cref="DomainShader"/>)<seealso cref="DomainShader"/>
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DSGetShader, ID3D11DeviceContext::DSSetShader)</para>
    /// Passing in null disables the shader for this pipeline stage.
    /// </summary>
    property DomainShader^ Shader
    {
        DomainShader^ get(void);
        void set(DomainShader^ domainShader);
    }

    /// <summary>
    /// Get the domain-shader resources.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DSGetShaderResources)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin getting shader resources from (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="viewCount">The number of resources to get from the device. Up to a maximum of 128 slots are available for shader resources (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - startSlot).</param>
    /// <returns>A collection of shader resource view objects to be returned by the device.</returns>
    ReadOnlyCollection<ShaderResourceView^>^ GetShaderResources(UInt32 startSlot, UInt32 viewCount);

    /// <summary>
    /// Set the constant buffers used by the domain-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DSSetConstantBuffers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the zero-based array to begin setting constant buffers to (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1).</param>
    /// <param name="constantBuffers">Collection of constant buffers (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> being given to the device.</param>
    void SetConstantBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ constantBuffers);

    /// <summary>
    /// Set an array of sampler states to the domain-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DSSetSamplers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting samplers to (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - 1).</param>
    /// <param name="samplers">A collection of sampler-state objects (see <see cref="SamplerState"/>)<seealso cref="SamplerState"/>. See Remarks.</param>
    void SetSamplers(UInt32 startSlot, IEnumerable<SamplerState^>^ samplers);

    /// <summary>
    /// Set a domain shader to the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DSSetShader)</para>
    /// </summary>
    /// <param name="domainShader">A domain shader (see <see cref="DomainShader"/>)<seealso cref="DomainShader"/>. 
    /// Passing in null disables the shader for this pipeline stage.</param>
    /// <param name="classInstances">A collection of class-instance objects (see <see cref="ClassInstance"/>)<seealso cref="ClassInstance"/>. 
    /// Each interface used by a shader must have a corresponding class instance or the shader will get disabled. 
    /// Set to null if the shader does not use any interfaces.</param>
    void SetShader(DomainShader^ domainShader, IEnumerable<ClassInstance^>^ classInstances);

    /// <summary>
    /// Set a domain shader to the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DSSetShader)</para>
    /// </summary>
    /// <param name="shaderAndClasses">
    /// A DomainShader object and its classes (see <see cref="DomainShader" />, <see cref="ShaderAndClasses" />)
    /// <seealso cref="DomainShader" />, <seealso cref="ShaderAndClasses" />.
    /// Passing in null for the shader disables the shader for this pipeline stage.
    /// </param>
    void SetShader(ShaderAndClasses<DomainShader^> shaderAndClasses);

    /// <summary>
    /// Bind an array of shader resources to the domain-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DSSetShaderResources)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting shader resources to (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="shaderResourceViews">Collection of shader resource view objects to set to the device.</param>
    void SetShaderResources(UInt32 startSlot, IEnumerable<ShaderResourceView^>^ shaderResourceViews);

protected:
    DomainShaderPipelineStage() {}
internal:
    DomainShaderPipelineStage(DeviceContext^ parent) : PipelineStage(parent)
    {
    }
};
} } } }
