// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Core.Gui
{
	/// <summary>
	/// This interface must be implemented by all services.
	/// </summary>
	public class MessageService : GuiSyncAbstractService, IMessageService
	{
		StringParserService stringParserService = Runtime.StringParserService;
		Window rootWindow;
		
		public object RootWindow {
			get { return rootWindow; }
			set { rootWindow = (Window) value; }
		}
		
		public void ShowError(Exception ex)
		{
			ShowError(ex, null, rootWindow);
		}
		
		public void ShowError(string message)
		{
			ShowError(null, message, rootWindow);
		}

		public void ShowError (Window parent, string message)
		{
			ShowError (null, message, parent);
		}
		
		public void ShowErrorFormatted(string formatstring, params string[] formatitems)
		{
			ShowError(null, String.Format(stringParserService.Parse(formatstring), formatitems), rootWindow);
		}

		private struct ErrorContainer
		{
			public Exception ex;
			public string message;

			public ErrorContainer (Exception e, string msg)
			{
				ex = e;
				message = msg;
			}
		}

		public void ShowError (Exception ex, string message)
		{
			ShowError (ex, message, rootWindow);
		}

		public void ShowError (Exception ex, string message, Window parent)
		{
			ShowError (ex, message, parent, false);
		}
		
		public void ShowError (Exception ex, string message, Window parent, bool modal)
		{
			ErrorDialog dlg = new ErrorDialog (parent);
			
			if (message == null) {
				if (ex != null)
					dlg.Message = GettextCatalog.GetString ("Exception occurred: {0}", ex.Message);
				else {
					dlg.Message = "An unknown error occurred";
					dlg.AddDetails (Environment.StackTrace, false);
				}
			} else
				dlg.Message = message;
			
			if (ex != null) {
				UserException uex = ex as UserException;
				if (uex != null) {
					if (uex.Details != null)
						dlg.AddDetails (uex.Details, true);
				} else {
					dlg.AddDetails (GettextCatalog.GetString ("Exception occurred: {0}", ex.Message) + "\n\n", true);
					dlg.AddDetails (ex.ToString (), false);
				}
			}

			if (modal) {
				dlg.Run ();
				dlg.Dispose ();
			} else
				dlg.Show ();
		}

		public void ShowWarning(string message)
		{
			MessageDialog md = new MessageDialog (rootWindow, DialogFlags.Modal | DialogFlags.DestroyWithParent, MessageType.Warning, ButtonsType.Ok, EscapeBraces(message));
			md.Response += new ResponseHandler(OnResponse);
			md.Close += new EventHandler(OnClose);
			md.ShowAll ();
		}		
		
		public void ShowWarningFormatted(string formatstring, params string[] formatitems)
		{
			ShowWarning(String.Format(stringParserService.Parse(formatstring), formatitems));
		}
		
		public bool AskQuestion(string question, string caption)
		{
			using (MessageDialog md = new MessageDialog (rootWindow, DialogFlags.Modal | DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, EscapeBraces(question))) {
				int response = md.Run ();
				md.Hide ();
				
				if ((ResponseType) response == ResponseType.Yes)
					return true;
				else
					return false;
			}
		}
		
		public bool AskQuestionFormatted(string caption, string formatstring, params string[] formatitems)
		{
			return AskQuestion(String.Format(stringParserService.Parse(formatstring), formatitems), caption);
		}
		
		public bool AskQuestionFormatted(string formatstring, params string[] formatitems)
		{
			return AskQuestion(String.Format(stringParserService.Parse(formatstring), formatitems));
		}
		
		public bool AskQuestion(string question)
		{
			return AskQuestion(stringParserService.Parse(question), GettextCatalog.GetString ("Question"));
		}

		public QuestionResponse AskQuestionWithCancel(string question, string caption)
		{
			using (MessageDialog md = new MessageDialog (rootWindow, DialogFlags.Modal | DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.None, EscapeBraces(question))) {
				
				md.AddActionWidget (new Button (Gtk.Stock.No), ResponseType.No);
				md.AddActionWidget (new Button (Gtk.Stock.Cancel), ResponseType.Cancel);
				md.AddActionWidget (new Button (Gtk.Stock.Yes), ResponseType.Yes);
				md.ActionArea.ShowAll ();
				
				ResponseType response = (ResponseType)md.Run ();
				md.Hide ();

				if (response == ResponseType.Yes) {
					return QuestionResponse.Yes;
				}

				if (response == ResponseType.No) {
					return QuestionResponse.No;
				}

				if (response == ResponseType.Cancel) {
					return QuestionResponse.Cancel;
				}

				return QuestionResponse.Cancel;
			}
		}
		
		public QuestionResponse AskQuestionFormattedWithCancel(string caption, string formatstring, params string[] formatitems)
		{
			return AskQuestionWithCancel(String.Format(stringParserService.Parse(formatstring), formatitems), caption);
		}
		
		public QuestionResponse AskQuestionFormattedWithCancel(string formatstring, params string[] formatitems)
		{
			return AskQuestionWithCancel(String.Format(stringParserService.Parse(formatstring), formatitems));
		}
		
		public QuestionResponse AskQuestionWithCancel(string question)
		{
			return AskQuestionWithCancel(stringParserService.Parse(question), GettextCatalog.GetString ("Question"));
		}
		
		public int ShowCustomDialog(string caption, string dialogText, params string[] buttontexts)
		{
			// TODO
			return 0;
		}
		
		public void ShowMessage(string message)
		{
			ShowMessage(message, "MonoDevelop");
		}
		
		public void ShowMessageFormatted(string formatstring, params string[] formatitems)
		{
			ShowMessage(String.Format(stringParserService.Parse(formatstring), formatitems));
		}
		
		public void ShowMessageFormatted(string caption, string formatstring, params string[] formatitems)
		{
			ShowMessage(String.Format(stringParserService.Parse(formatstring), formatitems), caption);
		}
		
		public void ShowMessage(string message, string caption)
		{
			MessageDialog md = new MessageDialog (rootWindow, DialogFlags.Modal | DialogFlags.DestroyWithParent, MessageType.Info, ButtonsType.Ok, EscapeBraces(message));
			md.Response += new ResponseHandler(OnResponse);
			md.Close += new EventHandler(OnClose);
			md.ShowAll ();
		}

		public void ShowMessage(string message, Window parent)
		{			
			MessageDialog md = new MessageDialog (rootWindow, DialogFlags.Modal | DialogFlags.DestroyWithParent, MessageType.Info, ButtonsType.Ok, EscapeBraces(message));
			
			if (parent != null)			
				md.TransientFor = parent;
							
			md.Response += new ResponseHandler(OnResponse);
			md.Close += new EventHandler(OnClose);
			md.ShowAll();
		}

		string EscapeBraces(string stringToEscape)
		{
			return stringToEscape.Replace("{", "{{").Replace("}", "}}");
		}

		void OnResponse (object o, ResponseArgs e)
		{
			((Dialog)o).Hide();
		}
		
		void OnClose(object o, EventArgs e)
		{
			((Dialog)o).Hide();
		} 
		
		// call this method to show a dialog and get a response value
		// returns null if cancel is selected
		public string GetTextResponse(string question, string caption, string initialValue)
		{
			string returnValue = null;
			
			using (Dialog md = new Dialog (caption, rootWindow, DialogFlags.Modal | DialogFlags.DestroyWithParent)) {
				// add a label with the question
				Label questionLabel = new Label(question);
				questionLabel.UseMarkup = true;
				questionLabel.Xalign = 0.0F;
				md.VBox.PackStart(questionLabel, true, false, 6);
				
				// add an entry with initialValue
				Entry responseEntry = (initialValue != null) ? new Entry(initialValue) : new Entry();
				md.VBox.PackStart(responseEntry, false, true, 6);
				
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
			}
			
			return returnValue;
		}
		
		public string GetTextResponse(string question, string caption)
		{
			return GetTextResponse(question, caption, string.Empty);
		}
	}
}
