//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

generic<typename T> where T : ref class
public value class ShaderAndClasses
{
public:

    property T Shader
    {
        T get(void) { return shader; }
    }

    property ReadOnlyCollection<ClassInstance^>^ Classes
    {
        ReadOnlyCollection<ClassInstance^>^ get(void) { return classes; }
    }

    ShaderAndClasses(T shader, ReadOnlyCollection<ClassInstance^>^ instanceCollection)
        : shader(shader), classes(instanceCollection)
    { }

    ShaderAndClasses(T shader, IEnumerable<ClassInstance^>^ instanceEnum)
        : shader(shader)
    {
        if (instanceEnum != nullptr)
        {
            IList<ClassInstance^>^ instanceList = gcnew List<ClassInstance^>();

            for each (ClassInstance^ instance in instanceEnum)
            {
                instanceList->Add(instance);
            }

            classes = gcnew ReadOnlyCollection<ClassInstance^>(instanceList);
        }
    }

private:

    T shader;
    ReadOnlyCollection<ClassInstance^>^ classes;
};

}}}}