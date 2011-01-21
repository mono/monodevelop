//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System::Linq;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// Stores the render targets for a merger pipeline stage
    /// </summary>
    public ref class OutputMergerRenderTargets
    {
    public:

        property ReadOnlyCollection<RenderTargetView^>^ RenderTargetViews
        {
            ReadOnlyCollection<RenderTargetView^>^ get(void)
            {
                return renderTargetViews;
            }
        }

        property DepthStencilView^ DepthStencilView
        {
            Direct3D10::DepthStencilView^ get(void)
            {
                return depthStencilView;
            }
        }

        // REVIEW: it would be possible to do some sanity checking on the arguments here;
        // MSDN docs say, for example, that the array sizes of all the views must be the
        // same. We could check that, rather than just passing this blindly to DirectX and
        // having it complain.

        OutputMergerRenderTargets(IEnumerable<RenderTargetView^>^ renderTargetViews)
        {
            Initialize(renderTargetViews, nullptr);
        }

        OutputMergerRenderTargets(Direct3D10::DepthStencilView^ depthStencilView)
        {
            Initialize(nullptr, depthStencilView);
        }

        OutputMergerRenderTargets(IEnumerable<RenderTargetView^>^ renderTargetViews,
            Direct3D10::DepthStencilView^ depthStencilView)
        {
            Initialize(renderTargetViews, depthStencilView);
        }

    private:

        void Initialize(IEnumerable<RenderTargetView^>^ renderTargetViewsInit,
            Direct3D10::DepthStencilView^ depthStencilViewInit)
        {
            this->renderTargetViews = renderTargetViewsInit != nullptr ?
                Array::AsReadOnly(Enumerable::ToArray(renderTargetViewsInit)) : nullptr;
            this->depthStencilView = depthStencilViewInit;
        }

        // REVIEW: is it more important to optimize for the managed or unmanaged side?
        // Members here could be unmanaged types, which would allow the same object to be
        // reused for calls into the API multiple times with less overhead. But there would
        // then be greater overhead for managed code reads from this data structure.

        ReadOnlyCollection<RenderTargetView^>^ renderTargetViews;
        Direct3D10::DepthStencilView^ depthStencilView;
    };
} } } }