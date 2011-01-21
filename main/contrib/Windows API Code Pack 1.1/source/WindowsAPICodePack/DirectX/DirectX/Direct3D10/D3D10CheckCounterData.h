//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// This class encapsulates the data returned by the D3DDevice.CheckCounter method.
    /// <para>(Also see DirectX SDK: ID3D10Device.CheckCounter)</para>
    /// </summary>
    public ref class CounterData
    {
    public: 

        property CounterType CounterType
        {
            Direct3D10::CounterType get(void) { return type; }
        }

        property int ActiveCounterCount
        {
            int get(void) { return activeCounterCount; }
        }

        property String^ Name
        {
            String^ get(void) { return name; }
        }

        property String^ Units
        {
            String^ get(void) { return units; }
        }

        property String^ Description
        {
            String^ get(void) { return description; }
        }

    internal:

        CounterData(Direct3D10::CounterType type, int activeCounterCount, String^ name, String^ units, String^ description)
        {
            this->type = type;
            this->activeCounterCount = activeCounterCount;
            this->name = name;
            this->units = units;
            this->description = description;
        }

    private:

        Direct3D10::CounterType type;
        int activeCounterCount;
        String^ name;
        String^ units;
        String^ description;

    };
} } } }
