//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectUnknown.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

    /// <summary>
    /// An GraphicsObject is the base for all Graphics classes. 
    /// <para>(Also see DirectX SDK: IDXGIObject)</para>
    /// </summary>
    public ref class GraphicsObject :
        public DirectUnknown
    {
    public: 

        // REVIEW: if this whole class were generic, then this GetParent() method
        // could be a property, because then the class's generic type parameters
        // could include both the underlying interface type and the parent type.

        // Also, right now the caller needs to know the exact type expected to be
        // returned. What we ought to have is just always ask for IDXGIObject and
        // always return GraphicsObject, but internally check the returned interface
        // for what it actually is and return an instance of the appropriate type.
        // Then the caller can cast if necessary, and we'll always know for sure
        // we're wrapping the interface with the correct managed type.

        /// <summary>
        /// Gets the parent of this object.
        /// <para>(Also see DirectX SDK: IDXGIObject::GetParent)</para>
        /// </summary>
        /// <typeparam name="T">The type of the parent object requested. 
        /// This type has to be a GraphicsObject or a subtype.</typeparam>
        /// <returns>The parent object. Or null if this object does not have a parent.</returns>
        generic <typename T> where T : GraphicsObject
        T GetParent(void);

    internal:

        GraphicsObject(void)
        { }

        GraphicsObject(IDXGIObject* pNativeIDXGIObject) : DirectUnknown(pNativeIDXGIObject)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(IDXGIObject)); }
        }
    };
} } } }
