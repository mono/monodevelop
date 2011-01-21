//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Collections::ObjectModel;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

ref class Asynchronous;
ref class DepthStencilView;
ref class RenderTargetView;
ref class UnorderedAccessView;
ref class D3DResource;
ref class D3DBuffer;
ref class SamplerState;
ref class ComputeShader;
ref class ClassInstance;
ref class ShaderResourceView;
ref class DomainShader;
ref class CommandList;
ref class D3DPredicate;
ref class GeometryShader;
ref class HullShader;
ref class InputLayout;
ref class BlendState;
ref class DepthStencilState;
ref class PixelShader;
ref class RasterizerState;
ref class VertexShader;


//Pipeline stages forward defs
ref class ComputeShaderPipelineStage;
ref class DomainShaderPipelineStage;
ref class GeometryShaderPipelineStage;
ref class HullShaderPipelineStage;
ref class InputAssemblerPipelineStage;
ref class OutputMergerPipelineStage;
ref class PixelShaderPipelineStage;
ref class RasterizerPipelineStage;
ref class StreamOutputPipelineStage;
ref class VertexShaderPipelineStage;

    /// <summary>
    /// The DeviceContext interface represents a device context which generates rendering commands.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext)</para>
    /// </summary>
    public ref class DeviceContext :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::DeviceChild
    {
    public: 

        /// <summary>
        /// Mark the beginning of a series of commands.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::Begin)</para>
        /// </summary>
        /// <param name="asyncData">A Asynchronous object.</param>
        void Begin(Asynchronous^ asyncData);

        /// <summary>
        /// Clears the depth-stencil resource.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::ClearDepthStencilView)</para>
        /// </summary>
        /// <param name="depthStencilView">Pointer to the depth stencil to be cleared.</param>
        /// <param name="clearOptions">Identify the type of data to clear (see <see cref="ClearOptions"/>)<seealso cref="ClearOptions"/>.</param>
        /// <param name="depth">Clear the depth buffer with this value. This value will be clamped between 0 and 1.</param>
        /// <param name="stencil">Clear the stencil buffer with this value.</param>
        void ClearDepthStencilView(DepthStencilView^ depthStencilView, ClearOptions clearOptions, Single depth, Byte stencil);

        /// <summary>
        /// Set all the elements in a render target to one value.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::ClearRenderTargetView)</para>
        /// </summary>
        /// <param name="renderTargetView">Pointer to the rendertarget.</param>
        /// <param name="colorRgba">The color to fill the render target with.</param>
        void ClearRenderTargetView(RenderTargetView^ renderTargetView, ColorRgba colorRgba);

        /// <summary>
        /// Restore all default settings.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::ClearState)</para>
        /// </summary>
        void ClearState();

        /// <summary>
        /// Clears an unordered access resource with a float value.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::ClearUnorderedAccessViewFloat)</para>
        /// </summary>
        /// <param name="unorderedAccessView">Pointer to the Unordered Access View.</param>
        /// <param name="values">A 4-component array that contains the values.</param>
        void ClearUnorderedAccessViewFloat(UnorderedAccessView^ unorderedAccessView, array<Single>^ values);

        /// <summary>
        /// Clears an unordered access resource with bit-precise values.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::ClearUnorderedAccessViewUint)</para>
        /// </summary>
        /// <param name="unorderedAccessView">Pointer to the Unordered Access View.</param>
        /// <param name="values">A 4-component array that contains the values.</param>
        void ClearUnorderedAccessViewUInt32(UnorderedAccessView^ unorderedAccessView, array<UInt32>^ values);

        /// <summary>
        /// Copy the entire contents of the source resource to the destination resource using the GPU.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::CopyResource)</para>
        /// </summary>
        /// <param name="destinationResource">The destination resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="sourceResource">The source resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        void CopyResource(D3DResource^ destinationResource, D3DResource^ sourceResource);

        /// <summary>
        /// Copy a region from a source resource to a destination resource.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::CopySubresourceRegion)</para>
        /// </summary>
        /// <param name="destinationResource">The destination resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="destinationSubresource">Destination subresource index.</param>
        /// <param name="destinationX">The x offset between the source box location and the destination location.</param>
        /// <param name="destinationY">The y offset between the source box location and the destination location. For a 1D subresource, this must be zero.</param>
        /// <param name="destinationZ">The z offset between the source box location and the destination location. For a 1D or 2D subresource, this must be zero.</param>
        /// <param name="sourceResource">The source resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="sourceSubresource">Source subresource index.</param>
        /// <param name="sourceBox">A 3D box (see <see cref="Box"/>)<seealso cref="Box"/> that defines the source subresources that can be copied. The box must fit within the source resource.</param>
        void CopySubresourceRegion(D3DResource^ destinationResource, UInt32 destinationSubresource, UInt32 destinationX, UInt32 destinationY, UInt32 destinationZ, D3DResource^ sourceResource, UInt32 sourceSubresource, Box sourceBox);

        /// <summary>
        /// Copy a region from a source resource to a destination resource.
        /// Because the source box is not defined by this function, the entire source subresource is copied. 
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::CopySubresourceRegion)</para>
        /// </summary>
        /// <param name="destinationResource">The destination resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="destinationSubresource">Destination subresource index.</param>
        /// <param name="destinationX">The x offset between the source box location and the destination location.</param>
        /// <param name="destinationY">The y offset between the source box location and the destination location. For a 1D subresource, this must be zero.</param>
        /// <param name="destinationZ">The z offset between the source box location and the destination location. For a 1D or 2D subresource, this must be zero.</param>
        /// <param name="sourceResource">The source resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="sourceSubresource">Source subresource index.</param>
        void CopySubresourceRegion(D3DResource^ destinationResource, UInt32 destinationSubresource, UInt32 destinationX, UInt32 destinationY, UInt32 destinationZ, D3DResource^ sourceResource, UInt32 sourceSubresource);

        /// <summary>
        /// Execute a command list from a thread group.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::Dispatch)</para>
        /// </summary>
        /// <param name="threadGroupCountX">The index of the thread in the x direction.</param>
        /// <param name="threadGroupCountY">The index of the thread in the y direction.</param>
        /// <param name="threadGroupCountZ">The index of the thread in the z direction.</param>
        void Dispatch(UInt32 threadGroupCountX, UInt32 threadGroupCountY, UInt32 threadGroupCountZ);

        /// <summary>
        /// Execute a command list to draw GPU-generated primitives over one of more thread groups.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DispatchIndirect)</para>
        /// </summary>
        /// <param name="bufferForArgs">A D3DBuffer, which must be loaded with data that matches the argument list for DeviceContext.Dispatch.</param>
        /// <param name="alignedOffsetForArgs">A byte-aligned offset between the start of the buffer and the arguments.</param>
        void DispatchIndirect(D3DBuffer^ bufferForArgs, UInt32 alignedOffsetForArgs);

        /// <summary>
        /// Draw non-indexed, non-instanced primitives.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::Draw)</para>
        /// </summary>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex, which is usually an offset in a vertex buffer; it could also be used as the first vertex id generated for a shader parameter marked with the SV_TargetId system-value semantic.</param>
        void Draw(UInt32 vertexCount, UInt32 startVertexLocation);

        /// <summary>
        /// Draw geometry of an unknown size.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DrawAuto)</para>
        /// </summary>
        void DrawAuto();

        /// <summary>
        /// Draw indexed, non-instanced primitives.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DrawIndexed)</para>
        /// </summary>
        /// <param name="indexCount">Number of indices to draw.</param>
        /// <param name="startIndexLocation">The location of the first index read by the GPU from the index buffer.</param>
        /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the vertex buffer.</param>
        void DrawIndexed(UInt32 indexCount, UInt32 startIndexLocation, Int32 baseVertexLocation);

        /// <summary>
        /// Draw indexed, instanced primitives.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DrawIndexedInstanced)</para>
        /// </summary>
        /// <param name="indexCountPerInstance">Number of indices read from the index buffer for each instance.</param>
        /// <param name="instanceCount">Number of instances to draw.</param>
        /// <param name="startIndexLocation">The location of the first index read by the GPU from the index buffer.</param>
        /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the vertex buffer.</param>
        /// <param name="startInstanceLocation">A value added to each index before reading per-instance data from a vertex buffer.</param>
        void DrawIndexedInstanced(UInt32 indexCountPerInstance, UInt32 instanceCount, UInt32 startIndexLocation, Int32 baseVertexLocation, UInt32 startInstanceLocation);

        /// <summary>
        /// Draw indexed, instanced, GPU-generated primitives.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DrawIndexedInstancedIndirect)</para>
        /// </summary>
		/// <param name="bufferForArgs">A pointer to a <see cref="Direct3D11::D3DBuffer" />, which is a buffer containing the GPU generated primitives.</param>
        /// <param name="offset">Offset into bufferForArgs to the start of the GPU-generated primitives</param>
        void DrawIndexedInstancedIndirect(D3DBuffer^ bufferForArgs, UInt32 offset);

        /// <summary>
        /// Draw non-indexed, instanced primitives.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DrawInstanced)</para>
        /// </summary>
        /// <param name="vertexCountPerInstance">Number of vertices to draw.</param>
        /// <param name="instanceCount">Number of instances to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex.</param>
        /// <param name="startInstanceLocation">A value added to each index before reading per-instance data from a vertex buffer.</param>
        void DrawInstanced(UInt32 vertexCountPerInstance, UInt32 instanceCount, UInt32 startVertexLocation, UInt32 startInstanceLocation);

        /// <summary>
        /// Draw instances, GPU-generated primitives
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::DrawInstancedIndirect)</para>
        /// </summary>
		/// <param name="bufferForArgs">A pointer to a <see cref="Direct3D11::D3DBuffer" />, which is a buffer containing the GPU-generated primitives</param>
        /// <param name="offset">Offset in bufferForArgs to the start of the GPU-generated primitives</param>
        void DrawInstancedIndirect(D3DBuffer^ bufferForArgs, UInt32 offset);

        /// <summary>
        /// Mark the end of a series of commands.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::End)</para>
        /// </summary>
        /// <param name="async">An Asynchronous object.</param>
        void End(Asynchronous^ async);

        /// <summary>
        /// Queues commands from a command list onto a device.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::ExecuteCommandList)</para>
        /// </summary>
        /// <param name="commandList">A CommandList interface that encapsulates a command list.</param>
        /// <param name="restoreContextState">A Boolean flag that determines whether the immediate context state is saved (prior) and restored (after) the execution of a command list. Use TRUE to indicate that the runtime needs to save and restore the state, which will cause lower performance. Use FALSE to indicate that no state shall be saved or restored, which causes the immediate context to return to its default state after the command list executes.</param>
        void ExecuteCommandList(CommandList^ commandList, Boolean restoreContextState);

        /// <summary>
        /// Create a command list and record graphics commands into it.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::FinishCommandList)</para>
        /// </summary>
        /// <param name="restoreDeferredContextState">A Boolean flag that determines whether the immediate context state is saved (prior) and restored (after) the execution of a command list. Use TRUE to indicate that the runtime needs to save and restore the state, which will cause lower performance. Use FALSE to indicate that no state shall be saved or restored, which causes the immediate context to return to its default state after the command list executes.</param>
        /// <returns>
        /// A CommandList instance initialized with the recorded command list information.
        /// </returns>
        CommandList^ FinishCommandList(Boolean restoreDeferredContextState);

        /// <summary>
        /// Send queued-up commands in the command buffer to the GPU.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::Flush)</para>
        /// </summary>
        void Flush();

        /// <summary>
        /// Generate mipmaps for the given shader resource.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::GenerateMips)</para>
        /// </summary>
        /// <param name="shaderResourceView">An ShaderResourceView interface that represents the shader resource.</param>
        void GenerateMips(ShaderResourceView^ shaderResourceView);

        /// <summary>
        /// Gets the initialization flags associated with the current deferred context.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::GetContextFlags)</para>
        /// </summary>
        property UInt32 ContextOptions
        {
            UInt32 get();
        }

        /// <summary>
        /// Get data from the GPU asynchronously.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::GetData)</para>
        /// </summary>
        /// <param name="asyncData">A Asynchronous object.</param>
        /// <param name="data">Address of memory that will receive the data. If NULL, GetData will be used only to check status. The type of data output depends on the type of asynchronous object. See Remarks.</param>
        /// <param name="dataSize">Size of the data to retrieve or 0. Must be 0 when pData is NULL.</param>
        /// <param name="options">Optional flags. Can be 0 or any combination of the flags enumerated by AsyncGetDataOptions.</param>
        void GetData(Asynchronous^ asyncData, IntPtr data, UInt32 dataSize, AsyncGetDataOptions options);

        /// <summary>
        /// Get the rendering predicate state.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::GetPredication)</para>
        /// </summary>
        /// <param name="outPredicate">Address of a predicate (see <see cref="D3DPredicate"/>)<seealso cref="D3DPredicate"/>. Value stored here will be NULL upon device creation.</param>
        /// <param name="outPredicateValue">Address of a boolean to fill with the predicate comparison value. FALSE upon device creation.</param>
        property D3DPredicate^ Predication
        {
            D3DPredicate^ get(void);
            void set(D3DPredicate^ predicate);
        }


        /// <summary>
        /// Gets the minimum level-of-detail (LOD).
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::GetResourceMinLOD)</para>
        /// </summary>
        /// <param name="resource">A D3DResource which represents the resource.</param>
        Single GetResourceMinimumLevelOfDetail(D3DResource^ resource);

        // REVIEW: System.Object already has GetType() method; this method should be
        // named something else

        /// <summary>
        /// Gets the type of device context.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::GetType)</para>
        /// </summary>
        property DeviceContextType DeviceContextType
        {
            Direct3D11::DeviceContextType get(void);
        }

        /// <summary>
        /// Get the data contained in a subresource, and deny the GPU access to that subresource.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::Map)</para>
        /// </summary>
        /// <param name="resource">A D3DResource object.</param>
        /// <param name="subresource">Index number of the subresource.</param>
        /// <param name="mapType">Specifies the CPU's read and write permissions for a resource. For possible values, see Map.</param>
        /// <param name="mapOptions">Flag that specifies what the CPU should do when the GPU is busy. This flag is optional.</param>
        /// <returns>The mapped subresource (see <see cref="MappedSubresource"/>)<seealso cref="MappedSubresource"/>.</returns>
        MappedSubresource Map(D3DResource^ resource, UInt32 subresource, Direct3D11::Map mapType, MapOptions mapOptions);

        /// <summary>
        /// Copy a multisampled resource into a non-multisampled resource.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::ResolveSubresource)</para>
        /// </summary>
        /// <param name="destinationResource">Destination resource. Must be a created with the Default flag and be single-sampled. See D3DResource.</param>
        /// <param name="destinationSubresource">A zero-based index, that identifies the destination subresource. Use D3D11CalcSubresource to calculate the index.</param>
        /// <param name="sourceResource">Source resource. Must be multisampled.</param>
        /// <param name="sourceSubresource">>The source subresource of the source resource.</param>
        /// <param name="format">A Format that indicates how the multisampled resource will be resolved to a single-sampled resource.</param>
        void ResolveSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, D3DResource^ sourceResource, UInt32 sourceSubresource, Format format);

        /// <summary>
        /// Set a rendering predicate.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::SetPredication)</para>
        /// </summary>
        /// <param name="predicate">A predicate (see <see cref="D3DPredicate"/>)<seealso cref="D3DPredicate"/>. A NULL value indicates "no" predication; in this case, the value of PredicateValue is irrelevent but will be preserved for DeviceContext.GetPredication.</param>
        /// <param name="predicateValue">If TRUE, rendering will be affected by when the predicate's conditions are met. If FALSE, rendering will be affected when the conditions are not met.</param>
        void SetPredication(D3DPredicate^ predicate, Boolean predicateValue);

        /// <summary>
        /// Sets the minimum level-of-detail (LOD).
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::SetResourceMinLOD)</para>
        /// </summary>
        /// <param name="resource">A D3DResource which represents the resource.</param>
        /// <param name="minimum">The level-of-detail, which ranges between 0 and 1.</param>
        void SetResourceMinimumLevelOfDetail(D3DResource^ resource, Single minimum);

        /// <summary>
        /// Invalidate the resource and re-enable the GPU's access to that resource.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::Unmap)</para>
        /// </summary>
        /// <param name="resource">A D3DResource object.</param>
        /// <param name="subresource">A subresource to be unmapped.</param>
        void Unmap(D3DResource^ resource, UInt32 subresource);

        /// <summary>
        /// The CPU copies data from memory to a subresource created in non-mappable memory.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::UpdateSubresource)</para>
        /// </summary>
        /// <param name="destinationResource">The destination resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="destinationSubresource">A zero-based index, that identifies the destination subresource. See D3D11CalcSubresource for more details.</param>
        /// <param name="destinationBox">A box that defines the portion of the destination subresource to copy the resource data into. Coordinates are in bytes for buffers and in texels for textures.  The dimensions of the source must fit the destination (see <see cref="Box"/>)<seealso cref="Box"/>.</param>
        /// <param name="sourceData">The source data in memory.</param>
        /// <param name="sourceRowPitch">The size of one row of the source data.</param>
        /// <param name="sourceDepthPitch">The size of one depth slice of source data.</param>
        void UpdateSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, IntPtr sourceData, UInt32 sourceRowPitch, UInt32 sourceDepthPitch, Box destinationBox);

        /// <summary>
        /// The CPU copies data from memory to a subresource created in non-mappable memory.
        /// A destination box is not defined by this method, so the data is written to the destination subresource with no offset.
        /// <para>(Also see DirectX SDK: ID3D11DeviceContext::UpdateSubresource)</para>
        /// </summary>
        /// <param name="destinationResource">The destination resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="destinationSubresource">A zero-based index, that identifies the destination subresource. See D3D11CalcSubresource for more details.</param>
        /// <param name="sourceData">The source data in memory.</param>
        /// <param name="sourceRowPitch">The size of one row of the source data.</param>
        /// <param name="sourceDepthPitch">The size of one depth slice of source data.</param>
        void UpdateSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, IntPtr sourceData, UInt32 sourceRowPitch, UInt32 sourceDepthPitch);

        /// <summary>
        /// Get the associated compute shader pipelines tage object.
        /// </summary>
        property ComputeShaderPipelineStage^ CS
        { 
            ComputeShaderPipelineStage^ get();
        }

        /// <summary>
        /// Get the associated domain shader pipeline stage object.
        /// </summary>
        property DomainShaderPipelineStage^ DS
        { 
            DomainShaderPipelineStage^ get();
        }

        /// <summary>
        /// Get the associated geometry shader pipeline stage object.
        /// </summary>
        property GeometryShaderPipelineStage^ GS
        { 
            GeometryShaderPipelineStage^ get();
        }

        /// <summary>
        /// Get the associated hull shader pipeline stage object.
        /// </summary>
        property HullShaderPipelineStage^ HS
        { 
            HullShaderPipelineStage^ get();
        }

        /// <summary>
        /// Get the associated input assembler pipeline stage object.
        /// </summary>
        property InputAssemblerPipelineStage^ IA
        { 
            InputAssemblerPipelineStage^ get();
        }

        /// <summary>
        /// Get the associated output merger pipeline stage object.
        /// </summary>
        property OutputMergerPipelineStage^ OM
        { 
            OutputMergerPipelineStage^ get();
        }

        /// <summary>
        /// Get the associated pixel shader pipeline stage object.
        /// </summary>
        property PixelShaderPipelineStage^ PS
        { 
            PixelShaderPipelineStage^ get();
        }

        /// <summary>
        /// Get the associated rasterizer pipeline stage object.
        /// </summary>
        property RasterizerPipelineStage^ RS
        { 
            RasterizerPipelineStage^ get();
        }

        /// <summary>
        /// Get the associated stream output pipeline stage object.
        /// </summary>
        property StreamOutputPipelineStage^ SO
        { 
            StreamOutputPipelineStage^ get();
        }

        /// <summary>
        /// Get the associated vertex shader pipeline stage object.
        /// </summary>
        property VertexShaderPipelineStage^ VS
        { 
            VertexShaderPipelineStage^ get();
        }

    internal:

        DeviceContext(void)
        { }

		DeviceContext(ID3D11DeviceContext* pNativeID3D11DeviceContext) : DeviceChild(pNativeID3D11DeviceContext)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11DeviceContext)); }
        }

    private:
        ComputeShaderPipelineStage^ m_CS;
        DomainShaderPipelineStage^ m_DS;
        GeometryShaderPipelineStage^ m_GS;
        HullShaderPipelineStage^ m_HS;
        InputAssemblerPipelineStage^ m_IA;
        OutputMergerPipelineStage^ m_OM;
        PixelShaderPipelineStage^ m_PS;
        RasterizerPipelineStage^ m_RS;
        StreamOutputPipelineStage^ m_SO;
        VertexShaderPipelineStage^ m_VS;
    };
} } } }
