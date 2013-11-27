//
// PadCollection.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using System.Linq;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Ide.Gui.Pads;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Gui
{
	// TODO: convert to ReadOnlyCollection
	public class PadCollection: List<Pad>
	{
		internal PadCollection ()
		{
		}

		public Pad PropertyPad {
			get {
				return IdeApp.Workbench.Pads.FirstOrDefault (p => p.Id == "MonoDevelop.DesignerSupport.PropertyPad");
			}
		}

		public Pad DocumentOutlinePad {
			get {
				return IdeApp.Workbench.Pads.FirstOrDefault (p => p.Id == "MonoDevelop.DesignerSupport.DocumentOutlinePad");
			}
		}

		public Pad ToolboxPad {
			get {
				return IdeApp.Workbench.Pads.FirstOrDefault (p => p.Id == "MonoDevelop.DesignerSupport.ToolboxPad");
			}
		}

		public Pad SolutionPad {
			get {
				return IdeApp.Workbench.GetPad<ProjectSolutionPad> ();
			}
		}

		public Pad ErrorsPad {
			get {
				return IdeApp.Workbench.GetPad<ErrorListPad> ();
			}
		}
	}
}

