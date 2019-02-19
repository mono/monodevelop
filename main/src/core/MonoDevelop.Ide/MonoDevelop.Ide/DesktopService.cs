//
// IdeApp.DesktopService.cs
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
using MonoDevelop.Components;
using MonoDevelop.Components.MainToolbar;
using System.Threading.Tasks;

namespace MonoDevelop.Ide
{
	[DefaultServiceImplementation]
	public class DesktopService : Service
	{
		PlatformService platformService;
		Xwt.Toolkit nativeToolkit;

		PlatformService PlatformService {
			get {
				if (platformService == null)
					throw new InvalidOperationException ("Not initialized");
				return platformService;
			}
		}

		protected override Task OnInitialize (ServiceProvider serviceProvider)
		{
			object [] platforms = AddinManager.GetExtensionObjects ("/MonoDevelop/Core/PlatformService");
			if (platforms.Length > 0)
				platformService = (PlatformService)platforms [0];
			else {
				platformService = new DefaultPlatformService ();
				LoggingService.LogFatalError ("A platform service implementation has not been found.");
			}
			PlatformService.Initialize ();

			if (PlatformService.CanOpenTerminal)
				Runtime.ProcessService.SetExternalConsoleHandler (PlatformService.StartConsoleProcess);

			FileService.FileRemoved += NotifyFileRemoved;
			FileService.FileRenamed += NotifyFileRenamed;

			// Ensure we initialize the native toolkit on the UI thread immediately
			// so that we can safely access this property later in other threads
			GC.KeepAlive (NativeToolkit);

			MemoryMonitor = platformService.CreateMemoryMonitor ();
			MemoryMonitor.StatusChanged += OnMemoryStatusChanged;

			ThermalMonitor = platformService.CreateThermalMonitor ();
			ThermalMonitor.StatusChanged += OnThermalStatusChanged;

			return Task.CompletedTask;
		}

		protected override Task OnDispose ()
		{
			if (PlatformService.CanOpenTerminal)
				Runtime.ProcessService.SetExternalConsoleHandler (null);

			FileService.FileRemoved -= NotifyFileRemoved;
			FileService.FileRenamed -= NotifyFileRenamed;

			MemoryMonitor.StatusChanged -= OnMemoryStatusChanged;
			ThermalMonitor.StatusChanged -= OnThermalStatusChanged;

			return Task.CompletedTask;
		}

		void OnMemoryStatusChanged (object sender, PlatformMemoryStatusEventArgs args)
		{
			Counters.MemoryPressure.Inc (args.CounterMetadata);
		}

		void OnThermalStatusChanged (object sender, PlatformThermalStatusEventArgs args)
		{
			Counters.ThermalNotification.Inc (args.CounterMetadata);
		}

		/// <summary>
		/// Returns the XWT toolkit for the native toolkit (Cocoa on Mac, WPF on Windows)
		/// </summary>
		/// <returns>The native toolkit.</returns>
		public Xwt.Toolkit NativeToolkit {
			get {
				if (nativeToolkit == null)
					nativeToolkit = platformService.LoadNativeToolkit ();
				return nativeToolkit;
			}
		}

		public void SetGlobalProgress (double progress)
		{
			platformService.SetGlobalProgressBar (progress);
		}

		public void ShowGlobalProgressIndeterminate ()
		{
			platformService.ShowGlobalProgressBarIndeterminate ();
		}

		public void ShowGlobalProgressError ()
		{
			platformService.ShowGlobalProgressBarError ();
		}

		public IEnumerable<DesktopApplication> GetApplications (string filename)
		{
			return PlatformService.GetApplications (filename);
		}

		internal string DefaultMonospaceFont {
			get { return PlatformService.DefaultMonospaceFont; }
		}

		public string PlatformName {
			get { return PlatformService.Name; }
		}

		public void ShowUrl (string url)
		{
			PlatformService.ShowUrl (url);
		}

		public void OpenFile (string filename)
		{
			PlatformService.OpenFile (filename);
		}

		public void OpenFolder (FilePath folderPath, params FilePath [] selectFiles)
		{
			PlatformService.OpenFolder (folderPath, selectFiles);
		}

		public string GetMimeTypeForRoslynLanguage (string language)
		{
			return PlatformService.GetMimeTypeForRoslynLanguage (language);
		}

