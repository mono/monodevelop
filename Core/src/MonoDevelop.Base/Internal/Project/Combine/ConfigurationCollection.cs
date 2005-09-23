//
// ConfigurationCollection.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Serialization;

using MonoDevelop.Core.Properties;

namespace MonoDevelop.Internal.Project
{
	public class ConfigurationCollection : CollectionBase
	{
		ArrayList tmpClear;
		
		public int Add (IConfiguration config)
		{
			return List.Add (config);
		}
		
		public new IConfiguration this [int index] {
			get {
				return (IConfiguration) List [index];
			}
		}
		
		public IConfiguration this [string name] {
			get {
				foreach (IConfiguration c in this)
					if (c.Name == name)
						return c;
				return null;
			}
		}
		
		public void Remove (IConfiguration config)
		{
			List.Remove (config);
		}
		
		public void Remove (string name)
		{
			for (int n=0; n<Count; n++) {
				if (this [n].Name == name) {
					List.RemoveAt (n);
					return;
				}
			}
		}
		
		protected override void OnInsertComplete (int index, object value)
		{
			OnConfigurationAdded ((IConfiguration) value);
		}
		
		protected override void OnRemoveComplete (int index, object value)
		{
			OnConfigurationRemoved ((IConfiguration) value);
		}
		
		protected override void OnSetComplete (int index, object oldValue, object newValue)
		{
			OnConfigurationRemoved ((IConfiguration) oldValue);
			OnConfigurationAdded ((IConfiguration) newValue);
		}
		
		protected override void OnClear ()
		{
			tmpClear = (ArrayList) InnerList.Clone ();
		}
		
		protected override void OnClearComplete ()
		{
			foreach (object value in tmpClear)
				OnConfigurationRemoved ((IConfiguration) value);
			tmpClear = null;
		}
		
		protected virtual void OnConfigurationAdded (IConfiguration conf)
		{
			if (ConfigurationAdded != null)
				ConfigurationAdded (this, new ConfigurationEventArgs (null, conf));
		}
		
		protected virtual void OnConfigurationRemoved (IConfiguration conf)
		{
			if (ConfigurationRemoved != null)
				ConfigurationRemoved (this, new ConfigurationEventArgs (null, conf));
		}
		
		public event ConfigurationEventHandler ConfigurationAdded;
		public event ConfigurationEventHandler ConfigurationRemoved;
	}
}
