//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D {

/// <summary>
/// Describes the set of features targeted by a Direct3D device.
/// <para>(Also see DirectX SDK: D3D_FEATURE_LEVEL)</para>
/// <para>(Also see DirectX SDK: D2D1_FEATURE_LEVEL)</para>
/// </summary>
public enum class FeatureLevel 
{
    /// <summary>
    /// The caller does not require a particular underlying D3D device level.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Targets features supported by Direct3D 9.1 including shader model 2.
    /// <para>(Also see DirectX SDK: D3D_FEATURE_LEVEL_9_1)</para>
    /// </summary>
    NinePointOne = D3D_FEATURE_LEVEL_9_1,
    /// <summary>
    /// Targets features supported by Direct3D 9.2 including shader model 2.
    /// <para>(Also see DirectX SDK: D3D_FEATURE_LEVEL_9_2)</para>
    /// </summary>
    NinePointTwo = D3D_FEATURE_LEVEL_9_2,
    /// <summary>
    /// Targets features supported by Direct3D 9.3 including shader shader model 3.
    /// <para>(Also see DirectX SDK: D3D_FEATURE_LEVEL_9_3)</para>
    /// </summary>
    NinePointThree = D3D_FEATURE_LEVEL_9_3,
    /// <summary>
    /// Targets features supported by Direct3D 10.0 including shader shader model 4.
    /// <para>(Also see DirectX SDK: D3D_FEATURE_LEVEL_10_0)</para>
    /// </summary>
    Ten = D3D_FEATURE_LEVEL_10_0,
    /// <summary>
    /// Targets features supported by Direct3D 10.1 including shader shader model 4.1.
    /// <para>(Also see DirectX SDK: D3D_FEATURE_LEVEL_10_1)</para>
    /// </summary>
    TenPointOne = D3D_FEATURE_LEVEL_10_1,
    /// <summary>
    /// Targets features supported by Direct3D 11.0 including shader shader model 5.
    /// <para>(Also see DirectX SDK: D3D_FEATURE_LEVEL_11_0)</para>
    /// </summary>
    Eleven = D3D_FEATURE_LEVEL_11_0,
};
/// <summary>
/// Direct3D Devices supported by this library.
/// </summary>
public enum class DeviceType
{
    /// <summary>
    /// Direct3D Device 10.0
    /// </summary>
    Direct3D10 = 0,

    /// <summary>
    /// Direct3D Device 10.1
    /// </summary>
    Direct3D10Point1 = 1,
    
    /// <summary>
    /// Direct3D Device 11.0
    /// </summary>
    Direct3D11 = 2,
};

} } }  }
