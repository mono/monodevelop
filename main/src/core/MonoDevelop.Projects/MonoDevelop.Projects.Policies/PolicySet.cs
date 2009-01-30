// 
// PolicySet.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Xml;

using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects.Policies
{
	
	
	public class PolicySet
	{
		Dictionary<Type, object> policies = new Dictionary<Type, object> ();
		bool isFromFile;
		
		internal PolicySet (string id, string name)
		{
			this.Id = id;
			this.Name = name;
		}
		
		internal Dictionary<Type, object> Policies { get { return policies; } } 
		
		public bool Has<T> ()
		{
			return Has (typeof (T));
		}
		
		public bool Has (Type t)
		{
			return policies.ContainsKey (t);
		}
		
		public object Get (Type t)
		{
			object o;
			policies.TryGetValue (t, out o);
			return o;
		}
		
		public T Get<T> () where T : class, IEquatable<T>
		{
			return Get (typeof (T)) as T;
		}
		
		public void Set<T> (T value) where T : class, IEquatable<T>
		{
			if (!isFromFile)
				throw new InvalidOperationException ("Cannot modify fixed policy sets");
			policies[typeof (T)] = value;
		}
		
		public void Set (object value)
		{
			if (!isFromFile)
				throw new InvalidOperationException ("Cannot modify fixed policy sets");
			policies[value.GetType ()] = value;
		}
		
		public string Name { get; private set; }
		public string Id { get; private set; }
		
		internal void AddSerializedPolicies (StreamReader reader)
		{
			foreach (object policy in PolicyService.RawDeserializeXml (reader)) {
				Type t = policy.GetType ();
				if (policies.ContainsKey (t))
					throw new InvalidOperationException ("Cannot add second policy of type '" +  
					                                     t.ToString () + "' to policy set '" + Id + "'");
				policies[policy.GetType ()] = policy;
			}
		}
		
		internal void RemoveSerializedPolicies (StreamReader reader)
		{
			// NOTE: this could be more efficient if it just got the types instead of a 
			// full deserialisation
			foreach (object policy in PolicyService.RawDeserializeXml (reader))
				policies.Remove (policy.GetType ());
		}
		
		internal void SaveToFile (StreamWriter writer)
		{
			using (XmlWriter xw = new XmlTextWriter (writer)) {
				xw.WriteStartDocument ();
				xw.WriteStartElement ("PolicySet");
				foreach (object o in policies.Values)
					XmlConfigurationWriter.DefaultWriter.Write (xw, PolicyService.RawSerialize (o));
				xw.WriteEndElement ();
			}
		}
		
		internal void LoadFromFile (StreamReader reader)
		{
			policies.Clear ();
			AddSerializedPolicies (reader);
			isFromFile = true;
		}
	}
}
