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
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.Components.MainToolbar;

namespace MonoDevelop.Ide
{
	public static class DesktopService
	{
		static PlatformService platformService;

		static PlatformService PlatformService {
			get {
				if (platformService == null) {
					Initialize ();
				}
				return platformService;
			}
		}

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
			Runtime.ProcessService.SetExternalConsoleHandler (PlatformService.StartConsoleProcess);
			
			FileService.FileRemoved += DispatchService.GuiDispatch (
				new EventHandler<FileEventArgs> (NotifyFileRemoved));
			FileService.FileRenamed += DispatchService.GuiDispatch (
				new EventHandler<FileCopyEventArgs> (NotifyFileRenamed));
		}
		
		public static IEnumerable<DesktopApplication> GetApplications (string filename)
		{
			return PlatformService.GetApplications (filename);
		}
		
		public static string DefaultMonospaceFont {
			get { return PlatformService.DefaultMonospaceFont; }
		}
		
		public static string PlatformName {
			get { return PlatformService.Name; }
		}

		/// <summary>
		/// Used in the text editor. Valid values are found in MonoDevelop.SourceEditor.ControlLeftRightMode in the
		/// source editor project.
		/// </summary>
		public static string DefaultControlLeftRightBehavior {
			get {
				return PlatformService.DefaultControlLeftRightBehavior;
			}
		}
		
		public static void ShowUrl (string url)
		{
			PlatformService.ShowUrl (url);
		}
		
		public static void OpenFile (string filename)
		{
			PlatformService.OpenFile (filename);
		}

		public static void OpenFolder (FilePath folderPath)
		{
			PlatformService.OpenFolder (folderPath);
		}

		public static string GetMimeTypeForUri (string uri)
		{
			return PlatformService.GetMimeTypeForUri (uri);
		}
		
		public static string GetMimeTypeDescription (string mimeType)
		{
			return PlatformService.GetMimeTypeDescription (mimeType);
		}
		
		public static bool GetMimeTypeIsText (string mimeType)
		{
			return PlatformService.GetMimeTypeIsText (mimeType);
		}
		
		public static bool GetMimeTypeIsSubtype (string subMimeType, string baseMimeType)
		{
			return PlatformService.GetMimeTypeIsSubtype (subMimeType, baseMimeType);
		}
		
		public static IEnumerable<string> GetMimeTypeInheritanceChain (string mimeType)
		{
			return PlatformService.GetMimeTypeInheritanceChain (mimeType);
		}
		
		public static Gdk.Pixbuf GetPixbufForFile (string filename, Gtk.IconSize size)
		{
			return PlatformService.GetPixbufForFile (filename, size);
		}
		
		public static Gdk.Pixbuf GetPixbufForType (string mimeType, Gtk.IconSize size)
		{
			return PlatformService.GetPixbufForType (mimeType, size);
		}

		public static bool SetGlobalMenu (MonoDevelop.Components.Commands.CommandManager commandManager,
			string commandMenuAddinPath, string appMenuAddinPath)
		{
			return PlatformService.SetGlobalMenu (commandManager, commandMenuAddinPath, appMenuAddinPath);
		}
		
		// Used for preserve the file attributes when monodevelop opens & writes a file.
		// This should work on unix & mac platform.
		public static object GetFileAttributes (string fileName)
		{
			return PlatformService.GetFileAttributes (fileName);
		}
		
		public static void SetFileAttributes (string fileName, object attributes)
		{
			PlatformService.SetFileAttributes (fileName, attributes);
		}
		
		public static Gdk.Rectangle GetUsableMonitorGeometry (Gdk.Screen screen, int monitor)
		{
			return PlatformService.GetUsableMonitorGeometry (screen, monitor);
		}
		
		public static bool CanOpenTerminal {
			get {
				return PlatformService.CanOpenTerminal;
			}
		}
		
		public static void OpenInTerminal (FilePath directory)
		{
			PlatformService.OpenInTerminal (directory);
		}
		
		public static RecentFiles RecentFiles {
			get {
				return PlatformService.RecentFiles;
			}
		}
		
		static void NotifyFileRemoved (object sender, FileEventArgs args)
		{
			foreach (FileEventInfo e in args) {
				if (!e.IsDirectory) {
					PlatformService.RecentFiles.NotifyFileRemoved (e.FileName);
				}
			}
		}
		
		static void NotifyFileRenamed (object sender, FileCopyEventArgs args)
		{
			foreach (FileCopyEventInfo e in args) {
				if (!e.IsDirectory) {
					PlatformService.RecentFiles.NotifyFileRenamed (e.SourceFile, e.TargetFile);
				}
			}
		}
		
		internal static string GetUpdaterUrl ()
		{
			return PlatformService.GetUpdaterUrl ();
		}
		
		internal static IEnumerable<string> GetUpdaterEnvironmentFlags ()
		{
			return PlatformService.GetUpdaterEnviromentFlags ();
		}
		
		internal static void StartUpdatesInstaller (FilePath installerDataFile, FilePath updatedInstallerPath)
		{
			PlatformService.StartUpdatesInstaller (installerDataFile, updatedInstallerPath);
		}
		
		/// <summary>
		/// Grab the desktop focus for the window.
		/// </summary>
		internal static void GrabDesktopFocus (Gtk.Window window)
		{
			PlatformService.GrabDesktopFocus (window);
		}

		public static void RemoveWindowShadow (Gtk.Window window)
		{
			PlatformService.RemoveWindowShadow (window);
		}


		public static void SetMainWindowDecorations (Gtk.Window window)
		{
			PlatformService.SetMainWindowDecorations (window);
		}

		internal static MainToolbar CreateMainToolbar (Gtk.Window window)
		{
			return PlatformService.CreateMainToolbar (window);
		}

		public static bool GetIsFullscreen (Gtk.Window window)
		{
			return PlatformService.GetIsFullscreen (window);
		}

		public static void SetIsFullscreen (Gtk.Window window, bool isFullscreen)
		{
			PlatformService.SetIsFullscreen (window, isFullscreen);
		}
	}
}
