// Copyright (c) Microsoft Corporation.  All rights reserved.

//+----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// File name:
//      common.h
//
// Description: 
//      Header file that includes common C++ definitions that are used throughout 
//      the assembly.
//
//-----------------------------------------------------------------------------
#pragma once

#if !defined(_PREFAST_)
    #define UNCONDITIONAL_EXPR(Exp) (0,Exp)
#else
    #define UNCONDITIONAL_EXPR(Exp) (Exp)
#endif

#define ReleaseInterface(p) \
    do                      \
    {                       \
        if (p != NULL)      \
        {                   \
            p->Release();   \
            p = NULL;       \
        }                   \
    } while(UNCONDITIONAL_EXPR(0))

#define OverwriteInterface(p, n)    \
    do                              \
    {                               \
        p = n;                      \
                                    \
        if (p != NULL)              \
        {                           \
            p->AddRef();            \
        }                           \
    } while(UNCONDITIONAL_EXPR(0))


using namespace System;

#include "D2DInteropTypes.h"
#include "D2DEnums.h"
#include "D2DStructs.h"
#include "D2DInterfaces.h"
#include "D2DGeometrySink.h"
#include "D2DTessellationSink.h"




