// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"

#include "CommonUtils.h"
#include "LibraryLoader.h"
#include "DWriteFactory.h"
#include "DWriteTextFormat.h"
#include "DWriteTextLayout.h"
#include "ICustomInlineObject.h"
#include "DWriteInlineObject.h"
#include "DWriteRenderingParams.h"
#include "DWriteFontFamily.h"
#include "DWriteTypography.h"
#include "DWriteFontFamilyCollection.h"
#include "DWriteFontFace.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::DirectWrite;

typedef HRESULT (WINAPI *CreateDWriteFactoryFuncPtr)(DWRITE_FACTORY_TYPE factoryType, REFIID riid, IUnknown **factory);

DWriteFactory^ DWriteFactory::CreateFactory()
{
    return CreateFactory(DWriteFactoryType::Shared);
}

DWriteFactory^ DWriteFactory::CreateFactory(DWriteFactoryType factoryType)
{
    CreateDWriteFactoryFuncPtr createFuncPtr = 
        (CreateDWriteFactoryFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
        DWriteLibrary, "DWriteCreateFactory");

    IDWriteFactory * pNativeIDWriteFactory = NULL;

    Validate::VerifyResult(
        (*createFuncPtr)(
        static_cast<DWRITE_FACTORY_TYPE>(factoryType), __uuidof(IDWriteFactory), (IUnknown**)(&pNativeIDWriteFactory)));

    return gcnew DWriteFactory(pNativeIDWriteFactory);
}


TextFormat^ DWriteFactory::CreateTextFormat(
    String^ fontFamilyName,
    Single fontSize,
    FontWeight fontWeight,
    FontStyle fontStyle,
    FontStretch fontStretch,
    CultureInfo^ cultureInfo)
{
    if (fontFamilyName == nullptr)
    {
        throw gcnew ArgumentNullException("fontFamilyName");
    }

    IDWriteTextFormat * textFormat = NULL;

    pin_ptr<const wchar_t> ciNamePtr = cultureInfo == nullptr ?  nullptr : PtrToStringChars(cultureInfo->Name);
    pin_ptr<const wchar_t> familyNamePtr = PtrToStringChars(fontFamilyName);

    Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateTextFormat(        
        familyNamePtr,
        NULL,
        static_cast<DWRITE_FONT_WEIGHT>(fontWeight),
        static_cast<DWRITE_FONT_STYLE>(fontStyle),
        static_cast<DWRITE_FONT_STRETCH>(fontStretch),
        fontSize,
        cultureInfo == nullptr ? L"" : ciNamePtr,            
        &textFormat));

    return textFormat ? gcnew TextFormat(textFormat) : nullptr;
}

TextFormat^ DWriteFactory::CreateTextFormat(
    String^ fontFamilyName,
    Single fontSize,
    FontWeight fontWeight,
    FontStyle fontStyle,
    FontStretch fontStretch)
{
    return CreateTextFormat(fontFamilyName, fontSize, fontWeight, fontStyle, fontStretch, nullptr);
}

TextFormat^ DWriteFactory::CreateTextFormat(
    String^ fontFamilyName,
    Single fontSize)
{
    return CreateTextFormat(fontFamilyName, fontSize, FontWeight::Normal, FontStyle::Normal, FontStretch::Normal, nullptr);
}



RenderingParams^ DWriteFactory::CreateRenderingParams()
{
    IDWriteRenderingParams * renderingParams = NULL;

    Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateRenderingParams(
        &renderingParams));

    return renderingParams ? gcnew RenderingParams(renderingParams) : nullptr;
}

RenderingParams^ DWriteFactory::CreateMonitorRenderingParams(
    IntPtr monitorHandle
    )
{
    IDWriteRenderingParams * renderingParams = NULL;

    Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateMonitorRenderingParams(
        static_cast<HMONITOR>(monitorHandle.ToPointer()),
        &renderingParams));

    return renderingParams ? gcnew RenderingParams(renderingParams) : nullptr;
}

RenderingParams^ DWriteFactory::CreateCustomRenderingParams(
    FLOAT gamma,
    FLOAT enhancedContrast,
    FLOAT clearTypeLevel,
    PixelGeometry pixelGeometry,
    RenderingMode renderingMode)
{
    IDWriteRenderingParams * renderingParams = NULL;

    Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateCustomRenderingParams(
        gamma,
        enhancedContrast,
        clearTypeLevel,
        static_cast<DWRITE_PIXEL_GEOMETRY>(pixelGeometry),
        static_cast<DWRITE_RENDERING_MODE>(renderingMode),
        &renderingParams));

    return renderingParams ? gcnew RenderingParams(renderingParams) : nullptr;
}

TextLayout^ DWriteFactory::CreateTextLayout(
    String^ text,
    TextFormat^ textFormat,
    FLOAT maxWidth,
    FLOAT maxHeight
    )
{
    if (text == nullptr)
    {
        throw gcnew ArgumentNullException("text");
    }

    IDWriteTextLayout* textLayout = NULL;

    pin_ptr<const wchar_t> textPtr = PtrToStringChars(text);

    Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateTextLayout(
        textPtr, text->Length, textFormat->CastInterface<IDWriteTextFormat>(),
        maxWidth, maxHeight, &textLayout));

    return textLayout ? gcnew TextLayout(textLayout, text) : nullptr;
}

