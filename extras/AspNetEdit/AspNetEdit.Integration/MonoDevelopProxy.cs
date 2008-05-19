//
// MonoDevelopProxy.cs: Proxies methods that run in the MD process so 
//    that they are remotely accessible to the AspNetEdit process.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.CodeDom;
using System.Collections.Generic;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.DesignerSupport;

namespace AspNetEdit.Integration
{
	
	public class MonoDevelopProxy : MarshalByRefObject, IDisposable
	{
		IClass codeBehindClass;
		CombineEntry project;
		
		public MonoDevelopProxy (CombineEntry project, IClass codeBehindClass)
		{
			this.codeBehindClass = codeBehindClass;
			this.project = project;
		}
		
		//keep this object available through remoting
		public override object InitializeLifetimeService ()
		{
			return null;
		}
		
		bool disposed = false;
		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			
			//The proxy uses InitializeLifetimeService to make sure it stays connected
			//So need to make sure we don't keep it around forever
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}
		
		//TODO: make this work with inline code
		#region event binding
		
		public bool IdentifierExistsInCodeBehind (string trialIdentifier)
		{
			if (codeBehindClass == null)
				return false;
			
			return BindingService.IdentifierExistsInClass (codeBehindClass, trialIdentifier);
		}
		
		public string GenerateIdentifierUniqueInCodeBehind (string trialIdentifier)
		{
			if (codeBehindClass == null)
				return trialIdentifier;
			
			return BindingService.GenerateIdentifierUniqueInClass (codeBehindClass, trialIdentifier);
		}
		
		
		public string[] GetCompatibleMethodsInCodeBehind (CodeMemberMethod method)
		{			
			if (codeBehindClass == null)
				return new string[0];
			
			return BindingService.GetCompatibleMethodsInClass (codeBehindClass, method);
		}
		
		public bool ShowMethod (CodeMemberMethod method)
		{
			if (codeBehindClass == null)
				return false;
			
			Gtk.Application.Invoke ( delegate {
				BindingService.CreateAndShowMember (project, codeBehindClass, method);
			});
			
			return true;
		}
		
		public bool ShowLine (int lineNumber)
		{
			if (codeBehindClass == null)
				return false;
			
			Gtk.Application.Invoke ( delegate {
				IdeApp.Workbench.OpenDocument (codeBehindClass.Region.FileName, lineNumber, 1, true);
			});
			
			return true;
		}
		
		#endregion event binding
	}
	
}
