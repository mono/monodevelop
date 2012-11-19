//
// StatusBarContextImpl.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Components.MainToolbar;

using StockIcons = MonoDevelop.Ide.Gui.Stock;

namespace MonoDevelop.Ide
{
	class StatusBarContextImpl: StatusBarContext
	{
		IconId image;
		string message;
		bool isMarkup;
		double progressFraction;
		bool showProgress;
		Pad sourcePad;
		bool autoPulse;
		protected StatusArea statusBar;

		// The last time any status bar context changed the status area message
		static DateTime globalLastChangeTime;
		static bool lastMessageIsTransient;

		// The last time this context changed the status area message
		DateTime lastChangeTime;
		bool messageShownAfterProgress;

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
			ShowMessage (StockIcons.StatusError, error);
		}
		
		public void ShowWarning (string warning)
		{
			ShowMessage (StockIcons.StatusWarning, warning);
		}
		
		public void ShowMessage (string message)
		{
			ShowMessage (null, message, false);
		}
		
		public void ShowMessage (string message, bool isMarkup)
		{
			ShowMessage (null, message, isMarkup);
		}
		
		public void ShowMessage (IconId image, string message)
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
		
		public void ShowMessage (IconId image, string message, bool isMarkup)
		{
			if (!showProgress)
				messageShownAfterProgress = true;
			this.image = image;
			this.message = message;
			this.isMarkup = isMarkup;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this)) {
				OnMessageChanged ();
				globalLastChangeTime = DateTime.Now;
				statusBar.ShowMessage (image, message, isMarkup);
				statusBar.SetMessageSourcePad (sourcePad);
				if (showProgress)
					lastMessageIsTransient = true;
				else {
					lastMessageIsTransient = false;
					ResetMessage (); // Once the message is shown, don't show it again
				}
			} else
				lastChangeTime = DateTime.Now;
		}
		
		public void BeginProgress (string name)
		{
			messageShownAfterProgress = false;
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
				lastMessageIsTransient = true;
			}
		}
		
		public void BeginProgress (IconId image, string name)
		{
			messageShownAfterProgress = false;
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
				lastMessageIsTransient = true;
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
			ResetMessage ();
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
				lastMessageIsTransient = true;
			} else {
				statusBar.EndProgress ();
				if (globalLastChangeTime < lastChangeTime && messageShownAfterProgress) {
					globalLastChangeTime = lastChangeTime;
					statusBar.ShowMessage (image, message, isMarkup);
					statusBar.SetMessageSourcePad (sourcePad);
					ResetMessage (); // Once the message is shown, don't show it again
					lastMessageIsTransient = !messageShownAfterProgress;
				} else if (lastMessageIsTransient)
					statusBar.ShowReady ();
			}
		}

		void ResetMessage ()
		{
			image = IconId.Null;
			message = string.Empty;
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
	
}
