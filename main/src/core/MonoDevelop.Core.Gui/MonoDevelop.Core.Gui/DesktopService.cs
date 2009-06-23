// 
// DesktopService.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using System.Collections.Generic;
using Mono.Addins;

namespace MonoDevelop.Core.Gui
{
	public class DesktopService
	{
		static PlatformService platformService;
		
		public static void Initialize ()
		{
			if (platformService != null)
				return;
			object[] platforms = AddinManager.GetExtensionObjects ("/MonoDevelop/Core/PlatformService");
			if (platforms.Length > 0)
				platformService = (PlatformService) platforms [0];
			else {
				platformService = new DefaultPlatformService ();
				LoggingService.LogFatalError ("A platform service implementation has not been found.");
			}
		}
		
		public static DesktopApplication GetDefaultApplication (string mimetype)
		{
			return platformService.GetDefaultApplication (mimetype);
		}
		
		public static DesktopApplication [] GetAllApplications (string mimetype)
		{
			return platformService.GetAllApplications (mimetype);
		}
		
		public static string DefaultMonospaceFont {
			get { return platformService.DefaultMonospaceFont; }
		}
		
		public static string PlatformName {
			get { return platformService.Name; }
		}

		/// <summary>
		/// Used in the text editor. Valid values are found in MonoDevelop.SourceEditor.ControlLeftRightMode in the
		/// source editor project.
		/// </summary>
		public static string DefaultControlLeftRightBehavior {
			get {
				return platformService.DefaultControlLeftRightBehavior;
			}
		}
		
		public static void ShowUrl (string url)
		{
			platformService.ShowUrl (url);
		}

		public static string GetMimeTypeForUri (string uri)
		{
			return platformService.GetMimeTypeForUri (uri);
		}
		
		public static string GetMimeTypeDescription (string mimeType)
		{
			return platformService.GetMimeTypeDescription (mimeType);
		}
		
		public static bool GetMimeTypeIsText (string mimeType)
		{
			return platformService.GetMimeTypeIsText (mimeType);
		}
		
		public static bool GetMimeTypeIsSubtype (string subMimeType, string baseMimeType)
		{
			return platformService.GetMimeTypeIsSubtype (subMimeType, baseMimeType);
		}
		
		public static IEnumerable<string> GetMimeTypeInheritanceChain (string mimeType)
		{
			return platformService.GetMimeTypeInheritanceChain (mimeType);
		}
		
		public static Gdk.Pixbuf GetPixbufForFile (string filename, Gtk.IconSize size)
		{
			return platformService.GetPixbufForFile (filename, size);
		}
		
		public static Gdk.Pixbuf GetPixbufForType (string mimeType, Gtk.IconSize size)
		{
			return platformService.GetPixbufForFile (mimeType, size);
		}
		
		public static bool SetGlobalMenu (MonoDevelop.Components.Commands.CommandManager commandManager, string commandMenuAddinPath)
		{
			return platformService.SetGlobalMenu (commandManager, commandMenuAddinPath);
		}
		
		// Used for preserve the file attributes when monodevelop opens & writes a file.
		// This should work on unix & mac platform.
		public static object GetFileAttributes (string fileName)
		{
			return platformService.GetFileAttributes (fileName);
		}
		
		public static void SetFileAttributes (string fileName, object attributes)
		{
			platformService.SetFileAttributes (fileName, attributes);
		}
	}
}
