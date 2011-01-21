//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10PipelineStage.h"
#include "D3D10OutputMergerBlendState.h"
#include "D3D10OutputMergerRenderTargets.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

/// <summary>
/// OutputMerger pipeline stage. 
/// </summary>
public ref class OutputMergerPipelineStage : PipelineStage
{
public:

    /// <summary>
    /// Get or sets the blend state of the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::OMGetBlendState, ID3D10Device::OMSetBlendState)</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// If OutputMergerBlendState.BlendState is null, the default blending state will be set.
    /// See the DX SDK (ID3D10Device::OMSetBlendState) for more details about the default blending state.
    /// </para>
    /// <para>
    /// A sample mask determines which samples get updated in all the active render targets. 
    /// The mapping of bits in a sample mask to samples in a multisample render target is 
    /// the responsibility of an individual application. 
    /// A sample mask is always applied; it is independent of whether multisampling is enabled, 
    /// and does not depend on whether an application uses multisample render targets.
    /// </para>
    /// </remarks>
    property OutputMergerBlendState^ BlendState
    {
        OutputMergerBlendState^ get(void);
        void set(OutputMergerBlendState^ mergerBlendState);
    }

    /// <summary>
    /// Get or sets render targets and depth-stencil buffer bound to the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::OMGetRenderTargets, ID3D10Device::OMSetRenderTargets)</para>
    /// </summary>
	property OutputMergerRenderTargets^ RenderTargets
	{
		OutputMergerRenderTargets^ get(void);
		void set(OutputMergerRenderTargets^ renderTargets);
	}

    /// <summary>
    /// Get the depth stencil view bound to the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::OMGetRenderTargets)</para>
    /// </summary>
    property DepthStencilView^ DepthStencilView
    {
        Direct3D10::DepthStencilView^ get(void);

        // REVIEW: could implement a setter with the existing API, but it would
        // be computationally expensive; the view can only be set with OMSetRenderTargets()
        // which requires both the render and stencil views to be set at the same time.
        // Neither argument is optional, so the setter for this property would have to build
        // an array of render views, by calling OMGetRenderTargets(), passing that back
        // to OMSetRenderTargets(), and the releasing all the interfaces that had been
        // returned by OMGetRenderTargets().
        //
        // But, if it turns out that there's a better way to do it -- e.g. cache render
        // views so that they can be reused in a setter here -- it might be worth revisiting.
    }

	// REVIEW: should check to see if it's actually allowed to pass NULL for the "pStencilRef"
	// in the native call to OMGetDepthStencilState; if so, could have a read-only
	// DepthStencilState property here for convenience.

    /// <summary>
    /// Gets the depth-stencil state of the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::OMGetDepthStencilState)</para>
    /// </summary>
    /// <param name="stencilRef">The stencil reference value used in the depth-stencil test.</param>
    /// <returns>A depth-stencil state object (see <see cref="DepthStencilState"/>)<seealso cref="DepthStencilState"/> to be filled with information from the device.</returns>
    DepthStencilState^ GetDepthStencilStateAndReferenceValue([System::Runtime::InteropServices::Out] UInt32 %stencilRef);

    /// <summary>
    /// Sets the depth-stencil state of the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::OMSetDepthStencilState)</para>
    /// </summary>
    /// <param name="depthStencilState">A depth-stencil state interface (see <see cref="DepthStencilState"/>)<seealso cref="DepthStencilState"/> to bind to the device. Set this to NULL to use the default state listed in DepthStencilDescription.</param>
    /// <param name="stencilRef">Reference value to perform against when doing a depth-stencil test.</param>
    void SetDepthStencilStateAndReferenceValue(DepthStencilState^ depthStencilState, UInt32 stencilRef);

internal:

    OutputMergerPipelineStage(void)
    { }

    OutputMergerPipelineStage(D3DDevice^ parent) : PipelineStage(parent)
    { }
};
} } } }
