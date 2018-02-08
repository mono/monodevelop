//
// MessageService.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Ide.Gui;
using System.Threading.Tasks;

#if MAC
using AppKit;
using MonoDevelop.Components.Mac;
#endif

namespace MonoDevelop.Ide
{
	public class AlertButtonEventArgs : EventArgs
	{
		public AlertButton Button {
			get;
			private set;
		}

		public bool CloseDialog {
			get;
			set;
		}

		public AlertButtonEventArgs (AlertButton button, bool closeDialog)
		{
			Button = button;
			CloseDialog = closeDialog;
		}

		public AlertButtonEventArgs (AlertButton button) : this (button, true)
		{
		}
	}

	public class AlertButton 
	{
		public static AlertButton Ok      = new AlertButton (Gtk.Stock.Ok, true);
		public static AlertButton Yes     = new AlertButton (Gtk.Stock.Yes, true);
		public static AlertButton No      = new AlertButton (Gtk.Stock.No, true);
		public static AlertButton Close   = new AlertButton (Gtk.Stock.Close, true);
		public static AlertButton Cancel  = new AlertButton (Gtk.Stock.Cancel, true);
		public static AlertButton Delete  = new AlertButton (Gtk.Stock.Delete, true);
		public static AlertButton Remove  = new AlertButton (Gtk.Stock.Remove, true);
		public static AlertButton Clear   = new AlertButton (Gtk.Stock.Clear, true);
		public static AlertButton Reload  = new AlertButton (GettextCatalog.GetString ("_Reload"), Gtk.Stock.Refresh);
		public static AlertButton Revert  = new AlertButton (Gtk.Stock.RevertToSaved, true );
		public static AlertButton Copy    = new AlertButton (Gtk.Stock.Copy, true);
		public static AlertButton Move    = new AlertButton (GettextCatalog.GetString ("_Move"));
		public static AlertButton Save    = new AlertButton (Gtk.Stock.Save, true);
		public static AlertButton SaveAs  = new AlertButton (Gtk.Stock.SaveAs, true);
		public static AlertButton CloseWithoutSave = new AlertButton (GettextCatalog.GetString ("Close _without Saving"));
		public static AlertButton BuildWithoutSave = new AlertButton (GettextCatalog.GetString ("Build _without Saving"));
		public static AlertButton Discard = new AlertButton (GettextCatalog.GetString ("D_iscard"));
		public static AlertButton Stop    = new AlertButton (Gtk.Stock.Stop, true);
		public static AlertButton Proceed = new AlertButton (GettextCatalog.GetString ("_Proceed"));
		public static AlertButton Replace = new AlertButton (GettextCatalog.GetString ("_Replace"));

		public static AlertButton OverwriteFile = new AlertButton (GettextCatalog.GetString ("_Overwrite file"));
		public static AlertButton AddExistingFile = new AlertButton (GettextCatalog.GetString ("Add existing file"));
		
		
		public string Label { get; set; }
		public string Icon { get; set; }
		public bool IsStockButton { get; set; }
		
		public AlertButton (string label, string icon)
		{
			this.Label = label;
			this.Icon = icon;
		}
		
		public AlertButton (string label) : this (label, null)
		{
		}
		
		public AlertButton (string label, bool isStockButton) : this (label)
		{
			this.IsStockButton = isStockButton;
		}
	}
	
	public class AlertOption
	{
		internal AlertOption (string id, string text)
		{
			this.Id = id;
			this.Text = text;
		}

		public string Id { get; private set; }
		public string Text { get; private set; }
		public bool Value { get; set; }
	}
	
	//all methods are synchronously invoked on the GUI thread, except those which take GTK# objects as arguments
	public static class MessageService
	{
		public static Window RootWindow { get; internal set; }

		#region ShowError
		public static void ShowError (string primaryText)
		{
			ShowError ((Window)null, primaryText);
		}

