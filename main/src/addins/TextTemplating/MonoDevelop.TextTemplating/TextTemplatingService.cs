// 
// TextTemplatingService.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Collections.Generic;
using Mono.TextTemplating;
using Microsoft.VisualStudio.TextTemplating;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Tasks;
using System.CodeDom.Compiler;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.TextTemplating
{
	
	
	public static class TextTemplatingService
	{	
		public static void ShowTemplateHostErrors (CompilerErrorCollection errors)
		{
			if (errors.Count == 0)
				return;
			
			TaskService.Errors.Clear ();
			foreach (CompilerError err in errors) {
					TaskService.Errors.Add (new Task (err.FileName, err.ErrorText, err.Column, err.Line,
					                                  err.IsWarning? TaskSeverity.Warning : TaskSeverity.Error));
			}
			TaskService.ShowErrors ();
		}
		
		static RecyclableAppDomain domain;
		
		public static RecyclableAppDomain.Handle GetTemplatingDomain ()
		{
			if (domain == null || domain.Used) {
				var dir = Path.GetDirectoryName (typeof (TemplatingEngine).Assembly.Location);
				var info = new AppDomainSetup () {
					ApplicationBase = dir,
				};
				domain = new RecyclableAppDomain ("T4Domain", info);
			}
			return domain.GetHandle ();
		}
	}
	
	public class RecyclableAppDomain
	{
		const int DOMAIN_TIMEOUT = 2 * 60 * 1000;
		const int DOMAIN_RECYCLE_AFTER = 10;
		
		int handleCount = 0;
		int uses = 0;
		AppDomain domain;
		
		public RecyclableAppDomain (string name, AppDomainSetup info)
		{
			domain = AppDomain.CreateDomain (name, null, info);
			
			//FIXME: do we really want to allow resolving arbitrary MD assemblies?
			// some things do depend on this behaviour, but maybe some kind of explicit registration system 
			// would be better, so we can prevent accidentally pulling in unwanted assemblies
			domain.AssemblyResolve += new Mono.TextTemplating.CrossAppDomainAssemblyResolver ().Resolve;
		}
		
		public bool Used { get; private set; }
		
		~RecyclableAppDomain ()
		{
			if (handleCount != 0)
				Console.WriteLine ("WARNING: RecyclableAppDomain's handles were not all disposed");
		}
		
		void Kill ()
		{
			if (domain != null) {
				AppDomain.Unload (domain);
				domain = null;
				GC.SuppressFinalize (this);
			}
		}
		
		public RecyclableAppDomain.Handle GetHandle ()
		{
			return new RecyclableAppDomain.Handle (this);
		}
		
		public class Handle : IDisposable
		{
			RecyclableAppDomain parent;
			
			internal Handle (RecyclableAppDomain parent)
			{
				this.parent = parent;
				parent.handleCount++;
				parent.uses++;
				if (parent.uses > DOMAIN_RECYCLE_AFTER)
					parent.Used = true;
			}
			
			public AppDomain Domain {
				get { return parent.domain; }
			}
			
			public void Dispose ()
			{
				if (parent != null) {
					parent.handleCount--;
					if (parent.Used && parent.handleCount == 0)
						parent.Kill ();
					parent = null;
				}
			}
		}
	}
}
