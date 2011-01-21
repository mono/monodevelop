//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11PipelineStage.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

/// <summary>
/// StreamOutput pipeline stage. 
/// </summary>
public ref class StreamOutputPipelineStage : PipelineStage
{
public:

    /// <summary>
    /// Get the target output buffers for the stream-output stage of the pipeline.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::SOGetTargets)</para>
    /// </summary>
    /// <param name="bufferCount">Number of buffers to get.</param>
    /// <returns>A collection of output buffers (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> to be retrieved from the device.</returns>
    ReadOnlyCollection<D3DBuffer^>^ GetTargets(UInt32 bufferCount);

    /// <summary>
    /// Set the target output buffers for the stream-output stage of the pipeline.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::SOSetTargets)</para>
    /// </summary>
    /// <param name="targets">The collection of output buffers (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> to bind to the device. 
    /// The buffers must have been created with the <b>StreamOutput</b> flag.
    /// A maximum of four output buffers can be set. If less than four are defined by the call, the remaining buffer slots are set to NULL.</param>
    /// <param name="offsets">Array of offsets to the output buffers from ppBuffers, 
    /// one offset for each buffer. The offset values must be in bytes.</param>
    void SetTargets(IEnumerable<D3DBuffer^>^ targets, array<UInt32>^ offsets);
protected:
    StreamOutputPipelineStage() {}
internal:
    StreamOutputPipelineStage(DeviceContext^ parent) : PipelineStage(parent)
    {
    }
};
} } } }
