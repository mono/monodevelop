//  DefaultStatusBarService.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Diagnostics;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui
{
	internal class DefaultStatusBarService : GuiSyncObject, IStatusBarService
	{
		SdStatusBar statusBar = null;
		
		public DefaultStatusBarService()
		{
			statusBar = new SdStatusBar ();
			IdeApp.ProjectOperations.CombineClosed += new CombineEventHandler (OnCombineClosed);
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
		
		public IStatusIcon ShowStatusIcon (Gdk.Pixbuf image)
		{
			return statusBar.ShowStatusIcon (image);
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
			statusBar.ShowErrorMessage(StringParserService.Parse(message));
		}
		
		[AsyncDispatch]
		public void SetMessage (string message)
		{
			Debug.Assert(statusBar != null);
			if (message == null) message = "";
			statusBar.SetMessage(StringParserService.Parse(message));
		}
		
		[AsyncDispatch]
		public void SetMessage(Gtk.Image image, string message)
		{
			Debug.Assert(statusBar != null);
			if (message == null) message = "";
			statusBar.SetMessage(image, StringParserService.Parse(message));
		}

		void OnCombineClosed (object sender, CombineEventArgs e)
		{
			SetMessage ("");
		}
	}
}
