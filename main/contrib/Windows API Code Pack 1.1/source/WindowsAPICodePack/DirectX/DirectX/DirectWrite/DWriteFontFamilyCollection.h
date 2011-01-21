//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DWriteEnums.h"
#include "DWriteStructs.h"

using namespace System::Globalization;
using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {
    
    ref class FontFamily;
    ref class FontFamilyCollection;


    private ref class FontFamilyEnum : DirectUnknown, System::Collections::Generic::IEnumerator<FontFamily^>, System::Collections::IEnumerator
    {

    public:

        // IEnumerator Members
        
        property FontFamily^ Current
        {
            virtual FontFamily^ get();
        }

        virtual bool MoveNext();

        virtual void Reset();

    protected:
        // The following methods are not being supported, and thus made protected to hide them

        property System::Object^ CurrentObject
        {
            virtual System::Object^ get()  = System::Collections::IEnumerator::Current::get;
        }

    internal:    
        FontFamilyEnum(IDWriteFontCollection* pfontCollection, FontFamilyCollection^ familyCollection);
        
    private:
        int m_current;
        FontFamilyCollection^ m_familyCollection;

    private:
        FontFamilyEnum()
        { }
    };


    /// <summary>
    /// A collection of <see cref="FontFamily"/> Objects.
    /// A <see cref="FontFamily"/> represents a set of fonts that share the same design but are differentiated
    /// by weight, stretch, and style. <see cref="FontFamily"/>
    /// <para>(Also see DirectX SDK: IDWriteFontFamily)</para>
    /// </summary>
    public ref class FontFamilyCollection : public DirectUnknown, IList<FontFamily^>
    {
    public: 

        /// <summary>
        /// Gets the font family at the specified index.
        /// </summary>
        property FontFamily^ default[int] 
        {
            virtual FontFamily^ get(int index);

            virtual void set (int index, FontFamily^ feature);
        }

        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        virtual property int Count
        {
            int get();
        }

        /// <summary>
        /// Determines whether the collection is read only. Returns True.
        /// </summary>
        property bool IsReadOnly
        {
            virtual bool get();
        }

        /// <summary>
        /// 
        /// </summary>
        virtual System::Collections::Generic::IEnumerator<FontFamily^>^ GetGenericEnumerator() = System::Collections::Generic::IEnumerable<FontFamily^>::GetEnumerator;

        /// <summary>
        /// Determines whether the collection contains a given font family name.
        /// </summary>
        /// <param name="fontFamilyName">Name of the font family. The name is not case-sensitive but must otherwise exactly match a family name in the collection.</param>
        /// <returns>True if the font family is found, otherwise false.</returns>
        bool Contains(String^ fontFamilyName);

        /// <summary>
        /// Retrives the index in the collection of a given font family given its name.
        /// </summary>
        /// <param name="fontFamilyName">Name of the font family. The name is not case-sensitive but must otherwise exactly match a family name in the collection.</param>
        /// <returns>The zero based index in the collection for this font family, or -1 if the font family is not found.</returns>
        int IndexOf(String^ fontFamilyName);

        /// <summary>
        /// Determines whether the collection contains a given font family.
        /// </summary>
        /// <param name="item">The font family to compare with.</param>
        /// <returns>True if the font family is found, otherwise false.</returns>
        virtual int IndexOf(FontFamily^ item);

        /// <summary>
        /// Retrives the index in the collection of a given font family.
        /// </summary>
        /// <param name="item">The font family.</param>
        /// <returns>The zero based index in the collection for this font family, or -1 if the font family is not found.</returns>
        virtual bool Contains(FontFamily^ item);

    protected:
        // REVIEW: why "protected"? why not just private?
        // The following methods are not being supported, and thus made protected to hide them
        virtual void CopyTo(array<FontFamily^>^ fontFamilyArray, int arrayIndex) = System::Collections::Generic::IList<FontFamily^>::CopyTo;

        virtual void Add(FontFamily^ item) = System::Collections::Generic::IList<FontFamily^>::Add;

        virtual void Insert(int index, FontFamily^ item) = System::Collections::Generic::IList<FontFamily^>::Insert;

        virtual void RemoveAt(int index) = System::Collections::Generic::IList<FontFamily^>::RemoveAt;

        virtual void Clear() = System::Collections::Generic::IList<FontFamily^>::Clear;

        virtual bool Remove(FontFamily^ item)= System::Collections::Generic::IList<FontFamily^>::Remove;

        virtual System::Collections::IEnumerator^ GetEnumerator() = System::Collections::IEnumerable::GetEnumerator;

    internal:
        FontFamilyCollection(IDWriteFontCollection* pFontCollection);
        ~FontFamilyCollection();

    private:
        FontFamily^ InitializeElement(int);

    private:
        // This class cannot be instantiated
        FontFamilyCollection() { }
        array<FontFamily^>^ m_families;
};

} } } }