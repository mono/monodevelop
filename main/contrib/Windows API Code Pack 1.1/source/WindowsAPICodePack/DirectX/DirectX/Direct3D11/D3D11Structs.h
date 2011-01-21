//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System;
using namespace msclr::interop;

using namespace Microsoft::WindowsAPICodePack::DirectX;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 { 

/// <summary>
/// Describes the blend state for a render target.
/// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_BLEND_DESC)</para>
/// </summary>
public value struct RenderTargetBlendDescription
{
public:
    /// <summary>
    /// Enable (or disable) blending.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_BLEND_DESC.BlendEnable)</para>
    /// </summary>
    property Boolean BlendEnable
    {
        Boolean get()
        {
            return blendEnable;
        }

        void set(Boolean value)
        {
            blendEnable = value;
        }
    }
    /// <summary>
    /// This blend option specifies the first RGB data source and includes an optional pre-blend operation.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_BLEND_DESC.SrcBlend)</para>
    /// </summary>
    property Blend SourceBlend
    {
        Blend get()
        {
            return sourceBlend;
        }

        void set(Blend value)
        {
            sourceBlend = value;
        }
    }
    /// <summary>
    /// This blend option specifies the second RGB data source and includes an optional pre-blend operation.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_BLEND_DESC.DestBlend)</para>
    /// </summary>
    property Blend DestinationBlend
    {
        Blend get()
        {
            return destinationBlend;
        }

        void set(Blend value)
        {
            destinationBlend = value;
        }
    }
    /// <summary>
    /// This blend operation defines how to combine the RGB data sources.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_BLEND_DESC.BlendOp)</para>
    /// </summary>
    property BlendOperation BlendOperation
    {
        Direct3D11::BlendOperation get()
        {
            return blendOperation;
        }

        void set(Direct3D11::BlendOperation value)
        {
            blendOperation = value;
        }
    }
    /// <summary>
    /// This blend option specifies the first alpha data source and includes an optional pre-blend operation. Blend options that end in _COLOR are not allowed.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_BLEND_DESC.SrcBlendAlpha)</para>
    /// </summary>
    property Blend SourceBlendAlpha
    {
        Blend get()
        {
            return sourceBlendAlpha;
        }

        void set(Blend value)
        {
            sourceBlendAlpha = value;
        }
    }
    /// <summary>
    /// This blend option specifies the second alpha data source and includes an optional pre-blend operation. Blend options that end in _COLOR are not allowed.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_BLEND_DESC.DestBlendAlpha)</para>
    /// </summary>
    property Blend DestinationBlendAlpha
    {
        Blend get()
        {
            return destinationBlendAlpha;
        }

        void set(Blend value)
        {
            destinationBlendAlpha = value;
        }
    }
    /// <summary>
    /// This blend operation defines how to combine the alpha data sources.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_BLEND_DESC.BlendOpAlpha)</para>
    /// </summary>
    property Direct3D11::BlendOperation BlendOperationAlpha
    {
        Direct3D11::BlendOperation get()
        {
            return blendOperationAlpha;
        }

        void set(Direct3D11::BlendOperation value)
        {
            blendOperationAlpha = value;
        }
    }
    /// <summary>
    /// A write mask.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_BLEND_DESC.RenderTargetWriteMask)</para>
    /// </summary>
    property ColorWriteEnableComponents RenderTargetWriteMask
    {
        ColorWriteEnableComponents get()
        {
            return renderTargetWriteMask;
        }

        void set(ColorWriteEnableComponents value)
        {
            renderTargetWriteMask = value;
        }
    }
private:

    Boolean blendEnable;
    Blend sourceBlend;
    Blend destinationBlend;
    Direct3D11::BlendOperation blendOperation;
    Blend sourceBlendAlpha;
    Blend destinationBlendAlpha;
    Direct3D11::BlendOperation blendOperationAlpha;
    ColorWriteEnableComponents renderTargetWriteMask;

public:

    static Boolean operator == (RenderTargetBlendDescription renderTargetBlendDescription1, RenderTargetBlendDescription renderTargetBlendDescription2)
    {
        return (renderTargetBlendDescription1.blendEnable == renderTargetBlendDescription2.blendEnable) &&
            (renderTargetBlendDescription1.sourceBlend == renderTargetBlendDescription2.sourceBlend) &&
            (renderTargetBlendDescription1.destinationBlend == renderTargetBlendDescription2.destinationBlend) &&
            (renderTargetBlendDescription1.blendOperation == renderTargetBlendDescription2.blendOperation) &&
            (renderTargetBlendDescription1.sourceBlendAlpha == renderTargetBlendDescription2.sourceBlendAlpha) &&
            (renderTargetBlendDescription1.destinationBlendAlpha == renderTargetBlendDescription2.destinationBlendAlpha) &&
            (renderTargetBlendDescription1.blendOperationAlpha == renderTargetBlendDescription2.blendOperationAlpha) &&
            (renderTargetBlendDescription1.renderTargetWriteMask == renderTargetBlendDescription2.renderTargetWriteMask);
    }

    static Boolean operator != (RenderTargetBlendDescription renderTargetBlendDescription1, RenderTargetBlendDescription renderTargetBlendDescription2)
    {
        return !(renderTargetBlendDescription1 == renderTargetBlendDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != RenderTargetBlendDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<RenderTargetBlendDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + blendEnable.GetHashCode();
        hashCode = hashCode * 31 + sourceBlend.GetHashCode();
        hashCode = hashCode * 31 + destinationBlend.GetHashCode();
        hashCode = hashCode * 31 + blendOperation.GetHashCode();
        hashCode = hashCode * 31 + sourceBlendAlpha.GetHashCode();
        hashCode = hashCode * 31 + destinationBlendAlpha.GetHashCode();
        hashCode = hashCode * 31 + blendOperationAlpha.GetHashCode();
        hashCode = hashCode * 31 + renderTargetWriteMask.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes the blend state.
/// <para>(Also see DirectX SDK: D3D11_BLEND_DESC)</para>
/// </summary>
public value struct BlendDescription
{
public:
    /// <summary>
    /// Determines whether or not to use alpha-to-coverage as a multisampling technique when setting a pixel to a rendertarget.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_DESC.AlphaToCoverageEnable)</para>
    /// </summary>
    property Boolean AlphaToCoverageEnable
    {
        Boolean get()
        {
            return alphaToCoverageEnable;
        }

        void set(Boolean value)
        {
            alphaToCoverageEnable = value;
        }
    }
    /// <summary>
    /// Set to TRUE to enable independent blending in simultaneous render targets.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_DESC.IndependentBlendEnable)</para>
    /// </summary>
    property Boolean IndependentBlendEnable
    {
        Boolean get()
        {
            return independentBlendEnable;
        }

        void set(Boolean value)
        {
            independentBlendEnable = value;
        }
    }
    /// <summary>
    /// A collection of render-target-blend descriptions (see <see cref="RenderTargetBlendDescription"/>)<seealso cref="RenderTargetBlendDescription"/>; these correspond to the eight rendertargets that can be set to the output-merger stage at one time.
    /// <para>(Also see DirectX SDK: D3D11_BLEND_DESC.RenderTarget)</para>
    /// </summary>
    property IEnumerable<RenderTargetBlendDescription>^ RenderTarget
    {
        IEnumerable<RenderTargetBlendDescription>^ get()
        {
            if (renderTarget == nullptr)
            {
                renderTarget  = gcnew array<RenderTargetBlendDescription>(RenderTargetArrayLength);
            }

            return Array::AsReadOnly(renderTarget);
        }
    }

internal:
    BlendDescription(const D3D11_BLEND_DESC& blendDescription)
    {
        AlphaToCoverageEnable = blendDescription.AlphaToCoverageEnable != 0;
        IndependentBlendEnable = blendDescription.IndependentBlendEnable != 0;

        renderTarget = gcnew array<RenderTargetBlendDescription>(RenderTargetArrayLength);

        for (int index = 0; index < RenderTargetArrayLength; index++)
        {
            // WARNING: setting mutable members of a value type instance retrieved by
            // index into an array is safe. But for other "indexable" contexts (e.g.
            // type indexer, IList<T>, etc.) this would not work. If the type of "renderTarget"
            // is ever changed to something other than an array, this code needs to be
            // fixed to match by initializing a local of type RenderTargetBlendDescription
            // and then copy the entire value type instance to the indexed storage at once.

            renderTarget[index].BlendEnable = blendDescription.RenderTarget[index].BlendEnable != FALSE;
            renderTarget[index].BlendOperation =
                static_cast<Direct3D11::BlendOperation>(blendDescription.RenderTarget[index].BlendOp);
            renderTarget[index].BlendOperationAlpha =
                static_cast<Direct3D11::BlendOperation>(blendDescription.RenderTarget[index].BlendOpAlpha);
            renderTarget[index].DestinationBlend =
                static_cast<Direct3D11::Blend>(blendDescription.RenderTarget[index].DestBlend);
            renderTarget[index].DestinationBlendAlpha =
                static_cast<Direct3D11::Blend>(blendDescription.RenderTarget[index].DestBlendAlpha);
            renderTarget[index].RenderTargetWriteMask =
                static_cast<ColorWriteEnableComponents>(blendDescription.RenderTarget[index].RenderTargetWriteMask);
            renderTarget[index].SourceBlend =
                static_cast<Direct3D11::Blend>(blendDescription.RenderTarget[index].SrcBlend);
            renderTarget[index].SourceBlendAlpha =
                static_cast<Direct3D11::Blend>(blendDescription.RenderTarget[index].SrcBlendAlpha);
        }
    }

    void CopyTo(D3D11_BLEND_DESC* blendDescription)
    {
        blendDescription->AlphaToCoverageEnable = AlphaToCoverageEnable ? 1 :0;
        blendDescription->IndependentBlendEnable = IndependentBlendEnable ? 1 :0;

        if (renderTarget != nullptr)
        {
            for (int index = 0; index < RenderTargetArrayLength; index++)
            {
                blendDescription->RenderTarget[index].BlendEnable =
                    renderTarget[index].BlendEnable ? TRUE : FALSE;
                blendDescription->RenderTarget[index].BlendOp =
                    static_cast<D3D11_BLEND_OP>(renderTarget[index].BlendOperation);
                blendDescription->RenderTarget[index].BlendOpAlpha =
                    static_cast<D3D11_BLEND_OP>(renderTarget[index].BlendOperationAlpha);
                blendDescription->RenderTarget[index].DestBlend =
                    static_cast<D3D11_BLEND>(renderTarget[index].DestinationBlend);
                blendDescription->RenderTarget[index].DestBlendAlpha =
                    static_cast<D3D11_BLEND>(renderTarget[index].DestinationBlendAlpha);
                blendDescription->RenderTarget[index].RenderTargetWriteMask =
                    static_cast<UINT8>(renderTarget[index].RenderTargetWriteMask);
                blendDescription->RenderTarget[index].SrcBlend =
                    static_cast<D3D11_BLEND>(renderTarget[index].SourceBlend);
                blendDescription->RenderTarget[index].SrcBlendAlpha =
                    static_cast<D3D11_BLEND>(renderTarget[index].SourceBlendAlpha);
            }
        }
        else
        {
            ZeroMemory(blendDescription->RenderTarget, sizeof(D3D11_RENDER_TARGET_BLEND_DESC) * RenderTargetArrayLength);
        }
    }

private:

    Boolean alphaToCoverageEnable;
    Boolean independentBlendEnable;
    literal int RenderTargetArrayLength = 8;
    array<RenderTargetBlendDescription>^ renderTarget;

};

/// <summary>
/// Defines a 3D box.
/// <para>(Also see DirectX SDK: D3D11_BOX)</para>
/// </summary>
public value struct Box
{
public:
    /// <summary>
    /// The x position of the left hand side of the box.
    /// <para>(Also see DirectX SDK: D3D11_BOX.left)</para>
    /// </summary>
    property UInt32 Left
    {
        UInt32 get()
        {
            return left;
        }

        void set(UInt32 value)
        {
            left = value;
        }
    }
    /// <summary>
    /// The y position of the top of the box.
    /// <para>(Also see DirectX SDK: D3D11_BOX.top)</para>
    /// </summary>
    property UInt32 Top
    {
        UInt32 get()
        {
            return top;
        }

        void set(UInt32 value)
        {
            top = value;
        }
    }
    /// <summary>
    /// The z position of the front of the box.
    /// <para>(Also see DirectX SDK: D3D11_BOX.front)</para>
    /// </summary>
    property UInt32 Front
    {
        UInt32 get()
        {
            return front;
        }

        void set(UInt32 value)
        {
            front = value;
        }
    }
    /// <summary>
    /// The x position of the right hand side of the box.
    /// <para>(Also see DirectX SDK: D3D11_BOX.right)</para>
    /// </summary>
    property UInt32 Right
    {
        UInt32 get()
        {
            return right;
        }

        void set(UInt32 value)
        {
            right = value;
        }
    }
    /// <summary>
    /// The y position of the bottom of the box.
    /// <para>(Also see DirectX SDK: D3D11_BOX.bottom)</para>
    /// </summary>
    property UInt32 Bottom
    {
        UInt32 get()
        {
            return bottom;
        }

        void set(UInt32 value)
        {
            bottom = value;
        }
    }
    /// <summary>
    /// The z position of the back of the box.
    /// <para>(Also see DirectX SDK: D3D11_BOX.back)</para>
    /// </summary>
    property UInt32 Back
    {
        UInt32 get()
        {
            return back;
        }

        void set(UInt32 value)
        {
            back = value;
        }
    }
private:

    UInt32 left;
    UInt32 top;
    UInt32 front;
    UInt32 right;
    UInt32 bottom;
    UInt32 back;

public:
    static Boolean operator == (Box box1, Box box2)
    {
        return 
            (box1.Back == box2.Back)  &&
            (box1.Bottom == box2.Bottom)  &&
            (box1.Front == box2.Front)  &&
            (box1.Left == box2.Left)  &&
            (box1.Right == box2.Right)  &&
            (box1.Top == box2.Top);
    }

    static Boolean operator != (Box box1, Box box2)
    {
        return !(box1 == box2);
    }

internal:
    Box(const D3D11_BOX& nBox)
    {
        Left = nBox.left;
        Top = nBox.top;
        Front = nBox.front;
        Right = nBox.right;
        Bottom = nBox.bottom;
        Back = nBox.back;
    }
public:

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Box::typeid)
        {
            return false;
        }

        return *this == safe_cast<Box>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + left.GetHashCode();
        hashCode = hashCode * 31 + top.GetHashCode();
        hashCode = hashCode * 31 + front.GetHashCode();
        hashCode = hashCode * 31 + right.GetHashCode();
        hashCode = hashCode * 31 + bottom.GetHashCode();
        hashCode = hashCode * 31 + back.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a raw buffer resource.
/// <para>(Also see DirectX SDK: D3D11_BUFFEREX_SRV)</para>
/// </summary>
public value struct ExtendedBufferShaderResourceView
{
public:
    /// <summary>
    /// The index of the first element to be accessed by the view.
    /// <para>(Also see DirectX SDK: D3D11_BUFFEREX_SRV.FirstElement)</para>
    /// </summary>
    property UInt32 FirstElement
    {
        UInt32 get()
        {
            return firstElement;
        }

        void set(UInt32 value)
        {
            firstElement = value;
        }
    }
    /// <summary>
    /// The number of elements in the resource.
    /// <para>(Also see DirectX SDK: D3D11_BUFFEREX_SRV.NumElements)</para>
    /// </summary>
    property UInt32 ElementCount
    {
        UInt32 get()
        {
            return numElements;
        }

        void set(UInt32 value)
        {
            numElements = value;
        }
    }
    /// <summary>
    /// Options for binding a raw buffer (see <see cref="ExtendedBufferBindingOptions"/>)<seealso cref="ExtendedBufferBindingOptions"/>
    /// <para>(Also see DirectX SDK: D3D11_BUFFEREX_SRV.Flags)</para>
    /// </summary>
    property ExtendedBufferBindingOptions BindingOptions
    {
        ExtendedBufferBindingOptions get()
        {
            return flags;
        }

        void set(ExtendedBufferBindingOptions value)
        {
            flags = value;
        }
    }
private:

    UInt32 firstElement;
    UInt32 numElements;
    ExtendedBufferBindingOptions flags;

public:

    static Boolean operator == (ExtendedBufferShaderResourceView extendedBufferShaderResourceView1, ExtendedBufferShaderResourceView extendedBufferShaderResourceView2)
    {
        return (extendedBufferShaderResourceView1.firstElement == extendedBufferShaderResourceView2.firstElement) &&
            (extendedBufferShaderResourceView1.numElements == extendedBufferShaderResourceView2.numElements) &&
            (extendedBufferShaderResourceView1.flags == extendedBufferShaderResourceView2.flags);
    }

    static Boolean operator != (ExtendedBufferShaderResourceView extendedBufferShaderResourceView1, ExtendedBufferShaderResourceView extendedBufferShaderResourceView2)
    {
        return !(extendedBufferShaderResourceView1 == extendedBufferShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != ExtendedBufferShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<ExtendedBufferShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + firstElement.GetHashCode();
        hashCode = hashCode * 31 + numElements.GetHashCode();
        hashCode = hashCode * 31 + flags.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a buffer resource.
/// <para>(Also see DirectX SDK: D3D11_BUFFER_DESC)</para>
/// </summary>
public value struct BufferDescription
{
public:
    /// <summary>
    /// Size of the buffer in bytes.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_DESC.ByteWidth)</para>
    /// </summary>
    property UInt32 ByteWidth
    {
        UInt32 get()
        {
            return byteWidth;
        }

        void set(UInt32 value)
        {
            byteWidth = value;
        }
    }
    /// <summary>
    /// Identify how the buffer is expected to be read from and written to. Frequency of update is a key factor. The most common value is typically Usage_DEFAULT; see Usage for all possible values.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_DESC.Usage)</para>
    /// </summary>
    property Usage Usage
    {
        Direct3D11::Usage get()
        {
            return usage;
        }

        void set(Direct3D11::Usage value)
        {
            usage = value;
        }
    }
    /// <summary>
    /// Identify how the buffer will be bound to the pipeline. Flags (see <see cref="Direct3D11::BindingOptions"/>)<seealso cref="Direct3D11::BindingOptions"/> can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_DESC.BindFlags)</para>
    /// </summary>
    property BindingOptions BindingOptions
    {
        Direct3D11::BindingOptions get()
        {
            return bindFlags;
        }

        void set(Direct3D11::BindingOptions value)
        {
            bindFlags = value;
        }
    }
    /// <summary>
    /// CPU access flags (see <see cref="CpuAccessOptions"/>)<seealso cref="CpuAccessOptions"/> or 0 if no CPU access is necessary. Flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_DESC.CPUAccessFlags)</para>
    /// </summary>
    property CpuAccessOptions CpuAccessOptions
    {
        Direct3D11::CpuAccessOptions get()
        {
            return cpuAccessFlags;
        }

        void set(Direct3D11::CpuAccessOptions value)
        {
            cpuAccessFlags = value;
        }
    }
    /// <summary>
    /// Miscellaneous flags (see <see cref="MiscellaneousResourceOptions"/>)<seealso cref="MiscellaneousResourceOptions"/> or 0 if unused. Flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_DESC.MiscFlags)</para>
    /// </summary>
    property MiscellaneousResourceOptions MiscellaneousResourceOptions
    {
        Direct3D11::MiscellaneousResourceOptions get()
        {
            return miscFlags;
        }

        void set(Direct3D11::MiscellaneousResourceOptions value)
        {
            miscFlags = value;
        }
    }
    /// <summary>
    /// The size of the structure (in bytes) when it represents a structured buffer.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_DESC.StructureByteStride)</para>
    /// </summary>
    property UInt32 StructureByteStride
    {
        UInt32 get()
        {
            return structureByteStride;
        }

        void set(UInt32 value)
        {
            structureByteStride = value;
        }
    }

private:

    Direct3D11::BindingOptions bindFlags;
    Direct3D11::CpuAccessOptions cpuAccessFlags;
    Direct3D11::MiscellaneousResourceOptions miscFlags;
    UInt32 structureByteStride;

internal:
    BufferDescription (const D3D11_BUFFER_DESC& desc)
    {
        ByteWidth = desc.ByteWidth;
        Usage = static_cast<Direct3D11::Usage>(desc.Usage);
        BindingOptions = static_cast<Direct3D11::BindingOptions>(desc.BindFlags);
        CpuAccessOptions = static_cast<Direct3D11::CpuAccessOptions>(desc.CPUAccessFlags);
        StructureByteStride = desc.StructureByteStride;
        MiscellaneousResourceOptions = static_cast<Direct3D11::MiscellaneousResourceOptions>(desc.MiscFlags);

    }

    void CopyTo(D3D11_BUFFER_DESC &desc)
    {
        desc.ByteWidth = ByteWidth;
        desc.Usage = static_cast<D3D11_USAGE>(Usage);
        desc.BindFlags = static_cast<UINT>(BindingOptions);
        desc.CPUAccessFlags = static_cast<UINT>(CpuAccessOptions);
        desc.StructureByteStride = StructureByteStride;
        desc.MiscFlags = static_cast<UINT>(MiscellaneousResourceOptions);
    }


private:

    UInt32 byteWidth;
    Direct3D11::Usage usage;

public:

    static Boolean operator == (BufferDescription bufferDescription1, BufferDescription bufferDescription2)
    {
        return (bufferDescription1.bindFlags == bufferDescription2.bindFlags) &&
            (bufferDescription1.cpuAccessFlags == bufferDescription2.cpuAccessFlags) &&
            (bufferDescription1.miscFlags == bufferDescription2.miscFlags) &&
            (bufferDescription1.structureByteStride == bufferDescription2.structureByteStride) &&
            (bufferDescription1.byteWidth == bufferDescription2.byteWidth) &&
            (bufferDescription1.usage == bufferDescription2.usage);
    }

    static Boolean operator != (BufferDescription bufferDescription1, BufferDescription bufferDescription2)
    {
        return !(bufferDescription1 == bufferDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != BufferDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<BufferDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + bindFlags.GetHashCode();
        hashCode = hashCode * 31 + cpuAccessFlags.GetHashCode();
        hashCode = hashCode * 31 + miscFlags.GetHashCode();
        hashCode = hashCode * 31 + structureByteStride.GetHashCode();
        hashCode = hashCode * 31 + byteWidth.GetHashCode();
        hashCode = hashCode * 31 + usage.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the elements in a buffer resource to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D11_BUFFER_RTV)</para>
/// </summary>
public value struct BufferRenderTargetView
{
public:
    /// <summary>
    /// Number of bytes between the beginning of the buffer and the first element to access.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_RTV.ElementOffset)</para>
    /// </summary>
    property UInt32 ElementOffset
    {
        UInt32 get()
        {
            return elementOffset;
        }

        void set(UInt32 value)
        {
            elementOffset = value;
        }
    }
    /// <summary>
    /// The width of each element (in bytes). This can be determined from the format stored in the render-target-view description.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_RTV.ElementWidth)</para>
    /// </summary>
    property UInt32 ElementWidth
    {
        UInt32 get()
        {
            return elementWidth;
        }

        void set(UInt32 value)
        {
            elementWidth = value;
        }
    }
private:

    UInt32 elementOffset;
    UInt32 elementWidth;

public:

    static Boolean operator == (BufferRenderTargetView bufferRenderTargetView1, BufferRenderTargetView bufferRenderTargetView2)
    {
        return (bufferRenderTargetView1.elementOffset == bufferRenderTargetView2.elementOffset) &&
            (bufferRenderTargetView1.elementWidth == bufferRenderTargetView2.elementWidth);
    }

    static Boolean operator != (BufferRenderTargetView bufferRenderTargetView1, BufferRenderTargetView bufferRenderTargetView2)
    {
        return !(bufferRenderTargetView1 == bufferRenderTargetView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != BufferRenderTargetView::typeid)
        {
            return false;
        }

        return *this == safe_cast<BufferRenderTargetView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + elementOffset.GetHashCode();
        hashCode = hashCode * 31 + elementWidth.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the elements in a buffer resource to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_BUFFER_SRV)</para>
/// </summary>
public value struct BufferShaderResourceView
{
public:
    /// <summary>
    /// The offset of the first element in the view to access, relative to element 0.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_SRV.ElementOffset)</para>
    /// </summary>
    property UInt32 ElementOffset
    {
        UInt32 get()
        {
            return elementOffset;
        }

        void set(UInt32 value)
        {
            elementOffset = value;
        }
    }
    /// <summary>
    /// The total number of elements in the view.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_SRV.ElementWidth)</para>
    /// </summary>
    property UInt32 ElementWidth
    {
        UInt32 get()
        {
            return elementWidth;
        }

        void set(UInt32 value)
        {
            elementWidth = value;
        }
    }
private:

    UInt32 elementOffset;
    UInt32 elementWidth;

public:

    static Boolean operator == (BufferShaderResourceView bufferShaderResourceView1, BufferShaderResourceView bufferShaderResourceView2)
    {
        return (bufferShaderResourceView1.elementOffset == bufferShaderResourceView2.elementOffset) &&
            (bufferShaderResourceView1.elementWidth == bufferShaderResourceView2.elementWidth);
    }

    static Boolean operator != (BufferShaderResourceView bufferShaderResourceView1, BufferShaderResourceView bufferShaderResourceView2)
    {
        return !(bufferShaderResourceView1 == bufferShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != BufferShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<BufferShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + elementOffset.GetHashCode();
        hashCode = hashCode * 31 + elementWidth.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a unordered-access buffer resource.
/// <para>(Also see DirectX SDK: D3D11_BUFFER_UAV)</para>
/// </summary>
public value struct BufferUnorderedAccessView
{
public:
    /// <summary>
    /// The zero-based index of the first element to be accessed.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_UAV.FirstElement)</para>
    /// </summary>
    property UInt32 FirstElement
    {
        UInt32 get()
        {
            return firstElement;
        }

        void set(UInt32 value)
        {
            firstElement = value;
        }
    }
    /// <summary>
    /// The number of elements in the resource.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_UAV.NumElements)</para>
    /// </summary>
    property UInt32 ElementCount
    {
        UInt32 get()
        {
            return numElements;
        }

        void set(UInt32 value)
        {
            numElements = value;
        }
    }
    /// <summary>
    /// View options for the resource (see <see cref="UnorderedAccessViewBufferOptions"/>)<seealso cref="UnorderedAccessViewBufferOptions"/>.
    /// <para>(Also see DirectX SDK: D3D11_BUFFER_UAV.Flags)</para>
    /// </summary>
    property UnorderedAccessViewBufferOptions BufferOptions
    {
        UnorderedAccessViewBufferOptions get()
        {
            return flags;
        }

        void set(UnorderedAccessViewBufferOptions value)
        {
            flags = value;
        }
    }
private:

    UInt32 firstElement;
    UInt32 numElements;
    UnorderedAccessViewBufferOptions flags;

public:

    static Boolean operator == (BufferUnorderedAccessView bufferUnorderedAccessView1, BufferUnorderedAccessView bufferUnorderedAccessView2)
    {
        return (bufferUnorderedAccessView1.firstElement == bufferUnorderedAccessView2.firstElement) &&
            (bufferUnorderedAccessView1.numElements == bufferUnorderedAccessView2.numElements) &&
            (bufferUnorderedAccessView1.flags == bufferUnorderedAccessView2.flags);
    }

    static Boolean operator != (BufferUnorderedAccessView bufferUnorderedAccessView1, BufferUnorderedAccessView bufferUnorderedAccessView2)
    {
        return !(bufferUnorderedAccessView1 == bufferUnorderedAccessView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != BufferUnorderedAccessView::typeid)
        {
            return false;
        }

        return *this == safe_cast<BufferUnorderedAccessView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + firstElement.GetHashCode();
        hashCode = hashCode * 31 + numElements.GetHashCode();
        hashCode = hashCode * 31 + flags.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes an HLSL class instance.
/// <para>(Also see DirectX SDK: D3D11_CLASS_INSTANCE_DESC)</para>
/// </summary>
public value struct ClassInstanceDescription
{
public:
    /// <summary>
    /// The instance ID of an HLSL class; the default value is 0.
    /// <para>(Also see DirectX SDK: D3D11_CLASS_INSTANCE_DESC.InstanceId)</para>
    /// </summary>
    property UInt32 InstanceId
    {
        UInt32 get()
        {
            return instanceId;
        }

        void set(UInt32 value)
        {
            instanceId = value;
        }
    }
    /// <summary>
    /// The instance index of an HLSL class; the default value is 0.
    /// <para>(Also see DirectX SDK: D3D11_CLASS_INSTANCE_DESC.InstanceIndex)</para>
    /// </summary>
    property UInt32 InstanceIndex
    {
        UInt32 get()
        {
            return instanceIndex;
        }

        void set(UInt32 value)
        {
            instanceIndex = value;
        }
    }
    /// <summary>
    /// The type ID of an HLSL class; the default value is 0.
    /// <para>(Also see DirectX SDK: D3D11_CLASS_INSTANCE_DESC.TypeId)</para>
    /// </summary>
    property UInt32 TypeId
    {
        UInt32 get()
        {
            return typeId;
        }

        void set(UInt32 value)
        {
            typeId = value;
        }
    }
    /// <summary>
    /// Describes the constant buffer associated with an HLSL class; the default value is 0.
    /// <para>(Also see DirectX SDK: D3D11_CLASS_INSTANCE_DESC.ConstantBuffer)</para>
    /// </summary>
    property UInt32 ConstantBuffer
    {
        UInt32 get()
        {
            return constantBuffer;
        }

        void set(UInt32 value)
        {
            constantBuffer = value;
        }
    }
    /// <summary>
    /// The base constant buffer offset associated with an HLSL class; the default value is 0.
    /// <para>(Also see DirectX SDK: D3D11_CLASS_INSTANCE_DESC.BaseConstantBufferOffset)</para>
    /// </summary>
    property UInt32 BaseConstantBufferOffset
    {
        UInt32 get()
        {
            return baseConstantBufferOffset;
        }

        void set(UInt32 value)
        {
            baseConstantBufferOffset = value;
        }
    }
    /// <summary>
    /// The base texture associated with an HLSL class; the default value is 127.
    /// <para>(Also see DirectX SDK: D3D11_CLASS_INSTANCE_DESC.BaseTexture)</para>
    /// </summary>
    property UInt32 BaseTexture
    {
        UInt32 get()
        {
            return baseTexture;
        }

        void set(UInt32 value)
        {
            baseTexture = value;
        }
    }
    /// <summary>
    /// The base sampler associated with an HLSL class; the default value is 15.
    /// <para>(Also see DirectX SDK: D3D11_CLASS_INSTANCE_DESC.BaseSampler)</para>
    /// </summary>
    property UInt32 BaseSampler
    {
        UInt32 get()
        {
            return baseSampler;
        }

        void set(UInt32 value)
        {
            baseSampler = value;
        }
    }
    /// <summary>
    /// True if the class was created; the default value is false.
    /// <para>(Also see DirectX SDK: D3D11_CLASS_INSTANCE_DESC.Created)</para>
    /// </summary>
    property Boolean Created
    {
        Boolean get()
        {
            return created;
        }

        void set(Boolean value)
        {
            created = value;
        }
    }
private:

    UInt32 instanceId;
    UInt32 instanceIndex;
    UInt32 typeId;
    UInt32 constantBuffer;
    UInt32 baseConstantBufferOffset;
    UInt32 baseTexture;
    UInt32 baseSampler;
    Boolean created;

public:

    static Boolean operator == (ClassInstanceDescription classInstanceDescription1, ClassInstanceDescription classInstanceDescription2)
    {
        return (classInstanceDescription1.instanceId == classInstanceDescription2.instanceId) &&
            (classInstanceDescription1.instanceIndex == classInstanceDescription2.instanceIndex) &&
            (classInstanceDescription1.typeId == classInstanceDescription2.typeId) &&
            (classInstanceDescription1.constantBuffer == classInstanceDescription2.constantBuffer) &&
            (classInstanceDescription1.baseConstantBufferOffset == classInstanceDescription2.baseConstantBufferOffset) &&
            (classInstanceDescription1.baseTexture == classInstanceDescription2.baseTexture) &&
            (classInstanceDescription1.baseSampler == classInstanceDescription2.baseSampler) &&
            (classInstanceDescription1.created == classInstanceDescription2.created);
    }

    static Boolean operator != (ClassInstanceDescription classInstanceDescription1, ClassInstanceDescription classInstanceDescription2)
    {
        return !(classInstanceDescription1 == classInstanceDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != ClassInstanceDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<ClassInstanceDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + instanceId.GetHashCode();
        hashCode = hashCode * 31 + instanceIndex.GetHashCode();
        hashCode = hashCode * 31 + typeId.GetHashCode();
        hashCode = hashCode * 31 + constantBuffer.GetHashCode();
        hashCode = hashCode * 31 + baseConstantBufferOffset.GetHashCode();
        hashCode = hashCode * 31 + baseTexture.GetHashCode();
        hashCode = hashCode * 31 + baseSampler.GetHashCode();
        hashCode = hashCode * 31 + created.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// Describes a counter.
/// <para>(Also see DirectX SDK: D3D11_COUNTER_DESC)</para>
/// </summary>
public value struct CounterDescription
{
public:
    /// <summary>
    /// Type of counter (see <see cref="Direct3D11::Counter"/>)<seealso cref="Direct3D11::Counter"/>.
    /// <para>(Also see DirectX SDK: D3D11_COUNTER_DESC.Counter)</para>
    /// </summary>
    property Counter Counter
    {
        Direct3D11::Counter get()
        {
            return counter;
        }

        void set(Direct3D11::Counter value)
        {
            counter = value;
        }
    }
    /// <summary>
    /// Reserved.
    /// <para>(Also see DirectX SDK: D3D11_COUNTER_DESC.MiscFlags)</para>
    /// </summary>
    property UInt32 ReservedOptions
    {
        UInt32 get()
        {
            return miscFlags;
        }

        void set(UInt32 value)
        {
            miscFlags = value;
        }
    }
private:

    Direct3D11::Counter counter;
    UInt32 miscFlags;

internal:
    CounterDescription(const D3D11_COUNTER_DESC& desc)
    {
        Counter = static_cast<Direct3D11::Counter>(desc.Counter);
        ReservedOptions = desc.MiscFlags;
    }
    
    void CopyTo(D3D11_COUNTER_DESC* desc)
    {
        desc->Counter = static_cast<D3D11_COUNTER>(Counter);
        desc->MiscFlags = ReservedOptions;
    }
public:

    static Boolean operator == (CounterDescription counterDescription1, CounterDescription counterDescription2)
    {
        return (counterDescription1.counter == counterDescription2.counter) &&
            (counterDescription1.miscFlags == counterDescription2.miscFlags);
    }

    static Boolean operator != (CounterDescription counterDescription1, CounterDescription counterDescription2)
    {
        return !(counterDescription1 == counterDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != CounterDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<CounterDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + counter.GetHashCode();
        hashCode = hashCode * 31 + miscFlags.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// Information about the video card's performance counter capabilities.
/// <para>(Also see DirectX SDK: D3D11_COUNTER_INFO)</para>
/// </summary>
public value struct CounterInformation
{
public:
    /// <summary>
    /// Largest device-dependent counter ID that the device supports. If none are supported, this value will be 0. Otherwise it will be greater than or equal to DeviceDependent_0.
    /// <para>(Also see DirectX SDK: D3D11_COUNTER_INFO.LastDeviceDependentCounter)</para>
    /// </summary>
    property Direct3D11::Counter LastDeviceDependentCounter
    {
        Direct3D11::Counter get()
        {
            return lastDeviceDependentCounter;
        }

        void set(Direct3D11::Counter value)
        {
            lastDeviceDependentCounter = value;
        }
    }
    /// <summary>
    /// Number of counters that can be simultaneously supported.
    /// <para>(Also see DirectX SDK: D3D11_COUNTER_INFO.NumSimultaneousCounters)</para>
    /// </summary>
    property UInt32 SimultaneousCounterCount
    {
        UInt32 get()
        {
            return numSimultaneousCounters;
        }

        void set(UInt32 value)
        {
            numSimultaneousCounters = value;
        }
    }
    /// <summary>
    /// Number of detectable parallel units that the counter is able to discern. Values are 1 ~ 4. Use DetectableParallelUnitCount to interpret the values of the VERTEX_PROCESSING, GEOMETRY_PROCESSING, PIXEL_PROCESSING, and OTHER_GPU_PROCESSING counters.
    /// <para>(Also see DirectX SDK: D3D11_COUNTER_INFO.NumDetectableParallelUnits)</para>
    /// </summary>;
    property Byte DetectableParallelUnitCount
    {
        Byte get()
        {
            return numDetectableParallelUnits;
        }

        void set(Byte value)
        {
            numDetectableParallelUnits = value;
        }
    }

private:

    Direct3D11::Counter lastDeviceDependentCounter;
    UInt32 numSimultaneousCounters;
    Byte numDetectableParallelUnits;

internal: 
    CounterInformation(const D3D11_COUNTER_INFO& info)
    {
        LastDeviceDependentCounter = static_cast<Counter>(info.LastDeviceDependentCounter);
        SimultaneousCounterCount = info.NumSimultaneousCounters;
        DetectableParallelUnitCount = info.NumDetectableParallelUnits;
    }
public:

    static Boolean operator == (CounterInformation counterInformation1, CounterInformation counterInformation2)
    {
        return (counterInformation1.lastDeviceDependentCounter == counterInformation2.lastDeviceDependentCounter) &&
            (counterInformation1.numSimultaneousCounters == counterInformation2.numSimultaneousCounters) &&
            (counterInformation1.numDetectableParallelUnits == counterInformation2.numDetectableParallelUnits);
    }

    static Boolean operator != (CounterInformation counterInformation1, CounterInformation counterInformation2)
    {
        return !(counterInformation1 == counterInformation2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != CounterInformation::typeid)
        {
            return false;
        }

        return *this == safe_cast<CounterInformation>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + lastDeviceDependentCounter.GetHashCode();
        hashCode = hashCode * 31 + numSimultaneousCounters.GetHashCode();
        hashCode = hashCode * 31 + numDetectableParallelUnits.GetHashCode();

        return hashCode;
    }

};



/// <summary>
/// Stencil operations that can be performed based on the results of stencil test.
/// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCILOP_DESC)</para>
/// </summary>
public value struct DepthStencilOperationDescription
{
public:
    DepthStencilOperationDescription(const D3D11_DEPTH_STENCILOP_DESC& desc)
    {
        StencilFailOperation = static_cast<StencilOperation>(desc.StencilFailOp);
        StencilDepthFailOperation = static_cast<StencilOperation>(desc.StencilDepthFailOp);
        StencilPassOperation = static_cast<StencilOperation>(desc.StencilPassOp);
        StencilFunction = static_cast<ComparisonFunction>(desc.StencilFunc);
    }

    /// <summary>
    /// The stencil operation to perform when stencil testing fails.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCILOP_DESC.StencilFailOp)</para>
    /// </summary>
    property StencilOperation StencilFailOperation
    {
        StencilOperation get()
        {
            return stencilFailOperation;
        }

        void set(StencilOperation value)
        {
            stencilFailOperation = value;
        }
    }
    /// <summary>
    /// The stencil operation to perform when stencil testing passes and depth testing fails.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCILOP_DESC.StencilDepthFailOp)</para>
    /// </summary>
    property StencilOperation StencilDepthFailOperation
    {
        StencilOperation get()
        {
            return stencilDepthFailOperation;
        }

        void set(StencilOperation value)
        {
            stencilDepthFailOperation = value;
        }
    }
    /// <summary>
    /// The stencil operation to perform when stencil testing and depth testing both pass.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCILOP_DESC.StencilPassOp)</para>
    /// </summary>
    property StencilOperation StencilPassOperation
    {
        StencilOperation get()
        {
            return stencilPassOperation;
        }

        void set(StencilOperation value)
        {
            stencilPassOperation = value;
        }
    }
    /// <summary>
    /// A function that compares stencil data against existing stencil data. The function options are listed in ComparisonFunction.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCILOP_DESC.StencilFunc)</para>
    /// </summary>
    property ComparisonFunction StencilFunction
    {
        ComparisonFunction get()
        {
            return stencilFunction;
        }

        void set(ComparisonFunction value)
        {
            stencilFunction = value;
        }
    }
private:

    StencilOperation stencilFailOperation;
    StencilOperation stencilDepthFailOperation;
    StencilOperation stencilPassOperation;
    ComparisonFunction stencilFunction;

public:

    static Boolean operator == (DepthStencilOperationDescription depthStencilOperationDescription1, DepthStencilOperationDescription depthStencilOperationDescription2)
    {
        return (depthStencilOperationDescription1.stencilFailOperation == depthStencilOperationDescription2.stencilFailOperation) &&
            (depthStencilOperationDescription1.stencilDepthFailOperation == depthStencilOperationDescription2.stencilDepthFailOperation) &&
            (depthStencilOperationDescription1.stencilPassOperation == depthStencilOperationDescription2.stencilPassOperation) &&
            (depthStencilOperationDescription1.stencilFunction == depthStencilOperationDescription2.stencilFunction);
    }

    static Boolean operator != (DepthStencilOperationDescription depthStencilOperationDescription1, DepthStencilOperationDescription depthStencilOperationDescription2)
    {
        return !(depthStencilOperationDescription1 == depthStencilOperationDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != DepthStencilOperationDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<DepthStencilOperationDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + stencilFailOperation.GetHashCode();
        hashCode = hashCode * 31 + stencilDepthFailOperation.GetHashCode();
        hashCode = hashCode * 31 + stencilPassOperation.GetHashCode();
        hashCode = hashCode * 31 + stencilFunction.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from an array of 1D textures to use in a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_DSV)</para>
/// </summary>
public value struct Texture1DArrayDepthStencilView
{
public:
    /// <summary>
    /// The index of the first mipmap level to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_DSV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
    /// <summary>
    /// The index of the first texture to use in an array of textures.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_DSV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// Number of textures to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_DSV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 mipSlice;
    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture1DArrayDepthStencilView textureArrayDepthStencilView1, Texture1DArrayDepthStencilView textureArrayDepthStencilView2)
    {
        return (textureArrayDepthStencilView1.mipSlice == textureArrayDepthStencilView2.mipSlice) &&
            (textureArrayDepthStencilView1.firstArraySlice == textureArrayDepthStencilView2.firstArraySlice) &&
            (textureArrayDepthStencilView1.arraySize == textureArrayDepthStencilView2.arraySize);
    }

    static Boolean operator != (Texture1DArrayDepthStencilView textureArrayDepthStencilView1, Texture1DArrayDepthStencilView textureArrayDepthStencilView2)
    {
        return !(textureArrayDepthStencilView1 == textureArrayDepthStencilView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture1DArrayDepthStencilView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture1DArrayDepthStencilView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();
        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from an array of 1D textures to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_RTV)</para>
/// </summary>
public value struct Texture1DArrayRenderTargetView
{
public:
    /// <summary>
    /// The index of the mipmap level to use mip slice.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_RTV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
    /// <summary>
    /// The index of the first texture to use in an array of textures.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_RTV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// Number of textures to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_RTV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 mipSlice;
    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture1DArrayRenderTargetView textureArrayRenderTargetView1, Texture1DArrayRenderTargetView textureArrayRenderTargetView2)
    {
        return (textureArrayRenderTargetView1.mipSlice == textureArrayRenderTargetView2.mipSlice) &&
            (textureArrayRenderTargetView1.firstArraySlice == textureArrayRenderTargetView2.firstArraySlice) &&
            (textureArrayRenderTargetView1.arraySize == textureArrayRenderTargetView2.arraySize);
    }

    static Boolean operator != (Texture1DArrayRenderTargetView textureArrayRenderTargetView1, Texture1DArrayRenderTargetView textureArrayRenderTargetView2)
    {
        return !(textureArrayRenderTargetView1 == textureArrayRenderTargetView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture1DArrayRenderTargetView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture1DArrayRenderTargetView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();
        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from an array of 1D textures to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_SRV)</para>
/// </summary>
public value struct Texture1DArrayShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_SRV.MostDetailedMip)</para>
    /// </summary>
    property UInt32 MostDetailedMip
    {
        UInt32 get()
        {
            return mostDetailedMip;
        }

        void set(UInt32 value)
        {
            mostDetailedMip = value;
        }
    }
    /// <summary>
    /// The maximum number of mipmap levels in the texture. See the remarks in Texture1DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_SRV.MipLevels)</para>
    /// </summary>
    property UInt32 MipLevels
    {
        UInt32 get()
        {
            return mipLevels;
        }

        void set(UInt32 value)
        {
            mipLevels = value;
        }
    }
    /// <summary>
    /// The index of the first texture to use in an array of textures.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_SRV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// Number of textures in the array.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_SRV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 mostDetailedMip;
    UInt32 mipLevels;
    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture1DArrayShaderResourceView textureArrayShaderResourceView1, Texture1DArrayShaderResourceView textureArrayShaderResourceView2)
    {
        return (textureArrayShaderResourceView1.mostDetailedMip == textureArrayShaderResourceView2.mostDetailedMip) &&
            (textureArrayShaderResourceView1.mipLevels == textureArrayShaderResourceView2.mipLevels) &&
            (textureArrayShaderResourceView1.firstArraySlice == textureArrayShaderResourceView2.firstArraySlice) &&
            (textureArrayShaderResourceView1.arraySize == textureArrayShaderResourceView2.arraySize);
    }

    static Boolean operator != (Texture1DArrayShaderResourceView textureArrayShaderResourceView1, Texture1DArrayShaderResourceView textureArrayShaderResourceView2)
    {
        return !(textureArrayShaderResourceView1 == textureArrayShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture1DArrayShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture1DArrayShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mostDetailedMip.GetHashCode();
        hashCode = hashCode * 31 + mipLevels.GetHashCode();
        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes an array of unordered-access 1D texture resources.
/// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_UAV)</para>
/// </summary>
public value struct Texture1DArrayUnorderedAccessView
{
public:
    /// <summary>
    /// The mipmap slice index.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_UAV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
    /// <summary>
    /// The zero-based index of the first array slice to be accessed.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_UAV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// The number of slices in the array.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_ARRAY_UAV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 mipSlice;
    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture1DArrayUnorderedAccessView textureArrayUnorderedAccessView1, Texture1DArrayUnorderedAccessView textureArrayUnorderedAccessView2)
    {
        return (textureArrayUnorderedAccessView1.mipSlice == textureArrayUnorderedAccessView2.mipSlice) &&
            (textureArrayUnorderedAccessView1.firstArraySlice == textureArrayUnorderedAccessView2.firstArraySlice) &&
            (textureArrayUnorderedAccessView1.arraySize == textureArrayUnorderedAccessView2.arraySize);
    }

    static Boolean operator != (Texture1DArrayUnorderedAccessView textureArrayUnorderedAccessView1, Texture1DArrayUnorderedAccessView textureArrayUnorderedAccessView2)
    {
        return !(textureArrayUnorderedAccessView1 == textureArrayUnorderedAccessView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture1DArrayUnorderedAccessView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture1DArrayUnorderedAccessView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();
        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresource from a 1D texture that is accessable to a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D11_TEX1D_DSV)</para>
/// </summary>
public value struct Texture1DDepthStencilView
{
public:
    /// <summary>
    /// The index of the first mipmap level to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_DSV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
private:

    UInt32 mipSlice;

public:

    static Boolean operator == (Texture1DDepthStencilView textureDepthStencilView1, Texture1DDepthStencilView textureDepthStencilView2)
    {
        return (textureDepthStencilView1.mipSlice == textureDepthStencilView2.mipSlice);
    }

    static Boolean operator != (Texture1DDepthStencilView textureDepthStencilView1, Texture1DDepthStencilView textureDepthStencilView2)
    {
        return !(textureDepthStencilView1 == textureDepthStencilView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture1DDepthStencilView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture1DDepthStencilView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresource from a 1D texture to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D11_TEX1D_RTV)</para>
/// </summary>
public value struct Texture1DRenderTargetView
{
public:
    /// <summary>
    /// The index of the mipmap level to use mip slice.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_RTV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
private:

    UInt32 mipSlice;

public:

    static Boolean operator == (Texture1DRenderTargetView textureRenderTargetView1, Texture1DRenderTargetView textureRenderTargetView2)
    {
        return (textureRenderTargetView1.mipSlice == textureRenderTargetView2.mipSlice);
    }

    static Boolean operator != (Texture1DRenderTargetView textureRenderTargetView1, Texture1DRenderTargetView textureRenderTargetView2)
    {
        return !(textureRenderTargetView1 == textureRenderTargetView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture1DRenderTargetView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture1DRenderTargetView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresource from a 1D texture to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_TEX1D_SRV)</para>
/// </summary>
public value struct Texture1DShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_SRV.MostDetailedMip)</para>
    /// </summary>
    property UInt32 MostDetailedMip
    {
        UInt32 get()
        {
            return mostDetailedMip;
        }

        void set(UInt32 value)
        {
            mostDetailedMip = value;
        }
    }
    /// <summary>
    /// The maximum number of mipmap levels in the texture. See the remarks in Texture1DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_SRV.MipLevels)</para>
    /// </summary>
    property UInt32 MipLevels
    {
        UInt32 get()
        {
            return mipLevels;
        }

        void set(UInt32 value)
        {
            mipLevels = value;
        }
    }
private:

    UInt32 mostDetailedMip;
    UInt32 mipLevels;

public:

    static Boolean operator == (Texture1DShaderResourceView textureShaderResourceView1, Texture1DShaderResourceView textureShaderResourceView2)
    {
        return (textureShaderResourceView1.mostDetailedMip == textureShaderResourceView2.mostDetailedMip) &&
            (textureShaderResourceView1.mipLevels == textureShaderResourceView2.mipLevels);
    }

    static Boolean operator != (Texture1DShaderResourceView textureShaderResourceView1, Texture1DShaderResourceView textureShaderResourceView2)
    {
        return !(textureShaderResourceView1 == textureShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture1DShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture1DShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mostDetailedMip.GetHashCode();
        hashCode = hashCode * 31 + mipLevels.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a unordered-access 1D texture resource.
/// <para>(Also see DirectX SDK: D3D11_TEX1D_UAV)</para>
/// </summary>
public value struct Texture1DUnorderedAccessView
{
public:
    /// <summary>
    /// The mipmap slice index.
    /// <para>(Also see DirectX SDK: D3D11_TEX1D_UAV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
private:

    UInt32 mipSlice;

public:

    static Boolean operator == (Texture1DUnorderedAccessView textureUnorderedAccessView1, Texture1DUnorderedAccessView textureUnorderedAccessView2)
    {
        return (textureUnorderedAccessView1.mipSlice == textureUnorderedAccessView2.mipSlice);
    }

    static Boolean operator != (Texture1DUnorderedAccessView textureUnorderedAccessView1, Texture1DUnorderedAccessView textureUnorderedAccessView2)
    {
        return !(textureUnorderedAccessView1 == textureUnorderedAccessView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture1DUnorderedAccessView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture1DUnorderedAccessView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from an array of multisampled 2D textures for a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D11_TEX2DMS_ARRAY_DSV)</para>
/// </summary>
public value struct Texture2DMultisampleArrayDepthStencilView
{
public:
    /// <summary>
    /// The index of the first texture to use in an array of textures.
    /// <para>(Also see DirectX SDK: D3D11_TEX2DMS_ARRAY_DSV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// Number of textures to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX2DMS_ARRAY_DSV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture2DMultisampleArrayDepthStencilView textureMultisampleArrayDepthStencilView1, Texture2DMultisampleArrayDepthStencilView textureMultisampleArrayDepthStencilView2)
    {
        return (textureMultisampleArrayDepthStencilView1.firstArraySlice == textureMultisampleArrayDepthStencilView2.firstArraySlice) &&
            (textureMultisampleArrayDepthStencilView1.arraySize == textureMultisampleArrayDepthStencilView2.arraySize);
    }

    static Boolean operator != (Texture2DMultisampleArrayDepthStencilView textureMultisampleArrayDepthStencilView1, Texture2DMultisampleArrayDepthStencilView textureMultisampleArrayDepthStencilView2)
    {
        return !(textureMultisampleArrayDepthStencilView1 == textureMultisampleArrayDepthStencilView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DMultisampleArrayDepthStencilView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DMultisampleArrayDepthStencilView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from a an array of multisampled 2D textures to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D11_TEX2DMS_ARRAY_RTV)</para>
/// </summary>
public value struct Texture2DMultisampleArrayRenderTargetView
{
public:
    /// <summary>
    /// The index of the first texture to use in an array of textures.
    /// <para>(Also see DirectX SDK: D3D11_TEX2DMS_ARRAY_RTV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// Number of textures to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX2DMS_ARRAY_RTV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture2DMultisampleArrayRenderTargetView textureMultisampleArrayRenderTargetView1, Texture2DMultisampleArrayRenderTargetView textureMultisampleArrayRenderTargetView2)
    {
        return (textureMultisampleArrayRenderTargetView1.firstArraySlice == textureMultisampleArrayRenderTargetView2.firstArraySlice) &&
            (textureMultisampleArrayRenderTargetView1.arraySize == textureMultisampleArrayRenderTargetView2.arraySize);
    }

    static Boolean operator != (Texture2DMultisampleArrayRenderTargetView textureMultisampleArrayRenderTargetView1, Texture2DMultisampleArrayRenderTargetView textureMultisampleArrayRenderTargetView2)
    {
        return !(textureMultisampleArrayRenderTargetView1 == textureMultisampleArrayRenderTargetView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DMultisampleArrayRenderTargetView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DMultisampleArrayRenderTargetView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from an array of multisampled 2D textures to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_TEX2DMS_ARRAY_SRV)</para>
/// </summary>
public value struct Texture2DMultisampleArrayShaderResourceView
{
public:
    /// <summary>
    /// The index of the first texture to use in an array of textures.
    /// <para>(Also see DirectX SDK: D3D11_TEX2DMS_ARRAY_SRV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// Number of textures to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX2DMS_ARRAY_SRV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture2DMultisampleArrayShaderResourceView textureMultisampleArrayShaderResourceView1, Texture2DMultisampleArrayShaderResourceView textureMultisampleArrayShaderResourceView2)
    {
        return (textureMultisampleArrayShaderResourceView1.firstArraySlice == textureMultisampleArrayShaderResourceView2.firstArraySlice) &&
            (textureMultisampleArrayShaderResourceView1.arraySize == textureMultisampleArrayShaderResourceView2.arraySize);
    }

    static Boolean operator != (Texture2DMultisampleArrayShaderResourceView textureMultisampleArrayShaderResourceView1, Texture2DMultisampleArrayShaderResourceView textureMultisampleArrayShaderResourceView2)
    {
        return !(textureMultisampleArrayShaderResourceView1 == textureMultisampleArrayShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DMultisampleArrayShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DMultisampleArrayShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresource from a multisampled 2D texture that is accessable to a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D11_TEX2DMS_DSV)</para>
/// </summary>
public value struct Texture2DMultisampleDepthStencilView
{
public:
    // REVIEW: probably don't need to expose the field at all; remove property
    // and leave field private?

    /// <summary>
    /// Unused.
    /// <para>(Also see DirectX SDK: D3D11_TEX2DMS_DSV.UnusedField_NothingToDefine)</para>
    /// </summary>
    property UInt32 UnusedField
    {
        UInt32 get()
        {
            return unusedField;
        }

        void set(UInt32 value)
        {
            unusedField = value;
        }
    }
private:

    UInt32 unusedField;

public:

    static Boolean operator == (Texture2DMultisampleDepthStencilView textureMultisampleDepthStencilView1, Texture2DMultisampleDepthStencilView textureMultisampleDepthStencilView2)
    {
        return (textureMultisampleDepthStencilView1.unusedField == textureMultisampleDepthStencilView2.unusedField);
    }

    static Boolean operator != (Texture2DMultisampleDepthStencilView textureMultisampleDepthStencilView1, Texture2DMultisampleDepthStencilView textureMultisampleDepthStencilView2)
    {
        return !(textureMultisampleDepthStencilView1 == textureMultisampleDepthStencilView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DMultisampleDepthStencilView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DMultisampleDepthStencilView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + unusedField.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresource from a multisampled 2D texture to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D11_TEX2DMS_RTV)</para>
/// </summary>
public value struct Texture2DMultisampleRenderTargetView
{
public:
    // REVIEW: probably don't need to expose the field at all; remove property
    // and leave field private?

    /// <summary>
    /// Integer of any value.
    /// <para>(Also see DirectX SDK: D3D11_TEX2DMS_RTV.UnusedField_NothingToDefine)</para>
    /// </summary>
    property UInt32 UnusedField
    {
        UInt32 get()
        {
            return unusedField;
        }

        void set(UInt32 value)
        {
            unusedField = value;
        }
    }
private:

    UInt32 unusedField;

public:

    static Boolean operator == (Texture2DMultisampleRenderTargetView textureMultisampleRenderTargetView1, Texture2DMultisampleRenderTargetView textureMultisampleRenderTargetView2)
    {
        return (textureMultisampleRenderTargetView1.unusedField == textureMultisampleRenderTargetView2.unusedField);
    }

    static Boolean operator != (Texture2DMultisampleRenderTargetView textureMultisampleRenderTargetView1, Texture2DMultisampleRenderTargetView textureMultisampleRenderTargetView2)
    {
        return !(textureMultisampleRenderTargetView1 == textureMultisampleRenderTargetView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DMultisampleRenderTargetView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DMultisampleRenderTargetView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + unusedField.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from a multisampled 2D texture to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_TEX2DMS_SRV)</para>
/// </summary>
public value struct Texture2DMultisampleShaderResourceView
{
public:
    // REVIEW: probably don't need to expose the field at all; remove property
    // and leave field private?

    /// <summary>
    /// Integer of any value.
    /// <para>(Also see DirectX SDK: D3D11_TEX2DMS_SRV.UnusedField_NothingToDefine)</para>
    /// </summary>
    property UInt32 UnusedField
    {
        UInt32 get()
        {
            return unusedField;
        }

        void set(UInt32 value)
        {
            unusedField = value;
        }
    }
private:

    UInt32 unusedField;

public:

    static Boolean operator == (Texture2DMultisampleShaderResourceView textureMultisampleShaderResourceView1, Texture2DMultisampleShaderResourceView textureMultisampleShaderResourceView2)
    {
        return (textureMultisampleShaderResourceView1.unusedField == textureMultisampleShaderResourceView2.unusedField);
    }

    static Boolean operator != (Texture2DMultisampleShaderResourceView textureMultisampleShaderResourceView1, Texture2DMultisampleShaderResourceView textureMultisampleShaderResourceView2)
    {
        return !(textureMultisampleShaderResourceView1 == textureMultisampleShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DMultisampleShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DMultisampleShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + unusedField.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from an array 2D textures that are accessable to a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_DSV)</para>
/// </summary>
public value struct Texture2DArrayDepthStencilView
{
public:
    /// <summary>
    /// The index of the first mipmap level to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_DSV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
    /// <summary>
    /// The index of the first texture to use in an array of textures.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_DSV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// Number of textures to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_DSV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 mipSlice;
    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture2DArrayDepthStencilView textureArrayDepthStencilView1, Texture2DArrayDepthStencilView textureArrayDepthStencilView2)
    {
        return (textureArrayDepthStencilView1.mipSlice == textureArrayDepthStencilView2.mipSlice) &&
            (textureArrayDepthStencilView1.firstArraySlice == textureArrayDepthStencilView2.firstArraySlice) &&
            (textureArrayDepthStencilView1.arraySize == textureArrayDepthStencilView2.arraySize);
    }

    static Boolean operator != (Texture2DArrayDepthStencilView textureArrayDepthStencilView1, Texture2DArrayDepthStencilView textureArrayDepthStencilView2)
    {
        return !(textureArrayDepthStencilView1 == textureArrayDepthStencilView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DArrayDepthStencilView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DArrayDepthStencilView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();
        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from an array of 2D textures to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_RTV)</para>
/// </summary>
public value struct Texture2DArrayRenderTargetView
{
public:
    /// <summary>
    /// The index of the mipmap level to use mip slice.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_RTV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
    /// <summary>
    /// The index of the first texture to use in an array of textures.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_RTV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// Number of textures in the array to use in the render target view, starting from FirstArraySlice.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_RTV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 mipSlice;
    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture2DArrayRenderTargetView textureArrayRenderTargetView1, Texture2DArrayRenderTargetView textureArrayRenderTargetView2)
    {
        return (textureArrayRenderTargetView1.mipSlice == textureArrayRenderTargetView2.mipSlice) &&
            (textureArrayRenderTargetView1.firstArraySlice == textureArrayRenderTargetView2.firstArraySlice) &&
            (textureArrayRenderTargetView1.arraySize == textureArrayRenderTargetView2.arraySize);
    }

    static Boolean operator != (Texture2DArrayRenderTargetView textureArrayRenderTargetView1, Texture2DArrayRenderTargetView textureArrayRenderTargetView2)
    {
        return !(textureArrayRenderTargetView1 == textureArrayRenderTargetView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DArrayRenderTargetView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DArrayRenderTargetView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();
        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from an array of 2D textures to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_SRV)</para>
/// </summary>
public value struct Texture2DArrayShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_SRV.MostDetailedMip)</para>
    /// </summary>
    property UInt32 MostDetailedMip
    {
        UInt32 get()
        {
            return mostDetailedMip;
        }

        void set(UInt32 value)
        {
            mostDetailedMip = value;
        }
    }
    /// <summary>
    /// The maximum number of mipmap levels in the texture. See the remarks in Texture1DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_SRV.MipLevels)</para>
    /// </summary>
    property UInt32 MipLevels
    {
        UInt32 get()
        {
            return mipLevels;
        }

        void set(UInt32 value)
        {
            mipLevels = value;
        }
    }
    /// <summary>
    /// The index of the first texture to use in an array of textures.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_SRV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// Number of textures in the array.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_SRV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 mostDetailedMip;
    UInt32 mipLevels;
    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture2DArrayShaderResourceView textureArrayShaderResourceView1, Texture2DArrayShaderResourceView textureArrayShaderResourceView2)
    {
        return (textureArrayShaderResourceView1.mostDetailedMip == textureArrayShaderResourceView2.mostDetailedMip) &&
            (textureArrayShaderResourceView1.mipLevels == textureArrayShaderResourceView2.mipLevels) &&
            (textureArrayShaderResourceView1.firstArraySlice == textureArrayShaderResourceView2.firstArraySlice) &&
            (textureArrayShaderResourceView1.arraySize == textureArrayShaderResourceView2.arraySize);
    }

    static Boolean operator != (Texture2DArrayShaderResourceView textureArrayShaderResourceView1, Texture2DArrayShaderResourceView textureArrayShaderResourceView2)
    {
        return !(textureArrayShaderResourceView1 == textureArrayShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DArrayShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DArrayShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mostDetailedMip.GetHashCode();
        hashCode = hashCode * 31 + mipLevels.GetHashCode();
        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes an array of unordered-access 2D texture resources.
/// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_UAV)</para>
/// </summary>
public value struct Texture2DArrayUnorderedAccessView
{
public:
    /// <summary>
    /// The mipmap slice index.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_UAV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
    /// <summary>
    /// The zero-based index of the first array slice to be accessed.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_UAV.FirstArraySlice)</para>
    /// </summary>
    property UInt32 FirstArraySlice
    {
        UInt32 get()
        {
            return firstArraySlice;
        }

        void set(UInt32 value)
        {
            firstArraySlice = value;
        }
    }
    /// <summary>
    /// The number of slices in the array.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_ARRAY_UAV.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
private:

    UInt32 mipSlice;
    UInt32 firstArraySlice;
    UInt32 arraySize;

public:

    static Boolean operator == (Texture2DArrayUnorderedAccessView textureArrayUnorderedAccessView1, Texture2DArrayUnorderedAccessView textureArrayUnorderedAccessView2)
    {
        return (textureArrayUnorderedAccessView1.mipSlice == textureArrayUnorderedAccessView2.mipSlice) &&
            (textureArrayUnorderedAccessView1.firstArraySlice == textureArrayUnorderedAccessView2.firstArraySlice) &&
            (textureArrayUnorderedAccessView1.arraySize == textureArrayUnorderedAccessView2.arraySize);
    }

    static Boolean operator != (Texture2DArrayUnorderedAccessView textureArrayUnorderedAccessView1, Texture2DArrayUnorderedAccessView textureArrayUnorderedAccessView2)
    {
        return !(textureArrayUnorderedAccessView1 == textureArrayUnorderedAccessView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DArrayUnorderedAccessView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DArrayUnorderedAccessView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();
        hashCode = hashCode * 31 + firstArraySlice.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresource from a 2D texture that is accessable to a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D11_TEX2D_DSV)</para>
/// </summary>
public value struct Texture2DDepthStencilView
{
public:
    /// <summary>
    /// The index of the first mipmap level to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_DSV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
private:

    UInt32 mipSlice;

public:

    static Boolean operator == (Texture2DDepthStencilView textureDepthStencilView1, Texture2DDepthStencilView textureDepthStencilView2)
    {
        return (textureDepthStencilView1.mipSlice == textureDepthStencilView2.mipSlice);
    }

    static Boolean operator != (Texture2DDepthStencilView textureDepthStencilView1, Texture2DDepthStencilView textureDepthStencilView2)
    {
        return !(textureDepthStencilView1 == textureDepthStencilView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DDepthStencilView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DDepthStencilView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresource from a 2D texture to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D11_TEX2D_RTV)</para>
/// </summary>
public value struct Texture2DRenderTargetView
{
public:
    /// <summary>
    /// The index of the mipmap level to use mip slice.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_RTV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
private:

    UInt32 mipSlice;

public:

    static Boolean operator == (Texture2DRenderTargetView textureRenderTargetView1, Texture2DRenderTargetView textureRenderTargetView2)
    {
        return (textureRenderTargetView1.mipSlice == textureRenderTargetView2.mipSlice);
    }

    static Boolean operator != (Texture2DRenderTargetView textureRenderTargetView1, Texture2DRenderTargetView textureRenderTargetView2)
    {
        return !(textureRenderTargetView1 == textureRenderTargetView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DRenderTargetView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DRenderTargetView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresource from a 2D texture to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_TEX2D_SRV)</para>
/// </summary>
public value struct Texture2DShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_SRV.MostDetailedMip)</para>
    /// </summary>
    property UInt32 MostDetailedMip
    {
        UInt32 get()
        {
            return mostDetailedMip;
        }

        void set(UInt32 value)
        {
            mostDetailedMip = value;
        }
    }
    /// <summary>
    /// The maximum number of mipmap levels in the texture. See the remarks in Texture1DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_SRV.MipLevels)</para>
    /// </summary>
    property UInt32 MipLevels
    {
        UInt32 get()
        {
            return mipLevels;
        }

        void set(UInt32 value)
        {
            mipLevels = value;
        }
    }
private:

    UInt32 mostDetailedMip;
    UInt32 mipLevels;

public:

    static Boolean operator == (Texture2DShaderResourceView textureShaderResourceView1, Texture2DShaderResourceView textureShaderResourceView2)
    {
        return (textureShaderResourceView1.mostDetailedMip == textureShaderResourceView2.mostDetailedMip) &&
            (textureShaderResourceView1.mipLevels == textureShaderResourceView2.mipLevels);
    }

    static Boolean operator != (Texture2DShaderResourceView textureShaderResourceView1, Texture2DShaderResourceView textureShaderResourceView2)
    {
        return !(textureShaderResourceView1 == textureShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mostDetailedMip.GetHashCode();
        hashCode = hashCode * 31 + mipLevels.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a unordered-access 2D texture resource.
/// <para>(Also see DirectX SDK: D3D11_TEX2D_UAV)</para>
/// </summary>
public value struct Texture2DUnorderedAccessView
{
public:
    /// <summary>
    /// The mipmap slice index.
    /// <para>(Also see DirectX SDK: D3D11_TEX2D_UAV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
private:

    UInt32 mipSlice;

public:

    static Boolean operator == (Texture2DUnorderedAccessView textureUnorderedAccessView1, Texture2DUnorderedAccessView textureUnorderedAccessView2)
    {
        return (textureUnorderedAccessView1.mipSlice == textureUnorderedAccessView2.mipSlice);
    }

    static Boolean operator != (Texture2DUnorderedAccessView textureUnorderedAccessView1, Texture2DUnorderedAccessView textureUnorderedAccessView2)
    {
        return !(textureUnorderedAccessView1 == textureUnorderedAccessView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DUnorderedAccessView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DUnorderedAccessView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from a 3D texture to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D11_TEX3D_RTV)</para>
/// </summary>
public value struct Texture3DRenderTargetView
{
public:
    /// <summary>
    /// The index of the mipmap level to use mip slice.
    /// <para>(Also see DirectX SDK: D3D11_TEX3D_RTV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
    /// <summary>
    /// First depth level to use.
    /// <para>(Also see DirectX SDK: D3D11_TEX3D_RTV.FirstWSlice)</para>
    /// </summary>
    property UInt32 FirstWSlice
    {
        UInt32 get()
        {
            return firstWSlice;
        }

        void set(UInt32 value)
        {
            firstWSlice = value;
        }
    }
    /// <summary>
    /// Number of depth levels to use in the render-target view, starting from FirstWSlice. A value of -1 indicates all of the slices along the w axis, starting from FirstWSlice.
    /// <para>(Also see DirectX SDK: D3D11_TEX3D_RTV.WSize)</para>
    /// </summary>
    property UInt32 WSize
    {
        UInt32 get()
        {
            return wSize;
        }

        void set(UInt32 value)
        {
            wSize = value;
        }
    }
private:

    UInt32 mipSlice;
    UInt32 firstWSlice;
    UInt32 wSize;

public:

    static Boolean operator == (Texture3DRenderTargetView textureRenderTargetView1, Texture3DRenderTargetView textureRenderTargetView2)
    {
        return (textureRenderTargetView1.mipSlice == textureRenderTargetView2.mipSlice) &&
            (textureRenderTargetView1.firstWSlice == textureRenderTargetView2.firstWSlice) &&
            (textureRenderTargetView1.wSize == textureRenderTargetView2.wSize);
    }

    static Boolean operator != (Texture3DRenderTargetView textureRenderTargetView1, Texture3DRenderTargetView textureRenderTargetView2)
    {
        return !(textureRenderTargetView1 == textureRenderTargetView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture3DRenderTargetView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture3DRenderTargetView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();
        hashCode = hashCode * 31 + firstWSlice.GetHashCode();
        hashCode = hashCode * 31 + wSize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from a 3D texture to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_TEX3D_SRV)</para>
/// </summary>
public value struct Texture3DShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D11_TEX3D_SRV.MostDetailedMip)</para>
    /// </summary>
    property UInt32 MostDetailedMip
    {
        UInt32 get()
        {
            return mostDetailedMip;
        }

        void set(UInt32 value)
        {
            mostDetailedMip = value;
        }
    }
    /// <summary>
    /// The maximum number of mipmap levels in the texture. See the remarks in Texture1DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D11_TEX3D_SRV.MipLevels)</para>
    /// </summary>
    property UInt32 MipLevels
    {
        UInt32 get()
        {
            return mipLevels;
        }

        void set(UInt32 value)
        {
            mipLevels = value;
        }
    }
private:

    UInt32 mostDetailedMip;
    UInt32 mipLevels;

public:

    static Boolean operator == (Texture3DShaderResourceView textureShaderResourceView1, Texture3DShaderResourceView textureShaderResourceView2)
    {
        return (textureShaderResourceView1.mostDetailedMip == textureShaderResourceView2.mostDetailedMip) &&
            (textureShaderResourceView1.mipLevels == textureShaderResourceView2.mipLevels);
    }

    static Boolean operator != (Texture3DShaderResourceView textureShaderResourceView1, Texture3DShaderResourceView textureShaderResourceView2)
    {
        return !(textureShaderResourceView1 == textureShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture3DShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture3DShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mostDetailedMip.GetHashCode();
        hashCode = hashCode * 31 + mipLevels.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a unordered-access 3D texture resource.
/// <para>(Also see DirectX SDK: D3D11_TEX3D_UAV)</para>
/// </summary>
public value struct Texture3DUnorderedAccessView
{
public:
    /// <summary>
    /// The mipmap slice index.
    /// <para>(Also see DirectX SDK: D3D11_TEX3D_UAV.MipSlice)</para>
    /// </summary>
    property UInt32 MipSlice
    {
        UInt32 get()
        {
            return mipSlice;
        }

        void set(UInt32 value)
        {
            mipSlice = value;
        }
    }
    /// <summary>
    /// The zero-based index of the first depth slice to be accessed.
    /// <para>(Also see DirectX SDK: D3D11_TEX3D_UAV.FirstWSlice)</para>
    /// </summary>
    property UInt32 FirstWSlice
    {
        UInt32 get()
        {
            return firstWSlice;
        }

        void set(UInt32 value)
        {
            firstWSlice = value;
        }
    }
    /// <summary>
    /// The number of depth slices.
    /// <para>(Also see DirectX SDK: D3D11_TEX3D_UAV.WSize)</para>
    /// </summary>
    property UInt32 WSize
    {
        UInt32 get()
        {
            return wSize;
        }

        void set(UInt32 value)
        {
            wSize = value;
        }
    }
private:

    UInt32 mipSlice;
    UInt32 firstWSlice;
    UInt32 wSize;

public:

    static Boolean operator == (Texture3DUnorderedAccessView textureUnorderedAccessView1, Texture3DUnorderedAccessView textureUnorderedAccessView2)
    {
        return (textureUnorderedAccessView1.mipSlice == textureUnorderedAccessView2.mipSlice) &&
            (textureUnorderedAccessView1.firstWSlice == textureUnorderedAccessView2.firstWSlice) &&
            (textureUnorderedAccessView1.wSize == textureUnorderedAccessView2.wSize);
    }

    static Boolean operator != (Texture3DUnorderedAccessView textureUnorderedAccessView1, Texture3DUnorderedAccessView textureUnorderedAccessView2)
    {
        return !(textureUnorderedAccessView1 == textureUnorderedAccessView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture3DUnorderedAccessView::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture3DUnorderedAccessView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mipSlice.GetHashCode();
        hashCode = hashCode * 31 + firstWSlice.GetHashCode();
        hashCode = hashCode * 31 + wSize.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresources from an array of cube textures to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_TEXCUBE_ARRAY_SRV)</para>
/// </summary>
public value struct TextureCubeArrayShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D11_TEXCUBE_ARRAY_SRV.MostDetailedMip)</para>
    /// </summary>
    property UInt32 MostDetailedMip
    {
        UInt32 get()
        {
            return mostDetailedMip;
        }

        void set(UInt32 value)
        {
            mostDetailedMip = value;
        }
    }
    /// <summary>
    /// The maximum number of mipmap levels in the texture. See the remarks in Texture1DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D11_TEXCUBE_ARRAY_SRV.MipLevels)</para>
    /// </summary>
    property UInt32 MipLevels
    {
        UInt32 get()
        {
            return mipLevels;
        }

        void set(UInt32 value)
        {
            mipLevels = value;
        }
    }
    /// <summary>
    /// Index of the first 2D texture to use.
    /// <para>(Also see DirectX SDK: D3D11_TEXCUBE_ARRAY_SRV.First2DArrayFace)</para>
    /// </summary>
    property UInt32 First2DArrayFace
    {
        UInt32 get()
        {
            return first2DArrayFace;
        }

        void set(UInt32 value)
        {
            first2DArrayFace = value;
        }
    }
    /// <summary>
    /// Number of cube textures in the array.
    /// <para>(Also see DirectX SDK: D3D11_TEXCUBE_ARRAY_SRV.NumCubes)</para>
    /// </summary>
    property UInt32 CubeCount
    {
        UInt32 get()
        {
            return numCubes;
        }

        void set(UInt32 value)
        {
            numCubes = value;
        }
    }
private:

    UInt32 mostDetailedMip;
    UInt32 mipLevels;
    UInt32 first2DArrayFace;
    UInt32 numCubes;

public:

    static Boolean operator == (TextureCubeArrayShaderResourceView textureCubeArrayShaderResourceView1, TextureCubeArrayShaderResourceView textureCubeArrayShaderResourceView2)
    {
        return (textureCubeArrayShaderResourceView1.mostDetailedMip == textureCubeArrayShaderResourceView2.mostDetailedMip) &&
            (textureCubeArrayShaderResourceView1.mipLevels == textureCubeArrayShaderResourceView2.mipLevels) &&
            (textureCubeArrayShaderResourceView1.first2DArrayFace == textureCubeArrayShaderResourceView2.first2DArrayFace) &&
            (textureCubeArrayShaderResourceView1.numCubes == textureCubeArrayShaderResourceView2.numCubes);
    }

    static Boolean operator != (TextureCubeArrayShaderResourceView textureCubeArrayShaderResourceView1, TextureCubeArrayShaderResourceView textureCubeArrayShaderResourceView2)
    {
        return !(textureCubeArrayShaderResourceView1 == textureCubeArrayShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != TextureCubeArrayShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<TextureCubeArrayShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mostDetailedMip.GetHashCode();
        hashCode = hashCode * 31 + mipLevels.GetHashCode();
        hashCode = hashCode * 31 + first2DArrayFace.GetHashCode();
        hashCode = hashCode * 31 + numCubes.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the subresource from a cube texture to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_TEXCUBE_SRV)</para>
/// </summary>
public value struct TextureCubeShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D11_TEXCUBE_SRV.MostDetailedMip)</para>
    /// </summary>
    property UInt32 MostDetailedMip
    {
        UInt32 get()
        {
            return mostDetailedMip;
        }

        void set(UInt32 value)
        {
            mostDetailedMip = value;
        }
    }
    /// <summary>
    /// The maximum number of mipmap levels in the texture. See the remarks in Texture1DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D11_TEXCUBE_SRV.MipLevels)</para>
    /// </summary>
    property UInt32 MipLevels
    {
        UInt32 get()
        {
            return mipLevels;
        }

        void set(UInt32 value)
        {
            mipLevels = value;
        }
    }
private:

    UInt32 mostDetailedMip;
    UInt32 mipLevels;

public:

    static Boolean operator == (TextureCubeShaderResourceView textureCubeShaderResourceView1, TextureCubeShaderResourceView textureCubeShaderResourceView2)
    {
        return (textureCubeShaderResourceView1.mostDetailedMip == textureCubeShaderResourceView2.mostDetailedMip) &&
            (textureCubeShaderResourceView1.mipLevels == textureCubeShaderResourceView2.mipLevels);
    }

    static Boolean operator != (TextureCubeShaderResourceView textureCubeShaderResourceView1, TextureCubeShaderResourceView textureCubeShaderResourceView2)
    {
        return !(textureCubeShaderResourceView1 == textureCubeShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != TextureCubeShaderResourceView::typeid)
        {
            return false;
        }

        return *this == safe_cast<TextureCubeShaderResourceView>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mostDetailedMip.GetHashCode();
        hashCode = hashCode * 31 + mipLevels.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a 1D texture.
/// <para>(Also see DirectX SDK: D3D11_TEXTURE1D_DESC)</para>
/// </summary>
public value struct Texture1DDescription
{
public:
    /// <summary>
    /// Texture width (in texels).
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE1D_DESC.Width)</para>
    /// </summary>
    property UInt32 Width
    {
        UInt32 get()
        {
            return width;
        }

        void set(UInt32 value)
        {
            width = value;
        }
    }
    /// <summary>
    /// The maximum number of mipmap levels in the texture. See the remarks in Texture1DShaderResourceView. Use 1 for a multisampled texture; or 0 to generate a full set of subtextures.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE1D_DESC.MipLevels)</para>
    /// </summary>
    property UInt32 MipLevels
    {
        UInt32 get()
        {
            return mipLevels;
        }

        void set(UInt32 value)
        {
            mipLevels = value;
        }
    }
    /// <summary>
    /// Number of textures in the array.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE1D_DESC.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
    /// <summary>
    /// Texture format (see <see cref="Format"/>)<seealso cref="Format"/>.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE1D_DESC.Format)</para>
    /// </summary>
    property Graphics::Format Format
    {
        Graphics::Format get()
        {
            return format;
        }

        void set(Graphics::Format value)
        {
            format = value;
        }
    }
    /// <summary>
    /// Value that identifies how the texture is to be read from and written to. The most common value is Usage-DEFAULT; see Usage for all possible values.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE1D_DESC.Usage)</para>
    /// </summary>
    property Usage Usage
    {
        Direct3D11::Usage get()
        {
            return usage;
        }

        void set(Direct3D11::Usage value)
        {
            usage = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="Direct3D11::BindingOptions"/>)<seealso cref="Direct3D11::BindingOptions"/> for binding to pipeline stages. The flags can be combined by a logical OR. For a 1D texture, the allowable values are: ShaderResource, RenderTarget and DepthStencil.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE1D_DESC.BindFlags)</para>
    /// </summary>
    property BindingOptions BindingOptions
    {
        Direct3D11::BindingOptions get()
        {
            return bindFlags;
        }

        void set(Direct3D11::BindingOptions value)
        {
            bindFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="CpuAccessOptions"/>)<seealso cref="CpuAccessOptions"/> to specify the types of CPU access allowed. Use 0 if CPU access is not required. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE1D_DESC.CPUAccessFlags)</para>
    /// </summary>
    property CpuAccessOptions CpuAccessOptions
    {
        Direct3D11::CpuAccessOptions get()
        {
            return cpuAccessFlags;
        }

        void set(Direct3D11::CpuAccessOptions value)
        {
            cpuAccessFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="MiscellaneousResourceOptions"/>)<seealso cref="MiscellaneousResourceOptions"/> that identifies other, less common resource options. Use 0 if none of these flags apply. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE1D_DESC.MiscFlags)</para>
    /// </summary>
    property MiscellaneousResourceOptions MiscellaneousResourceOptions
    {
        Direct3D11::MiscellaneousResourceOptions get()
        {
            return miscFlags;
        }

        void set(Direct3D11::MiscellaneousResourceOptions value)
        {
            miscFlags = value;
        }
    }

    static Boolean operator == (Texture1DDescription textureDescription1, Texture1DDescription textureDescription2)
    {
        return (textureDescription1.width == textureDescription2.width) &&
            (textureDescription1.mipLevels == textureDescription2.mipLevels) &&
            (textureDescription1.arraySize == textureDescription2.arraySize) &&
            (textureDescription1.format == textureDescription2.format) &&
            (textureDescription1.usage == textureDescription2.usage) &&
            (textureDescription1.bindFlags == textureDescription2.bindFlags) &&
            (textureDescription1.cpuAccessFlags == textureDescription2.cpuAccessFlags) &&
            (textureDescription1.miscFlags == textureDescription2.miscFlags);
    }

    static Boolean operator != (Texture1DDescription textureDescription1, Texture1DDescription textureDescription2)
    {
        return !(textureDescription1 == textureDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture1DDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture1DDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + width.GetHashCode();
        hashCode = hashCode * 31 + mipLevels.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();
        hashCode = hashCode * 31 + format.GetHashCode();
        hashCode = hashCode * 31 + usage.GetHashCode();
        hashCode = hashCode * 31 + bindFlags.GetHashCode();
        hashCode = hashCode * 31 + cpuAccessFlags.GetHashCode();
        hashCode = hashCode * 31 + miscFlags.GetHashCode();

        return hashCode;
    }

internal:

    Texture1DDescription(const D3D11_TEXTURE1D_DESC &desc)
    {
        Width = static_cast<UInt32>(desc.Width);
        MipLevels = static_cast<UInt32>(desc.MipLevels);
        ArraySize = static_cast<UInt32>(desc.ArraySize);
        Format = static_cast<Graphics::Format>(desc.Format);
        Usage = static_cast<Direct3D11::Usage>(desc.Usage);
        BindingOptions = static_cast<Direct3D11::BindingOptions>(desc.BindFlags);
        CpuAccessOptions = static_cast<Direct3D11::CpuAccessOptions>(desc.CPUAccessFlags);
        MiscellaneousResourceOptions = static_cast<Direct3D11::MiscellaneousResourceOptions>(desc.MiscFlags);
    }

    void CopyTo(D3D11_TEXTURE1D_DESC &desc)
    {
        desc.Width = static_cast<UINT>(Width);
        desc.MipLevels = static_cast<UINT>(MipLevels);
        desc.ArraySize = static_cast<UINT>(ArraySize);
        desc.Format = static_cast<DXGI_FORMAT>(Format);
        desc.Usage = static_cast<D3D11_USAGE>(Usage);
        desc.BindFlags = static_cast<UINT>(BindingOptions);
        desc.CPUAccessFlags = static_cast<UINT>(CpuAccessOptions);
        desc.MiscFlags = static_cast<UINT>(MiscellaneousResourceOptions);
    }

private:

    UInt32 width;
    UInt32 mipLevels;
    UInt32 arraySize;
    Graphics::Format format;
    Direct3D11::Usage usage;
    Direct3D11::BindingOptions bindFlags;
    Direct3D11::CpuAccessOptions cpuAccessFlags;
    Direct3D11::MiscellaneousResourceOptions miscFlags;

};

/// <summary>
/// Describes a 2D texture.
/// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC)</para>
/// </summary>
public value struct Texture2DDescription
{
public:
    /// <summary>
    /// Texture width (in texels). See Remarks.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC.Width)</para>
    /// </summary>
    property UInt32 Width
    {
        UInt32 get()
        {
            return width;
        }

        void set(UInt32 value)
        {
            width = value;
        }
    }
    /// <summary>
    /// Texture height (in texels). See Remarks.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC.Height)</para>
    /// </summary>
    property UInt32 Height
    {
        UInt32 get()
        {
            return height;
        }

        void set(UInt32 value)
        {
            height = value;
        }
    }
    /// <summary>
    /// The maximum number of mipmap levels in the texture. See the remarks in Texture1DShaderResourceView. Use 1 for a multisampled texture; or 0 to generate a full set of subtextures.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC.MipLevels)</para>
    /// </summary>
    property UInt32 MipLevels
    {
        UInt32 get()
        {
            return mipLevels;
        }

        void set(UInt32 value)
        {
            mipLevels = value;
        }
    }
    /// <summary>
    /// Number of textures in the texture array.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC.ArraySize)</para>
    /// </summary>
    property UInt32 ArraySize
    {
        UInt32 get()
        {
            return arraySize;
        }

        void set(UInt32 value)
        {
            arraySize = value;
        }
    }
    /// <summary>
    /// Texture format (see <see cref="Format"/>)<seealso cref="Format"/>.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC.Format)</para>
    /// </summary>
    property Graphics::Format Format
    {
        Graphics::Format get()
        {
            return format;
        }

        void set(Graphics::Format value)
        {
            format = value;
        }
    }
    /// <summary>
    /// Structure that specifies multisampling parameters for the texture. See SampleDescription.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC.SampleDesc)</para>
    /// </summary>
    property Graphics::SampleDescription SampleDescription
    {
        Graphics::SampleDescription get()
        {
            return sampleDescription;
        }

        void set(Graphics::SampleDescription value)
        {
            sampleDescription = value;
        }
    }
    /// <summary>
    /// Value that identifies how the texture is to be read from and written to. The most common value is Usage-DEFAULT; see Usage for all possible values.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC.Usage)</para>
    /// </summary>
    property Usage Usage
    {
        Direct3D11::Usage get()
        {
            return usage;
        }

        void set(Direct3D11::Usage value)
        {
            usage = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="Direct3D11::BindingOptions"/>)<seealso cref="Direct3D11::BindingOptions"/> for binding to pipeline stages. The flags can be combined by a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC.BindFlags)</para>
    /// </summary>
    property BindingOptions BindingOptions
    {
        Direct3D11::BindingOptions get()
        {
            return bindFlags;
        }

        void set(Direct3D11::BindingOptions value)
        {
            bindFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="CpuAccessOptions"/>)<seealso cref="CpuAccessOptions"/> to specify the types of CPU access allowed. Use 0 if CPU access is not required. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC.CPUAccessFlags)</para>
    /// </summary>
    property CpuAccessOptions CpuAccessOptions
    {
        Direct3D11::CpuAccessOptions get()
        {
            return cpuAccessFlags;
        }

        void set(Direct3D11::CpuAccessOptions value)
        {
            cpuAccessFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="MiscellaneousResourceOptions"/>)<seealso cref="MiscellaneousResourceOptions"/> that identifies other, less common resource options. Use 0 if none of these flags apply. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE2D_DESC.MiscFlags)</para>
    /// </summary>
    property MiscellaneousResourceOptions MiscellaneousResourceOptions
    {
        Direct3D11::MiscellaneousResourceOptions get()
        {
            return miscFlags;
        }

        void set(Direct3D11::MiscellaneousResourceOptions value)
        {
            miscFlags = value;
        }
    }

    static Boolean operator == (Texture2DDescription textureDescription1, Texture2DDescription textureDescription2)
    {
        return (textureDescription1.width == textureDescription2.width) &&
            (textureDescription1.height == textureDescription2.height) &&
            (textureDescription1.mipLevels == textureDescription2.mipLevels) &&
            (textureDescription1.arraySize == textureDescription2.arraySize) &&
            (textureDescription1.format == textureDescription2.format) &&
            (textureDescription1.sampleDescription == textureDescription2.sampleDescription) &&
            (textureDescription1.usage == textureDescription2.usage) &&
            (textureDescription1.bindFlags == textureDescription2.bindFlags) &&
            (textureDescription1.cpuAccessFlags == textureDescription2.cpuAccessFlags) &&
            (textureDescription1.miscFlags == textureDescription2.miscFlags);
    }

    static Boolean operator != (Texture2DDescription textureDescription1, Texture2DDescription textureDescription2)
    {
        return !(textureDescription1 == textureDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture2DDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture2DDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + width.GetHashCode();
        hashCode = hashCode * 31 + height.GetHashCode();
        hashCode = hashCode * 31 + mipLevels.GetHashCode();
        hashCode = hashCode * 31 + arraySize.GetHashCode();
        hashCode = hashCode * 31 + format.GetHashCode();
        hashCode = hashCode * 31 + sampleDescription.GetHashCode();
        hashCode = hashCode * 31 + usage.GetHashCode();
        hashCode = hashCode * 31 + bindFlags.GetHashCode();
        hashCode = hashCode * 31 + cpuAccessFlags.GetHashCode();
        hashCode = hashCode * 31 + miscFlags.GetHashCode();

        return hashCode;
    }

internal:

    Texture2DDescription(const D3D11_TEXTURE2D_DESC &desc)
    {
        Width = desc.Width;
        Height = desc.Height;
        MipLevels = desc.MipLevels;
        ArraySize = desc.ArraySize;
        Format = static_cast<Graphics::Format>(desc.Format);
        SampleDescription = Graphics::SampleDescription(desc.SampleDesc);
        Usage = static_cast<Direct3D11::Usage>(desc.Usage);
        BindingOptions = static_cast<Direct3D11::BindingOptions>(desc.BindFlags);
        CpuAccessOptions  = static_cast<Direct3D11::CpuAccessOptions>(desc.CPUAccessFlags);
        MiscellaneousResourceOptions = static_cast<Direct3D11::MiscellaneousResourceOptions>(desc.MiscFlags);
    }

    void CopyTo(D3D11_TEXTURE2D_DESC &desc)
    {
        desc.Width = Width;
        desc.Height = Height;
        desc.MipLevels = MipLevels;
        desc.ArraySize = ArraySize;
        desc.Format = static_cast<DXGI_FORMAT>(Format);
        SampleDescription.CopyTo(desc.SampleDesc);
        desc.Usage = static_cast<D3D11_USAGE>(Usage);
        desc.BindFlags = static_cast<UINT>(BindingOptions);
        desc.CPUAccessFlags  = static_cast<UINT>(CpuAccessOptions);
        desc.MiscFlags = static_cast<UINT>(MiscellaneousResourceOptions);
    }

private:

    UInt32 width;
    UInt32 height;
    UInt32 mipLevels;
    UInt32 arraySize;
    Graphics::Format format;
    Graphics::SampleDescription sampleDescription;
    Direct3D11::Usage usage;
    Direct3D11::BindingOptions bindFlags;
    Direct3D11::CpuAccessOptions cpuAccessFlags;
    Direct3D11::MiscellaneousResourceOptions miscFlags;

};

/// <summary>
/// Describes a 3D texture.
/// <para>(Also see DirectX SDK: D3D11_TEXTURE3D_DESC)</para>
/// </summary>
public value struct Texture3DDescription
{
public:
    /// <summary>
    /// Texture width (in texels). See Remarks.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE3D_DESC.Width)</para>
    /// </summary>
    property UInt32 Width
    {
        UInt32 get()
        {
            return width;
        }

        void set(UInt32 value)
        {
            width = value;
        }
    }
    /// <summary>
    /// Texture height (in texels). See Remarks.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE3D_DESC.Height)</para>
    /// </summary>
    property UInt32 Height
    {
        UInt32 get()
        {
            return height;
        }

        void set(UInt32 value)
        {
            height = value;
        }
    }
    /// <summary>
    /// Texture depth (in texels)
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE3D_DESC.Depth)</para>
    /// </summary>
    property UInt32 Depth
    {
        UInt32 get()
        {
            return depth;
        }

        void set(UInt32 value)
        {
            depth = value;
        }
    }
    /// <summary>
    /// The maximum number of mipmap levels in the texture. See the remarks in Texture1DShaderResourceView. Use 1 for a multisampled texture; or 0 to generate a full set of subtextures.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE3D_DESC.MipLevels)</para>
    /// </summary>
    property UInt32 MipLevels
    {
        UInt32 get()
        {
            return mipLevels;
        }

        void set(UInt32 value)
        {
            mipLevels = value;
        }
    }
    /// <summary>
    /// Texture format (see <see cref="Format"/>)<seealso cref="Format"/>.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE3D_DESC.Format)</para>
    /// </summary>
    property Graphics::Format Format
    {
        Graphics::Format get()
        {
            return format;
        }

        void set(Graphics::Format value)
        {
            format = value;
        }
    }
    /// <summary>
    /// Value that identifies how the texture is to be read from and written to. The most common value is Usage-DEFAULT; see Usage for all possible values.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE3D_DESC.Usage)</para>
    /// </summary>
    property Usage Usage
    {
        Direct3D11::Usage get()
        {
            return usage;
        }

        void set(Direct3D11::Usage value)
        {
            usage = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="Direct3D11::BindingOptions"/>)<seealso cref="Direct3D11::BindingOptions"/> for binding to pipeline stages. The flags can be combined by a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE3D_DESC.BindFlags)</para>
    /// </summary>
    property BindingOptions BindingOptions
    {
        Direct3D11::BindingOptions get()
        {
            return bindFlags;
        }

        void set(Direct3D11::BindingOptions value)
        {
            bindFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="CpuAccessOptions"/>)<seealso cref="CpuAccessOptions"/> to specify the types of CPU access allowed. Use 0 if CPU access is not required. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE3D_DESC.CPUAccessFlags)</para>
    /// </summary>
    property CpuAccessOptions CpuAccessOptions
    {
        Direct3D11::CpuAccessOptions get()
        {
            return cpuAccessFlags;
        }

        void set(Direct3D11::CpuAccessOptions value)
        {
            cpuAccessFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="MiscellaneousResourceOptions"/>)<seealso cref="MiscellaneousResourceOptions"/> that identifies other, less common resource options. Use 0 if none of these flags apply. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D11_TEXTURE3D_DESC.MiscFlags)</para>
    /// </summary>
    property MiscellaneousResourceOptions MiscellaneousResourceOptions
    {
        Direct3D11::MiscellaneousResourceOptions get()
        {
            return miscFlags;
        }

        void set(Direct3D11::MiscellaneousResourceOptions value)
        {
            miscFlags = value;
        }
    }

    static Boolean operator == (Texture3DDescription textureDescription1, Texture3DDescription textureDescription2)
    {
        return (textureDescription1.width == textureDescription2.width) &&
            (textureDescription1.height == textureDescription2.height) &&
            (textureDescription1.depth == textureDescription2.depth) &&
            (textureDescription1.mipLevels == textureDescription2.mipLevels) &&
            (textureDescription1.format == textureDescription2.format) &&
            (textureDescription1.usage == textureDescription2.usage) &&
            (textureDescription1.bindFlags == textureDescription2.bindFlags) &&
            (textureDescription1.cpuAccessFlags == textureDescription2.cpuAccessFlags) &&
            (textureDescription1.miscFlags == textureDescription2.miscFlags);
    }

    static Boolean operator != (Texture3DDescription textureDescription1, Texture3DDescription textureDescription2)
    {
        return !(textureDescription1 == textureDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Texture3DDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<Texture3DDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + width.GetHashCode();
        hashCode = hashCode * 31 + height.GetHashCode();
        hashCode = hashCode * 31 + depth.GetHashCode();
        hashCode = hashCode * 31 + mipLevels.GetHashCode();
        hashCode = hashCode * 31 + format.GetHashCode();
        hashCode = hashCode * 31 + usage.GetHashCode();
        hashCode = hashCode * 31 + bindFlags.GetHashCode();
        hashCode = hashCode * 31 + cpuAccessFlags.GetHashCode();
        hashCode = hashCode * 31 + miscFlags.GetHashCode();

        return hashCode;
    }

internal:

    Texture3DDescription(const D3D11_TEXTURE3D_DESC &desc)
    {
        width = static_cast<UInt32>(desc.Width);
        height = static_cast<UInt32>(desc.Height);
        depth = static_cast<UInt32>(desc.Depth);
        mipLevels = static_cast<UInt32>(desc.MipLevels);
        format = static_cast<Graphics::Format>(desc.Format);
        usage = static_cast<Direct3D11::Usage>(desc.Usage);
        bindFlags = static_cast<Direct3D11::BindingOptions>(desc.BindFlags);
        cpuAccessFlags = static_cast<Direct3D11::CpuAccessOptions>(desc.CPUAccessFlags);
        miscFlags = static_cast<Direct3D11::MiscellaneousResourceOptions>(desc.MiscFlags);
    }

    void CopyTo(D3D11_TEXTURE3D_DESC &desc)
    {
        desc.Width = static_cast<UINT>(width);
        desc.Height = static_cast<UINT>(height);
        desc.Depth = static_cast<UINT>(depth);
        desc.MipLevels = static_cast<UINT>(mipLevels);
        desc.Format = static_cast<DXGI_FORMAT>(format);
        desc.Usage = static_cast<D3D11_USAGE>(usage);
        desc.BindFlags = static_cast<UINT>(bindFlags);
        desc.CPUAccessFlags = static_cast<UINT>(cpuAccessFlags);
        desc.MiscFlags = static_cast<UINT>(miscFlags);
    }

private:

    UInt32 width;
    UInt32 height;
    UInt32 depth;
    UInt32 mipLevels;
    Graphics::Format format;
    Direct3D11::Usage usage;
    Direct3D11::BindingOptions bindFlags;
    Direct3D11::CpuAccessOptions cpuAccessFlags;
    Direct3D11::MiscellaneousResourceOptions miscFlags;

};

/// <summary>
/// Specifies the subresources of a texture that are accessible from a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_VIEW_DESC)</para>
/// </summary>
CA_SUPPRESS_MESSAGE("Microsoft.Portability", "CA1900:ValueTypeFieldsShouldBePortable", MessageId="texture1DArray")
CA_SUPPRESS_MESSAGE("Microsoft.Portability", "CA1900:ValueTypeFieldsShouldBePortable", MessageId="texture2DArray")
CA_SUPPRESS_MESSAGE("Microsoft.Portability", "CA1900:ValueTypeFieldsShouldBePortable", MessageId="texture2DMultisampleArray")
[StructLayout(LayoutKind::Explicit)]
public value struct DepthStencilViewDescription
{
    // REVIEW: D3D10 header files don't do this explicit layout stuff; the code
    // instead just copies between managed and unmanaged data structures.  Doing so
    // here would probably eliminate the need for the explicit layout as well as
    // the suppression of the portability warnings.
public:
    /// <summary>
    /// Resource data format (see <see cref="Format"/>)<seealso cref="Format"/>. See remarks for allowable formats.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_VIEW_DESC.Format)</para>
    /// </summary>
    property Graphics::Format Format
    {
        Graphics::Format get()
        {
            return format;
        }

        void set(Graphics::Format value)
        {
            format = value;
        }
    }
    /// <summary>
    /// Type of resource (see <see cref="DepthStencilViewDimension"/>)<seealso cref="DepthStencilViewDimension"/>. Specifies how a depth-stencil resource will be accessed; the value is stored in the union in this structure.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_VIEW_DESC.ViewDimension)</para>
    /// </summary>
    property DepthStencilViewDimension ViewDimension
    {
        DepthStencilViewDimension get()
        {
            return viewDimension;
        }

        void set(DepthStencilViewDimension value)
        {
            viewDimension = value;
        }
    }
    /// <summary>
    /// A value that describes whether the texture is read only.  Pass 0 to specify that it is not read only; otherwise, pass one of the members of the DepthStencilViewOptions enumerated type.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_VIEW_DESC.Flags)</para>
    /// </summary>
    property DepthStencilViewOptions Options
    {
		Direct3D11::DepthStencilViewOptions get()
        {
            return flags;
        }

        void set(Direct3D11::DepthStencilViewOptions value)
        {
            flags = value;
        }
    }
    /// <summary>
    /// Specifies a 1D texture subresource (see <see cref="Texture1DDepthStencilView"/>)<seealso cref="Texture1DDepthStencilView"/>.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_VIEW_DESC.Texture1D)</para>
    /// </summary>
    property Texture1DDepthStencilView Texture1D
    {
        Texture1DDepthStencilView get()
        {
            return texture1D;
        }

        void set(Texture1DDepthStencilView value)
        {
            texture1D = value;
        }
    }
    /// <summary>
    /// Specifies an array of 1D texture subresources (see <see cref="Texture1DArrayDepthStencilView"/>)<seealso cref="Texture1DArrayDepthStencilView"/>.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_VIEW_DESC.Texture1DArray)</para>
    /// </summary>
    property Texture1DArrayDepthStencilView Texture1DArray
    {
        Texture1DArrayDepthStencilView get()
        {
            return texture1DArray;
        }

        void set(Texture1DArrayDepthStencilView value)
        {
            texture1DArray = value;
        }
    }
    /// <summary>
    /// Specifies a 2D texture subresource (see <see cref="Texture2DDepthStencilView"/>)<seealso cref="Texture2DDepthStencilView"/>.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_VIEW_DESC.Texture2D)</para>
    /// </summary>
    property Texture2DDepthStencilView Texture2D
    {
        Texture2DDepthStencilView get()
        {
            return texture2D;
        }

        void set(Texture2DDepthStencilView value)
        {
            texture2D = value;
        }
    }
    /// <summary>
    /// Specifies an array of 2D texture subresources (see <see cref="Texture2DArrayDepthStencilView"/>)<seealso cref="Texture2DArrayDepthStencilView"/>.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_VIEW_DESC.Texture2DArray)</para>
    /// </summary>
    property Texture2DArrayDepthStencilView Texture2DArray
    {
        Texture2DArrayDepthStencilView get()
        {
            return texture2DArray;
        }

        void set(Texture2DArrayDepthStencilView value)
        {
            texture2DArray = value;
        }
    }
    /// <summary>
    /// Specifies a multisampled 2D texture (see <see cref="Texture2DMultisampleDepthStencilView"/>)<seealso cref="Texture2DMultisampleDepthStencilView"/>.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_VIEW_DESC.Texture2DMS)</para>
    /// </summary>
    property Texture2DMultisampleDepthStencilView Texture2DMultisample
    {
        Texture2DMultisampleDepthStencilView get()
        {
            return texture2DMultisample;
        }

        void set(Texture2DMultisampleDepthStencilView value)
        {
            texture2DMultisample = value;
        }
    }
    /// <summary>
    /// Specifies an array of multisampled 2D textures (see <see cref="Texture2DMultisampleArrayDepthStencilView"/>)<seealso cref="Texture2DMultisampleArrayDepthStencilView"/>.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_VIEW_DESC.Texture2DMSArray)</para>
    /// </summary>
    property Texture2DMultisampleArrayDepthStencilView Texture2DMultisampleArray
    {
        Texture2DMultisampleArrayDepthStencilView get()
        {
            return texture2DMultisampleArray;
        }

        void set(Texture2DMultisampleArrayDepthStencilView value)
        {
            texture2DMultisampleArray = value;
        }
    }

private:

    [FieldOffset(0)]
    Graphics::Format format;
    [FieldOffset(4)]
    DepthStencilViewDimension viewDimension;
    [FieldOffset(8)]
    DepthStencilViewOptions flags;
    [FieldOffset(12)]
    Texture1DDepthStencilView texture1D;
    [FieldOffset(12)]
    Texture1DArrayDepthStencilView texture1DArray;
    [FieldOffset(12)]
    Texture2DDepthStencilView texture2D;
    [FieldOffset(12)]
    Texture2DArrayDepthStencilView texture2DArray;
    [FieldOffset(12)]
    Texture2DMultisampleDepthStencilView texture2DMultisample;
    [FieldOffset(12)]
    Texture2DMultisampleArrayDepthStencilView texture2DMultisampleArray;

internal:
    DepthStencilViewDescription(const D3D11_DEPTH_STENCIL_VIEW_DESC& desc)
    {
        Format = static_cast<Graphics::Format>(desc.Format);
        ViewDimension = static_cast<DepthStencilViewDimension>(desc.ViewDimension);
        Options = static_cast<DepthStencilViewOptions>(desc.Flags);

        switch (ViewDimension)
        {
        case DepthStencilViewDimension::Texture1D :
              {
                  Texture1DDepthStencilView texture1D;

                  texture1D.MipSlice = desc.Texture1D.MipSlice;

                  Texture1D = texture1D;
                  break;
              }
        case DepthStencilViewDimension::Texture1DArray :
              {
                  Texture1DArrayDepthStencilView texture1DArray;

                  texture1DArray.ArraySize = desc.Texture1DArray.ArraySize;
                  texture1DArray.FirstArraySlice = desc.Texture1DArray.FirstArraySlice;
                  texture1DArray.MipSlice = desc.Texture1DArray.MipSlice;

                  Texture1DArray = texture1DArray;
                  break;
              }
        case DepthStencilViewDimension::Texture2D :
              {
                  Texture2DDepthStencilView texture2D;

                  texture2D.MipSlice = desc.Texture2D.MipSlice;

                  Texture2D = texture2D;
                  break;
              }
        case DepthStencilViewDimension::Texture2DArray :
              {
                  Texture2DArrayDepthStencilView texture2DArray;

                  texture2DArray.ArraySize = desc.Texture2DArray.ArraySize;
                  texture2DArray.FirstArraySlice = desc.Texture2DArray.FirstArraySlice;
                  texture2DArray.MipSlice = desc.Texture2DArray.MipSlice;

                  Texture2DArray = texture2DArray;
                  break;
              }
        case DepthStencilViewDimension::Texture2DMultisample :
              {
                  Texture2DMultisampleDepthStencilView texture2DMultisample;

                  texture2DMultisample.UnusedField = desc.Texture2DMS.UnusedField_NothingToDefine;

                  Texture2DMultisample = texture2DMultisample;
                  break;
              }
        case DepthStencilViewDimension::Texture2DMultisampleArray :
              {
                  Texture2DMultisampleArrayDepthStencilView texture2DMultisampleArray;

                  texture2DMultisampleArray.ArraySize = desc.Texture2DMSArray.ArraySize;
                  texture2DMultisampleArray.FirstArraySlice = desc.Texture2DMSArray.FirstArraySlice;

                  Texture2DMultisampleArray = texture2DMultisampleArray;
                  break;
              }
        default :
              {
                  throw gcnew NotSupportedException("Unknown or not supported DepthStencilViewDimension.");
              }
        }
    }

    void CopyTo(D3D11_DEPTH_STENCIL_VIEW_DESC* desc)
    {
        desc->Format = static_cast<DXGI_FORMAT>(Format);
        desc->ViewDimension = static_cast<D3D11_DSV_DIMENSION>(ViewDimension);
        desc->Flags = static_cast<UINT>(Options);

        switch (ViewDimension)
        {
        case DepthStencilViewDimension::Texture1D :
              {
                  desc->Texture1D.MipSlice = Texture1D.MipSlice;
                  break;
              }
        case DepthStencilViewDimension::Texture1DArray :
              {
                  desc->Texture1DArray.ArraySize = Texture1DArray.ArraySize;
                  desc->Texture1DArray.FirstArraySlice = Texture1DArray.FirstArraySlice;
                  desc->Texture1DArray.MipSlice = Texture1DArray.MipSlice;
                  break;
              }
        case DepthStencilViewDimension::Texture2D :
              {
                  desc->Texture2D.MipSlice = Texture2D.MipSlice;
                  break;
              }
        case DepthStencilViewDimension::Texture2DArray :
              {
                  desc->Texture2DArray.ArraySize = Texture2DArray.ArraySize;
                  desc->Texture2DArray.FirstArraySlice = Texture2DArray.FirstArraySlice;
                  desc->Texture2DArray.MipSlice = Texture2DArray.MipSlice;
                  break;
              }
        case DepthStencilViewDimension::Texture2DMultisample :
              {
                  desc->Texture2DMS.UnusedField_NothingToDefine = Texture2DMultisample.UnusedField;
                  break;
              }
        case DepthStencilViewDimension::Texture2DMultisampleArray :
              {
                  desc->Texture2DMSArray.ArraySize = Texture2DMultisampleArray.ArraySize;
                  desc->Texture2DMSArray.FirstArraySlice = Texture2DMultisampleArray.FirstArraySlice;
                  break;
              }
        default:
              {
                  throw gcnew NotSupportedException("Unknown or not supported DepthStencilViewDimension.");
              }
        }
    }

};

/// <summary>
/// Describes depth-stencil state.
/// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_DESC)</para>
/// </summary>
public value struct DepthStencilDescription
{
public:
    /// <summary>
    /// Enable depth testing.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_DESC.DepthEnable)</para>
    /// </summary>
    property Boolean DepthEnable
    {
        Boolean get()
        {
            return depthEnable;
        }

        void set(Boolean value)
        {
            depthEnable = value;
        }
    }
    /// <summary>
    /// Identify a portion of the depth-stencil buffer that can be modified by depth data (see <see cref="DepthWriteMask"/>)<seealso cref="DepthWriteMask"/>.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_DESC.DepthWriteMask)</para>
    /// </summary>
    property DepthWriteMask DepthWriteMask
    {
        Direct3D11::DepthWriteMask get()
        {
            return depthWriteMask;
        }

        void set(Direct3D11::DepthWriteMask value)
        {
            depthWriteMask = value;
        }
    }
    /// <summary>
    /// A function that compares depth data against existing depth data. The function options are listed in ComparisonFunction.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_DESC.DepthFunc)</para>
    /// </summary>
    property ComparisonFunction DepthFunction
    {
        ComparisonFunction get()
        {
            return depthFunction;
        }

        void set(ComparisonFunction value)
        {
            depthFunction = value;
        }
    }
    /// <summary>
    /// Enable stencil testing.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_DESC.StencilEnable)</para>
    /// </summary>
    property Boolean StencilEnable
    {
        Boolean get()
        {
            return stencilEnable;
        }

        void set(Boolean value)
        {
            stencilEnable = value;
        }
    }
    /// <summary>
    /// Identify a portion of the depth-stencil buffer for reading stencil data.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_DESC.StencilReadMask)</para>
    /// </summary>
    property StencilReadMask StencilReadMask
    {
        Direct3D11::StencilReadMask get()
        {
            return stencilReadMask;
        }

        void set(Direct3D11::StencilReadMask value)
        {
            stencilReadMask = value;
        }
    }
    /// <summary>
    /// Identify a portion of the depth-stencil buffer for writing stencil data.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_DESC.StencilWriteMask)</para>
    /// </summary>
    property StencilWriteMask StencilWriteMask
    {
        Direct3D11::StencilWriteMask get()
        {
            return stencilWriteMask;
        }

        void set(Direct3D11::StencilWriteMask value)
        {
            stencilWriteMask = value;
        }
    }
    /// <summary>
    /// Identify how to use the results of the depth test and the stencil test for pixels whose surface normal is facing towards the camera (see <see cref="DepthStencilOperationDescription"/>)<seealso cref="DepthStencilOperationDescription"/>.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_DESC.FrontFace)</para>
    /// </summary>
    property DepthStencilOperationDescription FrontFace
    {
        DepthStencilOperationDescription get()
        {
            return frontFace;
        }

        void set(DepthStencilOperationDescription value)
        {
            frontFace = value;
        }
    }
    /// <summary>
    /// Identify how to use the results of the depth test and the stencil test for pixels whose surface normal is facing away from the camera (see <see cref="DepthStencilOperationDescription"/>)<seealso cref="DepthStencilOperationDescription"/>.
    /// <para>(Also see DirectX SDK: D3D11_DEPTH_STENCIL_DESC.BackFace)</para>
    /// </summary>
    property DepthStencilOperationDescription BackFace
    {
        DepthStencilOperationDescription get()
        {
            return backFace;
        }

        void set(DepthStencilOperationDescription value)
        {
            backFace = value;
        }
    }

private:

    Direct3D11::DepthWriteMask depthWriteMask;
    ComparisonFunction depthFunction;
    Boolean stencilEnable;
    Direct3D11::StencilReadMask stencilReadMask;
    Direct3D11::StencilWriteMask stencilWriteMask;
    DepthStencilOperationDescription frontFace;
    DepthStencilOperationDescription backFace;

internal:
    DepthStencilDescription (const D3D11_DEPTH_STENCIL_DESC & desc)
    {
        DepthEnable = desc.DepthEnable != 0;
        DepthWriteMask = static_cast<Direct3D11::DepthWriteMask>(desc.DepthWriteMask);
        DepthFunction = static_cast<Direct3D11::ComparisonFunction>(desc.DepthFunc);
        StencilEnable = desc.StencilEnable != 0;
        StencilReadMask = static_cast<Direct3D11::StencilReadMask>(desc.StencilReadMask);
        StencilWriteMask = static_cast<Direct3D11::StencilWriteMask>(desc.StencilWriteMask);
        FrontFace = DepthStencilOperationDescription(desc.FrontFace);
        BackFace = DepthStencilOperationDescription(desc.BackFace);
    }

    void CopyTo (D3D11_DEPTH_STENCIL_DESC * desc)
    {
        desc->DepthEnable = DepthEnable ? 1 : 0;
        desc->DepthWriteMask = static_cast<D3D11_DEPTH_WRITE_MASK>(DepthWriteMask);
        desc->DepthFunc = static_cast<D3D11_COMPARISON_FUNC>(DepthFunction);
        desc->StencilEnable = StencilEnable ? 1 : 0;
        desc->StencilReadMask = static_cast<UINT8>(StencilReadMask);
        desc->StencilWriteMask = static_cast<UINT8>(StencilWriteMask);
        
        desc->FrontFace.StencilFailOp = static_cast<D3D11_STENCIL_OP>(FrontFace.StencilFailOperation);
        desc->FrontFace.StencilDepthFailOp = static_cast<D3D11_STENCIL_OP>(FrontFace.StencilDepthFailOperation);
        desc->FrontFace.StencilPassOp = static_cast<D3D11_STENCIL_OP>(FrontFace.StencilPassOperation );
        desc->FrontFace.StencilFunc = static_cast<D3D11_COMPARISON_FUNC>(FrontFace.StencilFunction);

        desc->BackFace.StencilFailOp = static_cast<D3D11_STENCIL_OP>(BackFace.StencilFailOperation);
        desc->BackFace.StencilDepthFailOp = static_cast<D3D11_STENCIL_OP>(BackFace.StencilDepthFailOperation);
        desc->BackFace.StencilPassOp = static_cast<D3D11_STENCIL_OP>(BackFace.StencilPassOperation );
        desc->BackFace.StencilFunc = static_cast<D3D11_COMPARISON_FUNC>(BackFace.StencilFunction);
    }
private:

    Boolean depthEnable;

public:

    static Boolean operator == (DepthStencilDescription depthStencilDescription1, DepthStencilDescription depthStencilDescription2)
    {
        return (depthStencilDescription1.depthWriteMask == depthStencilDescription2.depthWriteMask) &&
            (depthStencilDescription1.depthFunction == depthStencilDescription2.depthFunction) &&
            (depthStencilDescription1.stencilEnable == depthStencilDescription2.stencilEnable) &&
            (depthStencilDescription1.stencilReadMask == depthStencilDescription2.stencilReadMask) &&
            (depthStencilDescription1.stencilWriteMask == depthStencilDescription2.stencilWriteMask) &&
            (depthStencilDescription1.frontFace == depthStencilDescription2.frontFace) &&
            (depthStencilDescription1.backFace == depthStencilDescription2.backFace) &&
            (depthStencilDescription1.depthEnable == depthStencilDescription2.depthEnable);
    }

    static Boolean operator != (DepthStencilDescription depthStencilDescription1, DepthStencilDescription depthStencilDescription2)
    {
        return !(depthStencilDescription1 == depthStencilDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != DepthStencilDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<DepthStencilDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + depthWriteMask.GetHashCode();
        hashCode = hashCode * 31 + depthFunction.GetHashCode();
        hashCode = hashCode * 31 + stencilEnable.GetHashCode();
        hashCode = hashCode * 31 + stencilReadMask.GetHashCode();
        hashCode = hashCode * 31 + stencilWriteMask.GetHashCode();
        hashCode = hashCode * 31 + frontFace.GetHashCode();
        hashCode = hashCode * 31 + backFace.GetHashCode();
        hashCode = hashCode * 31 + depthEnable.GetHashCode();

        return hashCode;
    }

};

// REVIEW: rename to "ThreadingFeatureData"? The current name is just inherited
// from the unmanaged type name, which has awkward construction in the .NET context.

/// <summary>
/// Describes the multi-threading features that are supported by the current graphics driver.
/// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_THREADING)</para>
/// </summary>
public value struct FeatureDataThreading
{
public:
    /// <summary>
    /// TRUE means resources can be created concurrently on multiple threads while drawing; FALSE means that the presence of coarse synchronization will prevent concurrency.
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_THREADING.DriverConcurrentCreates)</para>
    /// </summary>
    property Boolean DriverConcurrentCreates
    {
        Boolean get()
        {
            return driverConcurrentCreates;
        }

        void set(Boolean value)
        {
            driverConcurrentCreates = value;
        }
    }
    /// <summary>
    /// TRUE means command lists are supported by the current driver; FALSE means that the API will emulate deferred contexts and command lists with software.
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_THREADING.DriverCommandLists)</para>
    /// </summary>
    property Boolean DriverCommandLists
    {
        Boolean get()
        {
            return driverCommandLists;
        }

        void set(Boolean value)
        {
            driverCommandLists = value;
        }
    }

private:

    Boolean driverConcurrentCreates;
    Boolean driverCommandLists;

internal:
    FeatureDataThreading(const D3D11_FEATURE_DATA_THREADING & feature)
    {
        DriverConcurrentCreates = feature.DriverConcurrentCreates != 0;
        DriverCommandLists = feature.DriverCommandLists != 0;
    }

    void CopyTo(D3D11_FEATURE_DATA_THREADING * feature)
    {
        feature->DriverConcurrentCreates = DriverConcurrentCreates ? 1 : 0;
        feature->DriverCommandLists = DriverCommandLists ? 1 : 0;
    }
public:

    static Boolean operator == (FeatureDataThreading featureDataThreading1, FeatureDataThreading featureDataThreading2)
    {
        return (featureDataThreading1.driverConcurrentCreates == featureDataThreading2.driverConcurrentCreates) &&
            (featureDataThreading1.driverCommandLists == featureDataThreading2.driverCommandLists);
    }

    static Boolean operator != (FeatureDataThreading featureDataThreading1, FeatureDataThreading featureDataThreading2)
    {
        return !(featureDataThreading1 == featureDataThreading2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != FeatureDataThreading::typeid)
        {
            return false;
        }

        return *this == safe_cast<FeatureDataThreading>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + driverConcurrentCreates.GetHashCode();
        hashCode = hashCode * 31 + driverCommandLists.GetHashCode();

        return hashCode;
    }

};

// REVIEW: do we really need a struct that encapsulates a single boolean value?

/// <summary>
/// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_DOUBLES)</para>
/// </summary>
public value struct FeatureDataDoubles
{
public:
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_DOUBLES.DoublePrecisionFloatShaderOps)</para>
    /// </summary>
    property Boolean DoublePrecisionFloatShaderOperations
    {
        Boolean get()
        {
            return doublePrecisionFloatShaderOperations;
        }

        void set(Boolean value)
        {
            doublePrecisionFloatShaderOperations = value;
        }
    }

private:

    Boolean doublePrecisionFloatShaderOperations;

internal:
    FeatureDataDoubles(const D3D11_FEATURE_DATA_DOUBLES & feature)
    {
        DoublePrecisionFloatShaderOperations = feature.DoublePrecisionFloatShaderOps != 0;
    }

    void CopyTo(D3D11_FEATURE_DATA_DOUBLES * feature)
    {
        feature->DoublePrecisionFloatShaderOps = DoublePrecisionFloatShaderOperations ? 1 : 0;
    }
public:

    static Boolean operator == (FeatureDataDoubles featureDataDoubles1, FeatureDataDoubles featureDataDoubles2)
    {
        return (featureDataDoubles1.doublePrecisionFloatShaderOperations == featureDataDoubles2.doublePrecisionFloatShaderOperations);
    }

    static Boolean operator != (FeatureDataDoubles featureDataDoubles1, FeatureDataDoubles featureDataDoubles2)
    {
        return !(featureDataDoubles1 == featureDataDoubles2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != FeatureDataDoubles::typeid)
        {
            return false;
        }

        return *this == safe_cast<FeatureDataDoubles>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + doublePrecisionFloatShaderOperations.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_FORMAT_SUPPORT)</para>
/// </summary>
public value struct FeatureDataFormatSupport
{
public:
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_FORMAT_SUPPORT.InFormat)</para>
    /// </summary>
    property Format InFormat
    {
        Graphics::Format get()
        {
            return inFormat;
        }

        void set(Graphics::Format value)
        {
            inFormat = value;
        }
    }

    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_FORMAT_SUPPORT.OutFormatSupport)</para>
    /// </summary>
    property FormatSupportOptions OutFormatSupport
    {
        FormatSupportOptions get()
        {
            return outFormatSupport;
        }

        void set(FormatSupportOptions value)
        {
            outFormatSupport = value;
        }
    }

private:

    Graphics::Format inFormat;
    FormatSupportOptions outFormatSupport;

internal:
    FeatureDataFormatSupport(const D3D11_FEATURE_DATA_FORMAT_SUPPORT & feature)
    {
        InFormat = static_cast<Graphics::Format>(feature.InFormat);
        OutFormatSupport = static_cast<FormatSupportOptions>(feature.OutFormatSupport);
    }

    void CopyTo(D3D11_FEATURE_DATA_FORMAT_SUPPORT * feature)
    {
        feature->InFormat = static_cast<DXGI_FORMAT>(InFormat);
        feature->OutFormatSupport = static_cast<UINT>(OutFormatSupport);
    }
public:

    static Boolean operator == (FeatureDataFormatSupport featureDataFormatSupport1, FeatureDataFormatSupport featureDataFormatSupport2)
    {
        return (featureDataFormatSupport1.inFormat == featureDataFormatSupport2.inFormat) &&
            (featureDataFormatSupport1.outFormatSupport == featureDataFormatSupport2.outFormatSupport);
    }

    static Boolean operator != (FeatureDataFormatSupport featureDataFormatSupport1, FeatureDataFormatSupport featureDataFormatSupport2)
    {
        return !(featureDataFormatSupport1 == featureDataFormatSupport2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != FeatureDataFormatSupport::typeid)
        {
            return false;
        }

        return *this == safe_cast<FeatureDataFormatSupport>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + inFormat.GetHashCode();
        hashCode = hashCode * 31 + outFormatSupport.GetHashCode();

        return hashCode;
    }

};

// REVIEW: the name of this type and its members need fixing

/// <summary>
/// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_FORMAT_SUPPORT)</para>
/// </summary>
public value struct FeatureDataFormatSupport2
{
public:
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_FORMAT_SUPPORT2.InFormat)</para>
    /// </summary>
    property Graphics::Format InFormat
    {
        Graphics::Format get()
        {
            return inFormat;
        }

        void set(Graphics::Format value)
        {
            inFormat = value;
        }
    }

    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_FORMAT_SUPPORT2.OutFormatSupport2)</para>
    /// </summary>
    property ExtendedFormatSupportOptions OutFormatSupport2
    {
        ExtendedFormatSupportOptions get()
        {
            return outFormatSupport2;
        }

        void set(ExtendedFormatSupportOptions value)
        {
            outFormatSupport2 = value;
        }
    }
private:

    Graphics::Format inFormat;
    ExtendedFormatSupportOptions outFormatSupport2;

internal:
    FeatureDataFormatSupport2(const D3D11_FEATURE_DATA_FORMAT_SUPPORT2 & feature)
    {
        InFormat = static_cast<Graphics::Format>(feature.InFormat);
        OutFormatSupport2 = static_cast<ExtendedFormatSupportOptions>(feature.OutFormatSupport2);
    }

    void CopyTo(D3D11_FEATURE_DATA_FORMAT_SUPPORT2 * feature)
    {
        feature->InFormat = static_cast<DXGI_FORMAT>(InFormat);
        feature->OutFormatSupport2 = static_cast<UINT>(OutFormatSupport2);
    }
public:

    static Boolean operator == (FeatureDataFormatSupport2 featureDataFormatSupport1, FeatureDataFormatSupport2 featureDataFormatSupport2)
    {
        return (featureDataFormatSupport1.inFormat == featureDataFormatSupport2.inFormat) &&
            (featureDataFormatSupport1.outFormatSupport2 == featureDataFormatSupport2.outFormatSupport2);
    }

    static Boolean operator != (FeatureDataFormatSupport2 featureDataFormatSupport1, FeatureDataFormatSupport2 featureDataFormatSupport2)
    {
        return !(featureDataFormatSupport1 == featureDataFormatSupport2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != FeatureDataFormatSupport2::typeid)
        {
            return false;
        }

        return *this == safe_cast<FeatureDataFormatSupport2>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + inFormat.GetHashCode();
        hashCode = hashCode * 31 + outFormatSupport2.GetHashCode();

        return hashCode;
    }

};

// REVIEW: here's another "struct that's just a bool" that we can probably get rid of

/// <summary>
/// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_D3D10_X_HARDWARE_OPTIONS)</para>
/// </summary>
public value struct FeatureDataD3D10XHardwareOptions
{
public:
    /// <summary>
    /// <para>(Also see DirectX SDK: D3D11_FEATURE_DATA_D3D10_X_HARDWARE_OPTIONS.ComputeShaders_Plus_RawAndStructuredBuffers_Via_Shader_4_x)</para>
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId="x")
    property Boolean ComputeShadersPlusRawAndStructuredBuffersViaShader4x
    {
        Boolean get()
        {
            return computeShadersPlusRawAndStructuredBuffersViaShader4x;
        }

        void set(Boolean value)
        {
            computeShadersPlusRawAndStructuredBuffersViaShader4x = value;
        }
    }

private:

    Boolean computeShadersPlusRawAndStructuredBuffersViaShader4x;

internal:
    FeatureDataD3D10XHardwareOptions(const D3D11_FEATURE_DATA_D3D10_X_HARDWARE_OPTIONS & feature)
    {
        ComputeShadersPlusRawAndStructuredBuffersViaShader4x = feature.ComputeShaders_Plus_RawAndStructuredBuffers_Via_Shader_4_x != 0;
    }

    void CopyTo(D3D11_FEATURE_DATA_D3D10_X_HARDWARE_OPTIONS * feature)
    {
        feature->ComputeShaders_Plus_RawAndStructuredBuffers_Via_Shader_4_x = ComputeShadersPlusRawAndStructuredBuffersViaShader4x ? 1 : 0;
    }
public:

    static Boolean operator == (FeatureDataD3D10XHardwareOptions featureDataHardwareOptions1, FeatureDataD3D10XHardwareOptions featureDataHardwareOptions2)
    {
        return (featureDataHardwareOptions1.computeShadersPlusRawAndStructuredBuffersViaShader4x == featureDataHardwareOptions2.computeShadersPlusRawAndStructuredBuffersViaShader4x);
    }

    static Boolean operator != (FeatureDataD3D10XHardwareOptions featureDataHardwareOptions1, FeatureDataD3D10XHardwareOptions featureDataHardwareOptions2)
    {
        return !(featureDataHardwareOptions1 == featureDataHardwareOptions2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != FeatureDataD3D10XHardwareOptions::typeid)
        {
            return false;
        }

        return *this == safe_cast<FeatureDataD3D10XHardwareOptions>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + computeShadersPlusRawAndStructuredBuffersViaShader4x.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// A description of a single element for the input-assembler stage.
/// <para>(Also see DirectX SDK: D3D11_INPUT_ELEMENT_DESC)</para>
/// </summary>
public value struct InputElementDescription
{
public:
    /// <summary>
    /// The HLSL semantic associated with this element in a shader input-signature.
    /// <para>(Also see DirectX SDK: D3D11_INPUT_ELEMENT_DESC.SemanticName)</para>
    /// </summary>
    property String^ SemanticName
    {
        String^ get()
        {
            return semanticName;
        }

        void set(String^ value)
        {
            semanticName = value;
        }
    }
    /// <summary>
    /// The semantic index for the element. A semantic index modifies a semantic, with an integer index number. A semantic index is only needed in a case where there is more than one element with the same semantic. For example, a 4x4 matrix would have four components each with the semantic name matrix, however each of the four component would have different semantic indices (0, 1, 2, and 3).
    /// <para>(Also see DirectX SDK: D3D11_INPUT_ELEMENT_DESC.SemanticIndex)</para>
    /// </summary>
    property UInt32 SemanticIndex
    {
        UInt32 get()
        {
            return semanticIndex;
        }

        void set(UInt32 value)
        {
            semanticIndex = value;
        }
    }
    /// <summary>
    /// The data type of the element data. See Format.
    /// <para>(Also see DirectX SDK: D3D11_INPUT_ELEMENT_DESC.Format)</para>
    /// </summary>
    property Graphics::Format Format
    {
        Graphics::Format get()
        {
            return format;
        }

        void set(Graphics::Format value)
        {
            format = value;
        }
    }
    /// <summary>
    /// An integer value that identifies the input-assembler (see input slot). Valid values are between 0 and 15, defined in D3D11.h.
    /// <para>(Also see DirectX SDK: D3D11_INPUT_ELEMENT_DESC.InputSlot)</para>
    /// </summary>
    property UInt32 InputSlot
    {
        UInt32 get()
        {
            return inputSlot;
        }

        void set(UInt32 value)
        {
            inputSlot = value;
        }
    }
    /// <summary>
    /// Optional. Offset (in bytes) between each element. Use D3D11_APPEND_ALIGNED_ELEMENT for convenience to define the current element directly after the previous one, including any packing if necessary.
    /// <para>(Also see DirectX SDK: D3D11_INPUT_ELEMENT_DESC.AlignedByteOffset)</para>
    /// </summary>
    property UInt32 AlignedByteOffset
    {
        UInt32 get()
        {
            return alignedByteOffset;
        }

        void set(UInt32 value)
        {
            alignedByteOffset = value;
        }
    }
    /// <summary>
    /// Identifies the input data class for a single input slot (see <see cref="InputClassification"/>)<seealso cref="InputClassification"/>.
    /// <para>(Also see DirectX SDK: D3D11_INPUT_ELEMENT_DESC.InputSlotClass)</para>
    /// </summary>
    property InputClassification InputSlotClass
    {
        InputClassification get()
        {
            return inputSlotClass;
        }

        void set(InputClassification value)
        {
            inputSlotClass = value;
        }
    }
    /// <summary>
    /// The number of instances to draw using the same per-instance data before advancing in the buffer by one element. This value must be 0 for an element that contains per-vertex data (the slot class is set to PerVertexData).
    /// <para>(Also see DirectX SDK: D3D11_INPUT_ELEMENT_DESC.InstanceDataStepRate)</para>
    /// </summary>
    property UInt32 InstanceDataStepRate
    {
        UInt32 get()
        {
            return instanceDataStepRate;
        }

        void set(UInt32 value)
        {
            instanceDataStepRate = value;
        }
    }

private:

    UInt32 semanticIndex;
    Graphics::Format format;
    UInt32 inputSlot;
    UInt32 alignedByteOffset;
    InputClassification inputSlotClass;
    UInt32 instanceDataStepRate;

internal:
    InputElementDescription(const D3D11_INPUT_ELEMENT_DESC & desc)
    {
        SemanticName = desc.SemanticName ? gcnew String(desc.SemanticName) : nullptr;
        SemanticIndex = desc.SemanticIndex;
        Format = static_cast<Graphics::Format>(desc.Format);
        InputSlot = desc.InputSlot;
        AlignedByteOffset = desc.AlignedByteOffset;
        InputSlotClass = static_cast<InputClassification>(desc.InputSlotClass);
        InstanceDataStepRate = desc.InstanceDataStepRate;
    }

    void CopyTo(D3D11_INPUT_ELEMENT_DESC * desc, marshal_context^ context)
    {
        desc->SemanticIndex = SemanticIndex;
        desc->Format = static_cast<DXGI_FORMAT>(Format);
        desc->InputSlot = InputSlot;
        desc->AlignedByteOffset = AlignedByteOffset;
        desc->InputSlotClass = static_cast<D3D11_INPUT_CLASSIFICATION>(InputSlotClass);
        desc->InstanceDataStepRate = InstanceDataStepRate;

        String^ name = SemanticName;
        desc->SemanticName = SemanticName == nullptr ? NULL : context->marshal_as<const char*>(name);
    }


private:

    String^ semanticName;

public:

    static Boolean operator == (InputElementDescription inputElementDescription1, InputElementDescription inputElementDescription2)
    {
        return (inputElementDescription1.semanticIndex == inputElementDescription2.semanticIndex) &&
            (inputElementDescription1.format == inputElementDescription2.format) &&
            (inputElementDescription1.inputSlot == inputElementDescription2.inputSlot) &&
            (inputElementDescription1.alignedByteOffset == inputElementDescription2.alignedByteOffset) &&
            (inputElementDescription1.inputSlotClass == inputElementDescription2.inputSlotClass) &&
            (inputElementDescription1.instanceDataStepRate == inputElementDescription2.instanceDataStepRate) &&
            (inputElementDescription1.semanticName == inputElementDescription2.semanticName);
    }

    static Boolean operator != (InputElementDescription inputElementDescription1, InputElementDescription inputElementDescription2)
    {
        return !(inputElementDescription1 == inputElementDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != InputElementDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<InputElementDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + semanticIndex.GetHashCode();
        hashCode = hashCode * 31 + format.GetHashCode();
        hashCode = hashCode * 31 + inputSlot.GetHashCode();
        hashCode = hashCode * 31 + alignedByteOffset.GetHashCode();
        hashCode = hashCode * 31 + inputSlotClass.GetHashCode();
        hashCode = hashCode * 31 + instanceDataStepRate.GetHashCode();
        hashCode = hashCode * 31 + semanticName->GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Provides access to subresource data.
/// <para>(Also see DirectX SDK: D3D11_MAPPED_SUBRESOURCE)</para>
/// </summary>
public value struct MappedSubresource
{
public:
    /// <summary>
    /// Pointer to the data.
    /// <para>(Also see DirectX SDK: D3D11_MAPPED_SUBRESOURCE.pData)</para>
    /// </summary>
    property IntPtr Data
    {
        IntPtr get()
        {
            return data;
        }

        void set(IntPtr value)
        {
            data = value;
        }
    }
    /// <summary>
    /// The row pitch, or width, or physical size (in bytes) of the data.
    /// <para>(Also see DirectX SDK: D3D11_MAPPED_SUBRESOURCE.RowPitch)</para>
    /// </summary>
    property UInt32 RowPitch
    {
        UInt32 get()
        {
            return rowPitch;
        }

        void set(UInt32 value)
        {
            rowPitch = value;
        }
    }
    /// <summary>
    /// The depth pitch, or width, or physical size (in bytes)of the data.
    /// <para>(Also see DirectX SDK: D3D11_MAPPED_SUBRESOURCE.DepthPitch)</para>
    /// </summary>
    property UInt32 DepthPitch
    {
        UInt32 get()
        {
            return depthPitch;
        }

        void set(UInt32 value)
        {
            depthPitch = value;
        }
    }

private:

    UInt32 rowPitch;
    UInt32 depthPitch;

internal:

    MappedSubresource (const D3D11_MAPPED_SUBRESOURCE &subresource)
    {
        Data = IntPtr(subresource.pData);
        RowPitch = subresource.RowPitch;
        DepthPitch = subresource.DepthPitch;
    }

    // MappedSubresource is output-only. No need for a CopyTo() method
    // because there isn't any place that it's needed in order to pass
    // back to the native API.

private:

    IntPtr data;

};


/// <summary>
/// A debug message in the Information Queue.
/// <para>(Also see DirectX SDK: D3D11_MESSAGE)</para>
/// </summary>
public value struct Message
{
public:
    /// <summary>
    /// The category of the message. See MessageCategory.
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE.Category)</para>
    /// </summary>
    property MessageCategory Category
    {
        MessageCategory get()
        {
            return category;
        }

        void set(MessageCategory value)
        {
            category = value;
        }
    }
    /// <summary>
    /// The severity of the message. See MessageSeverity.
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE.Severity)</para>
    /// </summary>
    property MessageSeverity Severity
    {
        MessageSeverity get()
        {
            return severity;
        }

        void set(MessageSeverity value)
        {
            severity = value;
        }
    }
    /// <summary>
    /// The ID of the message. See MessageId.
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE.ID)</para>
    /// </summary>
    property MessageId Id
    {
        MessageId get()
        {
            return id;
        }

        void set(MessageId value)
        {
            id = value;
        }
    }
    /// <summary>
    /// The message string.
    /// <para>(Also see DirectX SDK: D3D11_MESSAGE.pDescription)</para>
    /// </summary>
    property String^ Description
    {
        String^ get()
        {
            return description;
        }

        void set(String^ value)
        {
            description = value;
        }
    }

private:

    MessageCategory category;
    MessageSeverity severity;
    MessageId id;
    String^ description;

internal:
    Message (D3D11_MESSAGE* msg)
    {
        if (msg == NULL)
        {
            return;
        }

        Category = static_cast<MessageCategory>(msg->Category);
        Severity = static_cast<MessageSeverity>(msg->Severity);
        Id = static_cast<MessageId>(msg->ID);

        Description = msg->pDescription && msg->DescriptionByteLength  > 0 ? gcnew String(msg->pDescription) : nullptr;       
    }
public:

    static Boolean operator == (Message message1, Message message2)
    {
        return (message1.category == message2.category) &&
            (message1.severity == message2.severity) &&
            (message1.id == message2.id) &&
            (message1.description == message2.description);
    }

    static Boolean operator != (Message message1, Message message2)
    {
        return !(message1 == message2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Message::typeid)
        {
            return false;
        }

        return *this == safe_cast<Message>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + category.GetHashCode();
        hashCode = hashCode * 31 + severity.GetHashCode();
        hashCode = hashCode * 31 + id.GetHashCode();
        hashCode = hashCode * 31 + description->GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Query information about graphics-pipeline activity in between calls to DeviceContext.Begin and DeviceContext.End.
/// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_PIPELINE_STATISTICS)</para>
/// </summary>
public value struct QueryDataPipelineStatistics
{
public:
    property UInt64 InputAssemblerVertices
    {
        UInt64 get()
        {
            return inputAssemblerVertices;
        }

        void set(UInt64 value)
        {
            inputAssemblerVertices = value;
        }
    }
    /// <summary>
    /// Number of primitives read by the input assembler. This number can be different depending on the primitive topology used. For example, a triangle strip with 6 vertices will produce 4 triangles, however a triangle list with 6 vertices will produce 2 triangles.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_PIPELINE_STATISTICS.IAPrimitives)</para>
    /// </summary>
    property UInt64 InputAssemblerPrimitives
    {
        UInt64 get()
        {
            return inputAssemblerPrimitives;
        }

        void set(UInt64 value)
        {
            inputAssemblerPrimitives = value;
        }
    }
    /// <summary>
    /// Number of times a vertex shader was invoked. Direct3D invokes the vertex shader once per vertex.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_PIPELINE_STATISTICS.VSInvocations)</para>
    /// </summary>
    property UInt64 VertexShaderInvocations
    {
        UInt64 get()
        {
            return vertexShaderInvocations;
        }

        void set(UInt64 value)
        {
            vertexShaderInvocations = value;
        }
    }
    /// <summary>
    /// Number of times a geometry shader was invoked. When the geometry shader is set to NULL, this statistic may or may not increment depending on the hardware manufacturer.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_PIPELINE_STATISTICS.GSInvocations)</para>
    /// </summary>
    property UInt64 GeometryShaderInvocations
    {
        UInt64 get()
        {
            return geometryShaderInvocations;
        }

        void set(UInt64 value)
        {
            geometryShaderInvocations = value;
        }
    }
    /// <summary>
    /// Number of primitives output by a geometry shader.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_PIPELINE_STATISTICS.GSPrimitives)</para>
    /// </summary>
    property UInt64 GeometryShaderPrimitives
    {
        UInt64 get()
        {
            return geometryShaderPrimitives;
        }

        void set(UInt64 value)
        {
            geometryShaderPrimitives = value;
        }
    }
    /// <summary>
    /// Number of primitives that were sent to the rasterizer. When the rasterizer is disabled, this will not increment.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_PIPELINE_STATISTICS.CInvocations)</para>
    /// </summary>
    property UInt64 CInvocations
    {
        UInt64 get()
        {
            return cInvocations;
        }

        void set(UInt64 value)
        {
            cInvocations = value;
        }
    }
    /// <summary>
    /// Number of primitives that were rendered. This may be larger or smaller than CInvocations because after a primitive is clipped sometimes it is either broken up into more than one primitive or completely culled.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_PIPELINE_STATISTICS.CPrimitives)</para>
    /// </summary>
    property UInt64 CPrimitives
    {
        UInt64 get()
        {
            return cPrimitives;
        }

        void set(UInt64 value)
        {
            cPrimitives = value;
        }
    }
    /// <summary>
    /// Number of times a pixel shader was invoked.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_PIPELINE_STATISTICS.PSInvocations)</para>
    /// </summary>
    property UInt64 PixelShaderInvocations
    {
        UInt64 get()
        {
            return pixelShaderInvocations;
        }

        void set(UInt64 value)
        {
            pixelShaderInvocations = value;
        }
    }
    /// <summary>
    /// Number of times a hull shader was invoked.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_PIPELINE_STATISTICS.HSInvocations)</para>
    /// </summary>
    property UInt64 HullShaderInvocations
    {
        UInt64 get()
        {
            return hullShaderInvocations;
        }

        void set(UInt64 value)
        {
            hullShaderInvocations = value;
        }
    }
    /// <summary>
    /// Number of times a domain shader was invoked.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_PIPELINE_STATISTICS.DSInvocations)</para>
    /// </summary>
    property UInt64 DomainShaderInvocations
    {
        UInt64 get()
        {
            return domainShaderInvocations;
        }

        void set(UInt64 value)
        {
            domainShaderInvocations = value;
        }
    }
private:

    UInt64 inputAssemblerVertices;
    UInt64 inputAssemblerPrimitives;
    UInt64 vertexShaderInvocations;
    UInt64 geometryShaderInvocations;
    UInt64 geometryShaderPrimitives;
    UInt64 cInvocations;
    UInt64 cPrimitives;
    UInt64 pixelShaderInvocations;
    UInt64 hullShaderInvocations;
    UInt64 domainShaderInvocations;

public:

    static Boolean operator == (QueryDataPipelineStatistics queryDataPipelineStatistics1, QueryDataPipelineStatistics queryDataPipelineStatistics2)
    {
        return (queryDataPipelineStatistics1.inputAssemblerVertices == queryDataPipelineStatistics2.inputAssemblerVertices) &&
            (queryDataPipelineStatistics1.inputAssemblerPrimitives == queryDataPipelineStatistics2.inputAssemblerPrimitives) &&
            (queryDataPipelineStatistics1.vertexShaderInvocations == queryDataPipelineStatistics2.vertexShaderInvocations) &&
            (queryDataPipelineStatistics1.geometryShaderInvocations == queryDataPipelineStatistics2.geometryShaderInvocations) &&
            (queryDataPipelineStatistics1.geometryShaderPrimitives == queryDataPipelineStatistics2.geometryShaderPrimitives) &&
            (queryDataPipelineStatistics1.cInvocations == queryDataPipelineStatistics2.cInvocations) &&
            (queryDataPipelineStatistics1.cPrimitives == queryDataPipelineStatistics2.cPrimitives) &&
            (queryDataPipelineStatistics1.pixelShaderInvocations == queryDataPipelineStatistics2.pixelShaderInvocations) &&
            (queryDataPipelineStatistics1.hullShaderInvocations == queryDataPipelineStatistics2.hullShaderInvocations) &&
            (queryDataPipelineStatistics1.domainShaderInvocations == queryDataPipelineStatistics2.domainShaderInvocations);
    }

    static Boolean operator != (QueryDataPipelineStatistics queryDataPipelineStatistics1, QueryDataPipelineStatistics queryDataPipelineStatistics2)
    {
        return !(queryDataPipelineStatistics1 == queryDataPipelineStatistics2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != QueryDataPipelineStatistics::typeid)
        {
            return false;
        }

        return *this == safe_cast<QueryDataPipelineStatistics>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + inputAssemblerVertices.GetHashCode();
        hashCode = hashCode * 31 + inputAssemblerPrimitives.GetHashCode();
        hashCode = hashCode * 31 + vertexShaderInvocations.GetHashCode();
        hashCode = hashCode * 31 + geometryShaderInvocations.GetHashCode();
        hashCode = hashCode * 31 + geometryShaderPrimitives.GetHashCode();
        hashCode = hashCode * 31 + cInvocations.GetHashCode();
        hashCode = hashCode * 31 + cPrimitives.GetHashCode();
        hashCode = hashCode * 31 + pixelShaderInvocations.GetHashCode();
        hashCode = hashCode * 31 + hullShaderInvocations.GetHashCode();
        hashCode = hashCode * 31 + domainShaderInvocations.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Query information about the amount of data streamed out to the stream-output buffers in between DeviceContext.Begin and DeviceContext.End.
/// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_SO_STATISTICS)</para>
/// </summary>
public value struct QueryDataStreamOutputStatistics
{
public:
    /// <summary>
    /// Number of primitives (that is, points, lines, and triangles) written to the stream-output buffers.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_SO_STATISTICS.NumPrimitivesWritten)</para>
    /// </summary>
    property UInt64 PrimitiveWrittenCount
    {
        UInt64 get()
        {
            return numPrimitivesWritten;
        }

        void set(UInt64 value)
        {
            numPrimitivesWritten = value;
        }
    }
    /// <summary>
    /// Number of primitives that would have been written to the stream-output buffers if there had been enough space for them all.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_SO_STATISTICS.PrimitivesStorageNeeded)</para>
    /// </summary>
    property UInt64 PrimitivesStorageNeeded
    {
        UInt64 get()
        {
            return primitivesStorageNeeded;
        }

        void set(UInt64 value)
        {
            primitivesStorageNeeded = value;
        }
    }
private:

    UInt64 numPrimitivesWritten;
    UInt64 primitivesStorageNeeded;

public:

    static Boolean operator == (QueryDataStreamOutputStatistics queryDataStreamOutputStatistics1, QueryDataStreamOutputStatistics queryDataStreamOutputStatistics2)
    {
        return (queryDataStreamOutputStatistics1.numPrimitivesWritten == queryDataStreamOutputStatistics2.numPrimitivesWritten) &&
            (queryDataStreamOutputStatistics1.primitivesStorageNeeded == queryDataStreamOutputStatistics2.primitivesStorageNeeded);
    }

    static Boolean operator != (QueryDataStreamOutputStatistics queryDataStreamOutputStatistics1, QueryDataStreamOutputStatistics queryDataStreamOutputStatistics2)
    {
        return !(queryDataStreamOutputStatistics1 == queryDataStreamOutputStatistics2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != QueryDataStreamOutputStatistics::typeid)
        {
            return false;
        }

        return *this == safe_cast<QueryDataStreamOutputStatistics>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + numPrimitivesWritten.GetHashCode();
        hashCode = hashCode * 31 + primitivesStorageNeeded.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Query information about the reliability of a timestamp query.
/// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_TIMESTAMP_DISJOINT)</para>
/// </summary>
public value struct QueryDataTimestampDisjoint
{
public:
    /// <summary>
    /// How frequently the GPU counter increments in Hz.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_TIMESTAMP_DISJOINT.Frequency)</para>
    /// </summary>
    property UInt64 Frequency
    {
        UInt64 get()
        {
            return frequency;
        }

        void set(UInt64 value)
        {
            frequency = value;
        }
    }
    /// <summary>
    /// If this is TRUE, something occurred in between the query's DeviceContext.Begin and DeviceContext.End calls that caused the timestamp counter to become discontinuous or disjoint, such as unplugging the AC chord on a laptop, overheating, or throttling up/down due to laptop savings events. The timestamp returned by DeviceContext.GetData for a timestamp query is only reliable if Disjoint is FALSE.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DATA_TIMESTAMP_DISJOINT.Disjoint)</para>
    /// </summary>
    property Boolean Disjoint
    {
        Boolean get()
        {
            return disjoint;
        }

        void set(Boolean value)
        {
            disjoint = value;
        }
    }
private:

    UInt64 frequency;
    Boolean disjoint;

public:

    static Boolean operator == (QueryDataTimestampDisjoint queryDataTimestampDisjoint1, QueryDataTimestampDisjoint queryDataTimestampDisjoint2)
    {
        return (queryDataTimestampDisjoint1.frequency == queryDataTimestampDisjoint2.frequency) &&
            (queryDataTimestampDisjoint1.disjoint == queryDataTimestampDisjoint2.disjoint);
    }

    static Boolean operator != (QueryDataTimestampDisjoint queryDataTimestampDisjoint1, QueryDataTimestampDisjoint queryDataTimestampDisjoint2)
    {
        return !(queryDataTimestampDisjoint1 == queryDataTimestampDisjoint2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != QueryDataTimestampDisjoint::typeid)
        {
            return false;
        }

        return *this == safe_cast<QueryDataTimestampDisjoint>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + frequency.GetHashCode();
        hashCode = hashCode * 31 + disjoint.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a query.
/// <para>(Also see DirectX SDK: D3D11_QUERY_DESC)</para>
/// </summary>
public value struct QueryDescription
{
public:
    /// <summary>
    /// Type of query.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DESC.D3DQuery)</para>
    /// </summary>
    property Query Query
    {
        Direct3D11::Query get()
        {
            return query;
        }

        void set(Direct3D11::Query value)
        {
            query = value;
        }
    }
    /// <summary>
    /// Miscellaneous flags (see <see cref="MiscellaneousQueryOptions"/>)<seealso cref="MiscellaneousQueryOptions"/>.
    /// <para>(Also see DirectX SDK: D3D11_QUERY_DESC.MiscFlags)</para>
    /// </summary>
    property MiscellaneousQueryOptions MiscellaneousQueryOptions
    {
        Direct3D11::MiscellaneousQueryOptions get()
        {
            return miscFlags;
        }

        void set(Direct3D11::MiscellaneousQueryOptions value)
        {
            miscFlags = value;
        }
    }
private:

    Direct3D11::MiscellaneousQueryOptions miscFlags;

internal:
    QueryDescription (const D3D11_QUERY_DESC& desc)
    {
        Query = static_cast<Direct3D11::Query>(desc.Query);
        MiscellaneousQueryOptions = static_cast<Direct3D11::MiscellaneousQueryOptions>(desc.MiscFlags);
    }

    void CopyTo(D3D11_QUERY_DESC* desc)
    {
        desc->Query = static_cast<D3D11_QUERY>(Query);
        desc->MiscFlags = static_cast<UINT>(MiscellaneousQueryOptions);
    }

private:

    Direct3D11::Query query;

public:

    static Boolean operator == (QueryDescription queryDescription1, QueryDescription queryDescription2)
    {
        return (queryDescription1.miscFlags == queryDescription2.miscFlags) &&
            (queryDescription1.query == queryDescription2.query);
    }

    static Boolean operator != (QueryDescription queryDescription1, QueryDescription queryDescription2)
    {
        return !(queryDescription1 == queryDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != QueryDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<QueryDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + miscFlags.GetHashCode();
        hashCode = hashCode * 31 + query.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes rasterizer state.
/// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC)</para>
/// </summary>
public value struct RasterizerDescription
{
public:
    /// <summary>
    /// Determines the fill mode to use when rendering (see <see cref="FillMode"/>)<seealso cref="FillMode"/>.
    /// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC.FillMode)</para>
    /// </summary>
    property FillMode FillMode
    {
        Direct3D11::FillMode get()
        {
            return fillMode;
        }

        void set(Direct3D11::FillMode value)
        {
            fillMode = value;
        }
    }
    /// <summary>
    /// Indicates triangles facing the specified direction are not drawn (see <see cref="CullMode"/>)<seealso cref="CullMode"/>.
    /// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC.CullMode)</para>
    /// </summary>
    property CullMode CullMode
    {
        Direct3D11::CullMode get()
        {
            return cullMode;
        }

        void set(Direct3D11::CullMode value)
        {
            cullMode = value;
        }
    }
    /// <summary>
    /// Determines if a triangle is front- or back-facing. If this parameter is true, then a triangle will be considered front-facing if its vertices are counter-clockwise on the render target and considered back-facing if they are clockwise. If this parameter is false then the opposite is true.
    /// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC.FrontCounterClockwise)</para>
    /// </summary>
    property Boolean FrontCounterclockwise
    {
        Boolean get()
        {
            return frontCounterClockwise;
        }

        void set(Boolean value)
        {
            frontCounterClockwise = value;
        }
    }
    /// <summary>
    /// Depth value added to a given pixel.
    /// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC.DepthBias)</para>
    /// </summary>
    property Int32 DepthBias
    {
        Int32 get()
        {
            return depthBias;
        }

        void set(Int32 value)
        {
            depthBias = value;
        }
    }
    /// <summary>
    /// Maximum depth bias of a pixel.
    /// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC.DepthBiasClamp)</para>
    /// </summary>
    property Single DepthBiasClamp
    {
        Single get()
        {
            return depthBiasClamp;
        }

        void set(Single value)
        {
            depthBiasClamp = value;
        }
    }
    /// <summary>
    /// Scalar on a given pixel's slope.
    /// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC.SlopeScaledDepthBias)</para>
    /// </summary>
    property Single SlopeScaledDepthBias
    {
        Single get()
        {
            return slopeScaledDepthBias;
        }

        void set(Single value)
        {
            slopeScaledDepthBias = value;
        }
    }
    /// <summary>
    /// Enable clipping based on distance.
    /// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC.DepthClipEnable)</para>
    /// </summary>
    property Boolean DepthClipEnable
    {
        Boolean get()
        {
            return depthClipEnable;
        }

        void set(Boolean value)
        {
            depthClipEnable = value;
        }
    }
    /// <summary>
    /// Enable scissor-rectangle culling. All pixels ouside an active scissor rectangle are culled.
    /// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC.ScissorEnable)</para>
    /// </summary>
    property Boolean ScissorEnable
    {
        Boolean get()
        {
            return scissorEnable;
        }

        void set(Boolean value)
        {
            scissorEnable = value;
        }
    }
    /// <summary>
    /// Enable multisample antialiasing.
    /// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC.MultisampleEnable)</para>
    /// </summary>
    property Boolean MultisampleEnable
    {
        Boolean get()
        {
            return multisampleEnable;
        }

        void set(Boolean value)
        {
            multisampleEnable = value;
        }
    }
    /// <summary>
    /// Enable line antialiasing; only applies if doing line drawing and MultisampleEnable is false.
    /// <para>(Also see DirectX SDK: D3D11_RASTERIZER_DESC.AntialiasedLineEnable)</para>
    /// </summary>
    property Boolean AntiAliasedLineEnable
    {
        Boolean get()
        {
            return antiAliasedLineEnable;
        }

        void set(Boolean value)
        {
            antiAliasedLineEnable = value;
        }
    }

private:

    Direct3D11::FillMode fillMode;
    Direct3D11::CullMode cullMode;
    Boolean frontCounterClockwise;
    Int32 depthBias;
    Single depthBiasClamp;
    Single slopeScaledDepthBias;
    Boolean depthClipEnable;
    Boolean scissorEnable;
    Boolean multisampleEnable;
    Boolean antiAliasedLineEnable;

internal:
    RasterizerDescription(const D3D11_RASTERIZER_DESC& desc)
    {
        FillMode = static_cast<Direct3D11::FillMode>(desc.FillMode);
        CullMode = static_cast<Direct3D11::CullMode>(desc.CullMode);
        FrontCounterclockwise = desc.FrontCounterClockwise != 0;
        DepthBias = desc.DepthBias;
        DepthBiasClamp = desc.DepthBiasClamp;
        SlopeScaledDepthBias = desc.SlopeScaledDepthBias;
        DepthClipEnable = desc.DepthClipEnable != 0;
        ScissorEnable = desc.ScissorEnable != 0;
        MultisampleEnable = desc.MultisampleEnable != 0;
        AntiAliasedLineEnable = desc.AntialiasedLineEnable != 0;
    }

    void CopyTo(D3D11_RASTERIZER_DESC* desc)
    {
        desc->FillMode = static_cast<D3D11_FILL_MODE>(FillMode) ;
        desc->CullMode = static_cast<D3D11_CULL_MODE>(CullMode) ;

        desc->FrontCounterClockwise = FrontCounterclockwise ? 1 : 0;
        desc->DepthBias = DepthBias;
        desc->DepthBiasClamp = DepthBiasClamp;
        desc->SlopeScaledDepthBias = SlopeScaledDepthBias;

        desc->DepthClipEnable = DepthClipEnable? 1 : 0;
        desc->ScissorEnable = ScissorEnable? 1 : 0;
        desc->MultisampleEnable = MultisampleEnable? 1 : 0;
        desc->AntialiasedLineEnable = AntiAliasedLineEnable? 1 : 0;
    }
public:

    static Boolean operator == (RasterizerDescription rasterizerDescription1, RasterizerDescription rasterizerDescription2)
    {
        return (rasterizerDescription1.fillMode == rasterizerDescription2.fillMode) &&
            (rasterizerDescription1.cullMode == rasterizerDescription2.cullMode) &&
            (rasterizerDescription1.frontCounterClockwise == rasterizerDescription2.frontCounterClockwise) &&
            (rasterizerDescription1.depthBias == rasterizerDescription2.depthBias) &&
            (rasterizerDescription1.depthBiasClamp == rasterizerDescription2.depthBiasClamp) &&
            (rasterizerDescription1.slopeScaledDepthBias == rasterizerDescription2.slopeScaledDepthBias) &&
            (rasterizerDescription1.depthClipEnable == rasterizerDescription2.depthClipEnable) &&
            (rasterizerDescription1.scissorEnable == rasterizerDescription2.scissorEnable) &&
            (rasterizerDescription1.multisampleEnable == rasterizerDescription2.multisampleEnable) &&
            (rasterizerDescription1.antiAliasedLineEnable == rasterizerDescription2.antiAliasedLineEnable);
    }

    static Boolean operator != (RasterizerDescription rasterizerDescription1, RasterizerDescription rasterizerDescription2)
    {
        return !(rasterizerDescription1 == rasterizerDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != RasterizerDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<RasterizerDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + fillMode.GetHashCode();
        hashCode = hashCode * 31 + cullMode.GetHashCode();
        hashCode = hashCode * 31 + frontCounterClockwise.GetHashCode();
        hashCode = hashCode * 31 + depthBias.GetHashCode();
        hashCode = hashCode * 31 + depthBiasClamp.GetHashCode();
        hashCode = hashCode * 31 + slopeScaledDepthBias.GetHashCode();
        hashCode = hashCode * 31 + depthClipEnable.GetHashCode();
        hashCode = hashCode * 31 + scissorEnable.GetHashCode();
        hashCode = hashCode * 31 + multisampleEnable.GetHashCode();
        hashCode = hashCode * 31 + antiAliasedLineEnable.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// Specifies the subresources from a resource that are accessible using a render-target view.
/// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC)</para>
/// </summary>
[StructLayout(LayoutKind::Explicit)]
public value struct RenderTargetViewDescription
{
public:
    /// <summary>
    /// The data format (see <see cref="Format"/>)<seealso cref="Format"/>.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC.Format)</para>
    /// </summary>
    property Graphics::Format Format
    {
        Graphics::Format get()
        {
            return format;
        }

        void set(Graphics::Format value)
        {
            format = value;
        }
    }
    /// <summary>
    /// The resource type (see <see cref="RenderTargetViewDimension"/>)<seealso cref="RenderTargetViewDimension"/>, which specifies how the render-target resource will be accessed.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC.ViewDimension)</para>
    /// </summary>
    property RenderTargetViewDimension ViewDimension
    {
        RenderTargetViewDimension get()
        {
            return viewDimension;
        }

        void set(RenderTargetViewDimension value)
        {
            viewDimension = value;
        }
    }
    /// <summary>
    /// Specifies which buffer elements can be accessed (see <see cref="BufferRenderTargetView"/>)<seealso cref="BufferRenderTargetView"/>.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC.Buffer)</para>
    /// </summary>
    property BufferRenderTargetView Buffer
    {
        BufferRenderTargetView get()
        {
            return buffer;
        }

        void set(BufferRenderTargetView value)
        {
            buffer = value;
        }
    }
    /// <summary>
    /// Specifies the subresources in a 1D texture that can be accessed (see <see cref="Texture1DRenderTargetView"/>)<seealso cref="Texture1DRenderTargetView"/>.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC.Texture1D)</para>
    /// </summary>
    property Texture1DRenderTargetView Texture1D
    {
        Texture1DRenderTargetView get()
        {
            return texture1D;
        }

        void set(Texture1DRenderTargetView value)
        {
            texture1D = value;
        }
    }
    /// <summary>
    /// Specifies the subresources in a 1D texture array that can be accessed (see <see cref="Texture1DArrayRenderTargetView"/>)<seealso cref="Texture1DArrayRenderTargetView"/>.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC.Texture1DArray)</para>
    /// </summary>
    property Texture1DArrayRenderTargetView Texture1DArray
    {
        Texture1DArrayRenderTargetView get()
        {
            return texture1DArray;
        }

        void set(Texture1DArrayRenderTargetView value)
        {
            texture1DArray = value;
        }
    }
    /// <summary>
    /// Specifies the subresources in a 2D texture that can be accessed (see <see cref="Texture2DRenderTargetView"/>)<seealso cref="Texture2DRenderTargetView"/>.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC.Texture2D)</para>
    /// </summary>
    property Texture2DRenderTargetView Texture2D
    {
        Texture2DRenderTargetView get()
        {
            return texture2D;
        }

        void set(Texture2DRenderTargetView value)
        {
            texture2D = value;
        }
    }
    /// <summary>
    /// Specifies the subresources in a 2D texture array that can be accessed (see <see cref="Texture2DArrayRenderTargetView"/>)<seealso cref="Texture2DArrayRenderTargetView"/>.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC.Texture2DArray)</para>
    /// </summary>
    property Texture2DArrayRenderTargetView Texture2DArray
    {
        Texture2DArrayRenderTargetView get()
        {
            return texture2DArray;
        }

        void set(Texture2DArrayRenderTargetView value)
        {
            texture2DArray = value;
        }
    }
    /// <summary>
    /// Specifies a single subresource because a multisampled 2D texture only contains one subresource (see <see cref="Texture2DMultisampleRenderTargetView"/>)<seealso cref="Texture2DMultisampleRenderTargetView"/>.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC.Texture2DMS)</para>
    /// </summary>
    property Texture2DMultisampleRenderTargetView Texture2DMultisample
    {
        Texture2DMultisampleRenderTargetView get()
        {
            return texture2DMultisample;
        }

        void set(Texture2DMultisampleRenderTargetView value)
        {
            texture2DMultisample = value;
        }
    }
    /// <summary>
    /// Specifies the subresources in a multisampled 2D texture array that can be accessed (see <see cref="Texture2DMultisampleArrayRenderTargetView"/>)<seealso cref="Texture2DMultisampleArrayRenderTargetView"/>.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC.Texture2DMSArray)</para>
    /// </summary>
    property Texture2DMultisampleArrayRenderTargetView Texture2DMultisampleArray
    {
        Texture2DMultisampleArrayRenderTargetView get()
        {
            return texture2DMultisampleArray;
        }

        void set(Texture2DMultisampleArrayRenderTargetView value)
        {
            texture2DMultisampleArray = value;
        }
    }
    /// <summary>
    /// Specifies subresources in a 3D texture that can be accessed (see <see cref="Texture3DRenderTargetView"/>)<seealso cref="Texture3DRenderTargetView"/>.
    /// <para>(Also see DirectX SDK: D3D11_RENDER_TARGET_VIEW_DESC.Texture3D)</para>
    /// </summary>
    property Texture3DRenderTargetView Texture3D
    {
        Texture3DRenderTargetView get()
        {
            return texture3D;
        }

        void set(Texture3DRenderTargetView value)
        {
            texture3D = value;
        }
    }
private:

    [FieldOffset(0)]
    Graphics::Format format;
    [FieldOffset(4)]
    RenderTargetViewDimension viewDimension;
    [FieldOffset(8)]
    BufferRenderTargetView buffer;
    [FieldOffset(8)]
    Texture1DRenderTargetView texture1D;
    [FieldOffset(8)]
    Texture1DArrayRenderTargetView texture1DArray;
    [FieldOffset(8)]
    Texture2DRenderTargetView texture2D;
    [FieldOffset(8)]
    Texture2DArrayRenderTargetView texture2DArray;
    [FieldOffset(8)]
    Texture2DMultisampleRenderTargetView texture2DMultisample;
    [FieldOffset(8)]
    Texture2DMultisampleArrayRenderTargetView texture2DMultisampleArray;
    [FieldOffset(8)]
    Texture3DRenderTargetView texture3D;

};

/// <summary>
/// Describes a sampler state.
/// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC)</para>
/// </summary>
public value struct SamplerDescription
{
public:
    /// <summary>
    /// Filtering method to use when sampling a texture (see <see cref="Filter"/>)<seealso cref="Filter"/>.
    /// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC.Filter)</para>
    /// </summary>
    property Filter Filter
    {
        Direct3D11::Filter get()
        {
            return filter;
        }

        void set(Direct3D11::Filter value)
        {
            filter = value;
        }
    }
    /// <summary>
    /// Method to use for resolving a u texture coordinate that is outside the 0 to 1 range (see <see cref="TextureAddressMode"/>)<seealso cref="TextureAddressMode"/>.
    /// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC.AddressU)</para>
    /// </summary>
    property TextureAddressMode AddressU
    {
        TextureAddressMode get()
        {
            return addressU;
        }

        void set(TextureAddressMode value)
        {
            addressU = value;
        }
    }
    /// <summary>
    /// Method to use for resolving a v texture coordinate that is outside the 0 to 1 range.
    /// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC.AddressV)</para>
    /// </summary>
    property TextureAddressMode AddressV
    {
        TextureAddressMode get()
        {
            return addressV;
        }

        void set(TextureAddressMode value)
        {
            addressV = value;
        }
    }
    /// <summary>
    /// Method to use for resolving a w texture coordinate that is outside the 0 to 1 range.
    /// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC.AddressW)</para>
    /// </summary>
    property TextureAddressMode AddressW
    {
        TextureAddressMode get()
        {
            return addressW;
        }

        void set(TextureAddressMode value)
        {
            addressW = value;
        }
    }
    /// <summary>
    /// offset from the calculated mipmap level. For example, if Direct3D calculates that a texture should be sampled at mipmap level 3 and MipLevelOfDetailBias is 2, then the texture will be sampled at mipmap level 5.
    /// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC.MipLODBias)</para>
    /// </summary>
    property Single MipLevelOfDetailBias
    {
        Single get()
        {
            return mipLODBias;
        }

        void set(Single value)
        {
            mipLODBias = value;
        }
    }
    /// <summary>
    /// Clamping value used if Anisotropic or ComparisonAnisotropic is specified in Filter. Valid values are between 1 and 16.
    /// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC.MaxAnisotropy)</para>
    /// </summary>
    property UInt32 MaxAnisotropy
    {
        UInt32 get()
        {
            return maxAnisotropy;
        }

        void set(UInt32 value)
        {
            maxAnisotropy = value;
        }
    }
    /// <summary>
    /// A function that compares sampled data against existing sampled data. The function options are listed in ComparisonFunction.
    /// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC.ComparisonFunction)</para>
    /// </summary>
    property ComparisonFunction ComparisonFunction
    {
        Direct3D11::ComparisonFunction get()
        {
            return comparisonFunction;
        }

        void set(Direct3D11::ComparisonFunction value)
        {
            comparisonFunction = value;
        }
    }
    /// <summary>
    /// Border color to use if Border is specified for AddressU, AddressV, or AddressW. Range must be between 0.0 and 1.0 inclusive.
    /// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC.BorderColor)</para>
    /// </summary>
    property ColorRgba BorderColor
    {
        ColorRgba get()
        {
            return borderColor;
        }

        void set(ColorRgba value)
        {
            borderColor = value;
        }
    }

    /// <summary>
    /// Lower end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap level and any level higher than that is less detailed.
    /// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC.MinLOD)</para>
    /// </summary>
    property Single MinimumLevelOfDetail
    {
        Single get()
        {
            return minLOD;
        }

        void set(Single value)
        {
            minLOD = value;
        }
    }
    /// <summary>
    /// Upper end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap level and any level higher than that is less detailed. This value must be greater than or equal to MinimumLevelOfDetail. To have no upper limit on LOD set this to a large value such as D3D11_FLOAT32_MAX.
    /// <para>(Also see DirectX SDK: D3D11_SAMPLER_DESC.MaxLOD)</para>
    /// </summary>
    property Single MaximumLevelOfDetail
    {
        Single get()
        {
            return maxLOD;
        }

        void set(Single value)
        {
            maxLOD = value;
        }
    }

private:

    Direct3D11::Filter filter;
    TextureAddressMode addressU;
    TextureAddressMode addressV;
    TextureAddressMode addressW;
    Single mipLODBias;
    UInt32 maxAnisotropy;
    Direct3D11::ComparisonFunction comparisonFunction;
    ColorRgba borderColor;
    Single minLOD;
    Single maxLOD;

internal:
    SamplerDescription(const D3D11_SAMPLER_DESC & desc)
    {
        Filter = static_cast<Direct3D11::Filter>(desc.Filter);
        AddressU = static_cast<TextureAddressMode>(desc.AddressU);
        AddressV = static_cast<TextureAddressMode>(desc.AddressV);
        AddressW = static_cast<TextureAddressMode>(desc.AddressW);
        
        MipLevelOfDetailBias = desc.MipLODBias;
        MaxAnisotropy = desc.MaxAnisotropy;

        ComparisonFunction = static_cast<Direct3D11::ComparisonFunction>(desc.ComparisonFunc);

        BorderColor = ColorRgba(desc.BorderColor);

        MinimumLevelOfDetail = desc.MinLOD;
        MaximumLevelOfDetail = desc.MaxLOD;
    }

    void CopyTo(D3D11_SAMPLER_DESC* desc)
    {
        desc->Filter = static_cast<D3D11_FILTER>(Filter);
        desc->AddressU = static_cast<D3D11_TEXTURE_ADDRESS_MODE>(AddressU);
        desc->AddressV = static_cast<D3D11_TEXTURE_ADDRESS_MODE>(AddressV);
        desc->AddressW = static_cast<D3D11_TEXTURE_ADDRESS_MODE>(AddressW);
        
        desc->MipLODBias = MipLevelOfDetailBias;
        desc->MaxAnisotropy = MaxAnisotropy;

        desc->ComparisonFunc= static_cast<D3D11_COMPARISON_FUNC>(ComparisonFunction);

        desc->BorderColor[0] = BorderColor.Red;
        desc->BorderColor[1] = BorderColor.Green;
        desc->BorderColor[2] = BorderColor.Blue;
        desc->BorderColor[3] = BorderColor.Alpha;


        desc->MinLOD = MinimumLevelOfDetail;
        desc->MaxLOD = MaximumLevelOfDetail;
    }
public:

    static Boolean operator == (SamplerDescription samplerDescription1, SamplerDescription samplerDescription2)
    {
        return (samplerDescription1.filter == samplerDescription2.filter) &&
            (samplerDescription1.addressU == samplerDescription2.addressU) &&
            (samplerDescription1.addressV == samplerDescription2.addressV) &&
            (samplerDescription1.addressW == samplerDescription2.addressW) &&
            (samplerDescription1.mipLODBias == samplerDescription2.mipLODBias) &&
            (samplerDescription1.maxAnisotropy == samplerDescription2.maxAnisotropy) &&
            (samplerDescription1.comparisonFunction == samplerDescription2.comparisonFunction) &&
            (samplerDescription1.borderColor == samplerDescription2.borderColor) &&
            (samplerDescription1.minLOD == samplerDescription2.minLOD) &&
            (samplerDescription1.maxLOD == samplerDescription2.maxLOD);
    }

    static Boolean operator != (SamplerDescription samplerDescription1, SamplerDescription samplerDescription2)
    {
        return !(samplerDescription1 == samplerDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != SamplerDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<SamplerDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + filter.GetHashCode();
        hashCode = hashCode * 31 + addressU.GetHashCode();
        hashCode = hashCode * 31 + addressV.GetHashCode();
        hashCode = hashCode * 31 + addressW.GetHashCode();
        hashCode = hashCode * 31 + mipLODBias.GetHashCode();
        hashCode = hashCode * 31 + maxAnisotropy.GetHashCode();
        hashCode = hashCode * 31 + comparisonFunction.GetHashCode();
        hashCode = hashCode * 31 + borderColor.GetHashCode();
        hashCode = hashCode * 31 + minLOD.GetHashCode();
        hashCode = hashCode * 31 + maxLOD.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a shader-resource view.
/// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC)</para>
/// </summary>
[StructLayout(LayoutKind::Explicit)]
public value struct ShaderResourceViewDescription
{
public:
    /// <summary>
    /// A Format specifying the viewing format.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.Format)</para>
    /// </summary>
    property Graphics::Format Format
    {
        Graphics::Format get()
        {
            return format;
        }

        void set(Graphics::Format value)
        {
            format = value;
        }
    }
    /// <summary>
    /// The resource type of the view. See ShaderResourceViewDimension. This should be the same as the resource type of the underlying resource. This parameter also determines which _SRV to use in the union below.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.ViewDimension)</para>
    /// </summary>
    property ShaderResourceViewDimension ViewDimension
    {
        ShaderResourceViewDimension get()
        {
            return viewDimension;
        }

        void set(ShaderResourceViewDimension value)
        {
            viewDimension = value;
        }
    }
    /// <summary>
    /// View the resource as a buffer using information from a shader-resource view (see <see cref="BufferShaderResourceView"/>)<seealso cref="BufferShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.Buffer)</para>
    /// </summary>
    property BufferShaderResourceView Buffer
    {
        BufferShaderResourceView get()
        {
            return buffer;
        }

        void set(BufferShaderResourceView value)
        {
            buffer = value;
        }
    }
    /// <summary>
    /// View the resource as a 1D texture using information from a shader-resource view (see <see cref="Texture1DShaderResourceView"/>)<seealso cref="Texture1DShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.Texture1D)</para>
    /// </summary>
    property Texture1DShaderResourceView Texture1D
    {
        Texture1DShaderResourceView get()
        {
            return texture1D;
        }

        void set(Texture1DShaderResourceView value)
        {
            texture1D = value;
        }
    }
    /// <summary>
    /// View the resource as a 1D-texture array using information from a shader-resource view (see <see cref="Texture1DArrayShaderResourceView"/>)<seealso cref="Texture1DArrayShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.Texture1DArray)</para>
    /// </summary>
    property Texture1DArrayShaderResourceView Texture1DArray
    {
        Texture1DArrayShaderResourceView get()
        {
            return texture1DArray;
        }

        void set(Texture1DArrayShaderResourceView value)
        {
            texture1DArray = value;
        }
    }
    /// <summary>
    /// View the resource as a 2D-texture using information from a shader-resource view (see <see cref="Texture2DShaderResourceView"/>)<seealso cref="Texture2DShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.Texture2D)</para>
    /// </summary>
    property Texture2DShaderResourceView Texture2D
    {
        Texture2DShaderResourceView get()
        {
            return texture2D;
        }

        void set(Texture2DShaderResourceView value)
        {
            texture2D = value;
        }
    }
    /// <summary>
    /// View the resource as a 2D-texture array using information from a shader-resource view (see <see cref="Texture2DArrayShaderResourceView"/>)<seealso cref="Texture2DArrayShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.Texture2DArray)</para>
    /// </summary>
    property Texture2DArrayShaderResourceView Texture2DArray
    {
        Texture2DArrayShaderResourceView get()
        {
            return texture2DArray;
        }

        void set(Texture2DArrayShaderResourceView value)
        {
            texture2DArray = value;
        }
    }
    /// <summary>
    /// View the resource as a 2D-multisampled texture using information from a shader-resource view (see <see cref="Texture2DMultisampleShaderResourceView"/>)<seealso cref="Texture2DMultisampleShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.Texture2DMultisample)</para>
    /// </summary>
    property Texture2DMultisampleShaderResourceView Texture2DMultisample
    {
        Texture2DMultisampleShaderResourceView get()
        {
            return texture2DMultisample;
        }

        void set(Texture2DMultisampleShaderResourceView value)
        {
            texture2DMultisample = value;
        }
    }
    /// <summary>
    /// View the resource as a 2D-multisampled-texture array using information from a shader-resource view (see <see cref="Texture2DMultisampleArrayShaderResourceView"/>)<seealso cref="Texture2DMultisampleArrayShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.Texture2DMSArray)</para>
    /// </summary>
    property Texture2DMultisampleArrayShaderResourceView Texture2DMultisampleArray
    {
        Texture2DMultisampleArrayShaderResourceView get()
        {
            return texture2DMultisampleArray;
        }

        void set(Texture2DMultisampleArrayShaderResourceView value)
        {
            texture2DMultisampleArray = value;
        }
    }
    /// <summary>
    /// View the resource as a 3D texture using information from a shader-resource view (see <see cref="Texture3DShaderResourceView"/>)<seealso cref="Texture3DShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.Texture3D)</para>
    /// </summary>
    property Texture3DShaderResourceView Texture3D
    {
        Texture3DShaderResourceView get()
        {
            return texture3D;
        }

        void set(Texture3DShaderResourceView value)
        {
            texture3D = value;
        }
    }
    /// <summary>
    /// View the resource as a 3D-cube texture using information from a shader-resource view (see <see cref="TextureCubeShaderResourceView"/>)<seealso cref="TextureCubeShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.TextureCube)</para>
    /// </summary>
    property TextureCubeShaderResourceView TextureCube
    {
        TextureCubeShaderResourceView get()
        {
            return textureCube;
        }

        void set(TextureCubeShaderResourceView value)
        {
            textureCube = value;
        }
    }
    /// <summary>
    /// View the resource as a 3D-cube-texture array using information from a shader-resource view (see <see cref="TextureCubeArrayShaderResourceView"/>)<seealso cref="TextureCubeArrayShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.TextureCubeArray)</para>
    /// </summary>
    property TextureCubeArrayShaderResourceView TextureCubeArray
    {
        TextureCubeArrayShaderResourceView get()
        {
            return textureCubeArray;
        }

        void set(TextureCubeArrayShaderResourceView value)
        {
            textureCubeArray = value;
        }
    }
    /// <summary>
    /// View the resource as an extended buffer using information from a shader-resource view (see <see cref="ExtendedBufferShaderResourceView"/>)<seealso cref="ExtendedBufferShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D11_SHADER_RESOURCE_VIEW_DESC.BufferEx)</para>
    /// </summary>
    property ExtendedBufferShaderResourceView ExtendedBuffer
    {
        ExtendedBufferShaderResourceView get()
        {
            return extendedBuffer;
        }

        void set(ExtendedBufferShaderResourceView value)
        {
            extendedBuffer = value;
        }
    }

private:

    [FieldOffset(0)]
    Graphics::Format format;
    [FieldOffset(4)]
    ShaderResourceViewDimension viewDimension;
    [FieldOffset(8)]
    BufferShaderResourceView buffer;
    [FieldOffset(8)]
    Texture1DShaderResourceView texture1D;
    [FieldOffset(8)]
    Texture1DArrayShaderResourceView texture1DArray;
    [FieldOffset(8)]
    Texture2DShaderResourceView texture2D;
    [FieldOffset(8)]
    Texture2DArrayShaderResourceView texture2DArray;
    [FieldOffset(8)]
    Texture2DMultisampleShaderResourceView texture2DMultisample;
    [FieldOffset(8)]
    Texture2DMultisampleArrayShaderResourceView texture2DMultisampleArray;
    [FieldOffset(8)]
    Texture3DShaderResourceView texture3D;
    [FieldOffset(8)]
    TextureCubeShaderResourceView textureCube;
    [FieldOffset(8)]
    TextureCubeArrayShaderResourceView textureCubeArray;
    [FieldOffset(8)]
    ExtendedBufferShaderResourceView extendedBuffer;

internal:
    ShaderResourceViewDescription(const D3D11_SHADER_RESOURCE_VIEW_DESC& desc)
    {
        Format = static_cast<Graphics::Format>(desc.Format);
        ViewDimension = static_cast<ShaderResourceViewDimension>(desc.ViewDimension);

        switch (ViewDimension)
        {
        case ShaderResourceViewDimension::Buffer :
               {
                   BufferShaderResourceView buffer;

                   buffer.ElementOffset = desc.Buffer.ElementOffset;
                   buffer.ElementWidth = desc.Buffer.ElementWidth;

                   Buffer = buffer;
                   break;
               }
        case ShaderResourceViewDimension::Texture1D :
              {
                  Texture1DShaderResourceView texture1D;

                  texture1D.MipLevels = desc.Texture1D.MipLevels;
                  texture1D.MostDetailedMip = desc.Texture1D.MostDetailedMip;

                  Texture1D = texture1D;
                  break;
              }
        case ShaderResourceViewDimension::Texture1DArray :
              {
                  Texture1DArrayShaderResourceView texture1DArray;

                  texture1DArray.ArraySize = desc.Texture1DArray.ArraySize;
                  texture1DArray.FirstArraySlice = desc.Texture1DArray.FirstArraySlice;
                  texture1DArray.MipLevels = desc.Texture1DArray.MipLevels;
                  texture1DArray.MostDetailedMip = desc.Texture1DArray.MostDetailedMip;

                  Texture1DArray = texture1DArray;
                  break;
              }
        case ShaderResourceViewDimension::Texture2D :
              {
                  Texture2DShaderResourceView texture2D;

                  texture2D.MipLevels = desc.Texture2D.MipLevels;
                  texture2D.MostDetailedMip = desc.Texture2D.MostDetailedMip;

                  Texture2D = texture2D;
                  break;
              }
        case ShaderResourceViewDimension::Texture2DArray :
              {
                  Texture2DArrayShaderResourceView texture2DArray;

                  texture2DArray.ArraySize = desc.Texture2DArray.ArraySize;
                  texture2DArray.FirstArraySlice = desc.Texture2DArray.FirstArraySlice;
                  texture2DArray.MipLevels = desc.Texture2DArray.MipLevels;
                  texture2DArray.MostDetailedMip = desc.Texture2DArray.MostDetailedMip;

                  Texture2DArray = texture2DArray;
                  break;
              }
        case ShaderResourceViewDimension::Texture2DMultisample :
              {
                  Texture2DMultisampleShaderResourceView texture2DMultisample;

                  texture2DMultisample.UnusedField = desc.Texture2DMS.UnusedField_NothingToDefine;

                  Texture2DMultisample = texture2DMultisample;
                  break;
              }
        case ShaderResourceViewDimension::Texture2DMultisampleArray :
              {
                  Texture2DMultisampleArrayShaderResourceView texture2DMultisampleArray;

                  texture2DMultisampleArray.ArraySize = desc.Texture2DMSArray.ArraySize;
                  texture2DMultisampleArray.FirstArraySlice = desc.Texture2DMSArray.FirstArraySlice;

                  Texture2DMultisampleArray = texture2DMultisampleArray;
                  break;
              }
        case ShaderResourceViewDimension::Texture3D :
              {
                  Texture3DShaderResourceView texture3D;

                  texture3D.MipLevels = desc.Texture3D.MipLevels;
                  texture3D.MostDetailedMip = desc.Texture3D.MostDetailedMip;

                  Texture3D = texture3D;
                  break;
              }
        case ShaderResourceViewDimension::TextureCube :
              {
                  TextureCubeShaderResourceView textureCube;

                  textureCube.MipLevels = desc.TextureCube.MipLevels;
                  textureCube.MostDetailedMip = desc.TextureCube.MostDetailedMip;

                  TextureCube = textureCube;
                  break;
              }
        case ShaderResourceViewDimension::TextureCubeArray :
              {
                  TextureCubeArrayShaderResourceView textureCubeArray;

                  textureCubeArray.MostDetailedMip = desc.TextureCubeArray.MostDetailedMip;
                  textureCubeArray.MipLevels = desc.TextureCubeArray.MipLevels;
                  textureCubeArray.First2DArrayFace = desc.TextureCubeArray.First2DArrayFace;
                  textureCubeArray.CubeCount = desc.TextureCubeArray.NumCubes;

                  TextureCubeArray = textureCubeArray;
                  break;
              }
        case ShaderResourceViewDimension::ExtendedBuffer :
              {
                  ExtendedBufferShaderResourceView extendedBuffer;

                  extendedBuffer.FirstElement = desc.BufferEx.FirstElement;
                  extendedBuffer.ElementCount = desc.BufferEx.NumElements;
                  extendedBuffer.BindingOptions = static_cast<ExtendedBufferBindingOptions>(desc.BufferEx.Flags);

                  ExtendedBuffer = extendedBuffer;
                  break;
              }
        default :
              {
                  throw gcnew NotSupportedException("Unknown or not supported ViewDimension.");
              }
        }
    }

    void CopyTo(D3D11_SHADER_RESOURCE_VIEW_DESC* desc)
    {
        desc->Format = static_cast<DXGI_FORMAT>(Format);
        desc->ViewDimension = static_cast<D3D11_SRV_DIMENSION>(ViewDimension);

        switch (ViewDimension)
        {
        case ShaderResourceViewDimension::Buffer :
               {
                   desc->Buffer.ElementOffset = Buffer.ElementOffset;
                   desc->Buffer.ElementWidth = Buffer.ElementWidth;
                   break;
               }
        case ShaderResourceViewDimension::Texture1D :
              {
                  desc->Texture1D.MipLevels = Texture1D.MipLevels;
                  desc->Texture1D.MostDetailedMip = Texture1D.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension::Texture1DArray :
              {
                  desc->Texture1DArray.ArraySize = Texture1DArray.ArraySize;
                  desc->Texture1DArray.FirstArraySlice = Texture1DArray.FirstArraySlice;
                  desc->Texture1DArray.MipLevels = Texture1DArray.MipLevels;
                  desc->Texture1DArray.MostDetailedMip = Texture1DArray.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension::Texture2D :
              {
                  desc->Texture2D.MipLevels = Texture2D.MipLevels;
                  desc->Texture2D.MostDetailedMip = Texture2D.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension::Texture2DArray :
              {
                  desc->Texture2DArray.ArraySize = Texture2DArray.ArraySize;
                  desc->Texture2DArray.FirstArraySlice = Texture2DArray.FirstArraySlice;
                  desc->Texture2DArray.MipLevels = Texture2DArray.MipLevels;
                  desc->Texture2DArray.MostDetailedMip = Texture2DArray.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension::Texture2DMultisample :
              {
                  desc->Texture2DMS.UnusedField_NothingToDefine = Texture2DMultisample.UnusedField;
                  break;
              }
        case ShaderResourceViewDimension::Texture2DMultisampleArray :
              {
                  desc->Texture2DMSArray.ArraySize = Texture2DMultisampleArray.ArraySize;
                  desc->Texture2DMSArray.FirstArraySlice = Texture2DMultisampleArray.FirstArraySlice;
                  break;
              }
        case ShaderResourceViewDimension::Texture3D :
              {
                  desc->Texture3D.MipLevels = Texture3D.MipLevels;
                  desc->Texture3D.MostDetailedMip = Texture3D.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension::TextureCube :
              {
                  desc->TextureCube.MipLevels = TextureCube.MipLevels;
                  desc->TextureCube.MostDetailedMip = TextureCube.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension::TextureCubeArray :
              {
                  desc->TextureCubeArray.MostDetailedMip = TextureCubeArray.MostDetailedMip;
                  desc->TextureCubeArray.MipLevels = TextureCubeArray.MipLevels;
                  desc->TextureCubeArray.First2DArrayFace = TextureCubeArray.First2DArrayFace;
                  desc->TextureCubeArray.NumCubes = TextureCubeArray.CubeCount;
                  break;
              }
        case ShaderResourceViewDimension::ExtendedBuffer :
              {
                  desc->BufferEx.FirstElement = ExtendedBuffer.FirstElement;
                  desc->BufferEx.NumElements = ExtendedBuffer.ElementCount;
                  desc->BufferEx.Flags = static_cast<D3D11_BUFFEREX_SRV_FLAG>(ExtendedBuffer.BindingOptions);
                  break;
              }
        default:
              {
                  throw gcnew NotSupportedException("Unknown or not supported ViewDimension.");
              }
        }
    }



};

/// <summary>
/// Description of a vertex element in a vertex buffer in an output slot.
/// <para>(Also see DirectX SDK: D3D11_SO_DECLARATION_ENTRY)</para>
/// </summary>
public value struct StreamOutputDeclarationEntry
{
public:
    /// <summary>
    /// Zero-based, stream index.
    /// <para>(Also see DirectX SDK: D3D11_SO_DECLARATION_ENTRY.Stream)</para>
    /// </summary>
    property UInt32 StreamIndex
    {
        UInt32 get()
        {
            return streamIndex;
        }

        void set(UInt32 value)
        {
            streamIndex = value;
        }
    }
    /// <summary>
    /// Type of output element; possible values include: "POSITION", "NORMAL", or "TEXCOORD0".
    /// <para>(Also see DirectX SDK: D3D11_SO_DECLARATION_ENTRY.SemanticName)</para>
    /// </summary>
    property String^ SemanticName
    {
        String^ get()
        {
            return semanticName;
        }

        void set(String^ value)
        {
            semanticName = value;
        }
    }
    /// <summary>
    /// Output element's zero-based index. Should be used if, for example, you have more than one texture coordinate stored in each vertex.
    /// <para>(Also see DirectX SDK: D3D11_SO_DECLARATION_ENTRY.SemanticIndex)</para>
    /// </summary>
    property UInt32 SemanticIndex
    {
        UInt32 get()
        {
            return semanticIndex;
        }

        void set(UInt32 value)
        {
            semanticIndex = value;
        }
    }
    /// <summary>
    /// Which component of the entry to begin writing out to. Valid values are 0 ~ 3. For example, if you only wish to output to the y and z components of a position, then StartComponent should be 1 and ComponentCount should be 2.
    /// <para>(Also see DirectX SDK: D3D11_SO_DECLARATION_ENTRY.StartComponent)</para>
    /// </summary>
    property Byte StartComponent
    {
        Byte get()
        {
            return startComponent;
        }

        void set(Byte value)
        {
            startComponent = value;
        }
    }
    /// <summary>
    /// The number of components of the entry to write out to. Valid values are 1 ~ 4. For example, if you only wish to output to the y and z components of a position, then StartComponent should be 1 and ComponentCount should be 2.
    /// <para>(Also see DirectX SDK: D3D11_SO_DECLARATION_ENTRY.ComponentCount)</para>
    /// </summary>
    property Byte ComponentCount
    {
        Byte get()
        {
            return componentCount;
        }

        void set(Byte value)
        {
            componentCount = value;
        }
    }
    /// <summary>
    /// The output slot that contains the vertex buffer that contains this output entry.
    /// <para>(Also see DirectX SDK: D3D11_SO_DECLARATION_ENTRY.OutputSlot)</para>
    /// </summary>
    property Byte OutputSlot
    {
        Byte get()
        {
            return outputSlot;
        }

        void set(Byte value)
        {
            outputSlot = value;
        }
    }
private:

    UInt32 streamIndex;
    String^ semanticName;
    UInt32 semanticIndex;
    Byte startComponent;
    Byte componentCount;
    Byte outputSlot;

internal:
    StreamOutputDeclarationEntry(const D3D11_SO_DECLARATION_ENTRY& entry)
    {
        StreamIndex = entry.Stream;
        SemanticIndex = entry.SemanticIndex;
        StartComponent = entry.StartComponent;
        ComponentCount = entry.ComponentCount;
        OutputSlot = entry.OutputSlot;
        SemanticName = gcnew String(entry.SemanticName);
    }

    void CopyTo(D3D11_SO_DECLARATION_ENTRY* entry, marshal_context^ context)
    {
        entry->Stream = StreamIndex;
        entry->SemanticIndex = SemanticIndex;
        entry->StartComponent = StartComponent;
        entry->ComponentCount = ComponentCount;
        entry->OutputSlot = OutputSlot;
        
        String^ name = SemanticName;
        entry->SemanticName = context->marshal_as<const char*>(name);
    }
public:

    static Boolean operator == (StreamOutputDeclarationEntry streamOutputDeclarationEntry1, StreamOutputDeclarationEntry streamOutputDeclarationEntry2)
    {
        return (streamOutputDeclarationEntry1.streamIndex == streamOutputDeclarationEntry2.streamIndex) &&
            (streamOutputDeclarationEntry1.semanticName == streamOutputDeclarationEntry2.semanticName) &&
            (streamOutputDeclarationEntry1.semanticIndex == streamOutputDeclarationEntry2.semanticIndex) &&
            (streamOutputDeclarationEntry1.startComponent == streamOutputDeclarationEntry2.startComponent) &&
            (streamOutputDeclarationEntry1.componentCount == streamOutputDeclarationEntry2.componentCount) &&
            (streamOutputDeclarationEntry1.outputSlot == streamOutputDeclarationEntry2.outputSlot);
    }

    static Boolean operator != (StreamOutputDeclarationEntry streamOutputDeclarationEntry1, StreamOutputDeclarationEntry streamOutputDeclarationEntry2)
    {
        return !(streamOutputDeclarationEntry1 == streamOutputDeclarationEntry2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != StreamOutputDeclarationEntry::typeid)
        {
            return false;
        }

        return *this == safe_cast<StreamOutputDeclarationEntry>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + streamIndex.GetHashCode();
        hashCode = hashCode * 31 + semanticName->GetHashCode();
        hashCode = hashCode * 31 + semanticIndex.GetHashCode();
        hashCode = hashCode * 31 + startComponent.GetHashCode();
        hashCode = hashCode * 31 + componentCount.GetHashCode();
        hashCode = hashCode * 31 + outputSlot.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies data for initializing a subresource.
/// <para>(Also see DirectX SDK: D3D11_SUBRESOURCE_DATA)</para>
/// </summary>
public value struct SubresourceData
{
public:
    /// <summary>
    /// Pointer to the initialization data.
    /// <para>(Also see DirectX SDK: D3D11_SUBRESOURCE_DATA.pSysMem)</para>
    /// </summary>
    property IntPtr SystemMemory
    {
        IntPtr get()
        {
            return sysMem;
        }

        void set(IntPtr value)
        {
            sysMem = value;
        }
    }

    /// <summary>
    /// Pitch of the memory (in bytes). System-memory pitch is used only for 2D and 3D texture data as it is has no meaning for the other resource types.
    /// <para>(Also see DirectX SDK: D3D11_SUBRESOURCE_DATA.SysMemPitch)</para>
    /// </summary>
    property UInt32 SystemMemoryPitch
    {
        UInt32 get()
        {
            return sysMemPitch;
        }

        void set(UInt32 value)
        {
            sysMemPitch = value;
        }
    }

    /// <summary>
    /// Size of one depth level (in bytes). System-memory-slice pitch is only used for 3D texture data as it has no meaning for the other resource types.
    /// <para>(Also see DirectX SDK: D3D11_SUBRESOURCE_DATA.SysMemSlicePitch)</para>
    /// </summary>
    property UInt32 SystemMemorySlicePitch
    {
        UInt32 get()
        {
            return sysMemSlicePitch;
        }

        void set(UInt32 value)
        {
            sysMemSlicePitch = value;
        }
    }
private:

    UInt32 sysMemPitch;
    UInt32 sysMemSlicePitch;

internal:
    SubresourceData(const D3D11_SUBRESOURCE_DATA & subresourceData)
    {
        SystemMemory = IntPtr((void*)subresourceData.pSysMem);
        SystemMemoryPitch = subresourceData.SysMemPitch;
        SystemMemorySlicePitch = subresourceData.SysMemSlicePitch;
    }
    void CopyTo(D3D11_SUBRESOURCE_DATA &subresourceData)
    {
        subresourceData.pSysMem = SystemMemory.ToPointer();
        subresourceData.SysMemPitch = SystemMemoryPitch;
        subresourceData.SysMemSlicePitch = SystemMemorySlicePitch;
    }
private:

    IntPtr sysMem;

};

/// <summary>
/// Specifies the subresources from a resource that are accessible using an unordered-access view.
/// <para>(Also see DirectX SDK: D3D11_UNORDERED_ACCESS_VIEW_DESC)</para>
/// </summary>
[StructLayout(LayoutKind::Explicit)]
public value struct UnorderedAccessViewDescription
{
public:
    /// <summary>
    /// The data format (see <see cref="Format"/>)<seealso cref="Format"/>.
    /// <para>(Also see DirectX SDK: D3D11_UNORDERED_ACCESS_VIEW_DESC.Format)</para>
    /// </summary>
    property Graphics::Format Format
    {
        Graphics::Format get()
        {
            return format;
        }

        void set(Graphics::Format value)
        {
            format = value;
        }
    }
    /// <summary>
    /// The resource type (see <see cref="UnorderedAccessViewDimension"/>)<seealso cref="UnorderedAccessViewDimension"/>, which specifies how the resource will be accessed.
    /// <para>(Also see DirectX SDK: D3D11_UNORDERED_ACCESS_VIEW_DESC.ViewDimension)</para>
    /// </summary>
    property UnorderedAccessViewDimension ViewDimension
    {
        UnorderedAccessViewDimension get()
        {
            return viewDimension;
        }

        void set(UnorderedAccessViewDimension value)
        {
            viewDimension = value;
        }
    }
    /// <summary>
    /// Specifies which buffer elements can be accessed (see <see cref="BufferUnorderedAccessView"/>)<seealso cref="BufferUnorderedAccessView"/>.
    /// <para>(Also see DirectX SDK: D3D11_UNORDERED_ACCESS_VIEW_DESC.Buffer)</para>
    /// </summary>
    property BufferUnorderedAccessView Buffer
    {
        BufferUnorderedAccessView get()
        {
            return buffer;
        }

        void set(BufferUnorderedAccessView value)
        {
            buffer = value;
        }
    }
    /// <summary>
    /// Specifies the subresources in a 1D texture that can be accessed (see <see cref="Texture1DUnorderedAccessView"/>)<seealso cref="Texture1DUnorderedAccessView"/>.
    /// <para>(Also see DirectX SDK: D3D11_UNORDERED_ACCESS_VIEW_DESC.Texture1D)</para>
    /// </summary>
    property Texture1DUnorderedAccessView Texture1D
    {
        Texture1DUnorderedAccessView get()
        {
            return texture1D;
        }

        void set(Texture1DUnorderedAccessView value)
        {
            texture1D = value;
        }
    }
    /// <summary>
    /// Specifies the subresources in a 1D texture array that can be accessed (see <see cref="Texture1DArrayUnorderedAccessView"/>)<seealso cref="Texture1DArrayUnorderedAccessView"/>.
    /// <para>(Also see DirectX SDK: D3D11_UNORDERED_ACCESS_VIEW_DESC.Texture1DArray)</para>
    /// </summary>
    property Texture1DArrayUnorderedAccessView Texture1DArray
    {
        Texture1DArrayUnorderedAccessView get()
        {
            return texture1DArray;
        }

        void set(Texture1DArrayUnorderedAccessView value)
        {
            texture1DArray = value;
        }
    }
    /// <summary>
    /// Specifies the subresources in a 2D texture that can be accessed (see <see cref="Texture2DUnorderedAccessView"/>)<seealso cref="Texture2DUnorderedAccessView"/>.
    /// <para>(Also see DirectX SDK: D3D11_UNORDERED_ACCESS_VIEW_DESC.Texture2D)</para>
    /// </summary>
    property Texture2DUnorderedAccessView Texture2D
    {
        Texture2DUnorderedAccessView get()
        {
            return texture2D;
        }

        void set(Texture2DUnorderedAccessView value)
        {
            texture2D = value;
        }
    }
    /// <summary>
    /// Specifies the subresources in a 2D texture array that can be accessed (see <see cref="Texture2DArrayUnorderedAccessView"/>)<seealso cref="Texture2DArrayUnorderedAccessView"/>.
    /// <para>(Also see DirectX SDK: D3D11_UNORDERED_ACCESS_VIEW_DESC.Texture2DArray)</para>
    /// </summary>
    property Texture2DArrayUnorderedAccessView Texture2DArray
    {
        Texture2DArrayUnorderedAccessView get()
        {
            return texture2DArray;
        }

        void set(Texture2DArrayUnorderedAccessView value)
        {
            texture2DArray = value;
        }
    }
    /// <summary>
    /// Specifies subresources in a 3D texture that can be accessed (see <see cref="Texture3DUnorderedAccessView"/>)<seealso cref="Texture3DUnorderedAccessView"/>.
    /// <para>(Also see DirectX SDK: D3D11_UNORDERED_ACCESS_VIEW_DESC.Texture3D)</para>
    /// </summary>
    property Texture3DUnorderedAccessView Texture3D
    {
        Texture3DUnorderedAccessView get()
        {
            return texture3D;
        }

        void set(Texture3DUnorderedAccessView value)
        {
            texture3D = value;
        }
    }
private:

    [FieldOffset(0)]
    Graphics::Format format;
    [FieldOffset(4)]
    UnorderedAccessViewDimension viewDimension;
    [FieldOffset(8)]
    BufferUnorderedAccessView buffer;
    [FieldOffset(8)]
    Texture1DUnorderedAccessView texture1D;
    [FieldOffset(8)]
    Texture1DArrayUnorderedAccessView texture1DArray;
    [FieldOffset(8)]
    Texture2DUnorderedAccessView texture2D;
    [FieldOffset(8)]
    Texture2DArrayUnorderedAccessView texture2DArray;
    [FieldOffset(8)]
    Texture3DUnorderedAccessView texture3D;

internal:
    UnorderedAccessViewDescription(const D3D11_UNORDERED_ACCESS_VIEW_DESC& desc)
    {
        Format = static_cast<Graphics::Format>(desc.Format);
        ViewDimension = static_cast<UnorderedAccessViewDimension>(desc.ViewDimension);

        switch (ViewDimension)
        {
        case UnorderedAccessViewDimension::Buffer :
               {
                   BufferUnorderedAccessView buffer;

                   buffer.FirstElement = desc.Buffer.FirstElement;
                   buffer.ElementCount = desc.Buffer.NumElements;
                   buffer.BufferOptions = static_cast<UnorderedAccessViewBufferOptions>(desc.Buffer.Flags);

                   Buffer = buffer;
                   break;
               }
        case UnorderedAccessViewDimension::Texture1D :
              {
                  Texture1DUnorderedAccessView texture1D;

                  texture1D.MipSlice = desc.Texture1D.MipSlice;

                  Texture1D = texture1D;
                  break;
              }
        case ShaderResourceViewDimension::Texture1DArray :
              {
                  Texture1DArrayUnorderedAccessView texture1DArray;

                  texture1DArray.MipSlice = desc.Texture1DArray.MipSlice;
                  texture1DArray.FirstArraySlice = desc.Texture1DArray.FirstArraySlice;
                  texture1DArray.ArraySize = desc.Texture1DArray.ArraySize;

                  Texture1DArray = texture1DArray;
                  break;
              }
        case ShaderResourceViewDimension::Texture2D :
              {
                  Texture2DUnorderedAccessView texture2D;

                  texture2D.MipSlice = desc.Texture2D.MipSlice;

                  Texture2D = texture2D;
                  break;
              }
        case ShaderResourceViewDimension::Texture2DArray :
              {
                  Texture2DArrayUnorderedAccessView texture2DArray;

                  texture2DArray.MipSlice = desc.Texture2DArray.MipSlice;
                  texture2DArray.FirstArraySlice = desc.Texture2DArray.FirstArraySlice;
                  texture2DArray.ArraySize = desc.Texture2DArray.ArraySize;

                  Texture2DArray = texture2DArray;
                  break;
              }
        case UnorderedAccessViewDimension::Texture3D :
              {
                  Texture3DUnorderedAccessView texture3D;

                  texture3D.MipSlice = desc.Texture3D.MipSlice;
                  texture3D.FirstWSlice = desc.Texture3D.FirstWSlice;
                  texture3D.WSize = desc.Texture3D.WSize;

                  Texture3D = texture3D;
                  break;
              }
        default :
              {
                  throw gcnew NotSupportedException("Unknown or not supported ViewDimension.");
              }
        }
    }

    void CopyTo(D3D11_UNORDERED_ACCESS_VIEW_DESC* desc)
    {
        desc->Format = static_cast<DXGI_FORMAT>(Format);
        desc->ViewDimension = static_cast<D3D11_UAV_DIMENSION>(ViewDimension);

        switch (ViewDimension)
        {
        case UnorderedAccessViewDimension::Buffer :
               {
                   desc->Buffer.FirstElement = Buffer.FirstElement;
                   desc->Buffer.NumElements = Buffer.ElementCount;
                   desc->Buffer.Flags = static_cast<UINT>(Buffer.BufferOptions);
                   break;
               }
        case UnorderedAccessViewDimension::Texture1D :
              {
                  desc->Texture1D.MipSlice = Texture1D.MipSlice;
                  break;
              }
        case ShaderResourceViewDimension::Texture1DArray :
              {
                  desc->Texture1DArray.MipSlice = Texture1DArray.MipSlice;
                  desc->Texture1DArray.FirstArraySlice = Texture1DArray.FirstArraySlice;
                  desc->Texture1DArray.ArraySize = Texture1DArray.ArraySize;
                  break;
              }
        case ShaderResourceViewDimension::Texture2D :
              {
                  desc->Texture2D.MipSlice = Texture2D.MipSlice;
                  break;
              }
        case ShaderResourceViewDimension::Texture2DArray :
              {
                  desc->Texture2DArray.MipSlice = Texture2DArray.MipSlice;
                  desc->Texture2DArray.FirstArraySlice = Texture2DArray.FirstArraySlice;
                  desc->Texture2DArray.ArraySize = Texture2DArray.ArraySize;
                  break;
              }
        case UnorderedAccessViewDimension::Texture3D :
              {
                  desc->Texture3D.MipSlice = Texture3D.MipSlice;
                  desc->Texture3D.FirstWSlice = Texture3D.FirstWSlice;
                  desc->Texture3D.WSize = Texture3D.WSize;
                  break;
              }
        default:
              {
                  throw gcnew NotSupportedException("Unknown or not supported ViewDimension.");
              }
        }
    }

};

/// <summary>
/// Defines the dimensions of a viewport.
/// <para>(Also see DirectX SDK: D3D11_VIEWPORT)</para>
/// </summary>
public value struct Viewport
{
public:
    /// <summary>
    /// X position of the left hand side of the viewport. Ranges between D3D11_VIEWPORT_BOUNDS_MIN and D3D11_VIEWPORT_BOUNDS_MAX.
    /// <para>(Also see DirectX SDK: D3D11_VIEWPORT.TopLeftX)</para>
    /// </summary>
    property Single TopLeftX
    {
        Single get()
        {
            return topLeftX;
        }

        void set(Single value)
        {
            topLeftX = value;
        }
    }
    /// <summary>
    /// Y position of the top of the viewport. Ranges between D3D11_VIEWPORT_BOUNDS_MIN and D3D11_VIEWPORT_BOUNDS_MAX.
    /// <para>(Also see DirectX SDK: D3D11_VIEWPORT.TopLeftY)</para>
    /// </summary>
    property Single TopLeftY
    {
        Single get()
        {
            return topLeftY;
        }

        void set(Single value)
        {
            topLeftY = value;
        }
    }
    /// <summary>
    /// Width of the viewport.
    /// <para>(Also see DirectX SDK: D3D11_VIEWPORT.Width)</para>
    /// </summary>
    property Single Width
    {
        Single get()
        {
            return width;
        }

        void set(Single value)
        {
            width = value;
        }
    }
    /// <summary>
    /// Height of the viewport.
    /// <para>(Also see DirectX SDK: D3D11_VIEWPORT.Height)</para>
    /// </summary>
    property Single Height
    {
        Single get()
        {
            return height;
        }

        void set(Single value)
        {
            height = value;
        }
    }
    /// <summary>
    /// Minimum depth of the viewport. Ranges between 0 and 1.
    /// <para>(Also see DirectX SDK: D3D11_VIEWPORT.MinDepth)</para>
    /// </summary>
    property Single MinDepth
    {
        Single get()
        {
            return minDepth;
        }

        void set(Single value)
        {
            minDepth = value;
        }
    }
    /// <summary>
    /// Maximum depth of the viewport. Ranges between 0 and 1.
    /// <para>(Also see DirectX SDK: D3D11_VIEWPORT.MaxDepth)</para>
    /// </summary>
    property Single MaxDepth
    {
        Single get()
        {
            return maxDepth;
        }

        void set(Single value)
        {
            maxDepth = value;
        }
    }

private:

    Single topLeftX;
    Single topLeftY;
    Single width;
    Single height;
    Single minDepth;
    Single maxDepth;

internal:
    Viewport(const D3D11_VIEWPORT& viewport)
    {
        TopLeftX = viewport.TopLeftX;
        TopLeftY = viewport.TopLeftY;
        Width = viewport.Width;
        Height = viewport.Height;
        MinDepth = viewport.MinDepth;
        MaxDepth = viewport.MaxDepth;    
    }

    operator const D3D11_VIEWPORT ()
    {
        D3D11_VIEWPORT nativeViewport;

        nativeViewport.TopLeftX = TopLeftX;
        nativeViewport.TopLeftY = TopLeftY;
        nativeViewport.Width = Width;
        nativeViewport.Height = Height;
        nativeViewport.MinDepth = MinDepth;
        nativeViewport.MaxDepth = MaxDepth;

        return nativeViewport;
    }
public:

    static Boolean operator == (Viewport viewport1, Viewport viewport2)
    {
        return (viewport1.topLeftX == viewport2.topLeftX) &&
            (viewport1.topLeftY == viewport2.topLeftY) &&
            (viewport1.width == viewport2.width) &&
            (viewport1.height == viewport2.height) &&
            (viewport1.minDepth == viewport2.minDepth) &&
            (viewport1.maxDepth == viewport2.maxDepth);
    }

    static Boolean operator != (Viewport viewport1, Viewport viewport2)
    {
        return !(viewport1 == viewport2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != Viewport::typeid)
        {
            return false;
        }

        return *this == safe_cast<Viewport>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + topLeftX.GetHashCode();
        hashCode = hashCode * 31 + topLeftY.GetHashCode();
        hashCode = hashCode * 31 + width.GetHashCode();
        hashCode = hashCode * 31 + height.GetHashCode();
        hashCode = hashCode * 31 + minDepth.GetHashCode();
        hashCode = hashCode * 31 + maxDepth.GetHashCode();

        return hashCode;
    }

};

} } } }