		public static void ShowError (string primaryText, Exception ex)
		{
			ShowError ((Window)null, primaryText, null, ex);
		}

		public static void ShowError (string primaryText, string secondaryText)
		{
			ShowError ((Window)null, primaryText, secondaryText, null);
		}

		public static void ShowError (string primaryText, string secondaryText, Exception ex)
		{
			ShowError ((Window)null, primaryText, secondaryText, ex);
		}

		public static void ShowError (Window parent, string primaryText)
		{
			ShowError (parent, primaryText, null, null);
		}

		public static void ShowError (Window parent, string primaryText, string secondaryText)
		{
			ShowError (parent, primaryText, secondaryText, null);
		}

		public static void ShowError (Window parent, string primaryText, string secondaryText, Exception ex)
		{
			ShowError (parent, primaryText, secondaryText, ex, true, AlertButton.Ok);
		}

		internal static AlertButton ShowError (Window parent, string primaryText, string secondaryText, Exception ex, bool logError, params AlertButton[] buttons)
		{
			if (logError) {
				string msg = string.IsNullOrEmpty (secondaryText) ? primaryText : primaryText + ". " + secondaryText;
				LoggingService.LogError (msg, ex);
			}

			if (string.IsNullOrEmpty (secondaryText) && (ex != null))
				secondaryText = ErrorHelper.GetErrorMessage (ex);

			return GenericAlert (parent, MonoDevelop.Ide.Gui.Stock.Error, primaryText, secondaryText, buttons);
		}

		internal static void ShowFatalError (string primaryText, string secondaryText, Exception ex)
		{
			string msg = string.IsNullOrEmpty (secondaryText) ? primaryText : primaryText + ". " + secondaryText;
			LoggingService.LogFatalError (msg, ex);
			GenericAlert (null, MonoDevelop.Ide.Gui.Stock.Error, primaryText, secondaryText, AlertButton.Ok);
		}
		#endregion
		
		#region ShowWarning
		public static void ShowWarning (string primaryText)
		{
			ShowWarning ((Window)null, primaryText);
		}
		public static void ShowWarning (Window parent, string primaryText)
		{
			ShowWarning (parent, primaryText, null);
		}
		public static void ShowWarning (string primaryText, string secondaryText)
		{
			ShowWarning ((Window)null, primaryText, secondaryText);
		}
		public static void ShowWarning (Window parent, string primaryText, string secondaryText)
		{
			GenericAlert (parent, MonoDevelop.Ide.Gui.Stock.Warning, primaryText, secondaryText, AlertButton.Ok);
		}
		#endregion
		
		#region ShowMessage
		public static void ShowMessage (string primaryText)
		{
			ShowMessage ((Window)null, primaryText);
		}
		public static void ShowMessage (Window parent, string primaryText)
		{
			ShowMessage (parent, primaryText, null);
		}
		public static void ShowMessage (string primaryText, string secondaryText)
		{
			ShowMessage ((Window)null, primaryText, secondaryText);
		}
		public static void ShowMessage (Window parent, string primaryText, string secondaryText)
		{
			GenericAlert (parent, MonoDevelop.Ide.Gui.Stock.Information, primaryText, secondaryText, AlertButton.Ok);
		}
		#endregion
		
		#region Confirm
		public static bool Confirm (string primaryText, AlertButton button)
		{
			return Confirm (primaryText, null, button);
		}
		
		public static bool Confirm (string primaryText, string secondaryText, AlertButton button)
		{
			return GenericAlert (MonoDevelop.Ide.Gui.Stock.Question, primaryText, secondaryText, AlertButton.Cancel, button) == button;
		}
		public static bool Confirm (string primaryText, AlertButton button, bool confirmIsDefault)
		{
			return Confirm (primaryText, null, button, confirmIsDefault);
		}
		
