//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace System::Linq;
using namespace System::Runtime::InteropServices;
using namespace Microsoft::WindowsAPICodePack::DirectX;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

/// <summary>
/// Describes the blend state.
/// <para>(Also see DirectX SDK: D3D10_BLEND_DESC)</para>
/// </summary>
public value struct BlendDescription
{
public:
    /// <summary>
    /// Determines whether or not to use alpha-to-coverage as a multisampling technique when setting a pixel to a rendertarget.
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC.AlphaToCoverageEnable)</para>
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
    /// Enable (or disable) blending. There are eight elements in this array; these correspond to the eight rendertargets that can be set to output-merger stage at one time.
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC.BlendEnable)</para>
    /// </summary>
    property ReadOnlyCollection<Boolean>^ BlendEnable
    {
        ReadOnlyCollection<Boolean>^ get()
        {
            if (blendEnable == nullptr)
            {
                blendEnable = gcnew array<Boolean>(BlendEnableArrayLength);
            }
            return Array::AsReadOnly(blendEnable);
        }
    }
    /// <summary>
    /// This blend option specifies the first RGB data source and includes an optional pre-blend operation.
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC.SrcBlend)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC.DestBlend)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC.BlendOp)</para>
    /// </summary>
    property BlendOperation BlendOperation
    {
        Direct3D10::BlendOperation get()
        {
            return blendOperation;
        }

        void set(Direct3D10::BlendOperation value)
        {
            blendOperation = value;
        }
    }
    /// <summary>
    /// This blend option specifies the first alpha data source and includes an optional pre-blend operation. Blend options that end in _COLOR are not allowed.
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC.SrcBlendAlpha)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC.DestBlendAlpha)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC.BlendOpAlpha)</para>
    /// </summary>
    property Direct3D10::BlendOperation BlendOperationAlpha
    {
        Direct3D10::BlendOperation get()
        {
            return blendOperationAlpha;
        }

        void set(Direct3D10::BlendOperation value)
        {
            blendOperationAlpha = value;
        }
    }
    /// <summary>
    /// A per-pixel write mask that allows control over which components can be written (see <see cref="ColorWriteEnableComponents"/>)<seealso cref="ColorWriteEnableComponents"/>.
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC.RenderTargetWriteMask)</para>
    /// </summary>
    property ReadOnlyCollection<ColorWriteEnableComponents>^ RenderTargetWriteMask
    {
        ReadOnlyCollection<ColorWriteEnableComponents>^ get()
        {
            if (renderTargetWriteMask == nullptr)
            {
                renderTargetWriteMask =
                    gcnew array<ColorWriteEnableComponents>(RenderTargetWriteMaskArrayLength);

                for (int i =0; i < renderTargetWriteMask->Length; i++)
                {
                    renderTargetWriteMask[i] = ColorWriteEnableComponents::All;
                }
            }
            return Array::AsReadOnly(renderTargetWriteMask);
        }
    }
private:

    Boolean alphaToCoverageEnable;
    Blend sourceBlend;
    Blend destinationBlend;
    Direct3D10::BlendOperation blendOperation;
    Blend sourceBlendAlpha;
    Blend destinationBlendAlpha;
    Direct3D10::BlendOperation blendOperationAlpha;
    array<ColorWriteEnableComponents>^ renderTargetWriteMask;
    array<Boolean>^ blendEnable;
    literal int RenderTargetWriteMaskArrayLength = 8;
    literal int BlendEnableArrayLength = 8;

internal:
    BlendDescription(const D3D10_BLEND_DESC & blendDescription)
    {
        AlphaToCoverageEnable = blendDescription.AlphaToCoverageEnable != 0;
        BlendOperation = static_cast<Direct3D10::BlendOperation>(blendDescription.BlendOp);
        BlendOperationAlpha = static_cast<Direct3D10::BlendOperation>(blendDescription.BlendOpAlpha);

        DestinationBlend = static_cast<Direct3D10::Blend>(blendDescription.DestBlend);
        DestinationBlendAlpha = static_cast<Direct3D10::Blend>(blendDescription.DestBlendAlpha);

        SourceBlend = static_cast<Direct3D10::Blend>(blendDescription.SrcBlend);
        SourceBlendAlpha = static_cast<Direct3D10::Blend>(blendDescription.SrcBlendAlpha);
        
        renderTargetWriteMask = gcnew array<ColorWriteEnableComponents>(RenderTargetWriteMaskArrayLength);
        for (int index = 0; index < RenderTargetWriteMaskArrayLength; index++)
        {
            renderTargetWriteMask[index] =
                static_cast<ColorWriteEnableComponents>(blendDescription.RenderTargetWriteMask[index]);
        }
        
        blendEnable = gcnew array<Boolean>(BlendEnableArrayLength);
        for (int index = 0; index < BlendEnableArrayLength; index++)
        {
            blendEnable[index] = blendDescription.BlendEnable[index] != FALSE;
        }
    }

    void CopyTo(D3D10_BLEND_DESC * blendDescription)
    {
        blendDescription->AlphaToCoverageEnable = AlphaToCoverageEnable ? 1: 0;
        blendDescription->BlendOp = static_cast<D3D10_BLEND_OP>(BlendOperation);
        blendDescription->BlendOpAlpha = static_cast<D3D10_BLEND_OP>(BlendOperationAlpha);

        blendDescription->DestBlend = static_cast<D3D10_BLEND>(DestinationBlend);
        blendDescription->DestBlendAlpha = static_cast<D3D10_BLEND>(DestinationBlendAlpha);

        blendDescription->SrcBlend = static_cast<D3D10_BLEND>(SourceBlend);
        blendDescription->SrcBlendAlpha = static_cast<D3D10_BLEND>(SourceBlendAlpha);
        
        if (renderTargetWriteMask != nullptr)
        {
            for (int index = 0; index < RenderTargetWriteMaskArrayLength; index++)
            {
                blendDescription->RenderTargetWriteMask[index] =
                    static_cast<BYTE>(renderTargetWriteMask[index]);
            }
        } 
        else
        {
            ZeroMemory(blendDescription->RenderTargetWriteMask, sizeof(BYTE) * RenderTargetWriteMaskArrayLength);
        }
        
        if (blendEnable != nullptr)
        {
            for (int index = 0; index < BlendEnableArrayLength; index++)
            {
                blendDescription->BlendEnable[index] = blendEnable[index] ? TRUE : FALSE;
            }
        }
        else
        {
            ZeroMemory(blendDescription->BlendEnable, sizeof(BOOL) * BlendEnableArrayLength);
        }
    }
};

/// <summary>
/// Describes the blend state for a render target for a Direct3D 10.1 device
/// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_BLEND_DESC1)</para>
/// </summary>
public value struct RenderTargetBlendDescription1
{
public:
    /// <summary>
    /// Enable (or disable) blending.
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_BLEND_DESC1.BlendEnable)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_BLEND_DESC1.SrcBlend)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_BLEND_DESC1.DestBlend)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_BLEND_DESC1.BlendOp)</para>
    /// </summary>
    property BlendOperation BlendOperation
    {
        Direct3D10::BlendOperation get()
        {
            return blendOperation;
        }

        void set(Direct3D10::BlendOperation value)
        {
            blendOperation = value;
        }
    }
    /// <summary>
    /// This blend option specifies the first alpha data source and includes an optional pre-blend operation. Blend options that end in _COLOR are not allowed.
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_BLEND_DESC1.SrcBlendAlpha)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_BLEND_DESC1.DestBlendAlpha)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_BLEND_DESC1.BlendOpAlpha)</para>
    /// </summary>
    property Direct3D10::BlendOperation BlendOperationAlpha
    {
        Direct3D10::BlendOperation get()
        {
            return blendOperationAlpha;
        }

        void set(Direct3D10::BlendOperation value)
        {
            blendOperationAlpha = value;
        }
    }
    /// <summary>
    /// A write mask.
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_BLEND_DESC1.RenderTargetWriteMask)</para>
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
    Direct3D10::BlendOperation blendOperation;
    Blend sourceBlendAlpha;
    Blend destinationBlendAlpha;
    Direct3D10::BlendOperation blendOperationAlpha;
    ColorWriteEnableComponents renderTargetWriteMask;

internal:
    RenderTargetBlendDescription1(const D3D10_RENDER_TARGET_BLEND_DESC1 &renderTargetBlendDescription1)
    {
        BlendEnable = renderTargetBlendDescription1.BlendEnable != 0;

        BlendOperation = static_cast<Direct3D10::BlendOperation>(renderTargetBlendDescription1.BlendOp);
        BlendOperationAlpha = static_cast<Direct3D10::BlendOperation>(renderTargetBlendDescription1.BlendOpAlpha);

        DestinationBlend = static_cast<Direct3D10::Blend>(renderTargetBlendDescription1.DestBlend);
        DestinationBlendAlpha = static_cast<Direct3D10::Blend>(renderTargetBlendDescription1.DestBlendAlpha);

        SourceBlend = static_cast<Direct3D10::Blend>(renderTargetBlendDescription1.SrcBlend);
        SourceBlendAlpha = static_cast<Direct3D10::Blend>(renderTargetBlendDescription1.SrcBlendAlpha);
        
        RenderTargetWriteMask = static_cast<ColorWriteEnableComponents>(renderTargetBlendDescription1.RenderTargetWriteMask);
    }

    void CopyTo(D3D10_RENDER_TARGET_BLEND_DESC1 * renderTargetBlendDescription1)
    {
        renderTargetBlendDescription1->BlendEnable = BlendEnable ? 1 : 0;
        renderTargetBlendDescription1->BlendOp = static_cast<D3D10_BLEND_OP>(BlendOperation);
        renderTargetBlendDescription1->BlendOpAlpha = static_cast<D3D10_BLEND_OP>(BlendOperationAlpha);

        renderTargetBlendDescription1->DestBlend = static_cast<D3D10_BLEND>(DestinationBlend);
        renderTargetBlendDescription1->DestBlendAlpha = static_cast<D3D10_BLEND>(DestinationBlendAlpha);

        renderTargetBlendDescription1->SrcBlend = static_cast<D3D10_BLEND>(SourceBlend);
        renderTargetBlendDescription1->SrcBlendAlpha = static_cast<D3D10_BLEND>(SourceBlendAlpha);

        renderTargetBlendDescription1->RenderTargetWriteMask = static_cast<UINT8>(RenderTargetWriteMask);
    }
public:

    static Boolean operator == (RenderTargetBlendDescription1 renderTargetBlendDescription1, RenderTargetBlendDescription1 renderTargetBlendDescription2)
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

    static Boolean operator != (RenderTargetBlendDescription1 renderTargetBlendDescription1, RenderTargetBlendDescription1 renderTargetBlendDescription2)
    {
        return !(renderTargetBlendDescription1 == renderTargetBlendDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != RenderTargetBlendDescription1::typeid)
        {
            return false;
        }

        return *this == safe_cast<RenderTargetBlendDescription1>(obj);
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
/// Describes the blend state for a Direct3D 10.1 device.
/// <para>(Also see DirectX SDK: D3D10_BLEND_DESC1)</para>
/// </summary>
public value struct BlendDescription1
{
public:
    /// <summary>
    /// Determines whether or not to use the alpha-to-coverage multisampling technique when setting a render-target pixel.
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC1.AlphaToCoverageEnable)</para>
    /// </summary>
    property Boolean AlphaToCoverageEnable;
    /// <summary>
    /// Set to TRUE to enable independent blending in simultaneous render targets.
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC1.IndependentBlendEnable)</para>
    /// </summary>
    property Boolean IndependentBlendEnable;
    /// <summary>
    /// An array of render-target-blend descriptions (see <see cref="RenderTargetBlendDescription1"/>)<seealso cref="RenderTargetBlendDescription1"/>; these correspond to the eight rendertargets that can be set to the output-merger stage at one time.
    /// <para>(Also see DirectX SDK: D3D10_BLEND_DESC1.RenderTarget)</para>
    /// </summary>
    property ReadOnlyCollection<RenderTargetBlendDescription1>^ RenderTarget
    {
        ReadOnlyCollection<RenderTargetBlendDescription1>^ get()
        {
            if (renderTarget == nullptr)
            {
                renderTarget  = gcnew array<RenderTargetBlendDescription1>(RenderTargetArrayLength);
            }

            return Array::AsReadOnly(renderTarget);
        }
    }

internal:
    BlendDescription1(const D3D10_BLEND_DESC1& blendDescription1)
    {
        AlphaToCoverageEnable = blendDescription1.AlphaToCoverageEnable != 0;
        IndependentBlendEnable = blendDescription1.IndependentBlendEnable != 0;

        renderTarget = gcnew array<RenderTargetBlendDescription1>(RenderTargetArrayLength);
        pin_ptr<RenderTargetBlendDescription1> renderTargetPtr = &renderTarget[0];

        memcpy(renderTargetPtr, blendDescription1.RenderTarget, sizeof(D3D10_RENDER_TARGET_BLEND_DESC1) * RenderTargetArrayLength);
    }

    void CopyTo(D3D10_BLEND_DESC1* blendDescription1)
    {
        blendDescription1->AlphaToCoverageEnable = AlphaToCoverageEnable ? 1 :0;
        blendDescription1->IndependentBlendEnable = IndependentBlendEnable ? 1 :0;

        if (renderTarget != nullptr)
        {
            pin_ptr<RenderTargetBlendDescription1> renderTargetPtr = &renderTarget[0];
            memcpy(blendDescription1->RenderTarget, renderTargetPtr, sizeof(D3D10_RENDER_TARGET_BLEND_DESC1) * RenderTargetArrayLength);
        }
        else
        {
            ZeroMemory(blendDescription1->RenderTarget, sizeof(D3D10_RENDER_TARGET_BLEND_DESC1) * RenderTargetArrayLength);
        }
    }

private:
    literal int RenderTargetArrayLength = 8;
    array<RenderTargetBlendDescription1>^ renderTarget;
};

