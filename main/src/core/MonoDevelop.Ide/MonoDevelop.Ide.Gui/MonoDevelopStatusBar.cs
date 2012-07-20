//
// MonoDevelopStatusBar.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Components.MainToolbar;

namespace MonoDevelop.Ide
{
	/// <summary>
	/// The MonoDevelop status bar.
	/// </summary>
	public interface StatusBar: StatusBarContextBase
	{
		StatusBar MainContext { get; }
		/// <summary>
		/// Shows a status icon in the toolbar. The icon can be removed by disposing
		/// the StatusBarIcon instance.
		/// </summary>
		StatusBarIcon ShowStatusIcon (Gdk.Pixbuf pixbuf);
		
		/// <summary>
		/// Creates a status bar context. The returned context can be used to show status information
		/// which will be cleared when the context is disposed. When several contexts are created,
		/// the status bar will show the status of the latest created context.
		/// </summary>
		StatusBarContext CreateContext ();
		
		// Clears the status bar information
		void ShowReady ();
		
		/// <summary>
		/// Sets a pad which has detailed information about the status message. When clicking on the
		/// status bar, this pad will be activated. This source pad is reset at every ShowMessage call.
		/// </summary>
		void SetMessageSourcePad (Pad pad);
		
		/// <summary>
		/// When set to true, the resize grip is shown
		/// </summary>
		bool HasResizeGrip { get; set; }
	}
	
	public interface StatusBarContextBase: IDisposable
	{
		/// <summary>
		/// Shows a message with an error icon
		/// </summary>
		void ShowError (string error);
		
		/// <summary>
		/// Shows a message with a warning icon
		/// </summary>
		void ShowWarning (string warning);
		
		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		void ShowMessage (string message);
		
		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		void ShowMessage (string message, bool isMarkup);
		
		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		void ShowMessage (Image image, string message);
		
		/// <summary>
		/// Shows a progress bar, with the provided label next to it
		/// </summary>
		void BeginProgress (string name);
		
		/// <summary>
		/// Shows a progress bar, with the provided label and icon next to it
		/// </summary>
		void BeginProgress (Image image, string name);
		
		/// <summary>
		/// Sets the progress fraction. It can only be used after calling BeginProgress.
		/// </summary>
		void SetProgressFraction (double work);

		/// <summary>
		/// Hides the progress bar shown with BeginProgress
		/// </summary>
		void EndProgress ();
		
		/// <summary>
		/// Pulses the progress bar shown with BeginProgress
		/// </summary>
		void Pulse ();
		
		/// <summary>
		/// When set, the status bar progress will be automatically pulsed at short intervals
		/// </summary>
		bool AutoPulse { get; set; }
	}
	
	public interface StatusBarContext: StatusBarContextBase
	{
		Pad StatusSourcePad { get; set; }
	}
	
	public interface StatusBarIcon : IDisposable
	{
		/// <summary>
		/// Tooltip of the status icon
		/// </summary>
		string ToolTip { get; set; }
		
		/// <summary>
		/// Event box which can be used to subscribe mouse events on the icon
		/// </summary>
		EventBox EventBox { get; }
		
		/// <summary>
		/// The icon
		/// </summary>
		Gdk.Pixbuf Image { get; set; }
		
		/// <summary>
		/// Sets alert mode. The icon will flash for the provided number of seconds.
		/// </summary>
		void SetAlertMode (int seconds);
	}
	
	class StatusBarContextImpl: StatusBarContext
	{
		Image image;
		string message;
		bool isMarkup;
		double progressFraction;
		bool showProgress;
		Pad sourcePad;
		bool autoPulse;
		protected StatusArea statusBar;
		
		internal bool StatusChanged { get; set; }
		
		internal StatusBarContextImpl (StatusArea statusBar)
		{
			this.statusBar = statusBar;
		}
		
		public void Dispose ()
		{
			statusBar.Remove (this);
		}
		
		public void ShowError (string error)
		{
			ShowMessage (new Image (MonoDevelop.Ide.Gui.Stock.Error, IconSize.Menu), error);
		}
		
		public void ShowWarning (string warning)
		{
			ShowMessage (new Gtk.Image (MonoDevelop.Ide.Gui.Stock.Warning, IconSize.Menu), warning);
		}
		
