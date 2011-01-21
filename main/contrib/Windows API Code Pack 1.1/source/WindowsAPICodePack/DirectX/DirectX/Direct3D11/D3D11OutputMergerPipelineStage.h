//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11PipelineStage.h"
#include "D3D11OutputMergerBlendState.h"
#include "D3D11OutputMergerRenderTargets.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

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
    /// Gets the depth-stencil state of the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::OMGetDepthStencilState)</para>
    /// </summary>
    /// <returns>A depth-stencil state object (see <see cref="DepthStencilState"/>)<seealso cref="DepthStencilState"/> to be filled with information from the device.</returns>
    property DepthStencilState^ DepthStencilState
    {
        Direct3D11::DepthStencilState^ get(void);
    }

    /// <summary>
    /// Get the depth stencil view bound to the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::OMGetRenderTargets)</para>
    /// </summary>
    /// <returns>The depth-stencil view bound to the output-merger stage.</returns>
    property DepthStencilView^ DepthStencilView
    {
        Direct3D11::DepthStencilView^ get(void);
    }

    /// <summary>
    /// Gets the depth-stencil state of the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::OMGetDepthStencilState)</para>
    /// </summary>
    /// <param name="stencilRef">The stencil reference value used in the depth-stencil test.</param>
    /// <returns>A depth-stencil state object (see <see cref="DepthStencilState"/>)<seealso cref="DepthStencilState"/> to be filled with information from the device.</returns>
    Direct3D11::DepthStencilState^ GetDepthStencilStateAndReferenceValue([System::Runtime::InteropServices::Out] UInt32 % stencilRef);

    /// <summary>
    /// Sets the depth-stencil state of the output-merger stage with a reference value.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::OMSetDepthStencilState)</para>
    /// </summary>
    /// <param name="depthStencilState">A depth-stencil state interface (see <see cref="DepthStencilState"/>)<seealso cref="DepthStencilState"/> to bind to the device. Set this to NULL to use the default state listed in DepthStencilDescription.</param>
    /// <param name="stencilRef">Reference value to perform against when doing a depth-stencil test.</param>
    void SetDepthStencilStateAndReferenceValue(Direct3D11::DepthStencilState^ depthStencilState, UInt32 stencilRef);

    property OutputMergerRenderTargets^ RenderTargets
    {
        OutputMergerRenderTargets^ get(void);
        void set(OutputMergerRenderTargets^ renderTargets);
    }

protected:

    // REVIEW: why 'protected'?
    OutputMergerPipelineStage(void)
    { }

internal:

    OutputMergerPipelineStage(DeviceContext^ parent) : PipelineStage(parent)
    { }

private:

    /// <summary>
    /// Bind one or more render targets atomically and the depth-stencil buffer to the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::OMSetRenderTargets)</para>
    /// </summary>
    /// <param name="renderTargetViews">A collection of render targets (see <see cref="RenderTargetView"/>)<seealso cref="RenderTargetView"/> to bind to the device (ranges between 0 and D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT). 
    /// If this parameter is null, no render targets are bound.</param>
    /// <param name="depthStencilView">A depth-stencil view (see <see cref="DepthStencilView"/>)<seealso cref="DepthStencilView"/> to bind to the device. 
    /// If this parameter is null, the depth-stencil state is not bound.</param>
    void SetRenderTargets(IEnumerable<RenderTargetView^>^ renderTargetViews, Direct3D11::DepthStencilView^ depthStencilView);

    /// <summary>
    /// Bind resources to the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::OMSetRenderTargetsAndUnorderedAccessViews)</para>
    /// </summary>
    /// <param name="renderTargetViews">A collection of ID3D11RenderTargetViews, which represent render-target views. Specify NULL to set none.</param>
    /// <param name="depthStencilView">A DepthStencilView, which represents a depth-stencil view. Specify NULL to set none.</param>
    /// <param name="viewStartSlot">Index into a zero-based array to begin setting unordered access views (ranges from 0 to D3D11_PS_CS_UAV_REGISTER_COUNT - 1).</param>
    /// <param name="unorderedAccessViews">A collection of ID3D11UnorderedAccessViews, which represent unordered access views.</param>
    /// <param name="initialCounts">An array The number of unordered access views to set.</param>
    void SetRenderTargetsAndUnorderedAccessViews(IEnumerable<RenderTargetView^>^ renderTargetViews, Direct3D11::DepthStencilView^ depthStencilView, UInt32 viewStartSlot, IEnumerable<UnorderedAccessView^>^ unorderedAccessViews, IEnumerable<UInt32>^ initialCounts);

};
} } } }
