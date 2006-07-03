//
// DeployTarget.cs
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
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Deployment
{
	[DataItem (FallbackType=typeof(UnknownDeployTarget))]
	public class DeployTarget
	{
		DeployHandler handler;
		
		[ItemProperty ("Handler")]
		string handlerId;
		
		[ItemProperty ("Name")]
		string name;
		
		CombineEntry entry;
		
		public DeployTarget ()
		{
		}
		
		internal void Bind (DeployHandler handler)
		{
			this.handler = handler;
			this.handlerId = handler.Id;
		}
		
		internal void SetCombineEntry (CombineEntry entry)
		{
			this.entry = entry;
		}
		
		public void Deploy (IProgressMonitor monitor)
		{
			handler.Deploy (monitor, this);
		}
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public CombineEntry CombineEntry {
			get { return entry; }
		}
		
		public virtual void CopyFrom (DeployTarget other)
		{
			name = other.name;
			handlerId = other.handlerId;
			handler = other.handler;
			entry = other.entry;
		}
		
		public DeployHandler DeployHandler {
			get {
				if (handler == null) {
					if (handlerId == null)
						throw new InvalidOperationException ("Deploy target not bound to a handler");
					handler = Services.DeployService.GetDeployHandler (handlerId);
					if (handler == null)
						throw new InvalidOperationException ("Deploy handler not found: " + handlerId);
				}
				return handler;
			}
		}
		
		public virtual string GetDefaultName (CombineEntry entry)
		{
			string bname = DeployHandler.Description;
			string name = bname;
			
			int n = 2;
			while (true) {
				bool found = false;
				foreach (DeployTarget dt in entry.DeployTargets) {
					if (dt.Name == name) {
						found = true;
						break;
					}
				}
				if (found) {
					name = bname + " " + n;
					n++;
				} else
					break;
			}

			return name;
		}
	}
	
	public class DeployTargetCollection: List<DeployTarget>
	{
	}
}
