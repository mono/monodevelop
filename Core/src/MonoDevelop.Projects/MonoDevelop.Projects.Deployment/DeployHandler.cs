//
// DeployHandler.cs
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

namespace MonoDevelop.Projects.Deployment
{
	public sealed class DeployHandler
	{
		IDeployHandler handler;
		
		internal DeployHandler (IDeployHandler handler)
		{
			this.handler = handler;
		}
		
		public string Id {
			get { return handler.Id; }
		}
		
		public string Icon {
			get { return handler.Icon != null ? handler.Icon : "md-package"; }
		}
		
		public string Description { 
			get { return handler.Description; } 
		}
		
		public DeployTarget CreateTarget (CombineEntry entry)
		{
			return handler.CreateTarget (entry);
		}
		
		internal bool CanDeploy (CombineEntry entry)
		{
			return handler.CanDeploy (entry);
		}
	}
}
