//
// SerializationContext.cs
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
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.Core.Serialization
{
	public class SerializationContext: IDisposable
	{
		string file;
		IPropertyFilter propertyFilter;
		DataSerializer serializer;
		IProgressMonitor monitor;
		char directorySeparatorChar = System.IO.Path.DirectorySeparatorChar;
		HashSet<ItemProperty> forcedSerializationProps;
		
		public string BaseFile {
			get { return file; }
			set { file = value; }
		}

		public IPropertyFilter PropertyFilter {
			get {
				return propertyFilter;
			}
			set {
				propertyFilter = value;
			}
		}

		public DataSerializer Serializer {
			get {
				return serializer;
			}
			internal set {
				serializer = value;
			}
		}

		public char DirectorySeparatorChar {
			get {
				return directorySeparatorChar;
			}
			set {
				directorySeparatorChar = value;
			}
		}

		public IProgressMonitor ProgressMonitor {
			get {
				return monitor;
			}
			set {
				monitor = value;
			}
		}
		
		public bool IncludeDefaultValues { get; set; }
		
		public void ResetDefaultValueSerialization ()
		{
			forcedSerializationProps = null;
		}
		
		public void ForceDefaultValueSerialization (ItemProperty prop)
		{
			if (forcedSerializationProps == null)
				forcedSerializationProps = new HashSet<ItemProperty> ();
			forcedSerializationProps.Add (prop);
		}
		
		public bool IsDefaultValueSerializationForced (ItemProperty prop)
		{
			if (IncludeDefaultValues)
				return true;
			else if (forcedSerializationProps != null)
				return forcedSerializationProps.Contains (prop);
			else
				return false;
		}
		
		public virtual void Close ()
		{
		}
		
		public void Dispose ()
		{
			Close ();
		}
	}
}
