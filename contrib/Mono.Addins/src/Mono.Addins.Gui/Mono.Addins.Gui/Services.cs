//
// Services.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using Gtk;
using Mono.Unix;
using Mono.Addins.Setup;

namespace Mono.Addins.Gui
{
	internal class Services
	{
		public static bool InApplicationNamespace (SetupService service, string id)
		{
			return service.ApplicationNamespace == null || id.StartsWith (service.ApplicationNamespace + ".");
		}
		
		public static bool AskQuestion (string question)
		{
			MessageDialog md = new MessageDialog (null, DialogFlags.Modal | DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, question);
			try {
				int response = md.Run ();
				return ((ResponseType) response == ResponseType.Yes);
			} finally {
				md.Destroy ();
			}
		}
		
		public static void ShowError (Exception ex, string message, Window parent, bool modal)
		{
			ErrorDialog dlg = new ErrorDialog (parent);
			
			if (message == null) {
				if (ex != null)
					dlg.Message = string.Format (Catalog.GetString ("Exception occurred: {0}"), ex.Message);
				else {
					dlg.Message = "An unknown error occurred";
					dlg.AddDetails (Environment.StackTrace, false);
				}
			} else
				dlg.Message = message;
			
			if (ex != null) {
				dlg.AddDetails (string.Format (Catalog.GetString ("Exception occurred: {0}"), ex.Message) + "\n\n", true);
				dlg.AddDetails (ex.ToString (), false);
			}

			if (modal) {
				dlg.Run ();
				dlg.Destroy ();
			} else
				dlg.Show ();
		}
	}
}
