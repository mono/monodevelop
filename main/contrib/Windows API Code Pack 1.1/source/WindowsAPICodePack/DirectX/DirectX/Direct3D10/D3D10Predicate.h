//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10Query.h"
#include "D3D10Device.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A predicate interface determines whether geometry should be processed depending on the results of a previous draw call.
    /// <para>(Also see DirectX SDK: ID3D10Predicate)</para>
    /// </summary>
    public ref class D3DPredicate :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D10::D3DQuery
    {
    public:

        property bool WhenTrue
        {
            bool get(void);
            void set(bool newWhenTrue);
        }

    internal:

        D3DPredicate(void)
        { }

        D3DPredicate(ID3D10Predicate* pNativeID3D10Predicate)
            : D3DQuery(pNativeID3D10Predicate)
        { }

        D3DPredicate(ID3D10Predicate* pNativeID3D10Predicate, bool whenTrue)
            : D3DQuery(pNativeID3D10Predicate), whenTrue(Nullable<bool>(whenTrue))
        { }

    private:

        Nullable<bool> whenTrue;
    };
} } } }
