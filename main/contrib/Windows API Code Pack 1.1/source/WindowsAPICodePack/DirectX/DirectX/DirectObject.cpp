//Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "DirectObject.h"

void DirectObject::Attach(void* _right)
{
    // Non IUnknown interfaces should not be deleted
    nativeObject.Set(_right, false);
}

DirectObject::DirectObject() 
{
}

DirectObject::DirectObject(void* _ptr) 
{
    nativeObject.Set(_ptr, false);
}

DirectObject::DirectObject(void* _ptr, bool _deletable) 
{
    nativeObject.Set(_ptr, _deletable);
}