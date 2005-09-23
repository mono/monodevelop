// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Diagnostics;
using Gtk;

using MonoDevelop.Core.Services;
using MonoDevelop.Gui.Components;
using MonoDevelop.Gui;
using MonoDevelop.Services;

namespace MonoDevelop.Services
{
	internal class DefaultStatusBarService : GuiSyncAbstractService, IStatusBarService
	{
		SdStatusBar statusBar = null;
		StringParserService stringParserService = Runtime.StringParserService;
		
		public DefaultStatusBarService()
		{
			statusBar = new SdStatusBar(this);
		}
		
		protected override void OnInitialize (EventArgs e)
		{
			base.OnInitialize (e);
			Runtime.ProjectService.CombineClosed += new CombineEventHandler (OnCombineClosed);
		}

		public void Dispose()
		{
			if (statusBar != null) {
				statusBar.Dispose();
				statusBar = null;
			}
		}
		
		public Widget Control {
			get {
				Debug.Assert(statusBar != null);
				return statusBar;
			}
		}
		
		public void BeginProgress (string name)
		{
			statusBar.BeginProgress (name);
		}
		
		public void SetProgressFraction (double work)
		{
			statusBar.SetProgressFraction (work);
		}
		
		public void Pulse ()
		{
			statusBar.Pulse ();
		}
		
		public void EndProgress ()
		{
			statusBar.EndProgress ();
		}
		
		public bool CancelEnabled {
			get {
				return statusBar != null && statusBar.CancelEnabled;
			}
			set {
				Debug.Assert(statusBar != null);
				statusBar.CancelEnabled = value;
			}
		}
		
		public IStatusIcon ShowStatusIcon (Gtk.Image image)
		{
			return statusBar.ShowStatusIcon (image);
		}
		
		public void HideStatusIcon (IStatusIcon icon)
		{
			statusBar.HideStatusIcon (icon);
		}
		
		
		[AsyncDispatch]
		public void SetCaretPosition (int ln, int col, int ch)
		{
			statusBar.SetCursorPosition (ln, col, ch);
		}
		
		[AsyncDispatch]
		public void SetInsertMode (bool insertMode)
		{
			statusBar.SetModeStatus (insertMode ? GettextCatalog.GetString ("INS") : GettextCatalog.GetString ("OVR"));
		}
		
		[AsyncDispatch]
		public void ShowErrorMessage (string message)
		{
			Debug.Assert(statusBar != null);
			if (message == null) message = "";
			statusBar.ShowErrorMessage(stringParserService.Parse(message));
		}
		
		[AsyncDispatch]
		public void SetMessage (string message)
		{
			Debug.Assert(statusBar != null);
			if (message == null) message = "";
			statusBar.SetMessage(stringParserService.Parse(message));
		}
		
		[AsyncDispatch]
		public void SetMessage(Gtk.Image image, string message)
		{
			Debug.Assert(statusBar != null);
			if (message == null) message = "";
			statusBar.SetMessage(image, stringParserService.Parse(message));
		}

		void OnCombineClosed (object sender, CombineEventArgs e)
		{
			SetMessage ("");
		}
	}
}