		public IEnumerable<string> GetMimeTypeInheritanceChainForRoslynLanguage (string language)
		{
			var mime = GetMimeTypeForRoslynLanguage (language);
			if (mime == null) {
				return null;
			}
			return GetMimeTypeInheritanceChain (mime);
		}

		public string GetMimeTypeForUri (string uri)
		{
			return PlatformService.GetMimeTypeForUri (uri);
		}

		public string GetMimeTypeDescription (string mimeType)
		{
			return PlatformService.GetMimeTypeDescription (mimeType);
		}

		public bool GetMimeTypeIsText (string mimeType)
		{
			return PlatformService.GetMimeTypeIsText (mimeType);
		}

		public bool GetFileIsText (string file, string mimeType = null)
		{
			if (mimeType == null) {
				mimeType = GetMimeTypeForUri (file);
			}
			if (mimeType != "application/octet-stream" && mimeType != "application/x-msdownload") {
				return GetMimeTypeIsText (mimeType);
			}

			if (!File.Exists (file))
				return false;

			return !MonoDevelop.Core.Text.TextFileUtility.IsBinary (file);
		}

		public async Task<bool> GetFileIsTextAsync (string file, string mimeType = null)
		{
			if (mimeType == null) {
				mimeType = GetMimeTypeForUri (file);
			}

			if (mimeType != "application/octet-stream") {
				return GetMimeTypeIsText (mimeType);
			}

			return await Task<bool>.Factory.StartNew (delegate {
				if (!File.Exists (file))
					return false;

				using (var f = File.OpenRead (file)) {
					var buf = new byte [8192];
					var read = f.Read (buf, 0, buf.Length);
					for (int i = 0; i < read; i++)
						if (buf [i] == 0)
							return false;
				}
				return true;
			});
		}

		public bool GetMimeTypeIsSubtype (string subMimeType, string baseMimeType)
		{
			return PlatformService.GetMimeTypeIsSubtype (subMimeType, baseMimeType);
		}

		public IEnumerable<string> GetMimeTypeInheritanceChain (string mimeType)
		{
			return PlatformService.GetMimeTypeInheritanceChain (mimeType);
		}

		public IEnumerable<string> GetMimeTypeInheritanceChainForFile (string filename)
		{
			return GetMimeTypeInheritanceChain (GetMimeTypeForUri (filename));
		}

		public Xwt.Drawing.Image GetIconForFile (string filename)
		{
			return PlatformService.GetIconForFile (filename);
		}

		public Xwt.Drawing.Image GetIconForFile (string filename, Gtk.IconSize size)
		{
			return PlatformService.GetIconForFile (filename).WithSize (size);
		}

		public Xwt.Drawing.Image GetIconForType (string mimeType)
		{
			return PlatformService.GetIconForType (mimeType);
		}

		public Xwt.Drawing.Image GetIconForType (string mimeType, Gtk.IconSize size)
		{
			return PlatformService.GetIconForType (mimeType).WithSize (size);
		}

		internal bool SetGlobalMenu (MonoDevelop.Components.Commands.CommandManager commandManager,
			string commandMenuAddinPath, string appMenuAddinPath)
		{
			return PlatformService.SetGlobalMenu (commandManager, commandMenuAddinPath, appMenuAddinPath);
		}

		// Used for preserve the file attributes when monodevelop opens & writes a file.
		// This should work on unix & mac platform.
		public object GetFileAttributes (string fileName)
		{
			return PlatformService.GetFileAttributes (fileName);
		}

		public void SetFileAttributes (string fileName, object attributes)
		{
			PlatformService.SetFileAttributes (fileName, attributes);
		}

		public Xwt.Rectangle GetUsableMonitorGeometry (int screenNumber, int monitorNumber)
		{
			return PlatformService.GetUsableMonitorGeometry (screenNumber, monitorNumber);
		}

		public bool CanOpenTerminal {
			get {
				return PlatformService.CanOpenTerminal;
			}
		}

		/// <summary>
		/// Opens an external terminal window.
		/// </summary>
		/// <param name="workingDirectory">Working directory.</param>
		/// <param name="environmentVariables">Environment variables.</param>
		/// <param name="windowTitle">Window title.</param>
		public void OpenTerminal (
			FilePath workingDirectory,
			IDictionary<string, string> environmentVariables = null,
			string windowTitle = null)
		{
			PlatformService.OpenTerminal (workingDirectory, environmentVariables, windowTitle);
		}

