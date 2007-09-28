// DeployTargets.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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

using System;
using System.Collections;

namespace MonoDevelop.AspNet.Deployment
{
	
	public class WebDeployTargetCollection : CollectionBase
	{
		
		public int Add (WebDeployTarget target)
		{
			return List.Add (target);
		}
		
		public WebDeployTarget this [int index] {
			get {
				return (WebDeployTarget) List [index];
			}
		}
		
		public WebDeployTarget this [string name] {
			get {
				foreach (WebDeployTarget c in this)
					if (c.Name == name)
						return c;
				return null;
			}
		}
		
		public void Remove (WebDeployTarget target)
		{
			List.Remove (target);
		}
		
		public void Remove (string name)
		{
			int nameAt = ContainsName (name);
			if (nameAt >= 0)
				List.RemoveAt (nameAt);
		}
		
		int ContainsName (string name)
		{
			for (int i = 0; i < Count; i++)
				if (this [i].Name == name)
					return i;
			return -1;
		}
		
		//this is used to ensure that each target has a unique name within the collection
		bool settingName = false;
		internal void EnforceUniqueName (WebDeployTarget target)
		{
			if (settingName)
				return;
			
			bool foundSameName = false;
			if (!string.IsNullOrEmpty (target.Name)) {
				foreach (WebDeployTarget c in this)
					if (c != target && c.Name == target.Name)
						foundSameName = true;
			}
			if (!foundSameName)
				return;
			
			int newIndex = 1;
			do {
				string newName = MonoDevelop.Core.GettextCatalog.GetString ("Web Deploy Target {0}", newIndex);
				if (ContainsName (newName) < 0) {
					settingName = true;
					target.name = newName;
					settingName = false;
					return;
				}
				newIndex++;
			} while (newIndex < int.MaxValue);
			throw new InvalidOperationException ("Ran out of indices for default target names.");
		}
		
		protected override void OnSet (int index, object oldValue, object newValue)
		{
			WebDeployTarget target = (WebDeployTarget) newValue;
			target.parent = this;
			EnforceUniqueName (target);
			base.OnSet (index, oldValue, newValue);
		}
		
		protected override void OnInsert (int index, object value)
		{
			WebDeployTarget target = (WebDeployTarget) value;
			target.parent = this;
			EnforceUniqueName (target);
			base.OnInsert (index, value);
		}
		
		protected override void OnRemove (int index, object value)
		{
			WebDeployTarget target = (WebDeployTarget) value;
			target.parent = null;
			base.OnRemove (index, value);
		}

		
		internal WebDeployTargetCollection Clone ()
		{
			WebDeployTargetCollection clone = new WebDeployTargetCollection ();
			foreach (WebDeployTarget target in this)
				clone.Add ((WebDeployTarget) target.Clone ());
			return clone;
		}
		
		public bool ContainsTargetValidForDeployment ()
		{
			foreach (WebDeployTarget target in this)
				if (target.ValidForDeployment)
					return true;
			return false;
		}
	}
}
