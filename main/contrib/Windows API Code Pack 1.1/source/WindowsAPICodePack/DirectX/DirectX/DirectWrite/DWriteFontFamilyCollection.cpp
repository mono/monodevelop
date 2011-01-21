// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DWriteFontFamilyCollection.h"
#include "DWriteFontFamily.h"

#include <msclr/lock.h>
using namespace msclr;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

FontFamilyCollection::FontFamilyCollection(IDWriteFontCollection* pFontCollection) : DirectUnknown(pFontCollection)
{
    UINT count = CastInterface<IDWriteFontCollection>()->GetFontFamilyCount();

    if (count > 0)
    {
        m_families = gcnew array<FontFamily^>(count);
    }
}


FontFamilyCollection::~FontFamilyCollection()
{
    if (m_families != nullptr)
    {
        for each (FontFamily^ family in m_families)
        {
            if (family != nullptr)
            {
                delete family;
            }
        }
        m_families = nullptr;
    }
}

FontFamily^ FontFamilyCollection::InitializeElement(int i)
{
    // REVIEW: check thread-safety for class
    lock l(this); // Make sure the array is not being modified on another thread, 
                  // otherwise we might get an exception

    if (m_families[i] == nullptr || m_families[i]->CastInterface<IUnknown>() == NULL)
    {
        IDWriteFontFamily* fontFamily = NULL;
        Validate::VerifyResult(
            CastInterface<IDWriteFontCollection>()->GetFontFamily(i, &fontFamily));

        m_families[i] = gcnew FontFamily(fontFamily);
    }
    
    return m_families[i];
}

FontFamily^ FontFamilyCollection::default::get(int index)
{
    if (index >= 0 && index < m_families->Length)
    {        
        return InitializeElement(index);
    }
    else
    {
        throw gcnew ArgumentOutOfRangeException("index", index, "Index must be within the range of valid indexes for the collection");
    }
}

void FontFamilyCollection::default::set(int, FontFamily^)
{
    throw gcnew NotSupportedException("This collection is readonly.");
}

void FontFamilyCollection::Add(FontFamily^)
{
    throw gcnew NotSupportedException("This collection is readonly.");
}

int FontFamilyCollection::Count::get()
{
    return m_families->Length; 
}

bool FontFamilyCollection::IsReadOnly::get()
{
    return true;
}

System::Collections::Generic::IEnumerator<FontFamily^>^ FontFamilyCollection::GetGenericEnumerator(void)
{
    return gcnew FontFamilyEnum(CastInterface<IDWriteFontCollection>(), this);
}

System::Collections::IEnumerator^ FontFamilyCollection::GetEnumerator(void)
{
    return GetGenericEnumerator();
}

int FontFamilyCollection::IndexOf(FontFamily^ item)
{
    if (item == nullptr)
    {
        throw gcnew ArgumentNullException("item");
    }

	String^ firstName = Enumerable::FirstOrDefault(item->FamilyNames->Values);

	if (firstName != nullptr)
	{
		return IndexOf(firstName);
	}

    return -1;
}

void FontFamilyCollection::Insert(int, FontFamily^)
{
    throw gcnew NotSupportedException("This collection is readonly.");
}

void FontFamilyCollection::RemoveAt(int)
{
    throw gcnew NotSupportedException("This collection is readonly.");
}

void FontFamilyCollection::Clear()
{
    throw gcnew NotSupportedException("This collection is readonly.");
}

bool FontFamilyCollection::Contains(FontFamily^ item)
{
    if (item == nullptr)
    {
        throw gcnew ArgumentNullException("item");
    }

    for each (String^ value in item->FamilyNames->Values)
    {
        return this->Contains(value);
    }

    return false;
}

// REVIEW: why is not supported?
void FontFamilyCollection::CopyTo(array<FontFamily^>^ /* fontFamilyArray */, int /* arrayIndex */)
{
    throw gcnew NotSupportedException();
}

bool FontFamilyCollection::Remove(FontFamily^)
{
    throw gcnew NotSupportedException("This collection is readonly.");
}

bool FontFamilyCollection::Contains(String^ fontFamilyName)
{
    if (String::IsNullOrEmpty(fontFamilyName))
    {
        throw gcnew ArgumentNullException("fontFamilyName", "fontFamilyName cannot be null or empty.");
    }

    UINT32 index;
    BOOL exists;
    pin_ptr<const WCHAR> familyName = PtrToStringChars(fontFamilyName);

    Validate::VerifyResult(CastInterface<IDWriteFontCollection>()->FindFamilyName(familyName, &index, &exists));
    
    return exists ? true : false;
}

int FontFamilyCollection::IndexOf(String^ fontFamilyName)
{
    if (String::IsNullOrEmpty(fontFamilyName))
    {
        throw gcnew ArgumentNullException("fontFamilyName", "fontFamilyName cannot be null or empty.");
    }

    UINT32 index;
    BOOL exists;
    pin_ptr<const WCHAR> familyName = PtrToStringChars(fontFamilyName);

    Validate::VerifyResult(CastInterface<IDWriteFontCollection>()->FindFamilyName(familyName, &index, &exists));
    
    if (exists)
    {
        return index;
    }
    else
    {
        return -1;
    }
}

FontFamilyEnum::FontFamilyEnum(IDWriteFontCollection* pFontCollection, FontFamilyCollection^ familyCollection) : DirectUnknown(pFontCollection), m_current(-1), m_familyCollection(familyCollection)
{ 
    CastInterface<IUnknown>()->AddRef();
}

FontFamily^ FontFamilyEnum::Current::get()
{ 
    return m_familyCollection[m_current];
}
        
System::Object^ FontFamilyEnum::CurrentObject::get()
{
    throw gcnew NotImplementedException();
}

 bool FontFamilyEnum::MoveNext()
{
    if (m_current + 1 < static_cast<int>(m_familyCollection->Count))
    {
        m_current++;
        return true;
    }

    return false;
}

void FontFamilyEnum::Reset()
{
    m_current = -1;
}
