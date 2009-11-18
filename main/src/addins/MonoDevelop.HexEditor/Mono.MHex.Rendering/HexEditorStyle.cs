// 
// Style.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gdk;

namespace Mono.MHex.Rendering
{
	public class HexEditorStyle
	{
		public virtual Color HexOffset {
			get {
				return new Color (172, 168, 153);
			}
		}
		
		public virtual Color HexOffsetBg {
			get {
				return new Color (255, 255, 255);
			}
		}
		
		public virtual Color HexOffsetHighlighted {
			get {
				return new Gdk.Color (122, 118, 103);
			}
		}
		
		public virtual Color HexDigit {
			get {
				return new Color (0, 0, 0);
			}
		}
		
		public virtual Color HexDigitBg {
			get {
				return new Color (0xff, 0xff, 0xff);
			}
		}
		
		public virtual Color DashedLineFg {
			get {
				return new Color (0, 0, 0);
			}
		}
		
		public virtual Color DashedLineBg {
			get {
				return new Color (255, 255, 255);
			}
		}
		
		public virtual Color IconBarBg {
			get {
				return new Color (0xee, 0xee, 0xec);
			}
		}
		
		public virtual Color IconBarSeperator {
			get {
				return new Color (0xba, 0xbd, 0xb6);
			}
		}
		
		public virtual Color BookmarkColor1 {
			get {
				return new Color (0xff, 0xff, 0xff);
			}
		}
		
		public virtual Color BookmarkColor2 {
			get {
				return new Color (0x72, 0x9f, 0xcf);
			}
		}
		
		public virtual Color Selection {
			get {
				return new Color (0xff, 0xff, 0xff);
			}
		}
		
		public virtual Color SelectionBg {
			get {
				return new Color (0x72, 0x9f, 0xcf);
			}
		}
		
		public virtual Color HighlightOffset {
			get {
				return new Gdk.Color (0x8f, 0x59, 0x02);
			}
		}
	}
}
