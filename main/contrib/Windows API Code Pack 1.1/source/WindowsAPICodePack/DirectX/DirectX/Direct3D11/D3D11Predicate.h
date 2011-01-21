//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11Query.h"
#include "D3D11DeviceContext.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A predicate interface determines whether geometry should be processed depending on the results of a previous draw call.
    /// <para>(Also see DirectX SDK: ID3D11Predicate)</para>
    /// </summary>
    public ref class D3DPredicate :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::D3DQuery
    {
    public:

        property bool WhenTrue
        {
            bool get(void);
            void set(bool value);
        }

        property DeviceContext^ Context
        {
            DeviceContext^ get(void);
            void set(DeviceContext^ value);
        }

    internal:

        D3DPredicate(void)
        { }

		D3DPredicate(ID3D11Predicate* pNativeID3D11Predicate)
            : D3DQuery(pNativeID3D11Predicate)
        { }

		D3DPredicate(ID3D11Predicate* pNativeID3D11Predicate, bool whenTrue, DeviceContext^ context)
            : D3DQuery(pNativeID3D11Predicate), whenTrue(whenTrue), context(context)
        { }

    private:

        Nullable<bool> whenTrue;
        DeviceContext^ context;

    };
} } } }
