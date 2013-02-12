//
// PlatformService.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Mono.Addins;
using MonoDevelop.Core;
using Mono.Unix;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Core.Execution;
using MonoDevelop.Components.MainToolbar;


namespace MonoDevelop.Ide.Desktop
{
	public abstract class PlatformService
	{
		Hashtable iconHash = new Hashtable ();
		
		public abstract string DefaultMonospaceFont { get; }
		public abstract string Name { get; }

		/// <summary>
		/// Used in the text editor. Valid values are found in MonoDevelop.SourceEditor.ControlLeftRightMode in the
		/// source editor project.
		/// </summary>
		public virtual string DefaultControlLeftRightBehavior {
			get {
				return "MonoDevelop";
			}
		}
		
		public virtual void OpenFile (string filename)
		{
			Process.Start ("file://" + filename);
		}
		
		public virtual void OpenFolder (FilePath folderPath)
		{
			Process.Start ("file://" + folderPath);
		}
		
		public virtual void ShowUrl (string url)
		{
			Process.Start (url);
		}

		public string GetMimeTypeForUri (string uri)
		{
			if (!String.IsNullOrEmpty (uri)) {
// Creating file infos is expensive, should be avoided 
//				FileInfo file = new FileInfo (uri);
//				MimeTypeNode mt = FindMimeTypeForFile (file.Name);
				MimeTypeNode mt = FindMimeTypeForFile (uri);
				if (mt != null)
					return mt.Id;
			}
			return OnGetMimeTypeForUri (uri) ?? "text/plain";
		}
		
		public string GetMimeTypeDescription (string mimeType)
		{
			if (mimeType == "text/plain")
				return GettextCatalog.GetString ("Text file");
			MimeTypeNode mt = FindMimeType (mimeType);
			if (mt != null && mt.Description != null)
				return mt.Description;
			else
				return OnGetMimeTypeDescription (mimeType) ?? string.Empty;
		}
		
		public bool GetMimeTypeIsText (string mimeType)
		{
			return GetMimeTypeIsSubtype (mimeType, "text/plain");
		}
		
		public bool GetMimeTypeIsSubtype (string subMimeType, string baseMimeType)
		{
			foreach (string mt in GetMimeTypeInheritanceChain (subMimeType))
				if (mt == baseMimeType)
					return true;
			return false;
		}
		
		public IEnumerable<string> GetMimeTypeInheritanceChain (string mimeType)
		{
			yield return mimeType;
			
			while (mimeType != null && mimeType != "text/plain") {
				MimeTypeNode mt = FindMimeType (mimeType);
				if (mt != null && !string.IsNullOrEmpty (mt.BaseType))
					mimeType = mt.BaseType;
				else {
					if (mimeType.EndsWith ("+xml"))
						mimeType = "application/xml";
					else if (mimeType.StartsWith ("text") || OnGetMimeTypeIsText (mimeType))
						mimeType = "text/plain";
					else
						break;
				}
				yield return mimeType;
			}
		}
		
		public Gdk.Pixbuf GetPixbufForFile (string filename, Gtk.IconSize size)
		{
			Gdk.Pixbuf pic = null;
			
			string icon = GetIconForFile (filename);
			if (icon != null)
				pic = ImageService.GetPixbuf (icon, size, false);
			
			if (pic == null)
				pic = OnGetPixbufForFile (filename, size);
			
			if (pic == null) {
				string mtype = GetMimeTypeForUri (filename);
				if (mtype != null) {
					foreach (string mt in GetMimeTypeInheritanceChain (mtype)) {
						pic = GetPixbufForType (mt, size);
						if (pic != null)
							return pic;
					}
				}
			}
			return pic ?? GetDefaultIcon (size);
		}
		
		public Gdk.Pixbuf GetPixbufForType (string mimeType, Gtk.IconSize size)
		{
			Gdk.Pixbuf bf = (Gdk.Pixbuf) iconHash [mimeType+size];
			if (bf != null)
				return bf;
			
			foreach (string type in GetMimeTypeInheritanceChain (mimeType)) {
				// Try getting an icon name for the type
				string icon = GetIconForType (type);
				if (icon != null) {
					bf = ImageService.GetPixbuf (icon, size, false);
					if (bf != null)
						break;
				}
				
				// Try getting a pixbuff
				bf = OnGetPixbufForType (type, size);
				if (bf != null)
					break;
			}
			
			if (bf == null) {
				bf = ImageService.GetPixbuf (mimeType, size, false);
				if (bf == null)
					bf = GetDefaultIcon (size);
			}
			iconHash [mimeType+size] = bf;
			return bf;
		}
		
		Gdk.Pixbuf GetDefaultIcon (Gtk.IconSize size)
		{
			string id = "__default" + size;
			Gdk.Pixbuf bf = (Gdk.Pixbuf) iconHash [id];
			if (bf != null)
				return bf;

			string icon = DefaultFileIcon;
			if (icon != null)
				bf = ImageService.GetPixbuf (icon, size, false);
			if (bf == null)
				bf = OnGetDefaultFileIcon (size);
			if (bf == null)
				bf = ImageService.GetPixbuf ("md-regular-file", size, true);
			iconHash [id] = bf;
			return bf;
		}
		
		string GetIconForFile (string fileName)
		{
			MimeTypeNode mt = FindMimeTypeForFile (fileName);
			if (mt != null)
				return mt.Icon;
			else
				return OnGetIconForFile (fileName);
		}
		
		string GetIconForType (string type)
		{
			if (type == "text/plain")
				return "md-text-file-icon";
			MimeTypeNode mt = FindMimeType (type);
			if (mt != null)
				return mt.Icon;
			else
				return OnGetIconForType (type);
		}

