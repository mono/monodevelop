// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11DeviceContext.h"

#include "D3D11Asynchronous.h"
#include "D3D11DepthStencilView.h"
#include "D3D11RenderTargetView.h"
#include "D3D11UnorderedAccessView.h"
#include "D3D11Resource.h"
#include "D3D11Buffer.h"
#include "D3D11SamplerState.h"
#include "D3D11ComputeShader.h"
#include "D3D11ClassInstance.h"
#include "D3D11ShaderResourceView.h"
#include "D3D11DomainShader.h"
#include "D3D11CommandList.h"
#include "D3D11Predicate.h"
#include "D3D11GeometryShader.h"
#include "D3D11HullShader.h"
#include "D3D11InputLayout.h"
#include "D3D11BlendState.h"
#include "D3D11DepthStencilState.h"
#include "D3D11PixelShader.h"
#include "D3D11RasterizerState.h"
#include "D3D11VertexShader.h"

#include "D3D11ComputeShaderPipelineStage.h"
#include "D3D11DomainShaderPipelineStage.h"
#include "D3D11GeometryShaderPipelineStage.h"
#include "D3D11HullShaderPipelineStage.h"
#include "D3D11InputAssemblerPipelineStage.h"
#include "D3D11OutputMergerPipelineStage.h"
#include "D3D11PixelShaderPipelineStage.h"
#include "D3D11RasterizerPipelineStage.h"
#include "D3D11StreamOutputPipelineStage.h"
#include "D3D11VertexShaderPipelineStage.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

void DeviceContext::Begin(Asynchronous^ asyncData)
{
    CastInterface<ID3D11DeviceContext>()->Begin(asyncData->CastInterface<ID3D11Asynchronous>());
}

void DeviceContext::ClearDepthStencilView(DepthStencilView^ depthStencilView, ClearOptions clearOptions, Single depth, Byte stencil)
{
    CastInterface<ID3D11DeviceContext>()->ClearDepthStencilView(depthStencilView->CastInterface<ID3D11DepthStencilView>(), static_cast<UINT>(clearOptions), static_cast<FLOAT>(depth), static_cast<UINT8>(stencil));
}

void DeviceContext::ClearRenderTargetView(RenderTargetView^ renderTargetView, ColorRgba colorRgba)
{
    pin_ptr<ColorRgba> rgba = &colorRgba;
    CastInterface<ID3D11DeviceContext>()->ClearRenderTargetView(renderTargetView->CastInterface<ID3D11RenderTargetView>(), (float*)rgba);
}

void DeviceContext::ClearState()
{
    CastInterface<ID3D11DeviceContext>()->ClearState();
}

void DeviceContext::ClearUnorderedAccessViewFloat(UnorderedAccessView^ unorderedAccessView, array<Single>^ values)
{
    if (values == nullptr)
    {
        throw gcnew ArgumentNullException("values");
    }
    if (values->Length != 4)
    {
        throw gcnew ArgumentOutOfRangeException("values", "Array length must be 4.");
    }

    pin_ptr<FLOAT> arr = &values[0];

    CastInterface<ID3D11DeviceContext>()->ClearUnorderedAccessViewFloat(unorderedAccessView->CastInterface<ID3D11UnorderedAccessView>(), arr);
}

void DeviceContext::ClearUnorderedAccessViewUInt32(UnorderedAccessView^ unorderedAccessView, array<UInt32>^ values)
{
    if (values == nullptr)
    {
        throw gcnew ArgumentNullException("values");
    }
    if (values->Length != 4)
    {
        throw gcnew ArgumentOutOfRangeException("values", "Array length must be 4.");
    }

    pin_ptr<UINT> arr = &values[0];

    CastInterface<ID3D11DeviceContext>()->ClearUnorderedAccessViewUint(unorderedAccessView->CastInterface<ID3D11UnorderedAccessView>(), arr);
}

void DeviceContext::CopyResource(D3DResource^ destinationResource, D3DResource^ sourceResource)
{
    CastInterface<ID3D11DeviceContext>()->CopyResource(destinationResource->CastInterface<ID3D11Resource>(), sourceResource->CastInterface<ID3D11Resource>());
}

