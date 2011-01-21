// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX {

#pragma once

// XML comments can't be applied to templates
// <summary>
// An auto pointer style class supporting a simple void pointer
// Reserved for internal use only.
// </summary>
template <typename T>
private ref struct AutoPointer
{
internal:
    AutoPointer() : isDeletable(true) /* , target(0), isArray(false) - default values inited by runtime */
	{ }

    void Set(T* ptr)
    {
        Set(ptr, true, false);
    }

    void Set(T* ptr, bool deletable)
    {
        Set(ptr, deletable, false);
    }

    void SetArray(T* ptr, bool deletable)
    {
        Set(ptr, deletable, true);
    }

    T* operator->()
    {
        return target;
    }

    T* Get()
    {
        return target;
    }

protected:
    void Set(T* ptr, bool deletable, bool is_array)
    {
        target = ptr;
        isDeletable = deletable;
        isArray = is_array;

// To debug pointer allc/dealloc, define _DEBUG_AUTO_PTR
#ifdef _DEBUG_AUTO_PTR
        System::Diagnostics::Debug::WriteLine(
            System::String::Format("0x{0:X}\t{1}\t0\t{2}\tDeletable=\t{3}\tArray=\t{4}\tsetting target",
                (int)target,
                System::DateTime::Now.ToString("HH:mm:ss.fff"),
                T::typeid,
                isDeletable?"T":"F",
                isArray?"T":"F"));
#endif    
    }

    virtual void DisposeTarget()
    {

        if (isDeletable && target != 0)
        {
            if (isArray)
                delete [] target;
            else
                delete target;

            target = 0;
        }
    }

    virtual ~AutoPointer()
    {
        AutoPointer::!AutoPointer();
    }
    
    !AutoPointer()
    {
#ifdef _DEBUG_AUTO_PTR
        int targetWas = (int)target;
        System::Diagnostics::Debug::WriteLine(
            System::String::Format("0x{0:X}\t{1}\t1\t{2}\tDeletable=\t{3}\tArray=\t{4}\tdisposing",
                targetWas,
                System::DateTime::Now.ToString("HH:mm:ss.fff"),
                T::typeid,
                isDeletable?"T":"F",
                isArray?"T":"F"));
#endif

        DisposeTarget();

#ifdef _DEBUG_AUTO_PTR
        System::Diagnostics::Debug::WriteLine(
            System::String::Format("0x{0:X}\t{1}\t1\t{2}\tDeletable=\t{3}\tArray=\t{4}\tdone disposing",
                targetWas,
                System::DateTime::Now.ToString("HH:mm:ss.fff"),
                T::typeid,
                isDeletable?"T":"F",
                isArray?"T":"F"));
#endif
    }

protected:
    T* target;
    bool isDeletable;
    bool isArray;

};
} } }
