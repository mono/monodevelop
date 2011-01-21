//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

/// <summary>
/// Indicates the device state.
/// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK)</para>
/// </summary>
public ref class StateBlockMask
{
public:
    /// <summary>
    /// Boolean value indicating whether to save the vertex shader state.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.VS)</para>
    /// </summary>
    property bool VertexShader
    {
        bool get()
        {
            return nativeObject->VS != 0;
        }
    }
    /// <summary>
    /// Array of vertex-shader samplers.  The array is a multi-byte bitmask where each bit represents one sampler slot.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.VSSamplers)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ VertexShaderSamplers
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            if (VSSamplersArray == nullptr)
            {
                int size = sizeof(nativeObject->VSSamplers);
                array<unsigned char>^ tempArray = gcnew array<unsigned char>(size);
                pin_ptr<unsigned char> arr = &tempArray[0];

                memcpy(arr, nativeObject->VSSamplers, size);

                VSSamplersArray = Array::AsReadOnly(tempArray);
            }

            return VSSamplersArray;
        }
    }
    /// <summary>
    /// Array of vertex-shader resources. The array is a multi-byte bitmask where each bit represents one resource slot.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.VSShaderResources)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ VertexShaderShaderResources
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            if (VSShaderResourcesArray == nullptr)
            {
                int size = sizeof(nativeObject->VSShaderResources);
                array<unsigned char>^ tempArray = gcnew array<unsigned char>(size);
                pin_ptr<unsigned char> arr = &tempArray[0];

                memcpy(arr, nativeObject->VSShaderResources, size);

                VSShaderResourcesArray = Array::AsReadOnly(tempArray);
            }

            return VSShaderResourcesArray;
        }
    }
    /// <summary>
    /// Array of vertex-shader constant buffers. The array is a multi-byte bitmask where each bit represents one constant buffer slot.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.VSConstantBuffers)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ VertexShaderConstantBuffers
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            if (VSConstantBuffersArray == nullptr)
            {
                int size = sizeof(nativeObject->VSConstantBuffers);
                array<unsigned char>^ tempArray = gcnew array<unsigned char>(size);
                pin_ptr<unsigned char> arr = &tempArray[0];

                memcpy(arr, nativeObject->VSConstantBuffers, size);

                VSConstantBuffersArray = Array::AsReadOnly(tempArray);
            }

            return VSConstantBuffersArray;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the geometry shader state.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.GS)</para>
    /// </summary>
    property bool GeometryShader
    {
        bool get()
        {
            return nativeObject->GS != 0;
        }
    }
    /// <summary>
    /// Array of geometry-shader samplers. The array is a multi-byte bitmask where each bit represents one sampler slot.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.GSSamplers)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ GeometryShaderSamplers
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            if (GSSamplersArray == nullptr)
            {
                int size = sizeof(nativeObject->GSSamplers);
                array<unsigned char>^ tempArray = gcnew array<unsigned char>(size);
                pin_ptr<unsigned char> arr = &tempArray[0];

                memcpy(arr, nativeObject->GSSamplers, size);

                GSSamplersArray = Array::AsReadOnly(tempArray);
            }

            return GSSamplersArray;
        }
    }
    /// <summary>
    /// Array of geometry-shader resources. The array is a multi-byte bitmask where each bit represents one resource slot.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.GSShaderResources)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ GeometryShaderShaderResources
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            if (GSShaderResourcesArray == nullptr)
            {
                int size = sizeof(nativeObject->GSShaderResources);
                array<unsigned char>^ tempArray = gcnew array<unsigned char>(size);
                pin_ptr<unsigned char> arr = &tempArray[0];

                memcpy(arr, nativeObject->GSShaderResources, size);

                GSShaderResourcesArray = Array::AsReadOnly(tempArray);
            }

            return GSShaderResourcesArray;
        }
    }
    /// <summary>
    /// Array of geometry-shader constant buffers. The array is a multi-byte bitmask where each bit represents one buffer slot.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.GSConstantBuffers)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ GeometryShaderConstantBuffers
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            if (GSConstantBuffersArray == nullptr)
            {
                int size = sizeof(nativeObject->GSConstantBuffers);
                array<unsigned char>^ tempArray = gcnew array<unsigned char>(size);
                pin_ptr<unsigned char> arr = &tempArray[0];

                memcpy(arr, nativeObject->GSConstantBuffers, size);

                GSConstantBuffersArray = Array::AsReadOnly(tempArray);
            }

            return GSConstantBuffersArray;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the pixel shader state.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.PS)</para>
    /// </summary>
    property bool PixelShader
    {
        bool get()
        {
            return nativeObject->PS != 0;
        }
    }
    /// <summary>
    /// Array of pixel-shader samplers. The array is a multi-byte bitmask where each bit represents one sampler slot.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.PSSamplers)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ PixelShaderSamplers
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            if (PSSamplersArray == nullptr)
            {
                int size = sizeof(nativeObject->PSSamplers);
                array<unsigned char>^ tempArray = gcnew array<unsigned char>(size);
                pin_ptr<unsigned char> arr = &tempArray[0];

                memcpy(arr, nativeObject->PSSamplers, size);

                PSSamplersArray = Array::AsReadOnly(tempArray);
            }

            return PSSamplersArray;
        }
    }
    /// <summary>
    /// Array of pixel-shader resources. The array is a multi-byte bitmask where each bit represents one resource slot.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.PSShaderResources)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ PixelShaderShaderResources
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            if (PSShaderResourcesArray == nullptr)
            {
                int size = sizeof(nativeObject->PSShaderResources);
                array<unsigned char>^ tempArray = gcnew array<unsigned char>(size);
                pin_ptr<unsigned char> arr = &tempArray[0];

                memcpy(arr, nativeObject->PSShaderResources, size);

                PSShaderResourcesArray = Array::AsReadOnly(tempArray);
            }

            return PSShaderResourcesArray;
        }
    }
    /// <summary>
    /// Array of pixel-shader constant buffers. The array is a multi-byte bitmask where each bit represents one constant buffer slot.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.PSConstantBuffers)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ PixelShaderConstantBuffers
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            if (PSConstantBuffersArray == nullptr)
            {
                int size = sizeof(nativeObject->PSConstantBuffers);
                array<unsigned char>^ tempArray = gcnew array<unsigned char>(size);
                pin_ptr<unsigned char> arr = &tempArray[0];

                memcpy(arr, nativeObject->PSConstantBuffers, size);

                PSConstantBuffersArray = Array::AsReadOnly(tempArray);
            }

            return PSConstantBuffersArray;
        }
    }
    /// <summary>
    /// Array of vertex buffers. The array is a multi-byte bitmask where each bit represents one resource slot.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.IAVertexBuffers)</para>
    /// </summary>
    property ReadOnlyCollection<unsigned char>^ InputAssemblerVertexBuffers
    {
        ReadOnlyCollection<unsigned char>^ get()
        {
            if (IAVertexBuffersArray == nullptr)
            {
                int size = sizeof(nativeObject->IAVertexBuffers);
                array<unsigned char>^ tempArray = gcnew array<unsigned char>(size);
                pin_ptr<unsigned char> arr = &tempArray[0];

                memcpy(arr, nativeObject->IAVertexBuffers, size);

                IAVertexBuffersArray = Array::AsReadOnly(tempArray);
            }

            return IAVertexBuffersArray;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the index buffer state.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.IAIndexBuffer)</para>
    /// </summary>
    property bool InputAssemblerIndexBuffer
    {
        bool get()
        {
            return nativeObject->IAIndexBuffer != 0;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the input layout state.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.IAInputLayout)</para>
    /// </summary>
    property bool InputAssemblerInputLayout
    {
        bool get()
        {
            return nativeObject->IAInputLayout != 0;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the primitive topology state.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.IAPrimitiveTopology)</para>
    /// </summary>
    property bool InputAssemblerPrimitiveTopology
    {
        bool get()
        {
            return nativeObject->IAPrimitiveTopology != 0;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the render targets states.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.OMRenderTargets)</para>
    /// </summary>
    property bool OutputMergerRenderTargets
    {
        bool get()
        {
            return nativeObject->OMRenderTargets != 0;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the depth-stencil state.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.OMDepthStencilState)</para>
    /// </summary>
    property bool OutputMergerDepthStencilState
    {
        bool get()
        {
            return nativeObject->OMDepthStencilState != 0;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the blend state.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.OMBlendState)</para>
    /// </summary>
    property bool OutputMergerBlendState
    {
        bool get()
        {
            return nativeObject->OMBlendState != 0;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the viewports states.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.RSViewports)</para>
    /// </summary>
    property bool RasterizerViewports
    {
        bool get()
        {
            return nativeObject->RSViewports != 0;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the scissor rectangles states.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.RSScissorRects)</para>
    /// </summary>
    property bool RasterizerScissorRectangles
    {
        bool get()
        {
            return nativeObject->RSScissorRects != 0;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the rasterizer state.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.RSRasterizerState)</para>
    /// </summary>
    property bool RasterizerState
    {
        bool get()
        {
            return nativeObject->RSRasterizerState != 0;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the stream-out buffers states.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.SOBuffers)</para>
    /// </summary>
    property bool StreamOutputBuffers
    {
        bool get()
        {
            return nativeObject->SOBuffers != 0;
        }
    }
    /// <summary>
    /// Boolean value indicating whether to save the predication state.
    /// <para>(Also see DirectX SDK: D3D10_STATE_BLOCK_MASK.Predication)</para>
    /// </summary>
    property bool Predication
    {
        bool get()
        {
            return nativeObject->Predication != 0;
        }
    }

    StateBlockMask^ DisableAll(void);
    StateBlockMask^ DisableCapture(DeviceStateType type, int start, int length);
    StateBlockMask^ EnableAll(void);
    StateBlockMask^ EnableCapture(DeviceStateType type, int start, int length);

    StateBlockMask^ Xor(StateBlockMask^ otherMask)
    {
        return this ^ otherMask;
    }
    StateBlockMask^ BitwiseAnd(StateBlockMask^ otherMask)
    {
        return this & otherMask;
    }
    StateBlockMask^ BitwiseOr(StateBlockMask^ otherMask)
    {
        return this | otherMask;
    }

    static StateBlockMask^ operator ^(StateBlockMask^ mask1, StateBlockMask^ mask2);
    static StateBlockMask^ operator &(StateBlockMask^ mask1, StateBlockMask^ mask2);
    static StateBlockMask^ operator |(StateBlockMask^ mask1, StateBlockMask^ mask2);

    StateBlockMask()
    {
        nativeObject.Set(new D3D10_STATE_BLOCK_MASK());
        ZeroMemory(nativeObject.Get(), sizeof(D3D10_STATE_BLOCK_MASK));
    }

internal:

    StateBlockMask(const D3D10_STATE_BLOCK_MASK &stateBlockMask)
    {
        *(nativeObject.Get()) = stateBlockMask;
    }

    void CopyTo(D3D10_STATE_BLOCK_MASK &stateBlockMask)
    {
        stateBlockMask = *(nativeObject.Get());
    }

private:

    AutoPointer<D3D10_STATE_BLOCK_MASK> nativeObject;

    ReadOnlyCollection<unsigned char>^ VSSamplersArray;
    ReadOnlyCollection<unsigned char>^ VSShaderResourcesArray;
    ReadOnlyCollection<unsigned char>^ VSConstantBuffersArray;
    ReadOnlyCollection<unsigned char>^ GSSamplersArray;
    ReadOnlyCollection<unsigned char>^ GSShaderResourcesArray;
    ReadOnlyCollection<unsigned char>^ GSConstantBuffersArray;
    ReadOnlyCollection<unsigned char>^ PSSamplersArray;
    ReadOnlyCollection<unsigned char>^ PSShaderResourcesArray;
    ReadOnlyCollection<unsigned char>^ PSConstantBuffersArray;
    ReadOnlyCollection<unsigned char>^ IAVertexBuffersArray;
};

}}}}