		static List<MimeTypeNode> mimeTypeNodes = new List<MimeTypeNode> ();
		static PlatformService ()
		{
			if (AddinManager.IsInitialized) {
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/MimeTypes", delegate (object sender, ExtensionNodeEventArgs args) {
					var newList = new List<MimeTypeNode> (mimeTypeNodes);
					var mimeTypeNode = (MimeTypeNode)args.ExtensionNode;
					switch (args.Change) {
					case ExtensionChange.Add:
							// initialize child nodes.
						var initialize = mimeTypeNode.ChildNodes;
						newList.Add (mimeTypeNode);
						break;
					case ExtensionChange.Remove:
						newList.Remove (mimeTypeNode);
						break;
					}
					mimeTypeNodes = newList;
				});
			}
		}

		MimeTypeNode FindMimeTypeForFile (string fileName)
		{
			foreach (MimeTypeNode mt in mimeTypeNodes) {
				if (mt.SupportsFile (fileName))
					return mt;
			}
			return null;
		}
		
		MimeTypeNode FindMimeType (string type)
		{
			foreach (MimeTypeNode mt in mimeTypeNodes) {
				if (mt.Id == type)
					return mt;
			}
			return null;
		}
		
		protected virtual string OnGetMimeTypeForUri (string uri)
		{
			return null;
		}

		protected virtual string OnGetMimeTypeDescription (string mimeType)
		{
			return null;
		}
		
		protected virtual bool OnGetMimeTypeIsText (string mimeType)
		{
			return false;
		}
		
		protected virtual string OnGetIconForFile (string filename)
		{
			return null;
		}
		
		protected virtual string OnGetIconForType (string type)
		{
			return null;
		}
		
		protected virtual Gdk.Pixbuf OnGetPixbufForFile (string filename, Gtk.IconSize size)
		{
			return null;
		}
		
		protected virtual Gdk.Pixbuf OnGetPixbufForType (string type, Gtk.IconSize size)
		{
			return null;
		}
		
		protected virtual string DefaultFileIcon {
			get { return null; }
		}
		
		protected virtual Gdk.Pixbuf OnGetDefaultFileIcon (Gtk.IconSize size)
		{
			return null;
		}
		
		public virtual bool SetGlobalMenu (MonoDevelop.Components.Commands.CommandManager commandManager,
			string commandMenuAddinPath, string appMenuAddinPath)
		{
			return false;
		}
		
		// Used for preserve the file attributes when monodevelop opens & writes a file.
		// This should work on unix & mac platform.
		public virtual object GetFileAttributes (string fileName)
		{
			UnixFileSystemInfo info = UnixFileSystemInfo.GetFileSystemEntry (fileName);
			if (info == null)
				return null;
			return info.FileAccessPermissions;
		}
		
		public virtual void SetFileAttributes (string fileName, object attributes)
		{
			if (attributes == null)
				return;
			UnixFileSystemInfo info = UnixFileSystemInfo.GetFileSystemEntry (fileName);
			info.FileAccessPermissions = (FileAccessPermissions)attributes;
		}
		
		public virtual IProcessAsyncOperation StartConsoleProcess (string command, string arguments, string workingDirectory,
		                                                           IDictionary<string, string> environmentVariables, 
		                                                           string title, bool pauseWhenFinished)
		{
			return null;
		}
		
		public virtual bool CanOpenTerminal {
			get {
				return false;
			}
		}
		
		public virtual void OpenInTerminal (FilePath directory)
		{
			throw new InvalidOperationException ();
		}
		
		protected virtual RecentFiles CreateRecentFilesProvider ()
		{
			return new FdoRecentFiles ();
		}
		
		RecentFiles recentFiles;
		public RecentFiles RecentFiles {
			get {
				return recentFiles ?? (recentFiles = CreateRecentFilesProvider ());
			}
		}
		
		public virtual string GetUpdaterUrl ()
		{
			return null;
		}
		
		public virtual IEnumerable<string> GetUpdaterEnviromentFlags ()
		{
			return new string[0];
		}

		
		/// <summary>
		/// Starts the installer.
		/// </summary>
		/// <param name='installerDataFile'>
		/// File containing the list of updates to install
		/// </param>
		/// <param name='updatedInstallerPath'>
		/// Optional path to an updated installer executable
		/// </param>
		/// <remarks>
		/// This method should start the installer in an independent process.
		/// </remarks>
		public virtual void StartUpdatesInstaller (FilePath installerDataFile, FilePath updatedInstallerPath)
		{
		}
		
		public virtual IEnumerable<DesktopApplication> GetApplications (string filename)
		{
			return new DesktopApplication[0];
		}
		
		public virtual Gdk.Rectangle GetUsableMonitorGeometry (Gdk.Screen screen, int monitor)
		{
			return screen.GetMonitorGeometry (monitor);
		}
		
		/// <summary>
		/// Grab the desktop focus for the window.
		/// </summary>
		public virtual void GrabDesktopFocus (Gtk.Window window)
		{
			window.Present ();
		}

		internal virtual void RemoveWindowShadow (Gtk.Window window)
		{
		}

		internal virtual void SetMainWindowDecorations (Gtk.Window window)
		{
		}

		internal virtual MainToolbar CreateMainToolbar (Gtk.Window window)
		{
			return new MainToolbar ();
		}

		public virtual bool GetIsFullscreen (Gtk.Window window)
		{
			return ((bool?) window.Data ["isFullScreen"]) ?? false;
		}

		public virtual void SetIsFullscreen (Gtk.Window window, bool isFullscreen)
		{
			window.Data ["isFullScreen"] = isFullscreen;
			if (isFullscreen) {
				window.Fullscreen ();
			} else {
				window.Unfullscreen ();
				SetMainWindowDecorations (window);
			}
		}
	}
}