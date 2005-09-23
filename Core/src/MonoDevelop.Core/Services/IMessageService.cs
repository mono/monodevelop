// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core.Services
{
	public enum QuestionResponse
	{
		Yes,
		No,
		Cancel
	}
	
	/// <summary>
	/// This interface must be implemented by all services.
	/// </summary>
	public interface IMessageService
	{
		void ShowError(Exception ex);
		void ShowError(string message);
		void ShowError(Exception ex, string message);
		void ShowErrorFormatted(string formatstring, params string[] formatitems);
		
		void ShowWarning(string message);
		void ShowWarningFormatted(string formatstring, params string[] formatitems);
		
		void ShowMessage(string message);
		void ShowMessage(string message, string caption);
		void ShowMessageFormatted(string formatstring, params string[] formatitems);
		void ShowMessageFormatted(string caption, string formatstring, params string[] formatitems);
		
		/// <summary>
		/// returns the number of the chosen button
		/// </summary>
		int  ShowCustomDialog(string caption, 
		                      string dialogText,
		                      params string[] buttontexts);
		
		bool AskQuestion(string question);
		bool AskQuestionFormatted(string formatstring, params string[] formatitems);
		bool AskQuestion(string question, string caption);
		bool AskQuestionFormatted(string caption, string formatstring, params string[] formatitems);

		QuestionResponse AskQuestionWithCancel(string question);
		QuestionResponse AskQuestionFormattedWithCancel(string formatstring, params string[] formatitems);
		QuestionResponse AskQuestionWithCancel(string question, string caption);
		QuestionResponse AskQuestionFormattedWithCancel(string caption, string formatstring, params string[] formatitems);
		
		// used to return text input from a user in response to a question
		string GetTextResponse(string question, string caption, string initialValue);
		string GetTextResponse(string question, string caption);
	}
}
