// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectVariable.h"

#include "D3D10EffectBlendVariable.h"
#include "D3D10EffectConstantBuffer.h"
#include "D3D10EffectDepthStencilVariable.h"
#include "D3D10EffectDepthStencilViewVariable.h"
#include "D3D10EffectMatrixVariable.h"
#include "D3D10EffectRasterizerVariable.h"
#include "D3D10EffectRenderTargetViewVariable.h"
#include "D3D10EffectSamplerVariable.h"
#include "D3D10EffectScalarVariable.h"
#include "D3D10EffectShaderVariable.h"
#include "D3D10EffectShaderResourceVariable.h"
#include "D3D10EffectStringVariable.h"
#include "D3D10EffectVectorVariable.h"
#include "D3D10EffectVariable.h"
#include "D3D10EffectType.h"

using namespace msclr::interop;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

EffectBlendVariable^ EffectVariable::AsBlend::get()
{
    ID3D10EffectBlendVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsBlend();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectBlendVariable(returnValue);
}

EffectConstantBuffer^ EffectVariable::AsConstantBuffer::get()
{
    ID3D10EffectConstantBuffer* returnValue = CastInterface<ID3D10EffectVariable>()->AsConstantBuffer();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectConstantBuffer(returnValue);
}

EffectDepthStencilVariable^ EffectVariable::AsDepthStencil::get()
{
    ID3D10EffectDepthStencilVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsDepthStencil();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectDepthStencilVariable(returnValue);
}

EffectDepthStencilViewVariable^ EffectVariable::AsDepthStencilView::get()
{
    ID3D10EffectDepthStencilViewVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsDepthStencilView();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectDepthStencilViewVariable(returnValue);
}

EffectMatrixVariable^ EffectVariable::AsMatrix::get()
{
    ID3D10EffectMatrixVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsMatrix();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectMatrixVariable(returnValue);
}

EffectRasterizerVariable^ EffectVariable::AsRasterizer::get()
{
    ID3D10EffectRasterizerVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsRasterizer();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectRasterizerVariable(returnValue);
}

EffectRenderTargetViewVariable^ EffectVariable::AsRenderTargetView::get()
{
    ID3D10EffectRenderTargetViewVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsRenderTargetView();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectRenderTargetViewVariable(returnValue);
}

EffectSamplerVariable^ EffectVariable::AsSampler::get()
{
    ID3D10EffectSamplerVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsSampler();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectSamplerVariable(returnValue);
}

EffectScalarVariable^ EffectVariable::AsScalar::get()
{
    ID3D10EffectScalarVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsScalar();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectScalarVariable(returnValue);
}

EffectShaderVariable^ EffectVariable::AsShader::get()
{
    ID3D10EffectShaderVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsShader();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectShaderVariable(returnValue);
}

EffectShaderResourceVariable^ EffectVariable::AsShaderResource::get()
{
    ID3D10EffectShaderResourceVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsShaderResource();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectShaderResourceVariable(returnValue);
}

EffectStringVariable^ EffectVariable::AsString::get()
{
    ID3D10EffectStringVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsString();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectStringVariable(returnValue);
}

EffectVectorVariable^ EffectVariable::AsVector::get()
{
    ID3D10EffectVectorVariable* returnValue = CastInterface<ID3D10EffectVariable>()->AsVector();
    return (returnValue == NULL || !returnValue->IsValid()) ? nullptr : gcnew EffectVectorVariable(returnValue);
}

EffectVariable^ EffectVariable::GetAnnotationByIndex(UInt32 index)
{
    ID3D10EffectVariable* returnValue = CastInterface<ID3D10EffectVariable>()->GetAnnotationByIndex(static_cast<UINT>(index));
    return returnValue == NULL ? nullptr : gcnew EffectVariable(returnValue);
}

EffectVariable^ EffectVariable::GetAnnotationByName(String^ name)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        ID3D10EffectVariable * returnValue = CastInterface<ID3D10EffectVariable>()->GetAnnotationByName(static_cast<char*>(ptr.ToPointer()));

        return returnValue == NULL ? nullptr : gcnew EffectVariable(returnValue);
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

EffectVariableDescription EffectVariable::Description::get()
{
    D3D10_EFFECT_VARIABLE_DESC nativeDesc = {0};
    Validate::VerifyResult(CastInterface<ID3D10EffectVariable>()->GetDesc(&nativeDesc));

    return EffectVariableDescription(nativeDesc);
}

EffectVariable^ EffectVariable::GetElement(UInt32 index)
{
    ID3D10EffectVariable* returnValue = CastInterface<ID3D10EffectVariable>()->GetElement(static_cast<UINT>(index));
    return returnValue == NULL ? nullptr : gcnew EffectVariable(returnValue);
}

EffectVariable^ EffectVariable::GetMemberByIndex(UInt32 index)
{
    ID3D10EffectVariable* returnValue = CastInterface<ID3D10EffectVariable>()->GetMemberByIndex(static_cast<UINT>(index));
    return returnValue == NULL ? nullptr : gcnew EffectVariable(returnValue);
}

EffectVariable^ EffectVariable::GetMemberByName(String^ name)
{

    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        ID3D10EffectVariable * returnValue = CastInterface<ID3D10EffectVariable>()->GetMemberByName(static_cast<char*>(ptr.ToPointer()));

        return returnValue == NULL ? nullptr : gcnew EffectVariable(returnValue);
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

EffectVariable^ EffectVariable::GetMemberBySemantic(String^ semantic)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(semantic);

    try
    {
        ID3D10EffectVariable * returnValue = CastInterface<ID3D10EffectVariable>()->GetMemberBySemantic(static_cast<char*>(ptr.ToPointer()));

        return returnValue == NULL ? nullptr : gcnew EffectVariable(returnValue);
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

EffectConstantBuffer^ EffectVariable::ParentConstantBuffer::get(void)
{
    ID3D10EffectConstantBuffer* returnValue = CastInterface<ID3D10EffectVariable>()->GetParentConstantBuffer();
    return returnValue == NULL ? nullptr : gcnew EffectConstantBuffer(returnValue);
}


EffectType^ EffectVariable::EffectType::get(void)
{
    ID3D10EffectType* returnValue = CastInterface<ID3D10EffectVariable>()->GetType();
    return returnValue == NULL ? nullptr : gcnew Direct3D10::EffectType(returnValue);
}

Boolean EffectVariable::IsValid::get()
{
    return CastInterface<ID3D10EffectVariable>()->IsValid() != 0;
}

array<unsigned char>^ EffectVariable::GetRawValue(UInt32 count)
{
    if (count == 0)
    {
        return nullptr;
    }

    array<unsigned char>^ data = gcnew array<unsigned char>(count);
    pin_ptr<unsigned char> ptr = &data[0];
    Validate::VerifyResult(CastInterface<ID3D10EffectVariable>()->GetRawValue(
        (void*) ptr, 0, count));

    return data;
}

void EffectVariable::SetRawValue(array<unsigned char>^ data)
{
    pin_ptr<unsigned char> ptr = data != nullptr ? &data[0] : nullptr;

    Validate::VerifyResult(CastInterface<ID3D10EffectVariable>()->SetRawValue(
        data == nullptr ? NULL : (void*) ptr, 
        0, 
        data == nullptr ? 0 : data->Length));
}

