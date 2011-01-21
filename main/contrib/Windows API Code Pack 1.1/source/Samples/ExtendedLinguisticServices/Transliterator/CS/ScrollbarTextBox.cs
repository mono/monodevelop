// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace Transliterator
{
    public class ScrollbarTextBox : TextBox
    {
        private const int WM_HSCROLL = 0x114;
        private const int WM_VSCROLL = 0x115;

        public event ScrollEventHandler OnHorizontalScroll = null;
        public event ScrollEventHandler OnVerticalScroll = null;

        private static ScrollEventType[] scrollEventType = new ScrollEventType[] 
        {   ScrollEventType.SmallDecrement,
            ScrollEventType.SmallIncrement,
            ScrollEventType.LargeDecrement,
            ScrollEventType.LargeIncrement,
            ScrollEventType.ThumbPosition,
            ScrollEventType.ThumbTrack,
            ScrollEventType.First,
            ScrollEventType.Last,
            ScrollEventType.EndScroll
        };

        private ScrollEventType GetEventType(uint wParam)
        {
            if (wParam < scrollEventType.Length)
            {
                return scrollEventType[wParam];
            }
            else
            {
                return ScrollEventType.EndScroll;
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_HSCROLL)
            {
                if (OnHorizontalScroll != null)
                {
                    uint wParam = (uint)m.WParam.ToInt32();
                    OnHorizontalScroll(this, new ScrollEventArgs(GetEventType(wParam & 0xffff), (int)(wParam >> 16)));
                }
            }
            else if (m.Msg == WM_VSCROLL)
            {
                if (OnVerticalScroll != null)
                {
                    uint wParam = (uint)m.WParam.ToInt32();
                    OnVerticalScroll(this, new ScrollEventArgs(GetEventType(wParam & 0xffff), (int)(wParam >> 16)));
                }
            }
        }
    }
}
