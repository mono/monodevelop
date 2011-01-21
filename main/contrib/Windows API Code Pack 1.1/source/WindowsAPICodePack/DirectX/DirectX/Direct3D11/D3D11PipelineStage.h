//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceContext.h"
#include <vector>

using namespace std;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

/// <summary>
/// A pipeline stage. base class for all pipline stage related classes.
/// </summary>
public ref class PipelineStage abstract
{
protected:
    property DeviceContext^ Parent
    {
        DeviceContext^ get()
        {
            return parentDevice;
        }
    }

protected:
    PipelineStage() {}
internal:
    PipelineStage(DeviceContext^ parent) : parentDevice(parent) {}
private: 
    DeviceContext^ parentDevice;

};
} } } }