		public RecentFiles RecentFiles {
			get {
				PlatformService.RecentFiles.DesktopService = this;
				return PlatformService.RecentFiles;
			}
		}

		void NotifyFileRemoved (object sender, FileEventArgs args)
		{
			foreach (FileEventInfo e in args) {
				if (!e.IsDirectory) {
					PlatformService.RecentFiles.NotifyFileRemoved (e.FileName);
				}
			}
		}

		void NotifyFileRenamed (object sender, FileCopyEventArgs args)
		{
			if (args.IsExternal)
				return;

			foreach (FileCopyEventInfo e in args) {
				if (!e.IsDirectory) {
					PlatformService.RecentFiles.NotifyFileRenamed (e.SourceFile, e.TargetFile);
				}
			}
		}

		internal string GetUpdaterUrl ()
		{
			return PlatformService.GetUpdaterUrl ();
		}

		internal IEnumerable<string> GetUpdaterEnvironmentFlags ()
		{
			return PlatformService.GetUpdaterEnviromentFlags ();
		}

		internal void StartUpdatesInstaller (FilePath installerDataFile, FilePath updatedInstallerPath)
		{
			PlatformService.StartUpdatesInstaller (installerDataFile, updatedInstallerPath);
		}

		/// <summary>
		/// Grab the desktop focus for the window.
		/// </summary>
		internal void GrabDesktopFocus (Gtk.Window window)
		{
			PlatformService.GrabDesktopFocus (window);
		}

		public Window GetParentForModalWindow ()
		{
			return PlatformService.GetParentForModalWindow ();
		}

		public Window GetFocusedTopLevelWindow ()
		{
			return PlatformService.GetFocusedTopLevelWindow ();
		}

		public void FocusWindow (Window window)
		{
			if (window != null)
				PlatformService.FocusWindow (window);
		}

		public void RemoveWindowShadow (Window window)
		{
			PlatformService.RemoveWindowShadow (window);
		}


		public void SetMainWindowDecorations (Window window)
		{
			PlatformService.SetMainWindowDecorations (window);
		}

		internal MainToolbarController CreateMainToolbar (Gtk.Window window)
		{
			return new MainToolbarController (PlatformService.CreateMainToolbar (window));
		}

		internal void AttachMainToolbar (Gtk.VBox parent, MainToolbarController toolbar)
		{
			PlatformService.AttachMainToolbar (parent, toolbar.ToolbarView);
			toolbar.Initialize ();
		}

		public bool GetIsFullscreen (Window window)
		{
			return PlatformService.GetIsFullscreen (window);
		}

		public void SetIsFullscreen (Window window, bool isFullscreen)
		{
			PlatformService.SetIsFullscreen (window, isFullscreen);
		}

		public bool IsModalDialogRunning ()
		{
			return PlatformService.IsModalDialogRunning ();
		}

		internal void AddChildWindow (Gtk.Window parent, Gtk.Window child)
		{
			PlatformService.AddChildWindow (parent, child);
		}

		internal void RemoveChildWindow (Gtk.Window parent, Gtk.Window child)
		{
			PlatformService.RemoveChildWindow (parent, child);
		}

		internal void PlaceWindow (Gtk.Window window, int x, int y, int width, int height)
		{
			PlatformService.PlaceWindow (window, x, y, width, height);
		}

		/// <summary>
		/// Restarts MonoDevelop
		/// </summary>
		/// <returns> false if the user cancels exiting. </returns>
		/// <param name="reopenWorkspace"> true to reopen current workspace. </param>
		internal void RestartIde (bool reopenWorkspace)
		{
			PlatformService.RestartIde (reopenWorkspace);
		}

		public bool AccessibilityInUse {
			get {
				return PlatformService.AccessibilityInUse;
			}
		}

		public bool AccessibilityKeyboardFocusInUse {
			get {
				return PlatformService.AccessibilityKeyboardFocusInUse;
			}
		}

		internal string GetNativeRuntimeDescription () => PlatformService.GetNativeRuntimeDescription ();

		public ThermalMonitor ThermalMonitor { get; private set; }
		public MemoryMonitor MemoryMonitor { get; private set; }

		IPlatformTelemetryDetails platformTelemetryDetails;

		public IPlatformTelemetryDetails PlatformTelemetry {
			get {
				if (platformTelemetryDetails == null)
					platformTelemetryDetails = PlatformService.CreatePlatformTelemetryDetails ();
				return platformTelemetryDetails;
			}
		}
	}
}
