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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using System.Collections.Generic;
using MonoDevelop.Components.Extensions;
using Mono.Addins;

namespace MonoDevelop.Ide
{
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
			GenericAlert (MonoDevelop.Ide.Gui.Stock.Error, primaryText, secondaryText, AlertButton.Ok);
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
			GenericAlert (MonoDevelop.Ide.Gui.Stock.Warning, primaryText, secondaryText, AlertButton.Ok);
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
			GenericAlert (MonoDevelop.Ide.Gui.Stock.Information, primaryText, secondaryText, AlertButton.Ok);
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
			return messageService.GenericAlert (message) == message.ConfirmButton;
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
			return messageService.GenericAlert (message);
		}
		
		#endregion
		
		/// <summary>
		/// Places, runs and destroys a transient dialog.
		/// </summary>
		public static int ShowCustomDialog (Gtk.Dialog dialog)
		{
			return ShowCustomDialog (dialog, rootWindow);
		}
		
		public static int ShowCustomDialog (Gtk.Dialog dialog, Window parent)
		{
			try {
				return RunCustomDialog (dialog, parent);
			} finally {
				if (dialog != null)
					dialog.Destroy ();
			}
		}
		
		public static int RunCustomDialog (Gtk.Dialog dialog)
		{
			return RunCustomDialog (dialog, rootWindow);
		}
		
		/// <summary>
		/// Places and runs a transient dialog. Does not destroy it, so values can be retrieved from its widgets.
		/// </summary>
		public static int RunCustomDialog (Gtk.Dialog dialog, Window parent)
		{
			if (parent == null) {
				if (dialog.TransientFor != null)
					parent = dialog.TransientFor;
				else
					parent = GetDefaultParent (dialog);
			}
			dialog.TransientFor = parent;
			dialog.DestroyWithParent = true;
			PlaceDialog (dialog, parent);
			return dialog.Run ();
		}
		
		//make sure modal children are parented on top of other modal children
		static Window GetDefaultParent (Window child)
		{
			if (child.Modal) {
				return GetDefaultModalParent ();
			} else {
				return RootWindow;
			}
		}
		
		/// <summary>
		/// Gets a default parent for modal dialogs.
		/// </summary>
		public static Window GetDefaultModalParent ()
		{
			foreach (Gtk.Window w in Gtk.Window.ListToplevels ())
				if (w.Visible && w.HasToplevelFocus && w.Modal)
					return w;
			return RootWindow;
		}
		
		/// <summary>
		/// Positions a dialog relative to its parent on platforms where default placement is known to be poor.
		/// </summary>
		public static void PlaceDialog (Window child, Window parent)
		{
			//HACK: Mac GTK automatic window placement is broken
			if (PropertyService.IsMac)
				CenterWindow (child, parent ?? GetDefaultParent (child));
		}
		
		/// <summary>Centers a window relative to its parent.</summary>
		static void CenterWindow (Window child, Window parent)
		{
			child.Child.Show ();
			int w, h, winw, winh, x, y, winx, winy;
			child.GetSize (out w, out h);
			rootWindow.GetSize (out winw, out winh);
			rootWindow.GetPosition (out winx, out winy);
			x = Math.Max (0, (winw - w) /2) + winx;
			y = Math.Max (0, (winh - h) /2) + winy;
			child.Move (x, y);
		}
		
		public static AlertButton GenericAlert (string icon, string primaryText, string secondaryText, params AlertButton[] buttons)
		{
			return GenericAlert (icon, primaryText, secondaryText, buttons.Length - 1, buttons);
		}
		
		public static AlertButton GenericAlert (string icon, string primaryText, string secondaryText, int defaultButton, params AlertButton[] buttons)
		{
			GenericMessage message = new GenericMessage () {
				Icon = icon,
				Text = primaryText,
				SecondaryText = secondaryText,
				DefaultButton = defaultButton
			};
			foreach (AlertButton but in buttons)
				message.Buttons.Add (but);
			
			return messageService.GenericAlert (message);
		}
		
		public static AlertButton GenericAlert (GenericMessage message)
		{
			return messageService.GenericAlert (message);
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
				var exceptionDialog = new ExceptionDialog () {
					Message = primaryText,
					Exception = e,
					TransientFor = parent,
				};
				exceptionDialog.Run ();
			}
			
			public AlertButton GenericAlert (MessageDescription message)
			{
				var dialog = new AlertDialog (message);
				return dialog.Run ();
			}
			
			public string GetTextResponse (string question, string caption, string initialValue, bool isPassword)
			{
				var dialog = new TextQuestionDialog () {
					Question = question,
					Caption = caption,
					Value = initialValue,
					IsPassword = isPassword,
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
		internal MessageDescription ()
		{
			DefaultButton = -1;
			Buttons = new List<AlertButton> ();
			Options = new List<AlertOption> ();
		}
		
		internal IList<AlertButton> Buttons { get; private set; }
		internal IList<AlertOption> Options { get; private set; }
		
		internal AlertButton ApplyToAllButton { get; set; }
		
		public string Icon { get; set; }
		
		public string Text { get; set; }
		public string SecondaryText { get; set; }
		public bool AllowApplyToAll { get; set; }
		public int DefaultButton { get; set; }
		
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
		public GenericMessage ()
		{
		}
		
		public GenericMessage (string text)
		{
			Text = text;
		}
		
		public GenericMessage (string text, string secondaryText): this (text)
		{
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
