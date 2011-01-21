// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.IO;
using System.Windows;

namespace Microsoft.WindowsAPICodePack.Samples
{
    /// <summary>
    /// This class implements a custom Direct Write Inline Object (ICustomInlineObject)
    /// that displays a given image 
    /// </summary>
    public class ImageInlineObject : ICustomInlineObject
    {
        RenderTarget _renderTarget;
        D2DBitmap _bitmap ;
        SizeF _bitmapSize;
        
        public ImageInlineObject(RenderTarget renderTarget, ImagingFactory wicFactory, string resourceName)
        {
            _renderTarget = renderTarget;

            using (Stream stream = Application.ResourceAssembly.GetManifestResourceStream(resourceName))
            {
                _bitmap = BitmapUtilities.LoadBitmapFromStream(renderTarget, wicFactory, stream);

                // Save the bitmap size, for faster access
                _bitmapSize = _bitmap.Size;
            }
        }

        #region ICustomInlineObject Members

        public BreakCondition BreakConditionAfter
        {
            get 
            { 
                return BreakCondition.Neutral; 
            }
        }

        public BreakCondition BreakConditionBefore
        {
            get 
            { 
                return BreakCondition.Neutral; 
            }
        }

        public void Draw(float originX, float originY, bool isSideways, bool isRightToLeft, Brush clientDrawingEffect)
        {
            RectF imageRect = new RectF(originX, originY, originX + _bitmapSize.Width, originY + _bitmapSize.Height);
            _renderTarget.DrawBitmap(_bitmap, 1, BitmapInterpolationMode.Linear, imageRect);
        }

        public InlineObjectMetrics Metrics
        {
            // Simply return the image size
            get 
            { 
                return new InlineObjectMetrics(_bitmapSize.Width, _bitmapSize.Height, _bitmapSize.Height, false); 
            }
        }

        public OverhangMetrics OverhangMetrics
        {
            // No overhangs
            get 
            { 
                return new OverhangMetrics(0, 0, 0, 0); 
            }
        }

        #endregion
    }
}
