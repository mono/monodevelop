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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Mono.Addins;
using MonoDevelop.Core;
using Mono.Unix;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Core.Execution;
using MonoDevelop.Components;
using MonoDevelop.Components.MainToolbar;


namespace MonoDevelop.Ide.Desktop
{
	public abstract class PlatformService
	{
		Hashtable iconHash = new Hashtable ();
		static readonly bool UsePlatformFileIcons = false;
		
		public abstract string DefaultMonospaceFont { get; }
		public virtual string DefaultSansFont { get { return null; } }

		public abstract string Name { get; }

		[Obsolete]
		public virtual string DefaultControlLeftRightBehavior {
			get {
				return "MonoDevelop";
			}
		}

		public virtual void Initialize ()
		{
		}

		public virtual void SetGlobalProgressBar (double progress)
		{
		}

		public virtual void ShowGlobalProgressBarError ()
		{
		}

		public virtual void ShowGlobalProgressBarIndeterminate ()
		{
		}

		public virtual void OpenFile (string filename)
		{
			Process.Start (filename);
		}
		
		public virtual void OpenFolder (FilePath folderPath, FilePath[] selectFiles)
		{
			Process.Start (folderPath);
		}
		
		public virtual void ShowUrl (string url)
		{
			Process.Start (url);
		}

		/// <summary>
		/// Loads the XWT toolkit backend for the native toolkit (Cocoa on Mac, WPF on Windows)
		/// </summary>
		/// <returns>The native toolkit.</returns>
		public virtual Xwt.Toolkit LoadNativeToolkit ()
		{
			return Xwt.Toolkit.CurrentEngine;
		}

		public string GetMimeTypeForUri (string uri)
		{
			if (!String.IsNullOrEmpty (uri)) {
				MimeTypeNode mt = FindMimeTypeForFile (uri);
				if (mt != null)
					return mt.Id;
			}
			var mime = OnGetMimeTypeForUri (uri);
			if (mime != null) {
				return mime;
			}

			try {
				if (Path.IsPathRooted (uri) && File.Exists (uri) && !Core.Text.TextFileUtility.IsBinary (uri)) {
					return "text/plain";
				}
			} catch (IOException) {}

			return "application/octet-stream";
		}

		public string GetMimeTypeDescription (string mimeType)
		{
			if (mimeType == "text/plain")
				return GettextCatalog.GetString ("Text file");
			if (mimeType == "application/octet-stream")
				return GettextCatalog.GetString ("Unknown");
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
			
			while (mimeType != null && mimeType != "text/plain" && mimeType != "application/octet-stream") {
				MimeTypeNode mt = FindMimeType (mimeType);
				if (mt != null && !string.IsNullOrEmpty (mt.BaseType))
					mimeType = mt.BaseType;
				else {
					if (mimeType.EndsWith ("+xml", StringComparison.Ordinal))
						mimeType = "application/xml";
					else if (mimeType.StartsWith ("text/", StringComparison.Ordinal) || OnGetMimeTypeIsText (mimeType))
						mimeType = "text/plain";
					else
						break;
				}
				yield return mimeType;
			}
		}

		public string GetMimeTypeForRoslynLanguage (string language)
		{
			foreach (MimeTypeNode mt in mimeTypeNodes) {
				if (mt.RoslynName == language)
					return mt.Id;
			}
			return null;
		}
		
		public Xwt.Drawing.Image GetIconForFile (string filename)
		{
			Xwt.Drawing.Image pic = null;
			
			string icon = GetIconIdForFile (filename);
			if (icon != null)
				pic = ImageService.GetIcon (icon, false);

			if (pic == null && UsePlatformFileIcons)
				pic = Xwt.Desktop.GetFileIcon (filename);

			if (pic == null) {
				string mtype = GetMimeTypeForUri (filename);
				if (mtype != null) {
					foreach (string mt in GetMimeTypeInheritanceChain (mtype)) {
						pic = GetIconForType (mt);
						if (pic != null)
							return pic;
					}
				}
			}
			return pic ?? GetDefaultIcon ();
		}
		
		public Xwt.Drawing.Image GetIconForType (string mimeType)
		{
			Xwt.Drawing.Image bf = (Xwt.Drawing.Image) iconHash [mimeType];
			if (bf != null)
				return bf;
			
			foreach (string type in GetMimeTypeInheritanceChain (mimeType)) {
				// Try getting an icon name for the type
				string icon = GetIconIdForType (type);
				if (icon != null) {
					bf = ImageService.GetIcon (icon, false);
					if (bf != null)
						break;
				}
				
				// Try getting a pixbuff
				if (UsePlatformFileIcons) {
					bf = OnGetIconForType (type);
					if (bf != null)
						break;
				}
			}
			
			if (bf == null)
				bf = GetDefaultIcon ();

			iconHash [mimeType] = bf;
			return bf;
		}

		Xwt.Drawing.Image GetDefaultIcon ()
		{
			string id = "__default";
			Xwt.Drawing.Image bf = (Xwt.Drawing.Image) iconHash [id];
			if (bf != null)
				return bf;

			string icon = DefaultFileIconId;
			if (icon != null)
				bf = ImageService.GetIcon (icon, false);
			if (bf == null)
				bf = DefaultFileIcon;
			if (bf == null)
				bf = ImageService.GetIcon ("md-regular-file", true);
			iconHash [id] = bf;
			return bf;
		}
		
