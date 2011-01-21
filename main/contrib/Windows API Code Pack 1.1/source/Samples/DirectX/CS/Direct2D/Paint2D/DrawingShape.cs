// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace D2DPaint
{
    abstract class DrawingShape
    {
        protected Paint2DForm _parent;
        protected bool _fill;

        protected internal abstract void Draw(RenderTarget renderTarget);
        protected internal virtual void EndDraw()
        { }

        protected internal abstract Point2F EndPoint
        {
            set;
        }

        protected DrawingShape(Paint2DForm parent)
        {
            this._parent = parent;
        }

        protected DrawingShape(Paint2DForm parent, bool fill) : this(parent)
        {
            this._fill = fill;
        }
    }
}