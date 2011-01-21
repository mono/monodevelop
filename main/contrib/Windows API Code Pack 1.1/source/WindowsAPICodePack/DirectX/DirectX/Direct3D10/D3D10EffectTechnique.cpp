// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectTechnique.h"

#include "D3D10EffectVariable.h"
#include "D3D10EffectPass.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace msclr::interop;


StateBlockMask^ EffectTechnique::ComputeStateBlockMask()
{
    D3D10_STATE_BLOCK_MASK stateBlockMask;

    Validate::VerifyResult(CastInterface<ID3D10EffectTechnique>()->ComputeStateBlockMask(&stateBlockMask));

    return gcnew StateBlockMask(stateBlockMask);
}

EffectVariable^ EffectTechnique::GetAnnotationByIndex(UInt32 index)
{
    ID3D10EffectVariable* returnValue = CastInterface<ID3D10EffectTechnique>()->GetAnnotationByIndex(static_cast<UINT>(index));
    return gcnew EffectVariable(returnValue);
}

EffectVariable^ EffectTechnique::GetAnnotationByName(String^ name)
{ 
    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        ID3D10EffectVariable * returnValue = CastInterface<ID3D10EffectTechnique>()->GetAnnotationByName(static_cast<char*>(ptr.ToPointer()));

        return returnValue ? gcnew EffectVariable(returnValue, false) : nullptr;
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

TechniqueDescription EffectTechnique::Description::get()
{
    D3D10_TECHNIQUE_DESC desc = {0};

    Validate::VerifyResult(CastInterface<ID3D10EffectTechnique>()->GetDesc(&desc));

    return TechniqueDescription(desc);
}

EffectPass^ EffectTechnique::GetPassByIndex(UInt32 index)
{
    ID3D10EffectPass* returnValue = CastInterface<ID3D10EffectTechnique>()->GetPassByIndex(static_cast<UINT>(index));
    return returnValue != NULL ? gcnew EffectPass(returnValue) : nullptr;
}

EffectPass^ EffectTechnique::GetPassByName(String^ name)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        ID3D10EffectPass * returnValue = CastInterface<ID3D10EffectTechnique>()->GetPassByName(static_cast<char*>(ptr.ToPointer()));

        return returnValue != NULL ? gcnew EffectPass(returnValue) : nullptr;
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

Boolean EffectTechnique::IsValid::get()
{
    return CastInterface<ID3D10EffectTechnique>()->IsValid() != 0;
}