		public static bool Confirm (string primaryText, string secondaryText, AlertButton button, bool confirmIsDefault)
		{
			return GenericAlert (MonoDevelop.Ide.Gui.Stock.Question, primaryText, secondaryText, confirmIsDefault ? 0 : 1, AlertButton.Cancel, button) == button;
		}
		
		public static bool Confirm (ConfirmationMessage message)
		{
			return messageService.GenericAlert (null, message) == message.ConfirmButton;
		}
		#endregion
		
		#region AskQuestion
		public static AlertButton AskQuestion (string primaryText, params AlertButton[] buttons)
		{
			return AskQuestion (primaryText, null, buttons);
		}
		
		public static AlertButton AskQuestion (string primaryText, string secondaryText, params AlertButton[] buttons)
		{
			return GenericAlert (MonoDevelop.Ide.Gui.Stock.Question, primaryText, secondaryText, buttons);
		}
		public static AlertButton AskQuestion (string primaryText, int defaultButton, params AlertButton[] buttons)
		{
			return AskQuestion (primaryText, null, defaultButton, buttons);
		}
		
		public static AlertButton AskQuestion (string primaryText, string secondaryText, int defaultButton, params AlertButton[] buttons)
		{
			return GenericAlert (MonoDevelop.Ide.Gui.Stock.Question, primaryText, secondaryText, defaultButton, buttons);
		}
		
		public static AlertButton AskQuestion (QuestionMessage message)
		{
			return messageService.GenericAlert (null, message);
		}
		
		#endregion
		
		/// <summary>
		/// Places, runs and destroys a transient dialog.
		/// </summary>
		public static int ShowCustomDialog (Dialog dialog)
		{
			return ShowCustomDialog (dialog, null);
		}
		
		public static int ShowCustomDialog (Dialog dlg, Window parent)
		{
			Gtk.Dialog dialog = dlg;
			try {
				return RunCustomDialog (dlg, parent);
			} finally {
				dialog?.Destroy ();
			}
		}
		
		public static int RunCustomDialog (Dialog dialog)
		{
			return RunCustomDialog (dialog, null);
		}
		
		/// <summary>
		/// Places and runs a transient dialog. Does not destroy it, so values can be retrieved from its widgets.
		/// </summary>
		public static int RunCustomDialog (Dialog dlg, Window parent)
		{
			// if dialog is modal, make sure it's parented on any existing modal dialog
			Gtk.Dialog dialog = dlg;
			if (dialog.Modal) {
				parent = GetDefaultModalParent ();
			}

			//ensure the dialog has a parent
			if (parent == null) {
				parent = dialog.TransientFor ?? RootWindow;
			}

			dialog.TransientFor = parent;
			dialog.DestroyWithParent = true;
			MonoDevelop.Components.IdeTheme.ApplyTheme (dialog);

			if (dialog.Title == null)
				dialog.Title = BrandingService.ApplicationName;

			#if MAC
			Runtime.RunInMainThread (() => {
				// If there is a native NSWindow model window running, we need
				// to show the new dialog over that window.
				if (NSApplication.SharedApplication.ModalWindow != null)
					dialog.Shown += HandleShown;
				else
					PlaceDialog (dialog, parent);
			}).Wait ();
			#endif

			try {
				IdeApp.DisableIdleActions ();
				return GtkWorkarounds.RunDialogWithNotification (dialog);
			} finally {
				IdeApp.EnableIdleActions ();
			}
		}

		#if MAC
		static void HandleShown (object sender, EventArgs e)
		{
			var dialog = (Gtk.Window)sender;
			var nsdialog = GtkMacInterop.GetNSWindow (dialog);

			// Make the GTK window modal WRT the current modal NSWindow
			var s = NSApplication.SharedApplication.BeginModalSession (nsdialog);

			EventHandler unrealizer = null;
			unrealizer = delegate {
				NSApplication.SharedApplication.EndModalSession (s);
				dialog.Unrealized -= unrealizer;
			};
			dialog.Unrealized += unrealizer;
			dialog.Shown -= HandleShown;
		}
		#endif

