//
// IDialgoPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using Gtk;

using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui.Dialogs
{
	public enum DialogMessage {
		OK,
		Cancel,
		Help,
		Next,
		Prev,
		Finish,
		Activated
	}
	
	public interface IDialogPanel
	{
		Widget Control {
			get;
		}
		
		Gtk.Image Icon {
			get;
		}
		
		object CustomizationObject {
			get;
			set;
		}
		
		bool WasActivated {
			get;
		}
		
		bool ReceiveDialogMessage (DialogMessage message);
		
		event EventHandler EnableFinishChanged;
		bool EnableFinish {
			get;
		}
	}
	
	public interface IDialogPanelDescriptor
	{
		string ID {
			get;
		}
		
		string Label {
			get;
			set;
		}
		
		List<IDialogPanelDescriptor> DialogPanelDescriptors {
			get;
			set;
		}
		
		IDialogPanel DialogPanel {
			get;
			set;
		}
	}
}
