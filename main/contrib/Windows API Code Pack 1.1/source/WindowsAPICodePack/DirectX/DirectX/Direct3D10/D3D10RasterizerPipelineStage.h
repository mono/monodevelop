//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10PipelineStage.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

/// <summary>
/// Rasterizer pipeline stage. 
/// </summary>
public ref class RasterizerPipelineStage : PipelineStage
{
public:

    /// <summary>
    /// Get the collection of scissor rectangles bound to the rasterizer stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::RSGetScissorRects)</para>
    /// </summary>
    /// <returns>A collection of scissor rectangles (see <see cref="D3DRect"/>)<seealso cref="D3DRect"/>.
    /// If maximumNumberOfRects is greater than the number of scissor rects currently bound, 
    /// then the collection will be trimmed to contain only the currently bound. 
    /// </returns>
    IEnumerable<D3DRect>^ BoundedGetScissorRects(UInt32 maxNumberOfRects);

    /// <summary>
    /// Gets or sets the rasterizer state from the rasterizer stage of the pipeline.
    /// <para>(Also see DirectX SDK: ID3D10Device::RSGetState, ID3D10Device::RSSetState)</para>
    /// </summary>
    property RasterizerState^ State
    {
        RasterizerState^ get(void);
        void set(RasterizerState^ rasterizerState);
    }

    /// <summary>
    /// Get the array of viewports bound to the rasterizer stage
    /// <para>(Also see DirectX SDK: ID3D10Device::RSGetViewports)</para>
    /// </summary>
    /// <param name="maxNumberOfViewports">The input specifies the maximum number of viewports (ranges from 0 to D3D10_VIEWPORT_AND_SCISSORRECT_OBJECT_COUNT_PER_PIPELINE) to retrieve, the output contains the actual number of viewports returned.</param>
    /// <returns>A collection of Viewport structures that are bound to the device. 
    /// If the number of viewports (in maxNumberOfViewports) is greater than the actual number of viewports currently bound, 
    /// then the collection will be trimmed to contain only the currently bound. 
    /// </returns>
    IEnumerable<Viewport>^ BoundedGetViewports(UInt32 maxNumberOfViewports);

    /// <summary>
    /// Bind an array of scissor rectangles to the rasterizer stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::RSSetScissorRects)</para>
    /// </summary>
    /// <param name="rects">A collection of scissor rectangles (see <see cref="D3DRect"/>)<seealso cref="D3DRect"/>.</param>
    property IEnumerable<D3DRect>^ ScissorRects
    {
        IEnumerable<D3DRect>^ get(void);
        void set(IEnumerable<D3DRect>^ rects);
    }

    /// <summary>
    /// Bind an array of viewports to the rasterizer stage of the pipeline.
    /// <para>(Also see DirectX SDK: ID3D10Device::RSSetViewports)</para>
    /// </summary>
    /// <param name="viewports">A collection of Viewport structures to bind to the device.</param>
    property IEnumerable<Viewport>^ Viewports
    {
        IEnumerable<Viewport>^ get();
        void set(IEnumerable<Viewport>^ viewports);
    }

protected:
    RasterizerPipelineStage() {}
internal:
    RasterizerPipelineStage(D3DDevice^ parent) : PipelineStage(parent)
    {
    }
};
} } } }
