//
// StatusContext.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

using StockIcons = MonoDevelop.Ide.Gui.Stock;

namespace MonoDevelop.Ide.Status
{
	public class StatusMessageContext : IDisposable
	{
		internal StatusMessageContext ()
		{
		}

		public void Dispose ()
		{
			StatusService.Remove (this);
		}

		public event EventHandler<StatusMessageContextMessageChangedArgs> MessageChanged;
		public event EventHandler<StatusMessageContextProgressChangedArgs> ProgressChanged;

		public Pad StatusSourcePad { get; set; }

		public string Message { get; private set; }
		public bool IsMarkup { get; private set; }
		public IconId Image { get; private set; }

		public CancellationTokenSource CancellationTokenSource { get; set; }

		/// <summary>
		/// Clears the current context
		/// </summary>
		public void ShowReady ()
		{
			ShowMessage (null, "", false);
		}

		/// <summary>
		/// Shows a message with an error icon
		/// </summary>
		public void ShowError (string error)
		{
			ShowMessage (StockIcons.StatusError, error);
		}

		/// <summary>
		/// Shows a message with a warning icon
		/// </summary>
		public void ShowWarning (string warning)
		{
			ShowMessage (StockIcons.Warning, warning);
		}

		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		public void ShowMessage (string message)
		{
			ShowMessage (Image, message, IsMarkup);
		}

		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		public void ShowMessage (string message, bool isMarkup)
		{
			ShowMessage (Image, message, isMarkup);
		}

		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		public void ShowMessage (IconId image, string message)
		{
			ShowMessage (image, message, IsMarkup);
		}

		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		public void ShowMessage (IconId image, string message, bool isMarkup)
		{
			Message = message;
			Image = image;
			IsMarkup = isMarkup;

			OnMessageChanged ();
		}

		void OnMessageChanged ()
		{
			if (MessageChanged != null) {
				StatusMessageContextMessageChangedArgs args = new StatusMessageContextMessageChangedArgs (this, Message, IsMarkup, Image);
				MessageChanged (this, args);
			}
		}

		/// <summary>
		/// Shows a progress bar, with the provided label next to it
		/// </summary>
		public void BeginProgress (string name)
		{
			BeginProgress (IconId.Null, name);
		}

		/// <summary>
		/// Shows a progress bar, with the provided label and icon next to it
		/// </summary>
		public void BeginProgress (IconId image, string name)
		{
			ShowMessage (image, name);

			var args = new StatusMessageContextProgressChangedArgs (this, StatusMessageContextProgressChangedArgs.ProgressChangedType.Begin, 0.0);
			OnProgressChanged (args);
		}

		/// <summary>
		/// Sets the progress fraction. It can only be used after calling BeginProgress.
		/// </summary>
		public void SetProgressFraction (double work)
		{
			// Do nothing if autopulse...
			if (!AutoPulse)
				return;

			var args = new StatusMessageContextProgressChangedArgs (this, StatusMessageContextProgressChangedArgs.ProgressChangedType.Fraction, work);
			OnProgressChanged (args);
		}

		/// <summary>
		/// Hides the progress bar shown with BeginProgress
		/// </summary>
		public void EndProgress ()
		{
			var args = new StatusMessageContextProgressChangedArgs (this, StatusMessageContextProgressChangedArgs.ProgressChangedType.Finish, 0.0);
			OnProgressChanged (args);
		}

		/// <summary>
		/// Pulses the progress bar shown with BeginProgress
		/// </summary>
		public void Pulse ()
		{
			var args = new StatusMessageContextProgressChangedArgs (this, StatusMessageContextProgressChangedArgs.ProgressChangedType.Pulse, 0.0);
			OnProgressChanged (args);
		}

		/// <summary>
		/// When set, the status bar progress will be automatically pulsed at short intervals
		/// </summary>
		public bool AutoPulse { get; set; }

		void OnProgressChanged (StatusMessageContextProgressChangedArgs args)
		{
			if (ProgressChanged != null) {
				ProgressChanged (this, args);
			}
		}
	}

	public class StatusMessageContextMessageChangedArgs : EventArgs
	{
		public StatusMessageContext Context { get; private set; }
		public string Message { get; private set; }
		public IconId Image { get; private set; }
		public bool IsMarkup { get; private set; }

		public StatusMessageContextMessageChangedArgs (StatusMessageContext context, string message, bool isMarkup, IconId image)
		{
			Context = context;
			Message = message;
			Image = image;
			IsMarkup = isMarkup;
		}
	}

	public class StatusMessageContextProgressChangedArgs : EventArgs
	{
		public enum ProgressChangedType {
			Begin,
			Finish,
			Fraction,
			Pulse
		};

		public StatusMessageContext Context { get; private set; }
		public ProgressChangedType EventType { get; private set; }
		public double Work { get; private set; }

		public StatusMessageContextProgressChangedArgs (StatusMessageContext context, ProgressChangedType type, double work)
		{
			Context = context;
			EventType = type;
			Work = work;
		}
	}
}
