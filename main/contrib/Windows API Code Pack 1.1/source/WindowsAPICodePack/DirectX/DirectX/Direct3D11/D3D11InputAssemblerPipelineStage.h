//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11PipelineStage.h"
#include "D3D11Buffer.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

/// <summary>
/// InputAssembler pipeline stage. 
/// </summary>
public ref class InputAssemblerPipelineStage : PipelineStage
{
public:
    /// <summary>
    /// Gets or sets the index buffer that is bound to the input-assembler stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::IAGetIndexBuffer)</para>
    /// </summary>
    /// <remarks>Setting this property using a buffer that is currently bound for writing (i.e.
    /// bound to the stream output pipeline stage) will effectively bind null instead because a
    /// buffer cannot be bound as both an input and an output at the same time.
    /// </remarks>
    property Direct3D11::IndexBuffer IndexBuffer
    {
        Direct3D11::IndexBuffer get(void);
        void set(Direct3D11::IndexBuffer);
    }

    /// <summary>
    /// Gets or sets the input-layout object that is bound to the input-assembler stage. (See <see cref="InputLayout"/>)<seealso cref="InputLayout"/>
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::IAGetInputLayout, ID3D11DeviceContext::IASetInputLayout)</para>
    /// </summary>
    property InputLayout^ InputLayout
    {
        Direct3D11::InputLayout^ get(void);
        void set(Direct3D11::InputLayout^ inputLayout);
    }

    /// <summary>
    /// Get information about the primitive type, and data order that describes input data for the input assembler stage. (See <see cref="PrimitiveTopology"/>)<seealso cref="PrimitiveTopology"/>.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::IAGetPrimitiveTopology, ID3D11DeviceContext::IASetPrimitiveTopology)</para>
    /// </summary>
    property PrimitiveTopology PrimitiveTopology
    {
        Direct3D11::PrimitiveTopology get(void);
        void set(Direct3D11::PrimitiveTopology topology);
    }

    /// <summary>
    /// Get the vertex buffers bound to the input-assembler stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::IAGetVertexBuffers)</para>
    /// </summary>
    /// <param name="startSlot">The input slot of the first vertex buffer to get. The first vertex buffer is explicitly bound to the start slot; this causes each additional vertex buffer in the array to be implicitly bound to each subsequent input slot. There are 16 input slots.</param>
    /// <param name="bufferCount">The number of vertex buffers to get starting at the offset. The number of buffers (plus the starting slot) cannot exceed the total number of IA-stage input slots.</param>
    /// <returns>A collection of vertex buffers returned by the method (see <see cref="VertexBuffer"/>)<seealso cref="VertexBuffer"/>.</returns>
    ReadOnlyCollection<VertexBuffer>^ GetVertexBuffers(UInt32 startSlot, UInt32 bufferCount);

    /// <summary>
    /// Bind an array of vertex buffers to the input-assembler stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::IASetVertexBuffers)</para>
    /// </summary>
    /// <param name="startSlot">The first input slot for binding. The first vertex buffer is explicitly bound to the start slot; this causes each additional vertex buffer in the array to be implicitly bound to each subsequent input slot. There are 16 input slots (ranges from 0 to D3D11_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="vertexBuffers">A collection of vertex buffers (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/>. 
    /// The number of buffers (plus the starting slot) cannot exceed the total number of IA-stage input slots (ranges from 0 to D3D11_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT - startSlot).
    /// The vertex buffers must have been created with the VertexBuffer flag.</param>
    /// <param name="strides">An array of stride values; one stride value for each buffer in the vertex-buffer array. Each stride is the size (in bytes) of the elements that are to be used from that vertex buffer.</param>
    /// <param name="offsets">An array of offset values; one offset value for each buffer in the vertex-buffer array. Each offset is the number of bytes between the first element of a vertex buffer and the first element that will be used.</param>
    void SetVertexBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ vertexBuffers, array<UInt32>^ strides, array<UInt32>^ offsets);

    /// <summary>
    /// Bind an array of vertex buffers to the input-assembler stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::IASetVertexBuffers)</para>
    /// </summary>
    /// <param name="startSlot">The first input slot for binding. The first vertex buffer is explicitly bound to the start slot; this causes each additional vertex buffer in the array to be implicitly bound to each subsequent input slot. There are 16 input slots (ranges from 0 to D3D11_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="vertexBuffers">
    /// A collection of vertex buffers (see <see cref="VertexBuffer"/>)<seealso cref="VertexBuffer"/>. 
    /// The number of buffers (plus the starting slot) cannot exceed the total number of IA-stage input slots (ranges from 0 to D3D11_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT - startSlot).
    /// The vertex buffers must have been created with the <see cref="BindingOptions" /><b>.VertexBuffer flag</b>.
    /// </param>
    void SetVertexBuffers(UInt32 startSlot, IEnumerable<VertexBuffer>^ vertexBuffers);

protected:

    // REVIEW: why 'protected'?
    InputAssemblerPipelineStage(void)
    { }

internal:

    InputAssemblerPipelineStage(DeviceContext^ parent) : PipelineStage(parent) { }
};
} } } }
