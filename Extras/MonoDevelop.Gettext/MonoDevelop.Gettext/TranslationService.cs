//
// TranslationService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Gettext
{
	public class TranslationService
	{
		static bool isTranslationEnabled = false;
		
		public static bool IsTranslationEnabled {
			get {
				return isTranslationEnabled;
			}
			set {
				isTranslationEnabled = value;
			}
		}
		static bool isInitialized = false;
		internal static void InitializeTranslationService ()
		{
			Debug.Assert (!isInitialized);
			isInitialized = true;
			IdeApp.ProjectOperations.CombineOpened += new CombineEventHandler (CombineOpened);
			IdeApp.ProjectOperations.CombineClosed += delegate {
				isTranslationEnabled = false;
			};
		}
		
		static void CombineOpened (object sender, CombineEventArgs e)
		{
			foreach (CombineEntry entry in e.Combine.Entries) {
				if (entry is TranslationProject) {
					isTranslationEnabled = true;
					return;
				}
			}
			isTranslationEnabled = false;
		}
		

	}
	
	public class TranslationServiceStartupCommand : CommandHandler
	{
		protected override void Run ()
		{
			TranslationService.InitializeTranslationService ();
		}
	}
}