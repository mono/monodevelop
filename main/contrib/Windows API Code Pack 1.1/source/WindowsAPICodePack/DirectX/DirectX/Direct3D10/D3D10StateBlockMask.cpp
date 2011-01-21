// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10StateBlockMask.h"
#include "LibraryLoader.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

typedef HRESULT (WINAPI *D3D10StateBlockMaskDisableAllFuncPtr)(
    D3D10_STATE_BLOCK_MASK *pStateBlockMask
);

typedef HRESULT (WINAPI *D3D10StateBlockMaskDisableCaptureFuncPtr)(
  D3D10_STATE_BLOCK_MASK *pMask,
  D3D10_DEVICE_STATE_TYPES StateType,
  UINT RangeStart,
  UINT RangeLength
);

typedef HRESULT (WINAPI *D3D10StateBlockMaskEnableAllFuncPtr)(
  D3D10_STATE_BLOCK_MASK *pMask
);

typedef HRESULT (WINAPI *D3D10StateBlockMaskEnableCaptureFuncPtr)(
  D3D10_STATE_BLOCK_MASK *pMask,
  D3D10_DEVICE_STATE_TYPES StateType,
  UINT RangeStart,
  UINT RangeLength
);

typedef HRESULT (WINAPI *D3D10StateBlockMaskDifferenceFuncPtr)(
  D3D10_STATE_BLOCK_MASK *pA,
  D3D10_STATE_BLOCK_MASK *pB,
  D3D10_STATE_BLOCK_MASK *pResult
);

typedef HRESULT (WINAPI *D3D10StateBlockMaskIntersectFuncPtr)(
  D3D10_STATE_BLOCK_MASK *pA,
  D3D10_STATE_BLOCK_MASK *pB,
  D3D10_STATE_BLOCK_MASK *pResult
);

typedef HRESULT (WINAPI *D3D10StateBlockMaskUnionFuncPtr)(
  D3D10_STATE_BLOCK_MASK *pA,
  D3D10_STATE_BLOCK_MASK *pB,
  D3D10_STATE_BLOCK_MASK *pResult
);

StateBlockMask^ StateBlockMask::DisableAll(void)
{
    D3D10_STATE_BLOCK_MASK nativeMask;

    CopyTo(nativeMask);

    D3D10StateBlockMaskDisableAllFuncPtr disableAllFuncPtr = 
        (D3D10StateBlockMaskDisableAllFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10StateBlockMaskDisableAll");

    Validate::VerifyResult((*disableAllFuncPtr)(&nativeMask));

    return gcnew StateBlockMask(nativeMask);
}

StateBlockMask^ StateBlockMask::DisableCapture(DeviceStateType type, int start, int length)
{
    D3D10_STATE_BLOCK_MASK nativeMask;

    CopyTo(nativeMask);

    D3D10StateBlockMaskDisableCaptureFuncPtr disableCaptureFuncPtr = 
        (D3D10StateBlockMaskDisableCaptureFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10StateBlockMaskDisableCapture");

    Validate::VerifyResult((*disableCaptureFuncPtr)(&nativeMask,
        static_cast<D3D10_DEVICE_STATE_TYPES>(type),
        static_cast<UINT>(start), static_cast<UINT>(length)));

    return gcnew StateBlockMask(nativeMask);
}

StateBlockMask^ StateBlockMask::EnableAll(void)
{
    D3D10_STATE_BLOCK_MASK nativeMask;

    CopyTo(nativeMask);

    D3D10StateBlockMaskDisableAllFuncPtr disableAllFuncPtr = 
        (D3D10StateBlockMaskDisableAllFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10StateBlockMaskDisableAll");

    Validate::VerifyResult((*disableAllFuncPtr)(&nativeMask));

    return gcnew StateBlockMask(nativeMask);
}

StateBlockMask^ StateBlockMask::EnableCapture(DeviceStateType type, int start, int length)
{
    D3D10_STATE_BLOCK_MASK nativeMask;

    CopyTo(nativeMask);

    D3D10StateBlockMaskEnableCaptureFuncPtr enableCaptureFuncPtr = 
        (D3D10StateBlockMaskEnableCaptureFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10StateBlockMaskEnableCapture");

    Validate::VerifyResult((*enableCaptureFuncPtr)(&nativeMask,
        static_cast<D3D10_DEVICE_STATE_TYPES>(type),
        static_cast<UINT>(start), static_cast<UINT>(length)));

    return gcnew StateBlockMask(nativeMask);
}

StateBlockMask^ StateBlockMask::operator ^(StateBlockMask^ mask1, StateBlockMask^ mask2)
{
    if (mask1 == nullptr)
    {
        throw gcnew ArgumentNullException("mask1");
    }

    if (mask2 == nullptr)
    {
        throw gcnew ArgumentNullException("mask2");
    }

    D3D10_STATE_BLOCK_MASK resultMask;

    D3D10StateBlockMaskDifferenceFuncPtr differenceFuncPtr = 
        (D3D10StateBlockMaskDifferenceFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10StateBlockMaskDifference");

    Validate::VerifyResult((*differenceFuncPtr)(
        mask1->nativeObject.Get(), mask2->nativeObject.Get(), &resultMask));

    return gcnew StateBlockMask(resultMask);
}

StateBlockMask^ StateBlockMask::operator &(StateBlockMask^ mask1, StateBlockMask^ mask2)
{
    if (mask1 == nullptr)
    {
        throw gcnew ArgumentNullException("mask1");
    }

    if (mask2 == nullptr)
    {
        throw gcnew ArgumentNullException("mask2");
    }

    D3D10_STATE_BLOCK_MASK resultMask;

    D3D10StateBlockMaskIntersectFuncPtr intersectFuncPtr = 
        (D3D10StateBlockMaskIntersectFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10StateBlockMaskIntersect");

    Validate::VerifyResult((*intersectFuncPtr)(
        mask1->nativeObject.Get(), mask2->nativeObject.Get(), &resultMask));

    return gcnew StateBlockMask(resultMask);
}

StateBlockMask^ StateBlockMask::operator |(StateBlockMask^ mask1, StateBlockMask^ mask2)
{
    if (mask1 == nullptr)
    {
        throw gcnew ArgumentNullException("mask1");
    }

    if (mask2 == nullptr)
    {
        throw gcnew ArgumentNullException("mask2");
    }

    D3D10_STATE_BLOCK_MASK resultMask;

    D3D10StateBlockMaskUnionFuncPtr unionFuncPtr = 
        (D3D10StateBlockMaskUnionFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10StateBlockMaskUnion");

    Validate::VerifyResult((*unionFuncPtr)(
        mask1->nativeObject.Get(), mask2->nativeObject.Get(), &resultMask));

    return gcnew StateBlockMask(resultMask);
}