		/// <summary>
		/// Gets a default parent for modal dialogs.
		/// </summary>
		public static Window GetDefaultModalParent ()
		{
			foreach (Gtk.Window w in Gtk.Window.ListToplevels ())
				if (w.Visible && w.HasToplevelFocus && w.Modal)
					return w;
			return GetFocusedToplevel ();
		}

		static Gtk.Window GetFocusedToplevel ()
		{
			// use the first "normal" toplevel window (skipping docks, popups, etc.) or the main IDE window
			return Gtk.Window.ListToplevels ().FirstOrDefault (w => w.HasToplevelFocus &&
			                                                   (w.TypeHint == Gdk.WindowTypeHint.Dialog ||
			                                                    w.TypeHint == Gdk.WindowTypeHint.Normal ||
			                                                    w.TypeHint == Gdk.WindowTypeHint.Utility)) ?? RootWindow;
		}
		
		/// <summary>
		/// Positions a dialog relative to its parent on platforms where default placement is known to be poor.
		/// </summary>
		public static void PlaceDialog (Window childControl, Window parent)
		{
			//HACK: this is a workaround for broken automatic window placement on Mac
			if (!Platform.IsMac)
				return;

			Gtk.Window child = childControl;
			//modal windows should always be placed o top of existing modal windows
			if (child.Modal)
				parent = GetDefaultModalParent ();

			//else center on the focused toplevel
			if (parent == null)
				parent = GetFocusedToplevel ();

			if (parent != null)
				CenterWindow (child, parent);
		}
		
		/// <summary>Centers a window relative to its parent.</summary>
		static void CenterWindow (Window childControl, Window parentControl)
		{
			Gtk.Window child = childControl;
			Gtk.Window parent = parentControl;
			child.Child.Show ();
			int w, h, winw, winh, x, y, winx, winy;
			child.GetSize (out w, out h);
			parent.GetSize (out winw, out winh);
			parent.GetPosition (out winx, out winy);
			x = Math.Max (0, (winw - w) /2) + winx;
			y = Math.Max (0, (winh - h) /2) + winy;
			child.Move (x, y);
		}
		
		public static AlertButton GenericAlert (string icon, string primaryText, string secondaryText, params AlertButton[] buttons)
		{
			return GenericAlert ((Window)null, icon, primaryText, secondaryText, buttons.Length - 1, buttons);
		}

		public static AlertButton GenericAlert (Window parent, string icon, string primaryText, string secondaryText, params AlertButton[] buttons)
		{
			return GenericAlert (parent, icon, primaryText, secondaryText, buttons.Length - 1, buttons);
		}
		
		public static AlertButton GenericAlert (string icon, string primaryText, string secondaryText, int defaultButton,
			params AlertButton[] buttons)
		{
			return GenericAlert ((Window)null, icon, primaryText, secondaryText, defaultButton, CancellationToken.None, buttons);
		}

		public static AlertButton GenericAlert (Window parent, string icon, string primaryText, string secondaryText, int defaultButton,
			params AlertButton[] buttons)
		{
			return GenericAlert (parent, icon, primaryText, secondaryText, defaultButton, CancellationToken.None, buttons);
		}

		public static AlertButton GenericAlert (string icon, string primaryText, string secondaryText, int defaultButton,
			CancellationToken cancellationToken,
			params AlertButton[] buttons)
		{
			return GenericAlert ((Window)null, icon, primaryText, secondaryText, defaultButton, cancellationToken, buttons);
		}
		
		public static AlertButton GenericAlert (Window parent, string icon, string primaryText, string secondaryText, int defaultButton,
			CancellationToken cancellationToken,
			params AlertButton[] buttons)
		{
			var message = new GenericMessage (primaryText, secondaryText, cancellationToken) {
				Icon = icon,
				DefaultButton = defaultButton,
			};
			foreach (AlertButton but in buttons)
				message.Buttons.Add (but);
			
			return messageService.GenericAlert (parent, message);
		}