void DeviceContext::CopySubresourceRegion(D3DResource^ destinationResource, UInt32 destinationSubresource, UInt32 DstX, UInt32 DstY, UInt32 DstZ, D3DResource^ sourceResource, UInt32 sourceSubresource, Box SrcBox)
{
    pin_ptr<Box> boxPtr = &SrcBox;
    CastInterface<ID3D11DeviceContext>()->CopySubresourceRegion(destinationResource->CastInterface<ID3D11Resource>(), static_cast<UINT>(destinationSubresource), static_cast<UINT>(DstX), static_cast<UINT>(DstY), static_cast<UINT>(DstZ), sourceResource->CastInterface<ID3D11Resource>(), static_cast<UINT>(sourceSubresource), (D3D11_BOX*)boxPtr);
}

void DeviceContext::CopySubresourceRegion(D3DResource^ destinationResource, UInt32 destinationSubresource, UInt32 DstX, UInt32 DstY, UInt32 DstZ, D3DResource^ sourceResource, UInt32 sourceSubresource)
{
    CastInterface<ID3D11DeviceContext>()->CopySubresourceRegion(destinationResource->CastInterface<ID3D11Resource>(), static_cast<UINT>(destinationSubresource), static_cast<UINT>(DstX), static_cast<UINT>(DstY), static_cast<UINT>(DstZ), sourceResource->CastInterface<ID3D11Resource>(), static_cast<UINT>(sourceSubresource), NULL);
}
void DeviceContext::Dispatch(UInt32 threadGroupCountX, UInt32 threadGroupCountY, UInt32 threadGroupCountZ)
{
    CastInterface<ID3D11DeviceContext>()->Dispatch(static_cast<UINT>(threadGroupCountX), static_cast<UINT>(threadGroupCountY), static_cast<UINT>(threadGroupCountZ));
}

void DeviceContext::DispatchIndirect(D3DBuffer^ bufferForArgs, UInt32 alignedOffsetForArgs)
{
    CastInterface<ID3D11DeviceContext>()->DispatchIndirect(bufferForArgs->CastInterface<ID3D11Buffer>(), static_cast<UINT>(alignedOffsetForArgs));
}

void DeviceContext::Draw(UInt32 vertexCount, UInt32 startVertexLocation)
{
    CastInterface<ID3D11DeviceContext>()->Draw(static_cast<UINT>(vertexCount), static_cast<UINT>(startVertexLocation));
}

void DeviceContext::DrawAuto()
{
    CastInterface<ID3D11DeviceContext>()->DrawAuto();
}

void DeviceContext::DrawIndexed(UInt32 indexCount, UInt32 startIndexLocation, Int32 baseVertexLocation)
{
    CastInterface<ID3D11DeviceContext>()->DrawIndexed(static_cast<UINT>(indexCount), static_cast<UINT>(startIndexLocation), safe_cast<INT>(baseVertexLocation));
}

void DeviceContext::DrawIndexedInstanced(UInt32 indexCountPerInstance, UInt32 instanceCount, UInt32 startIndexLocation, Int32 baseVertexLocation, UInt32 startInstanceLocation)
{
    CastInterface<ID3D11DeviceContext>()->DrawIndexedInstanced(static_cast<UINT>(indexCountPerInstance), static_cast<UINT>(instanceCount), static_cast<UINT>(startIndexLocation), safe_cast<INT>(baseVertexLocation), static_cast<UINT>(startInstanceLocation));
}

void DeviceContext::DrawIndexedInstancedIndirect(D3DBuffer^ bufferForArgs, UInt32 offset)
{
    CastInterface<ID3D11DeviceContext>()->DrawIndexedInstancedIndirect(bufferForArgs->CastInterface<ID3D11Buffer>(), static_cast<UINT>(offset));
}

void DeviceContext::DrawInstanced(UInt32 vertexCountPerInstance, UInt32 instanceCount, UInt32 startVertexLocation, UInt32 startInstanceLocation)
{
    CastInterface<ID3D11DeviceContext>()->DrawInstanced(static_cast<UINT>(vertexCountPerInstance), static_cast<UINT>(instanceCount), static_cast<UINT>(startVertexLocation), static_cast<UINT>(startInstanceLocation));
}

void DeviceContext::DrawInstancedIndirect(D3DBuffer^ bufferForArgs, UInt32 offset)
{
    CastInterface<ID3D11DeviceContext>()->DrawInstancedIndirect(bufferForArgs->CastInterface<ID3D11Buffer>(), static_cast<UINT>(offset));
}

void DeviceContext::End(Asynchronous^ async)
{
    CastInterface<ID3D11DeviceContext>()->End(async->CastInterface<ID3D11Asynchronous>());
}

