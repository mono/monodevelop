//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

/// <summary>
/// Driver type options.
/// <para>(Also see DirectX SDK: D3D_DRIVER_TYPE)</para>
/// </summary>
public enum class DriverType : Int32
{
    /// <summary>
    /// The driver type is unknown.
    /// <para>(Also see DirectX SDK: D3D_DRIVER_TYPE_UNKNOWN)</para>
    /// </summary>
    Unknown = D3D_DRIVER_TYPE_UNKNOWN,
    /// <summary>
    /// A hardware driver, which implements Direct3D features in hardware. This is the primary driver that you should use in your Direct3D applications because it provides the best performance. A hardware driver uses hardware acceleration (on supported hardware) but can also use software for parts of the pipeline that are not supported in hardware. This driver type is often referred to as a hardware abstraction layer or HAL.
    /// <para>(Also see DirectX SDK: D3D_DRIVER_TYPE_HARDWARE)</para>
    /// </summary>
    Hardware = D3D_DRIVER_TYPE_HARDWARE,
    /// <summary>
    /// A reference driver, which is a software implementation that supports every Direct3D feature. A reference driver is designed for accuracy rather than speed and as a result is slow but accurate. The rasterizer portion of the driver does make use of special CPU instructions whenever it can, but it is not intended for retail applications; use it only for feature testing, demonstration of functionality, debugging, or verifying bugs in other drivers. This driver is installed by the DirectX SDK. This driver may be referred to as a REF driver, a reference driver or a reference rasterizer.
    /// <para>(Also see DirectX SDK: D3D_DRIVER_TYPE_REFERENCE)</para>
    /// </summary>
    Reference = D3D_DRIVER_TYPE_REFERENCE,
    /// <summary>
    /// A NULL driver, which is a reference driver without render capability. This driver is commonly used for debugging non-rendering API calls, it is not appropriate for retail applications. This driver is installed by the DirectX SDK.
    /// <para>(Also see DirectX SDK: D3D_DRIVER_TYPE_NULL)</para>
    /// </summary>
    Null = D3D_DRIVER_TYPE_NULL,
    /// <summary>
    /// A software driver, which is a driver implemented completely in software. The software implementation is not intended for a high-performance application due to its very slow performance.
    /// <para>(Also see DirectX SDK: D3D_DRIVER_TYPE_SOFTWARE)</para>
    /// </summary>
    Software = D3D_DRIVER_TYPE_SOFTWARE,
    /// <summary>
    /// A WARP driver, which is a high-performance software rasterizer. The rasterizer supports all current feature levels (level 9.1 through level 11.0) with a high performance software implementation when hardware is not available. For more information about using a WARP driver, see Windows Advanced Rasterization Platform (WARP) In-Depth Guide
    /// <para>(Also see DirectX SDK: D3D_DRIVER_TYPE_WARP)</para>
    /// </summary>
    Warp = D3D_DRIVER_TYPE_WARP,
};

/// <summary>
/// Setting a feature-mask flag will cause a rendering-operation method to do some extra task when called.
/// </summary>
[Flags]
public enum class DebugFeatures : Int32
{
    /// <summary>
    /// No features enabled
    /// </summary>
    None = 0,
    /// <summary>
    /// Application will wait for the GPU to finish processing the rendering operation before continuing.
    /// <para>(Also see DirectX SDK: D3D11_DEBUG_FEATURE_FINISH_PER_RENDER_OP)</para>
    /// </summary>
    FinishPerRenderOperation = D3D11_DEBUG_FEATURE_FINISH_PER_RENDER_OP,
    /// <summary>
    /// Runtime will additionally call DeviceContext.Flush.
    /// <para>(Also see DirectX SDK: D3D11_DEBUG_FEATURE_FLUSH_PER_RENDER_OP)</para>
    /// </summary>
    FlushPerRenderOperation = D3D11_DEBUG_FEATURE_FLUSH_PER_RENDER_OP,
    /// <summary>
    /// Runtime will call SwapChain.Present. Presentation of render buffers will occur according to the settings established by prior calls to Debug.SetSwapChain and Debug.SetPresentPerRenderOperationDelay.
    /// <para>(Also see DirectX SDK: D3D11_DEBUG_FEATURE_PRESENT_PER_RENDER_OP)</para>
    /// </summary>
    PresentPerRenderOperation = D3D11_DEBUG_FEATURE_PRESENT_PER_RENDER_OP,
};

/// <summary>
/// Primitive type, which determines how the data that makes up object geometry is organized.
/// <para>(Also see DirectX SDK: D3D11_PRIMITIVE)</para>
/// </summary>
CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1712:DoNotPrefixEnumValuesWithTypeName", Justification = "Type name is only coincidentally part of the descriptive value name")
public enum class Primitive : Int32
{
    /// <summary>
    /// The type is undefined.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_UNDEFINED)</para>
    /// </summary>
    Undefined = D3D11_PRIMITIVE_UNDEFINED,
    /// <summary>
    /// The data is organized in a point list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_POINT)</para>
    /// </summary>
    Point = D3D11_PRIMITIVE_POINT,
    /// <summary>
    /// The data is organized in a line list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_LINE)</para>
    /// </summary>
    Line = D3D11_PRIMITIVE_LINE,
    /// <summary>
    /// The data is organized in a triangle list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TRIANGLE)</para>
    /// </summary>
    Triangle = D3D11_PRIMITIVE_TRIANGLE,
    /// <summary>
    /// The data is organized in a line list with adjacency data.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_LINE_ADJ)</para>
    /// </summary>
    LineAdjacency = D3D11_PRIMITIVE_LINE_ADJ,
    /// <summary>
    /// The data is organized in a triangle list with adjacency data.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TRIANGLE_ADJ)</para>
    /// </summary>
    TriangleAdjacency = D3D11_PRIMITIVE_TRIANGLE_ADJ,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_1_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive1AsControlPointPatch = D3D11_PRIMITIVE_1_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_2_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive2AsControlPointPatch = D3D11_PRIMITIVE_2_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_3_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive3AsControlPointPatch = D3D11_PRIMITIVE_3_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_4_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive4AsControlPointPatch = D3D11_PRIMITIVE_4_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_5_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive5AsControlPointPatch = D3D11_PRIMITIVE_5_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_6_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive6AsControlPointPatch = D3D11_PRIMITIVE_6_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_7_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive7AsControlPointPatch = D3D11_PRIMITIVE_7_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_8_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive8AsControlPointPatch = D3D11_PRIMITIVE_8_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_9_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive9AsControlPointPatch = D3D11_PRIMITIVE_9_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_10_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive10AsControlPointPatch = D3D11_PRIMITIVE_10_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_11_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive11AsControlPointPatch = D3D11_PRIMITIVE_11_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_12_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive12AsControlPointPatch = D3D11_PRIMITIVE_12_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_13_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive13AsControlPointPatch = D3D11_PRIMITIVE_13_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_14_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive14AsControlPointPatch = D3D11_PRIMITIVE_14_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_15_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive15AsControlPointPatch = D3D11_PRIMITIVE_15_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_16_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive16AsControlPointPatch = D3D11_PRIMITIVE_16_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_17_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive17AsControlPointPatch = D3D11_PRIMITIVE_17_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_18_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive18AsControlPointPatch = D3D11_PRIMITIVE_18_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_19_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive19AsControlPointPatch = D3D11_PRIMITIVE_19_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_20_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive20AsControlPointPatch = D3D11_PRIMITIVE_20_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_21_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive21AsControlPointPatch = D3D11_PRIMITIVE_21_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_22_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive22AsControlPointPatch = D3D11_PRIMITIVE_22_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_23_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive23AsControlPointPatch = D3D11_PRIMITIVE_23_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_24_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive24AsControlPointPatch = D3D11_PRIMITIVE_24_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_25_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive25AsControlPointPatch = D3D11_PRIMITIVE_25_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_26_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive26AsControlPointPatch = D3D11_PRIMITIVE_26_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_27_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive27AsControlPointPatch = D3D11_PRIMITIVE_27_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_28_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive28AsControlPointPatch = D3D11_PRIMITIVE_28_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_29_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive29AsControlPointPatch = D3D11_PRIMITIVE_29_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_30_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive30AsControlPointPatch = D3D11_PRIMITIVE_30_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_31_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive31AsControlPointPatch = D3D11_PRIMITIVE_31_CONTROL_POINT_PATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_32_CONTROL_POINT_PATCH)</para>
    /// </summary>
    Primitive32AsControlPointPatch = D3D11_PRIMITIVE_32_CONTROL_POINT_PATCH,
};
/// <summary>
/// Optional flags that control the behavior of Asynchronous.GetData.
/// <para>(Also see DirectX SDK: D3D11_ASYNC_GETDATA_FLAG)</para>
/// </summary>
[Flags]
public enum class AsyncGetDataOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,
    /// <summary>
    /// Do not flush the command buffer. This can potentially cause an infinite loop if GetData is continually called until it returns S_OK as there may still be commands in the command buffer that need to be processed in order for GetData to return S_OK. Since the commands in the command buffer are not flushed they will not be processed and therefore GetData will never return S_OK.
    /// <para>(Also see DirectX SDK: D3D11_ASYNC_GETDATA_DONOTFLUSH)</para>
    /// </summary>
    DoNotFlush = D3D11_ASYNC_GETDATA_DONOTFLUSH,
};
/// <summary>
/// Identifies how to bind a resource to the pipeline.
/// <para>(Also see DirectX SDK: D3D11_BIND_FLAG)</para>
/// </summary>
[Flags]
public enum class BindingOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,
    /// <summary>
    /// Bind a buffer as a vertex buffer to the input-assembler stage.
    /// <para>(Also see DirectX SDK: D3D11_BIND_VERTEX_BUFFER)</para>
    /// </summary>
    VertexBuffer = D3D11_BIND_VERTEX_BUFFER,
    /// <summary>
    /// Bind a buffer as an index buffer to the input-assembler stage.
    /// <para>(Also see DirectX SDK: D3D11_BIND_INDEX_BUFFER)</para>
    /// </summary>
    IndexBuffer = D3D11_BIND_INDEX_BUFFER,
    /// <summary>
    /// Bind a buffer as a constant buffer to a shader stage; this flag may NOT be combined with any other bind flag.
    /// <para>(Also see DirectX SDK: D3D11_BIND_CONSTANT_BUFFER)</para>
    /// </summary>
    ConstantBuffer = D3D11_BIND_CONSTANT_BUFFER,
    /// <summary>
    /// Bind a buffer or texture to a shader stage; this flag cannot be used with the WriteNoOverwrite flag.
    /// <para>(Also see DirectX SDK: D3D11_BIND_SHADER_RESOURCE)</para>
    /// </summary>
    ShaderResource = D3D11_BIND_SHADER_RESOURCE,
    /// <summary>
    /// Bind an output buffer for the stream-output stage.
    /// <para>(Also see DirectX SDK: D3D11_BIND_STREAM_OUTPUT)</para>
    /// </summary>
    StreamOutput = D3D11_BIND_STREAM_OUTPUT,
    /// <summary>
    /// Bind a texture as a render target for the output-merger stage.
    /// <para>(Also see DirectX SDK: D3D11_BIND_RENDER_TARGET)</para>
    /// </summary>
    RenderTarget = D3D11_BIND_RENDER_TARGET,
    /// <summary>
    /// Bind a texture as a depth-stencil target for the output-merger stage.
    /// <para>(Also see DirectX SDK: D3D11_BIND_DEPTH_STENCIL)</para>
    /// </summary>
    DepthStencil = D3D11_BIND_DEPTH_STENCIL,
    /// <summary>
    /// Bind an unordered access resource.
    /// <para>(Also see DirectX SDK: D3D11_BIND_UNORDERED_ACCESS)</para>
    /// </summary>
    UnorderedAccess = D3D11_BIND_UNORDERED_ACCESS,
};
/// <summary>
/// Blend options. A blend option identifies the data source and an optional pre-blend operation.
/// <para>(Also see DirectX SDK: D3D11_BLEND)</para>
/// </summary>
public enum class Blend : Int32
{
    /// <summary>
    /// Undefined Blend
    /// </summary>
    Undefined = 0,
    /// <summary>
    /// The data source is the color black (0, 0, 0, 0). No pre-blend operation.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_ZERO)</para>
    /// </summary>
    Zero = D3D11_BLEND_ZERO,
    /// <summary>
    /// The data source is the color white (1, 1, 1, 1). No pre-blend operation.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_ONE)</para>
    /// </summary>
    One = D3D11_BLEND_ONE,
    /// <summary>
    /// The data source is color data (RGB) from a pixel shader. No pre-blend operation.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_SRC_COLOR)</para>
    /// </summary>
    SourceColor = D3D11_BLEND_SRC_COLOR,
    /// <summary>
    /// The data source is color data (RGB) from a pixel shader. The pre-blend operation inverts the data, generating 1 - RGB.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_INV_SRC_COLOR)</para>
    /// </summary>
    InverseSourceColor = D3D11_BLEND_INV_SRC_COLOR,
    /// <summary>
    /// The data source is alpha data (A) from a pixel shader. No pre-blend operation.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_SRC_ALPHA)</para>
    /// </summary>
    SourceAlpha = D3D11_BLEND_SRC_ALPHA,
    /// <summary>
    /// The data source is alpha data (A) from a pixel shader. The pre-blend operation inverts the data, generating 1 - A.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_INV_SRC_ALPHA)</para>
    /// </summary>
    InverseSourceAlpha = D3D11_BLEND_INV_SRC_ALPHA,
    /// <summary>
    /// The data source is alpha data from a rendertarget. No pre-blend operation.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_DEST_ALPHA)</para>
    /// </summary>
    DestinationAlpha = D3D11_BLEND_DEST_ALPHA,
    /// <summary>
    /// The data source is alpha data from a rendertarget. The pre-blend operation inverts the data, generating 1 - A.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_INV_DEST_ALPHA)</para>
    /// </summary>
    InverseDestinationAlpha = D3D11_BLEND_INV_DEST_ALPHA,
    /// <summary>
    /// The data source is color data from a rendertarget. No pre-blend operation.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_DEST_COLOR)</para>
    /// </summary>
    DestinationColor = D3D11_BLEND_DEST_COLOR,
    /// <summary>
    /// The data source is color data from a rendertarget. The pre-blend operation inverts the data, generating 1 - RGB.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_INV_DEST_COLOR)</para>
    /// </summary>
    InverseDestinationColor = D3D11_BLEND_INV_DEST_COLOR,
    /// <summary>
    /// The data source is alpha data from a pixel shader. The pre-blend operation clamps the data to 1 or less.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_SRC_ALPHA_SAT)</para>
    /// </summary>
    SourceAlphaSat = D3D11_BLEND_SRC_ALPHA_SAT,
    /// <summary>
    /// The data source is the blend factor set with DeviceContext.OMSetBlendState. No pre-blend operation.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_BLEND_FACTOR)</para>
    /// </summary>
    BlendFactor = D3D11_BLEND_BLEND_FACTOR,
    /// <summary>
    /// The data source is the blend factor set with DeviceContext.OMSetBlendState. The pre-blend operation inverts the blend factor, generating 1 - blend_factor.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_INV_BLEND_FACTOR)</para>
    /// </summary>
    InverseBlendFactor = D3D11_BLEND_INV_BLEND_FACTOR,
    /// <summary>
    /// The data sources are both color data output by a pixel shader. There is no pre-blend operation. This options supports dual-source color blending.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_SRC1_COLOR)</para>
    /// </summary>
    Source1Color = D3D11_BLEND_SRC1_COLOR,
    /// <summary>
    /// The data sources are both color data output by a pixel shader. The pre-blend operation inverts the data, generating 1 - RGB. This options supports dual-source color blending.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_INV_SRC1_COLOR)</para>
    /// </summary>
    InverseSource1Color = D3D11_BLEND_INV_SRC1_COLOR,
    /// <summary>
    /// The data sources are alpha data output by a pixel shader. There is no pre-blend operation. This options supports dual-source color blending.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_SRC1_ALPHA)</para>
    /// </summary>
    Source1Alpha = D3D11_BLEND_SRC1_ALPHA,
    /// <summary>
    /// The data sources are alpha data output by a pixel shader. The pre-blend operation inverts the data, generating 1 - A. This options supports dual-source color blending.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_INV_SRC1_ALPHA)</para>
    /// </summary>
    InverseSource1Alpha = D3D11_BLEND_INV_SRC1_ALPHA,
};
/// <summary>
/// RGB or alpha blending operation.
/// <para>(Also see DirectX SDK: D3D11_BLEND_OP)</para>
/// </summary>
public enum class BlendOperation : Int32
{
    /// <summary>
    /// Undefined Operation
    /// </summary>
    Undefined = 0,
    /// <summary>
    /// Add source 1 and source 2.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_OP_ADD)</para>
    /// </summary>
    Add = D3D11_BLEND_OP_ADD,
    /// <summary>
    /// Subtract source 1 from source 2.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_OP_SUBTRACT)</para>
    /// </summary>
    Subtract = D3D11_BLEND_OP_SUBTRACT,
    /// <summary>
    /// Subtract source 2 from source 1.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_OP_REV_SUBTRACT)</para>
    /// </summary>
    ReverseSubtract = D3D11_BLEND_OP_REV_SUBTRACT,
    /// <summary>
    /// Find the minimum of source 1 and source 2.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_OP_MIN)</para>
    /// </summary>
    Min = D3D11_BLEND_OP_MIN,
    /// <summary>
    /// Find the maximum of source 1 and source 2.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_OP_MAX)</para>
    /// </summary>
    Max = D3D11_BLEND_OP_MAX,
};
/// <summary>
/// Identifies how to bind a raw-buffer resource to the pipeline.
/// <para>(Also see DirectX SDK: D3D11_BUFFEREX_SRV_FLAG)</para>
/// </summary>
[Flags]
public enum class ExtendedBufferBindingOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,
    /// <summary>
    /// Bind a raw buffer to the input-assembler stage.
    /// <para>(Also see DirectX SDK: D3D11_BUFFEREX_SRV_FLAG_RAW)</para>
    /// </summary>
    Raw = D3D11_BUFFEREX_SRV_FLAG_RAW,
};
/// <summary>
/// Unordered-access-view buffer options.
/// <para>(Also see DirectX SDK: D3D11_BUFFER_UAV_FLAG)</para>
/// </summary>
[Flags]
public enum class UnorderedAccessViewBufferOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,
    /// <summary>
    /// Resource contains raw, unstructured data.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_UAV_FLAG_RAW)</para>
    /// </summary>
    Raw = D3D11_BUFFER_UAV_FLAG_RAW,
    /// <summary>
    /// Allow data to be appended to the end of the buffer.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_UAV_FLAG_APPEND)</para>
    /// </summary>
    Append = D3D11_BUFFER_UAV_FLAG_APPEND,
};
/// <summary>
/// Specifies the parts of the depth stencil to clear.
/// <para>(Also see DirectX SDK: D3D11_CLEAR_FLAG)</para>
/// </summary>
[Flags]
public enum class ClearOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,
    /// <summary>
    /// Clear the depth buffer.
    /// <para>(Also see DirectX SDK: D3D11_CLEAR_DEPTH)</para>
    /// </summary>
    Depth = D3D11_CLEAR_DEPTH,
    /// <summary>
    /// Clear the stencil buffer.
    /// <para>(Also see DirectX SDK: D3D11_CLEAR_STENCIL)</para>
    /// </summary>
    Stencil = D3D11_CLEAR_STENCIL,
};
/// <summary>
/// Identify which components of each pixel of a render target are writable during blending.
/// <para>(Also see DirectX SDK: D3D11_COLOR_WRITE_ENABLE)</para>
/// </summary>
[Flags]
public enum class ColorWriteEnableComponents : Int32
{
    /// <summary>
    /// No components enabled
    /// </summary>
    None = 0,
    /// <summary>
    /// Allow data to be stored in the red component.
    /// <para>(Also see DirectX SDK: D3D11_COLOR_WRITE_ENABLE_RED)</para>
    /// </summary>
    Red = D3D11_COLOR_WRITE_ENABLE_RED,
    /// <summary>
    /// Allow data to be stored in the green component.
    /// <para>(Also see DirectX SDK: D3D11_COLOR_WRITE_ENABLE_GREEN)</para>
    /// </summary>
    Green = D3D11_COLOR_WRITE_ENABLE_GREEN,
    /// <summary>
    /// Allow data to be stored in the blue component.
    /// <para>(Also see DirectX SDK: D3D11_COLOR_WRITE_ENABLE_BLUE)</para>
    /// </summary>
    Blue = D3D11_COLOR_WRITE_ENABLE_BLUE,
    /// <summary>
    /// Allow data to be stored in the alpha component.
    /// <para>(Also see DirectX SDK: D3D11_COLOR_WRITE_ENABLE_ALPHA)</para>
    /// </summary>
    Alpha = D3D11_COLOR_WRITE_ENABLE_ALPHA,
    /// <summary>
    /// Allow data to be stored in all components.
    /// <para>(Also see DirectX SDK: D3D11_COLOR_WRITE_ENABLE_ALL)</para>
    /// </summary>
    All = D3D11_COLOR_WRITE_ENABLE_ALL,
};
/// <summary>
/// Comparison options.
/// <para>(Also see DirectX SDK: D3D11_COMPARISON_FUNC)</para>
/// </summary>
public enum class ComparisonFunction : Int32
{
    /// <summary>
    /// Never pass the comparison.
    /// <para>(Also see DirectX SDK: D3D11_COMPARISON_NEVER)</para>
    /// </summary>
    Never = D3D11_COMPARISON_NEVER,
    /// <summary>
    /// If the source data is less than the destination data, the comparison passes.
    /// <para>(Also see DirectX SDK: D3D11_COMPARISON_LESS)</para>
    /// </summary>
    Less = D3D11_COMPARISON_LESS,
    /// <summary>
    /// If the source data is equal to the destination data, the comparison passes.
    /// <para>(Also see DirectX SDK: D3D11_COMPARISON_EQUAL)</para>
    /// </summary>
    Equal = D3D11_COMPARISON_EQUAL,
    /// <summary>
    /// If the source data is less than or equal to the destination data, the comparison passes.
    /// <para>(Also see DirectX SDK: D3D11_COMPARISON_LESS_EQUAL)</para>
    /// </summary>
    LessEqual = D3D11_COMPARISON_LESS_EQUAL,
    /// <summary>
    /// If the source data is greater than the destination data, the comparison passes.
    /// <para>(Also see DirectX SDK: D3D11_COMPARISON_GREATER)</para>
    /// </summary>
    Greater = D3D11_COMPARISON_GREATER,
    /// <summary>
    /// If the source data is not equal to the destination data, the comparison passes.
    /// <para>(Also see DirectX SDK: D3D11_COMPARISON_NOT_EQUAL)</para>
    /// </summary>
    NotEqual = D3D11_COMPARISON_NOT_EQUAL,
    /// <summary>
    /// If the source data is greater than or equal to the destination data, the comparison passes.
    /// <para>(Also see DirectX SDK: D3D11_COMPARISON_GREATER_EQUAL)</para>
    /// </summary>
    GreaterEqual = D3D11_COMPARISON_GREATER_EQUAL,
    /// <summary>
    /// Always pass the comparison.
    /// <para>(Also see DirectX SDK: D3D11_COMPARISON_ALWAYS)</para>
    /// </summary>
    Always = D3D11_COMPARISON_ALWAYS,
};

