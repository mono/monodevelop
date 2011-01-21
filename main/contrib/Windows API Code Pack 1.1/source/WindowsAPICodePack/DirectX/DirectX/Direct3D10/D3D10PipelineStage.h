//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10Device.h"
#include <vector>

using namespace std;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

/// <summary>
/// A pipeline stage. base class for all pipline stage related classes.
/// </summary>
public ref class PipelineStage abstract
{
internal:
    property D3DDevice^ Parent
    {
        D3DDevice^ get()
        {
            return parentDevice;
        }
    }
    PipelineStage() {}
    PipelineStage(D3DDevice^ parent) : parentDevice(parent) {}

private: 
    D3DDevice^ parentDevice;

};
} } } }