void DeviceContext::ExecuteCommandList(CommandList^ commandList, Boolean restoreContextState)
{
    CastInterface<ID3D11DeviceContext>()->ExecuteCommandList(commandList->CastInterface<ID3D11CommandList>(), safe_cast<BOOL>(restoreContextState));
}

CommandList^ DeviceContext::FinishCommandList(Boolean restoreDeferredContextState)
{
    ID3D11CommandList* tempCommandList;

    Validate::VerifyResult(CastInterface<ID3D11DeviceContext>()->FinishCommandList(restoreDeferredContextState ? TRUE : FALSE, &tempCommandList));

    return tempCommandList != NULL ? gcnew CommandList(tempCommandList) : nullptr;
}

void DeviceContext::Flush()
{
    CastInterface<ID3D11DeviceContext>()->Flush();
}

void DeviceContext::GenerateMips(ShaderResourceView^ shaderResourceView)
{
    CastInterface<ID3D11DeviceContext>()->GenerateMips(shaderResourceView->CastInterface<ID3D11ShaderResourceView>());
}

UInt32 DeviceContext::ContextOptions::get()
{
    return CastInterface<ID3D11DeviceContext>()->GetContextFlags();
}

void DeviceContext::GetData(Asynchronous^ asyncData, IntPtr data, UInt32 dataSize, AsyncGetDataOptions options)
{
    Validate::VerifyResult(CastInterface<ID3D11DeviceContext>()->GetData(asyncData->CastInterface<ID3D11Asynchronous>(), data.ToPointer(), static_cast<UINT>(dataSize), static_cast<UINT>(options)));
}

D3DPredicate^ DeviceContext::Predication::get(void)
{
    ID3D11Predicate* predicate;
    BOOL value;

    CastInterface<ID3D11DeviceContext>()->GetPredication(&predicate, &value);

    return predicate == NULL ? nullptr :
        gcnew D3DPredicate(predicate, value != FALSE, this);
}

void DeviceContext::Predication::set(D3DPredicate^ predicate)
{
    // REVIEW: current design is that any given D3DPredicate can be set
    // as the predicate for only one device context at a time. I think
    // that's fine design-wise (does Direct3D even allow a given predicate
    // instance to be set to more than one device context at a time?).
    //
    // But the current implementation of that policy means that setting the
    // DeviceContext's Predication property implicitly may remove a predicate
    // from some other DeviceContext. Is that really good behavior, or should
    // this method throw an exception when the predicate is already set to
    // some other device context (i.e. require the caller to explicitly set
    // the Context property).
    //
    // If we were going to throw an exception in that case, we would really
    // just get rid of this setter altogether. Then the question becomes: 
    // is it okay for the client code to retrieve multiple D3DPredicate instances
    // for the current device context, and then apply one to a different
    // device context, but then apply another to the current one? In that
    // scenario, the same underlying native interface winds up being applied
    // to two different device contexts, via different managed objects.
    //
    // All this, to get rid of an 'out' parameter. Might have to revisit this.

    predicate->Context = this;
}

void DeviceContext::SetPredication(D3DPredicate^ predicate, Boolean predicateValue)
{
    // If the predicate's context is already this context, setting WhenTrue will
    // call ID3D11DeviceContext::SetPredicate(), otherwise setting Context will.

    predicate->WhenTrue = predicateValue;
    predicate->Context = this;
}

Single DeviceContext::GetResourceMinimumLevelOfDetail(D3DResource^ resource)
{
    float returnValue = CastInterface<ID3D11DeviceContext>()->GetResourceMinLOD(resource->CastInterface<ID3D11Resource>());
    return safe_cast<Single>(returnValue);
}

DeviceContextType DeviceContext::DeviceContextType::get(void)
{
    D3D11_DEVICE_CONTEXT_TYPE returnValue = CastInterface<ID3D11DeviceContext>()->GetType();
    return safe_cast<Direct3D11::DeviceContextType>(returnValue);
}

MappedSubresource DeviceContext::Map(D3DResource^ resource, UInt32 subresource, Direct3D11::Map mapType, Direct3D11::MapOptions mapOptions)
{
    D3D11_MAPPED_SUBRESOURCE mappedRes;

    Validate::VerifyResult(CastInterface<ID3D11DeviceContext>()->Map(
        resource->CastInterface<ID3D11Resource>(), 
        subresource, 
        static_cast<D3D11_MAP>(mapType), 
        static_cast<UINT>(mapOptions), 
        &mappedRes));

    return MappedSubresource(mappedRes);
}