/// <summary>
/// Options for performance counters.
/// <para>(Also see DirectX SDK: D3D11_COUNTER)</para>
/// </summary>
public enum class Counter : Int32
{
    /// <summary>
    /// Define a performance counter that is dependent on the hardware device.
    /// <para>(Also see DirectX SDK: D3D11_COUNTER_DEVICE_DEPENDENT_0)</para>
    /// </summary>
    DeviceDependent0 = D3D11_COUNTER_DEVICE_DEPENDENT_0,
};
/// <summary>
/// Data type of a performance counter.
/// <para>(Also see DirectX SDK: D3D11_COUNTER_TYPE)</para>
/// </summary>
public enum class CounterType : Int32
{
    /// <summary>
    /// 32-bit floating point.
    /// <para>(Also see DirectX SDK: D3D11_COUNTER_TYPE_FLOAT32)</para>
    /// </summary>
    Float32 = D3D11_COUNTER_TYPE_FLOAT32,
    /// <summary>
    /// 16-bit unsigned integer.
    /// <para>(Also see DirectX SDK: D3D11_COUNTER_TYPE_UINT16)</para>
    /// </summary>
    UInt16 = D3D11_COUNTER_TYPE_UINT16,
    /// <summary>
    /// 32-bit unsigned integer.
    /// <para>(Also see DirectX SDK: D3D11_COUNTER_TYPE_UINT32)</para>
    /// </summary>
    UInt32 = D3D11_COUNTER_TYPE_UINT32,
    /// <summary>
    /// 64-bit unsigned integer.
    /// <para>(Also see DirectX SDK: D3D11_COUNTER_TYPE_UINT64)</para>
    /// </summary>
    UInt64 = D3D11_COUNTER_TYPE_UINT64,
};

/// <summary>
/// Specifies the types of CPU access allowed for a resource.
/// <para>(Also see DirectX SDK: D3D11_CPU_ACCESS_FLAG)</para>
/// </summary>
[Flags]
public enum class CpuAccessOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,
    /// <summary>
    /// The resource is to be mappable so that the CPU can change its contents. Resources created with this flag cannot be set as outputs of the pipeline and must be created with either dynamic or staging usage.
    /// <para>(Also see DirectX SDK: D3D11_CPU_ACCESS_WRITE)</para>
    /// </summary>
    Write = D3D11_CPU_ACCESS_WRITE,
    /// <summary>
    /// The resource is to be mappable so that the CPU can read its contents. Resources created with this flag cannot be set as either inputs or outputs to the pipeline and must be created with staging usage.
    /// <para>(Also see DirectX SDK: D3D11_CPU_ACCESS_READ)</para>
    /// </summary>
    Read = D3D11_CPU_ACCESS_READ,
};
/// <summary>
/// Describes parameters that are used to create a device.
/// <para>(Also see DirectX SDK: D3D11_CREATE_DEVICE_FLAG)</para>
/// </summary>
[Flags]
public enum class CreateDeviceOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,
    /// <summary>
    /// Use this flag if an application will only be calling D3D11 from a single thread. If this flag is not specified, the default behavior of D3D11 is to enter a lock during each API call to prevent multiple threads altering internal state. By using this flag no locks will be taken which can slightly increase performance, but could result in undefine behavior if D3D11 is called from multiple threads.
    /// <para>(Also see DirectX SDK: D3D11_CREATE_DEVICE_SINGLETHREADED)</para>
    /// </summary>
    SingleThreaded = D3D11_CREATE_DEVICE_SINGLETHREADED,
    /// <summary>
    /// Creates a device that supports the debug layer.
    /// <para>(Also see DirectX SDK: D3D11_CREATE_DEVICE_DEBUG)</para>
    /// </summary>
    Debug = D3D11_CREATE_DEVICE_DEBUG,
    /// <summary>
    /// Creates both a software (REF) and hardware (HAL) version of the device simultaneously, which allows an application to switch to a reference device to enable debugging.
    /// <para>(Also see DirectX SDK: D3D11_CREATE_DEVICE_SWITCH_TO_REF)</para>
    /// </summary>
    SwitchToRef = D3D11_CREATE_DEVICE_SWITCH_TO_REF,
    /// <summary>
    /// Prevents multiple threads from being created. This flag is not recommended for general use.
    /// <para>(Also see DirectX SDK: D3D11_CREATE_DEVICE_PREVENT_INTERNAL_THREADING_OPTIMIZATIONS)</para>
    /// </summary>
    PreventInternalThreadingOptimizations = D3D11_CREATE_DEVICE_PREVENT_INTERNAL_THREADING_OPTIMIZATIONS,
    /// <summary>
    /// Creates a device with BGRA color support.
    /// <para>(Also see DirectX SDK: D3D11_CREATE_DEVICE_BGRA_SUPPORT)</para>
    /// </summary>
    SupportBgra = D3D11_CREATE_DEVICE_BGRA_SUPPORT,
};
/// <summary>
/// Indicates triangles facing a particular direction are not drawn.
/// <para>(Also see DirectX SDK: D3D11_CULL_MODE)</para>
/// </summary>
public enum class CullMode : Int32
{
    /// <summary>
    /// Always draw all triangles.
    /// <para>(Also see DirectX SDK: D3D11_CULL_NONE)</para>
    /// </summary>
    None = D3D11_CULL_NONE,
    /// <summary>
    /// Do not draw triangles that are front-facing.
    /// <para>(Also see DirectX SDK: D3D11_CULL_FRONT)</para>
    /// </summary>
    Front = D3D11_CULL_FRONT,
    /// <summary>
    /// Do not draw triangles that are back-facing.
    /// <para>(Also see DirectX SDK: D3D11_CULL_BACK)</para>
    /// </summary>
    Back = D3D11_CULL_BACK,
};
/// <summary>
/// Identify the portion of a depth-stencil buffer for writing depth data.
/// <para>(Also see DirectX SDK: D3D11_DEPTH_WRITE_MASK)</para>
/// </summary>
public enum class DepthWriteMask : Int32
{
    /// <summary>
    /// Turn off writes to the depth-stencil buffer.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_WRITE_MASK_ZERO)</para>
    /// </summary>
    None = D3D11_DEPTH_WRITE_MASK_ZERO,
    /// <summary>
    /// Turn on writes to the depth-stencil buffer.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_WRITE_MASK_ALL)</para>
    /// </summary>
    Enabled = D3D11_DEPTH_WRITE_MASK_ALL,
};

/// <summary>
/// A value that identifies a portion of the depth-stencil buffer for reading stencil data.
/// </summary>
public enum class StencilReadMask : Int32
{
    /// <summary>
    /// Empty mask
    /// </summary>
    None = 0,
    /// <summary>
    /// Default Read Mask.
    /// <para>(Also see DirectX SDK: D3D10_DEFAULT_STENCIL_READ_MASK)</para>
    /// </summary>
    All = D3D11_DEFAULT_STENCIL_READ_MASK,
};

/// <summary>
/// A value that identifies a portion of the depth-stencil buffer for writing stencil data.
/// </summary>
public enum class StencilWriteMask : Int32
{
    /// <summary>
    /// Empty mask
    /// </summary>
    None = 0,
    /// <summary>
    /// Default write Mask.
    /// <para>(Also see DirectX SDK: D3D10_DEFAULT_STENCIL_READ_MASK)</para>
    /// </summary>
    All = D3D11_DEFAULT_STENCIL_WRITE_MASK,
};

