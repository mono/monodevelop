//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;
ref class ClassLinkage;

    /// <summary>
    /// This class encapsulates an HLSL class.
    /// <para>(Also see DirectX SDK: ID3D11ClassInstance)</para>
    /// </summary>
    public ref class ClassInstance :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::DeviceChild
    {
    public: 
        /// <summary>
        /// Gets the ClassLinkage object associated with the current HLSL class.
        /// <para>(Also see DirectX SDK: ID3D11ClassInstance::GetClassLinkage)</para>
        /// </summary>
        property ClassLinkage^ ClassLinkage
        {
            Direct3D11::ClassLinkage^ get(void);
        }

        /// <summary>
        /// Gets a description of the current HLSL class.
        /// <para>(Also see DirectX SDK: ID3D11ClassInstance::GetDesc)</para>
        /// </summary>
        property ClassInstanceDescription Description
        {
            ClassInstanceDescription get();
        }

        /// <summary>
        /// Gets the instance name of the current HLSL class.
        /// <para>(Also see DirectX SDK: ID3D11ClassInstance::GetInstanceName)</para>
        /// </summary>
        property String^ InstanceName
        {
            String^ get();
        }

        /// <summary>
        /// Gets the type of the current HLSL class.
        /// <para>(Also see DirectX SDK: ID3D11ClassInstance::GetTypeName)</para>
        /// </summary>
        property String^ TypeName
        {
            String^ get();
        }

    internal:

        ClassInstance(void)
        { }

		ClassInstance(ID3D11ClassInstance* pNativeID3D11ClassInstance) : DeviceChild(pNativeID3D11ClassInstance)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11ClassInstance)); }
        }
    };
} } } }