TextLayout^ DWriteFactory::CreateGdiCompatibleTextLayout(
    String^ text,
    TextFormat^ textFormat,
    FLOAT layoutWidth,
    FLOAT layoutHeight,
    FLOAT pixelsPerDip,
    Boolean useGdiNatural,
    Microsoft::WindowsAPICodePack::DirectX::Direct2D1::Matrix3x2F transform        
    )
{
    if (text == nullptr)
    {
        throw gcnew ArgumentNullException("text");
    }

    IDWriteTextLayout* textLayout = NULL;

    pin_ptr<const wchar_t> textPtr = PtrToStringChars(text);

    DWRITE_MATRIX matrixCopy;
    transform.CopyTo((D2D1_MATRIX_3X2_F*)&matrixCopy);

    Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateGdiCompatibleTextLayout(
        textPtr, text->Length, textFormat->CastInterface<IDWriteTextFormat>(),
        layoutWidth, layoutHeight, pixelsPerDip, &matrixCopy, useGdiNatural ? TRUE : FALSE, &textLayout));

    return textLayout ? gcnew TextLayout(textLayout, text) : nullptr;
}

TextLayout^ DWriteFactory::CreateGdiCompatibleTextLayout(
    String^ text,
    TextFormat^ textFormat,
    FLOAT layoutWidth,
    FLOAT layoutHeight,
    FLOAT pixelsPerDip,
    Boolean useGdiNatural
    )
{
    if (text == nullptr)
    {
        throw gcnew ArgumentNullException("text");
    }

    IDWriteTextLayout* textLayout = NULL;
    pin_ptr<const wchar_t> textPtr = PtrToStringChars(text);

    Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateGdiCompatibleTextLayout(
        textPtr, text->Length,textFormat->CastInterface<IDWriteTextFormat>(),
        layoutWidth, layoutHeight, pixelsPerDip, NULL, useGdiNatural, &textLayout));

    return textLayout ? gcnew TextLayout(textLayout, text) : nullptr;
}

InlineObject^ DWriteFactory::CreateEllipsisTrimmingSign(
    TextFormat^ textFormat
    )
{
    ::IDWriteInlineObject* inlineObject = NULL;

    Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateEllipsisTrimmingSign(
        textFormat->CastInterface<IDWriteTextFormat>(),
        &inlineObject));

    return inlineObject ? gcnew InlineObject(inlineObject) : nullptr;
}



FontFamilyCollection^ DWriteFactory::SystemFontFamilyCollection::get()
{
    // First check the font collection for updates
    IDWriteFontCollection* newFontCollection = NULL;
    Validate::VerifyResult(CastInterface<IDWriteFactory>()->GetSystemFontCollection(&newFontCollection));

    // Check for updates
    if (m_sysFontFamilies == nullptr || newFontCollection != m_fontCollection)
    {
        m_fontCollection = newFontCollection;
        m_sysFontFamilies = gcnew FontFamilyCollection(m_fontCollection);
    }

    return m_sysFontFamilies;
}

TypographySettingCollection^ DWriteFactory::CreateTypography()
{
    IDWriteTypography * typography = NULL;

    Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateTypography(
        &typography
        ));

    return typography ? gcnew TypographySettingCollection(typography) : nullptr;
}


FontFace^ DWriteFactory::CreateFontFaceFromFontFile(
    System::String^ fontFileName, 
    FontFaceType fontFaceType, 
    UINT32 faceIndex,
    FontSimulations fontSimulations)
{ 
    if (!File::Exists(fontFileName))
    {
        throw gcnew FileNotFoundException("Could not find font file \"" + fontFileName + "\"");
    }

    IDWriteFontFace* pFontFace = NULL;
    IDWriteFontFile* pFontFiles = NULL;

    pin_ptr<const wchar_t> fontFolderPath = PtrToStringChars(Path::GetFullPath(fontFileName));

    Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateFontFileReference(
        fontFolderPath,
        NULL,
        &pFontFiles));

    IDWriteFontFile* fontFileArray[] = {pFontFiles};

    try
    {
        Validate::VerifyResult(CastInterface<IDWriteFactory>()->CreateFontFace(
            static_cast<DWRITE_FONT_FACE_TYPE>(fontFaceType),
            1, // file count, this method supports a single file
            fontFileArray,
            faceIndex,
            static_cast<DWRITE_FONT_SIMULATIONS>(fontSimulations),
            &pFontFace
            ));
    }
    finally
    {
        if (pFontFiles)
        {
            pFontFiles->Release();
        }
    }

    return pFontFace ? gcnew FontFace(pFontFace) : nullptr;
}

FontFace^ DWriteFactory::CreateFontFaceFromFontFile(System::String^ fontFileName)
{
    return CreateFontFaceFromFontFile(fontFileName, FontFaceType::TrueType, 0, FontSimulations::None);
}

