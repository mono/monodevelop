//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "D3D10EffectPool.h"
#include "D3D10CheckCounterData.h"
#include "D3D10StateBlock.h"
#include "D3D10StateBlockMask.h"
#include "D3D10EffectTechnique.h"
#include "D3D10EffectPass.h"

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {
    ref class Adapter;
    ref class GraphicsObject;
    ref class Device;
    ref class Device1;
    ref class SwapChain;
}}}}

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {
    ref class DepthStencilView;
    ref class RenderTargetView;
    ref class D3DResource;
    ref class BlendState;
    ref class D3DBuffer;
    ref class D3DCounter;
    ref class DepthStencilState;
    ref class GeometryShader;
    ref class InputLayout;
    ref class PixelShader;
    ref class D3DPredicate;
    ref class D3DQuery;
    ref class RasterizerState;
    ref class SamplerState;
    ref class ShaderResourceView;
    ref class Texture1D;
    ref class Texture2D;
    ref class Texture3D;
    ref class VertexShader;
    ref class InfoQueue;
    ref class SwitchToRef;
	ref class Multithread;
	ref class D3DDebug;

    //Pipeline stages forward defs
    ref class GeometryShaderPipelineStage;
    ref class InputAssemblerPipelineStage;
    ref class OutputMergerPipelineStage;
    ref class PixelShaderPipelineStage;
    ref class RasterizerPipelineStage;
    ref class StreamOutputPipelineStage;
    ref class VertexShaderPipelineStage;

    /// <summary>
    /// The device interface represents a virtual adapter for Direct3D 10.0; it is used to perform rendering and create Direct3D resources.
    /// <para>To create a D3DDevice instance, use one of the static factory method overloads: CreateDevice() or CreateDeviceAndSwapChain().</para>
    /// <para>(Also see DirectX SDK: ID3D10Device)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")
    public ref class D3DDevice :
        public Microsoft::WindowsAPICodePack::DirectX::DirectUnknown
    {
    public: 

        /// <summary>
        /// Get the type, name, units of measure, and a description of an existing counter.
        /// <para>(Also see DirectX SDK: ID3D10Device::CheckCounter)</para>
        /// </summary>
        /// <param name="counterDescription">A counter description (see <see cref="CounterDescription"/>)<seealso cref="CounterDescription"/>. Specifies which counter information is to be retrieved about.</param>
        /// <returns>A CheckCounterData object containing the values associated with the counter</returns>
        CounterData^ GetCounterData(CounterDescription counterDescription);

        /// <summary>
        /// Gets information about the device's performance counter capabilities
        /// <para>(Also see DirectX SDK: ID3D10Device::CheckCounterInfo)</para>
        /// </summary>
        property CounterInformation CounterInformation
        {
            Direct3D10::CounterInformation get(void);
        }

        /// <summary>
        /// Get the support of a given format on the installed video device.
        /// <para>(Also see DirectX SDK: ID3D10Device::CheckFormatSupport)</para>
        /// </summary>
        /// <param name="format">A Format enumeration that describes a format for which to check for support.</param>
        /// <returns>A FormatSupportOptions enumeration values describing how the specified format is supported on the installed device. The values are ORed together.</returns>
        FormatSupportOptions GetFormatSupport(Format format);


        /// <summary>
        /// Get the number of quality levels available during multisampling.
        /// <para>(Also see DirectX SDK: ID3D10Device::CheckMultisampleQualityLevels)</para>
        /// </summary>
        /// <param name="format">The texture format. See Format.</param>
        /// <param name="sampleCount">The number of samples during multisampling.</param>
        /// <returns>Number of quality levels supported by the adapter.</returns>
        UInt32 GetMultisampleQualityLevels(Format format, UInt32 sampleCount);

        /// <summary>
        /// Clears the depth-stencil resource.
        /// <para>(Also see DirectX SDK: ID3D10Device::ClearDepthStencilView)</para>
        /// </summary>
        /// <param name="depthStencilView">Pointer to the depth stencil to be cleared.</param>
        /// <param name="options">Which parts of the buffer to clear. See ClearOptions.</param>
        /// <param name="depth">Clear the depth buffer with this value. This value will be clamped between 0 and 1.</param>
        /// <param name="stencil">Clear the stencil buffer with this value.</param>
        void ClearDepthStencilView(DepthStencilView^ depthStencilView, ClearOptions options, Single depth, Byte stencil);

        /// <summary>
        /// Set all the elements in a render target to one value.
        /// <para>(Also see DirectX SDK: ID3D10DeviceContext::ClearRenderTargetView)</para>
        /// </summary>
        /// <param name="renderTargetView">Pointer to the rendertarget.</param>
        /// <param name="colorRgba">The color to fill the render target with.</param>
        void ClearRenderTargetView(RenderTargetView^ renderTargetView, ColorRgba colorRgba);

        /// <summary>
        /// Restore all default device settings; return the device to the state it was in when it was created. This will set all set all input/output resource slots, shaders, input layouts, predications, scissor rectangles, depth-stencil state, rasterizer state, blend state, sampler state, and viewports to NULL. The primitive topology will be set to UNDEFINED.
        /// <para>(Also see DirectX SDK: ID3D10Device::ClearState)</para>
        /// </summary>
        void ClearState();

        /// <summary>
        /// Copy the entire contents of the source resource to the destination resource using the GPU.
        /// <para>(Also see DirectX SDK: ID3D10Device::CopyResource)</para>
        /// </summary>
        /// <param name="destinationResource">The destination resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="sourceResource">The source resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        void CopyResource(D3DResource^ destinationResource, D3DResource^ sourceResource);

        /// <summary>
        /// Copy a region from a source resource to a destination resource.
        /// <para>(Also see DirectX SDK: ID3D10Device::CopySubresourceRegion)</para>
        /// </summary>
        /// <param name="destinationResource">The destination resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="destinationSubresource">Destination subresource index.</param>
        /// <param name="destinationX">The x coordinate of the upper left corner of the destination region.</param>
        /// <param name="destinationY">The y coordinate of the upper left corner of the destination region.</param>
        /// <param name="destinationZ">The z coordinate of the upper left corner of the destination region. For a 1D or 2D subresource, this must be zero.</param>
        /// <param name="sourceResource">The source resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="sourceSubresourceIndex">Source subresource index.</param>
        /// <param name="sourceBox">A 3D box (see <see cref="Box"/>)<seealso cref="Box"/> that defines the source subresources that can be copied. The box must fit within the source resource.</param>
        void CopySubresourceRegion(D3DResource^ destinationResource, UInt32 destinationSubresource, UInt32 destinationX, UInt32 destinationY, UInt32 destinationZ, D3DResource^ sourceResource, UInt32 sourceSubresourceIndex, Box sourceBox);

        /// <summary>
        /// Copy a region from a source resource to a destination resource.
        /// The entire source subresource is copied.
        /// <para>(Also see DirectX SDK: ID3D10Device::CopySubresourceRegion)</para>
        /// </summary>
        /// <param name="destinationResource">The destination resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="destinationSubresource">Destination subresource index.</param>
        /// <param name="destinationX">The x coordinate of the upper left corner of the destination region.</param>
        /// <param name="destinationY">The y coordinate of the upper left corner of the destination region.</param>
        /// <param name="destinationZ">The z coordinate of the upper left corner of the destination region. For a 1D or 2D subresource, this must be zero.</param>
        /// <param name="sourceResource">The source resource (see <see cref="D3DResource"/>)<seealso cref="D3DResource"/>.</param>
        /// <param name="sourceSubresourceIndex">Source subresource index.</param>
        void CopySubresourceRegion(D3DResource^ destinationResource, UInt32 destinationSubresource, UInt32 destinationX, UInt32 destinationY, UInt32 destinationZ, D3DResource^ sourceResource, UInt32 sourceSubresourceIndex);

        /// <summary>
        /// Create a blend-state object that encapsules blend state for the output-merger stage.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateBlendState)</para>
        /// </summary>
        /// <param name="blendStateDescription">A blend-state description (see <see cref="BlendDescription"/>)<seealso cref="BlendDescription"/>.</param>
        /// <returns>The blend-state object created (see BlendState Object).</returns>
        BlendState^ CreateBlendState(BlendDescription blendStateDescription);

        /// <summary>
        /// Create a counter object for measuring GPU performance.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateCounter)</para>
        /// </summary>
        /// <param name="description">A counter description (see <see cref="CounterDescription"/>)<seealso cref="CounterDescription"/>.</param>
        /// <returns>A counter (see D3DCounter).</returns>
        D3DCounter^ CreateCounter(CounterDescription description);

        /// <summary>
        /// Create a buffer (vertex buffer, index buffer, or shader-constant buffer).
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateBuffer)</para>
        /// </summary>
        /// <param name="description">A buffer description (see <see cref="BufferDescription"/>)<seealso cref="BufferDescription"/>.</param>
        /// <param name="initialData">Pointer to the initialization data (see <see cref="SubresourceData"/>)<seealso cref="SubresourceData"/>; use NULL to allocate space only.</param>
        /// <returns>The buffer created (see D3DBuffer Object).</returns>
        D3DBuffer^ CreateBuffer(BufferDescription description, ... array<SubresourceData>^ initialData );

        /// <summary>
        /// Create a buffer (vertex buffer, index buffer, or shader-constant buffer) with no initial data.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateBuffer)</para>
        /// </summary>
        /// <param name="description">A buffer description (see <see cref="BufferDescription"/>)<seealso cref="BufferDescription"/>.</param>
        /// <returns>The buffer created (see D3DBuffer Object).</returns>
        D3DBuffer^ CreateBuffer(BufferDescription description);

        /// <summary>
        /// Create a depth-stencil state object that encapsulates depth-stencil test information for the output-merger stage.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateDepthStencilState)</para>
        /// </summary>
        /// <param name="description">A depth-stencil state description (see <see cref="DepthStencilDescription"/>)<seealso cref="DepthStencilDescription"/>.</param>
        /// <returns>The depth-stencil state object created (see DepthStencilState Object).</returns>
        DepthStencilState^ CreateDepthStencilState(DepthStencilDescription description);

        /// <summary>
        /// Create a depth-stencil view for accessing resource data.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateDepthStencilView)</para>
        /// </summary>
        /// <param name="resource">The resource that will serve as the depth-stencil surface. This resource must have been created with the DepthStencil flag.</param>
        /// <param name="description">A depth-stencil-view description (see <see cref="DepthStencilViewDescription"/>)<seealso cref="DepthStencilViewDescription"/>. </param>
        /// <returns>A DepthStencilView.</returns>
        DepthStencilView^ CreateDepthStencilView(D3DResource^ resource, DepthStencilViewDescription description);

        /// <summary>
        /// Create a depth-stencil view for accessing resource data.
        /// This method creates a view that accesses mipmap level 0 of the entire resource (using the format the resource was created with).
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateDepthStencilView)</para>
        /// </summary>
        /// <param name="resource">The resource that will serve as the depth-stencil surface. This resource must have been created with the DepthStencil flag.</param>
        /// <returns>A DepthStencilView.</returns>
        DepthStencilView^ CreateDepthStencilView(D3DResource^ resource);
        /// <summary>
        /// Create a geometry shader.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateGeometryShader)</para>
        /// </summary>
        /// <param name="shaderBytecode">The compiled shader. To get this object see Getting a A Compiled Shader in DX SDK.</param>
        /// <param name="shaderBytecodeLength">Size of the compiled geometry shader.</param>
        /// <returns>A GeometryShader Object.  If this is NULL, all other parameters will be validated, and if all parameters pass validation this API will return S_FALSE instead of S_OK.</returns>
        GeometryShader^ CreateGeometryShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength);

        /// <summary>
        /// Create a geometry shader.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateGeometryShader)</para>
        /// </summary>
        /// <param name="shaderBytecodeStream">The stream to read the compiled shader shader data from. To get this object see Getting a A Compiled Shader in DX SDK.</param>
        /// <returns>A GeometryShader Object.  If this is NULL, all other parameters will be validated, and if all parameters pass validation this API will return S_FALSE instead of S_OK.</returns>
        GeometryShader^ CreateGeometryShader(Stream^ shaderBytecodeStream);

        /// <summary>
        /// Create a geometry shader that can write to streaming output buffers.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateGeometryShaderWithStreamOutput)</para>
        /// </summary>
        /// <param name="shaderBytecode">The compiled shader.</param>
        /// <param name="shaderBytecodeLength">The length in bytes of compiled shader.</param>
        /// <param name="streamOutputDeclarations">A StreamOutputDeclarationEntry collection. ( ranges from 0 to D3D11_SO_STREAM_COUNT * D3D11_SO_OUTPUT_COMPONENT_COUNT ).</param>
        /// <param name="outputStreamStride">The size of pStreamOutputDecl. This parameter is only used when the output slot is 0 for all entries in streamOutputDeclarations.</param>
        /// <returns>A GeometryShader interface, representing the geometry shader that was created. </returns>
        GeometryShader^ CreateGeometryShaderWithStreamOutput(IntPtr shaderBytecode, UInt32 shaderBytecodeLength, IEnumerable<StreamOutputDeclarationEntry>^ streamOutputDeclarations, UInt32 outputStreamStride);

        /// <summary>
        /// Create a geometry shader that can write to streaming output buffers.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateGeometryShaderWithStreamOutput)</para>
        /// </summary>
        /// <param name="shaderBytecodeStream">The stream to read the compiled shader shader data from. To get this object see Getting a A Compiled Shader in DX SDK.</param>
        /// <param name="streamOutputDeclarations">A StreamOutputDeclarationEntry collection. ( ranges from 0 to D3D11_SO_STREAM_COUNT * D3D11_SO_OUTPUT_COMPONENT_COUNT ).</param>
        /// <param name="outputStreamStride">The size of pStreamOutputDecl. This parameter is only used when the output slot is 0 for all entries in streamOutputDeclarations.</param>
        /// <returns>A GeometryShader interface, representing the geometry shader that was created. </returns>
        GeometryShader^ CreateGeometryShaderWithStreamOutput(Stream^ shaderBytecodeStream, IEnumerable<StreamOutputDeclarationEntry>^ streamOutputDeclarations, UInt32 outputStreamStride);

        /// <summary>
        /// Create an input-layout object to describe the input-buffer data for the input-assembler stage.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateInputLayout)</para>
        /// </summary>
        /// <param name="inputElementDescriptions">A collection of the input-assembler stage input data types; each type is described by an element description (see <see cref="InputElementDescription"/>)<seealso cref="InputElementDescription"/>.</param>
        /// <param name="shaderBytecodeWithInputSignature">The compiled shader.  The compiled shader code contains a input signature which is validated against the array of elements.</param>        
        /// <param name="shaderBytecodeWithInputSignatureSize">The length in bytes of compiled shader.</param>        
        /// <returns>The input-layout object created (see <see cref="InputLayout"/>)<seealso cref="InputLayout"/>. To validate the other input parameters.</returns>
        InputLayout^ CreateInputLayout(IEnumerable<InputElementDescription>^ inputElementDescriptions, IntPtr shaderBytecodeWithInputSignature, UInt32 shaderBytecodeWithInputSignatureSize);

        /// <summary>
        /// Create an input-layout object to describe the input-buffer data for the input-assembler stage.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateInputLayout)</para>
        /// </summary>
        /// <param name="inputElementDescriptions">A collection of the input-assembler stage input data types; each type is described by an element description (see <see cref="InputElementDescription"/>)<seealso cref="InputElementDescription"/>.</param>
        /// <param name="shaderBytecodeWithInputSignatureStream">The compiled shader stream.  The compiled shader code contains a input signature which is validated against the array of elements.</param>        
        /// <returns>The input-layout object created (see <see cref="InputLayout"/>)<seealso cref="InputLayout"/>. To validate the other input parameters.</returns>
        InputLayout^ CreateInputLayout(IEnumerable<InputElementDescription>^ inputElementDescriptions, Stream^ shaderBytecodeWithInputSignatureStream);

        /// <summary>
        /// Creates a predicate.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreatePredicate)</para>
        /// </summary>
        /// <param name="predicateDescription">A query description where the type of query must be a StreamOutputOverflowPredicate or OcclusionPredicate (see <see cref="QueryDescription"/>)<seealso cref="QueryDescription"/>.</param>
        /// <returns>A predicate (see D3DPredicate Object).</returns>
        D3DPredicate^ CreatePredicate(QueryDescription predicateDescription);

        /// <summary>
        /// This class encapsulates methods for querying information from the GPU.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateQuery)</para>
        /// </summary>
        /// <param name="description">A query description (see <see cref="QueryDescription"/>)<seealso cref="QueryDescription"/>.</param>
        /// <returns>The query object created (see D3DQuery Object).</returns>
        D3DQuery^ CreateQuery(QueryDescription description);

        /// <summary>
        /// Create a pixel shader.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreatePixelShader)</para>
        /// </summary>
        /// <param name="shaderBytecode">The compiled shader. To get this object see Getting a A Compiled Shader.</param>
        /// <param name="shaderBytecodeLength">Size of the compiled pixel shader.</param>
        /// <returns>A PixelShader Object.</returns>
        PixelShader^ CreatePixelShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength);

        /// <summary>
        /// Create a pixel shader.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreatePixelShader)</para>
        /// </summary>
        /// <param name="shaderBytecodeStream">The compiled shader stream. To get this object see Getting a A Compiled Shader.</param>
        /// <returns>A PixelShader Object.</returns>
        PixelShader^ CreatePixelShader(Stream^ shaderBytecodeStream);

        /// <summary>
        /// Create a rasterizer state object that tells the rasterizer stage how to behave.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateRasterizerState)</para>
        /// </summary>
        /// <param name="description">A rasterizer state description (see <see cref="RasterizerDescription"/>)<seealso cref="RasterizerDescription"/>.</param>
        /// <returns>The rasterizer state object created (see RasterizerState Object).</returns>
        RasterizerState^ CreateRasterizerState(RasterizerDescription description);

        /// <summary>
        /// Create a render-target view for accessing resource data.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateRenderTargetView)</para>
        /// </summary>
        /// <param name="resource">Pointer to the resource that will serve as the render target. This resource must have been created with the RenderTarget flag.</param>
        /// <param name="description">A render-target-view description (see <see cref="RenderTargetViewDescription"/>)<seealso cref="RenderTargetViewDescription"/>. Set this parameter to NULL to create a view that accesses mipmap level 0 of the entire resource (using the format the resource was created with).</param>
        /// <returns>A RenderTargetView.</returns>
        RenderTargetView^ CreateRenderTargetView(D3DResource^ resource, RenderTargetViewDescription description);

        /// <summary>
        /// Create a render-target view for accessing resource data in mipmap level 0 of the entire resource (using the format the resource was created with).
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateRenderTargetView)</para>
        /// </summary>
        /// <param name="resource">Pointer to the resource that will serve as the render target. This resource must have been created with the RenderTarget flag.</param>
        /// <returns>A RenderTargetView.</returns>
        RenderTargetView^ CreateRenderTargetView(D3DResource^ resource);

        /// <summary>
        /// Create a sampler-state object that encapsulates sampling information for a texture.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateSamplerState)</para>
        /// </summary>
        /// <param name="description">A sampler state description (see <see cref="SamplerDescription"/>)<seealso cref="SamplerDescription"/>.</param>
        /// <returns>The sampler state object created (see SamplerState Object).</returns>
        SamplerState^ CreateSamplerState(SamplerDescription description);

        /// <summary>
        /// Create a shader-resource view for accessing data in a resource.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateShaderResourceView)</para>
        /// </summary>
        /// <param name="resource">Pointer to the resource that will serve as input to a shader. This resource must have been created with the ShaderResource flag.</param>
        /// <param name="description">A shader-resource-view description (see <see cref="ShaderResourceViewDescription"/>)<seealso cref="ShaderResourceViewDescription"/>. Set this parameter to NULL to create a view that accesses the entire resource (using the format the resource was created with).</param>
        /// <returns>A ShaderResourceView.</returns>
        ShaderResourceView^ CreateShaderResourceView(D3DResource^ resource, ShaderResourceViewDescription description);

        /// <summary>
        /// Create a shader-resource view for accessing data in a resource. It accesses the entire resource (using the format the resource was created with).
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateShaderResourceView)</para>
        /// </summary>
        /// <param name="resource">Pointer to the resource that will serve as input to a shader. This resource must have been created with the ShaderResource flag.</param>
        /// <returns>A ShaderResourceView.</returns>
        ShaderResourceView^ CreateShaderResourceView(D3DResource^ resource);

        /// <summary>
        /// Create an array of 1D textures.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateTexture1D)</para>
        /// </summary>
        /// <param name="description">A 1D texture description (see <see cref="Texture1DDescription"/>)<seealso cref="Texture1DDescription"/>. To create a typeless resource that can be interpreted at runtime into different, compatible formats, specify a typeless format in the texture description. To generate mipmap levels automatically, set the number of mipmap levels to 0.</param>
        /// <param name="initialData">Subresource descriptions (see <see cref="SubresourceData"/>)<seealso cref="SubresourceData"/>; one for each subresource. Applications may not specify NULL for initialData when creating IMMUTABLE resources (see Usage). If the resource is multisampled, pInitialData must be NULL because multisampled resources cannot be initialized with data when they are created.</param>
        /// <returns>The created texture (see <see cref="Texture1D"/>)<seealso cref="Texture1D"/>. </returns>
        Texture1D^ CreateTexture1D(Texture1DDescription description, ... array<SubresourceData>^ initialData);

        /// <summary>
        /// Create an array of 2D textures.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateTexture2D)</para>
        /// </summary>
        /// <param name="description">A 2D texture description (see <see cref="Texture2DDescription"/>)<seealso cref="Texture2DDescription"/>. To create a typeless resource that can be interpreted at runtime into different, compatible formats, specify a typeless format in the texture description. To generate mipmap levels automatically, set the number of mipmap levels to 0.</param>
        /// <param name="initialData">Subresource descriptions (see <see cref="SubresourceData"/>)<seealso cref="SubresourceData"/>; one for each subresource. Applications may not specify NULL for pInitialData when creating IMMUTABLE resources (see Usage). If the resource is multisampled, pInitialData must be NULL because multisampled resources cannot be initialized with data when they are created.</param>
        /// <returns>The created texture (see <see cref="Texture2D"/>)<seealso cref="Texture2D"/>. </returns>
        Texture2D^ CreateTexture2D(Texture2DDescription description, ... array<SubresourceData>^ initialData);

        /// <summary>
        /// Create a single 3D texture.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateTexture3D)</para>
        /// </summary>
        /// <param name="description">A 3D texture description (see <see cref="Texture3DDescription"/>)<seealso cref="Texture3DDescription"/>. To create a typeless resource that can be interpreted at runtime into different, compatible formats, specify a typeless format in the texture description. To generate mipmap levels automatically, set the number of mipmap levels to 0.</param>
        /// <param name="initialData">Subresource descriptions (see <see cref="SubresourceData"/>)<seealso cref="SubresourceData"/>; one for each subresource. Applications may not specify NULL for pInitialData when creating IMMUTABLE resources (see Usage). If the resource is multisampled, pInitialData must be NULL because multisampled resources cannot be initialized with data when they are created.</param>
        /// <returns>The created texture (see <see cref="Texture3D"/>)<seealso cref="Texture3D"/>. </returns>
        Texture3D^ CreateTexture3D(Texture3DDescription description, ... array<SubresourceData>^ initialData);

        /// <summary>
        /// Create an array of 1D textures.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateTexture1D)</para>
        /// </summary>
        /// <param name="description">A 1D texture description (see <see cref="Texture1DDescription"/>)<seealso cref="Texture1DDescription"/>. To create a typeless resource that can be interpreted at runtime into different, compatible formats, specify a typeless format in the texture description. To generate mipmap levels automatically, set the number of mipmap levels to 0.</param>
        /// <returns>The created texture (see <see cref="Texture1D"/>)<seealso cref="Texture1D"/>. </returns>
        Texture1D^ CreateTexture1D(Texture1DDescription description);

        /// <summary>
        /// Create an array of 2D textures.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateTexture2D)</para>
        /// </summary>
        /// <param name="description">A 2D texture description (see <see cref="Texture2DDescription"/>)<seealso cref="Texture2DDescription"/>. To create a typeless resource that can be interpreted at runtime into different, compatible formats, specify a typeless format in the texture description. To generate mipmap levels automatically, set the number of mipmap levels to 0.</param>
        /// <returns>The created texture (see <see cref="Texture2D"/>)<seealso cref="Texture2D"/>. </returns>
        Texture2D^ CreateTexture2D(Texture2DDescription description);

        /// <summary>
        /// Create a single 3D texture.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateTexture3D)</para>
        /// </summary>
        /// <param name="description">A 3D texture description (see <see cref="Texture3DDescription"/>)<seealso cref="Texture3DDescription"/>. To create a typeless resource that can be interpreted at runtime into different, compatible formats, specify a typeless format in the texture description. To generate mipmap levels automatically, set the number of mipmap levels to 0.</param>
        /// <returns>The created texture (see <see cref="Texture3D"/>)<seealso cref="Texture3D"/>. </returns>
        Texture3D^ CreateTexture3D(Texture3DDescription description);

        /// <summary>
        /// Create a vertex-shader object from a compiled shader.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateVertexShader)</para>
        /// </summary>
        /// <param name="shaderBytecode">The compiled shader. To get this object see Getting a A Compiled Shader.</param>
        /// <param name="shaderBytecodeLength">Size of the compiled vertex shader.</param>
        /// <returns>A VertexShader Object.</returns>
        VertexShader^ CreateVertexShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength);

        /// <summary>
        /// Create a vertex-shader object from a compiled shader.
        /// <para>(Also see DirectX SDK: ID3D10Device::CreateVertexShader)</para>
        /// </summary>
        /// <param name="shaderBytecodeStream">The compiled shader stream. To get this object see Getting a A Compiled Shader.</param>
        /// <returns>A VertexShader Object.</returns>
        VertexShader^ CreateVertexShader(Stream^ shaderBytecodeStream);

        /// <summary>
        /// Draw non-indexed, non-instanced primitives.
        /// <para>(Also see DirectX SDK: ID3D10Device::Draw)</para>
        /// </summary>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex, which is usually an offset in a vertex buffer; it could also be used as the first vertex id generated for a shader parameter marked with the SV_TargetId system-value semantic.</param>
        void Draw(UInt32 vertexCount, UInt32 startVertexLocation);

        /// <summary>
        /// Draw geometry of an unknown size that was created by the geometry shader stage.
        /// <para>(Also see DirectX SDK: ID3D10Device::DrawAuto)</para>
        /// </summary>
        void DrawAuto();

        /// <summary>
        /// Draw indexed, non-instanced primitives.
        /// <para>(Also see DirectX SDK: ID3D10Device::DrawIndexed)</para>
        /// </summary>
        /// <param name="indexCount">Number of indices to draw.</param>
        /// <param name="startIndexLocation">Index of the first index to use when accesssing the vertex buffer; begin at startIndexLocation to index vertices from the vertex buffer.</param>
        /// <param name="baseVertexLocation">Offset from the start of the vertex buffer to the first vertex.</param>
        void DrawIndexed(UInt32 indexCount, UInt32 startIndexLocation, Int32 baseVertexLocation);

        /// <summary>
        /// Draw indexed, instanced primitives.
        /// <para>(Also see DirectX SDK: ID3D10Device::DrawIndexedInstanced)</para>
        /// </summary>
        /// <param name="indexCountPerInstance">Size of the index buffer used in each instance.</param>
        /// <param name="instanceCount">Number of instances to draw.</param>
        /// <param name="startIndexLocation">Index of the first index.</param>
        /// <param name="baseVertexLocation">Index of the first vertex. The index is signed, which allows a negative index. If the negative index plus the index value from the index buffer are less than 0, the result is undefined.</param>
        /// <param name="startInstanceLocation">Index of the first instance.</param>
        void DrawIndexedInstanced(UInt32 indexCountPerInstance, UInt32 instanceCount, UInt32 startIndexLocation, Int32 baseVertexLocation, UInt32 startInstanceLocation);

        /// <summary>
        /// Draw non-indexed, instanced primitives.
        /// <para>(Also see DirectX SDK: ID3D10Device::DrawInstanced)</para>
        /// </summary>
        /// <param name="vertexCountPerInstance">Number of vertices to draw.</param>
        /// <param name="instanceCount">Number of instances to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex.</param>
        /// <param name="startInstanceLocation">Index of the first instance.</param>
        void DrawInstanced(UInt32 vertexCountPerInstance, UInt32 instanceCount, UInt32 startVertexLocation, UInt32 startInstanceLocation);

        /// <summary>
        /// Send queued-up commands in the command buffer to the GPU.
        /// <para>(Also see DirectX SDK: ID3D10Device::Flush)</para>
        /// </summary>
        void Flush();

        /// <summary>
        /// Generate mipmaps for the given shader resource.
        /// <para>(Also see DirectX SDK: ID3D10Device::GenerateMips)</para>
        /// </summary>
        /// <param name="shaderResourceView">An ShaderResourceView Object. The mipmaps will be generated for this shader resource.</param>
        void GenerateMips(ShaderResourceView^ shaderResourceView);

        /// <summary>
        /// Get the flags used during the call to create the device.
        /// <para>(Also see DirectX SDK: ID3D10Device::GetCreationFlags)</para>
        /// </summary>
        property CreateDeviceOptions CreateDeviceOptions
        {
			Direct3D10::CreateDeviceOptions get();
        };

        /// <summary>
        /// Get the reason why the device was removed.
        /// <para>(Also see DirectX SDK: ID3D10Device::GetDeviceRemovedReason)</para>
        /// </summary>
        property ErrorCode DeviceRemovedReason
        {
            ErrorCode get();
        }

        /// <summary>
        /// Gets or sets the exception-mode flags.
        /// <para>(Also see DirectX SDK: ID3D10Device::GetExceptionMode, ID3D10Device::SetExceptionMode)</para>
        /// </summary>
        property ExceptionErrors ExceptionMode
        {
            ExceptionErrors get();
            void set(ExceptionErrors value);
        }

        /// <summary>
        /// Get the rendering predicate state.
        /// <para>(Also see DirectX SDK: ID3D10Device::GetPredication)</para>
        /// </summary>
        property D3DPredicate^ Predication
        {
            D3DPredicate^ get(void);
            void set(D3DPredicate^ predicate);
        }

        /// <summary>
        /// Give a device access to a shared resource created on a different device.
        /// <para>(Also see DirectX SDK: ID3D10Device::OpenSharedResource)</para>
        /// </summary>
        /// <param name="resource">The resource handle.</param>
        /// <typeparam name="T">The type of this shared resource. Must be <see cref="GraphicsObject"/></typeparam>
        /// <returns>The requested resource using the given type.</returns>
        generic <typename T> where T : DirectUnknown
        T OpenSharedResource(IntPtr resource);

        /// <summary>
        /// Copy a multisampled resource into a non-multisampled resource. This API is most useful when re-using the resulting rendertarget of one render pass as an input to a second render pass.
        /// <para>(Also see DirectX SDK: ID3D10Device::ResolveSubresource)</para>
        /// </summary>
        /// <param name="destinationResource">Destination resource. Must be a created with the Default flag and be single-sampled. See D3DResource.</param>
        /// <param name="destinationSubresource">A zero-based index, that identifies the destination subresource. See D3D10CalcSubresource for more details.</param>
        /// <param name="sourceResource">Source resource. Must be multisampled.</param>
        /// <param name="sourceSubresource">The source subresource of the source resource.</param>
        /// <param name="format">Format that indicates how the multisampled resource will be resolved to a single-sampled resource.</param>
        void ResolveSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, D3DResource^ sourceResource, UInt32 sourceSubresource, Format format);

        /// <summary>
        /// Set a rendering predicate.
        /// <para>(Also see DirectX SDK: ID3D10Device::SetPredication)</para>
        /// </summary>
        /// <param name="predicate">A predicate (see <see cref="D3DPredicate"/>)<seealso cref="D3DPredicate"/>. A NULL value indicates "no" predication; in this case, the value of PredicateValue is irrelevent but will be preserved for Device.GetPredication.</param>
        /// <param name="predicateValue">If TRUE, rendering will be affected by when the predicate's conditions are met. If FALSE, rendering will be affected when the conditions are not met.</param>
        void SetPredication(D3DPredicate^ predicate, Boolean predicateValue);

        /// <summary>
        /// The CPU copies data from memory to a subresource created in non-mappable memory.
        /// <para>(Also see DirectX SDK: ID3D10Device::UpdateSubresource)</para>
        /// </summary>
        /// <param name="destinationResource">The destination resource (see D3DResource Object).</param>
        /// <param name="destinationSubresource">A zero-based index, that identifies the destination subresource. See D3D10CalcSubresource for more details.</param>
        /// <param name="destinationBox">A box that defines the portion of the destination subresource to copy the resource data into. Coordinates are in bytes for buffers and in texels for textures. The dimensions of the source must fit the destination (see <see cref="Box"/>)<seealso cref="Box"/>.</param>
        /// <param name="sourceData">The source data in memory.</param>
        /// <param name="sourceRowPitch">The size of one row of the source data.</param>
        /// <param name="sourceDepthPitch">The size of one depth slice of source data.</param>
        void UpdateSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, IntPtr sourceData, UInt32 sourceRowPitch, UInt32 sourceDepthPitch, Box destinationBox);

        /// <summary>
        /// The CPU copies data from memory to a subresource created in non-mappable memory.
        /// Because no destination box is defined, the data is written to the destination subresource with no offset.
        /// <para>(Also see DirectX SDK: ID3D10Device::UpdateSubresource)</para>
        /// </summary>
        /// <param name="destinationResource">The destination resource (see D3DResource Object).</param>
        /// <param name="destinationSubresource">A zero-based index, that identifies the destination subresource. See D3D10CalcSubresource for more details.</param>
        /// <param name="sourceData">The source data in memory.</param>
        /// <param name="sourceRowPitch">The size of one row of the source data.</param>
        /// <param name="sourceDepthPitch">The size of one depth slice of source data.</param>
        void UpdateSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, IntPtr sourceData, UInt32 sourceRowPitch, UInt32 sourceDepthPitch);

        /// <summary>
        /// Loads a precompiled effect from a file.
        /// <para>(Also see DirectX SDK: D3D10CreateEffectFromMemory() function)</para>
        /// </summary>
        /// <para>(Also see DirectX SDK: D3D10CreateEffectFromMemory)</para>
        /// <param name="path">The path to the file that contains the compiled effect.</param>
        Effect^ CreateEffectFromCompiledBinary( String^ path );

        /// <summary>
        /// Loads a precompiled effect from a binary data stream.
        /// <para>(Also see DirectX SDK: D3D10CreateEffectFromMemory() function)</para>
        /// </summary>
        /// <para>(Also see DirectX SDK: D3D10CreateEffectFromMemory)</para>
        /// <param name="binaryEffect">The binary data stream.</param>
        Effect^ CreateEffectFromCompiledBinary( BinaryReader^ binaryEffect );

        /// <summary>
        /// Loads a precompiled effect from a binary data stream.
        /// <para>(Also see DirectX SDK: D3D10CreateEffectFromMemory() function)</para>
        /// </summary>
        /// <para>(Also see DirectX SDK: D3D10CreateEffectFromMemory)</para>
        /// <param name="binaryEffect">The binary data stream.</param>
        /// <param name="effectCompileOptions">Effect compile options</param>
        /// <param name="effectPool">A memory space for effect variables shared across effects.</param>
        virtual Effect^ CreateEffectFromCompiledBinary( BinaryReader^ binaryEffect, int effectCompileOptions, EffectPool^ effectPool );

        /// <summary>
        /// Loads a precompiled effect from a binary data stream.
        /// </summary>
        /// <para>(Also see DirectX SDK: D3D10CreateEffectFromMemory)</para>
        /// <param name="inputStream">The input data stream.</param>
        Effect^ CreateEffectFromCompiledBinary( Stream^ inputStream );

        /// <summary>
        /// Loads a precompiled effect from a binary data stream.
        /// </summary>
        /// <para>(Also see DirectX SDK: D3D10CreateEffectFromMemory)</para>
        /// <param name="inputStream">The input data stream.</param>
        /// <param name="effectCompileOptions">Effect compile options</param>
        /// <param name="effectPool">A memory space for effect variables shared across effects.</param>
        Effect^ CreateEffectFromCompiledBinary( Stream^ inputStream, int effectCompileOptions, EffectPool^ effectPool );

        /// <summary>
        /// Create a state block.
        /// </summary>
        /// <param name="stateBlockMask">Indicates which parts of the device state will be captured when calling <see cref="StateBlock" />.Capture and reapplied when calling <b>StateBlock</b>.Capture</param>
        /// <returns>The <see cref="StateBlock" /> object that was created</returns>
        /// <remarks>
        /// A state block is a collection of device state, and is used for saving and
        /// restoring device state. Use a state-block mask to enable subsets of state
        /// for saving and restoring.
        ///
        /// The <see cref="StateBlockMask" /> object can be filled by using
        /// any of its public members. A state block mask can also be obtained
        /// by calling <see cref="EffectTechnique" />.ComputeStateBlockMask or
        /// <see cref="EffectPass" />.ComputeStateBlockMask.
        /// </remarks>
        StateBlock^ CreateStateBlock(StateBlockMask^ stateBlockMask);

        /// <summary>
        /// Get the associated geometry shader pipeline stage object.
        /// </summary>
        property GeometryShaderPipelineStage^ GS
        { 
            GeometryShaderPipelineStage^ get();
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

        /// <summary>
        /// Gets an information queue object that can retrieve, store and filter debug messages.
        /// </summary>
        /// <returns>An InfoQueue (information queue) object.</returns>
        /// <remarks>
        /// Can only be obtained if the device was created using <see cref="CreateDeviceOptions"/>.Debug flag. Otherwise, throws an exception.
        /// </remarks>
        property InfoQueue^ InfoQueue
        {
            Direct3D10::InfoQueue^ get(void);
        }

        /// <summary>
        /// Gets a switch-to-reference object that enables an application to switch between a hardware and software device.
        /// </summary>
        /// <returns>A SwitchToRef (switch-to-reference) object.</returns>
        /// <remarks>
        /// Can only be obtained if the device was created using <see cref="CreateDeviceOptions"/>.SwitchToRef flag. Otherwise, throws an exception.
        /// </remarks>
        property SwitchToRef^ SwitchToRef
        {
            Direct3D10::SwitchToRef^ get(void);
        }

        /// <summary>
        /// Gets the Graphics Device object for this device
        /// </summary>
        property Device^ GraphicsDevice
        {
            Device^ get(void);
        }

		property Device1^ GraphicsDevice1
		{
			Device1^ get(void);
		}

		property Direct3D10::Multithread^ Multithread
		{
			Direct3D10::Multithread^ get(void);
		}

		property D3DDebug^ Debug
		{
			D3DDebug^ get(void);
		}

        /// <summary>
        /// Create a Direct3D 10.0 device and a swap chain using the default hardware adapter 
        /// and the most common settings.
        /// <para>(Also see DirectX SDK: D3D10CreateDeviceAndSwapChain() function)</para>
        /// </summary>
        /// <param name="windowHandle">The window handle to the output window.</param>
        /// <returns>The created Direct3D 10.0 Device</returns>
        /// <remarks>
        /// The swap chain created can be retrieved from the <see cref="SwapChain" /> property.
        /// If DirectX SDK environment variable is found, and the build is a Debug one,
        /// this method will attempt to use <see cref="CreateDeviceOptions"/>.Debug flag. This should allow 
        /// using the InfoQueue object.
        /// </remarks>
        static D3DDevice^ CreateDeviceAndSwapChain(IntPtr windowHandle);

        /// <summary>
        /// Create a Direct3D 10.0 device and a swap chain.
        /// <para>(Also see DirectX SDK: D3D10CreateDeviceAndSwapChain() function)</para>
        /// </summary>
        /// <param name="adapter">An Adapter object. Can be null.</param>
        /// <param name="driverType">The type of driver for the device.</param>
        /// <param name="softwareRasterizerLibrary">A path the DLL that implements a software rasterizer. Must be NULL if driverType is non-software.</param>
        /// <param name="options">Device creation flags that enable API layers. These flags can be bitwise OR'd together.</param>
        /// <param name="swapChainDescription">Description of the swap chain.</param>
        /// <returns>The created Direct3D 10.0 Device</returns>
        /// <remarks>
        /// The swap chain created can be retrieved from the <see cref="SwapChain" /> property.
        /// By default, all Direct3D 10 calls are handled in a thread-safe way. 
        /// By creating a single-threaded device (using <see cref="CreateDeviceOptions"/>.SingleThreaded, 
        /// you disable thread-safe calling.
        /// </remarks>
        static D3DDevice^ CreateDeviceAndSwapChain(
            Adapter^ adapter, 
            DriverType driverType,
            String^ softwareRasterizerLibrary,
            Direct3D10::CreateDeviceOptions options,
            SwapChainDescription swapChainDescription);

        /// <summary>
        /// Create a Direct3D 10.0 device using the default hardware adapter and the most common settings.
        /// <para>(Also see DirectX SDK: D3D10CreateDevice() function)</para>
        /// </summary>
        /// <returns>The created Direct3D 10.0 Device</returns>
        /// <remarks>
        /// If DirectX SDK environment variable is found, and the build is a Debug one,
        /// this method will attempt to use <see cref="CreateDeviceOptions"/>.Debug flag. This should allow 
        /// using the InfoQueue object.
        /// </remarks>
        static D3DDevice^ CreateDevice();

        /// <summary>
        /// Create a Direct3D 10.0 device. 
        /// <para>(Also see DirectX SDK: D3D10CreateDevice() function)</para>
        /// </summary>
        /// <param name="adapter">An Adapter object. Can be null.</param>
        /// <param name="driverType">The type of driver for the device.</param>
        /// <param name="softwareRasterizerLibrary">A path the DLL that implements a software rasterizer. Must be NULL if driverType is non-software.</param>
        /// <param name="options">Device creation flags that enable API layers. These flags can be bitwise OR'd together.</param>
        /// <returns>The created Direct3D 10.0 Device</returns>
        /// <remarks>
        /// By default, all Direct3D 10 calls are handled in a thread-safe way. 
        /// By creating a single-threaded device (using <see cref="CreateDeviceOptions"/>.SingleThreaded, 
        /// you disable thread-safe calling.
        /// </remarks>
        static D3DDevice^ CreateDevice(
            Adapter^ adapter, 
            DriverType driverType,
            String^ softwareRasterizerLibrary,
            Direct3D10::CreateDeviceOptions options);

        property SwapChain^ SwapChain
        {
            Graphics::SwapChain^ get(void) { return swapChain; }
        }

    internal:
        D3DDevice()
        { }

        D3DDevice(ID3D10Device* pNativeID3D10Device) : DirectUnknown(pNativeID3D10Device)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10Device)); }
        }

    protected:

        void SetSwapChain(Graphics::SwapChain^ newSwapChain) { swapChain = newSwapChain; }

    private:

        GeometryShaderPipelineStage^ m_GS;
        InputAssemblerPipelineStage^ m_IA;
        OutputMergerPipelineStage^ m_OM;
        PixelShaderPipelineStage^ m_PS;
        RasterizerPipelineStage^ m_RS;
        StreamOutputPipelineStage^ m_SO;
        VertexShaderPipelineStage^ m_VS;

        Graphics::SwapChain^ swapChain;
    };
} } } }