/// <summary>
/// Defines a 3D box.
/// <para>(Also see DirectX SDK: D3D10_BOX)</para>
/// </summary>
public value struct Box
{
public:
    /// <summary>
    /// The x position of the left hand side of the box.
    /// <para>(Also see DirectX SDK: D3D10_BOX.left)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BOX.top)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BOX.front)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BOX.right)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BOX.bottom)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BOX.back)</para>
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
    Box(const D3D10_BOX& nBox)
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
/// Describes a buffer resource.
/// <para>(Also see DirectX SDK: D3D10_BUFFER_DESC)</para>
/// </summary>
public value struct BufferDescription
{
public:
    /// <summary>
    /// Size of the buffer in bytes.
    /// <para>(Also see DirectX SDK: D3D10_BUFFER_DESC.ByteWidth)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BUFFER_DESC.Usage)</para>
    /// </summary>
    property Usage Usage
    {
        Direct3D10::Usage get()
        {
            return usage;
        }

        void set(Direct3D10::Usage value)
        {
            usage = value;
        }
    }
    /// <summary>
    /// Identify how the buffer will be bound to the pipeline. Applications can logicaly OR flags together (see <see cref="Direct3D10::BindingOptions"/>)<seealso cref="Direct3D10::BindingOptions"/> to indicate that the buffer can be accessed in different ways.
    /// <para>(Also see DirectX SDK: D3D10_BUFFER_DESC.BindFlags)</para>
    /// </summary>
    property BindingOptions BindingOptions
    {
        Direct3D10::BindingOptions get()
        {
            return bindFlags;
        }

        void set(Direct3D10::BindingOptions value)
        {
            bindFlags = value;
        }
    }
    /// <summary>
    /// CPU access flags (see <see cref="CpuAccessOptions"/>)<seealso cref="CpuAccessOptions"/> or 0 if no CPU access is necessary. Applications can logicaly OR flags together.
    /// <para>(Also see DirectX SDK: D3D10_BUFFER_DESC.CPUAccessFlags)</para>
    /// </summary>
    property CpuAccessOptions CpuAccessOptions
    {
        Direct3D10::CpuAccessOptions get()
        {
            return cpuAccessFlags;
        }

        void set(Direct3D10::CpuAccessOptions value)
        {
            cpuAccessFlags = value;
        }
    }
    /// <summary>
    /// Miscellaneous flags (see <see cref="MiscellaneousResourceOptions"/>)<seealso cref="MiscellaneousResourceOptions"/> or 0 if unused. Applications can logically OR flags together.
    /// <para>(Also see DirectX SDK: D3D10_BUFFER_DESC.MiscFlags)</para>
    /// </summary>
    property MiscellaneousResourceOptions MiscellaneousResourceOptions
    {
        Direct3D10::MiscellaneousResourceOptions get()
        {
            return miscFlags;
        }

        void set(Direct3D10::MiscellaneousResourceOptions value)
        {
            miscFlags = value;
        }
    }

private:

    Direct3D10::BindingOptions bindFlags;
    Direct3D10::CpuAccessOptions cpuAccessFlags;
    Direct3D10::MiscellaneousResourceOptions miscFlags;
    UInt32 byteWidth;
    Direct3D10::Usage usage;

internal:

    BufferDescription(const D3D10_BUFFER_DESC &desc)
    {
        ByteWidth = desc.ByteWidth;
        Usage = static_cast<Direct3D10::Usage>(desc.Usage);
        BindingOptions = static_cast<Direct3D10::BindingOptions>(desc.BindFlags);
        CpuAccessOptions  = static_cast<Direct3D10::CpuAccessOptions>(desc.CPUAccessFlags);
        MiscellaneousResourceOptions = static_cast<Direct3D10::MiscellaneousResourceOptions>(desc.MiscFlags);
    }

    void CopyTo(D3D10_BUFFER_DESC &pDesc)
    {
        pDesc.ByteWidth = ByteWidth;
        pDesc.Usage = static_cast<D3D10_USAGE>(Usage);
        pDesc.BindFlags = static_cast<UINT>(BindingOptions);
        pDesc.CPUAccessFlags  = static_cast<UINT>(CpuAccessOptions);
        pDesc.MiscFlags = static_cast<UINT>(MiscellaneousResourceOptions);
    }
public:

    static Boolean operator == (BufferDescription bufferDescription1, BufferDescription bufferDescription2)
    {
        return (bufferDescription1.bindFlags == bufferDescription2.bindFlags) &&
            (bufferDescription1.cpuAccessFlags == bufferDescription2.cpuAccessFlags) &&
            (bufferDescription1.miscFlags == bufferDescription2.miscFlags) &&
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
        hashCode = hashCode * 31 + byteWidth.GetHashCode();
        hashCode = hashCode * 31 + usage.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Specifies the elements from a buffer resource to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D10_BUFFER_RTV)</para>
/// </summary>
public value struct BufferRenderTargetView
{
public:
    /// <summary>
    /// The offset (that is, the number of elements) between the beginning of the buffer and the first element that is to be used in the view, starting at 0.
    /// <para>(Also see DirectX SDK: D3D10_BUFFER_RTV.ElementOffset)</para>
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
    /// The number of elements in the view.
    /// <para>(Also see DirectX SDK: D3D10_BUFFER_RTV.ElementWidth)</para>
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
/// <para>(Also see DirectX SDK: D3D10_BUFFER_SRV)</para>
/// </summary>
public value struct BufferShaderResourceView
{
public:
    /// <summary>
    /// The offset of the first element in the view to access, relative to element 0.
    /// <para>(Also see DirectX SDK: D3D10_BUFFER_SRV.ElementOffset)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_BUFFER_SRV.ElementWidth)</para>
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
/// Describes the stencil operations that can be performed based on the results of stencil test.
/// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCILOP_DESC)</para>
/// </summary>
public value struct DepthStencilOperationDescription
{
public:
    DepthStencilOperationDescription(const D3D10_DEPTH_STENCILOP_DESC& desc)
    {
        StencilFailOperation = static_cast<StencilOperation>(desc.StencilFailOp);
        StencilDepthFailOperation = static_cast<StencilOperation>(desc.StencilDepthFailOp);
        StencilPassOperation = static_cast<StencilOperation>(desc.StencilPassOp);
        StencilFunction = static_cast<ComparisonFunction>(desc.StencilFunc);
    }

    /// <summary>
    /// A member of the StencilOperation enumerated type that describes the stencil operation to perform when stencil testing fails. The default value is StencilOperation_KEEP.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCILOP_DESC.StencilFailOp)</para>
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
    /// A member of the StencilOperation enumerated type that describes the stencil operation to perform when stencil testing passes and depth testing fails. The default value is StencilOperation_KEEP.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCILOP_DESC.StencilDepthFailOp)</para>
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
    /// A member of the StencilOperation enumerated type that describes the stencil operation to perform when stencil testing and depth testing both pass. The default value is StencilOperation_KEEP.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCILOP_DESC.StencilPassOp)</para>
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
    /// A member of the ComparisonFunction enumerated type that describes how stencil data is compared against existing stencil data. The default value is Always.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCILOP_DESC.StencilFunc)</para>
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
/// Describes a counter.
/// <para>(Also see DirectX SDK: D3D10_COUNTER_DESC)</para>
/// </summary>
public value struct CounterDescription
{
public:
    /// <summary>
    /// Type of counter (see <see cref="Counter"/>)<seealso cref="Counter"/>.
    /// <para>(Also see DirectX SDK: D3D10_COUNTER_DESC.Counter)</para>
    /// </summary>
    property Counter Counter
    {
        Direct3D10::Counter get()
        {
            return counter;
        }

        void set(Direct3D10::Counter value)
        {
            counter = value;
        }
    }

    // REVIEW: should we really expose "reserved" parts of the API?
    // Seems like that's an internal implementation detail that could
    // be left out of the public API.

    /// <summary>
    /// Reserved.
    /// <para>(Also see DirectX SDK: D3D10_COUNTER_DESC.MiscFlags)</para>
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

    Direct3D10::Counter counter;
    UInt32 miscFlags;

internal:
    CounterDescription(const D3D10_COUNTER_DESC& desc)
    {
        Counter = static_cast<Direct3D10::Counter>(desc.Counter);
        ReservedOptions = desc.MiscFlags;
    }
    
    void CopyTo(D3D10_COUNTER_DESC* desc)
    {
        desc->Counter = static_cast<D3D10_COUNTER>(Counter);
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
/// <para>(Also see DirectX SDK: D3D10_COUNTER_INFO)</para>
/// </summary>
public value struct CounterInformation
{
public:
    /// <summary>
    /// Largest device-dependent counter ID that the device supports. If none are supported, this value will be 0. Otherwise it will be greater than or equal to DeviceDependent0. See Counter.
    /// <para>(Also see DirectX SDK: D3D10_COUNTER_INFO.LastDeviceDependentCounter)</para>
    /// </summary>
    property Counter LastDeviceDependentCounter
    {
        Counter get()
        {
            return lastDeviceDependentCounter;
        }

        void set(Counter value)
        {
            lastDeviceDependentCounter = value;
        }
    }
    /// <summary>
    /// Number of counters that can be simultaneously supported.
    /// <para>(Also see DirectX SDK: D3D10_COUNTER_INFO.NumSimultaneousCounters)</para>
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
    /// Number of detectable parallel units that the counter is able to discern. Values are 1 ~ 4. Use DetectableParallelUnitCount to interpret the values of the VERTEX_PROCESSING, GEOMETRY_PROCESSING, PIXEL_PROCESSING, and OTHER_GPU_PROCESSING counters. See Asynchronous.GetData for an equation.
    /// <para>(Also see DirectX SDK: D3D10_COUNTER_INFO.NumDetectableParallelUnits)</para>
    /// </summary>
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

    Counter lastDeviceDependentCounter;
    UInt32 numSimultaneousCounters;
    Byte numDetectableParallelUnits;

internal:
    CounterInformation(const D3D10_COUNTER_INFO & info)
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
/// Describes depth-stencil state.
/// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_DESC)</para>
/// </summary>
public value struct DepthStencilDescription
{
public:
    /// <summary>
    /// A Boolean value that enables depth testing.  The default value is TRUE.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_DESC.DepthEnable)</para>
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
    /// A member of the DepthWriteMask enumerated type that identifies a portion of the depth-stencil buffer that can be modified by depth data.  The default value is DepthWriteMask_ALL.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_DESC.DepthWriteMask)</para>
    /// </summary>
    property DepthWriteMask DepthWriteMask
    {
        Direct3D10::DepthWriteMask get()
        {
            return depthWriteMask;
        }

        void set(Direct3D10::DepthWriteMask value)
        {
            depthWriteMask = value;
        }
    }

    // REVIEW: May want to change name to "DepthComparisonFunction"

    /// <summary>
    /// A member of the ComparisonFunction enumerated type that defines how depth data is compared against existing depth data.  The default value is Less
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_DESC.DepthFunc)</para>
    /// </summary>
    property ComparisonFunction DepthFunction
    {
        Direct3D10::ComparisonFunction get()
        {
            return depthFunction;
        }

        void set(Direct3D10::ComparisonFunction value)
        {
            depthFunction = value;
        }
    }
    /// <summary>
    /// A Boolean value that enables stencil testing.  The default value is FALSE.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_DESC.StencilEnable)</para>
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
    /// A value that identifies a portion of the depth-stencil buffer for reading stencil data.  The default value is Default.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_DESC.StencilReadMask)</para>
    /// </summary>
    property StencilReadMask StencilReadMask
    {
        Direct3D10::StencilReadMask get()
        {
            return stencilReadMask;
        }

        void set(Direct3D10::StencilReadMask value)
        {
            stencilReadMask = value;
        }
    }
    /// <summary>
    /// A value that identifies a portion of the depth-stencil buffer for writing stencil data. The default value is Default.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_DESC.StencilWriteMask)</para>
    /// </summary>
    property StencilWriteMask StencilWriteMask
    {
        Direct3D10::StencilWriteMask get()
        {
            return stencilWriteMask;
        }

        void set(Direct3D10::StencilWriteMask value)
        {
            stencilWriteMask = value;
        }
    }
    /// <summary>
    /// A DepthStencilOperationDescription structure that identifies how to use the results of the depth test and the stencil test for pixels whose surface normal is facing toward the camera.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_DESC.FrontFace)</para>
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
    /// A DepthStencilOperationDescription structure that identifies how to use the results of the depth test and the stencil test for pixels whose surface normal is facing away from the camera.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_DESC.BackFace)</para>
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

    Boolean depthEnable;
    Direct3D10::DepthWriteMask depthWriteMask;
    Direct3D10::ComparisonFunction depthFunction;
    Boolean stencilEnable;
    Direct3D10::StencilReadMask stencilReadMask;
    Direct3D10::StencilWriteMask stencilWriteMask;
    DepthStencilOperationDescription frontFace;
    DepthStencilOperationDescription backFace;

internal:
    DepthStencilDescription (const D3D10_DEPTH_STENCIL_DESC & desc)
    {
        DepthEnable = desc.DepthEnable != 0;
        DepthWriteMask = static_cast<Direct3D10::DepthWriteMask>(desc.DepthWriteMask);
        DepthFunction = static_cast<Direct3D10::ComparisonFunction>(desc.DepthFunc);
        StencilEnable = desc.StencilEnable != 0;
        StencilReadMask = static_cast<Direct3D10::StencilReadMask>(desc.StencilReadMask);
        StencilWriteMask = static_cast<Direct3D10::StencilWriteMask>(desc.StencilWriteMask);
        FrontFace = Direct3D10::DepthStencilOperationDescription(desc.FrontFace);
        BackFace = Direct3D10::DepthStencilOperationDescription(desc.BackFace);
    }

    void CopyTo (D3D10_DEPTH_STENCIL_DESC * desc)
    {
        desc->DepthEnable = DepthEnable ? 1 : 0;
        desc->DepthWriteMask = static_cast<D3D10_DEPTH_WRITE_MASK>(DepthWriteMask);
        desc->DepthFunc = static_cast<D3D10_COMPARISON_FUNC>(DepthFunction);
        desc->StencilEnable = StencilEnable ? 1 : 0;
        desc->StencilReadMask = static_cast<UINT8>(StencilReadMask);
        desc->StencilWriteMask = static_cast<UINT8>(StencilWriteMask);
        
        // REVIEW: would be convenient to have CopyTo() method or similar
        desc->FrontFace.StencilFailOp = static_cast<D3D10_STENCIL_OP>(FrontFace.StencilFailOperation);
        desc->FrontFace.StencilDepthFailOp = static_cast<D3D10_STENCIL_OP>(FrontFace.StencilDepthFailOperation);
        desc->FrontFace.StencilPassOp = static_cast<D3D10_STENCIL_OP>(FrontFace.StencilPassOperation );
        desc->FrontFace.StencilFunc = static_cast<D3D10_COMPARISON_FUNC>(FrontFace.StencilFunction);

        desc->BackFace.StencilFailOp = static_cast<D3D10_STENCIL_OP>(BackFace.StencilFailOperation);
        desc->BackFace.StencilDepthFailOp = static_cast<D3D10_STENCIL_OP>(BackFace.StencilDepthFailOperation);
        desc->BackFace.StencilPassOp = static_cast<D3D10_STENCIL_OP>(BackFace.StencilPassOperation );
        desc->BackFace.StencilFunc = static_cast<D3D10_COMPARISON_FUNC>(BackFace.StencilFunction);
    }
public:

    static Boolean operator == (DepthStencilDescription depthStencilDescription1, DepthStencilDescription depthStencilDescription2)
    {
        return (depthStencilDescription1.depthEnable == depthStencilDescription2.depthEnable) &&
            (depthStencilDescription1.depthWriteMask == depthStencilDescription2.depthWriteMask) &&
            (depthStencilDescription1.depthFunction == depthStencilDescription2.depthFunction) &&
            (depthStencilDescription1.stencilEnable == depthStencilDescription2.stencilEnable) &&
            (depthStencilDescription1.stencilReadMask == depthStencilDescription2.stencilReadMask) &&
            (depthStencilDescription1.stencilWriteMask == depthStencilDescription2.stencilWriteMask) &&
            (depthStencilDescription1.frontFace == depthStencilDescription2.frontFace) &&
            (depthStencilDescription1.backFace == depthStencilDescription2.backFace);
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

        hashCode = hashCode * 31 + depthEnable.GetHashCode();
        hashCode = hashCode * 31 + depthWriteMask.GetHashCode();
        hashCode = hashCode * 31 + depthFunction.GetHashCode();
        hashCode = hashCode * 31 + stencilEnable.GetHashCode();
        hashCode = hashCode * 31 + stencilReadMask.GetHashCode();
        hashCode = hashCode * 31 + stencilWriteMask.GetHashCode();
        hashCode = hashCode * 31 + frontFace.GetHashCode();
        hashCode = hashCode * 31 + backFace.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// Specifies the subresource from a 1D texture that is accessable to a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D10_TEX1D_DSV)</para>
/// </summary>
public value struct Texture1DDepthStencilView
{
public:
    /// <summary>
    /// The index of the first mipmap level to use (see mip slice).
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_DSV.MipSlice)</para>
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
/// <para>(Also see DirectX SDK: D3D10_TEX1D_RTV)</para>
/// </summary>
public value struct Texture1DRenderTargetView
{
public:
    /// <summary>
    /// The index of the mipmap level to use (see mip slice).
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_RTV.MipSlice)</para>
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
/// <para>(Also see DirectX SDK: D3D10_TEX1D_SRV)</para>
/// </summary>
public value struct Texture1DShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_SRV.MostDetailedMip)</para>
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
    /// Number of mipmap levels to use.
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_SRV.MipLevels)</para>
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
/// Specifies the subresource(s) from an array of multisampled 2D textures for a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D10_TEX2DMS_ARRAY_DSV)</para>
/// </summary>
public value struct Texture2DMultisampleArrayDepthStencilView
{
public:
    /// <summary>
    /// The index of the first texture to use in an array of textures (see array slice)
    /// <para>(Also see DirectX SDK: D3D10_TEX2DMS_ARRAY_DSV.FirstArraySlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX2DMS_ARRAY_DSV.ArraySize)</para>
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
/// Specifies the subresource(s) from a an array of multisampled 2D textures to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D10_TEX2DMS_ARRAY_RTV)</para>
/// </summary>
public value struct Texture2DMultisampleArrayRenderTargetView
{
public:
    /// <summary>
    /// The index of the first texture to use in an array of textures (see array slice)
    /// <para>(Also see DirectX SDK: D3D10_TEX2DMS_ARRAY_RTV.FirstArraySlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX2DMS_ARRAY_RTV.ArraySize)</para>
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
/// Specifies the subresource(s) from an array of multisampled 2D textures to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D10_TEX2DMS_ARRAY_SRV)</para>
/// </summary>
public value struct Texture2DMultisampleArrayShaderResourceView
{
public:
    /// <summary>
    /// The index of the first texture to use in an array of textures (see array slice)
    /// <para>(Also see DirectX SDK: D3D10_TEX2DMS_ARRAY_SRV.FirstArraySlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX2DMS_ARRAY_SRV.ArraySize)</para>
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
/// <para>(Also see DirectX SDK: D3D10_TEX2DMS_DSV)</para>
/// </summary>
public value struct Texture2DMultisampleDepthStencilView
{
public:
    // REVIEW: probably don't need to expose the field at all; remove property
    // and leave field private?

    /// <summary>
    /// Unused Field;
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
/// <para>(Also see DirectX SDK: D3D10_TEX2DMS_RTV)</para>
/// </summary>
public value struct Texture2DMultisampleRenderTargetView
{
public:
    // REVIEW: probably don't need to expose the field at all; remove property
    // and leave field private?

    /// <summary>
    /// Unused Field;
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
/// Specifies the subresource(s) from a multisampled 2D texture to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D10_TEX2DMS_SRV)</para>
/// </summary>
public value struct Texture2DMultisampleShaderResourceView
{
public:
    // REVIEW: probably don't need to expose the field at all; remove property
    // and leave field private?

    /// <summary>
    /// Unused Field;
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
/// Specifies the subresource(s) from an array 2D textures that are accessable to a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_DSV)</para>
/// </summary>
public value struct Texture2DArrayDepthStencilView
{
public:
    /// <summary>
    /// The index of the first mipmap level to use (see mip slice).
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_DSV.MipSlice)</para>
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
    /// The index of the first texture to use in an array of textures (see array slice)
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_DSV.FirstArraySlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_DSV.ArraySize)</para>
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
/// Specifies the subresource(s) from an array of 2D textures to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_RTV)</para>
/// </summary>
public value struct Texture2DArrayRenderTargetView
{
public:
    /// <summary>
    /// The index of the mipmap level to use (see mip slice).
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_RTV.MipSlice)</para>
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
    /// The index of the first texture to use in an array of textures (see array slice)
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_RTV.FirstArraySlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_RTV.ArraySize)</para>
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
/// Specifies the subresource(s) from an array of 2D textures to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_SRV)</para>
/// </summary>
public value struct Texture2DArrayShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_SRV.MostDetailedMip)</para>
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
    /// Number of subtextures to access.
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_SRV.MipLevels)</para>
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
    /// The index of the first texture to use in an array of textures (see array slice)
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_SRV.FirstArraySlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_ARRAY_SRV.ArraySize)</para>
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
/// Specifies the subresource from a 2D texture that is accessable to a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D10_TEX2D_DSV)</para>
/// </summary>
public value struct Texture2DDepthStencilView
{
public:
    /// <summary>
    /// The index of the first mipmap level to use (see mip slice).
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_DSV.MipSlice)</para>
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
/// <para>(Also see DirectX SDK: D3D10_TEX2D_RTV)</para>
/// </summary>
public value struct Texture2DRenderTargetView
{
public:
    /// <summary>
    /// The index of the mipmap level to use (see mip slice).
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_RTV.MipSlice)</para>
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
/// <para>(Also see DirectX SDK: D3D10_TEX2D_SRV)</para>
/// </summary>
public value struct Texture2DShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_SRV.MostDetailedMip)</para>
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
    /// Number of mipmap levels to use.
    /// <para>(Also see DirectX SDK: D3D10_TEX2D_SRV.MipLevels)</para>
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
/// Specifies the subresource(s) from a 3D texture to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D10_TEX3D_RTV)</para>
/// </summary>
public value struct Texture3DRenderTargetView
{
public:
    /// <summary>
    /// The index of the mipmap level to use (see mip slice).
    /// <para>(Also see DirectX SDK: D3D10_TEX3D_RTV.MipSlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX3D_RTV.FirstWSlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX3D_RTV.WSize)</para>
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
/// Specifies the subresource(s) from a 3D texture to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D10_TEX3D_SRV)</para>
/// </summary>
public value struct Texture3DShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D10_TEX3D_SRV.MostDetailedMip)</para>
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
    /// Number of mipmap levels to use.
    /// <para>(Also see DirectX SDK: D3D10_TEX3D_SRV.MipLevels)</para>
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
/// Specifies the subresource(s) from an array of 1D textures to use in a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_DSV)</para>
/// </summary>
public value struct Texture1DArrayDepthStencilView
{
public:
    /// <summary>
    /// The index of the first mipmap level to use (see mip slice).
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_DSV.MipSlice)</para>
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
    /// The index of the first texture to use in an array of textures (see array slice)
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_DSV.FirstArraySlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_DSV.ArraySize)</para>
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
/// Specifies the subresource(s) from an array of 1D textures to use in a render-target view.
/// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_RTV)</para>
/// </summary>
public value struct Texture1DArrayRenderTargetView
{
public:
    /// <summary>
    /// The index of the mipmap level to use (see mip slice).
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_RTV.MipSlice)</para>
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
    /// The index of the first texture to use in an array of textures (see array slice)
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_RTV.FirstArraySlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_RTV.ArraySize)</para>
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
/// Specifies the subresource(s) from an array of 1D textures to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_SRV)</para>
/// </summary>
public value struct Texture1DArrayShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_SRV.MostDetailedMip)</para>
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
    /// Number of subtextures to access.
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_SRV.MipLevels)</para>
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
    /// The index of the first texture to use in an array of textures (see array slice)
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_SRV.FirstArraySlice)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEX1D_ARRAY_SRV.ArraySize)</para>
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
/// Specifies the subresource(s) from an array of cube textures to use in a shader-resource view.
/// <para>(Also see DirectX SDK: D3D10_TEXCUBE_ARRAY_SRV1)</para>
/// </summary>
public value struct TextureCubeArrayShaderResourceView1
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D10_TEXCUBE_ARRAY_SRV1.MostDetailedMip)</para>
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
    /// Number of mipmap levels to use.
    /// <para>(Also see DirectX SDK: D3D10_TEXCUBE_ARRAY_SRV1.MipLevels)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXCUBE_ARRAY_SRV1.First2DArrayFace)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXCUBE_ARRAY_SRV1.NumCubes)</para>
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

    static Boolean operator == (TextureCubeArrayShaderResourceView1 textureCubeArrayShaderResourceView1, TextureCubeArrayShaderResourceView1 textureCubeArrayShaderResourceView2)
    {
        return (textureCubeArrayShaderResourceView1.mostDetailedMip == textureCubeArrayShaderResourceView2.mostDetailedMip) &&
            (textureCubeArrayShaderResourceView1.mipLevels == textureCubeArrayShaderResourceView2.mipLevels) &&
            (textureCubeArrayShaderResourceView1.first2DArrayFace == textureCubeArrayShaderResourceView2.first2DArrayFace) &&
            (textureCubeArrayShaderResourceView1.numCubes == textureCubeArrayShaderResourceView2.numCubes);
    }

    static Boolean operator != (TextureCubeArrayShaderResourceView1 textureCubeArrayShaderResourceView1, TextureCubeArrayShaderResourceView1 textureCubeArrayShaderResourceView2)
    {
        return !(textureCubeArrayShaderResourceView1 == textureCubeArrayShaderResourceView2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != TextureCubeArrayShaderResourceView1::typeid)
        {
            return false;
        }

        return *this == safe_cast<TextureCubeArrayShaderResourceView1>(obj);
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
/// <para>(Also see DirectX SDK: D3D10_TEXCUBE_SRV)</para>
/// </summary>
public value struct TextureCubeShaderResourceView
{
public:
    /// <summary>
    /// Index of the most detailed mipmap level to use; this number is between 0 and MipLevels.
    /// <para>(Also see DirectX SDK: D3D10_TEXCUBE_SRV.MostDetailedMip)</para>
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
    /// Number of mipmap levels to use.
    /// <para>(Also see DirectX SDK: D3D10_TEXCUBE_SRV.MipLevels)</para>
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
/// <para>(Also see DirectX SDK: D3D10_TEXTURE1D_DESC)</para>
/// </summary>
public value struct Texture1DDescription
{
public:
    /// <summary>
    /// Texture width (in texels).
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE1D_DESC.Width)</para>
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
    /// Number of subtextures (also called mipmap levels). Use 1 for a multisampled texture; or 0 to generate a full set of subtextures.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE1D_DESC.MipLevels)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE1D_DESC.ArraySize)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE1D_DESC.Format)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE1D_DESC.Usage)</para>
    /// </summary>
    property Usage Usage
    {
        Direct3D10::Usage get()
        {
            return usage;
        }

        void set(Direct3D10::Usage value)
        {
            usage = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="Direct3D10::BindingOptions"/>)<seealso cref="Direct3D10::BindingOptions"/> for binding to pipeline stages. The flags can be combined by a logical OR.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE1D_DESC.BindFlags)</para>
    /// </summary>
    property BindingOptions BindingOptions
    {
        Direct3D10::BindingOptions get()
        {
            return bindFlags;
        }

        void set(Direct3D10::BindingOptions value)
        {
            bindFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="CpuAccessOptions"/>)<seealso cref="CpuAccessOptions"/> to specify the types of CPU access allowed. Use 0 if CPU access is not required. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE1D_DESC.CPUAccessFlags)</para>
    /// </summary>
    property CpuAccessOptions CpuAccessOptions
    {
        Direct3D10::CpuAccessOptions get()
        {
            return cpuAccessFlags;
        }

        void set(Direct3D10::CpuAccessOptions value)
        {
            cpuAccessFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="MiscellaneousResourceOptions"/>)<seealso cref="MiscellaneousResourceOptions"/> that identifies other, less common resource options. Use 0 if none of these flags apply. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE1D_DESC.MiscFlags)</para>
    /// </summary>
    property MiscellaneousResourceOptions MiscellaneousResourceOptions
    {
        Direct3D10::MiscellaneousResourceOptions get()
        {
            return miscFlags;
        }

        void set(Direct3D10::MiscellaneousResourceOptions value)
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

    Texture1DDescription(const D3D10_TEXTURE1D_DESC &desc)
    {
        Width = static_cast<UInt32>(desc.Width);
        MipLevels = static_cast<UInt32>(desc.MipLevels);
        ArraySize = static_cast<UInt32>(desc.ArraySize);
        Format = static_cast<Graphics::Format>(desc.Format);
        Usage = static_cast<Direct3D10::Usage>(desc.Usage);
        BindingOptions = static_cast<Direct3D10::BindingOptions>(desc.BindFlags);
        CpuAccessOptions = static_cast<Direct3D10::CpuAccessOptions>(desc.CPUAccessFlags);
        MiscellaneousResourceOptions = static_cast<Direct3D10::MiscellaneousResourceOptions>(desc.MiscFlags);
    }

    void CopyTo(D3D10_TEXTURE1D_DESC &desc)
    {
        desc.Width = static_cast<UINT>(Width);
        desc.MipLevels = static_cast<UINT>(MipLevels);
        desc.ArraySize = static_cast<UINT>(ArraySize);
        desc.Format = static_cast<DXGI_FORMAT>(Format);
        desc.Usage = static_cast<D3D10_USAGE>(Usage);
        desc.BindFlags = static_cast<UINT>(BindingOptions);
        desc.CPUAccessFlags = static_cast<UINT>(CpuAccessOptions);
        desc.MiscFlags = static_cast<UINT>(MiscellaneousResourceOptions);
    }

private:

    UInt32 width;
    UInt32 mipLevels;
    UInt32 arraySize;
    Graphics::Format format;
    Direct3D10::Usage usage;
    Direct3D10::BindingOptions bindFlags;
    Direct3D10::CpuAccessOptions cpuAccessFlags;
    Direct3D10::MiscellaneousResourceOptions miscFlags;

};

/// <summary>
/// Describes a 2D texture.
/// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC)</para>
/// </summary>
public value struct Texture2DDescription
{
public:
    /// <summary>
    /// Texture width (in texels). See Remarks.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC.Width)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC.Height)</para>
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
    /// Number of subtextures (also called mipmap levels). Use 1 for a multisampled texture; or 0 to generate a full set of subtextures.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC.MipLevels)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC.ArraySize)</para>
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
    ///  format (see <see cref="Format"/>)<seealso cref="Format"/>.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC.Format)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC.SampleDesc)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC.Usage)</para>
    /// </summary>
    property Usage Usage
    {
        Direct3D10::Usage get()
        {
            return usage;
        }

        void set(Direct3D10::Usage value)
        {
            usage = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="Direct3D10::BindingOptions"/>)<seealso cref="Direct3D10::BindingOptions"/> for binding to pipeline stages. The flags can be combined by a logical OR.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC.BindFlags)</para>
    /// </summary>
    property BindingOptions BindingOptions
    {
        Direct3D10::BindingOptions get()
        {
            return bindFlags;
        }

        void set(Direct3D10::BindingOptions value)
        {
            bindFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="CpuAccessOptions"/>)<seealso cref="CpuAccessOptions"/> to specify the types of CPU access allowed. Use 0 if CPU access is not required. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC.CPUAccessFlags)</para>
    /// </summary>
    property CpuAccessOptions CpuAccessOptions
    {
        Direct3D10::CpuAccessOptions get()
        {
            return cpuAccessFlags;
        }

        void set(Direct3D10::CpuAccessOptions value)
        {
            cpuAccessFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="MiscellaneousResourceOptions"/>)<seealso cref="MiscellaneousResourceOptions"/> that identifies other, less common resource options. Use 0 if none of these flags apply. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE2D_DESC.MiscFlags)</para>
    /// </summary>
    property MiscellaneousResourceOptions MiscellaneousResourceOptions
    {
        Direct3D10::MiscellaneousResourceOptions get()
        {
            return miscFlags;
        }

        void set(Direct3D10::MiscellaneousResourceOptions value)
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

    Texture2DDescription(const D3D10_TEXTURE2D_DESC &desc)
    {
        Width = desc.Width;
        Height = desc.Height;
        MipLevels = desc.MipLevels;
        ArraySize = desc.ArraySize;
        Format = static_cast<Graphics::Format>(desc.Format);
        SampleDescription = Graphics::SampleDescription(desc.SampleDesc);
        Usage = static_cast<Direct3D10::Usage>(desc.Usage);
        BindingOptions = static_cast<Direct3D10::BindingOptions>(desc.BindFlags);
        CpuAccessOptions  = static_cast<Direct3D10::CpuAccessOptions>(desc.CPUAccessFlags);
        MiscellaneousResourceOptions = static_cast<Direct3D10::MiscellaneousResourceOptions>(desc.MiscFlags);
    }

    void CopyTo(D3D10_TEXTURE2D_DESC &desc)
    {
        desc.Width = Width;
        desc.Height = Height;
        desc.MipLevels = MipLevels;
        desc.ArraySize = ArraySize;
        desc.Format = static_cast<DXGI_FORMAT>(Format);
        SampleDescription.CopyTo(desc.SampleDesc);
        desc.Usage = static_cast<D3D10_USAGE>(Usage);
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
    Direct3D10::Usage usage;
    Direct3D10::BindingOptions bindFlags;
    Direct3D10::CpuAccessOptions cpuAccessFlags;
    Direct3D10::MiscellaneousResourceOptions miscFlags;

};

/// <summary>
/// Describes a 3D texture.
/// <para>(Also see DirectX SDK: D3D10_TEXTURE3D_DESC)</para>
/// </summary>
public value struct Texture3DDescription
{
public:
    /// <summary>
    /// Texture width (in texels). See Remarks.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE3D_DESC.Width)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE3D_DESC.Height)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE3D_DESC.Depth)</para>
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
    /// Number of subtextures (also called mipmap levels). Use 1 for a multisampled texture; or 0 to generate a full set of subtextures.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE3D_DESC.MipLevels)</para>
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
    ///  format (see <see cref="Format"/>)<seealso cref="Format"/>.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE3D_DESC.Format)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE3D_DESC.Usage)</para>
    /// </summary>
    property Direct3D10::Usage Usage
    {
        Direct3D10::Usage get()
        {
            return usage;
        }

        void set(Direct3D10::Usage value)
        {
            usage = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="Direct3D10::BindingOptions"/>)<seealso cref="Direct3D10::BindingOptions"/> for binding to pipeline stages. The flags can be combined by a logical OR.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE3D_DESC.BindFlags)</para>
    /// </summary>
    property BindingOptions BindingOptions
    {
        Direct3D10::BindingOptions get()
        {
            return bindFlags;
        }

        void set(Direct3D10::BindingOptions value)
        {
            bindFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="CpuAccessOptions"/>)<seealso cref="CpuAccessOptions"/> to specify the types of CPU access allowed. Use 0 if CPU access is not required. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE3D_DESC.CPUAccessFlags)</para>
    /// </summary>
    property CpuAccessOptions CpuAccessOptions
    {
        Direct3D10::CpuAccessOptions get()
        {
            return cpuAccessFlags;
        }

        void set(Direct3D10::CpuAccessOptions value)
        {
            cpuAccessFlags = value;
        }
    }
    /// <summary>
    /// Flags (see <see cref="MiscellaneousResourceOptions"/>)<seealso cref="MiscellaneousResourceOptions"/> that identifies other, less common resource options. Use 0 if none of these flags apply. These flags can be combined with a logical OR.
    /// <para>(Also see DirectX SDK: D3D10_TEXTURE3D_DESC.MiscFlags)</para>
    /// </summary>
    property MiscellaneousResourceOptions MiscellaneousResourceOptions
    {
        Direct3D10::MiscellaneousResourceOptions get()
        {
            return miscFlags;
        }

        void set(Direct3D10::MiscellaneousResourceOptions value)
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

    Texture3DDescription(const D3D10_TEXTURE3D_DESC &desc)
    {
        width = static_cast<UInt32>(desc.Width);
        height = static_cast<UInt32>(desc.Height);
        depth = static_cast<UInt32>(desc.Depth);
        mipLevels = static_cast<UInt32>(desc.MipLevels);
        format = static_cast<Graphics::Format>(desc.Format);
        usage = static_cast<Direct3D10::Usage>(desc.Usage);
        bindFlags = static_cast<Direct3D10::BindingOptions>(desc.BindFlags);
        cpuAccessFlags = static_cast<Direct3D10::CpuAccessOptions>(desc.CPUAccessFlags);
        miscFlags = static_cast<Direct3D10::MiscellaneousResourceOptions>(desc.MiscFlags);
    }

    void CopyTo(D3D10_TEXTURE3D_DESC &desc)
    {
        desc.Width = static_cast<UINT>(width);
        desc.Height = static_cast<UINT>(height);
        desc.Depth = static_cast<UINT>(depth);
        desc.MipLevels = static_cast<UINT>(mipLevels);
        desc.Format = static_cast<DXGI_FORMAT>(format);
        desc.Usage = static_cast<D3D10_USAGE>(usage);
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
    Direct3D10::Usage usage;
    Direct3D10::BindingOptions bindFlags;
    Direct3D10::CpuAccessOptions cpuAccessFlags;
    Direct3D10::MiscellaneousResourceOptions miscFlags;

};

/// <summary>
/// Specifies the subresource(s) from a texture that are accessible using a depth-stencil view.
/// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_VIEW_DESC)</para>
/// </summary>
[StructLayout(LayoutKind::Explicit)]
public value struct DepthStencilViewDescription
{
public:
    /// <summary>
    ///   format (see <see cref="Format"/>)<seealso cref="Format"/>. See remarks for allowable formats.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_VIEW_DESC.Format)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_VIEW_DESC.ViewDimension)</para>
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
    /// Specifies a 1D texture subresource (see <see cref="Texture1DDepthStencilView"/>)<seealso cref="Texture1DDepthStencilView"/>.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_VIEW_DESC.Texture1D)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_VIEW_DESC.Texture1DArray)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_VIEW_DESC.Texture2D)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_VIEW_DESC.Texture2DArray)</para>
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
    /// Unused -- a multisampled 2D texture contains a single subresource.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_VIEW_DESC.Texture2DMultisample)</para>
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
    /// Unused -- a multisampled 2D texture contains a single subresource per texture.
    /// <para>(Also see DirectX SDK: D3D10_DEPTH_STENCIL_VIEW_DESC.Texture2DMSArray)</para>
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
    Texture1DDepthStencilView texture1D;
    [FieldOffset(8)]
    Texture1DArrayDepthStencilView texture1DArray;
    [FieldOffset(8)]
    Texture2DDepthStencilView texture2D;
    [FieldOffset(8)]
    Texture2DArrayDepthStencilView texture2DArray;
    [FieldOffset(8)]
    Texture2DMultisampleDepthStencilView texture2DMultisample;
    [FieldOffset(8)]
    Texture2DMultisampleArrayDepthStencilView texture2DMultisampleArray;

internal:
    DepthStencilViewDescription(const D3D10_DEPTH_STENCIL_VIEW_DESC& desc)
    {
        Format = static_cast<Graphics::Format>(desc.Format);
        ViewDimension = static_cast<DepthStencilViewDimension>(desc.ViewDimension);

        // REVIEW: constructor with a switch? might be better to have
        // factory methods with different names according to what the caller
        // really wants to do; assuming, of course, the caller is basically
        // setting this switched-on value explicitly
        switch (ViewDimension)
        {
        case DepthStencilViewDimension::Texture1D :
              {
                  // REVIEW: constructor overload for Texture1DDepthStencilView that takes
                  // a D3D10_TEX1D_DSV would make this a little nicer
                  Texture1DDepthStencilView texture1D;

                  texture1D.MipSlice = desc.Texture1D.MipSlice;

                  Texture1D = texture1D;
                  break;
              }
        case DepthStencilViewDimension::Texture1DArray :
              {
                  // REVIEW: ditto
                  Texture1DArrayDepthStencilView texture1DArray;

                  texture1DArray.ArraySize = desc.Texture1DArray.ArraySize;
                  texture1DArray.FirstArraySlice = desc.Texture1DArray.FirstArraySlice;
                  texture1DArray.MipSlice = desc.Texture1DArray.MipSlice;

                  Texture1DArray = texture1DArray;
                  break;
              }
        case DepthStencilViewDimension::Texture2D :
              {
                  // REVIEW: ditto
                  Texture2DDepthStencilView texture2D;

                  texture2D.MipSlice = desc.Texture2D.MipSlice;

                  Texture2D = texture2D;
                  break;
              }
        case DepthStencilViewDimension::Texture2DArray :
              {
                  // REVIEW: ditto
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

    void CopyTo(D3D10_DEPTH_STENCIL_VIEW_DESC* desc)
    {
        desc->Format = static_cast<DXGI_FORMAT>(Format);
        desc->ViewDimension = static_cast<D3D10_DSV_DIMENSION>(ViewDimension);

        switch (ViewDimension)
        {
        case DepthStencilViewDimension::Texture1D :
              {
                  // REVIEW: ditto previous comments about CopyTo() method making this
                  // and similar code below nicer (especially the multi-line code)
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
/// Describes an effect.
/// <para>(Also see DirectX SDK: D3D10_EFFECT_DESC)</para>
/// </summary>
public value struct EffectDescription
{
public:
    /// <summary>
    /// TRUE if the effect is a child effect; otherwise FALSE.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_DESC.IsChildEffect)</para>
    /// </summary>
    property Boolean IsChildEffect
    {
        Boolean get()
        {
            return isChildEffect;
        }

        void set(Boolean value)
        {
            isChildEffect = value;
        }
    }
    /// <summary>
    /// The number of constant buffers.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_DESC.ConstantBuffers)</para>
    /// </summary>
    property UInt32 ConstantBuffers
    {
        UInt32 get()
        {
            return constantBuffers;
        }

        void set(UInt32 value)
        {
            constantBuffers = value;
        }
    }
    /// <summary>
    /// The number of constant buffers shared in an effect pool.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_DESC.SharedConstantBuffers)</para>
    /// </summary>
    property UInt32 SharedConstantBuffers
    {
        UInt32 get()
        {
            return sharedConstantBuffers;
        }

        void set(UInt32 value)
        {
            sharedConstantBuffers = value;
        }
    }
    /// <summary>
    /// The number of global variables.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_DESC.GlobalVariables)</para>
    /// </summary>
    property UInt32 GlobalVariables
    {
        UInt32 get()
        {
            return globalVariables;
        }

        void set(UInt32 value)
        {
            globalVariables = value;
        }
    }
    /// <summary>
    /// The number of global variables shared in an effect pool.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_DESC.SharedGlobalVariables)</para>
    /// </summary>
    property UInt32 SharedGlobalVariables
    {
        UInt32 get()
        {
            return sharedGlobalVariables;
        }

        void set(UInt32 value)
        {
            sharedGlobalVariables = value;
        }
    }
    /// <summary>
    /// The number of techniques.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_DESC.Techniques)</para>
    /// </summary>
    property UInt32 Techniques
    {
        UInt32 get()
        {
            return techniques;
        }

        void set(UInt32 value)
        {
            techniques = value;
        }
    }
private:

    Boolean isChildEffect;
    UInt32 constantBuffers;
    UInt32 sharedConstantBuffers;
    UInt32 globalVariables;
    UInt32 sharedGlobalVariables;
    UInt32 techniques;

public:

    static Boolean operator == (EffectDescription effectDescription1, EffectDescription effectDescription2)
    {
        return (effectDescription1.isChildEffect == effectDescription2.isChildEffect) &&
            (effectDescription1.constantBuffers == effectDescription2.constantBuffers) &&
            (effectDescription1.sharedConstantBuffers == effectDescription2.sharedConstantBuffers) &&
            (effectDescription1.globalVariables == effectDescription2.globalVariables) &&
            (effectDescription1.sharedGlobalVariables == effectDescription2.sharedGlobalVariables) &&
            (effectDescription1.techniques == effectDescription2.techniques);
    }

    static Boolean operator != (EffectDescription effectDescription1, EffectDescription effectDescription2)
    {
        return !(effectDescription1 == effectDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != EffectDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<EffectDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + isChildEffect.GetHashCode();
        hashCode = hashCode * 31 + constantBuffers.GetHashCode();
        hashCode = hashCode * 31 + sharedConstantBuffers.GetHashCode();
        hashCode = hashCode * 31 + globalVariables.GetHashCode();
        hashCode = hashCode * 31 + sharedGlobalVariables.GetHashCode();
        hashCode = hashCode * 31 + techniques.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes an effect shader.
/// <para>(Also see DirectX SDK: D3D10_EFFECT_SHADER_DESC)</para>
/// </summary>
public value struct EffectShaderDescription
{
public:
    /// <summary>
    /// Passed into CreateInputLayout. Only valid on a vertex shader or geometry shader. See ID3D10Device_CreateInputLayout.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_SHADER_DESC.pInputSignature)</para>
    /// </summary>
    property IntPtr InputSignature
    {
        IntPtr get()
        {
            return inputSignature;
        }

        void set(IntPtr value)
        {
            inputSignature = value;
        }
    }
    /// <summary>
    /// TRUE is the shader is defined inline; otherwise FALSE.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_SHADER_DESC.IsInline)</para>
    /// </summary>
    property Boolean IsInline
    {
        Boolean get()
        {
            return isInline;
        }

        void set(Boolean value)
        {
            isInline = value;
        }
    }
    /// <summary>
    /// The compiled shader.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_SHADER_DESC.pBytecode)</para>
    /// </summary>
    property IntPtr Bytecode
    {
        IntPtr get()
        {
            return bytecode;
        }

        void set(IntPtr value)
        {
            bytecode = value;
        }
    }
    /// <summary>
    /// The length of pBytecode.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_SHADER_DESC.BytecodeLength)</para>
    /// </summary>
    property UInt32 BytecodeLength
    {
        UInt32 get()
        {
            return bytecodeLength;
        }

        void set(UInt32 value)
        {
            bytecodeLength = value;
        }
    }
    /// <summary>
    /// A string that constains a declaration of the stream output from a geometry shader.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_SHADER_DESC.SODecl)</para>
    /// </summary>
    property String^ StreamOutputDeclaration
    {
        String^ get()
        {
            return streamOutputDeclaration;
        }

        void set(String^ value)
        {
            streamOutputDeclaration = value;
        }
    }
    /// <summary>
    /// The number of entries in the input signature.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_SHADER_DESC.NumInputSignatureEntries)</para>
    /// </summary>
    property UInt32 InputSignatureEntryCount
    {
        UInt32 get()
        {
            return numInputSignatureEntries;
        }

        void set(UInt32 value)
        {
            numInputSignatureEntries = value;
        }
    }
    /// <summary>
    /// The number of entries in the output signature.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_SHADER_DESC.NumOutputSignatureEntries)</para>
    /// </summary>
    property UInt32 OutputSignatureEntryCount
    {
        UInt32 get()
        {
            return numOutputSignatureEntries;
        }

        void set(UInt32 value)
        {
            numOutputSignatureEntries = value;
        }
    }

private:

    IntPtr inputSignature;
    Boolean isInline;
    IntPtr bytecode;
    UInt32 bytecodeLength;
    String^ streamOutputDeclaration;
    UInt32 numInputSignatureEntries;
    UInt32 numOutputSignatureEntries;

internal:
    EffectShaderDescription(const D3D10_EFFECT_SHADER_DESC & desc)
    {
        StreamOutputDeclaration = desc.SODecl ? gcnew String(desc.SODecl) : nullptr;

        InputSignature = IntPtr((void*)desc.pInputSignature);
        IsInline = desc.IsInline != 0;
        Bytecode = IntPtr((void*)desc.pBytecode);
        InputSignatureEntryCount = desc.NumInputSignatureEntries;
        OutputSignatureEntryCount = desc.NumOutputSignatureEntries;
    }
};

/// <summary>
/// Describes an effect-variable type.
/// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC)</para>
/// </summary>
public value struct EffectTypeDescription
{
public:
    /// <summary>
    /// A string that contains the variable name.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC.TypeName)</para>
    /// </summary>
    property String^ TypeName
    {
        String^ get()
        {
            return typeName;
        }

        void set(String^ value)
        {
            typeName = value;
        }
    }
    /// <summary>
    /// The variable class (see <see cref="ShaderVariableClass"/>)<seealso cref="ShaderVariableClass"/>.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC.Class)</para>
    /// </summary>
    property ShaderVariableClass ShaderVariableClass
    {
        Direct3D10::ShaderVariableClass get()
        {
            return shaderVariableClass;
        }

        void set(Direct3D10::ShaderVariableClass value)
        {
            shaderVariableClass = value;
        }
    }
    /// <summary>
    /// The variable type (see <see cref="ShaderVariableType"/>)<seealso cref="ShaderVariableType"/>.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC.Type)</para>
    /// </summary>
    property ShaderVariableType ShaderVariableType
    {
        Direct3D10::ShaderVariableType get()
        {
            return type;
        }

        void set(Direct3D10::ShaderVariableType value)
        {
            type = value;
        }
    }
    /// <summary>
    /// The number of elements if the variable is an array; otherwise 0.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC.Elements)</para>
    /// </summary>
    property UInt32 Elements
    {
        UInt32 get()
        {
            return elements;
        }

        void set(UInt32 value)
        {
            elements = value;
        }
    }
    /// <summary>
    /// The number of members if the variable is a structure; otherwise 0.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC.Members)</para>
    /// </summary>
    property UInt32 Members
    {
        UInt32 get()
        {
            return members;
        }

        void set(UInt32 value)
        {
            members = value;
        }
    }
    /// <summary>
    /// The number of rows if the variable is a matrix; otherwise 0.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC.Rows)</para>
    /// </summary>
    property UInt32 Rows
    {
        UInt32 get()
        {
            return rows;
        }

        void set(UInt32 value)
        {
            rows = value;
        }
    }
    /// <summary>
    /// The number of columns if the variable is a matrix; otherwise 0.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC.Columns)</para>
    /// </summary>
    property UInt32 Columns
    {
        UInt32 get()
        {
            return columns;
        }

        void set(UInt32 value)
        {
            columns = value;
        }
    }
    /// <summary>
    /// The number of bytes that the variable consumes when it is packed tightly by the compiler.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC.PackedSize)</para>
    /// </summary>
    property UInt32 PackedSize
    {
        UInt32 get()
        {
            return packedSize;
        }

        void set(UInt32 value)
        {
            packedSize = value;
        }
    }
    /// <summary>
    /// The number of bytes that the variable consumes before it is packed by the compiler.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC.UnpackedSize)</para>
    /// </summary>
    property UInt32 UnpackedSize
    {
        UInt32 get()
        {
            return unpackedSize;
        }

        void set(UInt32 value)
        {
            unpackedSize = value;
        }
    }
    /// <summary>
    /// The number of bytes between elements.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_TYPE_DESC.Stride)</para>
    /// </summary>
    property UInt32 Stride
    {
        UInt32 get()
        {
            return stride;
        }

        void set(UInt32 value)
        {
            stride = value;
        }
    }
private:

    Direct3D10::ShaderVariableClass shaderVariableClass;
    Direct3D10::ShaderVariableType type;
    UInt32 elements;
    UInt32 members;
    UInt32 rows;
    UInt32 columns;
    UInt32 packedSize;
    UInt32 unpackedSize;
    UInt32 stride;

internal:
    EffectTypeDescription(const D3D10_EFFECT_TYPE_DESC & effectTypeDescription)
    {
        TypeName = effectTypeDescription.TypeName ? gcnew String(effectTypeDescription.TypeName) : nullptr;

        ShaderVariableClass = static_cast<Direct3D10::ShaderVariableClass>(effectTypeDescription.Class);
        ShaderVariableType = static_cast<Direct3D10::ShaderVariableType>(effectTypeDescription.Type);

        Elements = effectTypeDescription.Elements ;
        Members = effectTypeDescription.Members ;
        Rows = effectTypeDescription.Rows ;
        Columns = effectTypeDescription.Columns ;
        PackedSize = effectTypeDescription.PackedSize ;
        UnpackedSize = effectTypeDescription.UnpackedSize ;
        Stride = effectTypeDescription.Stride ;
    }

    void CopyTo(D3D10_EFFECT_TYPE_DESC* effectTypeDescription, marshal_context^ context)
    {
        String^ name = TypeName;
        effectTypeDescription->TypeName = TypeName != nullptr ? context->marshal_as<const char*>(name) : NULL;

        effectTypeDescription->Class = static_cast<D3D10_SHADER_VARIABLE_CLASS>(ShaderVariableClass);
        effectTypeDescription->Type = static_cast<D3D10_SHADER_VARIABLE_TYPE>(ShaderVariableType);

        effectTypeDescription->Elements = Elements;
        effectTypeDescription->Members = Members;
        effectTypeDescription->Rows = Rows;
        effectTypeDescription->Columns = Columns;
        effectTypeDescription->PackedSize = PackedSize;
        effectTypeDescription->UnpackedSize = UnpackedSize;
        effectTypeDescription->Stride = Stride;
    }
private:

    String^ typeName;

public:

    static Boolean operator == (EffectTypeDescription effectTypeDescription1, EffectTypeDescription effectTypeDescription2)
    {
        return (effectTypeDescription1.shaderVariableClass == effectTypeDescription2.shaderVariableClass) &&
            (effectTypeDescription1.type == effectTypeDescription2.type) &&
            (effectTypeDescription1.elements == effectTypeDescription2.elements) &&
            (effectTypeDescription1.members == effectTypeDescription2.members) &&
            (effectTypeDescription1.rows == effectTypeDescription2.rows) &&
            (effectTypeDescription1.columns == effectTypeDescription2.columns) &&
            (effectTypeDescription1.packedSize == effectTypeDescription2.packedSize) &&
            (effectTypeDescription1.unpackedSize == effectTypeDescription2.unpackedSize) &&
            (effectTypeDescription1.stride == effectTypeDescription2.stride) &&
            (effectTypeDescription1.typeName == effectTypeDescription2.typeName);
    }

    static Boolean operator != (EffectTypeDescription effectTypeDescription1, EffectTypeDescription effectTypeDescription2)
    {
        return !(effectTypeDescription1 == effectTypeDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != EffectTypeDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<EffectTypeDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + shaderVariableClass.GetHashCode();
        hashCode = hashCode * 31 + type.GetHashCode();
        hashCode = hashCode * 31 + elements.GetHashCode();
        hashCode = hashCode * 31 + members.GetHashCode();
        hashCode = hashCode * 31 + rows.GetHashCode();
        hashCode = hashCode * 31 + columns.GetHashCode();
        hashCode = hashCode * 31 + packedSize.GetHashCode();
        hashCode = hashCode * 31 + unpackedSize.GetHashCode();
        hashCode = hashCode * 31 + stride.GetHashCode();
        hashCode = hashCode * 31 + typeName->GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// Describes an effect variable.
/// <para>(Also see DirectX SDK: D3D10_EFFECT_VARIABLE_DESC)</para>
/// </summary>
public value struct EffectVariableDescription
{
public:
    /// <summary>
    /// A string that contains the variable name.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_VARIABLE_DESC.Name)</para>
    /// </summary>
    property String^ Name
    {
        String^ get()
        {
            return name;
        }

        void set(String^ value)
        {
            name = value;
        }
    }
    /// <summary>
    /// The semantic attached to the variable; otherwise NULL.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_VARIABLE_DESC.Semantic)</para>
    /// </summary>
    property String^ Semantic
    {
        String^ get()
        {
            return semantic;
        }

        void set(String^ value)
        {
            semantic = value;
        }
    }
    /// <summary>
    /// Optional flags for effect variables.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_VARIABLE_DESC.Flags)</para>
    /// </summary>
    property UInt32 Options
    {
        UInt32 get()
        {
            return flags;
        }

        void set(UInt32 value)
        {
            flags = value;
        }
    }
    /// <summary>
    /// The number of annotations; otherwise 0.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_VARIABLE_DESC.Annotations)</para>
    /// </summary>
    property UInt32 Annotations
    {
        UInt32 get()
        {
            return annotations;
        }

        void set(UInt32 value)
        {
            annotations = value;
        }
    }
    /// <summary>
    /// The offset between the begining of the constant buffer and this variable; otherwise 0.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_VARIABLE_DESC.BufferOffset)</para>
    /// </summary>
    property UInt32 BufferOffset
    {
        UInt32 get()
        {
            return bufferOffset;
        }

        void set(UInt32 value)
        {
            bufferOffset = value;
        }
    }
    /// <summary>
    /// The register that this variable is bound to. To bind a variable explicitly use the D3D10_EFFECT_VARIABLE_EXPLICIT_BIND_POINT flag.
    /// <para>(Also see DirectX SDK: D3D10_EFFECT_VARIABLE_DESC.ExplicitBindPoint)</para>
    /// </summary>
    property UInt32 ExplicitBindPoint
    {
        UInt32 get()
        {
            return explicitBindPoint;
        }

        void set(UInt32 value)
        {
            explicitBindPoint = value;
        }
    }

private:

    String^ name;
    String^ semantic;
    UInt32 flags;
    UInt32 annotations;
    UInt32 bufferOffset;
    UInt32 explicitBindPoint;

internal:
    EffectVariableDescription(const D3D10_EFFECT_VARIABLE_DESC & effectVariableDescription)
    {
        Options = effectVariableDescription.Flags;
        Annotations = effectVariableDescription.Annotations;
        BufferOffset = effectVariableDescription.BufferOffset;
        ExplicitBindPoint = effectVariableDescription.ExplicitBindPoint;

        Name = effectVariableDescription.Name ? gcnew String(effectVariableDescription.Name) : nullptr;
        Semantic = effectVariableDescription.Semantic ? gcnew String(effectVariableDescription.Semantic) : nullptr;

    }

public:

    static Boolean operator == (EffectVariableDescription effectVariableDescription1, EffectVariableDescription effectVariableDescription2)
    {
        return (effectVariableDescription1.name == effectVariableDescription2.name) &&
            (effectVariableDescription1.semantic == effectVariableDescription2.semantic) &&
            (effectVariableDescription1.flags == effectVariableDescription2.flags) &&
            (effectVariableDescription1.annotations == effectVariableDescription2.annotations) &&
            (effectVariableDescription1.bufferOffset == effectVariableDescription2.bufferOffset) &&
            (effectVariableDescription1.explicitBindPoint == effectVariableDescription2.explicitBindPoint);
    }

    static Boolean operator != (EffectVariableDescription effectVariableDescription1, EffectVariableDescription effectVariableDescription2)
    {
        return !(effectVariableDescription1 == effectVariableDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != EffectVariableDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<EffectVariableDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + name->GetHashCode();
        hashCode = hashCode * 31 + semantic->GetHashCode();
        hashCode = hashCode * 31 + flags.GetHashCode();
        hashCode = hashCode * 31 + annotations.GetHashCode();
        hashCode = hashCode * 31 + bufferOffset.GetHashCode();
        hashCode = hashCode * 31 + explicitBindPoint.GetHashCode();

        return hashCode;
    }

};


/// <summary>
/// A description of a single element for the input-assembler stage.
/// <para>(Also see DirectX SDK: D3D10_INPUT_ELEMENT_DESC)</para>
/// </summary>
public value struct InputElementDescription
{
public:
    /// <summary>
    /// The HLSL semantic associated with this element in a shader input-signature.
    /// <para>(Also see DirectX SDK: D3D10_INPUT_ELEMENT_DESC.SemanticName)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_INPUT_ELEMENT_DESC.SemanticIndex)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_INPUT_ELEMENT_DESC.Format)</para>
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
    /// An integer value that identifies the input-assembler (see input slot). Valid values are between 0 and 15, defined in D3D10.h.
    /// <para>(Also see DirectX SDK: D3D10_INPUT_ELEMENT_DESC.InputSlot)</para>
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
    /// Optional. Offset (in bytes) between each element. Use D3D10_APPEND_ALIGNED_ELEMENT for convenience to define the current element directly after the previous one, including any packing if necessary.
    /// <para>(Also see DirectX SDK: D3D10_INPUT_ELEMENT_DESC.AlignedByteOffset)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_INPUT_ELEMENT_DESC.InputSlotClass)</para>
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
    /// The number of instances to draw before stepping one unit forward in a vertex buffer filled with instance data. Can be any unsigned integer value (0 means do not step) when the slot class is D3D10_INPUT_PER_INSTANCE_DATA; must be 0 when the slot class is PerVertexData.
    /// <para>(Also see DirectX SDK: D3D10_INPUT_ELEMENT_DESC.InstanceDataStepRate)</para>
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
    InputElementDescription(const D3D10_INPUT_ELEMENT_DESC & desc)
    {
        SemanticName = desc.SemanticName ? gcnew String(desc.SemanticName) : nullptr;
        SemanticIndex = desc.SemanticIndex;
        Format = static_cast<Graphics::Format>(desc.Format);
        InputSlot = desc.InputSlot;
        AlignedByteOffset = desc.AlignedByteOffset;
        InputSlotClass = static_cast<InputClassification>(desc.InputSlotClass);
        InstanceDataStepRate = desc.InstanceDataStepRate;
    }

    void CopyTo(D3D10_INPUT_ELEMENT_DESC * desc, marshal_context^ context)
    {
        desc->SemanticIndex = SemanticIndex;
        desc->Format = static_cast<DXGI_FORMAT>(Format);
        desc->InputSlot = InputSlot;
        desc->AlignedByteOffset = AlignedByteOffset;
        desc->InputSlotClass = static_cast<D3D10_INPUT_CLASSIFICATION>(InputSlotClass);
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
/// Provides access to subresource data in a 2D texture.
/// <para>(Also see DirectX SDK: D3D10_MAPPED_TEXTURE2D)</para>
/// </summary>
public value struct MappedTexture2D
{
public:
    /// <summary>
    /// Pointer to the data.
    /// <para>(Also see DirectX SDK: D3D10_MAPPED_TEXTURE2D.pData)</para>
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
    /// The pitch, or width, or physical size (in bytes), of one row of an uncompressed texture. A block-compressed texture is encoded in 4x4 blocks (see virtual size vs physical size) ; therefore, RowPitch is the number of bytes in a block of 4x4 texels.
    /// <para>(Also see DirectX SDK: D3D10_MAPPED_TEXTURE2D.RowPitch)</para>
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
private:

    UInt32 rowPitch;

internal:
    MappedTexture2D(const D3D10_MAPPED_TEXTURE2D & tex)
    {
        Data = IntPtr(tex.pData);
        RowPitch = tex.RowPitch;
    }

private:

    IntPtr data;

};

/// <summary>
/// Provides access to subresource data in a 3D texture.
/// <para>(Also see DirectX SDK: D3D10_MAPPED_TEXTURE3D)</para>
/// </summary>
public value struct MappedTexture3D
{
public:
    /// <summary>
    /// Pointer to the data.
    /// <para>(Also see DirectX SDK: D3D10_MAPPED_TEXTURE3D.pData)</para>
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
    /// The pitch, or width, or physical size (in bytes) of one row of an uncompressed texture. Since a block-compressed texture is encoded in 4x4 blocks, the RowPitch for a compressed texture is the number of bytes in a block of 4x4 texels. See virtual size vs physical size for more information on block compression.
    /// <para>(Also see DirectX SDK: D3D10_MAPPED_TEXTURE3D.RowPitch)</para>
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
    /// The pitch or number of bytes in all rows for a single depth.
    /// <para>(Also see DirectX SDK: D3D10_MAPPED_TEXTURE3D.DepthPitch)</para>
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
    MappedTexture3D(const D3D10_MAPPED_TEXTURE3D & tex)
    {
        Data = IntPtr(tex.pData);
        RowPitch = tex.RowPitch;
        DepthPitch = tex.DepthPitch;
    }
private:

    IntPtr data;

};


/// <summary>
/// A debug message in the Information Queue.
/// <para>(Also see DirectX SDK: D3D10_MESSAGE)</para>
/// </summary>
public value struct Message
{
public:
    /// <summary>
    /// The category of the message. See MessageCategory.
    /// <para>(Also see DirectX SDK: D3D10_MESSAGE.Category)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_MESSAGE.Severity)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_MESSAGE.ID)</para>
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
    /// The message description string.
    /// <para>(Also see DirectX SDK: D3D10_MESSAGE.pDescription)</para>
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
    Message (D3D10_MESSAGE* msg)
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
/// Describes an effect pass, which contains pipeline state.
/// <para>(Also see DirectX SDK: D3D10_PASS_DESC)</para>
/// </summary>
public value struct PassDescription
{
public:
    /// <summary>
    /// A string that contains the name of the pass; otherwise NULL.
    /// <para>(Also see DirectX SDK: D3D10_PASS_DESC.Name)</para>
    /// </summary>
    property String^ Name
    {
        String^ get()
        {
            return name;
        }

        void set(String^ value)
        {
            name = value;
        }
    }
    /// <summary>
    /// The number of annotations.
    /// <para>(Also see DirectX SDK: D3D10_PASS_DESC.Annotations)</para>
    /// </summary>
    property UInt32 Annotations
    {
        UInt32 get()
        {
            return annotations;
        }

        void set(UInt32 value)
        {
            annotations = value;
        }
    }
    /// <summary>
    /// The input signature or the vertex shader; otherwise NULL.
    /// <para>(Also see DirectX SDK: D3D10_PASS_DESC.pIAInputSignature)</para>
    /// </summary>
    property IntPtr InputAssemblerInputSignature
    {
        IntPtr get()
        {
            return inputAssemblerInputSignature;
        }

        void set(IntPtr value)
        {
            inputAssemblerInputSignature = value;
        }
    }
    /// <summary>
    /// The size of the input signature (in bytes).
    /// <para>(Also see DirectX SDK: D3D10_PASS_DESC.IAInputSignatureSize)</para>
    /// </summary>
    property UInt32 InputAssemblerInputSignatureSize
    {
        UInt32 get()
        {
            return inputAssemblerInputSignatureSize;
        }

        void set(UInt32 value)
        {
            inputAssemblerInputSignatureSize = value;
        }
    }
    /// <summary>
    /// The stencil-reference value used in the depth-stencil state (see Configuring Depth-Stencil Functionality (Direct3D 10)).
    /// <para>(Also see DirectX SDK: D3D10_PASS_DESC.StencilRef)</para>
    /// </summary>
    property UInt32 StencilRef
    {
        UInt32 get()
        {
            return stencilRef;
        }

        void set(UInt32 value)
        {
            stencilRef = value;
        }
    }
    /// <summary>
    /// The sample mask for the blend state (see Configuring Blending Functionality (Direct3D 10)).
    /// <para>(Also see DirectX SDK: D3D10_PASS_DESC.SampleMask)</para>
    /// </summary>
    property UInt32 SampleMask
    {
        UInt32 get()
        {
            return sampleMask;
        }

        void set(UInt32 value)
        {
            sampleMask = value;
        }
    }
    /// <summary>
    /// The per-component blend factors (RGBA) for the blend state (see Configuring Blending Functionality (Direct3D 10)).
    /// <para>(Also see DirectX SDK: D3D10_PASS_DESC.BlendFactor)</para>
    /// </summary>
    property ReadOnlyCollection<Single>^ BlendFactor
    {
        ReadOnlyCollection<Single>^ get()
        {
            if (blendFactor == nullptr)
            {
                 blendFactor = gcnew array<Single>(BlendFactorArrayLength);
            }

            return Array::AsReadOnly(blendFactor);
        }

    }

private:

    UInt32 inputAssemblerInputSignatureSize;
    UInt32 stencilRef;
    UInt32 sampleMask;

    String^ name;
    UInt32 annotations;
    IntPtr inputAssemblerInputSignature;

internal:
    PassDescription(const D3D10_PASS_DESC & desc)
    {
        Name = desc.Name ? gcnew String(desc.Name) : nullptr;
        Annotations = desc.Annotations;
        InputAssemblerInputSignature = IntPtr((void*)desc.pIAInputSignature);
        InputAssemblerInputSignatureSize = static_cast<UInt32>(desc.IAInputSignatureSize);
        StencilRef = desc.StencilRef;
        SampleMask = desc.SampleMask;


        blendFactor = gcnew array<Single>(BlendFactorArrayLength);       
        pin_ptr<Single> ptr = &blendFactor[0];

        memcpy(ptr, desc.BlendFactor, sizeof(FLOAT)* BlendFactorArrayLength);        
    }
private:
    literal int BlendFactorArrayLength = 4;
    array<Single>^ blendFactor;
};

ref class EffectShaderVariable;
/// <summary>
/// Describes an effect variable that contains a shader.
/// <para>(Also see DirectX SDK: D3D10_PASS_SHADER_DESC)</para>
/// </summary>
public value struct PassShaderDescription
{
public:
    /// <summary>
    /// The variable that the shader came from. If it is an inline shader assignment, the returned interface will be an anonymous shader variable, which is not retrievable any other way.  Its name in the variable description will be "$Anonymous". If there is no assignment of this type in the pass block, this will point to a shader variable that returns false when IsValid is called.
    /// <para>(Also see DirectX SDK: D3D10_PASS_SHADER_DESC.pShaderVariable)</para>
    /// </summary>
    property EffectShaderVariable^ ShaderVariable
    {
        EffectShaderVariable^ get()
        {
            return shaderVariable;
        }

        void set(EffectShaderVariable^ value)
        {
            shaderVariable = value;
        }
    }

    /// <summary>
    /// A zero-based array index; otherwise 0.
    /// <para>(Also see DirectX SDK: D3D10_PASS_SHADER_DESC.ShaderIndex)</para>
    /// </summary>
    property UInt32 ShaderIndex
    {
        UInt32 get()
        {
            return shaderIndex;
        }

        void set(UInt32 value)
        {
            shaderIndex = value;
        }
    }
private:

    EffectShaderVariable^ shaderVariable;
    UInt32 shaderIndex;

internal:
    PassShaderDescription(const D3D10_PASS_SHADER_DESC&);
};


/// <summary>
/// Query information about graphics-pipeline activity in between calls to Asynchronous.Begin and Asynchronous.End.
/// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_PIPELINE_STATISTICS)</para>
/// </summary>
public value struct QueryDataPipelineStatistics
{
public:
    /// <summary>
    /// Number of vertices read by input assembler.
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_PIPELINE_STATISTICS.IAVertices)</para>
    /// </summary>
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
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_PIPELINE_STATISTICS.IAPrimitives)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_PIPELINE_STATISTICS.VSInvocations)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_PIPELINE_STATISTICS.GSInvocations)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_PIPELINE_STATISTICS.GSPrimitives)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_PIPELINE_STATISTICS.CInvocations)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_PIPELINE_STATISTICS.CPrimitives)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_PIPELINE_STATISTICS.PSInvocations)</para>
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
private:

    UInt64 inputAssemblerVertices;
    UInt64 inputAssemblerPrimitives;
    UInt64 vertexShaderInvocations;
    UInt64 geometryShaderInvocations;
    UInt64 geometryShaderPrimitives;
    UInt64 cInvocations;
    UInt64 cPrimitives;
    UInt64 pixelShaderInvocations;

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
            (queryDataPipelineStatistics1.pixelShaderInvocations == queryDataPipelineStatistics2.pixelShaderInvocations);
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

        return hashCode;
    }

};

/// <summary>
/// Query information about the amount of data streamed out to the stream-output buffers in between Asynchronous.Begin and Asynchronous.End.
/// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_SO_STATISTICS)</para>
/// </summary>
public value struct QueryDataStreamOutputStatistics
{
public:
    /// <summary>
    /// Number of primitives (that is, points, lines, and triangles) written to the stream-output buffers.
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_SO_STATISTICS.NumPrimitivesWritten)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_SO_STATISTICS.PrimitivesStorageNeeded)</para>
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
/// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_TIMESTAMP_DISJOINT)</para>
/// </summary>
public value struct QueryDataTimestampDisjoint
{
public:
    /// <summary>
    /// How frequently the GPU counter increments in Hz.
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_TIMESTAMP_DISJOINT.Frequency)</para>
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
    /// If this is TRUE, something occurred in between the query's Asynchronous.Begin and Asynchronous.End calls that caused the timestamp counter to become discontinuous or disjoint, such as unplugging the AC chord on a laptop, overheating, or throttling up/down due to laptop savings events. The timestamp returned by Asynchronous.GetData for a timestamp query is only reliable if Disjoint is FALSE.
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DATA_TIMESTAMP_DISJOINT.Disjoint)</para>
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
/// <para>(Also see DirectX SDK: D3D10_QUERY_DESC)</para>
/// </summary>
public value struct QueryDescription
{
public:
    /// <summary>
    /// Type of query.
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DESC.D3DQuery)</para>
    /// </summary>
    property Query Query
    {
        Direct3D10::Query get()
        {
            return query;
        }

        void set(Direct3D10::Query value)
        {
            query = value;
        }
    }
    /// <summary>
    /// Miscellaneous flags (see <see cref="MiscellaneousQueryOptions"/>)<seealso cref="MiscellaneousQueryOptions"/>.
    /// <para>(Also see DirectX SDK: D3D10_QUERY_DESC.MiscFlags)</para>
    /// </summary>
    property MiscellaneousQueryOptions MiscellaneousQueryOptions
    {
        Direct3D10::MiscellaneousQueryOptions get()
        {
            return miscFlags;
        }

        void set(Direct3D10::MiscellaneousQueryOptions value)
        {
            miscFlags = value;
        }
    }

private:

    Direct3D10::MiscellaneousQueryOptions miscFlags;

internal:
    QueryDescription (const D3D10_QUERY_DESC& desc)
    {
        Query = static_cast<Direct3D10::Query>(desc.Query);
        MiscellaneousQueryOptions = static_cast<Direct3D10::MiscellaneousQueryOptions>(desc.MiscFlags);
    }

    void CopyTo(D3D10_QUERY_DESC* desc)
    {
        desc->Query = static_cast<D3D10_QUERY>(Query);
        desc->MiscFlags = static_cast<UINT>(MiscellaneousQueryOptions);
    }
private:

    Direct3D10::Query query;

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
/// Describes the rasterizer state.
/// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC)</para>
/// </summary>
public value struct RasterizerDescription
{
public:
    /// <summary>
    /// A member of the FillMode enumerated type that determines the fill mode to use when rendering.  The default value is Solid.
    /// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC.FillMode)</para>
    /// </summary>
    property FillMode FillMode
    {
        Direct3D10::FillMode get()
        {
            return fillMode;
        }

        void set(Direct3D10::FillMode value)
        {
            fillMode = value;
        }
    }
    /// <summary>
    /// A member of the CullMode enumerated type that indicates whether triangles facing the specified direction are drawn.  The default value is Back.
    /// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC.CullMode)</para>
    /// </summary>
    property CullMode CullMode
    {
        Direct3D10::CullMode get()
        {
            return cullMode;
        }

        void set(Direct3D10::CullMode value)
        {
            cullMode = value;
        }
    }
    /// <summary>
    /// Determines if a triangle is front-facing or back-facing. If this parameter is TRUE, then a triangle is considered front-facing if its vertices are counter-clockwise on the render target, and considered back-facing if they are clockwise. If this parameter is FALSE, then the opposite is true.  The default value is FALSE.
    /// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC.FrontCounterClockwise)</para>
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
    /// Specifies the depth value added to a given pixel. The default value is 0. For more information, see Depth Bias.
    /// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC.DepthBias)</para>
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
    /// Specifies the maximum depth bias of a pixel. The default value is 0.0f. For more information, see Depth Bias.
    /// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC.DepthBiasClamp)</para>
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
    /// Specifies a scalar on a given pixel's slope. The default value is 0.0f. For more information, see Depth Bias.
    /// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC.SlopeScaledDepthBias)</para>
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
    /// Enables or disables clipping based on distance.  The default value is TRUE.
    /// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC.DepthClipEnable)</para>
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
    /// Enable or disables scissor-rectangle culling. All pixels outside an active scissor rectangle are culled. The default value is FALSE. For more information, see Set the Scissor Rectangle.
    /// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC.ScissorEnable)</para>
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
    /// Enables or disables multisample antialiasing.  The default value is FALSE.
    /// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC.MultisampleEnable)</para>
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
    /// Enable or disables line antialiasing. Note that this option only applies when alpha blending is enabled, you are drawing lines, and the MultisampleEnable member is FALSE.  The default value is FALSE.
    /// <para>(Also see DirectX SDK: D3D10_RASTERIZER_DESC.AntialiasedLineEnable)</para>
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

    Direct3D10::FillMode fillMode;
    Direct3D10::CullMode cullMode;
    Boolean frontCounterClockwise;
    Int32 depthBias;
    Single depthBiasClamp;
    Single slopeScaledDepthBias;
    Boolean depthClipEnable;
    Boolean scissorEnable;
    Boolean multisampleEnable;
    Boolean antiAliasedLineEnable;

internal:
    RasterizerDescription(const D3D10_RASTERIZER_DESC& desc)
    {
        FillMode = static_cast<Direct3D10::FillMode>(desc.FillMode);
        CullMode = static_cast<Direct3D10::CullMode>(desc.CullMode);
        FrontCounterclockwise = desc.FrontCounterClockwise != 0;
        DepthBias = desc.DepthBias;
        DepthBiasClamp = desc.DepthBiasClamp;
        SlopeScaledDepthBias = desc.SlopeScaledDepthBias;
        DepthClipEnable = desc.DepthClipEnable != 0;
        ScissorEnable = desc.ScissorEnable != 0;
        MultisampleEnable = desc.MultisampleEnable != 0;
        AntiAliasedLineEnable = desc.AntialiasedLineEnable != 0;
    }

    void CopyTo(D3D10_RASTERIZER_DESC* desc)
    {
        desc->FillMode = static_cast<D3D10_FILL_MODE>(FillMode) ;
        desc->CullMode = static_cast<D3D10_CULL_MODE>(CullMode) ;

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
/// Specifies the subresource(s) from a resource that are accessible using a render-target view.
/// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC)</para>
/// </summary>
[StructLayout(LayoutKind::Explicit)]
public value struct RenderTargetViewDescription
{
public:
    /// <summary>
    /// The data format (see <see cref="Format"/>)<seealso cref="Format"/>.
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC.Format)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC.ViewDimension)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC.Buffer)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC.Texture1D)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC.Texture1DArray)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC.Texture2D)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC.Texture2DArray)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC.Texture2DMultisample)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC.Texture2DMSArray)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_RENDER_TARGET_VIEW_DESC.Texture3D)</para>
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
/// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC)</para>
/// </summary>
public value struct SamplerDescription
{
public:
    /// <summary>
    /// Filtering method to use when sampling a texture (see <see cref="Filter"/>)<seealso cref="Filter"/>.
    /// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC.Filter)</para>
    /// </summary>
    property Filter Filter
    {
        Direct3D10::Filter get()
        {
            return filter;
        }

        void set(Direct3D10::Filter value)
        {
            filter = value;
        }
    }
    /// <summary>
    /// Method to use for resolving a u texture coordinate that is outside the 0 to 1 range (see <see cref="TextureAddressMode"/>)<seealso cref="TextureAddressMode"/>.
    /// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC.AddressU)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC.AddressV)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC.AddressW)</para>
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
    /// Offset from the calculated mipmap level. For example, if Direct3D calculates that a texture should be sampled at mipmap level 3 and MipLevelOfDetailBias is 2, then the texture will be sampled at mipmap level 5.
    /// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC.MipLODBias)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC.MaxAnisotropy)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC.ComparisonFunc)</para>
    /// </summary>
    property ComparisonFunction ComparisonFunction
    {
        Direct3D10::ComparisonFunction get()
        {
            return comparisonFunction;
        }

        void set(Direct3D10::ComparisonFunction value)
        {
            comparisonFunction = value;
        }
    }

    /// <summary>
    /// Border color to use if Border is specified for AddressU, AddressV, or AddressW. Range must be between 0.0 and 1.0 inclusive.
    /// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC.BorderColor)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC.MinLOD)</para>
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
    /// Upper end of the mipmap range to clamp access to, where 0 is the largest and most detailed mipmap level and any level higher than that is less detailed. This value must be greater than or equal to MinimumLevelOfDetail. To have no upper limit on LOD set this to a large value such as D3D10_FLOAT32_MAX.
    /// <para>(Also see DirectX SDK: D3D10_SAMPLER_DESC.MaxLOD)</para>
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

    Direct3D10::Filter filter;
    TextureAddressMode addressU;
    TextureAddressMode addressV;
    TextureAddressMode addressW;
    Single mipLODBias;
    UInt32 maxAnisotropy;
    Direct3D10::ComparisonFunction comparisonFunction;
    ColorRgba borderColor;
    Single minLOD;
    Single maxLOD;

internal:
    SamplerDescription(const D3D10_SAMPLER_DESC & desc)
    {
        Filter = static_cast<Direct3D10::Filter>(desc.Filter);
        AddressU = static_cast<TextureAddressMode>(desc.AddressU);
        AddressV = static_cast<TextureAddressMode>(desc.AddressV);
        AddressW = static_cast<TextureAddressMode>(desc.AddressW);
        
        MipLevelOfDetailBias = desc.MipLODBias;
        MaxAnisotropy = desc.MaxAnisotropy;

        ComparisonFunction = static_cast<Direct3D10::ComparisonFunction>(desc.ComparisonFunc);

        BorderColor = ColorRgba(desc.BorderColor);

        MinimumLevelOfDetail = desc.MinLOD;
        MaximumLevelOfDetail = desc.MaxLOD;
    }

    void CopyTo(D3D10_SAMPLER_DESC* desc)
    {
        desc->Filter = static_cast<D3D10_FILTER>(Filter);
        desc->AddressU = static_cast<D3D10_TEXTURE_ADDRESS_MODE>(AddressU);
        desc->AddressV = static_cast<D3D10_TEXTURE_ADDRESS_MODE>(AddressV);
        desc->AddressW = static_cast<D3D10_TEXTURE_ADDRESS_MODE>(AddressW);
        
        desc->MipLODBias = MipLevelOfDetailBias;
        desc->MaxAnisotropy = MaxAnisotropy;

        desc->BorderColor[0] = BorderColor.Red;
        desc->BorderColor[1] = BorderColor.Green;
        desc->BorderColor[2] = BorderColor.Blue;
        desc->BorderColor[3] = BorderColor.Alpha;

        desc->ComparisonFunc= static_cast<D3D10_COMPARISON_FUNC>(ComparisonFunction);

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
/// Describes a shader constant-buffer.
/// <para>(Also see DirectX SDK: D3D10_SHADER_BUFFER_DESC)</para>
/// </summary>
public value struct ShaderBufferDescription
{
public:
    /// <summary>
    /// The name of the buffer.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_BUFFER_DESC.Name)</para>
    /// </summary>
    property String^ Name
    {
        String^ get()
        {
            return name;
        }

        void set(String^ value)
        {
            name = value;
        }
    }
    /// <summary>
    /// The intended use of the constant data. See ConstantBufferType.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_BUFFER_DESC.Type)</para>
    /// </summary>
    property ConstantBufferType ConstantBufferType
    {
        Direct3D10::ConstantBufferType get()
        {
            return type;
        }

        void set(Direct3D10::ConstantBufferType value)
        {
            type = value;
        }
    }
    /// <summary>
    /// The number of unique variables.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_BUFFER_DESC.Variables)</para>
    /// </summary>
    property UInt32 Variables
    {
        UInt32 get()
        {
            return variables;
        }

        void set(UInt32 value)
        {
            variables = value;
        }
    }
    /// <summary>
    /// Buffer size (in bytes).
    /// <para>(Also see DirectX SDK: D3D10_SHADER_BUFFER_DESC.Size)</para>
    /// </summary>
    property UInt32 Size
    {
        UInt32 get()
        {
            return size;
        }

        void set(UInt32 value)
        {
            size = value;
        }
    }
    /// <summary>
    /// Shader buffer properties. See ShaderConstantBufferFlags.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_BUFFER_DESC.uFlags)</para>
    /// </summary>
    property UInt32 Options
    {
        UInt32 get()
        {
            return flags;
        }

        void set(UInt32 value)
        {
            flags = value;
        }
    }

private:

    UInt32 size;
    UInt32 flags;

internal:
    ShaderBufferDescription(const D3D10_SHADER_BUFFER_DESC & desc)
    {
        Name = desc.Name ? gcnew String(desc.Name) : nullptr;
        ConstantBufferType = static_cast<Direct3D10::ConstantBufferType>(desc.Type);
        Variables = desc.Variables;
        Size = desc.Size;
        Options = desc.Type;
    }
private:

    String^ name;
    Direct3D10::ConstantBufferType type;
    UInt32 variables;

public:

    static Boolean operator == (ShaderBufferDescription shaderBufferDescription1, ShaderBufferDescription shaderBufferDescription2)
    {
        return (shaderBufferDescription1.size == shaderBufferDescription2.size) &&
            (shaderBufferDescription1.flags == shaderBufferDescription2.flags) &&
            (shaderBufferDescription1.name == shaderBufferDescription2.name) &&
            (shaderBufferDescription1.type == shaderBufferDescription2.type) &&
            (shaderBufferDescription1.variables == shaderBufferDescription2.variables);
    }

    static Boolean operator != (ShaderBufferDescription shaderBufferDescription1, ShaderBufferDescription shaderBufferDescription2)
    {
        return !(shaderBufferDescription1 == shaderBufferDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != ShaderBufferDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<ShaderBufferDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + size.GetHashCode();
        hashCode = hashCode * 31 + flags.GetHashCode();
        hashCode = hashCode * 31 + name->GetHashCode();
        hashCode = hashCode * 31 + type.GetHashCode();
        hashCode = hashCode * 31 + variables.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a shader.
/// <para>(Also see DirectX SDK: D3D10_SHADER_DESC)</para>
/// </summary>
public value struct ShaderDescription
{
public:
    /// <summary>
    /// Shader version.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.Version)</para>
    /// </summary>
    property UInt32 Version
    {
        UInt32 get()
        {
            return version;
        }

        void set(UInt32 value)
        {
            version = value;
        }
    }
    /// <summary>
    /// The name of the originator of the shader.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.Creator)</para>
    /// </summary>
    property String^ Creator
    {
        String^ get()
        {
            return creator;
        }

        void set(String^ value)
        {
            creator = value;
        }
    }
    /// <summary>
    /// Shader compilation/parse flags.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.Flags)</para>
    /// </summary>
    property UInt32 Options
    {
        UInt32 get()
        {
            return flags;
        }

        void set(UInt32 value)
        {
            flags = value;
        }
    }
    /// <summary>
    /// The number of shader-constant buffers.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.ConstantBuffers)</para>
    /// </summary>
    property UInt32 ConstantBuffers
    {
        UInt32 get()
        {
            return constantBuffers;
        }

        void set(UInt32 value)
        {
            constantBuffers = value;
        }
    }
    /// <summary>
    /// The number of resource (textures and buffers) bound to a shader.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.BoundResources)</para>
    /// </summary>
    property UInt32 BoundResources
    {
        UInt32 get()
        {
            return boundResources;
        }

        void set(UInt32 value)
        {
            boundResources = value;
        }
    }
    /// <summary>
    /// The number of parameters in the input signature.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.InputParameters)</para>
    /// </summary>
    property UInt32 InputParameters
    {
        UInt32 get()
        {
            return inputParameters;
        }

        void set(UInt32 value)
        {
            inputParameters = value;
        }
    }
    /// <summary>
    /// The number of parameters in the output signature.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.OutputParameters)</para>
    /// </summary>
    property UInt32 OutputParameters
    {
        UInt32 get()
        {
            return outputParameters;
        }

        void set(UInt32 value)
        {
            outputParameters = value;
        }
    }
    /// <summary>
    /// The number of intermediate-language instructions in the compiled shader.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.InstructionCount)</para>
    /// </summary>
    property UInt32 InstructionCount
    {
        UInt32 get()
        {
            return instructionCount;
        }

        void set(UInt32 value)
        {
            instructionCount = value;
        }
    }
    /// <summary>
    /// The number of temporary registers in the compiled shader.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.TempRegisterCount)</para>
    /// </summary>
    property UInt32 TempRegisterCount
    {
        UInt32 get()
        {
            return tempRegisterCount;
        }

        void set(UInt32 value)
        {
            tempRegisterCount = value;
        }
    }
    /// <summary>
    /// Number of temporary arrays used.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.TempArrayCount)</para>
    /// </summary>
    property UInt32 TempArrayCount
    {
        UInt32 get()
        {
            return tempArrayCount;
        }

        void set(UInt32 value)
        {
            tempArrayCount = value;
        }
    }
    /// <summary>
    /// Number of constant defines.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.DefCount)</para>
    /// </summary>
    property UInt32 DefCount
    {
        UInt32 get()
        {
            return defCount;
        }

        void set(UInt32 value)
        {
            defCount = value;
        }
    }
    /// <summary>
    /// Number of declarations (input + output).
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.DclCount)</para>
    /// </summary>
    property UInt32 DeclarationCount
    {
        UInt32 get()
        {
            return dclCount;
        }

        void set(UInt32 value)
        {
            dclCount = value;
        }
    }
    /// <summary>
    /// Number of non-categorized texture instructions.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.TextureNormalInstructions)</para>
    /// </summary>
    property UInt32 TextureNormalInstructions
    {
        UInt32 get()
        {
            return textureNormalInstructions;
        }

        void set(UInt32 value)
        {
            textureNormalInstructions = value;
        }
    }
    /// <summary>
    /// Number of texture load instructions
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.TextureLoadInstructions)</para>
    /// </summary>
    property UInt32 TextureLoadInstructions
    {
        UInt32 get()
        {
            return textureLoadInstructions;
        }

        void set(UInt32 value)
        {
            textureLoadInstructions = value;
        }
    }
    /// <summary>
    /// Number of texture comparison instructions
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.TextureCompInstructions)</para>
    /// </summary>
    property UInt32 TextureCompInstructions
    {
        UInt32 get()
        {
            return textureCompInstructions;
        }

        void set(UInt32 value)
        {
            textureCompInstructions = value;
        }
    }
    /// <summary>
    /// Number of texture bias instructions
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.TextureBiasInstructions)</para>
    /// </summary>
    property UInt32 TextureBiasInstructions
    {
        UInt32 get()
        {
            return textureBiasInstructions;
        }

        void set(UInt32 value)
        {
            textureBiasInstructions = value;
        }
    }
    /// <summary>
    /// Number of texture gradient instructions.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.TextureGradientInstructions)</para>
    /// </summary>
    property UInt32 TextureGradientInstructions
    {
        UInt32 get()
        {
            return textureGradientInstructions;
        }

        void set(UInt32 value)
        {
            textureGradientInstructions = value;
        }
    }
    /// <summary>
    /// Number of floating point arithmetic instructions used.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.FloatInstructionCount)</para>
    /// </summary>
    property UInt32 FloatInstructionCount
    {
        UInt32 get()
        {
            return floatInstructionCount;
        }

        void set(UInt32 value)
        {
            floatInstructionCount = value;
        }
    }
    /// <summary>
    /// Number of signed integer arithmetic instructions used.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.IntInstructionCount)</para>
    /// </summary>
    property UInt32 IntegerInstructionCount
    {
        UInt32 get()
        {
            return intInstructionCount;
        }

        void set(UInt32 value)
        {
            intInstructionCount = value;
        }
    }
    /// <summary>
    /// Number of unsigned integer arithmetic instructions used.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.UintInstructionCount)</para>
    /// </summary>
    property UInt32 UnsignedIntegerInstructionCount
    {
        UInt32 get()
        {
            return uintInstructionCount;
        }

        void set(UInt32 value)
        {
            uintInstructionCount = value;
        }
    }
    /// <summary>
    /// Number of static flow control instructions used.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.StaticFlowControlCount)</para>
    /// </summary>
    property UInt32 StaticFlowControlCount
    {
        UInt32 get()
        {
            return staticFlowControlCount;
        }

        void set(UInt32 value)
        {
            staticFlowControlCount = value;
        }
    }
    /// <summary>
    /// Number of dynamic flow control instructions used.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.DynamicFlowControlCount)</para>
    /// </summary>
    property UInt32 DynamicFlowControlCount
    {
        UInt32 get()
        {
            return dynamicFlowControlCount;
        }

        void set(UInt32 value)
        {
            dynamicFlowControlCount = value;
        }
    }
    /// <summary>
    /// Number of macro instructions used.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.MacroInstructionCount)</para>
    /// </summary>
    property UInt32 MacroInstructionCount
    {
        UInt32 get()
        {
            return macroInstructionCount;
        }

        void set(UInt32 value)
        {
            macroInstructionCount = value;
        }
    }
    /// <summary>
    /// Number of array instructions used.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.ArrayInstructionCount)</para>
    /// </summary>
    property UInt32 ArrayInstructionCount
    {
        UInt32 get()
        {
            return arrayInstructionCount;
        }

        void set(UInt32 value)
        {
            arrayInstructionCount = value;
        }
    }
    /// <summary>
    /// Number of cut instructions used.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.CutInstructionCount)</para>
    /// </summary>
    property UInt32 CutInstructionCount
    {
        UInt32 get()
        {
            return cutInstructionCount;
        }

        void set(UInt32 value)
        {
            cutInstructionCount = value;
        }
    }
    /// <summary>
    /// Number of emit instructions used.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.EmitInstructionCount)</para>
    /// </summary>
    property UInt32 EmitInstructionCount
    {
        UInt32 get()
        {
            return emitInstructionCount;
        }

        void set(UInt32 value)
        {
            emitInstructionCount = value;
        }
    }
    /// <summary>
    /// Geometry shader output topology.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.GSOutputTopology)</para>
    /// </summary>
    property PrimitiveTopology GeometryShaderOutputTopology
    {
        PrimitiveTopology get()
        {
            return geometryShaderOutputTopology;
        }

        void set(PrimitiveTopology value)
        {
            geometryShaderOutputTopology = value;
        }
    }
    /// <summary>
    /// Geometry shader maximum output vertex count.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_DESC.GSMaxOutputVertexCount)</para>
    /// </summary>
    property UInt32 GeometryShaderMaxOutputVertexCount
    {
        UInt32 get()
        {
            return geometryShaderMaxOutputVertexCount;
        }

        void set(UInt32 value)
        {
            geometryShaderMaxOutputVertexCount = value;
        }
    }

private:

    UInt32 boundResources;
    UInt32 inputParameters;
    UInt32 outputParameters;
    UInt32 instructionCount;
    UInt32 tempRegisterCount;
    UInt32 tempArrayCount;
    UInt32 defCount;
    UInt32 dclCount;
    UInt32 textureNormalInstructions;
    UInt32 textureLoadInstructions;
    UInt32 textureCompInstructions;
    UInt32 textureBiasInstructions;
    UInt32 textureGradientInstructions;
    UInt32 floatInstructionCount;
    UInt32 intInstructionCount;
    UInt32 uintInstructionCount;
    UInt32 staticFlowControlCount;
    UInt32 dynamicFlowControlCount;
    UInt32 macroInstructionCount;
    UInt32 arrayInstructionCount;
    UInt32 cutInstructionCount;
    UInt32 emitInstructionCount;
    PrimitiveTopology geometryShaderOutputTopology;
    UInt32 geometryShaderMaxOutputVertexCount;

internal:
    ShaderDescription(const D3D10_SHADER_DESC & desc)
    {
        Version = desc.Version;
        Creator = desc.Creator != NULL ? gcnew String(desc.Creator) : nullptr;
        Options = desc.Flags;
		ConstantBuffers = desc.ConstantBuffers;
		BoundResources = desc.BoundResources;
		InputParameters = desc.InputParameters;
		OutputParameters = desc.OutputParameters;
		InstructionCount = desc.InstructionCount;
		TempRegisterCount = desc.TempRegisterCount;
		TempArrayCount = desc.TempArrayCount;
		DefCount = desc.DefCount;
		DeclarationCount = desc.DclCount;
		TextureNormalInstructions = desc.TextureNormalInstructions;
		TextureLoadInstructions = desc.TextureLoadInstructions;
		TextureCompInstructions = desc.TextureCompInstructions;
		TextureBiasInstructions = desc.TextureBiasInstructions;
		TextureGradientInstructions = desc.TextureGradientInstructions;
		FloatInstructionCount = desc.FloatInstructionCount;
		IntegerInstructionCount = desc.IntInstructionCount;
		UnsignedIntegerInstructionCount = desc.UintInstructionCount;
		StaticFlowControlCount = desc.StaticFlowControlCount;
		DynamicFlowControlCount = desc.DynamicFlowControlCount;
		MacroInstructionCount = desc.MacroInstructionCount;
		ArrayInstructionCount = desc.ArrayInstructionCount;
		CutInstructionCount = desc.CutInstructionCount;
		EmitInstructionCount = desc.EmitInstructionCount;
		GeometryShaderOutputTopology = static_cast<PrimitiveTopology>(desc.GSOutputTopology );
		GeometryShaderMaxOutputVertexCount = desc.GSMaxOutputVertexCount;
    }
private:

    UInt32 version;
    String^ creator;
    UInt32 flags;
    UInt32 constantBuffers;

public:

    static Boolean operator == (ShaderDescription shaderDescription1, ShaderDescription shaderDescription2)
    {
        return (shaderDescription1.boundResources == shaderDescription2.boundResources) &&
            (shaderDescription1.inputParameters == shaderDescription2.inputParameters) &&
            (shaderDescription1.outputParameters == shaderDescription2.outputParameters) &&
            (shaderDescription1.instructionCount == shaderDescription2.instructionCount) &&
            (shaderDescription1.tempRegisterCount == shaderDescription2.tempRegisterCount) &&
            (shaderDescription1.tempArrayCount == shaderDescription2.tempArrayCount) &&
            (shaderDescription1.defCount == shaderDescription2.defCount) &&
            (shaderDescription1.dclCount == shaderDescription2.dclCount) &&
            (shaderDescription1.textureNormalInstructions == shaderDescription2.textureNormalInstructions) &&
            (shaderDescription1.textureLoadInstructions == shaderDescription2.textureLoadInstructions) &&
            (shaderDescription1.textureCompInstructions == shaderDescription2.textureCompInstructions) &&
            (shaderDescription1.textureBiasInstructions == shaderDescription2.textureBiasInstructions) &&
            (shaderDescription1.textureGradientInstructions == shaderDescription2.textureGradientInstructions) &&
            (shaderDescription1.floatInstructionCount == shaderDescription2.floatInstructionCount) &&
            (shaderDescription1.intInstructionCount == shaderDescription2.intInstructionCount) &&
            (shaderDescription1.uintInstructionCount == shaderDescription2.uintInstructionCount) &&
            (shaderDescription1.staticFlowControlCount == shaderDescription2.staticFlowControlCount) &&
            (shaderDescription1.dynamicFlowControlCount == shaderDescription2.dynamicFlowControlCount) &&
            (shaderDescription1.macroInstructionCount == shaderDescription2.macroInstructionCount) &&
            (shaderDescription1.arrayInstructionCount == shaderDescription2.arrayInstructionCount) &&
            (shaderDescription1.cutInstructionCount == shaderDescription2.cutInstructionCount) &&
            (shaderDescription1.emitInstructionCount == shaderDescription2.emitInstructionCount) &&
            (shaderDescription1.geometryShaderOutputTopology == shaderDescription2.geometryShaderOutputTopology) &&
            (shaderDescription1.geometryShaderMaxOutputVertexCount == shaderDescription2.geometryShaderMaxOutputVertexCount) &&
            (shaderDescription1.version == shaderDescription2.version) &&
            (shaderDescription1.creator == shaderDescription2.creator) &&
            (shaderDescription1.flags == shaderDescription2.flags) &&
            (shaderDescription1.constantBuffers == shaderDescription2.constantBuffers);
    }

    static Boolean operator != (ShaderDescription shaderDescription1, ShaderDescription shaderDescription2)
    {
        return !(shaderDescription1 == shaderDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != ShaderDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<ShaderDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + boundResources.GetHashCode();
        hashCode = hashCode * 31 + inputParameters.GetHashCode();
        hashCode = hashCode * 31 + outputParameters.GetHashCode();
        hashCode = hashCode * 31 + instructionCount.GetHashCode();
        hashCode = hashCode * 31 + tempRegisterCount.GetHashCode();
        hashCode = hashCode * 31 + tempArrayCount.GetHashCode();
        hashCode = hashCode * 31 + defCount.GetHashCode();
        hashCode = hashCode * 31 + dclCount.GetHashCode();
        hashCode = hashCode * 31 + textureNormalInstructions.GetHashCode();
        hashCode = hashCode * 31 + textureLoadInstructions.GetHashCode();
        hashCode = hashCode * 31 + textureCompInstructions.GetHashCode();
        hashCode = hashCode * 31 + textureBiasInstructions.GetHashCode();
        hashCode = hashCode * 31 + textureGradientInstructions.GetHashCode();
        hashCode = hashCode * 31 + floatInstructionCount.GetHashCode();
        hashCode = hashCode * 31 + intInstructionCount.GetHashCode();
        hashCode = hashCode * 31 + uintInstructionCount.GetHashCode();
        hashCode = hashCode * 31 + staticFlowControlCount.GetHashCode();
        hashCode = hashCode * 31 + dynamicFlowControlCount.GetHashCode();
        hashCode = hashCode * 31 + macroInstructionCount.GetHashCode();
        hashCode = hashCode * 31 + arrayInstructionCount.GetHashCode();
        hashCode = hashCode * 31 + cutInstructionCount.GetHashCode();
        hashCode = hashCode * 31 + emitInstructionCount.GetHashCode();
        hashCode = hashCode * 31 + geometryShaderOutputTopology.GetHashCode();
        hashCode = hashCode * 31 + geometryShaderMaxOutputVertexCount.GetHashCode();
        hashCode = hashCode * 31 + version.GetHashCode();
        hashCode = hashCode * 31 + creator->GetHashCode();
        hashCode = hashCode * 31 + flags.GetHashCode();
        hashCode = hashCode * 31 + constantBuffers.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes how a shader resource is bound to a shader input.
/// <para>(Also see DirectX SDK: D3D10_SHADER_INPUT_BIND_DESC)</para>
/// </summary>
public value struct ShaderInputBindDescription
{
public:
    /// <summary>
    /// Name of the shader resource.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_INPUT_BIND_DESC.Name)</para>
    /// </summary>
    property String^ Name
    {
        String^ get()
        {
            return name;
        }

        void set(String^ value)
        {
            name = value;
        }
    }
    /// <summary>
    /// Identifies the type of data in the resource. See ShaderInputType.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_INPUT_BIND_DESC.Type)</para>
    /// </summary>
    property ShaderInputType ShaderInputType
    {
        Direct3D10::ShaderInputType get()
        {
            return type;
        }

        void set(Direct3D10::ShaderInputType value)
        {
            type = value;
        }
    }
    /// <summary>
    /// Starting bind point.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_INPUT_BIND_DESC.BindPoint)</para>
    /// </summary>
    property UInt32 BindPoint
    {
        UInt32 get()
        {
            return bindPoint;
        }

        void set(UInt32 value)
        {
            bindPoint = value;
        }
    }
    /// <summary>
    /// Number of contiguous bind points for arrays.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_INPUT_BIND_DESC.BindCount)</para>
    /// </summary>
    property UInt32 BindCount
    {
        UInt32 get()
        {
            return bindCount;
        }

        void set(UInt32 value)
        {
            bindCount = value;
        }
    }
    /// <summary>
    /// Shader input-parameter options. See ShaderInputOptions.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_INPUT_BIND_DESC.uFlags)</para>
    /// </summary>
    property ShaderInputOptions ShaderInputOptions
    {
        Direct3D10::ShaderInputOptions get()
        {
            return flags;
        }

        void set(Direct3D10::ShaderInputOptions value)
        {
            flags = value;
        }
    }
    /// <summary>
    /// If the input is a texture, the return type. See ResourceReturnType.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_INPUT_BIND_DESC.ReturnType)</para>
    /// </summary>
    property ResourceReturnType ReturnType
    {
        ResourceReturnType get()
        {
            return returnType;
        }

        void set(ResourceReturnType value)
        {
            returnType = value;
        }
    }
    /// <summary>
    /// Identifies the amount of data in the resource. See ShaderResourceViewDimension.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_INPUT_BIND_DESC.Dimension)</para>
    /// </summary>
    property ShaderResourceViewDimension Dimension
    {
        ShaderResourceViewDimension get()
        {
            return dimension;
        }

        void set(ShaderResourceViewDimension value)
        {
            dimension = value;
        }
    }
    /// <summary>
    /// The number of samples for a multisampled texture; otherwise 0.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_INPUT_BIND_DESC.NumSamples)</para>
    /// </summary>
    property UInt32 SampleCount
    {
        UInt32 get()
        {
            return numSamples;
        }

        void set(UInt32 value)
        {
            numSamples = value;
        }
    }
private:

    String^ name;
    Direct3D10::ShaderInputType type;
    UInt32 bindPoint;
    UInt32 bindCount;
    Direct3D10::ShaderInputOptions flags;
    ResourceReturnType returnType;
    ShaderResourceViewDimension dimension;
    UInt32 numSamples;

internal:
    ShaderInputBindDescription(const D3D10_SHADER_INPUT_BIND_DESC & desc)
    {
        Name = desc.Name ? gcnew String(desc.Name) : nullptr;
        ShaderInputType = static_cast<Direct3D10::ShaderInputType>(desc.Type);
        BindCount = desc.BindCount;
        BindPoint = desc.BindPoint;
        ShaderInputOptions = static_cast<Direct3D10::ShaderInputOptions>(desc.uFlags);
        ReturnType = static_cast<ResourceReturnType>(desc.ReturnType);
        Dimension = static_cast<ShaderResourceViewDimension>(desc.Dimension);
        SampleCount = desc.NumSamples;
    }
public:

    static Boolean operator == (ShaderInputBindDescription shaderInputBindDescription1, ShaderInputBindDescription shaderInputBindDescription2)
    {
        return (shaderInputBindDescription1.name == shaderInputBindDescription2.name) &&
            (shaderInputBindDescription1.type == shaderInputBindDescription2.type) &&
            (shaderInputBindDescription1.bindPoint == shaderInputBindDescription2.bindPoint) &&
            (shaderInputBindDescription1.bindCount == shaderInputBindDescription2.bindCount) &&
            (shaderInputBindDescription1.flags == shaderInputBindDescription2.flags) &&
            (shaderInputBindDescription1.returnType == shaderInputBindDescription2.returnType) &&
            (shaderInputBindDescription1.dimension == shaderInputBindDescription2.dimension) &&
            (shaderInputBindDescription1.numSamples == shaderInputBindDescription2.numSamples);
    }

    static Boolean operator != (ShaderInputBindDescription shaderInputBindDescription1, ShaderInputBindDescription shaderInputBindDescription2)
    {
        return !(shaderInputBindDescription1 == shaderInputBindDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != ShaderInputBindDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<ShaderInputBindDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + name->GetHashCode();
        hashCode = hashCode * 31 + type.GetHashCode();
        hashCode = hashCode * 31 + bindPoint.GetHashCode();
        hashCode = hashCode * 31 + bindCount.GetHashCode();
        hashCode = hashCode * 31 + flags.GetHashCode();
        hashCode = hashCode * 31 + returnType.GetHashCode();
        hashCode = hashCode * 31 + dimension.GetHashCode();
        hashCode = hashCode * 31 + numSamples.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a shader-resource view.
/// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC)</para>
/// </summary>
[StructLayout(LayoutKind::Explicit)]
public value struct ShaderResourceViewDescription
{
public:
    /// <summary>
    /// The viewing format.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.Format)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.ViewDimension)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.Buffer)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.Texture1D)</para>
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
    /// View the resource as a 1D-texture array using information from a shader-resource view (see Texture1DArrayShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.Texture1DArray)</para>
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
    /// View the resource as a 2D-texture using information from a shader-resource view (see Texture2DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.Texture2D)</para>
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
    /// View the resource as a 2D-texture array using information from a shader-resource view (see Texture2DArrayShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.Texture2DArray)</para>
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
    /// View the resource as a 2D-multisampled texture using information from a shader-resource view (see Texture2DMultisampleShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.Texture2DMultisample)</para>
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
    /// View the resource as a 2D-multisampled-texture array using information from a shader-resource view (see Texture2DMultisampleArrayShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.Texture2DMSArray)</para>
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
    /// View the resource as a 3D texture using information from a shader-resource view (see Texture3DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.Texture3D)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC.TextureCube)</para>
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

internal:
    ShaderResourceViewDescription(const D3D10_SHADER_RESOURCE_VIEW_DESC& desc)
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
        default :
              {
                  throw gcnew NotSupportedException("Unknown or not supported ViewDimension.");
              }
        }
    }

    void CopyTo(D3D10_SHADER_RESOURCE_VIEW_DESC* desc)
    {
        desc->Format = static_cast<DXGI_FORMAT>(Format);
        desc->ViewDimension = static_cast<D3D10_SRV_DIMENSION>(ViewDimension);

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
        default:
              {
                  throw gcnew NotSupportedException("Unknown or not supported ViewDimension.");
              }
        }
    }


};

/// <summary>
/// Describes a shader-resource view.
/// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1)</para>
/// </summary>
[StructLayout(LayoutKind::Explicit)]
public value struct ShaderResourceViewDescription1
{
public:
    /// <summary>
    /// The viewing format.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.Format)</para>
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
    /// The resource type of the view. See ShaderResourceViewDimension1. This should be the same as the resource type of the underlying resource. This parameter also determines which _SRV to use in the union below.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.ViewDimension)</para>
    /// </summary>
    property ShaderResourceViewDimension1 ViewDimension
    {
        ShaderResourceViewDimension1 get()
        {
            return viewDimension;
        }

        void set(ShaderResourceViewDimension1 value)
        {
            viewDimension = value;
        }
    }
    /// <summary>
    /// View the resource as a buffer using information from a shader-resource view (see <see cref="BufferShaderResourceView"/>)<seealso cref="BufferShaderResourceView"/>.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.Buffer)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.Texture1D)</para>
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
    /// View the resource as a 1D-texture array using information from a shader-resource view (see Texture1DArrayShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.Texture1DArray)</para>
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
    /// View the resource as a 2D-texture using information from a shader-resource view (see Texture2DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.Texture2D)</para>
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
    /// View the resource as a 2D-texture array using information from a shader-resource view (see Texture2DArrayShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.Texture2DArray)</para>
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
    /// View the resource as a 2D-multisampled texture using information from a shader-resource view (see Texture2DMultisampleShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.Texture2DMultisample)</para>
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
    /// View the resource as a 2D-multisampled-texture array using information from a shader-resource view (see Texture2DMultisampleArrayShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.Texture2DMSArray)</para>
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
    /// View the resource as a 3D texture using information from a shader-resource view (see Texture3DShaderResourceView.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.Texture3D)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.TextureCube)</para>
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
    /// View the resource as an array of cube textures using information from a shader-resource view (see <see cref="TextureCubeArrayShaderResourceView1"/>)<seealso cref="TextureCubeArrayShaderResourceView1"/>.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_RESOURCE_VIEW_DESC1.TextureCubeArray)</para>
    /// </summary>
    property TextureCubeArrayShaderResourceView1 TextureCubeArray
    {
        TextureCubeArrayShaderResourceView1 get()
        {
            return textureCubeArray;
        }

        void set(TextureCubeArrayShaderResourceView1 value)
        {
            textureCubeArray = value;
        }
    }
private:

    [FieldOffset(0)]
    Graphics::Format format;
    [FieldOffset(4)]
    ShaderResourceViewDimension1 viewDimension;
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
    TextureCubeArrayShaderResourceView1 textureCubeArray;

internal:
    ShaderResourceViewDescription1(const D3D10_SHADER_RESOURCE_VIEW_DESC1& desc)
    {
        Format = static_cast<Graphics::Format>(desc.Format);
        ViewDimension = static_cast<ShaderResourceViewDimension1>(desc.ViewDimension);

        switch (ViewDimension)
        {
        case ShaderResourceViewDimension1::Buffer :
               {
                   BufferShaderResourceView buffer;

                   buffer.ElementOffset = desc.Buffer.ElementOffset;
                   buffer.ElementWidth = desc.Buffer.ElementWidth;

                   Buffer = buffer;
                   break;
               }
        case ShaderResourceViewDimension1::Texture1D :
              {
                  Texture1DShaderResourceView texture1D;

                  texture1D.MipLevels = desc.Texture1D.MipLevels;
                  texture1D.MostDetailedMip = desc.Texture1D.MostDetailedMip;

                  Texture1D = texture1D;
                  break;
              }
        case ShaderResourceViewDimension1::Texture1DArray :
              {
                  Texture1DArrayShaderResourceView texture1DArray;

                  texture1DArray.ArraySize = desc.Texture1DArray.ArraySize;
                  texture1DArray.FirstArraySlice = desc.Texture1DArray.FirstArraySlice;
                  texture1DArray.MipLevels = desc.Texture1DArray.MipLevels;
                  texture1DArray.MostDetailedMip = desc.Texture1DArray.MostDetailedMip;

                  Texture1DArray = texture1DArray;
                  break;
              }
        case ShaderResourceViewDimension1::Texture2D :
              {
                  Texture2DShaderResourceView texture2D;

                  texture2D.MipLevels = desc.Texture2D.MipLevels;
                  texture2D.MostDetailedMip = desc.Texture2D.MostDetailedMip;

                  Texture2D = texture2D;
                  break;
              }
        case ShaderResourceViewDimension1::Texture2DArray :
              {
                  Texture2DArrayShaderResourceView texture2DArray;

                  texture2DArray.ArraySize = desc.Texture2DArray.ArraySize;
                  texture2DArray.FirstArraySlice = desc.Texture2DArray.FirstArraySlice;
                  texture2DArray.MipLevels = desc.Texture2DArray.MipLevels;
                  texture2DArray.MostDetailedMip = desc.Texture2DArray.MostDetailedMip;

                  Texture2DArray = texture2DArray;
                  break;
              }
        case ShaderResourceViewDimension1::Texture2DMultisample :
              {
                  Texture2DMultisampleShaderResourceView texture2DMultisample;

                  texture2DMultisample.UnusedField = desc.Texture2DMS.UnusedField_NothingToDefine;

                  Texture2DMultisample = texture2DMultisample;
                  break;
              }
        case ShaderResourceViewDimension1::Texture2DMultisampleArray :
              {
                  Texture2DMultisampleArrayShaderResourceView texture2DMultisampleArray;

                  texture2DMultisampleArray.ArraySize = desc.Texture2DMSArray.ArraySize;
                  texture2DMultisampleArray.FirstArraySlice = desc.Texture2DMSArray.FirstArraySlice;

                  Texture2DMultisampleArray = texture2DMultisampleArray;
                  break;
              }
        case ShaderResourceViewDimension1::Texture3D :
              {
                  Texture3DShaderResourceView texture3D;

                  texture3D.MipLevels = desc.Texture3D.MipLevels;
                  texture3D.MostDetailedMip = desc.Texture3D.MostDetailedMip;

                  Texture3D = texture3D;
                  break;
              }
        case ShaderResourceViewDimension1::TextureCube :
              {
                  TextureCubeShaderResourceView textureCube;

                  textureCube.MipLevels = desc.TextureCube.MipLevels;
                  textureCube.MostDetailedMip = desc.TextureCube.MostDetailedMip;

                  TextureCube = textureCube;
                  break;
              }
        case ShaderResourceViewDimension1::TextureCubeArray :
              {
                  TextureCubeArrayShaderResourceView1 textureCubeArray;

                  textureCubeArray.First2DArrayFace = desc.TextureCubeArray.First2DArrayFace;
                  textureCubeArray.MipLevels = desc.TextureCubeArray.MipLevels;
                  textureCubeArray.MostDetailedMip = desc.TextureCubeArray.MostDetailedMip;
                  textureCubeArray.CubeCount = desc.TextureCubeArray.NumCubes;

                  TextureCubeArray = textureCubeArray;
                  break;
              }
        default:
              {
                  throw gcnew NotSupportedException("Unknown or not supported ViewDimension.");
              }
        }
    }

    void CopyTo(D3D10_SHADER_RESOURCE_VIEW_DESC1* desc)
    {
        desc->Format = static_cast<DXGI_FORMAT>(Format);
        desc->ViewDimension = static_cast<D3D10_SRV_DIMENSION1>(ViewDimension);

        switch (ViewDimension)
        {
        case ShaderResourceViewDimension1::Buffer :
               {
                   desc->Buffer.ElementOffset = Buffer.ElementOffset;
                   desc->Buffer.ElementWidth = Buffer.ElementWidth;
                   break;
               }
        case ShaderResourceViewDimension1::Texture1D :
              {
                  desc->Texture1D.MipLevels = Texture1D.MipLevels;
                  desc->Texture1D.MostDetailedMip = Texture1D.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension1::Texture1DArray :
              {
                  desc->Texture1DArray.ArraySize = Texture1DArray.ArraySize;
                  desc->Texture1DArray.FirstArraySlice = Texture1DArray.FirstArraySlice;
                  desc->Texture1DArray.MipLevels = Texture1DArray.MipLevels;
                  desc->Texture1DArray.MostDetailedMip = Texture1DArray.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension1::Texture2D :
              {
                  desc->Texture2D.MipLevels = Texture2D.MipLevels;
                  desc->Texture2D.MostDetailedMip = Texture2D.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension1::Texture2DArray :
              {
                  desc->Texture2DArray.ArraySize = Texture2DArray.ArraySize;
                  desc->Texture2DArray.FirstArraySlice = Texture2DArray.FirstArraySlice;
                  desc->Texture2DArray.MipLevels = Texture2DArray.MipLevels;
                  desc->Texture2DArray.MostDetailedMip = Texture2DArray.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension1::Texture2DMultisample :
              {
                  desc->Texture2DMS.UnusedField_NothingToDefine = Texture2DMultisample.UnusedField;
                  break;
              }
        case ShaderResourceViewDimension1::Texture2DMultisampleArray :
              {
                  desc->Texture2DMSArray.ArraySize = Texture2DMultisampleArray.ArraySize;
                  desc->Texture2DMSArray.FirstArraySlice = Texture2DMultisampleArray.FirstArraySlice;
                  break;
              }
        case ShaderResourceViewDimension1::Texture3D :
              {
                  desc->Texture3D.MipLevels = Texture3D.MipLevels;
                  desc->Texture3D.MostDetailedMip = Texture3D.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension1::TextureCube :
              {
                  desc->TextureCube.MipLevels = TextureCube.MipLevels;
                  desc->TextureCube.MostDetailedMip = TextureCube.MostDetailedMip;
                  break;
              }
        case ShaderResourceViewDimension1::TextureCubeArray :
              {
                  desc->TextureCubeArray.First2DArrayFace = TextureCubeArray.First2DArrayFace;
                  desc->TextureCubeArray.MipLevels = TextureCubeArray.MipLevels;
                  desc->TextureCubeArray.MostDetailedMip = TextureCubeArray.MostDetailedMip;
                  desc->TextureCubeArray.NumCubes = TextureCubeArray.CubeCount;
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
/// Describes a shader-variable type.
/// <para>(Also see DirectX SDK: D3D10_SHADER_TYPE_DESC)</para>
/// </summary>
public value struct ShaderTypeDescription
{
public:
    /// <summary>
    /// Identifies the variable class as one of scalar, vector, matrix or object. See ShaderVariableClass.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_TYPE_DESC.Class)</para>
    /// </summary>
    property ShaderVariableClass Class
    {
        ShaderVariableClass get()
        {
            return shaderVariableClass;
        }

        void set(ShaderVariableClass value)
        {
            shaderVariableClass = value;
        }
    }
    /// <summary>
    /// The variable type. See ShaderVariableType.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_TYPE_DESC.Type)</para>
    /// </summary>
    property ShaderVariableType ShaderVariableType
    {
        Direct3D10::ShaderVariableType get()
        {
            return type;
        }

        void set(Direct3D10::ShaderVariableType value)
        {
            type = value;
        }
    }
    /// <summary>
    /// Number of rows in a matrix. Otherwise a numeric type returns 1, any other type returns 0.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_TYPE_DESC.Rows)</para>
    /// </summary>
    property UInt32 Rows
    {
        UInt32 get()
        {
            return rows;
        }

        void set(UInt32 value)
        {
            rows = value;
        }
    }
    /// <summary>
    /// Number of columns in a matrix. Otherwise a numeric type returns 1, any other type returns 0.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_TYPE_DESC.Columns)</para>
    /// </summary>
    property UInt32 Columns
    {
        UInt32 get()
        {
            return columns;
        }

        void set(UInt32 value)
        {
            columns = value;
        }
    }
    /// <summary>
    /// Number of elements in an array; otherwise 0.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_TYPE_DESC.Elements)</para>
    /// </summary>
    property UInt32 Elements
    {
        UInt32 get()
        {
            return elements;
        }

        void set(UInt32 value)
        {
            elements = value;
        }
    }
    /// <summary>
    /// Number of members in the structure; otherwise 0.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_TYPE_DESC.Members)</para>
    /// </summary>
    property UInt32 Members
    {
        UInt32 get()
        {
            return members;
        }

        void set(UInt32 value)
        {
            members = value;
        }
    }
    /// <summary>
    /// Offset, in bytes, between the start of the parent structure and this variable.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_TYPE_DESC.Offset)</para>
    /// </summary>
    property UInt32 Offset
    {
        UInt32 get()
        {
            return offset;
        }

        void set(UInt32 value)
        {
            offset = value;
        }
    }
private:

    ShaderVariableClass shaderVariableClass;
    Direct3D10::ShaderVariableType type;
    UInt32 rows;
    UInt32 columns;
    UInt32 elements;
    UInt32 members;
    UInt32 offset;

public:

    static Boolean operator == (ShaderTypeDescription shaderTypeDescription1, ShaderTypeDescription shaderTypeDescription2)
    {
        return (shaderTypeDescription1.shaderVariableClass == shaderTypeDescription2.shaderVariableClass) &&
            (shaderTypeDescription1.type == shaderTypeDescription2.type) &&
            (shaderTypeDescription1.rows == shaderTypeDescription2.rows) &&
            (shaderTypeDescription1.columns == shaderTypeDescription2.columns) &&
            (shaderTypeDescription1.elements == shaderTypeDescription2.elements) &&
            (shaderTypeDescription1.members == shaderTypeDescription2.members) &&
            (shaderTypeDescription1.offset == shaderTypeDescription2.offset);
    }

    static Boolean operator != (ShaderTypeDescription shaderTypeDescription1, ShaderTypeDescription shaderTypeDescription2)
    {
        return !(shaderTypeDescription1 == shaderTypeDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != ShaderTypeDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<ShaderTypeDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + shaderVariableClass.GetHashCode();
        hashCode = hashCode * 31 + type.GetHashCode();
        hashCode = hashCode * 31 + rows.GetHashCode();
        hashCode = hashCode * 31 + columns.GetHashCode();
        hashCode = hashCode * 31 + elements.GetHashCode();
        hashCode = hashCode * 31 + members.GetHashCode();
        hashCode = hashCode * 31 + offset.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Describes a shader variable.
/// <para>(Also see DirectX SDK: D3D10_SHADER_VARIABLE_DESC)</para>
/// </summary>
public value struct ShaderVariableDescription
{
public:
    /// <summary>
    /// The variable name.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_VARIABLE_DESC.Name)</para>
    /// </summary>
    property String^ Name
    {
        String^ get()
        {
            return name;
        }

        void set(String^ value)
        {
            name = value;
        }
    }
    /// <summary>
    /// Offset from the start of the parent structure, to the beginning of the variable.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_VARIABLE_DESC.StartOffset)</para>
    /// </summary>
    property UInt32 StartOffset
    {
        UInt32 get()
        {
            return startOffset;
        }

        void set(UInt32 value)
        {
            startOffset = value;
        }
    }
    /// <summary>
    /// Size of the variable (in bytes).
    /// <para>(Also see DirectX SDK: D3D10_SHADER_VARIABLE_DESC.Size)</para>
    /// </summary>
    property UInt32 Size
    {
        UInt32 get()
        {
            return size;
        }

        void set(UInt32 value)
        {
            size = value;
        }
    }
    /// <summary>
    /// Flags, which identify shader-variable properties (see <see cref="ShaderVariableProperties"/>)<seealso cref="ShaderVariableProperties"/>.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_VARIABLE_DESC.uFlags)</para>
    /// </summary>
    property ShaderVariableProperties ShaderVariableProperties
    {
        Direct3D10::ShaderVariableProperties get()
        {
            return flags;
        }

        void set(Direct3D10::ShaderVariableProperties value)
        {
            flags = value;
        }
    }
    /// <summary>
    /// The default value for initializing the variable.
    /// <para>(Also see DirectX SDK: D3D10_SHADER_VARIABLE_DESC.DefaultValue)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ DefaultValue
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            return Array::AsReadOnly(defaultValue);
        }
    }

    // This setter method has been split from the property, because a common use
    // case for the data returned from the property is random, indexed access,
    // something supported by an IList<T> implementation like ReadOnlyCollection<T>.
    // But we don't want to _require_ the client code to have to provide a
    // ReadOnlyCollection<T> instance, or even an IList<T> instance.

	void SetDefaultValue(IEnumerable<unsigned char>^ newDefaultValue)
	{
		if (Enumerable::Count(newDefaultValue) != defaultValue->Length)
		{
			throw gcnew ArgumentException("Length of new data must be same as current data", "newDefaultValue");
		}

		int ib = 0;

		for each (unsigned char b in newDefaultValue)
		{
			defaultValue[ib++] = b;
		}
	}

private:

    array<unsigned char>^ defaultValue;

    UInt32 size;
    Direct3D10::ShaderVariableProperties flags;

internal:
    ShaderVariableDescription(const D3D10_SHADER_VARIABLE_DESC & desc)
    {
        Name = desc.Name ? gcnew String(desc.Name) : nullptr;
        StartOffset = desc.StartOffset;
        Size = desc.Size;
        ShaderVariableProperties = static_cast<Direct3D10::ShaderVariableProperties>(desc.uFlags);
        if (desc.Size > 0)
        {
            defaultValue = gcnew array<unsigned char>(desc.Size);
            pin_ptr<unsigned char> ptr = &defaultValue[0];
            memcpy(ptr, desc.DefaultValue, desc.Size);
        }
    }
private:

    String^ name;
    UInt32 startOffset;

};

/// <summary>
/// Describes a shader signature.
/// <para>(Also see DirectX SDK: D3D10_SIGNATURE_PARAMETER_DESC)</para>
/// </summary>
public value struct SignatureParameterDescription
{
public:
    /// <summary>
    /// A per-parameter string that identifies how the data will be used. See Semantics (DirectX HLSL).
    /// <para>(Also see DirectX SDK: D3D10_SIGNATURE_PARAMETER_DESC.SemanticName)</para>
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
    /// Semantic index that modifies the semantic. Used to differentiate different parameters that use the same semantic.
    /// <para>(Also see DirectX SDK: D3D10_SIGNATURE_PARAMETER_DESC.SemanticIndex)</para>
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
    /// The register that will contain this variable's data.
    /// <para>(Also see DirectX SDK: D3D10_SIGNATURE_PARAMETER_DESC.Register)</para>
    /// </summary>
    property UInt32 RegisterNumber
    {
        UInt32 get()
        {
            return registerNumber;
        }

        void set(UInt32 value)
        {
            registerNumber = value;
        }
    }
    /// <summary>
    /// A predefined string that determines the functionality of certain pipeline stages. See Name.
    /// <para>(Also see DirectX SDK: D3D10_SIGNATURE_PARAMETER_DESC.SystemValueType)</para>
    /// </summary>
    property Name SystemValueType
    {
        Name get()
        {
            return systemValueType;
        }

        void set(Name value)
        {
            systemValueType = value;
        }
    }
    /// <summary>
    /// The per-component-data type that is stored in a register. See RegisterComponentType. Each register can store up to four-components of data.
    /// <para>(Also see DirectX SDK: D3D10_SIGNATURE_PARAMETER_DESC.ComponentType)</para>
    /// </summary>
    property RegisterComponentType ComponentType
    {
        RegisterComponentType get()
        {
            return componentType;
        }

        void set(RegisterComponentType value)
        {
            componentType = value;
        }
    }
    /// <summary>
    /// Mask which indicates which components of a register are used.
    /// <para>(Also see DirectX SDK: D3D10_SIGNATURE_PARAMETER_DESC.Mask)</para>
    /// </summary>
    property unsigned char Mask
    {
        unsigned char get()
        {
            return mask;
        }

        void set(unsigned char value)
        {
            mask = value;
        }
    }
    /// <summary>
    /// Mask which indicates whether a given component is never written (if the signature is an output signature) or always read (if the signature is an input signature). 
    /// The mask is a combination of RegisterComponentType values.
    /// <para>(Also see DirectX SDK: D3D10_SIGNATURE_PARAMETER_DESC.ReadWriteMask)</para>
    /// </summary>
    property unsigned char ReadWriteMask
    {
        unsigned char get()
        {
            return readWriteMask;
        }

        void set(unsigned char value)
        {
            readWriteMask = value;
        }
    }
private:

    unsigned char mask;
    unsigned char readWriteMask;

    String^ semanticName;
    UInt32 semanticIndex;
    UInt32 registerNumber;
    Name systemValueType;
    RegisterComponentType componentType;

internal:
    SignatureParameterDescription(const D3D10_SIGNATURE_PARAMETER_DESC& desc)
    {
        SemanticName = desc.SemanticName ? gcnew String(desc.SemanticName) : nullptr;
        SemanticIndex = desc.SemanticIndex;
        RegisterNumber = desc.Register;
        SystemValueType = static_cast<Name>(desc.SystemValueType);
        ComponentType = static_cast<RegisterComponentType>(desc.ComponentType);
        Mask = desc.Mask;
        ReadWriteMask = desc.ReadWriteMask;
    }
public:

    static Boolean operator == (SignatureParameterDescription signatureParameterDescription1, SignatureParameterDescription signatureParameterDescription2)
    {
        return (signatureParameterDescription1.mask == signatureParameterDescription2.mask) &&
            (signatureParameterDescription1.readWriteMask == signatureParameterDescription2.readWriteMask) &&
            (signatureParameterDescription1.semanticName == signatureParameterDescription2.semanticName) &&
            (signatureParameterDescription1.semanticIndex == signatureParameterDescription2.semanticIndex) &&
            (signatureParameterDescription1.registerNumber == signatureParameterDescription2.registerNumber) &&
            (signatureParameterDescription1.systemValueType == signatureParameterDescription2.systemValueType) &&
            (signatureParameterDescription1.componentType == signatureParameterDescription2.componentType);
    }

    static Boolean operator != (SignatureParameterDescription signatureParameterDescription1, SignatureParameterDescription signatureParameterDescription2)
    {
        return !(signatureParameterDescription1 == signatureParameterDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != SignatureParameterDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<SignatureParameterDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + mask.GetHashCode();
        hashCode = hashCode * 31 + readWriteMask.GetHashCode();
        hashCode = hashCode * 31 + semanticName->GetHashCode();
        hashCode = hashCode * 31 + semanticIndex.GetHashCode();
        hashCode = hashCode * 31 + registerNumber.GetHashCode();
        hashCode = hashCode * 31 + systemValueType.GetHashCode();
        hashCode = hashCode * 31 + componentType.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Description of a vertex element in a vertex buffer in an output slot.
/// <para>(Also see DirectX SDK: D3D10_SO_DECLARATION_ENTRY)</para>
/// </summary>
public value struct StreamOutputDeclarationEntry
{
public:
    /// <summary>
    /// Type of output element.  Possible values: "POSITION", "NORMAL", or "TEXCOORD0".
    /// <para>(Also see DirectX SDK: D3D10_SO_DECLARATION_ENTRY.SemanticName)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SO_DECLARATION_ENTRY.SemanticIndex)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SO_DECLARATION_ENTRY.StartComponent)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SO_DECLARATION_ENTRY.ComponentCount)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SO_DECLARATION_ENTRY.OutputSlot)</para>
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

    String^ semanticName;
    UInt32 semanticIndex;
    Byte startComponent;
    Byte componentCount;
    Byte outputSlot;

internal:
    StreamOutputDeclarationEntry(const D3D10_SO_DECLARATION_ENTRY& entry)
    {
        SemanticIndex = entry.SemanticIndex;
        StartComponent = entry.StartComponent;
        ComponentCount = entry.ComponentCount;
        OutputSlot = entry.OutputSlot;
        SemanticName = gcnew String(entry.SemanticName);
    }

    void CopyTo(D3D10_SO_DECLARATION_ENTRY * entry, marshal_context^ context)
    {
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
        return (streamOutputDeclarationEntry1.semanticName == streamOutputDeclarationEntry2.semanticName) &&
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
/// <para>(Also see DirectX SDK: D3D10_SUBRESOURCE_DATA)</para>
/// </summary>
public value struct SubresourceData
{
public:
    /// <summary>
    /// Pointer to the initialization data.
    /// <para>(Also see DirectX SDK: D3D10_SUBRESOURCE_DATA.pSysMem)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SUBRESOURCE_DATA.SysMemPitch)</para>
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
    /// <para>(Also see DirectX SDK: D3D10_SUBRESOURCE_DATA.SysMemSlicePitch)</para>
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
    SubresourceData(const D3D10_SUBRESOURCE_DATA & subresourceData)
    {
        SystemMemory = IntPtr((void*)subresourceData.pSysMem);
        SystemMemoryPitch = subresourceData.SysMemPitch;
        SystemMemorySlicePitch = subresourceData.SysMemSlicePitch;
    }
    void CopyTo(D3D10_SUBRESOURCE_DATA &subresourceData)
    {
        subresourceData.pSysMem = SystemMemory.ToPointer();
        subresourceData.SysMemPitch = SystemMemoryPitch;
        subresourceData.SysMemSlicePitch = SystemMemorySlicePitch;
    }
private:

    IntPtr sysMem;

};

/// <summary>
/// Describes an effect technique.
/// <para>(Also see DirectX SDK: D3D10_TECHNIQUE_DESC)</para>
/// </summary>
public value struct TechniqueDescription
{
public:
    /// <summary>
    /// A string that contains the technique name; otherwise NULL.
    /// <para>(Also see DirectX SDK: D3D10_TECHNIQUE_DESC.Name)</para>
    /// </summary>
    property String^ Name
    {
        String^ get()
        {
            return name;
        }

        void set(String^ value)
        {
            name = value;
        }
    }
    /// <summary>
    /// The number of passes in the technique.
    /// <para>(Also see DirectX SDK: D3D10_TECHNIQUE_DESC.Passes)</para>
    /// </summary>
    property UInt32 Passes
    {
        UInt32 get()
        {
            return passes;
        }

        void set(UInt32 value)
        {
            passes = value;
        }
    }
    /// <summary>
    /// The number of annotations.
    /// <para>(Also see DirectX SDK: D3D10_TECHNIQUE_DESC.Annotations)</para>
    /// </summary>
    property UInt32 Annotations
    {
        UInt32 get()
        {
            return annotations;
        }

        void set(UInt32 value)
        {
            annotations = value;
        }
    }

private:

    String^ name;
    UInt32 passes;
    UInt32 annotations;

internal:
    TechniqueDescription(const D3D10_TECHNIQUE_DESC & desc)
    {
        Annotations = desc.Annotations;
        Passes = desc.Passes;

        Name = desc.Name ? gcnew String(desc.Name) : nullptr;
    }

    void CopyTo(D3D10_TECHNIQUE_DESC* desc, marshal_context^ context)
    {
        desc->Annotations = Annotations;
        desc->Passes = Passes;

        String^ tempName = Name;
        desc->Name = Name == nullptr ? NULL : context->marshal_as<const char*>(tempName);
    }
public:

    static Boolean operator == (TechniqueDescription techniqueDescription1, TechniqueDescription techniqueDescription2)
    {
        return (techniqueDescription1.name == techniqueDescription2.name) &&
            (techniqueDescription1.passes == techniqueDescription2.passes) &&
            (techniqueDescription1.annotations == techniqueDescription2.annotations);
    }

    static Boolean operator != (TechniqueDescription techniqueDescription1, TechniqueDescription techniqueDescription2)
    {
        return !(techniqueDescription1 == techniqueDescription2);
    }

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj->GetType() != TechniqueDescription::typeid)
        {
            return false;
        }

        return *this == safe_cast<TechniqueDescription>(obj);
    }

    virtual int GetHashCode(void) override
    {
        int hashCode = 0;

        hashCode = hashCode * 31 + name->GetHashCode();
        hashCode = hashCode * 31 + passes.GetHashCode();
        hashCode = hashCode * 31 + annotations.GetHashCode();

        return hashCode;
    }

};

/// <summary>
/// Defines the dimensions of a viewport.
/// <para>(Also see DirectX SDK: D3D10_VIEWPORT)</para>
/// </summary>
public value struct Viewport
{
public:
    /// <summary>
    /// X position of the left hand side of the viewport. Must be between -16384 and 16383.
    /// <para>(Also see DirectX SDK: D3D10_VIEWPORT.TopLeftX)</para>
    /// </summary>
    property Int32 TopLeftX
    {
        Int32 get()
        {
            return topLeftX;
        }

        void set(Int32 value)
        {
            topLeftX = value;
        }
    }
    /// <summary>
    /// Y position of the top of the viewport. Must be between -16384 and 16383.
    /// <para>(Also see DirectX SDK: D3D10_VIEWPORT.TopLeftY)</para>
    /// </summary>
    property Int32 TopLeftY
    {
        Int32 get()
        {
            return topLeftY;
        }

        void set(Int32 value)
        {
            topLeftY = value;
        }
    }
    /// <summary>
    /// Width of the viewport. Must be between 0 and 16383.
    /// <para>(Also see DirectX SDK: D3D10_VIEWPORT.Width)</para>
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
    /// Height of the viewport. Must be between 0 and 16383.
    /// <para>(Also see DirectX SDK: D3D10_VIEWPORT.Height)</para>
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
    /// Minimum depth of the viewport. Must be between 0 and 1.
    /// <para>(Also see DirectX SDK: D3D10_VIEWPORT.MinDepth)</para>
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
    /// Maximum depth of the viewport. Must be between 0 and 1.
    /// <para>(Also see DirectX SDK: D3D10_VIEWPORT.MaxDepth)</para>
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

    Int32 topLeftX;
    Int32 topLeftY;
    UInt32 width;
    UInt32 height;
    Single minDepth;
    Single maxDepth;

public:
    static Boolean operator == ( Viewport viewport1, Viewport viewport2 )
    {
        return 
            (viewport1.Height == viewport2.Height)  &&
            (viewport1.MaxDepth == viewport2.MaxDepth)  &&
            (viewport1.MinDepth == viewport2.MinDepth)  &&
            (viewport1.TopLeftX == viewport2.TopLeftX)  &&
            (viewport1.TopLeftY == viewport2.TopLeftY)  &&
            (viewport1.Width == viewport2.Width);
    }

    static Boolean operator != ( Viewport viewport1, Viewport viewport2 )
    {
        return !(viewport1 == viewport2);
    }

internal:
    Viewport(const D3D10_VIEWPORT& viewport)
    {
        TopLeftX = viewport.TopLeftX;
        TopLeftY = viewport.TopLeftY;
        Width = viewport.Width;
        Height = viewport.Height;
        MinDepth = viewport.MinDepth;
        MaxDepth = viewport.MaxDepth;    
    }

    operator const D3D10_VIEWPORT ()
    {
        D3D10_VIEWPORT nativeViewport;

        nativeViewport.TopLeftX = TopLeftX;
        nativeViewport.TopLeftY = TopLeftY;
        nativeViewport.Width = Width;
        nativeViewport.Height = Height;
        nativeViewport.MinDepth = MinDepth;
        nativeViewport.MaxDepth = MaxDepth;

        return nativeViewport;
    }
public:

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
