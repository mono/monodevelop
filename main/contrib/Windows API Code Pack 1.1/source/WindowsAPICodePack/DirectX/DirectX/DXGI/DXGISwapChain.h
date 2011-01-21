//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DXGIDeviceSubObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {

using namespace System;

ref class Output;

    /// <summary>
    /// An SwapChain interface implements one or more surfaces for storing rendered data before presenting it to an output.
    /// <para>(Also see DirectX SDK: IDXGISwapChain)</para>
    /// </summary>
    public ref class SwapChain :
        public DeviceSubObject
    {
    public: 
        /// <summary>
        /// Access one of the swap-chain back buffers.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::GetBuffer)</para>
        /// </summary>
        /// <param name="bufferIndex">A zero-based buffer index. 
        /// If the swap effect is not Sequential, this method only has access to the first buffer; for this case, set the index to zero.
        /// </param>
        /// <typeparam name="T">The type of the buffer object. Must inherit from <see cref="DirectUnknown"/>.</typeparam>
        /// <returns>The back-buffer object.</returns>
        generic <typename T>  where T : DirectUnknown  
        T GetBuffer(UInt32 bufferIndex);

        /// <summary>
        /// Get the output (the display monitor) that contains the majority of the client area of the target window.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::GetContainingOutput)</para>
        /// </summary>
        property Output^ ContainingOutput
        {
            Output^ get(void);
        }

        /// <summary>
        /// Get a description of the swap chain.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::GetDesc)</para>
        /// </summary>
        property SwapChainDescription Description
        {
            SwapChainDescription get();
        }

        /// <summary>
        /// Get performance statistics about the last render frame.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::GetFrameStatistics)</para>
        /// </summary>
        property FrameStatistics FrameStatistics
        {
            Graphics::FrameStatistics get(void);
        }

        /// <summary>
        /// Gets the Output object associated with full-screen mode.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::GetFullscreenState)</para>
        /// </summary>
        /// <returns>
        /// If the swap chain is in full-screen mode, returns the output target. Otherwise, null.
        /// </returns>
        property Output^ FullScreenOutput
        {
            Output^ get(void);
        }

        /// <summary>
        /// Sets or get the swap chain in full screen.
        /// Graphics will choose the output based on the swap-chain's device and the output window's placement.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::GetFullscreenState)</para>
        /// </summary>
        property bool IsFullScreen
        {
            bool get();
            void set(bool);
        }

        /// <summary>
        /// Get the number of times SwapChain.Present has been called.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::GetLastPresentCount)</para>
        /// </summary>
        property UInt32 LastPresentCount
        {
            UInt32 get();
        }

        /// <summary>
        /// Present a rendered image to the user.
        /// This method can throw exceptions if the Swap Chain is unable to present. 
        /// TryPresent() method should be used instead when exceptions can impact performance.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::Present)</para>
        /// </summary>
        /// <param name="syncInterval">If the update region straddles more than one output (each represented by Output), the synchronization will be performed to the output that contains the largest subrectangle of the target window's client area.</param>
        /// <param name="options">An integer value that contains swap-chain presentation options (see <see cref="Present"/>)<seealso cref="Present"/>.</param>
        void Present(UInt32 syncInterval, PresentOptions options);

        /// <summary>
        /// Try to present a rendered image to the user.
        /// No exceptions will be thrown by this method.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::Present)</para>
        /// </summary>
        /// <param name="syncInterval">If the update region straddles more than one output (each represented by Output), the synchronization will be performed to the output that contains the largest subrectangle of the target window's client area.</param>
        /// <param name="options">An integer value that contains swap-chain presentation options (see <see cref="Present"/>)<seealso cref="Present"/>.</param>
        /// <param name="error">An error code indicating Present error if unsuccessful.</param>
        /// <returns>False if unsuccessful; True otherwise.</returns>
        bool TryPresent(UInt32 syncInterval, PresentOptions options, [System::Runtime::InteropServices::Out] ErrorCode % error);

        /// <summary>
        /// Change the swap chain's back buffer size, format, and number of buffers. This should be called when the application window is resized.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::ResizeBuffers)</para>
        /// </summary>
        /// <param name="bufferCount">The number of buffers in the swap chain (including all back and front buffers). This can be different from the number of buffers the swap chain was created with.</param>
        /// <param name="width">New width of the back buffer. If 0 is specified the width of the client area of the target window will be used.</param>
        /// <param name="height">New height of the back buffer. If 0 is specified the height of the client area of the target window will be used.</param>
        /// <param name="newFormat">The new format of the back buffer.</param>
        /// <param name="options">Flags that indicate how the swap chain will function.</param>
        void ResizeBuffers(UInt32 bufferCount, UInt32 width, UInt32 height, Format newFormat, SwapChainOptions options);

        /// <summary>
        /// Change the swap chain's back buffer size, format, and number of buffers. This should be called when the application window is resized.
        /// This method will not throw exceptions, but will return a bool indicating success or failure.
        /// The errorCode output value can also be used to track the error type.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::ResizeBuffers)</para>
        /// </summary>
        /// <param name="bufferCount">The number of buffers in the swap chain (including all back and front buffers). This can be different from the number of buffers the swap chain was created with.</param>
        /// <param name="width">New width of the back buffer. If 0 is specified the width of the client area of the target window will be used.</param>
        /// <param name="height">New height of the back buffer. If 0 is specified the height of the client area of the target window will be used.</param>
        /// <param name="newFormat">The new format of the back buffer.</param>
        /// <param name="options">Flags that indicate how the swap chain will function.</param>
        /// <param name="errorCode">Returned error code.</param>
        /// <returns>True if successful, false otherwise.</returns>
        bool TryResizeBuffers(UInt32 bufferCount, UInt32 width, UInt32 height, Format newFormat, SwapChainOptions options, [System::Runtime::InteropServices::Out] ErrorCode % errorCode);

        /// <summary>
        /// Resize the output target.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::ResizeTarget)</para>
        /// </summary>
        /// <param name="newTargetParameters">The mode description (see <see cref="ModeDescription"/>)<seealso cref="ModeDescription"/>, which specifies the new width, height, format and refresh rate of the target. 
        /// If the format is UNKNOWN, the existing format will be used. Using UNKNOWN is only recommended when the swap chain is in full-screen mode as this method is not thread safe.</param>
        void ResizeTarget(ModeDescription newTargetParameters);

        /// <summary>
        /// Set the display mode to windowed or full-screen.
        /// <para>(Also see DirectX SDK: IDXGISwapChain::SetFullscreenState)</para>
        /// </summary>
        /// <param name="isFullScreen">Use True for full-screen, False for windowed.</param>
        /// <param name="target">If the current display mode is full-screen, this parameter must be the output target (see <see cref="Output"/>)<seealso cref="Output"/> that contains the swap chain; 
        /// otherwise, this parameter is ignored. If you set this parameter to Null, Graphics will choose the output based on the swap-chain's device and the output window's placement.</param>
        void SetFullScreenState(Boolean isFullScreen, Output^ target);

    internal:
        SwapChain()
        { }

        SwapChain(IDXGISwapChain* pNativeIDXGISwapChain)
            : DeviceSubObject(pNativeIDXGISwapChain)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(IDXGISwapChain)); }
        }
    };
} } } }
