// GuiService.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
//
//


using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using System;
using Mono.Addins;
using Mono.Addins.Gui;

namespace MonoDevelop.Core.Gui
{
	public class Services
	{
		static PlatformService platformService;
		
		static Services ()
		{
			object[] platforms = AddinManager.GetExtensionObjects ("/MonoDevelop/Core/PlatformService");
			if (platforms.Length > 0)
				platformService = (PlatformService) platforms [0];
			else {
				platformService = new DefaultPlatformService ();
				LoggingService.LogFatalError ("A platform service implementation has not been found.");
			}
		}
		
		public static PlatformService PlatformService {
			get {
				return platformService;
			}
		}
	
		public static void RunAddinManager (Gtk.Window parent)
		{
			AddinManagerWindow.Run (parent);
		}
	}
}
