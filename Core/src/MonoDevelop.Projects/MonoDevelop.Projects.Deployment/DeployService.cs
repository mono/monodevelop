//
// DeployService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Projects.Deployment
{
	public class DeployService: AbstractService
	{
		List<FileCopyHandler> copiers = new List<FileCopyHandler> ();
		
		public DeployService()
		{
		}
		
		public override void InitializeService()
		{
			base.InitializeService ();
			Runtime.AddInService.RegisterExtensionItemListener ("/SharpDevelop/Workbench/DeployFileCopiers", OnCopierExtensionChanged);
		}
		
		void OnCopierExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add) {
				copiers.Add (new FileCopyHandler ((IFileCopyHandler)item));
			}
		}
		
		public DeployTarget[] GetSupportedDeployTargets (CombineEntry entry)
		{
			object[] targets = Runtime.AddInService.GetTreeItems ("/SharpDevelop/Workbench/DeployHandlers");
			List<DeployTarget> list = new List<DeployTarget> ();
			foreach (DeployTarget target in targets) {
				if (target.CanDeploy (entry)) {
					target.CombineEntry = entry;
					list.Add (target);
				}
			}

			return list.ToArray ();
		}
		
		public FileCopyHandler[] GetFileCopyHandlers ()
		{
			return copiers.ToArray ();
		}
		
		internal FileCopyHandler GetFileCopyHandler (string id)
		{
			foreach (FileCopyHandler handler in copiers)
				if (handler.Id == id)
					return handler;

			return null;
		}
	}
}
