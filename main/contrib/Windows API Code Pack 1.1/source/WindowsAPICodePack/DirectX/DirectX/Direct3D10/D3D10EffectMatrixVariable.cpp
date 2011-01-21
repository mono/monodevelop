// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectMatrixVariable.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

Matrix4x4F EffectMatrixVariable::Matrix::get()
{
    Matrix4x4F matrix;
    pin_ptr<FLOAT> pinnedArray = &matrix.dangerousFirstField;

    Validate::VerifyResult(CastInterface<ID3D10EffectMatrixVariable>()->GetMatrix(pinnedArray));
    return matrix;
}

array<Matrix4x4F>^ EffectMatrixVariable::GetMatrixArray(UInt32 offset, UInt32 count)
{
    array<Matrix4x4F>^ matrix = gcnew array<Matrix4x4F>(count);
    pin_ptr<FLOAT> pinnedArray = &matrix[0].dangerousFirstField;

    Validate::VerifyResult(CastInterface<ID3D10EffectMatrixVariable>()->GetMatrixArray(pinnedArray, offset, count));
    return matrix;
}

Matrix4x4F EffectMatrixVariable::MatrixTranspose::get()
{
    Matrix4x4F matrix;
    pin_ptr<FLOAT> pinnedArray = &matrix.dangerousFirstField;

    Validate::VerifyResult(CastInterface<ID3D10EffectMatrixVariable>()->GetMatrixTranspose(pinnedArray));
    return matrix;
}

array<Matrix4x4F>^ EffectMatrixVariable::GetMatrixTransposeArray(UInt32 offset, UInt32 count)
{
    array<Matrix4x4F>^ matrix = gcnew array<Matrix4x4F>(count);
    pin_ptr<FLOAT> pinnedArray = &matrix[0].dangerousFirstField;

    Validate::VerifyResult(CastInterface<ID3D10EffectMatrixVariable>()->GetMatrixTransposeArray(pinnedArray, offset, count));    
    return matrix;
}

void EffectMatrixVariable::Matrix::set(Matrix4x4F data)
{
    pin_ptr<FLOAT> matrix = &data.dangerousFirstField;
    Validate::VerifyResult(CastInterface<ID3D10EffectMatrixVariable>()->SetMatrix(matrix));
}

void EffectMatrixVariable::SetMatrixArray(array<Matrix4x4F>^ data, UInt32 offset)
{
    pin_ptr<FLOAT> matrix = &data[0].dangerousFirstField;
    Validate::VerifyResult(CastInterface<ID3D10EffectMatrixVariable>()->SetMatrixArray(matrix, static_cast<UINT>(offset), static_cast<UINT>(data->Rank)));
}

void EffectMatrixVariable::MatrixTranspose::set(Matrix4x4F data)
{
    pin_ptr<FLOAT> matrix= &data.dangerousFirstField;
    Validate::VerifyResult(CastInterface<ID3D10EffectMatrixVariable>()->SetMatrixTranspose(matrix));
}

void EffectMatrixVariable::SetMatrixTransposeArray(array<Matrix4x4F>^ data, UInt32 offset)
{
    pin_ptr<FLOAT> matrix = &data[0].dangerousFirstField;
    Validate::VerifyResult(CastInterface<ID3D10EffectMatrixVariable>()->SetMatrixTransposeArray(matrix, static_cast<UINT>(offset), static_cast<UINT>(data->Rank)));
}

