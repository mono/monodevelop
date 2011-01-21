//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DWriteEnums.h"
#include "DWriteStructs.h"

using namespace System::Collections::Generic;

using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {
    
    /// <summary>
    /// Implements an enumerable list of FontFeature
    /// </summary>
    private ref class TypographyList : System::Collections::Generic::IEnumerator<FontFeature>, System::Collections::IEnumerator
    {

    public:

        property FontFeature Current
        {
            virtual FontFeature get();
        }

        virtual bool MoveNext();

        virtual void Reset();

    protected:

        property System::Object^ CurrentObject
        {
            virtual System::Object^ get()  = System::Collections::IEnumerator::Current::get;
        }

    internal:
    
        TypographyList(IDWriteTypography* pNativeIDWriteTypography) : nativeInterface(pNativeIDWriteTypography), m_current(-1)
        { 
            nativeInterface->AddRef();
        }
        
        TypographyList::~TypographyList()
        {
            nativeInterface->Release();
        }


    private:
        int m_current;
        IDWriteTypography* nativeInterface;

    private:
        TypographyList()
        { }

    };

    /// <summary>
    /// Represents a collection of OpenType font typography settings (<see cref="FontFeature"/>).
    /// <para>(Also see DirectX SDK: IDWriteTypography)</para>
    /// </summary>
    /// <remarks>Once an OpenType font feature setting has been added, it cannot be removed from the collection.</remarks>
    public ref class TypographySettingCollection : public DirectUnknown, IList<FontFeature>
    {

    public: 

        /// <summary>
        /// Gets the font feature at the specified index.
        /// </summary>
        property FontFeature default[int] 
        {
            virtual FontFeature get(int index);

            virtual void set (int i, FontFeature feature);
        }

        /// <summary>
        /// Adds an OpenType font feature.
        /// </summary>
        /// <param name="feature">Font feature to add to the collection.</param>
        virtual void Add(FontFeature feature);

        /// <summary>
        /// Get the number of font features in the collection.
        /// </summary>
        virtual property int Count
        {
            int get();
        }

        /// <summary>
        /// Determines if the collection is readonly.
        /// </summary>
        property bool IsReadOnly
        {
            virtual bool get();
        }

        /// <summary>
        /// Gets the enumerator for this collection.
        /// </summary>
        virtual System::Collections::IEnumerator^ GetEnumerator() = System::Collections::IEnumerable::GetEnumerator;
        virtual System::Collections::Generic::IEnumerator<FontFeature>^ GetGenericEnumerator() = System::Collections::Generic::IEnumerable<FontFeature>::GetEnumerator;

    protected:

        // REVIEW: why aren't these supported?

        // The following methods are not being supported, and thus made protected to hide them

        virtual int IndexOf(FontFeature item) = System::Collections::Generic::IList<FontFeature>::IndexOf;

        virtual void Insert(int index, FontFeature item) = System::Collections::Generic::IList<FontFeature>::Insert;

        virtual void RemoveAt(int index) = System::Collections::Generic::IList<FontFeature>::RemoveAt;

        virtual void Clear() = System::Collections::Generic::IList<FontFeature>::Clear;

        virtual bool Contains(FontFeature item) = System::Collections::Generic::IList<FontFeature>::Contains;

        virtual void CopyTo(array<FontFeature>^ fontFeatureArray, int arrayIndex) = System::Collections::Generic::IList<FontFeature>::CopyTo;

        virtual bool Remove(FontFeature item) = System::Collections::Generic::IList<FontFeature>::Remove;

    internal:
        TypographySettingCollection()
        { }
    
        TypographySettingCollection(IDWriteTypography* pNativeIDWriteTypography) : DirectUnknown(pNativeIDWriteTypography)
        { }

    };

} } } }