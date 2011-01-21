// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX {

// XML comments can't be applied to templates
// <summary>
// An Auto pointer styled class supporting an IUnknown interface
// Reserved for internal use only.
// </summary>
template <typename T>
private ref struct AutoIUnknown : AutoPointer<T>
{
internal:
    AutoIUnknown() : AutoPointer<T>::AutoPointer()  {}

protected:
    virtual void DisposeTarget() override;
};
} } }
