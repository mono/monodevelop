// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "CommonUtils.h"
#include "LibraryLoader.h"
#include "DirectWrite/DWriteRenderingParams.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace System::Runtime::InteropServices;

typedef HRESULT (WINAPI *D2D1CreateFactoryFuncPtr)(
  D2D1_FACTORY_TYPE factoryType,
  REFIID riid,
  const D2D1_FACTORY_OPTIONS *pFactoryOptions,
  void **ppIFactory
    );

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {

    D2DFactory ^
    D2DFactory::CreateFactory() 
    {
        return CreateFactory(D2DFactoryType::SingleThreaded, FactoryOptions(DebugLevel::None));
    };

    D2DFactory ^
    D2DFactory::CreateFactory(
        D2DFactoryType factoryType
        ) 
    {
        return CreateFactory(factoryType, FactoryOptions(DebugLevel::None));
    };

    D2DFactory ^
    D2DFactory::CreateFactory(
        D2DFactoryType factoryType,
        FactoryOptions factoryOptions
        ) 
    {
        D2D1CreateFactoryFuncPtr createFuncPtr = 
            (D2D1CreateFactoryFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
                D2DLibrary, "D2D1CreateFactory");

        ID2D1Factory *pIFactory = NULL;
        D2D1_FACTORY_OPTIONS tempOptions;
        factoryOptions.CopyTo(&tempOptions);

        Validate::VerifyResult(
            (*createFuncPtr)(
                static_cast<D2D1_FACTORY_TYPE>(factoryType),
                __uuidof(ID2D1Factory),
                &tempOptions,
                (void**)&pIFactory));                        

        return pIFactory ? gcnew D2DFactory(pIFactory) : nullptr;
    };

    DrawingStateBlock ^
    D2DFactory::CreateDrawingStateBlock(
        DrawingStateDescription drawingStateDescription,
        RenderingParams^ textRenderingParams
        )
    {
        D2D1_DRAWING_STATE_DESCRIPTION tempDescription;
        drawingStateDescription.CopyTo(&tempDescription);

        ID2D1DrawingStateBlock * ptr = NULL;
        Validate::VerifyResult(
            CastInterface<ID2D1Factory>()->CreateDrawingStateBlock(
                &tempDescription,
                textRenderingParams == nullptr ? NULL : textRenderingParams->CastInterface<IDWriteRenderingParams>(),
                &ptr
                ));
    
        return ptr ? gcnew DrawingStateBlock(ptr) : nullptr;
    }

    DrawingStateBlock ^
    D2DFactory::CreateDrawingStateBlock(
        DrawingStateDescription drawingStateDescription
        )
    {
        D2D1_DRAWING_STATE_DESCRIPTION tempDescription;
        drawingStateDescription.CopyTo(&tempDescription);


        ID2D1DrawingStateBlock * ptr = NULL;
        Validate::VerifyResult(
            CastInterface<ID2D1Factory>()->CreateDrawingStateBlock(
                &tempDescription,
                NULL,
                &ptr
                ));
    
        return ptr ? gcnew DrawingStateBlock(ptr) : nullptr;
    }

    DrawingStateBlock ^
    D2DFactory::CreateDrawingStateBlock(
        )
    {
        ID2D1DrawingStateBlock * ptr = NULL;
        Validate::VerifyResult(
            CastInterface<ID2D1Factory>()->CreateDrawingStateBlock(
                NULL,
                NULL,
                &ptr
                ));
    
        return ptr ? gcnew DrawingStateBlock(ptr) : nullptr;
    }

    //
    // Implement methods for sink interfaces
    // 
    void
    Geometry::Simplify(
        GeometrySimplificationOption simplificationOption,
        ISimplifiedGeometrySink ^ pIGeometrySink,
        FLOAT flatteningTolerance,
        Matrix3x2F pWorldTransform
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        D2D1_MATRIX_3X2_F copyWorldTransform;
        pWorldTransform.CopyTo(&copyWorldTransform);

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Simplify(
                static_cast<D2D1_GEOMETRY_SIMPLIFICATION_OPTION>(simplificationOption),
                &copyWorldTransform,
                flatteningTolerance,
                pGeometrySinkCallback
                )); 
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }
    }

    void
    Geometry::Simplify(
        GeometrySimplificationOption simplificationOption,
        ISimplifiedGeometrySink ^ pIGeometrySink,
        FLOAT flatteningTolerance
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Simplify(
                static_cast<D2D1_GEOMETRY_SIMPLIFICATION_OPTION>(simplificationOption),
                NULL,
                flatteningTolerance,
                pGeometrySinkCallback
                )); 
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }
    }

    void
    Geometry::Simplify(
        GeometrySimplificationOption simplificationOption,
        ISimplifiedGeometrySink ^ pIGeometrySink
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Simplify(
                static_cast<D2D1_GEOMETRY_SIMPLIFICATION_OPTION>(simplificationOption),
                NULL,
                pGeometrySinkCallback
                )); 
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }
    }

    void
    Geometry::CombineWithGeometry(
        Geometry ^ pInputGeometry,
        CombineMode combineMode,
        ISimplifiedGeometrySink ^ pIGeometrySink,
        FLOAT flatteningTolerance,
        Matrix3x2F pInputGeometryTransform
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        D2D1_MATRIX_3X2_F copyInputGeometryTransform;
        pInputGeometryTransform.CopyTo(&copyInputGeometryTransform);

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->CombineWithGeometry(
                pInputGeometry->CastInterface<ID2D1Geometry>(),
                static_cast<D2D1_COMBINE_MODE>(combineMode),
                &copyInputGeometryTransform,
                flatteningTolerance,
                pGeometrySinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }
    }
    

    void
    Geometry::CombineWithGeometry(
        Geometry ^ pInputGeometry,
        CombineMode combineMode,
        ISimplifiedGeometrySink ^ pIGeometrySink,
        FLOAT flatteningTolerance
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->CombineWithGeometry(
                pInputGeometry->CastInterface<ID2D1Geometry>(),
                static_cast<D2D1_COMBINE_MODE>(combineMode),
                NULL,
                flatteningTolerance,
                pGeometrySinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }
    }
    
    void
    Geometry::CombineWithGeometry(
        Geometry ^ pInputGeometry,
        CombineMode combineMode,
        ISimplifiedGeometrySink ^ pIGeometrySink
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->CombineWithGeometry(
                pInputGeometry->CastInterface<ID2D1Geometry>(),
                static_cast<D2D1_COMBINE_MODE>(combineMode),
                NULL,
                pGeometrySinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }
    }

    void
    Geometry::Outline(
        ISimplifiedGeometrySink ^ pIGeometrySink,
        FLOAT flatteningTolerance,
        Matrix3x2F pWorldTransform
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        D2D1_MATRIX_3X2_F copyWorldTransform;
        pWorldTransform.CopyTo(&copyWorldTransform);

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Outline(
                &copyWorldTransform,
                flatteningTolerance,
                pGeometrySinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }
    }

    void
    Geometry::Outline(
        ISimplifiedGeometrySink ^ pIGeometrySink,
        FLOAT flatteningTolerance
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Outline(
                NULL,
                flatteningTolerance,
                pGeometrySinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }
    }

    void
    Geometry::Outline(
        ISimplifiedGeometrySink ^ pIGeometrySink
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Outline(
                NULL,
                pGeometrySinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }
    }

    void
    Geometry::Widen(
        FLOAT strokeWidth,
        StrokeStyle ^ pIStrokeStyle,
        ISimplifiedGeometrySink ^ pIGeometrySink,
        FLOAT flatteningTolerance,
        Matrix3x2F pWorldTransform
        )
    {
        D2D1_MATRIX_3X2_F copyWorldTransform;
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        pWorldTransform.CopyTo(&copyWorldTransform);

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Widen(
                strokeWidth,
                pIStrokeStyle->CastInterface<ID2D1StrokeStyle>(),
                &copyWorldTransform,
                flatteningTolerance,
                pGeometrySinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }        
    }

    void
    Geometry::Widen(
        FLOAT strokeWidth,
        StrokeStyle ^ pIStrokeStyle,
        ISimplifiedGeometrySink ^ pIGeometrySink,
        FLOAT flatteningTolerance
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Widen(
                strokeWidth,
                pIStrokeStyle->CastInterface<ID2D1StrokeStyle>(),
                NULL,
                flatteningTolerance,
                pGeometrySinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }        
    }

    void
    Geometry::Widen(
        FLOAT strokeWidth,
        StrokeStyle ^ pIStrokeStyle,
        ISimplifiedGeometrySink ^ pIGeometrySink
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Widen(
                strokeWidth,
                pIStrokeStyle->CastInterface<ID2D1StrokeStyle>(),
                NULL,
                pGeometrySinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }        
    }

    void
    Geometry::Tessellate(
        ITessellationSink ^ pITessellationSink,
        FLOAT flatteningTolerance,
        Matrix3x2F pWorldTransform
        )
    {
        Direct2D1::TessellationSinkCallback *pTessellationSinkCallback = NULL;

        D2D1_MATRIX_3X2_F copyWorldTransform;
        pWorldTransform.CopyTo(&copyWorldTransform);

        try
        {
            pTessellationSinkCallback = new Direct2D1::TessellationSinkCallback(
                pITessellationSink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Tessellate(
                &copyWorldTransform,
                flatteningTolerance,
                pTessellationSinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pTessellationSinkCallback);
        }        
    }


    void
    Geometry::Tessellate(
        ITessellationSink ^ pITessellationSink,
        FLOAT flatteningTolerance
        )
    {
        Direct2D1::TessellationSinkCallback *pTessellationSinkCallback = NULL;

        try
        {
            pTessellationSinkCallback = new Direct2D1::TessellationSinkCallback(
                pITessellationSink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Tessellate(
                NULL,
                flatteningTolerance,
                pTessellationSinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pTessellationSinkCallback);
        }        
    }

    void
    Geometry::Tessellate(
        ITessellationSink ^ pITessellationSink
        )
    {
        Direct2D1::TessellationSinkCallback *pTessellationSinkCallback = NULL;

        try
        {
            pTessellationSinkCallback = new Direct2D1::TessellationSinkCallback(
                pITessellationSink
                );

            Validate::VerifyResult(CastInterface<ID2D1Geometry>()->Tessellate(
                NULL,
                pTessellationSinkCallback
                ));
        }
        __finally
        {
            ReleaseInterface(pTessellationSinkCallback);
        }        
    }

    void
    PathGeometry::Stream(
        IGeometrySink ^ pIGeometrySink
        )
    {
        Direct2D1::GeometrySinkCallback *pGeometrySinkCallback = NULL;

        try
        {
            pGeometrySinkCallback = new Direct2D1::GeometrySinkCallback(
                pIGeometrySink
                );

            Validate::VerifyResult(CastInterface<ID2D1PathGeometry>()->Stream(
                pGeometrySinkCallback
                ));            
        }
        __finally
        {
            ReleaseInterface(pGeometrySinkCallback);
        }        
    }

} } } }
