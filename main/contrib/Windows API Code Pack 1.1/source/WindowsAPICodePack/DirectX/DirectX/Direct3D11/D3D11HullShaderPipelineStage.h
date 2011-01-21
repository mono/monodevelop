//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11PipelineStage.h"
#include "D3D11ShaderAndClasses.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

/// <summary>
/// Hull Shader pipeline stage. 
/// </summary>
public ref class HullShaderPipelineStage : PipelineStage
{
public:
    /// <summary>
    /// Get the constant buffers used by the hull-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::HSGetConstantBuffers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin retrieving constant buffers from (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1).</param>
    /// <param name="bufferCount">Number of buffers to retrieve (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - startSlot).</param>
    /// <returns>Collection of constant buffer objects (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> to be returned by the method.</returns>
    ReadOnlyCollection<D3DBuffer^>^ GetConstantBuffers(UInt32 startSlot, UInt32 bufferCount);

    /// <summary>
    /// Get an array of sampler state objects from the hull-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::HSGetSamplers)</para>
    /// </summary>
    /// <param name="startSlot">Index into a zero-based array to begin getting samplers from (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - 1).</param>
    /// <param name="samplerCount">Number of samplers to get from a device context. Each pipeline stage has a total of 16 sampler slots available (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - startSlot).</param>
    /// <returns>A collection of sampler-state objects (see <see cref="SamplerState"/>)<seealso cref="SamplerState"/>.</returns>
    ReadOnlyCollection<SamplerState^>^ GetSamplers(UInt32 startSlot, UInt32 samplerCount);

    /// <summary>
    /// Get the hull shader currently set on the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::HSGetShader)</para>
    /// </summary>
    /// <param name="classInstanceCount">The number of class-instance elements requested.</param>
    /// <returns>
    /// A HullShader object and its classes (see <see cref="HullShader" />, <see cref="ShaderAndClasses" />)
    /// <seealso cref="HullShader" />, <seealso cref="ShaderAndClasses" />.
    /// </returns>
    ShaderAndClasses<HullShader^> GetShaderAndClasses(UInt32 classInstanceCount);

    /// <summary>
    /// Gets or sets the hull shader currently set on the device. (See <see cref="GeometryShader"/>)<seealso cref="GeometryShader"/>
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::HSGetShader, ID3D11DeviceContext::HSSetShader)</para>
    /// Setting this property to null disables the shader for this pipeline stage.
    /// </summary>
    property HullShader^ Shader
    {
        HullShader^ get(void);
        void set(HullShader^ shader);
    }

    /// <summary>
    /// Get the hull-shader resources.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::HSGetShaderResources)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin getting shader resources from (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="viewCount">The number of resources to get from the device. Up to a maximum of 128 slots are available for shader resources (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - startSlot).</param>
    /// <returns>Collection of shader resource view objects to be returned by the device.</returns>
    ReadOnlyCollection<ShaderResourceView^>^ GetShaderResources(UInt32 startSlot, UInt32 viewCount);

    /// <summary>
    /// Set the constant buffers used by the hull-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::HSSetConstantBuffers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting constant buffers to (ranges from 0 to D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1).</param>
    /// <param name="constantBuffers">Collection of constant buffers (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> being given to the device.</param>
    void SetConstantBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ constantBuffers);

    /// <summary>
    /// Set an array of sampler states to the hull-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::HSSetSamplers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the zero-based array to begin setting samplers to (ranges from 0 to D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT - 1).</param>
    /// <param name="samplers">A collection of sampler-state objects (see <see cref="SamplerState"/>)<seealso cref="SamplerState"/>. See Remarks.</param>
    void SetSamplers(UInt32 startSlot, IEnumerable<SamplerState^>^ samplers);

    /// <summary>
    /// Set a hull shader to the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::HSSetShader)</para>
    /// </summary>
    /// <param name="shader">A hull shader (see <see cref="HullShader"/>)<seealso cref="HullShader"/>. 
    /// Passing in null disables the shader for this pipeline stage.</param>
    /// <param name="classInstances">A collection of class-instance objects (see <see cref="ClassInstance"/>)<seealso cref="ClassInstance"/>. 
    /// Each interface used by a shader must have a corresponding class instance or the shader will get disabled. Set to null if the shader does not use any interfaces.</param>
    void SetShader(HullShader^ shader, IEnumerable<ClassInstance^>^ classInstances);

    /// <summary>
    /// Set a hull shader to the device.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::HSSetShader)</para>
    /// </summary>
    /// <param name="shaderAndClasses">
    /// A HullShader object and its classes (see <see cref="HullShader" />, <see cref="ShaderAndClasses" />)
    /// <seealso cref="HullShader" />, <seealso cref="ShaderAndClasses" />.
    /// Passing in null for the shader disables the shader for this pipeline stage.
    /// </param>
    void SetShader(ShaderAndClasses<HullShader^> shaderAndClasses);

    /// <summary>
    /// Bind an array of shader resources to the hull-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::HSSetShaderResources)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting shader resources to (ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="shaderResourceViews">Collection of shader resource view objects to set to the device. 
    /// Up to a maximum of 128 slots are available for shader resources(ranges from 0 to D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - startSlot).</param>
    void SetShaderResources(UInt32 startSlot, IEnumerable<ShaderResourceView^>^ shaderResourceViews);

protected:
    HullShaderPipelineStage() { }

internal:
    HullShaderPipelineStage(DeviceContext^ parent) : PipelineStage(parent) { }
};
} } } }
