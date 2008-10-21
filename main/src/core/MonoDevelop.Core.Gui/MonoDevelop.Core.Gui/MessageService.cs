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
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Core.Gui
{
	public class AlertButton 
	{
		public static AlertButton Ok      = new AlertButton (Gtk.Stock.Ok, true);
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
		public static AlertButton Discard = new AlertButton (GettextCatalog.GetString ("D_iscard"));
		public static AlertButton Stop    = new AlertButton (Gtk.Stock.Stop, true);
		public static AlertButton Proceed = new AlertButton (GettextCatalog.GetString ("_Proceed"));
		
		public static AlertButton OverwriteFile = new AlertButton (GettextCatalog.GetString ("_Overwrite file"));
		
		string label;
		string icon;
		bool   isStockButton;
		
		public string Label {
			get {
				return label;
			}
			set {
				label = value;
			}
		}
		
		public string Icon {
			get {
				return icon;
			}
			set {
				icon = value;
			}
		}

		public bool IsStockButton {
			get {
				return isStockButton;
			}
			set {
				isStockButton = value;
			}
		}
		
		public AlertButton (string label, string icon)
		{
			this.label = label;
			this.icon = icon;
		}
		
		public AlertButton (string label) : this (label, null)
		{
		}
		public AlertButton (string label, bool isStockButton) : this (label)
		{
			this.isStockButton = isStockButton;
		}
	}
	
	//all methods are synchronously invoked on the GUI thread, except those which take GTK# objects as arguments
	public static class MessageService
	{
		static Gtk.Window rootWindow;
		
		public static Gtk.Window RootWindow {
			get {
				return rootWindow; 
			}
			
			set {
				rootWindow = value;
			}
		}
		
		#region ShowException
		public static void ShowException (Exception e, string primaryText)
		{
			ShowException (RootWindow, e, primaryText);
		}
		
		public static void ShowException (Exception e)
		{
			ShowException (RootWindow, e);
		}
		
		public static void ShowException (Gtk.Window parent, Exception e)
		{
			ShowException (RootWindow, e, e.Message);
		}
		
		public static void ShowException (Gtk.Window parent, Exception e, string primaryText)
		{
			messageService.ShowException (parent, e, primaryText);
		}
		#endregion
		
		#region ShowError
		public static void ShowError (string primaryText)
		{
			ShowError (RootWindow, primaryText);
		}
		public static void ShowError (Gtk.Window parent, string primaryText)
		{
			ShowError (parent, primaryText, null);
		}
		public static void ShowError (string primaryText, string secondaryText)
		{
			ShowError (RootWindow, primaryText, secondaryText);
		}
		public static void ShowError (Gtk.Window parent, string primaryText, string secondaryText)
		{
			GenericAlert (Stock.Error, primaryText, secondaryText, AlertButton.Cancel);
		}
		#endregion
		
		#region ShowWarning
		public static void ShowWarning (string primaryText)
		{
			ShowWarning (RootWindow, primaryText);
		}
		public static void ShowWarning (Gtk.Window parent, string primaryText)
		{
			ShowWarning (parent, primaryText, null);
		}
		public static void ShowWarning (string primaryText, string secondaryText)
		{
			ShowWarning (RootWindow, primaryText, secondaryText);
		}
		public static void ShowWarning (Gtk.Window parent, string primaryText, string secondaryText)
		{
			GenericAlert (Stock.Warning, primaryText, secondaryText, AlertButton.Ok);
		}
		#endregion
		
		#region ShowMessage
		public static void ShowMessage (string primaryText)
		{
			ShowMessage (RootWindow, primaryText);
		}
		public static void ShowMessage (Gtk.Window parent, string primaryText)
		{
			ShowMessage (parent, primaryText, null);
		}
		public static void ShowMessage (string primaryText, string secondaryText)
		{
			ShowMessage (RootWindow, primaryText, secondaryText);
		}
		public static void ShowMessage (Gtk.Window parent, string primaryText, string secondaryText)
		{
			GenericAlert (Stock.Information, primaryText, secondaryText, AlertButton.Cancel);
		}
		#endregion
		
		#region Confirm
		public static bool Confirm (string primaryText, AlertButton button)
		{
			return Confirm (primaryText, null, button);
		}
		
		public static bool Confirm (string primaryText, string secondaryText, AlertButton button)
		{
			return GenericAlert (Stock.Question, primaryText, secondaryText, AlertButton.Cancel, button) == button;
		}
		public static bool Confirm (string primaryText, AlertButton button, bool confirmIsDefault)
		{
			return Confirm (primaryText, null, button, confirmIsDefault);
		}
		
		public static bool Confirm (string primaryText, string secondaryText, AlertButton button, bool confirmIsDefault)
		{
			return GenericAlert (Stock.Question, primaryText, secondaryText, confirmIsDefault ? 0 : 1, AlertButton.Cancel, button) == button;
		}
		#endregion
		
		#region AskQuestion
		public static AlertButton AskQuestion (string primaryText, params AlertButton[] buttons)
		{
			return AskQuestion (primaryText, null, buttons);
		}
		
		public static AlertButton AskQuestion (string primaryText, string secondaryText, params AlertButton[] buttons)
		{
			return GenericAlert (Stock.Question, primaryText, secondaryText, buttons);
		}
		public static AlertButton AskQuestion (string primaryText, int defaultButton, params AlertButton[] buttons)
		{
			return AskQuestion (primaryText, null, defaultButton, buttons);
		}
		
		public static AlertButton AskQuestion (string primaryText, string secondaryText, int defaultButton, params AlertButton[] buttons)
		{
			return GenericAlert (Stock.Question, primaryText, secondaryText, defaultButton, buttons);
		}
		#endregion
		
		public static int ShowCustomDialog (Gtk.Dialog dialog)
		{
			MonoDevelop.Core.Gui.DispatchService.AssertGuiThread ();
			try {
				dialog.Modal             = true;
				dialog.TransientFor      = rootWindow;
				dialog.DestroyWithParent = true;
				return dialog.Run ();
			} finally {
				if (dialog != null)
					dialog.Destroy ();
			}
		}
		
		public static AlertButton GenericAlert (string icon, string primaryText, string secondaryText, params AlertButton[] buttons)
		{
			return GenericAlert (icon, primaryText, secondaryText, buttons.Length - 1, buttons);
		}
		public static AlertButton GenericAlert (string icon, string primaryText, string secondaryText, int defaultButton, params AlertButton[] buttons)
		{
			return messageService.GenericAlert (icon, primaryText, secondaryText, defaultButton, buttons);
		}
		
		public static string GetTextResponse (string question, string caption, string initialValue)
		{
			return GetTextResponse (question, caption, initialValue, false);
		}
		public static string GetPassword (string question, string caption)
		{
			return GetTextResponse(question, caption, string.Empty, true);
		}
		static string GetTextResponse (string question, string caption, string initialValue, bool isPassword)
		{
			return messageService.GetTextResponse (question, caption, initialValue, isPassword);
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
		private class InternalMessageService : GuiSyncObject
		{
			public void ShowException (Gtk.Window parent, Exception e, string primaryText)
			{
				MonoDevelop.Core.Gui.Dialogs.ErrorDialog errorDialog = new MonoDevelop.Core.Gui.Dialogs.ErrorDialog (parent);
				try {
					errorDialog.Message = primaryText;
					errorDialog.AddDetails (e.ToString (), false);
					errorDialog.Run ();
				} finally {
					if (errorDialog != null)
						errorDialog.Dispose ();
				}
			}
			
			public AlertButton GenericAlert (string icon, string primaryText, string secondaryText, int defaultButton, params AlertButton[] buttons)
			{
				if (string.IsNullOrEmpty (secondaryText)) {
					secondaryText = primaryText;
					primaryText = null;
				}
				AlertDialog alertDialog = new AlertDialog (icon, primaryText, secondaryText, buttons);
				alertDialog.FocusButton (defaultButton);
				ShowCustomDialog (alertDialog);
				return alertDialog.ResultButton;
			}
			
			public string GetTextResponse (string question, string caption, string initialValue, bool isPassword)
			{
				string returnValue = null;
				
				Dialog md = new Dialog (caption, rootWindow, DialogFlags.Modal | DialogFlags.DestroyWithParent);
				try {
					// add a label with the question
					Label questionLabel = new Label(question);
					questionLabel.UseMarkup = true;
					questionLabel.Xalign = 0.0F;
					md.VBox.PackStart(questionLabel, true, false, 6);
					
					// add an entry with initialValue
					Entry responseEntry = (initialValue != null) ? new Entry(initialValue) : new Entry();
					md.VBox.PackStart(responseEntry, false, true, 6);
					responseEntry.Visibility = !isPassword;
					
					// add action widgets
					md.AddActionWidget(new Button(Gtk.Stock.Cancel), ResponseType.Cancel);
					md.AddActionWidget(new Button(Gtk.Stock.Ok), ResponseType.Ok);
					
					md.VBox.ShowAll();
					md.ActionArea.ShowAll();
					md.HasSeparator = false;
					md.BorderWidth = 6;
					
					int response = md.Run ();
					md.Hide ();
					
					if ((ResponseType) response == ResponseType.Ok) {
						returnValue =  responseEntry.Text;
					}
					
					return returnValue;
				} finally {
					md.Destroy ();
				}
			}
		}
		#endregion
	}
}
