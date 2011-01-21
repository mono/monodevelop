// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DWriteTypography.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

FontFeature TypographyList::Current::get()
{ 
    DWRITE_FONT_FEATURE tempFeature;
    Validate::VerifyResult(
        nativeInterface->GetFontFeature((UINT) m_current, &tempFeature
        ));

    FontFeature fontfeature;
    fontfeature.CopyFrom(tempFeature);

    return fontfeature;
}
        
System::Object^ TypographyList::CurrentObject::get()
{
    throw gcnew NotImplementedException();
}

 bool TypographyList::MoveNext()
{
    if (m_current + 1 < static_cast<int>(nativeInterface->GetFontFeatureCount()))
    {
        m_current++;
        return true;
    }

    return false;
}

void TypographyList::Reset()
{
    m_current = -1;
}


FontFeature TypographySettingCollection::default::get(int index)
{
    DWRITE_FONT_FEATURE tempFeature;
    Validate::VerifyResult(
        CastInterface<IDWriteTypography>()->GetFontFeature((UINT) index, &tempFeature
        ));

    FontFeature fontfeature;
    fontfeature.CopyFrom(tempFeature);

    return fontfeature;
}

void TypographySettingCollection::default::set(int /* i */, FontFeature /* feature */)
{
    // REVIEW: should support? if not, why have the setter at all?
    // Need a better exception message, and may want to make the
    // setter an explicit implementation of the IList<T>.Item
    // property if that's possible in C++ (or whole property if not
    // and appropriate)
    throw gcnew NotSupportedException();
}

void TypographySettingCollection::Add(FontFeature feature)
{
    DWRITE_FONT_FEATURE tempFeature;
    feature.CopyTo(&tempFeature);
    Validate::VerifyResult(
        CastInterface<IDWriteTypography>()->AddFontFeature(tempFeature));
}

int TypographySettingCollection::Count::get()
{
    return static_cast<int>(CastInterface<IDWriteTypography>()->GetFontFeatureCount()); 
}

bool TypographySettingCollection::IsReadOnly::get()
{
    return false;
}

System::Collections::Generic::IEnumerator<FontFeature>^ TypographySettingCollection::GetGenericEnumerator(void)
{
    return gcnew TypographyList(CastInterface<IDWriteTypography>());
}


System::Collections::IEnumerator^ TypographySettingCollection::GetEnumerator(void)
{
    return GetGenericEnumerator();
}

int TypographySettingCollection::IndexOf(FontFeature /* item */)
{
    throw gcnew NotSupportedException();
}

void TypographySettingCollection::Insert(int /* index */, FontFeature /* item */)
{
    throw gcnew NotSupportedException();
}

void TypographySettingCollection::RemoveAt(int /* index */)
{
    throw gcnew NotSupportedException();
}

void TypographySettingCollection::Clear()
{
    throw gcnew NotSupportedException();
}

bool TypographySettingCollection::Contains(FontFeature /* item */)
{
        throw gcnew NotSupportedException();
}

void TypographySettingCollection::CopyTo(array<FontFeature>^ /* fontFeatureArray */, int /* arrayIndex */)
{
        throw gcnew NotSupportedException();
}

bool TypographySettingCollection::Remove(FontFeature /* item */)
{
    throw gcnew NotSupportedException();
}