		string GetIconIdForFile (string fileName)
		{
			MimeTypeNode mt = FindMimeTypeForFile (fileName);
			if (mt != null)
				return mt.Icon;
			else
				return OnGetIconIdForFile (fileName);
		}
		
		string GetIconIdForType (string type)
		{
			if (type == "text/plain")
				return "md-text-file-icon";
			MimeTypeNode mt = FindMimeType (type);
			if (mt != null)
				return mt.Icon;
			else if (UsePlatformFileIcons)
				return OnGetIconIdForType (type);
			else
				return null;
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
						mimeTypeNode.ChildNodes.GetEnumerator ();
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
		
		protected virtual string OnGetIconIdForFile (string filename)
		{
			return null;
		}
		
		protected virtual string OnGetIconIdForType (string type)
		{
			return null;
		}
		
		protected virtual Xwt.Drawing.Image OnGetIconForFile (string filename)
		{
			return null;
		}
		
		protected virtual Xwt.Drawing.Image OnGetIconForType (string type)
		{
			return null;
		}
		
		protected virtual string DefaultFileIconId {
			get { return null; }
		}
		
		protected virtual Xwt.Drawing.Image DefaultFileIcon {
			get { return null; }
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

		//must be implemented if CanOpenTerminal returns true
		public virtual ProcessAsyncOperation StartConsoleProcess (
			string command, string arguments, string workingDirectory,
			IDictionary<string, string> environmentVariables,
			string title, bool pauseWhenFinished)
		{
			throw new InvalidOperationException ();
		}

		/// <summary>
		/// True if both OpenTerminal and StartConsoleProcess are implemented.
		/// </summary>
		public virtual bool CanOpenTerminal {
			get { return false; }
		}

		public virtual void OpenTerminal (FilePath directory, IDictionary<string, string> environmentVariables, string title)
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
		
		public virtual Xwt.Rectangle GetUsableMonitorGeometry (int screenNumber, int monitorNumber)
		{
			var screen = Gdk.Display.Default.GetScreen (screenNumber);
			var rect = screen.GetMonitorGeometry (monitorNumber);

			return new Xwt.Rectangle {
				X = rect.X,
				Y = rect.Y,
				Width = rect.Width,
				Height = rect.Height,
			};
		}
		
		/// <summary>
		/// Grab the desktop focus for the window.
		/// </summary>
		internal virtual void GrabDesktopFocus (Gtk.Window window)
		{
			if (Platform.IsWindows && window.IsRealized) {
				/* On Windows calling Present() will break out of window edge snapping mode. */
				window.GdkWindow.Focus (0);
				window.GdkWindow.Raise ();
			} else {
				window.Present ();
			}
		}

		internal virtual void RemoveWindowShadow (Gtk.Window window)
		{
		}

		internal virtual void SetMainWindowDecorations (Gtk.Window window)
		{
		}

		internal virtual IMainToolbarView CreateMainToolbar (Gtk.Window window)
		{
			return new MainToolbar ();
		}

		internal virtual void AttachMainToolbar (Gtk.VBox parent, IMainToolbarView toolbar)
		{
			var toolbarBox = new Gtk.HBox ();
			parent.PackStart (toolbarBox, false, false, 0);
			toolbarBox.PackStart ((MainToolbar)toolbar, true, true, 0);
		}

		public virtual bool GetIsFullscreen (Window window)
		{
			return ((bool?) window.GetNativeWidget <Gtk.Window> ().Data ["isFullScreen"]) ?? false;
		}

		public virtual bool IsModalDialogRunning ()
		{
			var windows = Gtk.Window.ListToplevels ();
			return windows.Any (w => w.Modal && w.Visible);
		}

		public virtual void SetIsFullscreen (Window window, bool isFullscreen)
		{
			Gtk.Window windowControl = window;
			windowControl.Data ["isFullScreen"] = isFullscreen;
			if (isFullscreen) {
				windowControl.Fullscreen ();
			} else {
				windowControl.Unfullscreen ();
				SetMainWindowDecorations (windowControl);
			}
		}

		internal virtual void AddChildWindow (Gtk.Window parent, Gtk.Window child)
		{
		}

		internal virtual void RemoveChildWindow (Gtk.Window parent, Gtk.Window child)
		{
		}

		internal virtual void PlaceWindow (Gtk.Window window, int x, int y, int width, int height)
		{
			window.Move (x, y);
			window.Resize (width, height);
		}

		/// <summary>
		/// Restarts MonoDevelop
		/// </summary>
		/// <param name="reopenWorkspace"> true to reopen current workspace. </param>
		internal virtual void RestartIde (bool reopenWorkspace)
		{
			var reopen = reopenWorkspace && IdeApp.Workspace != null && IdeApp.Workspace.Items.Count > 0;

			FilePath path = Environment.GetCommandLineArgs ()[0];
			if (Platform.IsMac && path.Extension == ".exe")
				path = path.ChangeExtension (null);

			if (!File.Exists (path))
				throw new Exception (path + " not found");

			var proc = new Process ();
				
			var psi = new ProcessStartInfo (path) {
				CreateNoWindow = true,
				UseShellExecute = false,
				WorkingDirectory = Environment.CurrentDirectory,
			};

			var recentWorkspace = reopen ? DesktopService.RecentFiles.GetProjects ().FirstOrDefault ()?.FileName : string.Empty;
			if (!string.IsNullOrEmpty (recentWorkspace))
				psi.Arguments = recentWorkspace;
			
			proc.StartInfo = psi;
			proc.Start ();
		}

		public static bool AccessibilityInUse { get; protected set; }
	}

}