void DeviceContext::ResolveSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, D3DResource^ sourceResource, UInt32 sourceSubresource, Format format)
{
    CastInterface<ID3D11DeviceContext>()->ResolveSubresource(destinationResource->CastInterface<ID3D11Resource>(), static_cast<UINT>(destinationSubresource), sourceResource->CastInterface<ID3D11Resource>(), static_cast<UINT>(sourceSubresource), static_cast<DXGI_FORMAT>(format));
}

void DeviceContext::SetResourceMinimumLevelOfDetail(D3DResource^ resource, Single minimum)
{
    CastInterface<ID3D11DeviceContext>()->SetResourceMinLOD(resource->CastInterface<ID3D11Resource>(), minimum);
}

void DeviceContext::Unmap(D3DResource^ resource, UInt32 subresource)
{
    CastInterface<ID3D11DeviceContext>()->Unmap(resource->CastInterface<ID3D11Resource>(), subresource);
}

void DeviceContext::UpdateSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, IntPtr sourceData, UInt32 sourceRowPitch, UInt32 sourceDepthPitch, Box DstBox)
{
    pin_ptr<Box> boxPtr = &DstBox;;
    CastInterface<ID3D11DeviceContext>()->UpdateSubresource(destinationResource->CastInterface<ID3D11Resource>(), static_cast<UINT>(destinationSubresource), (D3D11_BOX*)boxPtr, reinterpret_cast<LPVOID>(sourceData.ToPointer()), static_cast<UINT>(sourceRowPitch), static_cast<UINT>(sourceDepthPitch));
}

void DeviceContext::UpdateSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, IntPtr sourceData, UInt32 sourceRowPitch, UInt32 sourceDepthPitch)
{
    CastInterface<ID3D11DeviceContext>()->UpdateSubresource(destinationResource->CastInterface<ID3D11Resource>(), static_cast<UINT>(destinationSubresource), NULL, reinterpret_cast<LPVOID>(sourceData.ToPointer()), static_cast<UINT>(sourceRowPitch), static_cast<UINT>(sourceDepthPitch));
}

// The pipeline stage accessors
ComputeShaderPipelineStage^ DeviceContext::CS::get() 
{ 
    if (m_CS == nullptr) 
        m_CS = gcnew ComputeShaderPipelineStage(this); 
    
    return m_CS; 
}

DomainShaderPipelineStage^ DeviceContext::DS::get() 
{ 
    if (m_DS == nullptr) 
        m_DS = gcnew DomainShaderPipelineStage(this); 
    
    return m_DS; 
}

GeometryShaderPipelineStage^ DeviceContext::GS::get() 
{ 
    if (m_GS == nullptr) 
        m_GS = gcnew GeometryShaderPipelineStage(this); 
    
    return m_GS; 
}

HullShaderPipelineStage^ DeviceContext::HS::get() 
{ 
    if (m_HS == nullptr) 
        m_HS = gcnew HullShaderPipelineStage(this); 
    
    return m_HS; 
}

InputAssemblerPipelineStage^ DeviceContext::IA::get() 
{ 
    if (m_IA == nullptr) 
        m_IA = gcnew InputAssemblerPipelineStage(this); 
    
    return m_IA; 
}

OutputMergerPipelineStage^ DeviceContext::OM::get() 
{ 
    if (m_OM == nullptr) 
        m_OM = gcnew OutputMergerPipelineStage(this); 
    
    return m_OM; 
}

PixelShaderPipelineStage^ DeviceContext::PS::get() 
{ 
    if (m_PS == nullptr) 
        m_PS = gcnew PixelShaderPipelineStage(this); 
    
    return m_PS; 
}

RasterizerPipelineStage^ DeviceContext::RS::get() 
{ 
    if (m_RS == nullptr) 
        m_RS = gcnew RasterizerPipelineStage(this); 
    
    return m_RS; 
}

StreamOutputPipelineStage^ DeviceContext::SO::get() 
{ 
    if (m_SO == nullptr) 
        m_SO = gcnew StreamOutputPipelineStage(this); 
    
    return m_SO; 
}

VertexShaderPipelineStage^ DeviceContext::VS::get() 
{ 
    if (m_VS == nullptr) 
        m_VS = gcnew VertexShaderPipelineStage(this); 
    
    return m_VS; 
}
