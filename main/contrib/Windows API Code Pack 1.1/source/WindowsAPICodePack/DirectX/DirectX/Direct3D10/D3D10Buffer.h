//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10Resource.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A buffer interface accesses a buffer resource, which is unstructured memory. Buffers typically store vertex or index data.
    /// <para>(Also see DirectX SDK: ID3D10Buffer)</para>
    /// </summary>
    public ref class D3DBuffer :
        public D3DResource
    {
    public: 
        /// <summary>
        /// Get the properties of a buffer resource.
        /// <para>(Also see DirectX SDK: ID3D10Buffer::GetDesc)</para>
        /// </summary>
        property BufferDescription Description
        {
            BufferDescription get();
        }

        /// <summary>
        /// Get the data contained in the resource and deny GPU access to the resource.
        /// <para>(Also see DirectX SDK: ID3D10Buffer::Map)</para>
        /// </summary>
        /// <param name="type">Flag that specifies the CPU's permissions for the reading and writing of a resource. For possible values, see Map.</param>
        /// <param name="options">Flag that specifies what the CPU should do when the GPU is busy (see <see cref="MapOptions"/>)<seealso cref="MapOptions"/>. This flag is optional.</param>
        /// <returns>Pointer to the buffer resource data.</returns>
        IntPtr Map(Direct3D10::Map type, MapOptions options);

        /// <summary>
        /// Invalidate the pointer to the resource retrieved by D3DBuffer.Map and reenable GPU access to the resource.
        /// <para>(Also see DirectX SDK: ID3D10Buffer::Unmap)</para>
        /// </summary>
        void Unmap();

    internal:

        D3DBuffer(void)
        { }

		D3DBuffer(ID3D10Buffer* pNativeID3D10Buffer) : D3DResource(pNativeID3D10Buffer)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10Buffer)); }
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

    public value class OutputBuffer
    {
    public:

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="buffer">
        /// The buffers must have been created with the <see cref="BindingOptions" /><b>.StreamOutput</b> flag.
        /// </param>
        /// <param name="offset">
        /// Offset in bytes to the start of the buffer position to use. -1 indicates that output should
        /// continue after the last location written to the buffer in a previous stream output pass.
        /// </param>
        OutputBuffer(D3DBuffer^ buffer, UInt32 offset)
            : buffer(buffer), offset(offset)
        { }

        property D3DBuffer^ Buffer
        {
            D3DBuffer^ get(void) { return buffer; }
        }

        property UInt32 Offset
        {
            UInt32 get(void) { return offset; }
        }

        static bool operator ==(OutputBuffer buffer1, OutputBuffer buffer2)
        {
            return buffer1.buffer == buffer2.buffer &&
                buffer1.offset == buffer2.offset;
        }

        static bool operator !=(OutputBuffer buffer1, OutputBuffer buffer2)
        {
            return !(buffer1 == buffer2);
        }

        virtual bool Equals(Object^ obj) override
        {
            if (obj->GetType() != OutputBuffer::typeid)
            {
                return false;
            }

            return *this == safe_cast<OutputBuffer>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + buffer->GetHashCode();
            hashCode = hashCode * 31 + offset.GetHashCode();

            return hashCode;
        }

    private:

        D3DBuffer^ buffer;
        UInt32 offset;
    };

} } } }