/// <summary>
/// Device context options.
/// <para>(Also see DirectX SDK: D3D11_DEVICE_CONTEXT_TYPE)</para>
/// </summary>
public enum class DeviceContextType : Int32
{
    /// <summary>
    /// The device context is an immediate context.
    /// <para>(Also see DirectX SDK: D3D11_DEVICE_CONTEXT_IMMEDIATE)</para>
    /// </summary>
    Immediate = D3D11_DEVICE_CONTEXT_IMMEDIATE,
    /// <summary>
    /// The device context is a deferred context.
    /// <para>(Also see DirectX SDK: D3D11_DEVICE_CONTEXT_DEFERRED)</para>
    /// </summary>
    Deferred = D3D11_DEVICE_CONTEXT_DEFERRED,
};
/// <summary>
/// Specifies how to access a resource used in a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D11_DSV_DIMENSION)</para>
/// </summary>
public enum class DepthStencilViewDimension : Int32
{
    /// <summary>
    /// The resource will be accessed according to its type as determined from the actual instance this enumeration is paired with when the depth-stencil view is created.
    /// <para>(Also see DirectX SDK: D3D11_DSV_DIMENSION_UNKNOWN)</para>
    /// </summary>
    Unknown = D3D11_DSV_DIMENSION_UNKNOWN,
    /// <summary>
    /// The resource will be accessed as a 1D texture.
    /// <para>(Also see DirectX SDK: D3D11_DSV_DIMENSION_TEXTURE1D)</para>
    /// </summary>
    Texture1D = D3D11_DSV_DIMENSION_TEXTURE1D,
    /// <summary>
    /// The resource will be accessed as an array of 1D textures.
    /// <para>(Also see DirectX SDK: D3D11_DSV_DIMENSION_TEXTURE1DARRAY)</para>
    /// </summary>
    Texture1DArray = D3D11_DSV_DIMENSION_TEXTURE1DARRAY,
    /// <summary>
    /// The resource will be accessed as a 2D texture.
    /// <para>(Also see DirectX SDK: D3D11_DSV_DIMENSION_TEXTURE2D)</para>
    /// </summary>
    Texture2D = D3D11_DSV_DIMENSION_TEXTURE2D,
    /// <summary>
    /// The resource will be accessed as an array of 2D textures.
    /// <para>(Also see DirectX SDK: D3D11_DSV_DIMENSION_TEXTURE2DARRAY)</para>
    /// </summary>
    Texture2DArray = D3D11_DSV_DIMENSION_TEXTURE2DARRAY,
    /// <summary>
    /// The resource will be accessed as a 2D texture with multisampling.
    /// <para>(Also see DirectX SDK: D3D11_DSV_DIMENSION_TEXTURE2DMS)</para>
    /// </summary>
    Texture2DMultisample = D3D11_DSV_DIMENSION_TEXTURE2DMS,
    /// <summary>
    /// The resource will be accessed as an array of 2D textures with multisampling.
    /// <para>(Also see DirectX SDK: D3D11_DSV_DIMENSION_TEXTURE2DMSARRAY)</para>
    /// </summary>
    Texture2DMultisampleArray = D3D11_DSV_DIMENSION_TEXTURE2DMSARRAY,
};
/// <summary>
/// Depth-stencil view options.
/// <para>(Also see DirectX SDK: D3D11_DSV_FLAG)</para>
/// </summary>
[Flags]
public enum class DepthStencilViewOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,
    /// <summary>
    /// Indicates that depth values are read only.
    /// <para>(Also see DirectX SDK: D3D11_DSV_READ_ONLY_DEPTH)</para>
    /// </summary>
    ReadOnlyDepth = D3D11_DSV_READ_ONLY_DEPTH,
    /// <summary>
    /// Indicates that stencil values are read only.
    /// <para>(Also see DirectX SDK: D3D11_DSV_READ_ONLY_STENCIL)</para>
    /// </summary>
    ReadOnlyStencil = D3D11_DSV_READ_ONLY_STENCIL,
};
/// <summary>
/// Direct3D 11 feature options.
/// <para>(Also see DirectX SDK: D3D11_FEATURE)</para>
/// </summary>
public enum class Feature : Int32
{
    /// <summary>
    /// The driver supports multithreading. To see an example of testing a driver for multithread support, see How To: Check for Driver Support.
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_THREADING)</para>
    /// </summary>
    Threading = D3D11_FEATURE_THREADING,
    /// <summary>
    /// Unused. Supports the use of the doubles type in HLSL.
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_DOUBLES)</para>
    /// </summary>
    Doubles = D3D11_FEATURE_DOUBLES,
    /// <summary>
    /// Supports the formats in FormatSupportOptions.
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_FORMAT_SUPPORT)</para>
    /// </summary>
    FormatSupport = D3D11_FEATURE_FORMAT_SUPPORT,
    /// <summary>
    /// Supports the formats in ExtendedFormatSupportOptions.
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_FORMAT_SUPPORT2)</para>
    /// </summary>
    ExtendedFormatSupport = D3D11_FEATURE_FORMAT_SUPPORT2,
    /// <summary>
    /// Supports D3D10 X hardware options.
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_D3D10_X_HARDWARE_OPTIONS)</para>
    /// </summary>
    D3D10XHardwareOptions    = D3D11_FEATURE_D3D10_X_HARDWARE_OPTIONS,
};
/// <summary>
/// Determines the fill mode to use when rendering triangles.
/// <para>(Also see DirectX SDK: D3D11_FILL_MODE)</para>
/// </summary>
public enum class FillMode : Int32
{
    /// <summary>
    /// Draw lines connecting the vertices. Adjacent vertices are not drawn.
    /// <para>(Also see DirectX SDK: D3D11_FILL_WIREFRAME)</para>
    /// </summary>
    Wireframe = D3D11_FILL_WIREFRAME,
    /// <summary>
    /// Fill the triangles formed by the vertices. Adjacent vertices are not drawn.
    /// <para>(Also see DirectX SDK: D3D11_FILL_SOLID)</para>
    /// </summary>
    Solid = D3D11_FILL_SOLID,
};
/// <summary>
/// Filtering options during texture sampling.
/// <para>(Also see DirectX SDK: D3D11_FILTER)</para>
/// </summary>
public enum class Filter : Int32
{
    /// <summary>
    /// Use point sampling for minification, magnification, and mip-level sampling.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_MIN_MAG_MIP_POINT)</para>
    /// </summary>
    MinMagMipPoint = D3D11_FILTER_MIN_MAG_MIP_POINT,
    /// <summary>
    /// Use point sampling for minification and magnification; use linear interpolation for mip-level sampling.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_MIN_MAG_POINT_MIP_LINEAR)</para>
    /// </summary>
    MinMagPointMipLinear = D3D11_FILTER_MIN_MAG_POINT_MIP_LINEAR,
    /// <summary>
    /// Use point sampling for minification; use linear interpolation for magnification; use point sampling for mip-level sampling.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_MIN_POINT_MAG_LINEAR_MIP_POINT)</para>
    /// </summary>
    MinPointMagLinearMipPoint = D3D11_FILTER_MIN_POINT_MAG_LINEAR_MIP_POINT,
    /// <summary>
    /// Use point sampling for minification; use linear interpolation for magnification and mip-level sampling.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_MIN_POINT_MAG_MIP_LINEAR)</para>
    /// </summary>
    MinPointMagMipLinear = D3D11_FILTER_MIN_POINT_MAG_MIP_LINEAR,
    /// <summary>
    /// Use linear interpolation for minification; use point sampling for magnification and mip-level sampling.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_MIN_LINEAR_MAG_MIP_POINT)</para>
    /// </summary>
    MinLinearMagMipPoint = D3D11_FILTER_MIN_LINEAR_MAG_MIP_POINT,
    /// <summary>
    /// Use linear interpolation for minification; use point sampling for magnification; use linear interpolation for mip-level sampling.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR)</para>
    /// </summary>
    MinLinearMagPointMipLinear = D3D11_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR,
    /// <summary>
    /// Use linear interpolation for minification and magnification; use point sampling for mip-level sampling.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT)</para>
    /// </summary>
    MinMagLinearMipPoint = D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT,
    /// <summary>
    /// Use linear interpolation for minification, magnification, and mip-level sampling.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_MIN_MAG_MIP_LINEAR)</para>
    /// </summary>
    MinMagMipLinear = D3D11_FILTER_MIN_MAG_MIP_LINEAR,
    /// <summary>
    /// Use anisotropic interpolation for minification, magnification, and mip-level sampling.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_ANISOTROPIC)</para>
    /// </summary>
    Anisotropic = D3D11_FILTER_ANISOTROPIC,
    /// <summary>
    /// Use point sampling for minification, magnification, and mip-level sampling. Compare the result to the comparison value.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_COMPARISON_MIN_MAG_MIP_POINT)</para>
    /// </summary>
    ComparisonMinMagMipPoint = D3D11_FILTER_COMPARISON_MIN_MAG_MIP_POINT,
    /// <summary>
    /// Use point sampling for minification and magnification; use linear interpolation for mip-level sampling. Compare the result to the comparison value.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_COMPARISON_MIN_MAG_POINT_MIP_LINEAR)</para>
    /// </summary>
    ComparisonMinMagPointMipLinear = D3D11_FILTER_COMPARISON_MIN_MAG_POINT_MIP_LINEAR,
    /// <summary>
    /// Use point sampling for minification; use linear interpolation for magnification; use point sampling for mip-level sampling. Compare the result to the comparison value.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_COMPARISON_MIN_POINT_MAG_LINEAR_MIP_POINT)</para>
    /// </summary>
    ComparisonMinPointMagLinearMipPoint = D3D11_FILTER_COMPARISON_MIN_POINT_MAG_LINEAR_MIP_POINT,
    /// <summary>
    /// Use point sampling for minification; use linear interpolation for magnification and mip-level sampling. Compare the result to the comparison value.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_COMPARISON_MIN_POINT_MAG_MIP_LINEAR)</para>
    /// </summary>
    ComparisonMinPointMagMipLinear = D3D11_FILTER_COMPARISON_MIN_POINT_MAG_MIP_LINEAR,
    /// <summary>
    /// Use linear interpolation for minification; use point sampling for magnification and mip-level sampling. Compare the result to the comparison value.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_COMPARISON_MIN_LINEAR_MAG_MIP_POINT)</para>
    /// </summary>
    ComparisonMinLinearMagMipPoint = D3D11_FILTER_COMPARISON_MIN_LINEAR_MAG_MIP_POINT,
    /// <summary>
    /// Use linear interpolation for minification; use point sampling for magnification; use linear interpolation for mip-level sampling. Compare the result to the comparison value.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_COMPARISON_MIN_LINEAR_MAG_POINT_MIP_LINEAR)</para>
    /// </summary>
    ComparisonMinLinearMagPointMipLinear = D3D11_FILTER_COMPARISON_MIN_LINEAR_MAG_POINT_MIP_LINEAR,
    /// <summary>
    /// Use linear interpolation for minification and magnification; use point sampling for mip-level sampling. Compare the result to the comparison value.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_COMPARISON_MIN_MAG_LINEAR_MIP_POINT)</para>
    /// </summary>
    ComparisonMinMagLinearMipPoint = D3D11_FILTER_COMPARISON_MIN_MAG_LINEAR_MIP_POINT,
    /// <summary>
    /// Use linear interpolation for minification, magnification, and mip-level sampling. Compare the result to the comparison value.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_COMPARISON_MIN_MAG_MIP_LINEAR)</para>
    /// </summary>
    ComparisonMinMagMipLinear = D3D11_FILTER_COMPARISON_MIN_MAG_MIP_LINEAR,
    /// <summary>
    /// Use anisotropic interpolation for minification, magnification, and mip-level sampling. Compare the result to the comparison value.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_COMPARISON_ANISOTROPIC)</para>
    /// </summary>
    ComparisonAnisotropic = D3D11_FILTER_COMPARISON_ANISOTROPIC,
    // REVIEW: D3D11_FILTER_TEXT_1BIT is documented, but does not appear to be
    // in the D3D11 header files.
    ///// <summary>
    ///// For use in pixel shaders with textures that have the R1_UNORM format.
    ///// <para>(Also see DirectX SDK: D3D11_FILTER_TEXT_1BIT)</para>
    ///// </summary>
    //OneBitTexture = D3D11_FILTER_TEXT_1BIT,
};
/// <summary>
/// Types of magnification or minification sampler filters.
/// <para>(Also see DirectX SDK: D3D11_FILTER_TYPE)</para>
/// </summary>
public enum class FilterType : Int32
{
    /// <summary>
    /// Point filtering used as a texture magnification or minification filter. The texel with coordinates nearest to the desired pixel value is used. The texture filter to be used between mipmap levels is nearest-point mipmap filtering. The rasterizer uses the color from the texel of the nearest mipmap texture.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_TYPE_POINT)</para>
    /// </summary>
    Point = D3D11_FILTER_TYPE_POINT,
    /// <summary>
    /// Bilinear interpolation filtering used as a texture magnification or minification filter. A weighted average of a 2 x 2 area of texels surrounding the desired pixel is used. The texture filter to use between mipmap levels is trilinear mipmap interpolation. The rasterizer linearly interpolates pixel color, using the texels of the two nearest mipmap textures.
    /// <para>(Also see DirectX SDK: D3D11_FILTER_TYPE_LINEAR)</para>
    /// </summary>
    Linear = D3D11_FILTER_TYPE_LINEAR,
};
/// <summary>
/// Which resources are supported for a given format and given device.
/// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT)</para>
/// </summary>
[Flags]
public enum class FormatSupportOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,
    /// <summary>
    /// Buffer resources supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_BUFFER)</para>
    /// </summary>
    Buffer = D3D11_FORMAT_SUPPORT_BUFFER,
    /// <summary>
    /// Vertex buffers supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_IA_VERTEX_BUFFER)</para>
    /// </summary>
    InputAssemblerVertexBuffer = D3D11_FORMAT_SUPPORT_IA_VERTEX_BUFFER,
    /// <summary>
    /// Index buffers supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_IA_INDEX_BUFFER)</para>
    /// </summary>
    InputAssemblerIndexBuffer = D3D11_FORMAT_SUPPORT_IA_INDEX_BUFFER,
    /// <summary>
    /// Streaming output buffers supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_SO_BUFFER)</para>
    /// </summary>
    StreamOutputBuffer = D3D11_FORMAT_SUPPORT_SO_BUFFER,
    /// <summary>
    /// 1D texture resources supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_TEXTURE1D)</para>
    /// </summary>
    Texture1D = D3D11_FORMAT_SUPPORT_TEXTURE1D,
    /// <summary>
    /// 2D texture resources supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_TEXTURE2D)</para>
    /// </summary>
    Texture2D = D3D11_FORMAT_SUPPORT_TEXTURE2D,
    /// <summary>
    /// 3D texture resources supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_TEXTURE3D)</para>
    /// </summary>
    Texture3D = D3D11_FORMAT_SUPPORT_TEXTURE3D,
    /// <summary>
    /// Cube texture resources supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_TEXTURECUBE)</para>
    /// </summary>
    TextureCube = D3D11_FORMAT_SUPPORT_TEXTURECUBE,
    /// <summary>
    /// The intrinsic HLSL function load is supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_SHADER_LOAD)</para>
    /// </summary>
    ShaderLoad = D3D11_FORMAT_SUPPORT_SHADER_LOAD,
    /// <summary>
    /// The intrinsic HLSL functions sample supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_SHADER_SAMPLE)</para>
    /// </summary>
    ShaderSample = D3D11_FORMAT_SUPPORT_SHADER_SAMPLE,
    /// <summary>
    /// The intrinsic HLSL functions samplecmp and samplecmplevelzero are supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_SHADER_SAMPLE_COMPARISON)</para>
    /// </summary>
    ShaderSampleComparison = D3D11_FORMAT_SUPPORT_SHADER_SAMPLE_COMPARISON,
    /// <summary>
    /// Reserved.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_SHADER_SAMPLE_MONO_TEXT)</para>
    /// </summary>
    ShaderSampleMonoText = D3D11_FORMAT_SUPPORT_SHADER_SAMPLE_MONO_TEXT,
    /// <summary>
    /// Mipmaps are supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_MIP)</para>
    /// </summary>
    MipMap = D3D11_FORMAT_SUPPORT_MIP,
    /// <summary>
    /// Automatic generation of mipmaps is supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_MIP_AUTOGEN)</para>
    /// </summary>
    MipMapAutoGeneration = D3D11_FORMAT_SUPPORT_MIP_AUTOGEN,
    /// <summary>
    /// Rendertargets are supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_RENDER_TARGET)</para>
    /// </summary>
    RenderTarget = D3D11_FORMAT_SUPPORT_RENDER_TARGET,
    /// <summary>
    /// Blend operations supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_BLENDABLE)</para>
    /// </summary>
    Blendable = D3D11_FORMAT_SUPPORT_BLENDABLE,
    /// <summary>
    /// Depth stencils supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_DEPTH_STENCIL)</para>
    /// </summary>
    DepthStencil = D3D11_FORMAT_SUPPORT_DEPTH_STENCIL,
    /// <summary>
    /// CPU locking supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_CPU_LOCKABLE)</para>
    /// </summary>
    CpuLockable = D3D11_FORMAT_SUPPORT_CPU_LOCKABLE,
    /// <summary>
    /// Multisampling resolution supported.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_MULTISAMPLE_RESOLVE)</para>
    /// </summary>
    MultisampleResolve = D3D11_FORMAT_SUPPORT_MULTISAMPLE_RESOLVE,
    /// <summary>
    /// Format can be displayed on screen.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_DISPLAY)</para>
    /// </summary>
    Display = D3D11_FORMAT_SUPPORT_DISPLAY,
    /// <summary>
    /// Format cannot be cast to another format.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_CAST_WITHIN_BIT_LAYOUT)</para>
    /// </summary>
    CastWithinBitLayout = D3D11_FORMAT_SUPPORT_CAST_WITHIN_BIT_LAYOUT,
    /// <summary>
    /// Format can be used as a multisampled rendertarget.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_MULTISAMPLE_RENDERTARGET)</para>
    /// </summary>
    MultisampleRenderTarget = D3D11_FORMAT_SUPPORT_MULTISAMPLE_RENDERTARGET,
    /// <summary>
    /// Format can be used as a multisampled texture and read into a shader with the HLSL load function.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_MULTISAMPLE_LOAD)</para>
    /// </summary>
    MultisampleLoad = D3D11_FORMAT_SUPPORT_MULTISAMPLE_LOAD,
    /// <summary>
    /// Format can be used with the HLSL gather function. This value is available in DirectX 10.1 or higher.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_SHADER_GATHER)</para>
    /// </summary>
    ShaderGather = D3D11_FORMAT_SUPPORT_SHADER_GATHER,
    /// <summary>
    /// Format supports casting when used the resource is a back buffer.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_BACK_BUFFER_CAST)</para>
    /// </summary>
    BackBufferCast = D3D11_FORMAT_SUPPORT_BACK_BUFFER_CAST,
    /// <summary>
    /// Format can be used for an unordered access view.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_TYPED_UNORDERED_ACCESS_VIEW)</para>
    /// </summary>
    TypedUnorderedAccessView = D3D11_FORMAT_SUPPORT_TYPED_UNORDERED_ACCESS_VIEW,
    /// <summary>
    /// Format can be used with the HLSL gather with comparison function.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT_SHADER_GATHER_COMPARISON)</para>
    /// </summary>
    ShaderGatherComparison = D3D11_FORMAT_SUPPORT_SHADER_GATHER_COMPARISON,
};
/// <summary>
/// Unordered resource support options for a compute shader resource.
/// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT2)</para>
/// </summary>
[Flags]
public enum class ExtendedFormatSupportOptions : Int32
{
    /// <summary>
    /// Format supports atomic add.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_ADD)</para>
    /// </summary>
    UnorderedAccessViewAtomicAdd = D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_ADD,
    /// <summary>
    /// Format supports atomic bitwise operations.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_BITWISE_OPS)</para>
    /// </summary>
    UnorderedAccessViewAtomicBitwiseOperations = D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_BITWISE_OPS,
    /// <summary>
    /// Format supports atomic compare with store or exchange.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_COMPARE_STORE_OR_COMPARE_EXCHANGE)</para>
    /// </summary>
    UnorderedAccessViewAtomicCompareStoreOrCompareExchange = D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_COMPARE_STORE_OR_COMPARE_EXCHANGE,
    /// <summary>
    /// Format supports atomic exchange.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_EXCHANGE)</para>
    /// </summary>
    UnorderedAccessViewAtomicExchange = D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_EXCHANGE,
    /// <summary>
    /// Format supports atomic min and max.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_SIGNED_MIN_OR_MAX)</para>
    /// </summary>
    UnorderedAccessViewAtomicSignedMinOrMax = D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_SIGNED_MIN_OR_MAX,
    /// <summary>
    /// Format supports atomic unsigned min and max.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_UNSIGNED_MIN_OR_MAX)</para>
    /// </summary>
    UnorderedAccessViewAtomicUnsignedMinOrMax = D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_UNSIGNED_MIN_OR_MAX,
    /// <summary>
    /// Format supports a typed load.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT2_UAV_TYPED_LOAD)</para>
    /// </summary>
    UnorderedAccessViewTypedLoad = D3D11_FORMAT_SUPPORT2_UAV_TYPED_LOAD,
    /// <summary>
    /// Format supports a typed store.
    /// <para>(Also see DirectX SDK: D3D11_FORMAT_SUPPORT2_UAV_TYPED_STORE)</para>
    /// </summary>
    UnorderedAccessViewTypedStore = D3D11_FORMAT_SUPPORT2_UAV_TYPED_STORE,
};
/// <summary>
/// Type of data contained in an input slot.
/// <para>(Also see DirectX SDK: D3D11_INPUT_CLASSIFICATION)</para>
/// </summary>
public enum class InputClassification : Int32
{
    /// <summary>
    /// Input data is per-vertex data.
    /// <para>(Also see DirectX SDK: D3D11_INPUT_PER_VERTEX_DATA)</para>
    /// </summary>
    PerVertexData = D3D11_INPUT_PER_VERTEX_DATA,
    /// <summary>
    /// Input data is per-instance data.
    /// <para>(Also see DirectX SDK: D3D11_INPUT_PER_INSTANCE_DATA)</para>
    /// </summary>
    PerInstanceData = D3D11_INPUT_PER_INSTANCE_DATA,
};
/// <summary>
/// Identifies a resource to be accessed for reading and writing by the CPU. 
/// Applications may combine one or more of these flags.
/// <para>(Also see DirectX SDK: D3D11_MAP)</para>
/// </summary>
public enum class Map : Int32
{
    /// <summary>
    /// Resource is mapped for reading. The resource must have been created with read access (see <see cref="Read"/>)<seealso cref="Read"/>.
    /// <para>(Also see DirectX SDK: D3D11_MAP_READ)</para>
    /// </summary>
    Read = D3D11_MAP_READ,
    /// <summary>
    /// Resource is mapped for writing. The resource must have been created with write access (see <see cref="Write"/>)<seealso cref="Write"/>.
    /// <para>(Also see DirectX SDK: D3D11_MAP_WRITE)</para>
    /// </summary>
    Write = D3D11_MAP_WRITE,
    /// <summary>
    /// Resource is mapped for reading and writing. The resource must have been created with read and write access (see Read and Write).
    /// <para>(Also see DirectX SDK: D3D11_MAP_READ_WRITE)</para>
    /// </summary>
    ReadWrite = D3D11_MAP_READ_WRITE,
    /// <summary>
    /// Resource is mapped for writing; the previous contents of the resource will be undefined. The resource must have been created with write access (see <see cref="Write"/>)<seealso cref="Write"/>.
    /// <para>(Also see DirectX SDK: D3D11_MAP_WRITE_DISCARD)</para>
    /// </summary>
    WriteDiscard = D3D11_MAP_WRITE_DISCARD,
    /// <summary>
    /// Resource is mapped for writing; the existing contents of the resource cannot be overwritten. This flag is only valid on vertex and index buffers. The resource must have been created with write access (see <see cref="Write"/>)<seealso cref="Write"/>. Cannot be used on a resource created with the ConstantBuffer flag.
    /// <para>(Also see DirectX SDK: D3D11_MAP_WRITE_NO_OVERWRITE)</para>
    /// </summary>
    WriteNoOverwrite = D3D11_MAP_WRITE_NO_OVERWRITE,
};
/// <summary>
/// Specifies how the CPU should respond when Map is called on a resource being used by the GPU.
/// <para>(Also see DirectX SDK: D3D11_MAP_FLAG)</para>
/// </summary>
[Flags]
public enum class MapOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,

    /// <summary>
    /// Specifies that Map should return E_WASSTILLRENDERING when the GPU blocks the CPU from accessing a resource.
    /// <para>(Also see DirectX SDK: D3D11_MAP_FLAG_DO_NOT_WAIT)</para>
    /// </summary>
    DoNotWait = D3D11_MAP_FLAG_DO_NOT_WAIT,
};

