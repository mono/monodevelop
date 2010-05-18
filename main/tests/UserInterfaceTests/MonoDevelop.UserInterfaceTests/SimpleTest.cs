// 
// SimpleTest.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.UserInterfaceTests
{
	public class SimpleTest: BaseStressTest
	{
		string project;
		
		protected override void Setup ()
		{
			base.Setup ();
			project = IdeApi.OpenTestSolution ("ContactBook/ContactBook.mds");
		}
		
		protected override void TearDown ()
		{
			base.TearDown ();
			IdeApi.CloseWorkspace ();
		}
		
		[TestStep]
		public void LoadFile ()
		{
			IdeApi.OpenFile ("ContactBook/Main.cs", project);
			System.Threading.Thread.Sleep (50);
		}
		
		[UndoTestStep ("LoadFile")]
		public void UndoLoadFile ()
		{
			IdeApi.CloseActiveFile ();
			System.Threading.Thread.Sleep (50);
		}
		
		[TestStep (RunAfter="LoadFile")]
		public void TypeSomeText ()
		{
			Session.SelectActiveWidget ();
			for (int n=0; n<12; n++)
				Session.ExecuteCommand (TextEditorCommands.LineDown);
			Session.ExecuteCommand (TextEditorCommands.LineEnd);
			Session.TypeText ("Gtk.Widget w; w.ca");
		}
		
		[UndoTestStep ("TypeSomeText")]
		public void UndoTypeSomeText ()
		{
			Session.SelectActiveWidget ();
			for (int n=0; n<18;n++)
				Session.ExecuteCommand (EditCommands.Undo);
			Session.ExecuteCommand (TextEditorCommands.DocumentStart);
		}
		
		[TestStep (RunAfter="TypeSomeText")]
		public void CloseFile ()
		{
			IdeApi.CloseActiveFile ();
		}
	}
}

