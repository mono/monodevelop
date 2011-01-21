//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11Resource.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// Allows acccess to a buffer resource, which is unstructured memory. 
    /// Buffers typically store vertex or index data.
    /// <para>(Also see DirectX SDK: ID3D11Buffer)</para>
    /// </summary>
    public ref class D3DBuffer :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::D3DResource
    {
    public: 
        /// <summary>
        /// Get the properties of a buffer resource (see <see cref="BufferDescription"/>)<seealso cref="BufferDescription"/>.
        /// <para>(Also see DirectX SDK: ID3D11Buffer::GetDesc)</para>
        /// </summary>
        property BufferDescription Description
        {
            BufferDescription get();
        }

    internal:

		D3DBuffer(void)
        { }

		D3DBuffer(ID3D11Buffer* pNativeID3D11Buffer) : D3DResource(pNativeID3D11Buffer)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11Buffer)); }
        }
    };

    /// <summary>
    /// Stores the parameters required to get or set a vertex buffer for a device.
    /// </summary>
    public value class VertexBuffer
    {
    public:

        /// <param name="buffer">A vertex buffer (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/>. 
        /// The vertex buffer must have been created with the VertexBuffer flag.</param>
        /// <param name="stride">The stride value. This is the size (in bytes) of the elements that are to be used from the vertex buffer.</param>
        /// <param name="offset">The offset value. This is the number of bytes between the first element of the vertex buffer and the first element that will be used.</param>
        VertexBuffer(D3DBuffer^ buffer, UInt32 stride, UInt32 offset)
            : buffer(buffer), stride(stride), offset(offset)
        { }

        property D3DBuffer^ Buffer
        {
            D3DBuffer^ get(void) { return buffer; }
        }

        property UInt32 Stride
        {
            UInt32 get(void) { return stride; }
        }

        property UInt32 Offset
        {
            UInt32 get(void) { return offset; }
        }

        static bool operator ==(VertexBuffer buffer1, VertexBuffer buffer2)
        {
            return buffer1.buffer == buffer2.buffer &&
                buffer1.stride == buffer2.stride &&
                buffer1.offset == buffer2.offset;
        }

        static bool operator !=(VertexBuffer buffer1, VertexBuffer buffer2)
        {
            return !(buffer1 == buffer2);
        }

        virtual bool Equals(Object^ obj) override
        {
            if (obj->GetType() != VertexBuffer::typeid)
            {
                return false;
            }

            return *this == safe_cast<VertexBuffer>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + buffer->GetHashCode();
            hashCode = hashCode * 31 + stride.GetHashCode();
            hashCode = hashCode * 31 + offset.GetHashCode();

            return hashCode;
        }

    private:

        D3DBuffer^ buffer;
        UInt32 stride;
        UInt32 offset;
    };

    /// <summary>
    /// Stores the parameters required to get or set the current index buffer for a device.
    /// </summary>
    public value class IndexBuffer
    {
    public:

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="buffer">A D3DBuffer object, that contains indices. The index buffer must have been created with the indexBuffer flag.</param>
        /// <param name="format">A Format that specifies the format of the data in the index buffer. The only formats allowed for index buffer data are 16-bit (Format_R16_UINT) and 32-bit (Format_R32_UINT) integers.</param>
        /// <param name="offset">Offset (in bytes) from the start of the index buffer to the first index to use.</param>
        IndexBuffer(D3DBuffer^ buffer, Graphics::Format format, UInt32 offset)
            : buffer(buffer), format(format), offset(offset)
        { }

        property D3DBuffer^ Buffer
        {
            D3DBuffer^ get(void) { return buffer; }
        }

        property Format Format
        {
            Graphics::Format get(void) { return format; }
        }

        property UInt32 Offset
        {
            UInt32 get(void) { return offset; }
        }

        static bool operator ==(IndexBuffer buffer1, IndexBuffer buffer2)
        {
            return buffer1.buffer == buffer2.buffer &&
                buffer1.format == buffer2.format &&
                buffer1.offset == buffer2.offset;
        }

        static bool operator !=(IndexBuffer buffer1, IndexBuffer buffer2)
        {
            return !(buffer1 == buffer2);
        }

        virtual bool Equals(Object^ obj) override
        {
            if (obj->GetType() != IndexBuffer::typeid)
            {
                return false;
            }

            return *this == safe_cast<IndexBuffer>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + buffer->GetHashCode();
            hashCode = hashCode * 31 + format.GetHashCode();
            hashCode = hashCode * 31 + offset.GetHashCode();

            return hashCode;
        }

    private:

        D3DBuffer^ buffer;
        Graphics::Format format;
        UInt32 offset;
    };

} } } }
