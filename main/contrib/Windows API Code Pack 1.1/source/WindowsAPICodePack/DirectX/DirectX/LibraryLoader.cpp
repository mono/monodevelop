//Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "LibraryLoader.h"

using namespace Microsoft::WindowsAPICodePack::DirectX;

LibraryLoader::LibraryLoader()
{
    m_libraryNames[D2DLibrary] = L"D2d1.dll";
    m_libraryNames[D3D11Library] = L"D3d11.dll";
    m_libraryNames[D3D10Library] = L"D3d10.dll";
    m_libraryNames[D3D10_1Library] = L"D3d10_1.dll";
    m_libraryNames[DXGILibrary] = L"DXGI.dll";
    m_libraryNames[DWriteLibrary] = L"DWrite.dll";
}

LibraryLoader* LibraryLoader::Instance()
{
    static LibraryLoader* m_instance = 0;

    if (!m_instance)
    {
        m_instance = new LibraryLoader();
    }
    return m_instance;
}


FARPROC LibraryLoader::GetFunctionFromDll(DirectXLibrary library, LPCSTR functionName)
{
    if(m_loadedMethods.find(functionName) == m_loadedMethods.end())
    {
        HINSTANCE libInstance = LoadDll(library);
        FARPROC funcPtr = GetProcAddress(libInstance, functionName);

        if (funcPtr == 0)
        {
            throw gcnew EntryPointNotFoundException(
                String::Format("Unable to find entry point for {0}().", gcnew String(functionName)),
                gcnew System::ComponentModel::Win32Exception(Marshal::GetLastWin32Error()));
        }

        m_loadedMethods[functionName] = funcPtr;
    }

    return m_loadedMethods[functionName];

}

HINSTANCE LibraryLoader::LoadDll(DirectXLibrary library)
{
    if(m_libraries.find(library) == m_libraries.end())
    {
        HINSTANCE libraryInstance = LoadLibrary(m_libraryNames[library].c_str());
         
        // Check to see if the library was loaded successfully 
        if (libraryInstance == 0)
        {
            throw gcnew DllNotFoundException(
                String::Format("Unable to load dynamic link library: \"{0}\".", gcnew String(m_libraryNames[library].c_str())),
                gcnew System::ComponentModel::Win32Exception(Marshal::GetLastWin32Error())
                );
        }

        m_libraries[library] = libraryInstance;
    }

    return m_libraries[library];
}