		public static AlertButton GenericAlert (GenericMessage message)
		{
			return GenericAlert ((Window)null, message);
		}
		
		public static AlertButton GenericAlert (Window parent, GenericMessage message)
		{
			return messageService.GenericAlert (parent, message);
		}

		public static async Task<T> ExecuteTaskAndShowWaitDialog<T> (Task<T> task, string waitMessage, CancellationTokenSource cts)
		{
			bool taskFinished = false;
			var dontExitMethodUntilDialogClosed = new TaskCompletionSource<bool> ();
			var delayTask = Task.Delay (1000);//Don't show wait dialog immediately, wait 1 sec before showing
			var finishedTask = await Task.WhenAny (delayTask, task).ConfigureAwait (false);
			if (finishedTask == task)//If task finished before delayTask, great return value and never display dialog
				return task.Result;
			//cancelDialog is used to close dialog when task is finished
			var cancelDialog = new CancellationTokenSource ();
			Gtk.Application.Invoke ((o, args) => {
				if (cancelDialog.Token.IsCancellationRequested)
					return;
				var gm = new GenericMessage (waitMessage, null, cancelDialog.Token);
				gm.Buttons.Add (AlertButton.Cancel);
				gm.DefaultButton = 0;
				GenericAlert (gm);
				dontExitMethodUntilDialogClosed.SetResult (true);
				if (!taskFinished) {
					//Don't cancel if task finished already, we closed dialog via cancelDialog.Cancel ();
					//caller of this method might reuse this cts for other tasks
					cts.Cancel ();
				}
			});
			try {
				await task.ConfigureAwait (false);
			} finally {
				taskFinished = true;
				cancelDialog.Cancel ();
			}
			await dontExitMethodUntilDialogClosed.Task.ConfigureAwait (false);
			return task.Result;
		}
		
		public static string GetTextResponse (string question, string caption, string initialValue)
		{
			return GetTextResponse ((Window)null, question, caption, initialValue, false);
		}

		public static string GetTextResponse (Window parent, string question, string caption, string initialValue)
		{
			return GetTextResponse (parent, question, caption, initialValue, false);
		}

		public static string GetPassword (string question, string caption)
		{
			return GetTextResponse ((Window)null, question, caption, string.Empty, true);
		}

		public static string GetPassword (Window parent, string question, string caption)
		{
			return GetTextResponse (parent, question, caption, string.Empty, true);
		}

		static string GetTextResponse (Window parent, string question, string caption, string initialValue, bool isPassword)
		{
			return messageService.GetTextResponse (parent, question, caption, initialValue, isPassword);
		}
		
		#region Internal GUI object
		static InternalMessageService mso;
		static InternalMessageService messageService
		{
			get {
				if (mso == null)
					mso = new InternalMessageService ();
				return mso;
			}
		}
		
		//The real GTK# code is wrapped in a GuiSyncObject to make calls synchronous on the GUI thread
		class InternalMessageService : GuiSyncObject
		{
			public AlertButton GenericAlert (Window parent, MessageDescription message)
			{
				var dialog = new AlertDialog (message) {
					TransientFor = parent ?? GetDefaultModalParent ()
				};
				return dialog.Run ();
			}
			
			public string GetTextResponse (Window parent, string question, string caption, string initialValue, bool isPassword)
			{
				var dialog = new TextQuestionDialog {
					Question = question,
					Caption = caption,
					Value = initialValue,
					IsPassword = isPassword,
					TransientFor = parent ?? GetDefaultModalParent ()
				};
				if (dialog.Run ())
					return dialog.Value;
				return null;
			}
		}
		#endregion
	}
	
	public class MessageDescription
	{
		internal MessageDescription () : this (CancellationToken.None)
		{
		}
		