/// <summary>
/// Categories of debug messages. This will identify the category of a message when retrieving a message with InfoQueue.GetMessage and when adding a message with InfoQueue.AddMessage. When creating an info queue filter, these values can be used to allow or deny any categories of messages to pass through the storage and retrieval filters.
/// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY)</para>
/// </summary>
public enum class MessageCategory : Int32
{
    /// <summary>
    /// User defined message. See InfoQueue.AddMessage.
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY_APPLICATION_DEFINED)</para>
    /// </summary>
    ApplicationDefined = D3D11_MESSAGE_CATEGORY_APPLICATION_DEFINED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY_MISCELLANEOUS)</para>
    /// </summary>
    Miscellaneous = D3D11_MESSAGE_CATEGORY_MISCELLANEOUS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY_INITIALIZATION)</para>
    /// </summary>
    Initialization = D3D11_MESSAGE_CATEGORY_INITIALIZATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY_CLEANUP)</para>
    /// </summary>
    Cleanup = D3D11_MESSAGE_CATEGORY_CLEANUP,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY_COMPILATION)</para>
    /// </summary>
    Compilation = D3D11_MESSAGE_CATEGORY_COMPILATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY_STATE_CREATION)</para>
    /// </summary>
    StateCreation = D3D11_MESSAGE_CATEGORY_STATE_CREATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY_STATE_SETTING)</para>
    /// </summary>
    StateSetting = D3D11_MESSAGE_CATEGORY_STATE_SETTING,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY_STATE_GETTING)</para>
    /// </summary>
    StateGetting = D3D11_MESSAGE_CATEGORY_STATE_GETTING,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY_RESOURCE_MANIPULATION)</para>
    /// </summary>
    ResourceManipulation = D3D11_MESSAGE_CATEGORY_RESOURCE_MANIPULATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_CATEGORY_EXECUTION)</para>
    /// </summary>
    Execution = D3D11_MESSAGE_CATEGORY_EXECUTION,
};
/// <summary>
/// Debug messages for setting up an info-queue filter; use these messages to allow or deny 
/// message categories to pass through the storage and retrieval filters. 
/// These IDs are used by methods such as InfoQueue.GetMessage or InfoQueue.AddMessage.
/// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID)</para>
/// </summary>
public enum class MessageId : Int32
{
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_UNKNOWN)</para>
    /// </summary>
    Unknown = D3D11_MESSAGE_ID_UNKNOWN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_IASETVERTEXBUFFERS_HAZARD)</para>
    /// </summary>
    DeviceIASetVertexBuffersHazard = D3D11_MESSAGE_ID_DEVICE_IASETVERTEXBUFFERS_HAZARD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_IASETINDEXBUFFER_HAZARD)</para>
    /// </summary>
    DeviceIASetIndexBufferHazard = D3D11_MESSAGE_ID_DEVICE_IASETINDEXBUFFER_HAZARD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_VSSETSHADERRESOURCES_HAZARD)</para>
    /// </summary>
    DeviceVSSetShaderResourcesHazard = D3D11_MESSAGE_ID_DEVICE_VSSETSHADERRESOURCES_HAZARD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_VSSETCONSTANTBUFFERS_HAZARD)</para>
    /// </summary>
    DeviceVSSetConstantBuffersHazard = D3D11_MESSAGE_ID_DEVICE_VSSETCONSTANTBUFFERS_HAZARD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_GSSETSHADERRESOURCES_HAZARD)</para>
    /// </summary>
    DeviceGSSetShaderResourcesHazard = D3D11_MESSAGE_ID_DEVICE_GSSETSHADERRESOURCES_HAZARD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_GSSETCONSTANTBUFFERS_HAZARD)</para>
    /// </summary>
    DeviceGSSetConstantBuffersHazard = D3D11_MESSAGE_ID_DEVICE_GSSETCONSTANTBUFFERS_HAZARD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_PSSETSHADERRESOURCES_HAZARD)</para>
    /// </summary>
    DevicePSSetShaderResourcesHazard = D3D11_MESSAGE_ID_DEVICE_PSSETSHADERRESOURCES_HAZARD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_PSSETCONSTANTBUFFERS_HAZARD)</para>
    /// </summary>
    DevicePSSetConstantBuffersHazard = D3D11_MESSAGE_ID_DEVICE_PSSETCONSTANTBUFFERS_HAZARD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_OMSETRENDERTARGETS_HAZARD)</para>
    /// </summary>
    DeviceOMSetRenderTargetsHazard = D3D11_MESSAGE_ID_DEVICE_OMSETRENDERTARGETS_HAZARD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_SOSETTARGETS_HAZARD)</para>
    /// </summary>
    DeviceSOSetTargetsHazard = D3D11_MESSAGE_ID_DEVICE_SOSETTARGETS_HAZARD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_STRING_FROM_APPLICATION)</para>
    /// </summary>
    StringFromApplication = D3D11_MESSAGE_ID_STRING_FROM_APPLICATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_THIS)</para>
    /// </summary>
    CorruptedThis = D3D11_MESSAGE_ID_CORRUPTED_THIS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER1)</para>
    /// </summary>
    CorruptedParameter1 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER1,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER2)</para>
    /// </summary>
    CorruptedParameter2 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER2,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER3)</para>
    /// </summary>
    CorruptedParameter3 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER3,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER4)</para>
    /// </summary>
    CorruptedParameter4 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER4,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER5)</para>
    /// </summary>
    CorruptedParameter5 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER5,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER6)</para>
    /// </summary>
    CorruptedParameter6 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER6,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER7)</para>
    /// </summary>
    CorruptedParameter7 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER7,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER8)</para>
    /// </summary>
    CorruptedParameter8 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER8,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER9)</para>
    /// </summary>
    CorruptedParameter9 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER9,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER10)</para>
    /// </summary>
    CorruptedParameter10 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER10,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER11)</para>
    /// </summary>
    CorruptedParameter11 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER11,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER12)</para>
    /// </summary>
    CorruptedParameter12 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER12,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER13)</para>
    /// </summary>
    CorruptedParameter13 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER13,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER14)</para>
    /// </summary>
    CorruptedParameter14 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER14,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_PARAMETER15)</para>
    /// </summary>
    CorruptedParameter15 = D3D11_MESSAGE_ID_CORRUPTED_PARAMETER15,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CORRUPTED_MULTITHREADING)</para>
    /// </summary>
    CorruptedMultithreading = D3D11_MESSAGE_ID_CORRUPTED_MULTITHREADING,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_MESSAGE_REPORTING_OUTOFMEMORY)</para>
    /// </summary>
    MessageReportingOutOfMemory = D3D11_MESSAGE_ID_MESSAGE_REPORTING_OUTOFMEMORY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_IASETINPUTLAYOUT_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    IASetInputLayoutUnbindDeletingObject = D3D11_MESSAGE_ID_IASETINPUTLAYOUT_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_IASETVERTEXBUFFERS_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    IASetVertexBuffersUnbindDeletingObject = D3D11_MESSAGE_ID_IASETVERTEXBUFFERS_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_IASETINDEXBUFFER_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    IASetIndexBufferUnbindDeletingObject = D3D11_MESSAGE_ID_IASETINDEXBUFFER_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_VSSETSHADER_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    VSSetShaderUnbindDeletingObject = D3D11_MESSAGE_ID_VSSETSHADER_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_VSSETSHADERRESOURCES_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    VSSetShaderResourcesUnbindDeletingObject = D3D11_MESSAGE_ID_VSSETSHADERRESOURCES_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_VSSETCONSTANTBUFFERS_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    VSSetConstantBuffersUnbindDeletingObject = D3D11_MESSAGE_ID_VSSETCONSTANTBUFFERS_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_VSSETSAMPLERS_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    VSSetSamplersUnbindDeletingObject = D3D11_MESSAGE_ID_VSSETSAMPLERS_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_GSSETSHADER_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    GSSetShaderUnbindDeletingObject = D3D11_MESSAGE_ID_GSSETSHADER_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_GSSETSHADERRESOURCES_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    GSSetShaderResourcesUnbindDeletingObject = D3D11_MESSAGE_ID_GSSETSHADERRESOURCES_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_GSSETCONSTANTBUFFERS_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    GSSetConstantBuffersUnbindDeletingObject = D3D11_MESSAGE_ID_GSSETCONSTANTBUFFERS_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_GSSETSAMPLERS_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    GSSetSamplersUnbindDeletingObject = D3D11_MESSAGE_ID_GSSETSAMPLERS_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SOSETTARGETS_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    SOSetTargetsUnbindDeletingObject = D3D11_MESSAGE_ID_SOSETTARGETS_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_PSSETSHADER_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    PSSetShaderUnbindDeletingObject = D3D11_MESSAGE_ID_PSSETSHADER_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_PSSETSHADERRESOURCES_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    PSSetShaderResourcesUnbindDeletingObject = D3D11_MESSAGE_ID_PSSETSHADERRESOURCES_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_PSSETCONSTANTBUFFERS_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    PSSetConstantBuffersUnbindDeletingObject = D3D11_MESSAGE_ID_PSSETCONSTANTBUFFERS_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_PSSETSAMPLERS_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    PSSetSamplersUnbindDeletingObject = D3D11_MESSAGE_ID_PSSETSAMPLERS_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_RSSETSTATE_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    RSSetStateUnbindDeletingObject = D3D11_MESSAGE_ID_RSSETSTATE_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_OMSETBLENDSTATE_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    OMSetBlendStateUnbindDeletingObject = D3D11_MESSAGE_ID_OMSETBLENDSTATE_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_OMSETDEPTHSTENCILSTATE_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    OMSetDepthStencilStateUnbindDeletingObject = D3D11_MESSAGE_ID_OMSETDEPTHSTENCILSTATE_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_OMSETRENDERTARGETS_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    OMSetRenderTargetsUnbindDeletingObject = D3D11_MESSAGE_ID_OMSETRENDERTARGETS_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SETPREDICATION_UNBINDDELETINGOBJECT)</para>
    /// </summary>
    SetPredicationUnbindDeletingObject = D3D11_MESSAGE_ID_SETPREDICATION_UNBINDDELETINGOBJECT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_GETPRIVATEDATA_MOREDATA)</para>
    /// </summary>
    GetPrivateDataMoreData = D3D11_MESSAGE_ID_GETPRIVATEDATA_MOREDATA,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SETPRIVATEDATA_INVALIDFREEDATA)</para>
    /// </summary>
    SetPrivateDataInvalidFreeData = D3D11_MESSAGE_ID_SETPRIVATEDATA_INVALIDFREEDATA,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SETPRIVATEDATA_INVALIDIUNKNOWN)</para>
    /// </summary>
    SetPrivateDataInvalidIUnknown = D3D11_MESSAGE_ID_SETPRIVATEDATA_INVALIDIUNKNOWN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SETPRIVATEDATA_INVALIDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    SetPrivateDataInvalidFlags = D3D11_MESSAGE_ID_SETPRIVATEDATA_INVALIDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SETPRIVATEDATA_CHANGINGPARAMS)</para>
    /// </summary>
    SetPrivateDataChangingParams = D3D11_MESSAGE_ID_SETPRIVATEDATA_CHANGINGPARAMS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SETPRIVATEDATA_OUTOFMEMORY)</para>
    /// </summary>
    SetPrivateDataOutOfMemory = D3D11_MESSAGE_ID_SETPRIVATEDATA_OUTOFMEMORY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_UNRECOGNIZEDFORMAT)</para>
    /// </summary>
    CreateBufferUnrecognizedFormat = D3D11_MESSAGE_ID_CREATEBUFFER_UNRECOGNIZEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDSAMPLES)</para>
    /// </summary>
    CreateBufferInvalidSamples = D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDSAMPLES,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_UNRECOGNIZEDUSAGE)</para>
    /// </summary>
    CreateBufferUnrecognizedUsage = D3D11_MESSAGE_ID_CREATEBUFFER_UNRECOGNIZEDUSAGE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_UNRECOGNIZEDBINDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateBufferUnrecognizedBindFlags = D3D11_MESSAGE_ID_CREATEBUFFER_UNRECOGNIZEDBINDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_UNRECOGNIZEDCPUACCESSFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateBufferUnrecognizedCpuAccessFlags = D3D11_MESSAGE_ID_CREATEBUFFER_UNRECOGNIZEDCPUACCESSFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_UNRECOGNIZEDMISCFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateBufferUnrecognizedMiscFlags = D3D11_MESSAGE_ID_CREATEBUFFER_UNRECOGNIZEDMISCFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDCPUACCESSFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateBufferInvalidCpuAccessFlags = D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDCPUACCESSFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDBINDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateBufferInvalidBindFlags = D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDBINDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDINITIALDATA)</para>
    /// </summary>
    CreateBufferInvalidInitialData = D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDINITIALDATA,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDDIMENSIONS)</para>
    /// </summary>
    CreateBufferInvalidDimensions = D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDDIMENSIONS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDMIPLEVELS)</para>
    /// </summary>
    CreateBufferInvalidMipLevels = D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDMIPLEVELS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDMISCFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateBufferInvalidMiscFlags = D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDMISCFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDARG_RETURN)</para>
    /// </summary>
    CreateBufferInvalidArgReturn = D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDARG_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    CreateBufferOutOfMemoryReturn = D3D11_MESSAGE_ID_CREATEBUFFER_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_NULLDESC)</para>
    /// </summary>
    CreateBufferNullDesc = D3D11_MESSAGE_ID_CREATEBUFFER_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDCONSTANTBUFFERBINDINGS)</para>
    /// </summary>
    CreateBufferInvalidConstantBufferBindings = D3D11_MESSAGE_ID_CREATEBUFFER_INVALIDCONSTANTBUFFERBINDINGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBUFFER_LARGEALLOCATION)</para>
    /// </summary>
    CreateBufferLargeAllocation = D3D11_MESSAGE_ID_CREATEBUFFER_LARGEALLOCATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_UNRECOGNIZEDFORMAT)</para>
    /// </summary>
    CreateTexture1DUnrecognizedFormat = D3D11_MESSAGE_ID_CREATETEXTURE1D_UNRECOGNIZEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_UNSUPPORTEDFORMAT)</para>
    /// </summary>
    CreateTexture1DUnsupportedFormat = D3D11_MESSAGE_ID_CREATETEXTURE1D_UNSUPPORTEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDSAMPLES)</para>
    /// </summary>
    CreateTexture1DInvalidSamples = D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDSAMPLES,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_UNRECOGNIZEDUSAGE)</para>
    /// </summary>
    CreateTexture1DUnrecognizedUsage = D3D11_MESSAGE_ID_CREATETEXTURE1D_UNRECOGNIZEDUSAGE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_UNRECOGNIZEDBINDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture1DUnrecognizedBindFlags = D3D11_MESSAGE_ID_CREATETEXTURE1D_UNRECOGNIZEDBINDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_UNRECOGNIZEDCPUACCESSFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture1DUnrecognizedCpuAccessFlags = D3D11_MESSAGE_ID_CREATETEXTURE1D_UNRECOGNIZEDCPUACCESSFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_UNRECOGNIZEDMISCFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture1DUnrecognizedMiscFlags = D3D11_MESSAGE_ID_CREATETEXTURE1D_UNRECOGNIZEDMISCFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDCPUACCESSFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture1DInvalidCpuAccessFlags = D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDCPUACCESSFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDBINDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture1DInvalidBindFlags = D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDBINDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDINITIALDATA)</para>
    /// </summary>
    CreateTexture1DInvalidInitialData = D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDINITIALDATA,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDDIMENSIONS)</para>
    /// </summary>
    CreateTexture1DInvalidDimensions = D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDDIMENSIONS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDMIPLEVELS)</para>
    /// </summary>
    CreateTexture1DInvalidMipLevels = D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDMIPLEVELS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDMISCFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture1DInvalidMiscFlags = D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDMISCFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDARG_RETURN)</para>
    /// </summary>
    CreateTexture1DInvalidArgReturn = D3D11_MESSAGE_ID_CREATETEXTURE1D_INVALIDARG_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    CreateTexture1DOutOfMemoryReturn = D3D11_MESSAGE_ID_CREATETEXTURE1D_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_NULLDESC)</para>
    /// </summary>
    CreateTexture1DNullDesc = D3D11_MESSAGE_ID_CREATETEXTURE1D_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE1D_LARGEALLOCATION)</para>
    /// </summary>
    CreateTexture1DLargeAllocation = D3D11_MESSAGE_ID_CREATETEXTURE1D_LARGEALLOCATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_UNRECOGNIZEDFORMAT)</para>
    /// </summary>
    CreateTexture2DUnrecognizedFormat = D3D11_MESSAGE_ID_CREATETEXTURE2D_UNRECOGNIZEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_UNSUPPORTEDFORMAT)</para>
    /// </summary>
    CreateTexture2DUnsupportedFormat = D3D11_MESSAGE_ID_CREATETEXTURE2D_UNSUPPORTEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDSAMPLES)</para>
    /// </summary>
    CreateTexture2DInvalidSamples = D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDSAMPLES,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_UNRECOGNIZEDUSAGE)</para>
    /// </summary>
    CreateTexture2DUnrecognizedUsage = D3D11_MESSAGE_ID_CREATETEXTURE2D_UNRECOGNIZEDUSAGE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_UNRECOGNIZEDBINDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture2DUnrecognizedBindFlags = D3D11_MESSAGE_ID_CREATETEXTURE2D_UNRECOGNIZEDBINDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_UNRECOGNIZEDCPUACCESSFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture2DUnrecognizedCpuAccessFlags = D3D11_MESSAGE_ID_CREATETEXTURE2D_UNRECOGNIZEDCPUACCESSFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_UNRECOGNIZEDMISCFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture2DUnrecognizedMiscFlags = D3D11_MESSAGE_ID_CREATETEXTURE2D_UNRECOGNIZEDMISCFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDCPUACCESSFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture2DInvalidCpuAccessFlags = D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDCPUACCESSFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDBINDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture2DInvalidBindFlags = D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDBINDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDINITIALDATA)</para>
    /// </summary>
    CreateTexture2DInvalidInitialData = D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDINITIALDATA,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDDIMENSIONS)</para>
    /// </summary>
    CreateTexture2DInvalidDimensions = D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDDIMENSIONS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDMIPLEVELS)</para>
    /// </summary>
    CreateTexture2DInvalidMipLevels = D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDMIPLEVELS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDMISCFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture2DInvalidMiscFlags = D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDMISCFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDARG_RETURN)</para>
    /// </summary>
    CreateTexture2DInvalidArgReturn = D3D11_MESSAGE_ID_CREATETEXTURE2D_INVALIDARG_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    CreateTexture2DOutOfMemoryReturn = D3D11_MESSAGE_ID_CREATETEXTURE2D_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_NULLDESC)</para>
    /// </summary>
    CreateTexture2DNullDesc = D3D11_MESSAGE_ID_CREATETEXTURE2D_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE2D_LARGEALLOCATION)</para>
    /// </summary>
    CreateTexture2DLargeAllocation = D3D11_MESSAGE_ID_CREATETEXTURE2D_LARGEALLOCATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_UNRECOGNIZEDFORMAT)</para>
    /// </summary>
    CreateTexture3DUnrecognizedFormat = D3D11_MESSAGE_ID_CREATETEXTURE3D_UNRECOGNIZEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_UNSUPPORTEDFORMAT)</para>
    /// </summary>
    CreateTexture3DUnsupportedFormat = D3D11_MESSAGE_ID_CREATETEXTURE3D_UNSUPPORTEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDSAMPLES)</para>
    /// </summary>
    CreateTexture3DInvalidSamples = D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDSAMPLES,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_UNRECOGNIZEDUSAGE)</para>
    /// </summary>
    CreateTexture3DUnrecognizedUsage = D3D11_MESSAGE_ID_CREATETEXTURE3D_UNRECOGNIZEDUSAGE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_UNRECOGNIZEDBINDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture3DUnrecognizedBindFlags = D3D11_MESSAGE_ID_CREATETEXTURE3D_UNRECOGNIZEDBINDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_UNRECOGNIZEDCPUACCESSFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture3DUnrecognizedCpuAccessFlags = D3D11_MESSAGE_ID_CREATETEXTURE3D_UNRECOGNIZEDCPUACCESSFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_UNRECOGNIZEDMISCFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture3DUnrecognizedMiscFlags = D3D11_MESSAGE_ID_CREATETEXTURE3D_UNRECOGNIZEDMISCFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDCPUACCESSFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture3DInvalidCpuAccessFlags = D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDCPUACCESSFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDBINDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture3DInvalidBindFlags = D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDBINDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDINITIALDATA)</para>
    /// </summary>
    CreateTexture3DInvalidInitialData = D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDINITIALDATA,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDDIMENSIONS)</para>
    /// </summary>
    CreateTexture3DInvalidDimensions = D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDDIMENSIONS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDMIPLEVELS)</para>
    /// </summary>
    CreateTexture3DInvalidMipLevels = D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDMIPLEVELS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDMISCFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateTexture3DInvalidMiscFlags = D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDMISCFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDARG_RETURN)</para>
    /// </summary>
    CreateTexture3DInvalidArgReturn = D3D11_MESSAGE_ID_CREATETEXTURE3D_INVALIDARG_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    CreateTexture3DOutOfMemoryReturn = D3D11_MESSAGE_ID_CREATETEXTURE3D_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_NULLDESC)</para>
    /// </summary>
    CreateTexture3DNullDesc = D3D11_MESSAGE_ID_CREATETEXTURE3D_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATETEXTURE3D_LARGEALLOCATION)</para>
    /// </summary>
    CreateTexture3DLargeAllocation = D3D11_MESSAGE_ID_CREATETEXTURE3D_LARGEALLOCATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_UNRECOGNIZEDFORMAT)</para>
    /// </summary>
    CreateShaderResourceViewUnrecognizedFormat = D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_UNRECOGNIZEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_INVALIDDESC)</para>
    /// </summary>
    CreateShaderResourceViewInvalidDesc = D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_INVALIDDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_INVALIDFORMAT)</para>
    /// </summary>
    CreateShaderResourceViewInvalidFormat = D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_INVALIDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_INVALIDDIMENSIONS)</para>
    /// </summary>
    CreateShaderResourceViewInvalidDimensions = D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_INVALIDDIMENSIONS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_INVALIDRESOURCE)</para>
    /// </summary>
    CreateShaderResourceViewInvalidResource = D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_INVALIDRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_TOOMANYOBJECTS)</para>
    /// </summary>
    CreateShaderResourceViewTooManyObjects = D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_TOOMANYOBJECTS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_INVALIDARG_RETURN)</para>
    /// </summary>
    CreateShaderResourceViewInvalidArgReturn = D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_INVALIDARG_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    CreateShaderResourceViewOutOfMemoryReturn = D3D11_MESSAGE_ID_CREATESHADERRESOURCEVIEW_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_UNRECOGNIZEDFORMAT)</para>
    /// </summary>
    CreateRenderTargetViewUnrecognizedFormat = D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_UNRECOGNIZEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_UNSUPPORTEDFORMAT)</para>
    /// </summary>
    CreateRenderTargetViewUnsupportedFormat = D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_UNSUPPORTEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_INVALIDDESC)</para>
    /// </summary>
    CreateRenderTargetViewInvalidDesc = D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_INVALIDDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_INVALIDFORMAT)</para>
    /// </summary>
    CreateRenderTargetViewInvalidFormat = D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_INVALIDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_INVALIDDIMENSIONS)</para>
    /// </summary>
    CreateRenderTargetViewInvalidDimensions = D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_INVALIDDIMENSIONS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_INVALIDRESOURCE)</para>
    /// </summary>
    CreateRenderTargetViewInvalidResource = D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_INVALIDRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_TOOMANYOBJECTS)</para>
    /// </summary>
    CreateRenderTargetViewTooManyObjects = D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_TOOMANYOBJECTS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_INVALIDARG_RETURN)</para>
    /// </summary>
    CreateRenderTargetViewInvalidArgReturn = D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_INVALIDARG_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    CreateRenderTargetViewOutOfMemoryReturn = D3D11_MESSAGE_ID_CREATERENDERTARGETVIEW_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_UNRECOGNIZEDFORMAT)</para>
    /// </summary>
    CreateDepthStencilViewUnrecognizedFormat = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_UNRECOGNIZEDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_INVALIDDESC)</para>
    /// </summary>
    CreateDepthStencilViewInvalidDesc = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_INVALIDDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_INVALIDFORMAT)</para>
    /// </summary>
    CreateDepthStencilViewInvalidFormat = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_INVALIDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_INVALIDDIMENSIONS)</para>
    /// </summary>
    CreateDepthStencilViewInvalidDimensions = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_INVALIDDIMENSIONS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_INVALIDRESOURCE)</para>
    /// </summary>
    CreateDepthStencilViewInvalidResource = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_INVALIDRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_TOOMANYOBJECTS)</para>
    /// </summary>
    CreateDepthStencilViewTooManyObjects = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_TOOMANYOBJECTS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_INVALIDARG_RETURN)</para>
    /// </summary>
    CreateDepthStencilViewInvalidArgReturn = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_INVALIDARG_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    CreateDepthStencilViewOutOfMemoryReturn = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILVIEW_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_OUTOFMEMORY)</para>
    /// </summary>
    CreateInputLayoutOutOfMemory = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_OUTOFMEMORY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_TOOMANYELEMENTS)</para>
    /// </summary>
    CreateInputLayoutTooManyElements = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_TOOMANYELEMENTS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDFORMAT)</para>
    /// </summary>
    CreateInputLayoutInvalidFormat = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INCOMPATIBLEFORMAT)</para>
    /// </summary>
    CreateInputLayoutIncompatibleFormat = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INCOMPATIBLEFORMAT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDSLOT)</para>
    /// </summary>
    CreateInputLayoutInvalidSlot = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDSLOT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDINPUTSLOTCLASS)</para>
    /// </summary>
    CreateInputLayoutInvalidInputSlotClass = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDINPUTSLOTCLASS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_STEPRATESLOTCLASSMISMATCH)</para>
    /// </summary>
    CreateInputLayoutStepRateSlotClassMismatch = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_STEPRATESLOTCLASSMISMATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDSLOTCLASSCHANGE)</para>
    /// </summary>
    CreateInputLayoutInvalidSlotClassChange = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDSLOTCLASSCHANGE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDSTEPRATECHANGE)</para>
    /// </summary>
    CreateInputLayoutInvalidStepRateChange = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDSTEPRATECHANGE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDALIGNMENT)</para>
    /// </summary>
    CreateInputLayoutInvalidAlignment = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_INVALIDALIGNMENT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_DUPLICATESEMANTIC)</para>
    /// </summary>
    CreateInputLayoutDuplicateSemantic = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_DUPLICATESEMANTIC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_UNPARSEABLEINPUTSIGNATURE)</para>
    /// </summary>
    CreateInputLayoutUnparsableInputSignature = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_UNPARSEABLEINPUTSIGNATURE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_NULLSEMANTIC)</para>
    /// </summary>
    CreateInputLayoutNullSemantic = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_NULLSEMANTIC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_MISSINGELEMENT)</para>
    /// </summary>
    CreateInputLayoutMissingElement = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_MISSINGELEMENT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_NULLDESC)</para>
    /// </summary>
    CreateInputLayoutNullDesc = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEVERTEXSHADER_OUTOFMEMORY)</para>
    /// </summary>
    CreateVertexShaderOutOfMemory = D3D11_MESSAGE_ID_CREATEVERTEXSHADER_OUTOFMEMORY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEVERTEXSHADER_INVALIDSHADERBYTECODE)</para>
    /// </summary>
    CreateVertexShaderInvalidShaderBytecode = D3D11_MESSAGE_ID_CREATEVERTEXSHADER_INVALIDSHADERBYTECODE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEVERTEXSHADER_INVALIDSHADERTYPE)</para>
    /// </summary>
    CreateVertexShaderInvalidShaderType = D3D11_MESSAGE_ID_CREATEVERTEXSHADER_INVALIDSHADERTYPE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADER_OUTOFMEMORY)</para>
    /// </summary>
    CreateGeometryShaderOutOfMemory = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADER_OUTOFMEMORY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADER_INVALIDSHADERBYTECODE)</para>
    /// </summary>
    CreateGeometryShaderInvalidShaderBytecode = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADER_INVALIDSHADERBYTECODE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADER_INVALIDSHADERTYPE)</para>
    /// </summary>
    CreateGeometryShaderInvalidShaderType = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADER_INVALIDSHADERTYPE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_OUTOFMEMORY)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputOutOfMemory = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_OUTOFMEMORY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDSHADERBYTECODE)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputInvalidShaderBytecode = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDSHADERBYTECODE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDSHADERTYPE)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputInvalidShaderType = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDSHADERTYPE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDNUMENTRIES)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId="Num")
    CreateGeometryShaderWithStreamOutputInvalidNumEntries = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDNUMENTRIES,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_OUTPUTSTREAMSTRIDEUNUSED)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputOutputStreamStrideUnused = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_OUTPUTSTREAMSTRIDEUNUSED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_UNEXPECTEDDECL)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputUnexpectedDecl = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_UNEXPECTEDDECL,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_EXPECTEDDECL)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputExpectedDecl = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_EXPECTEDDECL,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_OUTPUTSLOT0EXPECTED)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputOutputSlot0Expected = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_OUTPUTSLOT0EXPECTED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDOUTPUTSLOT)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputInvalidOutputSlot = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDOUTPUTSLOT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_ONLYONEELEMENTPERSLOT)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputOnlyOneElementPerSlot = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_ONLYONEELEMENTPERSLOT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDCOMPONENTCOUNT)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputInvalidComponentCount = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDCOMPONENTCOUNT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDSTARTCOMPONENTANDCOMPONENTCOUNT)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputInvalidStartComponentAndComponentCount = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDSTARTCOMPONENTANDCOMPONENTCOUNT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDGAPDEFINITION)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputInvalidGapDefinition = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDGAPDEFINITION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_REPEATEDOUTPUT)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputRepeatedOutput = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_REPEATEDOUTPUT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDOUTPUTSTREAMSTRIDE)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputInvalidOutputStreamStride = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_INVALIDOUTPUTSTREAMSTRIDE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_MISSINGSEMANTIC)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputMissingSemantic = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_MISSINGSEMANTIC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_MASKMISMATCH)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputMaskMismatch = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_MASKMISMATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_CANTHAVEONLYGAPS)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputCannotHaveOnlyGaps = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_CANTHAVEONLYGAPS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_DECLTOOCOMPLEX)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputDeclTooComplex = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_DECLTOOCOMPLEX,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_MISSINGOUTPUTSIGNATURE)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputMissingOutputSignature = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_MISSINGOUTPUTSIGNATURE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEPIXELSHADER_OUTOFMEMORY)</para>
    /// </summary>
    CreatePixelShaderOutOfMemory = D3D11_MESSAGE_ID_CREATEPIXELSHADER_OUTOFMEMORY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEPIXELSHADER_INVALIDSHADERBYTECODE)</para>
    /// </summary>
    CreatePixelShaderInvalidShaderBytecode = D3D11_MESSAGE_ID_CREATEPIXELSHADER_INVALIDSHADERBYTECODE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEPIXELSHADER_INVALIDSHADERTYPE)</para>
    /// </summary>
    CreatePixelShaderInvalidShaderType = D3D11_MESSAGE_ID_CREATEPIXELSHADER_INVALIDSHADERTYPE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_INVALIDFILLMODE)</para>
    /// </summary>
    CreateRasterizerStateInvalidFillMode = D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_INVALIDFILLMODE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_INVALIDCULLMODE)</para>
    /// </summary>
    CreateRasterizerStateInvalidCullMode = D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_INVALIDCULLMODE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_INVALIDDEPTHBIASCLAMP)</para>
    /// </summary>
    CreateRasterizerStateInvalidDepthBiasClamp = D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_INVALIDDEPTHBIASCLAMP,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_INVALIDSLOPESCALEDDEPTHBIAS)</para>
    /// </summary>
    CreateRasterizerStateInvalidSlopeScaledDepthBias = D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_INVALIDSLOPESCALEDDEPTHBIAS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_TOOMANYOBJECTS)</para>
    /// </summary>
    CreateRasterizerStateTooManyObjects = D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_TOOMANYOBJECTS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_NULLDESC)</para>
    /// </summary>
    CreateRasterizerStateNullDesc = D3D11_MESSAGE_ID_CREATERASTERIZERSTATE_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDDEPTHWRITEMASK)</para>
    /// </summary>
    CreateDepthStencilStateInvalidDepthWriteMask = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDDEPTHWRITEMASK,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDDEPTHFUNC)</para>
    /// </summary>
    CreateDepthStencilStateInvalidDepthFunc = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDDEPTHFUNC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDFRONTFACESTENCILFAILOP)</para>
    /// </summary>
    CreateDepthStencilStateInvalidFrontFaceStencilFailOperation = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDFRONTFACESTENCILFAILOP,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDFRONTFACESTENCILZFAILOP)</para>
    /// </summary>
    CreateDepthStencilStateInvalidFrontFaceStencilZFailOperation = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDFRONTFACESTENCILZFAILOP,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDFRONTFACESTENCILPASSOP)</para>
    /// </summary>
    CreateDepthStencilStateInvalidFrontFaceStencilPassOperation = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDFRONTFACESTENCILPASSOP,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDFRONTFACESTENCILFUNC)</para>
    /// </summary>
    CreateDepthStencilStateInvalidFrontFaceStencilFunc = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDFRONTFACESTENCILFUNC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDBACKFACESTENCILFAILOP)</para>
    /// </summary>
    CreateDepthStencilStateInvalidBackFaceStencilFailOperation = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDBACKFACESTENCILFAILOP,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDBACKFACESTENCILZFAILOP)</para>
    /// </summary>
    CreateDepthStencilStateInvalidBackFaceStencilZFailOperation = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDBACKFACESTENCILZFAILOP,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDBACKFACESTENCILPASSOP)</para>
    /// </summary>
    CreateDepthStencilStateInvalidBackFaceStencilPassOperation = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDBACKFACESTENCILPASSOP,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDBACKFACESTENCILFUNC)</para>
    /// </summary>
    CreateDepthStencilStateInvalidBackFaceStencilFunc = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_INVALIDBACKFACESTENCILFUNC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_TOOMANYOBJECTS)</para>
    /// </summary>
    CreateDepthStencilStateTooManyObjects = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_TOOMANYOBJECTS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_NULLDESC)</para>
    /// </summary>
    CreateDepthStencilStateNullDesc = D3D11_MESSAGE_ID_CREATEDEPTHSTENCILSTATE_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDSRCBLEND)</para>
    /// </summary>
    CreateBlendStateInvalidSourceBlend = D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDSRCBLEND,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDDESTBLEND)</para>
    /// </summary>
    CreateBlendStateInvalidDestinationBlend = D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDDESTBLEND,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDBLENDOP)</para>
    /// </summary>
    CreateBlendStateInvalidBlendOperation = D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDBLENDOP,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDSRCBLENDALPHA)</para>
    /// </summary>
    CreateBlendStateInvalidSourceBlendAlpha = D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDSRCBLENDALPHA,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDDESTBLENDALPHA)</para>
    /// </summary>
    CreateBlendStateInvalidDestinationBlendAlpha = D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDDESTBLENDALPHA,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDBLENDOPALPHA)</para>
    /// </summary>
    CreateBlendStateInvalidBlendOperationAlpha = D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDBLENDOPALPHA,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDRENDERTARGETWRITEMASK)</para>
    /// </summary>
    CreateBlendStateInvalidRenderTargetWriteMask = D3D11_MESSAGE_ID_CREATEBLENDSTATE_INVALIDRENDERTARGETWRITEMASK,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBLENDSTATE_TOOMANYOBJECTS)</para>
    /// </summary>
    CreateBlendStateTooManyObjects = D3D11_MESSAGE_ID_CREATEBLENDSTATE_TOOMANYOBJECTS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEBLENDSTATE_NULLDESC)</para>
    /// </summary>
    CreateBlendStateNullDesc = D3D11_MESSAGE_ID_CREATEBLENDSTATE_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDFILTER)</para>
    /// </summary>
    CreateSamplerStateInvalidFilter = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDFILTER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDADDRESSU)</para>
    /// </summary>
    CreateSamplerStateInvalidAddressU = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDADDRESSU,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDADDRESSV)</para>
    /// </summary>
    CreateSamplerStateInvalidAddressV = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDADDRESSV,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDADDRESSW)</para>
    /// </summary>
    CreateSamplerStateInvalidAddressW = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDADDRESSW,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDMIPLODBIAS)</para>
    /// </summary>
    CreateSamplerStateInvalidMipLodBias = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDMIPLODBIAS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDMAXANISOTROPY)</para>
    /// </summary>
    CreateSamplerStateInvalidMaxAnisotropy = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDMAXANISOTROPY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDCOMPARISONFUNC)</para>
    /// </summary>
    CreateSamplerStateInvalidComparisonFunc = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDCOMPARISONFUNC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDMINLOD)</para>
    /// </summary>
    CreateSamplerStateInvalidMinLod = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDMINLOD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDMAXLOD)</para>
    /// </summary>
    CreateSamplerStateInvalidMaxLod = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_INVALIDMAXLOD,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_TOOMANYOBJECTS)</para>
    /// </summary>
    CreateSamplerStateTooManyObjects = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_TOOMANYOBJECTS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATESAMPLERSTATE_NULLDESC)</para>
    /// </summary>
    CreateSamplerStateNullDesc = D3D11_MESSAGE_ID_CREATESAMPLERSTATE_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEQUERYORPREDICATE_INVALIDQUERY)</para>
    /// </summary>
    CreateQueryOrPredicateInvalidQuery = D3D11_MESSAGE_ID_CREATEQUERYORPREDICATE_INVALIDQUERY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEQUERYORPREDICATE_INVALIDMISCFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    CreateQueryOrPredicateInvalidMiscFlags = D3D11_MESSAGE_ID_CREATEQUERYORPREDICATE_INVALIDMISCFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEQUERYORPREDICATE_UNEXPECTEDMISCFLAG)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flag")
    CreateQueryOrPredicateUnexpectedMiscFlag = D3D11_MESSAGE_ID_CREATEQUERYORPREDICATE_UNEXPECTEDMISCFLAG,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEQUERYORPREDICATE_NULLDESC)</para>
    /// </summary>
    CreateQueryOrPredicateNullDesc = D3D11_MESSAGE_ID_CREATEQUERYORPREDICATE_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_IASETPRIMITIVETOPOLOGY_TOPOLOGY_UNRECOGNIZED)</para>
    /// </summary>
    DeviceIASetPrimitiveTopologyTopologyUnrecognized = D3D11_MESSAGE_ID_DEVICE_IASETPRIMITIVETOPOLOGY_TOPOLOGY_UNRECOGNIZED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_IASETPRIMITIVETOPOLOGY_TOPOLOGY_UNDEFINED)</para>
    /// </summary>
    DeviceIASetPrimitiveTopologyTopologyUndefined = D3D11_MESSAGE_ID_DEVICE_IASETPRIMITIVETOPOLOGY_TOPOLOGY_UNDEFINED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_IASETVERTEXBUFFERS_INVALIDBUFFER)</para>
    /// </summary>
    IASetVertexBuffersInvalidBuffer = D3D11_MESSAGE_ID_IASETVERTEXBUFFERS_INVALIDBUFFER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_IASETVERTEXBUFFERS_OFFSET_TOO_LARGE)</para>
    /// </summary>
    DeviceIASetVertexBuffersOffsetTooLarge = D3D11_MESSAGE_ID_DEVICE_IASETVERTEXBUFFERS_OFFSET_TOO_LARGE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_IASETVERTEXBUFFERS_BUFFERS_EMPTY)</para>
    /// </summary>
    DeviceIASetVertexBuffersBuffersEmpty = D3D11_MESSAGE_ID_DEVICE_IASETVERTEXBUFFERS_BUFFERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_IASETINDEXBUFFER_INVALIDBUFFER)</para>
    /// </summary>
    IASetIndexBufferInvalidBuffer = D3D11_MESSAGE_ID_IASETINDEXBUFFER_INVALIDBUFFER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_IASETINDEXBUFFER_FORMAT_INVALID)</para>
    /// </summary>
    DeviceIASetIndexBufferFormatInvalid = D3D11_MESSAGE_ID_DEVICE_IASETINDEXBUFFER_FORMAT_INVALID,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_IASETINDEXBUFFER_OFFSET_TOO_LARGE)</para>
    /// </summary>
    DeviceIASetIndexBufferOffsetTooLarge = D3D11_MESSAGE_ID_DEVICE_IASETINDEXBUFFER_OFFSET_TOO_LARGE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_IASETINDEXBUFFER_OFFSET_UNALIGNED)</para>
    /// </summary>
    DeviceIASetIndexBufferOffsetUnaligned = D3D11_MESSAGE_ID_DEVICE_IASETINDEXBUFFER_OFFSET_UNALIGNED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_VSSETSHADERRESOURCES_VIEWS_EMPTY)</para>
    /// </summary>
    DeviceVSSetShaderResourcesViewsEmpty = D3D11_MESSAGE_ID_DEVICE_VSSETSHADERRESOURCES_VIEWS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_VSSETCONSTANTBUFFERS_INVALIDBUFFER)</para>
    /// </summary>
    VSSetConstantBuffersInvalidBuffer = D3D11_MESSAGE_ID_VSSETCONSTANTBUFFERS_INVALIDBUFFER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_VSSETCONSTANTBUFFERS_BUFFERS_EMPTY)</para>
    /// </summary>
    DeviceVSSetConstantBuffersBuffersEmpty = D3D11_MESSAGE_ID_DEVICE_VSSETCONSTANTBUFFERS_BUFFERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_VSSETSAMPLERS_SAMPLERS_EMPTY)</para>
    /// </summary>
    DeviceVSSetSamplersSamplersEmpty = D3D11_MESSAGE_ID_DEVICE_VSSETSAMPLERS_SAMPLERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_GSSETSHADERRESOURCES_VIEWS_EMPTY)</para>
    /// </summary>
    DeviceGSSetShaderResourcesViewsEmpty = D3D11_MESSAGE_ID_DEVICE_GSSETSHADERRESOURCES_VIEWS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_GSSETCONSTANTBUFFERS_INVALIDBUFFER)</para>
    /// </summary>
    GSSetConstantBuffersInvalidBuffer = D3D11_MESSAGE_ID_GSSETCONSTANTBUFFERS_INVALIDBUFFER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_GSSETCONSTANTBUFFERS_BUFFERS_EMPTY)</para>
    /// </summary>
    DeviceGSSetConstantBuffersBuffersEmpty = D3D11_MESSAGE_ID_DEVICE_GSSETCONSTANTBUFFERS_BUFFERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_GSSETSAMPLERS_SAMPLERS_EMPTY)</para>
    /// </summary>
    DeviceGSSetSamplersSamplersEmpty = D3D11_MESSAGE_ID_DEVICE_GSSETSAMPLERS_SAMPLERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SOSETTARGETS_INVALIDBUFFER)</para>
    /// </summary>
    SoSetTargetsInvalidBuffer = D3D11_MESSAGE_ID_SOSETTARGETS_INVALIDBUFFER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_SOSETTARGETS_OFFSET_UNALIGNED)</para>
    /// </summary>
    DeviceSoSetTargetsOffsetUnaligned = D3D11_MESSAGE_ID_DEVICE_SOSETTARGETS_OFFSET_UNALIGNED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_PSSETSHADERRESOURCES_VIEWS_EMPTY)</para>
    /// </summary>
    DevicePSSetShaderResourcesViewsEmpty = D3D11_MESSAGE_ID_DEVICE_PSSETSHADERRESOURCES_VIEWS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_PSSETCONSTANTBUFFERS_INVALIDBUFFER)</para>
    /// </summary>
    PSSetConstantBuffersInvalidBuffer = D3D11_MESSAGE_ID_PSSETCONSTANTBUFFERS_INVALIDBUFFER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_PSSETCONSTANTBUFFERS_BUFFERS_EMPTY)</para>
    /// </summary>
    DevicePSSetConstantBuffersBuffersEmpty = D3D11_MESSAGE_ID_DEVICE_PSSETCONSTANTBUFFERS_BUFFERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_PSSETSAMPLERS_SAMPLERS_EMPTY)</para>
    /// </summary>
    DevicePSSetSamplersSamplersEmpty = D3D11_MESSAGE_ID_DEVICE_PSSETSAMPLERS_SAMPLERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_RSSETVIEWPORTS_INVALIDVIEWPORT)</para>
    /// </summary>
    DeviceRSSetViewportsInvalidViewport = D3D11_MESSAGE_ID_DEVICE_RSSETVIEWPORTS_INVALIDVIEWPORT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_RSSETSCISSORRECTS_INVALIDSCISSOR)</para>
    /// </summary>
    DeviceRSSetScissorRectsInvalidScissor = D3D11_MESSAGE_ID_DEVICE_RSSETSCISSORRECTS_INVALIDSCISSOR,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CLEARRENDERTARGETVIEW_DENORMFLUSH)</para>
    /// </summary>
    ClearRenderTargetViewDenormFlush = D3D11_MESSAGE_ID_CLEARRENDERTARGETVIEW_DENORMFLUSH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CLEARDEPTHSTENCILVIEW_DENORMFLUSH)</para>
    /// </summary>
    ClearDepthStencilViewDenormFlush = D3D11_MESSAGE_ID_CLEARDEPTHSTENCILVIEW_DENORMFLUSH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CLEARDEPTHSTENCILVIEW_INVALID)</para>
    /// </summary>
    ClearDepthStencilViewInvalid = D3D11_MESSAGE_ID_CLEARDEPTHSTENCILVIEW_INVALID,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_IAGETVERTEXBUFFERS_BUFFERS_EMPTY)</para>
    /// </summary>
    DeviceIAGetVertexBuffersBuffersEmpty = D3D11_MESSAGE_ID_DEVICE_IAGETVERTEXBUFFERS_BUFFERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_VSGETSHADERRESOURCES_VIEWS_EMPTY)</para>
    /// </summary>
    DeviceVSGetShaderResourcesViewsEmpty = D3D11_MESSAGE_ID_DEVICE_VSGETSHADERRESOURCES_VIEWS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_VSGETCONSTANTBUFFERS_BUFFERS_EMPTY)</para>
    /// </summary>
    DeviceVSGetConstantBuffersBuffersEmpty = D3D11_MESSAGE_ID_DEVICE_VSGETCONSTANTBUFFERS_BUFFERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_VSGETSAMPLERS_SAMPLERS_EMPTY)</para>
    /// </summary>
    DeviceVSGetSamplersSamplersEmpty = D3D11_MESSAGE_ID_DEVICE_VSGETSAMPLERS_SAMPLERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_GSGETSHADERRESOURCES_VIEWS_EMPTY)</para>
    /// </summary>
    DeviceGSGetShaderResourcesViewsEmpty = D3D11_MESSAGE_ID_DEVICE_GSGETSHADERRESOURCES_VIEWS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_GSGETCONSTANTBUFFERS_BUFFERS_EMPTY)</para>
    /// </summary>
    DeviceGSGetConstantBuffersBuffersEmpty = D3D11_MESSAGE_ID_DEVICE_GSGETCONSTANTBUFFERS_BUFFERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_GSGETSAMPLERS_SAMPLERS_EMPTY)</para>
    /// </summary>
    DeviceGSGetSamplersSamplersEmpty = D3D11_MESSAGE_ID_DEVICE_GSGETSAMPLERS_SAMPLERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_SOGETTARGETS_BUFFERS_EMPTY)</para>
    /// </summary>
    DeviceSoGetTargetsBuffersEmpty = D3D11_MESSAGE_ID_DEVICE_SOGETTARGETS_BUFFERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_PSGETSHADERRESOURCES_VIEWS_EMPTY)</para>
    /// </summary>
    DevicePSGetShaderResourcesViewsEmpty = D3D11_MESSAGE_ID_DEVICE_PSGETSHADERRESOURCES_VIEWS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_PSGETCONSTANTBUFFERS_BUFFERS_EMPTY)</para>
    /// </summary>
    DevicePSGetConstantBuffersBuffersEmpty = D3D11_MESSAGE_ID_DEVICE_PSGETCONSTANTBUFFERS_BUFFERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_PSGETSAMPLERS_SAMPLERS_EMPTY)</para>
    /// </summary>
    DevicePSGetSamplersSamplersEmpty = D3D11_MESSAGE_ID_DEVICE_PSGETSAMPLERS_SAMPLERS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_RSGETVIEWPORTS_VIEWPORTS_EMPTY)</para>
    /// </summary>
    DeviceRSGetViewportsViewportsEmpty = D3D11_MESSAGE_ID_DEVICE_RSGETVIEWPORTS_VIEWPORTS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_RSGETSCISSORRECTS_RECTS_EMPTY)</para>
    /// </summary>
    DeviceRSGetScissorRectsRectsEmpty = D3D11_MESSAGE_ID_DEVICE_RSGETSCISSORRECTS_RECTS_EMPTY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_GENERATEMIPS_RESOURCE_INVALID)</para>
    /// </summary>
    DeviceGenerateMipsResourceInvalid = D3D11_MESSAGE_ID_DEVICE_GENERATEMIPS_RESOURCE_INVALID,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDDESTINATIONSUBRESOURCE)</para>
    /// </summary>
    CopySubresourceRegionInvalidDestinationSubresource = D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDDESTINATIONSUBRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDSOURCESUBRESOURCE)</para>
    /// </summary>
    CopySubresourceRegionInvalidSourceSubresource = D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDSOURCESUBRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDSOURCEBOX)</para>
    /// </summary>
    CopySubresourceRegionInvalidSourceBox = D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDSOURCEBOX,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDSOURCE)</para>
    /// </summary>
    CopySubresourceRegionInvalidSource = D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDSOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDDESTINATIONSTATE)</para>
    /// </summary>
    CopySubresourceRegionInvalidDestinationState = D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDDESTINATIONSTATE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDSOURCESTATE)</para>
    /// </summary>
    CopySubresourceRegionInvalidSourceState = D3D11_MESSAGE_ID_COPYSUBRESOURCEREGION_INVALIDSOURCESTATE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_COPYRESOURCE_INVALIDSOURCE)</para>
    /// </summary>
    CopyResourceInvalidSource = D3D11_MESSAGE_ID_COPYRESOURCE_INVALIDSOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_COPYRESOURCE_INVALIDDESTINATIONSTATE)</para>
    /// </summary>
    CopyResourceInvalidDestinationState = D3D11_MESSAGE_ID_COPYRESOURCE_INVALIDDESTINATIONSTATE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_COPYRESOURCE_INVALIDSOURCESTATE)</para>
    /// </summary>
    CopyResourceInvalidSourceState = D3D11_MESSAGE_ID_COPYRESOURCE_INVALIDSOURCESTATE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_UPDATESUBRESOURCE_INVALIDDESTINATIONSUBRESOURCE)</para>
    /// </summary>
    UpdateSubresourceInvalidDestinationSubresource = D3D11_MESSAGE_ID_UPDATESUBRESOURCE_INVALIDDESTINATIONSUBRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_UPDATESUBRESOURCE_INVALIDDESTINATIONBOX)</para>
    /// </summary>
    UpdateSubresourceInvalidDestinationBox = D3D11_MESSAGE_ID_UPDATESUBRESOURCE_INVALIDDESTINATIONBOX,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_UPDATESUBRESOURCE_INVALIDDESTINATIONSTATE)</para>
    /// </summary>
    UpdateSubresourceInvalidDestinationState = D3D11_MESSAGE_ID_UPDATESUBRESOURCE_INVALIDDESTINATIONSTATE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_RESOLVESUBRESOURCE_DESTINATION_INVALID)</para>
    /// </summary>
    DeviceResolveSubresourceDestinationInvalid = D3D11_MESSAGE_ID_DEVICE_RESOLVESUBRESOURCE_DESTINATION_INVALID,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_RESOLVESUBRESOURCE_DESTINATION_SUBRESOURCE_INVALID)</para>
    /// </summary>
    DeviceResolveSubresourceDestinationSubresourceInvalid = D3D11_MESSAGE_ID_DEVICE_RESOLVESUBRESOURCE_DESTINATION_SUBRESOURCE_INVALID,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_RESOLVESUBRESOURCE_SOURCE_INVALID)</para>
    /// </summary>
    DeviceResolveSubresourceSourceInvalid = D3D11_MESSAGE_ID_DEVICE_RESOLVESUBRESOURCE_SOURCE_INVALID,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_RESOLVESUBRESOURCE_SOURCE_SUBRESOURCE_INVALID)</para>
    /// </summary>
    DeviceResolveSubresourceSourceSubresourceInvalid = D3D11_MESSAGE_ID_DEVICE_RESOLVESUBRESOURCE_SOURCE_SUBRESOURCE_INVALID,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_RESOLVESUBRESOURCE_FORMAT_INVALID)</para>
    /// </summary>
    DeviceResolveSubresourceFormatInvalid = D3D11_MESSAGE_ID_DEVICE_RESOLVESUBRESOURCE_FORMAT_INVALID,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_BUFFER_MAP_INVALIDMAPTYPE)</para>
    /// </summary>
    BufferMapInvalidMapType = D3D11_MESSAGE_ID_BUFFER_MAP_INVALIDMAPTYPE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_BUFFER_MAP_INVALIDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    BufferMapInvalidFlags = D3D11_MESSAGE_ID_BUFFER_MAP_INVALIDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_BUFFER_MAP_ALREADYMAPPED)</para>
    /// </summary>
    BufferMapAlreadyMapped = D3D11_MESSAGE_ID_BUFFER_MAP_ALREADYMAPPED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_BUFFER_MAP_DEVICEREMOVED_RETURN)</para>
    /// </summary>
    BufferMapDeviceRemovedReturn = D3D11_MESSAGE_ID_BUFFER_MAP_DEVICEREMOVED_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_BUFFER_UNMAP_NOTMAPPED)</para>
    /// </summary>
    BufferUnmapNotMapped = D3D11_MESSAGE_ID_BUFFER_UNMAP_NOTMAPPED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE1D_MAP_INVALIDMAPTYPE)</para>
    /// </summary>
    Texture1DMapInvalidMapType = D3D11_MESSAGE_ID_TEXTURE1D_MAP_INVALIDMAPTYPE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE1D_MAP_INVALIDSUBRESOURCE)</para>
    /// </summary>
    Texture1DMapInvalidSubresource = D3D11_MESSAGE_ID_TEXTURE1D_MAP_INVALIDSUBRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE1D_MAP_INVALIDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    Texture1DMapInvalidFlags = D3D11_MESSAGE_ID_TEXTURE1D_MAP_INVALIDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE1D_MAP_ALREADYMAPPED)</para>
    /// </summary>
    Texture1DMapAlreadyMapped = D3D11_MESSAGE_ID_TEXTURE1D_MAP_ALREADYMAPPED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE1D_MAP_DEVICEREMOVED_RETURN)</para>
    /// </summary>
    Texture1DMapDeviceRemovedReturn = D3D11_MESSAGE_ID_TEXTURE1D_MAP_DEVICEREMOVED_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE1D_UNMAP_INVALIDSUBRESOURCE)</para>
    /// </summary>
    Texture1DUnmapInvalidSubresource = D3D11_MESSAGE_ID_TEXTURE1D_UNMAP_INVALIDSUBRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE1D_UNMAP_NOTMAPPED)</para>
    /// </summary>
    Texture1DUnmapNotMapped = D3D11_MESSAGE_ID_TEXTURE1D_UNMAP_NOTMAPPED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE2D_MAP_INVALIDMAPTYPE)</para>
    /// </summary>
    Texture2DMapInvalidMapType = D3D11_MESSAGE_ID_TEXTURE2D_MAP_INVALIDMAPTYPE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE2D_MAP_INVALIDSUBRESOURCE)</para>
    /// </summary>
    Texture2DMapInvalidSubresource = D3D11_MESSAGE_ID_TEXTURE2D_MAP_INVALIDSUBRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE2D_MAP_INVALIDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    Texture2DMapInvalidFlags = D3D11_MESSAGE_ID_TEXTURE2D_MAP_INVALIDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE2D_MAP_ALREADYMAPPED)</para>
    /// </summary>
    Texture2DMapAlreadyMapped = D3D11_MESSAGE_ID_TEXTURE2D_MAP_ALREADYMAPPED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE2D_MAP_DEVICEREMOVED_RETURN)</para>
    /// </summary>
    Texture2DMapDeviceRemovedReturn = D3D11_MESSAGE_ID_TEXTURE2D_MAP_DEVICEREMOVED_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE2D_UNMAP_INVALIDSUBRESOURCE)</para>
    /// </summary>
    Texture2DUnmapInvalidSubresource = D3D11_MESSAGE_ID_TEXTURE2D_UNMAP_INVALIDSUBRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE2D_UNMAP_NOTMAPPED)</para>
    /// </summary>
    Texture2DUnmapNotMapped = D3D11_MESSAGE_ID_TEXTURE2D_UNMAP_NOTMAPPED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE3D_MAP_INVALIDMAPTYPE)</para>
    /// </summary>
    Texture3DMapInvalidMapType = D3D11_MESSAGE_ID_TEXTURE3D_MAP_INVALIDMAPTYPE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE3D_MAP_INVALIDSUBRESOURCE)</para>
    /// </summary>
    Texture3DMapInvalidSubresource = D3D11_MESSAGE_ID_TEXTURE3D_MAP_INVALIDSUBRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE3D_MAP_INVALIDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    Texture3DMapInvalidFlags = D3D11_MESSAGE_ID_TEXTURE3D_MAP_INVALIDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE3D_MAP_ALREADYMAPPED)</para>
    /// </summary>
    Texture3DMapAlreadyMapped = D3D11_MESSAGE_ID_TEXTURE3D_MAP_ALREADYMAPPED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE3D_MAP_DEVICEREMOVED_RETURN)</para>
    /// </summary>
    Texture3DMapDeviceRemovedReturn = D3D11_MESSAGE_ID_TEXTURE3D_MAP_DEVICEREMOVED_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE3D_UNMAP_INVALIDSUBRESOURCE)</para>
    /// </summary>
    Texture3DUnmapInvalidSubresource = D3D11_MESSAGE_ID_TEXTURE3D_UNMAP_INVALIDSUBRESOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_TEXTURE3D_UNMAP_NOTMAPPED)</para>
    /// </summary>
    Texture3DUnmapNotMapped = D3D11_MESSAGE_ID_TEXTURE3D_UNMAP_NOTMAPPED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CHECKFORMATSUPPORT_FORMAT_DEPRECATED)</para>
    /// </summary>
    CheckFormatSupportFormatDeprecated = D3D11_MESSAGE_ID_CHECKFORMATSUPPORT_FORMAT_DEPRECATED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CHECKMULTISAMPLEQUALITYLEVELS_FORMAT_DEPRECATED)</para>
    /// </summary>
    CheckMultisampleQualityLevelsFormatDeprecated = D3D11_MESSAGE_ID_CHECKMULTISAMPLEQUALITYLEVELS_FORMAT_DEPRECATED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SETEXCEPTIONMODE_UNRECOGNIZEDFLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    SetExceptionModeUnrecognizedFlags = D3D11_MESSAGE_ID_SETEXCEPTIONMODE_UNRECOGNIZEDFLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SETEXCEPTIONMODE_INVALIDARG_RETURN)</para>
    /// </summary>
    SetExceptionModeInvalidArgReturn = D3D11_MESSAGE_ID_SETEXCEPTIONMODE_INVALIDARG_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SETEXCEPTIONMODE_DEVICEREMOVED_RETURN)</para>
    /// </summary>
    SetExceptionModeDeviceRemovedReturn = D3D11_MESSAGE_ID_SETEXCEPTIONMODE_DEVICEREMOVED_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_REF_SIMULATING_INFINITELY_FAST_HARDWARE)</para>
    /// </summary>
    RefSimulatingInfinitelyFastHardware = D3D11_MESSAGE_ID_REF_SIMULATING_INFINITELY_FAST_HARDWARE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_REF_THREADING_MODE)</para>
    /// </summary>
    RefThreadingMode = D3D11_MESSAGE_ID_REF_THREADING_MODE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_REF_UMDRIVER_EXCEPTION)</para>
    /// </summary>
    RefUserModeDriverException = D3D11_MESSAGE_ID_REF_UMDRIVER_EXCEPTION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_REF_KMDRIVER_EXCEPTION)</para>
    /// </summary>
    RefKernelModeDriverException = D3D11_MESSAGE_ID_REF_KMDRIVER_EXCEPTION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_REF_HARDWARE_EXCEPTION)</para>
    /// </summary>
    RefHardwareException = D3D11_MESSAGE_ID_REF_HARDWARE_EXCEPTION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_REF_ACCESSING_INDEXABLE_TEMP_OUT_OF_RANGE)</para>
    /// </summary>
    RefAccessingIndexableTempOutOfRange = D3D11_MESSAGE_ID_REF_ACCESSING_INDEXABLE_TEMP_OUT_OF_RANGE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_REF_PROBLEM_PARSING_SHADER)</para>
    /// </summary>
    RefProblemParsingShader = D3D11_MESSAGE_ID_REF_PROBLEM_PARSING_SHADER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_REF_OUT_OF_MEMORY)</para>
    /// </summary>
    RefOutOfMemory = D3D11_MESSAGE_ID_REF_OUT_OF_MEMORY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_REF_INFO)</para>
    /// </summary>
    RefInfo = D3D11_MESSAGE_ID_REF_INFO,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEXPOS_OVERFLOW)</para>
    /// </summary>
    DeviceDrawVertexPosOverflow = D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEXPOS_OVERFLOW,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAWINDEXED_INDEXPOS_OVERFLOW)</para>
    /// </summary>
    DeviceDrawIndexedIndexPosOverflow = D3D11_MESSAGE_ID_DEVICE_DRAWINDEXED_INDEXPOS_OVERFLOW,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAWINSTANCED_VERTEXPOS_OVERFLOW)</para>
    /// </summary>
    DeviceDrawInstancedVertexPosOverflow = D3D11_MESSAGE_ID_DEVICE_DRAWINSTANCED_VERTEXPOS_OVERFLOW,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAWINSTANCED_INSTANCEPOS_OVERFLOW)</para>
    /// </summary>
    DeviceDrawInstancedInstancePosOverflow = D3D11_MESSAGE_ID_DEVICE_DRAWINSTANCED_INSTANCEPOS_OVERFLOW,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAWINDEXEDINSTANCED_INSTANCEPOS_OVERFLOW)</para>
    /// </summary>
    DeviceDrawIndexedInstancedInstancePosOverflow = D3D11_MESSAGE_ID_DEVICE_DRAWINDEXEDINSTANCED_INSTANCEPOS_OVERFLOW,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAWINDEXEDINSTANCED_INDEXPOS_OVERFLOW)</para>
    /// </summary>
    DeviceDrawIndexedInstancedIndexPosOverflow = D3D11_MESSAGE_ID_DEVICE_DRAWINDEXEDINSTANCED_INDEXPOS_OVERFLOW,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_SHADER_NOT_SET)</para>
    /// </summary>
    DeviceDrawVertexShaderNotSet = D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_SHADER_NOT_SET,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_SEMANTICNAME_NOT_FOUND)</para>
    /// </summary>
    DeviceShaderLinkageSemanticNameNotFound = D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_SEMANTICNAME_NOT_FOUND,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_REGISTERINDEX)</para>
    /// </summary>
    DeviceShaderLinkageRegisterIndex = D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_REGISTERINDEX,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_COMPONENTTYPE)</para>
    /// </summary>
    DeviceShaderLinkageComponentType = D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_COMPONENTTYPE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_REGISTERMASK)</para>
    /// </summary>
    DeviceShaderLinkageRegisterMask = D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_REGISTERMASK,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_SYSTEMVALUE)</para>
    /// </summary>
    DeviceShaderLinkageSystemValue = D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_SYSTEMVALUE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_NEVERWRITTEN_ALWAYSREADS)</para>
    /// </summary>
    DeviceShaderLinkageNeverWrittenAlwaysReads = D3D11_MESSAGE_ID_DEVICE_SHADER_LINKAGE_NEVERWRITTEN_ALWAYSREADS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_BUFFER_NOT_SET)</para>
    /// </summary>
    DeviceDrawVertexBufferNotSet = D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_BUFFER_NOT_SET,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_INPUTLAYOUT_NOT_SET)</para>
    /// </summary>
    DeviceDrawInputLayoutNotSet = D3D11_MESSAGE_ID_DEVICE_DRAW_INPUTLAYOUT_NOT_SET,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_CONSTANT_BUFFER_NOT_SET)</para>
    /// </summary>
    DeviceDrawConstantBufferNotSet = D3D11_MESSAGE_ID_DEVICE_DRAW_CONSTANT_BUFFER_NOT_SET,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_CONSTANT_BUFFER_TOO_SMALL)</para>
    /// </summary>
    DeviceDrawConstantBufferTooSmall = D3D11_MESSAGE_ID_DEVICE_DRAW_CONSTANT_BUFFER_TOO_SMALL,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_SAMPLER_NOT_SET)</para>
    /// </summary>
    DeviceDrawSamplerNotSet = D3D11_MESSAGE_ID_DEVICE_DRAW_SAMPLER_NOT_SET,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_SHADERRESOURCEVIEW_NOT_SET)</para>
    /// </summary>
    DeviceDrawShaderResourceViewNotSet = D3D11_MESSAGE_ID_DEVICE_DRAW_SHADERRESOURCEVIEW_NOT_SET,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_VIEW_DIMENSION_MISMATCH)</para>
    /// </summary>
    DeviceDrawViewDimensionMismatch = D3D11_MESSAGE_ID_DEVICE_DRAW_VIEW_DIMENSION_MISMATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_BUFFER_STRIDE_TOO_SMALL)</para>
    /// </summary>
    DeviceDrawVertexBufferStrideTooSmall = D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_BUFFER_STRIDE_TOO_SMALL,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_BUFFER_TOO_SMALL)</para>
    /// </summary>
    DeviceDrawVertexBufferTooSmall = D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_BUFFER_TOO_SMALL,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_INDEX_BUFFER_NOT_SET)</para>
    /// </summary>
    DeviceDrawIndexBufferNotSet = D3D11_MESSAGE_ID_DEVICE_DRAW_INDEX_BUFFER_NOT_SET,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_INDEX_BUFFER_FORMAT_INVALID)</para>
    /// </summary>
    DeviceDrawIndexBufferFormatInvalid = D3D11_MESSAGE_ID_DEVICE_DRAW_INDEX_BUFFER_FORMAT_INVALID,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_INDEX_BUFFER_TOO_SMALL)</para>
    /// </summary>
    DeviceDrawIndexBufferTooSmall = D3D11_MESSAGE_ID_DEVICE_DRAW_INDEX_BUFFER_TOO_SMALL,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_GS_INPUT_PRIMITIVE_MISMATCH)</para>
    /// </summary>
    DeviceDrawGSInputPrimitiveMismatch = D3D11_MESSAGE_ID_DEVICE_DRAW_GS_INPUT_PRIMITIVE_MISMATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_RETURN_TYPE_MISMATCH)</para>
    /// </summary>
    DeviceDrawResourceReturnTypeMismatch = D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_RETURN_TYPE_MISMATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_POSITION_NOT_PRESENT)</para>
    /// </summary>
    DeviceDrawPositionNotPresent = D3D11_MESSAGE_ID_DEVICE_DRAW_POSITION_NOT_PRESENT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_OUTPUT_STREAM_NOT_SET)</para>
    /// </summary>
    DeviceDrawOutputStreamNotSet = D3D11_MESSAGE_ID_DEVICE_DRAW_OUTPUT_STREAM_NOT_SET,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_BOUND_RESOURCE_MAPPED)</para>
    /// </summary>
    DeviceDrawBoundResourceMapped = D3D11_MESSAGE_ID_DEVICE_DRAW_BOUND_RESOURCE_MAPPED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_INVALID_PRIMITIVETOPOLOGY)</para>
    /// </summary>
    DeviceDrawInvalidPrimitiveTopology = D3D11_MESSAGE_ID_DEVICE_DRAW_INVALID_PRIMITIVETOPOLOGY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_OFFSET_UNALIGNED)</para>
    /// </summary>
    DeviceDrawVertexOffsetUnaligned = D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_OFFSET_UNALIGNED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_STRIDE_UNALIGNED)</para>
    /// </summary>
    DeviceDrawVertexStrideUnaligned = D3D11_MESSAGE_ID_DEVICE_DRAW_VERTEX_STRIDE_UNALIGNED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_INDEX_OFFSET_UNALIGNED)</para>
    /// </summary>
    DeviceDrawIndexOffsetUnaligned = D3D11_MESSAGE_ID_DEVICE_DRAW_INDEX_OFFSET_UNALIGNED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_OUTPUT_STREAM_OFFSET_UNALIGNED)</para>
    /// </summary>
    DeviceDrawOutputStreamOffsetUnaligned = D3D11_MESSAGE_ID_DEVICE_DRAW_OUTPUT_STREAM_OFFSET_UNALIGNED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_FORMAT_LD_UNSUPPORTED)</para>
    /// </summary>
    DeviceDrawResourceFormatLDUnsupported = D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_FORMAT_LD_UNSUPPORTED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_FORMAT_SAMPLE_UNSUPPORTED)</para>
    /// </summary>
    DeviceDrawResourceFormatSampleUnsupported = D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_FORMAT_SAMPLE_UNSUPPORTED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_FORMAT_SAMPLE_C_UNSUPPORTED)</para>
    /// </summary>
    DeviceDrawResourceFormatSampleCUnsupported = D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_FORMAT_SAMPLE_C_UNSUPPORTED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_MULTISAMPLE_UNSUPPORTED)</para>
    /// </summary>
    DeviceDrawResourceMultisampleUnsupported = D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_MULTISAMPLE_UNSUPPORTED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_SO_TARGETS_BOUND_WITHOUT_SOURCE)</para>
    /// </summary>
    DeviceDrawSoTargetsBoundWithoutSource = D3D11_MESSAGE_ID_DEVICE_DRAW_SO_TARGETS_BOUND_WITHOUT_SOURCE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_SO_STRIDE_LARGER_THAN_BUFFER)</para>
    /// </summary>
    DeviceDrawSoStrideLargerThanBuffer = D3D11_MESSAGE_ID_DEVICE_DRAW_SO_STRIDE_LARGER_THAN_BUFFER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_OM_RENDER_TARGET_DOES_NOT_SUPPORT_BLENDING)</para>
    /// </summary>
    DeviceDrawOMRenderTargetDoesNotSupportBlending = D3D11_MESSAGE_ID_DEVICE_DRAW_OM_RENDER_TARGET_DOES_NOT_SUPPORT_BLENDING,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_OM_DUAL_SOURCE_BLENDING_CAN_ONLY_HAVE_RENDER_TARGET_0)</para>
    /// </summary>
    DeviceDrawOMDualSourceBlendingCanOnlyHaveRenderTargetZero = D3D11_MESSAGE_ID_DEVICE_DRAW_OM_DUAL_SOURCE_BLENDING_CAN_ONLY_HAVE_RENDER_TARGET_0,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_REMOVAL_PROCESS_AT_FAULT)</para>
    /// </summary>
    DeviceRemovalProcessAtFault = D3D11_MESSAGE_ID_DEVICE_REMOVAL_PROCESS_AT_FAULT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_REMOVAL_PROCESS_POSSIBLY_AT_FAULT)</para>
    /// </summary>
    DeviceRemovalProcessPossiblyAtFault = D3D11_MESSAGE_ID_DEVICE_REMOVAL_PROCESS_POSSIBLY_AT_FAULT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_REMOVAL_PROCESS_NOT_AT_FAULT)</para>
    /// </summary>
    DeviceRemovalProcessNotAtFault = D3D11_MESSAGE_ID_DEVICE_REMOVAL_PROCESS_NOT_AT_FAULT,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_OPEN_SHARED_RESOURCE_INVALIDARG_RETURN)</para>
    /// </summary>
    DeviceOpenSharedResourceInvalidArgReturn = D3D11_MESSAGE_ID_DEVICE_OPEN_SHARED_RESOURCE_INVALIDARG_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_OPEN_SHARED_RESOURCE_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    DeviceOpenSharedResourceOutOfMemoryReturn = D3D11_MESSAGE_ID_DEVICE_OPEN_SHARED_RESOURCE_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_OPEN_SHARED_RESOURCE_BADINTERFACE_RETURN)</para>
    /// </summary>
    DeviceOpenSharedResourceBadInterfaceReturn = D3D11_MESSAGE_ID_DEVICE_OPEN_SHARED_RESOURCE_BADINTERFACE_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_VIEWPORT_NOT_SET)</para>
    /// </summary>
    DeviceDrawViewportNotSet = D3D11_MESSAGE_ID_DEVICE_DRAW_VIEWPORT_NOT_SET,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_TRAILING_DIGIT_IN_SEMANTIC)</para>
    /// </summary>
    CreateInputLayoutTrailingDigitInSemantic = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_TRAILING_DIGIT_IN_SEMANTIC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_TRAILING_DIGIT_IN_SEMANTIC)</para>
    /// </summary>
    CreateGeometryShaderWithStreamOutputTrailingDigitInSemantic = D3D11_MESSAGE_ID_CREATEGEOMETRYSHADERWITHSTREAMOUTPUT_TRAILING_DIGIT_IN_SEMANTIC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_RSSETVIEWPORTS_DENORMFLUSH)</para>
    /// </summary>
    DeviceRSSetViewportsDenormFlush = D3D11_MESSAGE_ID_DEVICE_RSSETVIEWPORTS_DENORMFLUSH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_OMSETRENDERTARGETS_INVALIDVIEW)</para>
    /// </summary>
    OMSetRenderTargetsInvalidView = D3D11_MESSAGE_ID_OMSETRENDERTARGETS_INVALIDVIEW,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_SETTEXTFILTERSIZE_INVALIDDIMENSIONS)</para>
    /// </summary>
    DeviceSetTextFilterSizeInvalidDimensions = D3D11_MESSAGE_ID_DEVICE_SETTEXTFILTERSIZE_INVALIDDIMENSIONS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_SAMPLER_MISMATCH)</para>
    /// </summary>
    DeviceDrawSamplerMismatch = D3D11_MESSAGE_ID_DEVICE_DRAW_SAMPLER_MISMATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_TYPE_MISMATCH)</para>
    /// </summary>
    CreateInputLayoutTypeMismatch = D3D11_MESSAGE_ID_CREATEINPUTLAYOUT_TYPE_MISMATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_BLENDSTATE_GETDESC_LEGACY)</para>
    /// </summary>
    BlendStateGetDescLegacy = D3D11_MESSAGE_ID_BLENDSTATE_GETDESC_LEGACY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SHADERRESOURCEVIEW_GETDESC_LEGACY)</para>
    /// </summary>
    ShaderResourceViewGetDescLegacy = D3D11_MESSAGE_ID_SHADERRESOURCEVIEW_GETDESC_LEGACY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEQUERY_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    CreateQueryOutOfMemoryReturn = D3D11_MESSAGE_ID_CREATEQUERY_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATEPREDICATE_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    CreatePredicateOutOfMemoryReturn = D3D11_MESSAGE_ID_CREATEPREDICATE_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATECOUNTER_OUTOFRANGE_COUNTER)</para>
    /// </summary>
    CreateCounterOutOfRangeCounter = D3D11_MESSAGE_ID_CREATECOUNTER_OUTOFRANGE_COUNTER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATECOUNTER_SIMULTANEOUS_ACTIVE_COUNTERS_EXHAUSTED)</para>
    /// </summary>
    CreateCounterSimultaneousActiveCountersExhausted = D3D11_MESSAGE_ID_CREATECOUNTER_SIMULTANEOUS_ACTIVE_COUNTERS_EXHAUSTED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATECOUNTER_UNSUPPORTED_WELLKNOWN_COUNTER)</para>
    /// </summary>
    CreateCounterUnsupportedWellKnownCounter = D3D11_MESSAGE_ID_CREATECOUNTER_UNSUPPORTED_WELLKNOWN_COUNTER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATECOUNTER_OUTOFMEMORY_RETURN)</para>
    /// </summary>
    CreateCounterOutOfMemoryReturn = D3D11_MESSAGE_ID_CREATECOUNTER_OUTOFMEMORY_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATECOUNTER_NONEXCLUSIVE_RETURN)</para>
    /// </summary>
    CreateCounterNonexclusiveReturn = D3D11_MESSAGE_ID_CREATECOUNTER_NONEXCLUSIVE_RETURN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CREATECOUNTER_NULLDESC)</para>
    /// </summary>
    CreateCounterNullDesc = D3D11_MESSAGE_ID_CREATECOUNTER_NULLDESC,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CHECKCOUNTER_OUTOFRANGE_COUNTER)</para>
    /// </summary>
    CheckCounterOutOfRangeCounter = D3D11_MESSAGE_ID_CHECKCOUNTER_OUTOFRANGE_COUNTER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_CHECKCOUNTER_UNSUPPORTED_WELLKNOWN_COUNTER)</para>
    /// </summary>
    CheckCounterUnsupportedWellKnownCounter = D3D11_MESSAGE_ID_CHECKCOUNTER_UNSUPPORTED_WELLKNOWN_COUNTER,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_SETPREDICATION_INVALID_PREDICATE_STATE)</para>
    /// </summary>
    SetPredicationInvalidPredicateState = D3D11_MESSAGE_ID_SETPREDICATION_INVALID_PREDICATE_STATE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_QUERY_BEGIN_UNSUPPORTED)</para>
    /// </summary>
    QueryBeginUnsupported = D3D11_MESSAGE_ID_QUERY_BEGIN_UNSUPPORTED,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_PREDICATE_BEGIN_DURING_PREDICATION)</para>
    /// </summary>
    PredicateBeginDuringPredication = D3D11_MESSAGE_ID_PREDICATE_BEGIN_DURING_PREDICATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_QUERY_BEGIN_DUPLICATE)</para>
    /// </summary>
    QueryBeginDuplicate = D3D11_MESSAGE_ID_QUERY_BEGIN_DUPLICATE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_QUERY_BEGIN_ABANDONING_PREVIOUS_RESULTS)</para>
    /// </summary>
    QueryBeginAbandoningPreviousResults = D3D11_MESSAGE_ID_QUERY_BEGIN_ABANDONING_PREVIOUS_RESULTS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_PREDICATE_END_DURING_PREDICATION)</para>
    /// </summary>
    PredicateEndDuringPredication = D3D11_MESSAGE_ID_PREDICATE_END_DURING_PREDICATION,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_QUERY_END_ABANDONING_PREVIOUS_RESULTS)</para>
    /// </summary>
    QueryEndAbandoningPreviousResults = D3D11_MESSAGE_ID_QUERY_END_ABANDONING_PREVIOUS_RESULTS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_QUERY_END_WITHOUT_BEGIN)</para>
    /// </summary>
    QueryEndWithoutBegin = D3D11_MESSAGE_ID_QUERY_END_WITHOUT_BEGIN,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_QUERY_GETDATA_INVALID_DATASIZE)</para>
    /// </summary>
    QueryGetDataInvalidDataSize = D3D11_MESSAGE_ID_QUERY_GETDATA_INVALID_DATASIZE,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_QUERY_GETDATA_INVALID_FLAGS)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags")
    QueryGetDataInvalidFlags = D3D11_MESSAGE_ID_QUERY_GETDATA_INVALID_FLAGS,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_QUERY_GETDATA_INVALID_CALL)</para>
    /// </summary>
    QueryGetDataInvalidCall = D3D11_MESSAGE_ID_QUERY_GETDATA_INVALID_CALL,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_PS_OUTPUT_TYPE_MISMATCH)</para>
    /// </summary>
    DeviceDrawPSOutputTypeMismatch = D3D11_MESSAGE_ID_DEVICE_DRAW_PS_OUTPUT_TYPE_MISMATCH,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_FORMAT_GATHER_UNSUPPORTED)</para>
    /// </summary>
    DeviceDrawResourceFormatGatherUnsupported = D3D11_MESSAGE_ID_DEVICE_DRAW_RESOURCE_FORMAT_GATHER_UNSUPPORTED,
};
/// <summary>
/// Debug message severity levels for an information queue.
/// <para>(Also see DirectX SDK: D3D11_MESSAGE_SEVERITY)</para>
/// </summary>
public enum class MessageSeverity : Int32
{
    /// <summary>
    /// Defines some type of corruption which has occured.
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_SEVERITY_CORRUPTION)</para>
    /// </summary>
    Corruption = D3D11_MESSAGE_SEVERITY_CORRUPTION,
    /// <summary>
    /// Defines an error message.
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_SEVERITY_ERROR)</para>
    /// </summary>
    Error = D3D11_MESSAGE_SEVERITY_ERROR,
    /// <summary>
    /// Defines a warning message.
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_SEVERITY_WARNING)</para>
    /// </summary>
    Warning = D3D11_MESSAGE_SEVERITY_WARNING,
    /// <summary>
    /// Defines an information message.
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE_SEVERITY_INFO)</para>
    /// </summary>
    Info = D3D11_MESSAGE_SEVERITY_INFO,
};
/// <summary>
/// How the pipeline interprets vertex data that is bound to the input-assembler stage. These primitive types determine how the vertex data is rendered on screen.
/// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY)</para>
/// </summary>
CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1712:DoNotPrefixEnumValuesWithTypeName")
public enum class PrimitiveTopology : Int32
{
    /// <summary>
    /// The IA stage has not been initialized with a primitive topology. The IA stage will not function properly unless a primitive topology is defined.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_UNDEFINED)</para>
    /// </summary>
    Undefined = D3D11_PRIMITIVE_TOPOLOGY_UNDEFINED,
    /// <summary>
    /// Interpret the vertex data as a collection of points.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_POINTLIST)</para>
    /// </summary>
    PointList = D3D11_PRIMITIVE_TOPOLOGY_POINTLIST,
    /// <summary>
    /// Interpret the vertex data as a collection of lines.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_LINELIST)</para>
    /// </summary>
    LineList = D3D11_PRIMITIVE_TOPOLOGY_LINELIST,
    /// <summary>
    /// Interpret the vertex data as a line strip.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP)</para>
    /// </summary>
    LineStrip = D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP,
    /// <summary>
    /// Interpret the vertex data as a collection of triangles.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST)</para>
    /// </summary>
    TriangleList = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST,
    /// <summary>
    /// Interpret the vertex data as a triangle strip.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP)</para>
    /// </summary>
    TriangleStrip = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP,
    /// <summary>
    /// Interpret the vertex data as collection of lines with adjacency data.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_LINELIST_ADJ)</para>
    /// </summary>
    LineListAdjacency = D3D11_PRIMITIVE_TOPOLOGY_LINELIST_ADJ,
    /// <summary>
    /// Interpret the vertex data as line strip with adjacency data.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP_ADJ)</para>
    /// </summary>
    LineStripAdjacency = D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP_ADJ,
    /// <summary>
    /// Interpret the vertex data as collection of triangles with adjacency data.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST_ADJ)</para>
    /// </summary>
    TriangleListAdjacency = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST_ADJ,
    /// <summary>
    /// Interpret the vertex data as triangle strip with adjacency data.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP_ADJ)</para>
    /// </summary>
    TriangleStripAdjacency = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP_ADJ,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_1_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology1AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_1_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_2_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology2AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_2_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_3_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology3AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_3_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_4_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology4AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_4_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_5_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology5AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_5_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_6_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology6AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_6_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_7_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology7AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_7_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_8_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology8AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_8_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_9_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology9AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_9_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_10_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology10AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_10_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_11_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology11AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_11_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_12_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology12AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_12_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_13_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology13AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_13_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_14_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology14AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_14_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_15_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology15AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_15_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_16_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology16AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_16_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_17_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology17AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_17_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_18_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology18AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_18_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_19_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology19AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_19_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_20_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology20AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_20_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_21_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology21AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_21_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_22_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology22AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_22_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_23_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology23AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_23_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_24_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology24AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_24_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_25_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology25AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_25_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_26_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology26AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_26_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_27_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology27AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_27_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_28_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology28AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_28_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_29_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology29AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_29_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_30_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology30AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_30_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_31_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology31AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_31_CONTROL_POINT_PATCHLIST,
    /// <summary>
    /// Interpret the vertex data as a patch list.
    /// <para>(Also see DirectX SDK: D3D11_PRIMITIVE_TOPOLOGY_32_CONTROL_POINT_PATCHLIST)</para>
    /// </summary>
    PrimitiveTopology32AsControlPointPatchList = D3D11_PRIMITIVE_TOPOLOGY_32_CONTROL_POINT_PATCHLIST,
};
/// <summary>
/// D3DQuery types.
/// <para>(Also see DirectX SDK: D3D11_QUERY)</para>
/// </summary>
public enum class Query : Int32
{
    /// <summary>
    /// Determines whether or not the GPU is finished processing commands. When the GPU is finished processing commands DeviceContext.GetData will return S_OK, and pData will point to a BOOL with a value of TRUE. When using this type of query, DeviceContext.Begin is disabled.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_EVENT)</para>
    /// </summary>
    Event = D3D11_QUERY_EVENT,
    /// <summary>
    /// Get the number of samples that passed the depth and stencil tests in between DeviceContext.Begin and DeviceContext.End. DeviceContext.GetData returns a UINT64. If a depth or stencil test is disabled, then each of those tests will be counted as a pass.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_OCCLUSION)</para>
    /// </summary>
    Occlusion = D3D11_QUERY_OCCLUSION,
    /// <summary>
    /// Get a timestamp value where DeviceContext.GetData returns a UINT64. This kind of query is only useful if two timestamp queries are done in the middle of a TimestampDisjoint query. The difference of two timestamps can be used to determine how many ticks have elapsed, and the TimestampDisjoint query will determine if that difference is a reliable value and also has a value that shows how to convert the number of ticks into seconds. See QueryDataTimestampDisjoint. When using this type of query, DeviceContext.Begin is disabled.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_TIMESTAMP)</para>
    /// </summary>
    Timestamp = D3D11_QUERY_TIMESTAMP,
    /// <summary>
    /// Determines whether or not a Timestamp is returning reliable values, and also gives the frequency of the processor enabling you to convert the number of elapsed ticks into seconds. DeviceContext.GetData will return a QueryDataTimestampDisjoint. This type of query should only be invoked once per frame or less.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_TIMESTAMP_DISJOINT)</para>
    /// </summary>
    TimestampDisjoint = D3D11_QUERY_TIMESTAMP_DISJOINT,
    /// <summary>
    /// Get pipeline statistics, such as the number of pixel shader invocations in between DeviceContext.Begin and DeviceContext.End. DeviceContext.GetData will return a QueryDataPipelineStatistics.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_PIPELINE_STATISTICS)</para>
    /// </summary>
    PipelineStatistics = D3D11_QUERY_PIPELINE_STATISTICS,
    /// <summary>
    /// Similar to Occlusion, except DeviceContext.GetData returns a BOOL indicating whether or not any samples passed the depth and stencil tests - TRUE meaning at least one passed, FALSE meaning none passed.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_OCCLUSION_PREDICATE)</para>
    /// </summary>
    OcclusionPredicate = D3D11_QUERY_OCCLUSION_PREDICATE,
    /// <summary>
    /// Get streaming output statistics, such as the number of primitives streamed out in between DeviceContext.Begin and DeviceContext.End. DeviceContext.GetData will return a QueryDataStreamOutputStatistics structure.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_SO_STATISTICS)</para>
    /// </summary>
    StreamOutputStatistics = D3D11_QUERY_SO_STATISTICS,
    /// <summary>
    /// Determines whether or not any of the streaming output buffers overflowed in between DeviceContext.Begin and DeviceContext.End. DeviceContext.GetData returns a BOOL - TRUE meaning there was an overflow, FALSE meaning there was not an overflow. If streaming output writes to multiple buffers, and one of the buffers overflows, then it will stop writing to all the output buffers. When an overflow is detected by Direct3D it is prevented from happening - no memory is corrupted. This predication may be used in conjunction with an SO_STATISTICS query so that when an overflow occurs the SO_STATISTIC query will let the application know how much memory was needed to prevent an overflow.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_SO_OVERFLOW_PREDICATE)</para>
    /// </summary>
    StreamOutputOverflowPredicate = D3D11_QUERY_SO_OVERFLOW_PREDICATE,
    /// <summary>
    /// TBD
    /// <para>(Also see DirectX SDK: D3D11_QUERY_SO_STATISTICS_STREAM0)</para>
    /// </summary>
    StreamOutputStatisticsStream0 = D3D11_QUERY_SO_STATISTICS_STREAM0,
    /// <summary>
    /// TBD
    /// <para>(Also see DirectX SDK: D3D11_QUERY_SO_OVERFLOW_PREDICATE_STREAM0)</para>
    /// </summary>
    StreamOutputOverflowPredicateStream0 = D3D11_QUERY_SO_OVERFLOW_PREDICATE_STREAM0,
    /// <summary>
    /// TBD
    /// <para>(Also see DirectX SDK: D3D11_QUERY_SO_STATISTICS_STREAM1)</para>
    /// </summary>
    StreamOutputStatisticsStream1 = D3D11_QUERY_SO_STATISTICS_STREAM1,
    /// <summary>
    /// TBD
    /// <para>(Also see DirectX SDK: D3D11_QUERY_SO_OVERFLOW_PREDICATE_STREAM1)</para>
    /// </summary>
    StreamOutputOverflowPredicateStream1 = D3D11_QUERY_SO_OVERFLOW_PREDICATE_STREAM1,
    /// <summary>
    /// TBD
    /// <para>(Also see DirectX SDK: D3D11_QUERY_SO_STATISTICS_STREAM2)</para>
    /// </summary>
    StreamOutputStatisticsStream2 = D3D11_QUERY_SO_STATISTICS_STREAM2,
    /// <summary>
    /// TBD
    /// <para>(Also see DirectX SDK: D3D11_QUERY_SO_OVERFLOW_PREDICATE_STREAM2)</para>
    /// </summary>
    StreamOutputOverflowPredicateStream2 = D3D11_QUERY_SO_OVERFLOW_PREDICATE_STREAM2,
    /// <summary>
    /// TBD
    /// <para>(Also see DirectX SDK: D3D11_QUERY_SO_STATISTICS_STREAM3)</para>
    /// </summary>
    StreamOutputStatisticsStream3 = D3D11_QUERY_SO_STATISTICS_STREAM3,
    /// <summary>
    /// TBD
    /// <para>(Also see DirectX SDK: D3D11_QUERY_SO_OVERFLOW_PREDICATE_STREAM3)</para>
    /// </summary>
    StreamOutputOverflowPredicateStream3 = D3D11_QUERY_SO_OVERFLOW_PREDICATE_STREAM3,
};
/// <summary>
/// Flags that describe miscellaneous query behavior.
/// <para>(Also see DirectX SDK: D3D11_QUERY_MISC_FLAG)</para>
/// </summary>
[Flags]
public enum class MiscellaneousQueryOptions : Int32
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0,
    /// <summary>
    /// Tell the hardware that if it is not yet sure if something is hidden or not to draw it anyway. This is only used with an occlusion predicate. Predication data cannot be returned to your application via DeviceContext.GetData when using this flag.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_MISC_PREDICATEHINT)</para>
    /// </summary>
    PredicateHint = D3D11_QUERY_MISC_PREDICATEHINT,
};
/// <summary>
/// Option(s) for raising an error to a non-continuable exception.
/// <para>(Also see DirectX SDK: D3D11_RAISE_FLAG)</para>
/// </summary>
[Flags]
public enum class ExceptionErrors : Int32
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0,
    /// <summary>
    /// Raise an internal driver error to a non-continuable exception.
    /// <para>(Also see DirectX SDK: D3D11_RAISE_FLAG_DRIVER_INTERNAL_ERROR)</para>
    /// </summary>
    DriverInternalError = D3D11_RAISE_FLAG_DRIVER_INTERNAL_ERROR,
};
/// <summary>
/// Identifies the type of resource being used.
/// <para>(Also see DirectX SDK: D3D11_RESOURCE_DIMENSION)</para>
/// </summary>
public enum class ResourceDimension : Int32
{
    /// <summary>
    /// Resource is of unknown type.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_DIMENSION_UNKNOWN)</para>
    /// </summary>
    Unknown = D3D11_RESOURCE_DIMENSION_UNKNOWN,
    /// <summary>
    /// Resource is a buffer.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_DIMENSION_BUFFER)</para>
    /// </summary>
    Buffer = D3D11_RESOURCE_DIMENSION_BUFFER,
    /// <summary>
    /// Resource is a 1D texture.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_DIMENSION_TEXTURE1D)</para>
    /// </summary>
    Texture1D = D3D11_RESOURCE_DIMENSION_TEXTURE1D,
    /// <summary>
    /// Resource is a 2D texture.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_DIMENSION_TEXTURE2D)</para>
    /// </summary>
    Texture2D = D3D11_RESOURCE_DIMENSION_TEXTURE2D,
    /// <summary>
    /// Resource is a 3D texture.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_DIMENSION_TEXTURE3D)</para>
    /// </summary>
    Texture3D = D3D11_RESOURCE_DIMENSION_TEXTURE3D,
};
/// <summary>
/// Identifies other, less common options for resources.
/// <para>(Also see DirectX SDK: D3D11_RESOURCE_MISC_FLAG)</para>
/// </summary>
[Flags]
public enum class MiscellaneousResourceOptions : Int32
{
    /// <summary>
    /// No options set
    /// </summary>
    None = 0,
    /// <summary>
    /// Enables mipmap generation using DeviceContext.GenerateMips on a texture resource. The resource must be created with the bind flags that specify that the resource is a render target and a shader resource.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_MISC_GENERATE_MIPS)</para>
    /// </summary>
    GenerateMips = D3D11_RESOURCE_MISC_GENERATE_MIPS,
    /// <summary>
    /// Enables resource data sharing between two or more Direct3D devices. The only resources that can be shared are 2D non-mipmapped textures.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_MISC_SHARED)</para>
    /// </summary>
    Shared = D3D11_RESOURCE_MISC_SHARED,
    /// <summary>
    /// Enables a resource to be a cube texture created from a Texture2DArray that contains 6 textures.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_MISC_TEXTURECUBE)</para>
    /// </summary>
    TextureCube = D3D11_RESOURCE_MISC_TEXTURECUBE,
    /// <summary>
    /// Enables instancing of GPU-generated content.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_MISC_DRAWINDIRECT_ARGS)</para>
    /// </summary>
    DrawIndirectArgs = D3D11_RESOURCE_MISC_DRAWINDIRECT_ARGS,
    /// <summary>
    /// Enables a resource as a byte address buffer.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_MISC_BUFFER_ALLOW_RAW_VIEWS)</para>
    /// </summary>
    BufferAllowRawViews = D3D11_RESOURCE_MISC_BUFFER_ALLOW_RAW_VIEWS,
    /// <summary>
    /// Enables a resource as a structured buffer.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_MISC_BUFFER_STRUCTURED)</para>
    /// </summary>
    BufferStructured = D3D11_RESOURCE_MISC_BUFFER_STRUCTURED,
    /// <summary>
    /// Enables a resource with a clamped depth bias.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_MISC_RESOURCE_CLAMP)</para>
    /// </summary>
    ResourceClamp = D3D11_RESOURCE_MISC_RESOURCE_CLAMP,
    /// <summary>
    /// Enables a resource as a keyed mutex, see KeyedMutex.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX)</para>
    /// </summary>
    SharedKeyedMutex = D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX,
    /// <summary>
    /// Enables a resource compatible with GDI.
    /// <para>(Also see DirectX SDK: D3D11_RESOURCE_MISC_GDI_COMPATIBLE)</para>
    /// </summary>
    GdiCompatible = D3D11_RESOURCE_MISC_GDI_COMPATIBLE,
};
/// <summary>
/// Options for the amount of information returned about live device objects.
/// <para>(Also see DirectX SDK: D3D11_RLDO_FLAGS)</para>
/// </summary>
[Flags]
public enum class LiveDeviceObjectsReportOptions : Int32
{
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_RLDO_SUMMARY)</para>
    /// </summary>
    Summary = D3D11_RLDO_SUMMARY,
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_RLDO_DETAIL)</para>
    /// </summary>
    Detail = D3D11_RLDO_DETAIL,
};
/// <summary>
/// These flags identify the type of resource that will be viewed as a render target.
/// <para>(Also see DirectX SDK: D3D11_RTV_DIMENSION)</para>
/// </summary>
public enum class RenderTargetViewDimension : Int32
{
    /// <summary>
    /// Do not use this value, as it will cause Device.CreateRenderTargetView to fail.
    /// <para>(Also see DirectX SDK: D3D11_RTV_DIMENSION_UNKNOWN)</para>
    /// </summary>
    Unknown = D3D11_RTV_DIMENSION_UNKNOWN,
    /// <summary>
    /// The resource will be accessed as a buffer.
    /// <para>(Also see DirectX SDK: D3D11_RTV_DIMENSION_BUFFER)</para>
    /// </summary>
    Buffer = D3D11_RTV_DIMENSION_BUFFER,
    /// <summary>
    /// The resource will be accessed as a 1D texture.
    /// <para>(Also see DirectX SDK: D3D11_RTV_DIMENSION_TEXTURE1D)</para>
    /// </summary>
    Texture1D = D3D11_RTV_DIMENSION_TEXTURE1D,
    /// <summary>
    /// The resource will be accessed as an array of 1D textures.
    /// <para>(Also see DirectX SDK: D3D11_RTV_DIMENSION_TEXTURE1DARRAY)</para>
    /// </summary>
    Texture1DArray = D3D11_RTV_DIMENSION_TEXTURE1DARRAY,
    /// <summary>
    /// The resource will be accessed as a 2D texture.
    /// <para>(Also see DirectX SDK: D3D11_RTV_DIMENSION_TEXTURE2D)</para>
    /// </summary>
    Texture2D = D3D11_RTV_DIMENSION_TEXTURE2D,
    /// <summary>
    /// The resource will be accessed as an array of 2D textures.
    /// <para>(Also see DirectX SDK: D3D11_RTV_DIMENSION_TEXTURE2DARRAY)</para>
    /// </summary>
    Texture2DArray = D3D11_RTV_DIMENSION_TEXTURE2DARRAY,
    /// <summary>
    /// The resource will be accessed as a 2D texture with multisampling.
    /// <para>(Also see DirectX SDK: D3D11_RTV_DIMENSION_TEXTURE2DMS)</para>
    /// </summary>
    Texture2DMultisample = D3D11_RTV_DIMENSION_TEXTURE2DMS,
    /// <summary>
    /// The resource will be accessed as an array of 2D textures with multisampling.
    /// <para>(Also see DirectX SDK: D3D11_RTV_DIMENSION_TEXTURE2DMSARRAY)</para>
    /// </summary>
    Texture2DMultisampleArray = D3D11_RTV_DIMENSION_TEXTURE2DMSARRAY,
    /// <summary>
    /// The resource will be accessed as a 3D texture.
    /// <para>(Also see DirectX SDK: D3D11_RTV_DIMENSION_TEXTURE3D)</para>
    /// </summary>
    Texture3D = D3D11_RTV_DIMENSION_TEXTURE3D,
};
/// <summary>
/// These flags identify the type of resource that will be viewed as a shader resource.
/// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION)</para>
/// </summary>
public enum class ShaderResourceViewDimension : Int32
{
    /// <summary>
    /// The type is unknown.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_UNKNOWN)</para>
    /// </summary>
    Unknown = D3D11_SRV_DIMENSION_UNKNOWN,
    /// <summary>
    /// The resource is a buffer.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_BUFFER)</para>
    /// </summary>
    Buffer = D3D11_SRV_DIMENSION_BUFFER,
    /// <summary>
    /// The resource is a 1D texture.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_TEXTURE1D)</para>
    /// </summary>
    Texture1D = D3D11_SRV_DIMENSION_TEXTURE1D,
    /// <summary>
    /// The resource is an array of 1D textures.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_TEXTURE1DARRAY)</para>
    /// </summary>
    Texture1DArray = D3D11_SRV_DIMENSION_TEXTURE1DARRAY,
    /// <summary>
    /// The resource is a 2D texture.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_TEXTURE2D)</para>
    /// </summary>
    Texture2D = D3D11_SRV_DIMENSION_TEXTURE2D,
    /// <summary>
    /// The resource is an array of 2D textures.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_TEXTURE2DARRAY)</para>
    /// </summary>
    Texture2DArray = D3D11_SRV_DIMENSION_TEXTURE2DARRAY,
    /// <summary>
    /// The resource is a multisampling 2D texture.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_TEXTURE2DMS)</para>
    /// </summary>
    Texture2DMultisample = D3D11_SRV_DIMENSION_TEXTURE2DMS,
    /// <summary>
    /// The resource is an array of multisampling 2D textures.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_TEXTURE2DMSARRAY)</para>
    /// </summary>
    Texture2DMultisampleArray = D3D11_SRV_DIMENSION_TEXTURE2DMSARRAY,
    /// <summary>
    /// The resource is a 3D texture.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_TEXTURE3D)</para>
    /// </summary>
    Texture3D = D3D11_SRV_DIMENSION_TEXTURE3D,
    /// <summary>
    /// The resource is a cube texture.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_TEXTURECUBE)</para>
    /// </summary>
    TextureCube = D3D11_SRV_DIMENSION_TEXTURECUBE,
    /// <summary>
    /// The resource is an array of cube textures.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_TEXTURECUBEARRAY)</para>
    /// </summary>
    TextureCubeArray = D3D11_SRV_DIMENSION_TEXTURECUBEARRAY,
    /// <summary>
    /// The resource is an extended buffer.
    /// <para>(Also see DirectX SDK: D3D11_SRV_DIMENSION_BUFFEREX)</para>
    /// </summary>
    ExtendedBuffer = D3D11_SRV_DIMENSION_BUFFEREX,
};
/// <summary>
/// The stencil operations that can be performed during depth-stencil testing.
/// <para>(Also see DirectX SDK: D3D11_STENCIL_OP)</para>
/// </summary>
public enum class StencilOperation : Int32
{
    /// <summary>
    /// Keep the existing stencil data.
    /// <para>(Also see DirectX SDK: D3D11_STENCIL_OP_KEEP)</para>
    /// </summary>
    Keep = D3D11_STENCIL_OP_KEEP,
    /// <summary>
    /// Set the stencil data to 0.
    /// <para>(Also see DirectX SDK: D3D11_STENCIL_OP_ZERO)</para>
    /// </summary>
    Zero = D3D11_STENCIL_OP_ZERO,
    /// <summary>
    /// Set the stencil data to the reference value set by calling DeviceContext.OMSetDepthStencilState.
    /// <para>(Also see DirectX SDK: D3D11_STENCIL_OP_REPLACE)</para>
    /// </summary>
    Replace = D3D11_STENCIL_OP_REPLACE,
    /// <summary>
    /// Increment the stencil value by 1, and clamp the result.
    /// <para>(Also see DirectX SDK: D3D11_STENCIL_OP_INCR_SAT)</para>
    /// </summary>
    IncrementSat = D3D11_STENCIL_OP_INCR_SAT,
    /// <summary>
    /// Decrement the stencil value by 1, and clamp the result.
    /// <para>(Also see DirectX SDK: D3D11_STENCIL_OP_DECR_SAT)</para>
    /// </summary>
    DecrementSat = D3D11_STENCIL_OP_DECR_SAT,
    /// <summary>
    /// Invert the stencil data.
    /// <para>(Also see DirectX SDK: D3D11_STENCIL_OP_INVERT)</para>
    /// </summary>
    Invert = D3D11_STENCIL_OP_INVERT,
    /// <summary>
    /// Increment the stencil value by 1, and wrap the result if necessary.
    /// <para>(Also see DirectX SDK: D3D11_STENCIL_OP_INCR)</para>
    /// </summary>
    Increment = D3D11_STENCIL_OP_INCR,
    /// <summary>
    /// Increment the stencil value by 1, and wrap the result if necessary.
    /// <para>(Also see DirectX SDK: D3D11_STENCIL_OP_DECR)</para>
    /// </summary>
    Decrement = D3D11_STENCIL_OP_DECR,
};
/// <summary>
/// The different faces of a cube texture.
/// <para>(Also see DirectX SDK: D3D11_TEXTURECUBE_FACE)</para>
/// </summary>
public enum class TextureCubeFace : Int32
{
    /// <summary>
    /// Positive X face.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURECUBE_FACE_POSITIVE_X)</para>
    /// </summary>
    PositiveX = D3D11_TEXTURECUBE_FACE_POSITIVE_X,
    /// <summary>
    /// Negative X face.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURECUBE_FACE_NEGATIVE_X)</para>
    /// </summary>
    NegativeX = D3D11_TEXTURECUBE_FACE_NEGATIVE_X,
    /// <summary>
    /// Positive Y face.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURECUBE_FACE_POSITIVE_Y)</para>
    /// </summary>
    PositiveY = D3D11_TEXTURECUBE_FACE_POSITIVE_Y,
    /// <summary>
    /// Negative Y face.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURECUBE_FACE_NEGATIVE_Y)</para>
    /// </summary>
    NegativeY = D3D11_TEXTURECUBE_FACE_NEGATIVE_Y,
    /// <summary>
    /// Positive Z face.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURECUBE_FACE_POSITIVE_Z)</para>
    /// </summary>
    PositiveZ = D3D11_TEXTURECUBE_FACE_POSITIVE_Z,
    /// <summary>
    /// Negative Z face.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURECUBE_FACE_NEGATIVE_Z)</para>
    /// </summary>
    NegativeZ = D3D11_TEXTURECUBE_FACE_NEGATIVE_Z,
};
/// <summary>
/// Identify a technique for resolving texture coordinates that are outside of the boundaries of a texture.
/// <para>(Also see DirectX SDK: D3D11_TEXTURE_ADDRESS_MODE)</para>
/// </summary>
public enum class TextureAddressMode : Int32
{
    /// <summary>
    /// Tile the texture at every (u,v) integer junction. For example, for u values between 0 and 3, the texture is repeated three times.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE_ADDRESS_WRAP)</para>
    /// </summary>
    Wrap = D3D11_TEXTURE_ADDRESS_WRAP,
    /// <summary>
    /// Flip the texture at every (u,v) integer junction. For u values between 0 and 1, for example, the texture is addressed normally; between 1 and 2, the texture is flipped (mirrored); between 2 and 3, the texture is normal again; and so on.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE_ADDRESS_MIRROR)</para>
    /// </summary>
    Mirror = D3D11_TEXTURE_ADDRESS_MIRROR,
    /// <summary>
    /// Texture coordinates outside the range [0.0, 1.0] are set to the texture color at 0.0 or 1.0, respectively.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE_ADDRESS_CLAMP)</para>
    /// </summary>
    Clamp = D3D11_TEXTURE_ADDRESS_CLAMP,
    /// <summary>
    /// Texture coordinates outside the range [0.0, 1.0] are set to the border color specified in SamplerDescription or HLSL code.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE_ADDRESS_BORDER)</para>
    /// </summary>
    Border = D3D11_TEXTURE_ADDRESS_BORDER,
    /// <summary>
    /// Similar to Mirror and Clamp. Takes the absolute value of the texture coordinate (thus, mirroring around 0), and then clamps to the maximum value.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE_ADDRESS_MIRROR_ONCE)</para>
    /// </summary>
    MirrorOnce = D3D11_TEXTURE_ADDRESS_MIRROR_ONCE,
};
/// <summary>
/// Unordered-access view options.
/// <para>(Also see DirectX SDK: D3D11_UAV_DIMENSION)</para>
/// </summary>
public enum class UnorderedAccessViewDimension : Int32
{
    /// <summary>
    /// The view type is unknown.
    /// <para>(Also see DirectX SDK: D3D11_UAV_DIMENSION_UNKNOWN)</para>
    /// </summary>
    Unknown = D3D11_UAV_DIMENSION_UNKNOWN,
    /// <summary>
    /// View the resource as a buffer.
    /// <para>(Also see DirectX SDK: D3D11_UAV_DIMENSION_BUFFER)</para>
    /// </summary>
    Buffer = D3D11_UAV_DIMENSION_BUFFER,
    /// <summary>
    /// View the resource as a 1D texture.
    /// <para>(Also see DirectX SDK: D3D11_UAV_DIMENSION_TEXTURE1D)</para>
    /// </summary>
    Texture1D = D3D11_UAV_DIMENSION_TEXTURE1D,
    /// <summary>
    /// View the resource as a 1D texture array.
    /// <para>(Also see DirectX SDK: D3D11_UAV_DIMENSION_TEXTURE1DARRAY)</para>
    /// </summary>
    Texture1DArray = D3D11_UAV_DIMENSION_TEXTURE1DARRAY,
    /// <summary>
    /// View the resource as a 2D texture.
    /// <para>(Also see DirectX SDK: D3D11_UAV_DIMENSION_TEXTURE2D)</para>
    /// </summary>
    Texture2D = D3D11_UAV_DIMENSION_TEXTURE2D,
    /// <summary>
    /// View the resource as a 2D texture array.
    /// <para>(Also see DirectX SDK: D3D11_UAV_DIMENSION_TEXTURE2DARRAY)</para>
    /// </summary>
    Texture2DArray = D3D11_UAV_DIMENSION_TEXTURE2DARRAY,
    /// <summary>
    /// View the resource as a 3D texture array.
    /// <para>(Also see DirectX SDK: D3D11_UAV_DIMENSION_TEXTURE3D)</para>
    /// </summary>
    Texture3D = D3D11_UAV_DIMENSION_TEXTURE3D,
};
/// <summary>
/// Identifies expected resource use during rendering. The usage directly reflects whether a resource is accessible by the CPU and/or the GPU.
/// <para>(Also see DirectX SDK: D3D11_USAGE)</para>
/// </summary>
public enum class Usage : Int32
{
    /// <summary>
    /// A resource that requires read and write access by the GPU. This is likely to be the most common usage choice.
    /// <para>(Also see DirectX SDK: D3D11_USAGE_DEFAULT)</para>
    /// </summary>
    Default = D3D11_USAGE_DEFAULT,
    /// <summary>
    /// A resource that can only be read by the GPU. It cannot be written by the GPU, and cannot be accessed at all by the CPU. This type of resource must be initialized when it is created, since it cannot be changed after creation.
    /// <para>(Also see DirectX SDK: D3D11_USAGE_IMMUTABLE)</para>
    /// </summary>
    Immutable = D3D11_USAGE_IMMUTABLE,
    /// <summary>
    /// A resource that is accessible by both the GPU (read only) and the CPU (write only). A dynamic resource is a good choice for a resource that will be updated by the CPU at least once per frame. There are two ways to update a dynamic resource: if your data is laid exactly the way the resource stores it, use DeviceContext.UpdateSubresource, otherwise, use a Map method.
    /// <para>(Also see DirectX SDK: D3D11_USAGE_DYNAMIC)</para>
    /// </summary>
    Dynamic = D3D11_USAGE_DYNAMIC,
    /// <summary>
    /// A resource that supports data transfer (copy) from the GPU to the CPU.
    /// <para>(Also see DirectX SDK: D3D11_USAGE_STAGING)</para>
    /// </summary>
    Staging = D3D11_USAGE_STAGING,
};
} } } }
