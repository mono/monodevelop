/* 
 * ToggleButton.cs - A basic toogle button implementing INativeChildView
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if MAC
using System;
using AppKit;
using CoreGraphics;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	sealed class NSEventArgs : EventArgs
	{
		public NSEventArgs (NSEvent nsEvent)
		{
			Event = nsEvent;
		}

		public bool Handled { get; set; }
		public NSEvent Event { get; set; }
	}

	class ToggleButton : NSButton
	{
		public event EventHandler Focused;

		public override CGSize IntrinsicContentSize => Hidden ? CGSize.Empty : new CGSize (25, 25);

		public ToggleButton () 
		{
			Title = "";
			BezelStyle = NSBezelStyle.RoundRect;
			SetButtonType (NSButtonType.OnOff);
			FocusRingType = NSFocusRingType.Default;
		}

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		public bool Active {
			get => State == NSCellStateValue.On;
			set {
				State = value ? NSCellStateValue.On : NSCellStateValue.Off;
			}
		}

		public override void KeyDown (NSEvent theEvent)
		{
			if ((int)theEvent.ModifierFlags == (int) KeyModifierFlag.None && (theEvent.KeyCode == (int)KeyCodes.Enter || theEvent.KeyCode == (int)KeyCodes.Space)) {
				PerformClick (this);
			}
		}
	}
}
#endif