// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10InfoQueue.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace msclr::interop;

void InfoQueue::AddApplicationMessage(MessageSeverity severity, String^ description)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(description);

    try
    {
        Validate::VerifyResult(CastInterface<ID3D10InfoQueue>()->AddApplicationMessage(
            static_cast<D3D10_MESSAGE_SEVERITY>(severity), 
            static_cast<char*>(ptr.ToPointer())));
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

void InfoQueue::AddMessage(MessageCategory Category, MessageSeverity Severity, MessageId ID, String^ description)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(description);

    try
    {
        Validate::VerifyResult(CastInterface<ID3D10InfoQueue>()->AddMessage(
            static_cast<D3D10_MESSAGE_CATEGORY>(Category), 
            static_cast<D3D10_MESSAGE_SEVERITY>(Severity), 
            static_cast<D3D10_MESSAGE_ID>(ID), 
            static_cast<char*>(ptr.ToPointer())));
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

void InfoQueue::ClearStoredMessages()
{
    CastInterface<ID3D10InfoQueue>()->ClearStoredMessages();
}

Boolean InfoQueue::GetBreakOnCategory(MessageCategory Category)
{
    return CastInterface<ID3D10InfoQueue>()->GetBreakOnCategory(static_cast<D3D10_MESSAGE_CATEGORY>(Category)) != 0;
}

Boolean InfoQueue::GetBreakOnId(MessageId ID)
{
    return CastInterface<ID3D10InfoQueue>()->GetBreakOnID(static_cast<D3D10_MESSAGE_ID>(ID)) != 0;
}

Boolean InfoQueue::GetBreakOnSeverity(MessageSeverity Severity)
{
    return CastInterface<ID3D10InfoQueue>()->GetBreakOnSeverity(static_cast<D3D10_MESSAGE_SEVERITY>(Severity)) != 0;
}

Message InfoQueue::GetMessage(UInt64 messageIndex)
{
    // Get the size of the message
    SIZE_T messageLength = 0;
    Validate::VerifyResult(
        CastInterface<ID3D10InfoQueue>()->GetMessage(
        messageIndex, 
        NULL, 
        &messageLength));

    // Allocate space and get the message
    D3D10_MESSAGE * pMessage = (D3D10_MESSAGE*)malloc(messageLength);
    try
    {
        Validate::VerifyResult(CastInterface<ID3D10InfoQueue>()->GetMessage(
            messageIndex, 
            pMessage, 
            &messageLength));
        
        return Message(pMessage);
    }
    finally
    {
        free(pMessage);
    }

}

 bool InfoQueue::TryGetMessage(UInt64 messageIndex, [System::Runtime::InteropServices::Out]  Message % outMessage)
{
    // Get the size of the message
    SIZE_T messageLength = 0;
    if (FAILED(CastInterface<ID3D10InfoQueue>()->GetMessage(messageIndex, NULL, &messageLength)))
    {
        outMessage = Message(NULL);
        return false;
    }

    // Allocate space and get the message
    D3D10_MESSAGE * pMessage = (D3D10_MESSAGE*)malloc(messageLength);
    try
    {
        if (SUCCEEDED(CastInterface<ID3D10InfoQueue>()->GetMessage(messageIndex, pMessage, &messageLength)))
        {
            outMessage = Message(pMessage);
            return true;
        }
        else
        {
            outMessage = Message(NULL);
            return false;
        }
    }
    finally
    {
        free(pMessage);
    }
}

UInt64 InfoQueue::MessageCountLimit::get()
{
    return CastInterface<ID3D10InfoQueue>()->GetMessageCountLimit();
}

Boolean InfoQueue::MuteDebugOutput::get()
{
    return CastInterface<ID3D10InfoQueue>()->GetMuteDebugOutput() != 0;
}

UInt64 InfoQueue::AllowedByStorageFilterCount::get()
{
    return CastInterface<ID3D10InfoQueue>()->GetNumMessagesAllowedByStorageFilter();
}

UInt64 InfoQueue::DeniedByStorageFilterCount::get()
{
    return CastInterface<ID3D10InfoQueue>()->GetNumMessagesDeniedByStorageFilter();
}

UInt64 InfoQueue::DiscardedByMessageCountLimitCount::get()
{
    return CastInterface<ID3D10InfoQueue>()->GetNumMessagesDiscardedByMessageCountLimit();
}

UInt64 InfoQueue::StoredCount::get()
{
    return CastInterface<ID3D10InfoQueue>()->GetNumStoredMessages();
}

UInt64 InfoQueue::StoredAllowedByRetrievalFilterCount::get()
{
    return CastInterface<ID3D10InfoQueue>()->GetNumStoredMessagesAllowedByRetrievalFilter();
}

void InfoQueue::SetBreakOnCategory(MessageCategory Category, Boolean bEnable)
{
    Validate::VerifyResult(CastInterface<ID3D10InfoQueue>()->SetBreakOnCategory(static_cast<D3D10_MESSAGE_CATEGORY>(Category), bEnable? 1 : 0));
}

void InfoQueue::SetBreakOnId(MessageId ID, Boolean bEnable)
{
    Validate::VerifyResult(CastInterface<ID3D10InfoQueue>()->SetBreakOnID(static_cast<D3D10_MESSAGE_ID>(ID), bEnable ? 1 : 0));
}

void InfoQueue::SetBreakOnSeverity(MessageSeverity Severity, Boolean bEnable)
{
    Validate::VerifyResult(CastInterface<ID3D10InfoQueue>()->SetBreakOnSeverity(static_cast<D3D10_MESSAGE_SEVERITY>(Severity), safe_cast<BOOL>(bEnable)));
}

void InfoQueue::MessageCountLimit::set(UInt64 value)
{
    Validate::VerifyResult(CastInterface<ID3D10InfoQueue>()->SetMessageCountLimit(value));
}

void InfoQueue::MuteDebugOutput::set(Boolean value)
{
    CastInterface<ID3D10InfoQueue>()->SetMuteDebugOutput(value ? 1 : 0);
}

