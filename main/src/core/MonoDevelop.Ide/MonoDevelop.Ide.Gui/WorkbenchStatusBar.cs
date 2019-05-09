//
// WorkbenchStatusBar.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using Xwt.Drawing;

namespace MonoDevelop.Ide.Gui
{
	class WorkbenchStatusBar : StatusBar
	{
		StatusBar delegatedStatusBar;
		bool autoPulse;
		string progressMessage;
		IconId progressImage;
		bool inProgress;
		StatusBarContextHandler ctxHandler;
		CancellationTokenSource cancellationTokenSource;
		Pad messageSourcePad;
		double? progressFraction;
		string message;
		MessageType messageType;
		bool messageIsMarkup;
		IconId messageIcon;

		enum MessageType { None, Error, Warning, Message, Ready }

		List<StatusBarIconWrapper> icons = new List<StatusBarIconWrapper> ();

		public WorkbenchStatusBar ()
		{
			ctxHandler = new StatusBarContextHandler (this);
		}

		public void Attach (StatusBar statusBar)
		{
			delegatedStatusBar = statusBar;
			if (autoPulse)
				statusBar.AutoPulse = true;
			if (inProgress) {
				if (progressImage != IconId.Null)
					statusBar.BeginProgress (progressMessage, progressImage);
				else
					statusBar.BeginProgress (progressMessage);
			}
			if (cancellationTokenSource != null)
				statusBar.SetCancellationTokenSource (cancellationTokenSource);
			if (messageSourcePad != null)
				statusBar.SetMessageSourcePad (messageSourcePad);
			if (progressFraction != null)
				statusBar.SetProgressFraction (progressFraction.Value);
			if (messageType != MessageType.None) {
				switch (messageType) {
				case MessageType.Error:
					statusBar.ShowError (message);
					break;
				case MessageType.Warning:
					statusBar.ShowWarning (message);
					break;
				case MessageType.Ready:
					statusBar.ShowReady ();
					break;
				default:
					statusBar.ShowMessage (messageIcon, message, messageIsMarkup);
					break;
				}
			}
			foreach (var icon in icons)
				icon.Attach (statusBar);

			cancellationTokenSource = null;
			messageSourcePad = null;
			icons.Clear ();
		}

		public StatusBar MainContext => delegatedStatusBar?.MainContext ?? this;

		public bool AutoPulse {
			get {
				return delegatedStatusBar != null ? delegatedStatusBar.AutoPulse : autoPulse;
			}
			set {
				if (delegatedStatusBar != null)
					delegatedStatusBar.AutoPulse = value;
				else
					autoPulse = value;
			}
		}

		public void BeginProgress (string name)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.BeginProgress (name);
			else {
				inProgress = true;
				progressMessage = name;
			}
		}

		public void BeginProgress (IconId image, string name)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.BeginProgress (image, name);
			else {
				inProgress = true;
				progressMessage = name;
				progressImage = image;
			}
		}

		public StatusBarContext CreateContext ()
		{
			return ctxHandler.CreateContext ();
		}

		public void Dispose ()
		{
			delegatedStatusBar?.Dispose ();
		}

		public void EndProgress ()
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.EndProgress ();
			else {
				inProgress = false;
				progressMessage = null;
				progressImage = IconId.Null;
			}
		}

		public void Pulse ()
		{
			delegatedStatusBar?.Pulse ();
		}

		public void SetCancellationTokenSource (CancellationTokenSource source)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.SetCancellationTokenSource (source);
			else
				cancellationTokenSource = source;
		}

		public void SetMessageSourcePad (Pad pad)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.SetMessageSourcePad (pad);
			else
				messageSourcePad = pad;
		}

		public void SetProgressFraction (double work)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.SetProgressFraction (work);
			else
				progressFraction = work;
		}

		public void ShowError (string error)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.ShowError (error);
			else
				StoreMessage (MessageType.Error, message);
		}

		public void ShowMessage (string message)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.ShowMessage (message);
			else
				StoreMessage (MessageType.Message, message);
		}

		public void ShowMessage (string message, bool isMarkup)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.ShowMessage (message, isMarkup);
			else
				StoreMessage (MessageType.Message, message, isMarkup:isMarkup);
		}

		public void ShowMessage (IconId image, string message)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.ShowMessage (image, message);
			else
				StoreMessage (MessageType.Message, message, image);
		}

		public void ShowMessage (IconId image, string message, bool isMarkup)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.ShowMessage (image, message, isMarkup);
			else
				StoreMessage (MessageType.Message, message, image, isMarkup);
		}

		public void ShowReady ()
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.ShowReady ();
			else
				StoreMessage (MessageType.Ready, null);
		}

		public void ShowWarning (string warning)
		{
			if (delegatedStatusBar != null)
				delegatedStatusBar.ShowWarning (warning);
			else
				StoreMessage (MessageType.Warning, message);
		}

		void StoreMessage (MessageType messageType, string message, IconId image = default(IconId), bool isMarkup = false)
		{
			this.messageType = messageType;
			this.message = message;
			messageIsMarkup = isMarkup;
			messageIcon = image;
		}

		public StatusBarIcon ShowStatusIcon (Image pixbuf)
		{
			if (delegatedStatusBar != null)
				return delegatedStatusBar.ShowStatusIcon (pixbuf);

			var icon = new StatusBarIconWrapper (this, pixbuf);
			icons.Add (icon);
			return icon;
		}

		class StatusBarIconWrapper : StatusBarIcon
		{
			WorkbenchStatusBar parent;
			StatusBarIcon wrapped;
			Image pixbuf;
			string title;
			string tooltip;
			string help;

			public StatusBarIconWrapper (WorkbenchStatusBar parent, Image pixbuf)
			{
				this.parent = parent;
				this.pixbuf = pixbuf;
			}

			public void Attach (StatusBar delegatedStatusBar)
			{
				wrapped = delegatedStatusBar.ShowStatusIcon (pixbuf);
				wrapped.Clicked += Wrapped_Clicked;
				if (title != null)
					wrapped.Title = title;
				if (tooltip != null)
					wrapped.ToolTip = tooltip;
				if (help != null)
					wrapped.Help = help;
			}

			private void Wrapped_Clicked (object sender, StatusBarIconClickedEventArgs e)
			{
				Clicked?.Invoke (this, e);
			}

			public string Title {
				get { return wrapped != null ? wrapped.Title : title; }
				set {
					if (wrapped != null)
						wrapped.Title = value;
					else
						title = value;
				}
			}

			public string ToolTip {
				get { return wrapped != null ? wrapped.ToolTip : tooltip; }
				set {
					if (wrapped != null)
						wrapped.ToolTip = value;
					else
						tooltip = value;
				}
			}

			public string Help {
				get { return wrapped != null ? wrapped.Help : help; }
				set {
					if (wrapped != null)
						wrapped.Help = value;
					else
						help = value;
				}
			}
			public Image Image {
				get { return wrapped != null ? wrapped.Image : pixbuf; }
				set {
					if (wrapped != null)
						wrapped.Image = value;
					else
						pixbuf = value;
				}
			}

			public event EventHandler<StatusBarIconClickedEventArgs> Clicked;

			public void Dispose ()
			{
				if (wrapped != null) {
					wrapped.Clicked -= Wrapped_Clicked;
					wrapped.Dispose ();
				} else
					parent.icons.Remove (this);
			}

			public void SetAlertMode (int seconds)
			{
				if (wrapped != null)
					wrapped.SetAlertMode (seconds);
			}
		}
	}
}
