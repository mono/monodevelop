//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;
using namespace System::Runtime::InteropServices::ComTypes;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent {

/// <summary>
/// This is an object that provides a managed System.IO.Stream object with a COM IStream implementation.
/// </summary>
public ref class StreamWrapper : public System::Runtime::InteropServices::ComTypes::IStream
{
public:
    /// <summary>
    /// Creates an StreamWrapper for the specified stream.
    /// </summary>
    /// <param name="stream">The stream to wrap.</param>
    StreamWrapper( Stream^ stream );

// IStream implementation
protected:
    virtual void Clone( System::Runtime::InteropServices::ComTypes::IStream^% ppstm ) = IStream::Clone;
    virtual void Commit( int grfCommitFlags ) = IStream::Commit;
    virtual void CopyTo( System::Runtime::InteropServices::ComTypes::IStream^ pstm, __int64 cb, IntPtr pcbRead, IntPtr pcbWritten) = IStream::CopyTo;
    virtual void LockRegion( __int64 libOffset, __int64 cb, int dwLockType ) = IStream::LockRegion;
    virtual void Read( cli::array<unsigned char>^ pv, int cb, System::IntPtr pcbRead ) = IStream::Read;
    virtual void Revert( ) = IStream::Revert;
    virtual void Seek( __int64 dlibMove, int dwOrigin, IntPtr plibNewPosition ) = IStream::Seek;
    virtual void SetSize( __int64 libNewSize ) = IStream::SetSize;
    virtual void Stat( System::Runtime::InteropServices::ComTypes::STATSTG% pstatstg, int grfStatFlag ) = IStream::Stat;
    virtual void UnlockRegion( __int64 libOffset, __int64 cb, int dwLockType ) = IStream::UnlockRegion;
    virtual void Write( cli::array<unsigned char>^ pv, int cb, IntPtr pcbWritten ) = IStream::Write;

private:
    Stream^ stream;
};
} } } }