		internal MessageDescription (CancellationToken cancellationToken)
		{
			DefaultButton = -1;
			Buttons = new List<AlertButton> ();
			Options = new List<AlertOption> ();
			CancellationToken = cancellationToken;
		}
		
		internal IList<AlertButton> Buttons { get; private set; }
		internal IList<AlertOption> Options { get; private set; }
		
		internal AlertButton ApplyToAllButton { get; set; }
		
		public string Icon { get; set; }
		
		public string Text { get; set; }
		public string SecondaryText { get; set; }
		public bool AllowApplyToAll { get; set; }
		public int DefaultButton { get; set; }
		public CancellationToken CancellationToken { get; private set; }
		public bool UseMarkup { get; set; }

		public event EventHandler<AlertButtonEventArgs> AlertButtonClicked;

		internal bool NotifyClicked (AlertButton button)
		{
			var args = new AlertButtonEventArgs (button);
			if (AlertButtonClicked != null)
				AlertButtonClicked (this, args);
			return args.CloseDialog;
		}

		public void AddOption (string id, string text, bool setByDefault)
		{
			Options.Add (new AlertOption (id, text) { Value = setByDefault });
		}
		
		public bool GetOptionValue (string id)
		{
			foreach (var op in Options)
				if (op.Id == id)
					return op.Value;
			throw new ArgumentException ("Invalid option id");
		}
		
		public void SetOptionValue (string id, bool value)
		{
			foreach (var op in Options) {
				if (op.Id == id) {
					op.Value = value;
					return;
				}
			}
			throw new ArgumentException ("Invalid option id");
		}
	}
	
	public sealed class GenericMessage: MessageDescription
	{
		public GenericMessage () : base (CancellationToken.None)
		{
		}
		
		public GenericMessage (string text) : this () 
		{
			Text = text;
		}
		
		public GenericMessage (string text, string secondaryText) : this (text)
		{
			SecondaryText = secondaryText;
		}

		public GenericMessage (string text, string secondaryText, CancellationToken cancellationToken)
			: base (cancellationToken)
		{
			Text = text;
			SecondaryText = secondaryText;
		}
		
		public new IList<AlertButton> Buttons {
			get { return base.Buttons; }
		}
	}
	
	
	public sealed class QuestionMessage: MessageDescription
	{
		public QuestionMessage ()
		{
			Icon = MonoDevelop.Ide.Gui.Stock.Question;
		}
		
		public QuestionMessage (string text): this ()
		{
			Text = text;
		}
		
		public QuestionMessage (string text, string secondaryText): this (text)
		{
			SecondaryText = secondaryText;
		}
		
		public new IList<AlertButton> Buttons {
			get { return base.Buttons; }
		}
	}
	
	public sealed class ConfirmationMessage: MessageDescription
	{
		AlertButton confirmButton;
		
		public ConfirmationMessage ()
		{
			Icon = MonoDevelop.Ide.Gui.Stock.Question;
			Buttons.Add (AlertButton.Cancel);
		}
		
		public ConfirmationMessage (AlertButton button): this ()
		{
			ConfirmButton = button;
		}
		
		public ConfirmationMessage (string primaryText, AlertButton button): this (button)
		{
			Text = primaryText;
		}
		
		public ConfirmationMessage (string primaryText, string secondaryText, AlertButton button): this (primaryText, button)
		{
			SecondaryText = secondaryText;
		}
		
		public AlertButton ConfirmButton {
			get { return confirmButton; }
			set {
				if (Buttons.Count == 2)
					Buttons.RemoveAt (1);
				Buttons.Add (value);
				confirmButton = value;
			}
		}
		
		public bool ConfirmIsDefault {
			get {
				return DefaultButton == 1;
			}
			set {
				if (value)
					DefaultButton = 1;
				else
					DefaultButton = 0;
			}
		}
	}
}
