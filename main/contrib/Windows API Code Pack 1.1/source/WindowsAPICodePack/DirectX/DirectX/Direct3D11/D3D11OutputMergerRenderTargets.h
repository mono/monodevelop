//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System::Linq;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

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
            Direct3D11::DepthStencilView^ get(void)
            {
                return depthStencilView;
            }
        }

        property UInt32 UnorderedViewStartSlot
        {
            UInt32 get(void)
            {
                return viewStartSlot;
            }
        }

        property ReadOnlyCollection<UnorderedAccessView^>^ UnorderedAccessViews
        {
            ReadOnlyCollection<UnorderedAccessView^>^ get(void)
            {
                return unorderedAccessViews;
            }
        }

        property ReadOnlyCollection<UInt32>^ InitialCounts
        {
            ReadOnlyCollection<UInt32>^ get(void)
            {
                return initialCounts;
            }
        }

        // REVIEW: it would be possible to do some sanity checking on the arguments here;
        // MSDN docs say, for example, that the array sizes of all the views must be the
        // same. We could check that, rather than just passing this blindly to DirectX and
        // having it complain.

        OutputMergerRenderTargets(IEnumerable<RenderTargetView^>^ renderTargetViews)
        {
            Initialize(renderTargetViews, nullptr, 0, nullptr, nullptr);
        }

        OutputMergerRenderTargets(Direct3D11::DepthStencilView^ depthStencilView)
        {
            Initialize(nullptr, depthStencilView, 0, nullptr, nullptr);
        }

        OutputMergerRenderTargets(IEnumerable<RenderTargetView^>^ renderTargetViews,
            Direct3D11::DepthStencilView^ depthStencilView)
        {
            Initialize(renderTargetViews, depthStencilView, 0, nullptr, nullptr);
        }

// "view" local variable is intentionally not used
#pragma warning(push)
#pragma warning(disable:4189)
        OutputMergerRenderTargets(IEnumerable<RenderTargetView^>^ renderTargetViews,
            Direct3D11::DepthStencilView^ depthStencilView, UInt32 viewStartSlot,
            IEnumerable<UnorderedAccessView^>^ unorderedAccessViews)
        {
            List<UInt32>^ initialCountsList = gcnew List<UInt32>();

            for each (UnorderedAccessView^ view in unorderedAccessViews)
            {
                initialCountsList->Add(static_cast<UInt32>(-1));
            }

            Initialize(renderTargetViews, depthStencilView, viewStartSlot, unorderedAccessViews, initialCountsList->ToArray());
        }
#pragma warning(pop)

        OutputMergerRenderTargets(IEnumerable<RenderTargetView^>^ renderTargetViews,
            Direct3D11::DepthStencilView^ depthStencilView, UInt32 viewStartSlot,
            IEnumerable<UnorderedAccessView^>^ unorderedAccessViews, IEnumerable<UInt32>^ initialCounts)
        {
            Initialize(renderTargetViews, depthStencilView, viewStartSlot, unorderedAccessViews,
                initialCounts != nullptr ? Enumerable::ToArray(initialCounts) : nullptr);
        }

    private:

        void Initialize(IEnumerable<RenderTargetView^>^ renderTargetViewsInit,
            Direct3D11::DepthStencilView^ depthStencilViewInit, UInt32 viewStartSlotInit,
            IEnumerable<UnorderedAccessView^>^ unorderedAccessViewsInit, array<UInt32>^ initialCountsInit)
        {
            this->renderTargetViews = renderTargetViewsInit != nullptr ?
                Array::AsReadOnly(Enumerable::ToArray(renderTargetViewsInit)) : nullptr;
            this->depthStencilView = depthStencilViewInit;
            this->viewStartSlot = viewStartSlotInit;
            this->unorderedAccessViews = unorderedAccessViewsInit != nullptr ?
                Array::AsReadOnly(Enumerable::ToArray(unorderedAccessViewsInit)) : nullptr;
            this->initialCounts = initialCountsInit != nullptr ? Array::AsReadOnly(initialCountsInit) : nullptr;
        }

        // REVIEW: is it more important to optimize for the managed or unmanaged side?
        // Members here could be unmanaged types, which would allow the same object to be
        // reused for calls into the API multiple times with less overhead. But there would
        // then be greater overhead for managed code reads from this data structure.

        ReadOnlyCollection<RenderTargetView^>^ renderTargetViews;
        Direct3D11::DepthStencilView^ depthStencilView;
        UInt32 viewStartSlot;
        ReadOnlyCollection<UnorderedAccessView^>^ unorderedAccessViews;
        ReadOnlyCollection<UInt32>^ initialCounts;
    };
} } } }