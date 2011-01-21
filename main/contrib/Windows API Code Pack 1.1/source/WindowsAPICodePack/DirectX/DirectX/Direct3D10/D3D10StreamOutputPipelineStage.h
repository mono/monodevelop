//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10PipelineStage.h"
#include "D3D10Buffer.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

/// <summary>
/// StreamOutput pipeline stage. 
/// </summary>
public ref class StreamOutputPipelineStage : PipelineStage
{
public:

    /// <summary>
    /// Gets or sets the target output buffers for the stream-output stage of the pipeline.
    /// See <see cref="OutputBuffer" />.
    /// <para>(Also see DirectX SDK: ID3D10Device::SOGetTargets, ID3D10Device::SOSetTargets)</para>
    /// </summary>
    /// <remarks>
    /// Always returns four targets. It may be set with fewer than four, in which case
    /// the remaining targets will be set to null.
    /// </remarks>
    property IEnumerable<OutputBuffer>^ Targets
    {
        IEnumerable<OutputBuffer>^ get(void);
        void set(IEnumerable<OutputBuffer>^ targets);
    }

protected:

    // It is odd that this constructor is 'protected'. Why not 'internal' like
    // all the other types? Client code isn't really expected to inherit this type,
    // is it?

    StreamOutputPipelineStage(void)
    { }

internal:

    StreamOutputPipelineStage(D3DDevice^ parent) : PipelineStage(parent)
    { }

private:

    literal int maximumOutputBufferCount = 4;
};
} } } }
