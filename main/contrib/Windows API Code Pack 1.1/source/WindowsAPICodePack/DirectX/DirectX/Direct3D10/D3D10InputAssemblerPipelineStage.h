//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10PipelineStage.h"
#include "D3D10Buffer.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

/// <summary>
/// InputAssembler pipeline stage. 
/// </summary>
public ref class InputAssemblerPipelineStage : PipelineStage
{
public:
    /// <summary>
    /// Get or sets the index buffer that is bound to the input-assembler stage.
    /// (see <see cref="IndexBuffer"/>)<seealso cref="IndexBuffer"/>
    /// <para>(Also see DirectX SDK: ID3D10Device::IAGetIndexBuffer, ID3D10Device::IASetIndexBuffer)</para>
    /// </summary>
    /// <remarks>Calling this method using a buffer that is currently bound for writing (i.e. bound to the stream output pipeline stage) will effectively 
    /// bind null instead because a buffer cannot be bound as both an input and an output at the same time.</remarks>
    property Direct3D10::IndexBuffer IndexBuffer
    {
        Direct3D10::IndexBuffer get(void);
        void set(Direct3D10::IndexBuffer);
    }

    /// <summary>
    /// Gets or sets the input-layout object that is bound to the input-assembler stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::IAGetInputLayout, ID3D10Device::IASetInputLayout)</para>
    /// </summary>
    property InputLayout^ InputLayout
    {
        Direct3D10::InputLayout^ get(void);
        void set(Direct3D10::InputLayout^ inputLayout);
    }

    /// <summary>
    /// Gets or sets information about the primitive type, and data order that describes input data for the input assembler stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::IAGetPrimitiveTopology, ID3D10Device::IASetPrimitiveTopology)</para>
    /// </summary>
    property PrimitiveTopology PrimitiveTopology
    {
        Direct3D10::PrimitiveTopology get(void);
        void set(Direct3D10::PrimitiveTopology topology);
    }

    /// <summary>
    /// Get the vertex buffers bound to the input-assembler stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::IAGetVertexBuffers)</para>
    /// </summary>
    /// <param name="startSlot">The input slot of the first vertex buffer to get. The first vertex buffer is explicitly bound to the start slot; this causes each additional vertex buffer in the array to be implicitly bound to each subsequent input slot. There are 16 input slots.</param>
    /// <param name="bufferCount">The number of vertex buffers to get starting at the offset. The number of buffers (plus the starting slot) cannot exceed the total number of IA-stage input slots.</param>
    /// <returns>A collection of vertex buffers returned by the method (see <see cref="VertexBuffer"/>)<seealso cref="VertexBuffer"/>.</returns>
    ReadOnlyCollection<VertexBuffer>^ GetVertexBuffers(UInt32 startSlot, UInt32 bufferCount);

    /// <summary>
    /// Bind an array of vertex buffers to the input-assembler stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::IASetVertexBuffers)</para>
    /// </summary>
    /// <param name="startSlot">The first input slot for binding. The first vertex buffer is explicitly bound to the start slot; this causes each additional vertex buffer in the array to be implicitly bound to each subsequent input slot. There are 16 input slots (ranges from 0 to D3D10_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="vertexBuffers">A collection of vertex buffers (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/>. 
    /// The number of buffers (plus the starting slot) cannot exceed the total number of IA-stage input slots (ranges from 0 to D3D10_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT - startSlot).
    /// The vertex buffers must have been created with the VertexBuffer flag.</param>
    /// <param name="strides">An array of stride values; one stride value for each buffer in the vertex-buffer array. Each stride is the size (in bytes) of the elements that are to be used from that vertex buffer.</param>
    /// <param name="offsets">An array of offset values; one offset value for each buffer in the vertex-buffer array. Each offset is the number of bytes between the first element of a vertex buffer and the first element that will be used.</param>
    void SetVertexBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ vertexBuffers, array<UInt32>^ strides, array<UInt32>^ offsets);

    /// <summary>
    /// Bind an array of vertex buffers to the input-assembler stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::IASetVertexBuffers)</para>
    /// </summary>
    /// <param name="startSlot">The first input slot for binding. The first vertex buffer is explicitly bound to the start slot; this causes each additional vertex buffer in the array to be implicitly bound to each subsequent input slot. There are 16 input slots (ranges from 0 to D3D10_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="vertexBuffers">
    /// A collection of vertex buffers (see <see cref="VertexBuffer"/>)<seealso cref="VertexBuffer"/>. 
    /// The number of buffers (plus the starting slot) cannot exceed the total number of IA-stage input slots (ranges from 0 to D3D10_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT - startSlot).
    /// The vertex buffers must have been created with the <see cref="BindingOptions" /><b>.VertexBuffer flag</b>.</param>
    void SetVertexBuffers(UInt32 startSlot, IEnumerable<VertexBuffer>^ vertexBuffers);

internal:

    InputAssemblerPipelineStage(void)
    { }

    InputAssemblerPipelineStage(D3DDevice^ parent) : PipelineStage(parent)
    { }
};
} } } }