		public void ShowMessage (string message)
		{
			ShowMessage (null, message, false);
		}
		
		public void ShowMessage (string message, bool isMarkup)
		{
			ShowMessage (null, message, isMarkup);
		}
		
		public void ShowMessage (Image image, string message)
		{
			ShowMessage (image, message, false);
		}
		
		bool InitialSetup ()
		{
			if (!StatusChanged) {
				StatusChanged = true;
				statusBar.UpdateActiveContext ();
				return true;
			} else
				return false;
		}
		
		public void ShowMessage (Image image, string message, bool isMarkup)
		{
			this.image = image;
			this.message = message;
			this.isMarkup = isMarkup;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this)) {
				OnMessageChanged ();
				statusBar.ShowMessage (image, message, isMarkup);
				statusBar.SetMessageSourcePad (sourcePad);
			}
		}
		
		public void BeginProgress (string name)
		{
			image = null;
			isMarkup = false;
			progressFraction = 0;
			message = name;
			showProgress = true;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this)) {
				OnMessageChanged ();
				statusBar.BeginProgress (name);
				statusBar.SetMessageSourcePad (sourcePad);
			}
		}
		
		public void BeginProgress (Image image, string name)
		{
			this.image = image;
			isMarkup = false;
			progressFraction = 0;
			message = name;
			showProgress = true;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this)) {
				OnMessageChanged ();
				statusBar.BeginProgress (name);
				statusBar.SetMessageSourcePad (sourcePad);
			}
		}
		
		public void SetProgressFraction (double work)
		{
			progressFraction = work;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this))
				statusBar.SetProgressFraction (work);
		}
		
		public void EndProgress ()
		{
			showProgress = false;
			message = string.Empty;
			progressFraction = 0;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this))
				statusBar.EndProgress ();
		}
		
		public void Pulse ()
		{
			showProgress = true;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this))
				statusBar.Pulse ();
		}
		
		public bool AutoPulse {
			get {
				return autoPulse;
			}
			set {
				if (value)
					showProgress = true;
				autoPulse = value;
				if (InitialSetup ())
					return;
				if (statusBar.IsCurrentContext (this))
					statusBar.AutoPulse = value;
			}
		}
		
		
		internal void Update ()
		{
			if (showProgress) {
				statusBar.BeginProgress (image, message);
				statusBar.SetProgressFraction (progressFraction);
				statusBar.AutoPulse = autoPulse;
				statusBar.SetMessageSourcePad (sourcePad);
			} else {
				statusBar.EndProgress ();
				statusBar.ShowMessage (image, message, isMarkup);
				statusBar.SetMessageSourcePad (sourcePad);
			}
		}
		
		public Pad StatusSourcePad {
			get {
				return sourcePad;
			}
			set {
				sourcePad = value;
			}
		}
		
		protected virtual void OnMessageChanged ()
		{
		}
	}
	
	class MainStatusBarContextImpl: StatusBarContextImpl, StatusBar
	{
		public StatusBar MainContext {
			get {
				return this;
			}
		}
		public MainStatusBarContextImpl (StatusArea statusBar): base (statusBar)
		{
			StatusChanged = true;
		}
		
		public void ShowCaretState (int line, int column, int selectedChars, bool isInInsertMode)
		{
			statusBar.ShowCaretState (line, column, selectedChars, isInInsertMode);
		}
		
		public void ClearCaretState ()
		{
			statusBar.ClearCaretState ();
		}
		
		public StatusBarIcon ShowStatusIcon (Gdk.Pixbuf pixbuf)
		{
			return statusBar.ShowStatusIcon (pixbuf);
		}
		
		public StatusBarContext CreateContext ()
		{
			return statusBar.CreateContext ();
		}
		
		public void ShowReady ()
		{
			statusBar.ShowReady ();
		}
		
		public void SetMessageSourcePad (Pad pad)
		{
			StatusSourcePad = pad;
			if (statusBar.IsCurrentContext (this))
				statusBar.SetMessageSourcePad (pad);
		}
		
		protected override void OnMessageChanged ()
		{
			StatusSourcePad = null;
		}
		
		public bool HasResizeGrip {
			get {
				return statusBar.HasResizeGrip;
			}
			set {
				statusBar.HasResizeGrip = value;
			}
		}
		
	}
}
