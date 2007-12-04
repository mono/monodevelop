//  IMessageService.cs
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

using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui
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
		
		object RootWindow { get; set; }
	}
}
