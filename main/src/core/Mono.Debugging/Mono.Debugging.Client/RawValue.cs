// 
// RawValue.cs
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
using Mono.Debugging.Backend;

namespace Mono.Debugging.Client
{
	/// <summary>
	/// Represents an object in the process being debugged
	/// </summary>
	[Serializable]
	public class RawValue
	{
		IRawValue source;
		internal EvaluationOptions options;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Mono.Debugging.Client.RawValue"/> class.
		/// </summary>
		/// <param name='source'>
		/// Value source
		/// </param>
		public RawValue (IRawValue source)
		{
			this.source = source;
		}
		
		internal IRawValue Source {
			get { return this.source; }
		}
		
		/// <summary>
		/// Full name of the type of the object
		/// </summary>
		public string TypeName { get; set; }
		
		/// <summary>
		/// Invokes a method on the object
		/// </summary>
		/// <returns>
		/// The result of the invocation
		/// </returns>
		/// <param name='methodName'>
		/// The name of the method
		/// </param>
		/// <param name='parameters'>
		/// The parameters (primitive type values, RawValue instances or RawValueArray instances)
		/// </param>
		public object CallMethod (string methodName, params object[] parameters)
		{
			object res = source.CallMethod (methodName, parameters, options);
			RawValue val = res as RawValue;
			if (val != null)
				val.options = options;
			return res;
		}
		
		/// <summary>
		/// Gets the value of a field or property
		/// </summary>
		/// <returns>
		/// The value (a primitive type value, a RawValue instance or a RawValueArray instance)
		/// </returns>
		/// <param name='name'>
		/// Name of the field or property
		/// </param>
		public object GetMemberValue (string name)
		{
			object res = source.GetMemberValue (name, options);
			RawValue val = res as RawValue;
			if (val != null)
				val.options = options;
			return res;
		}
		
		/// <summary>
		/// Sets the value of a field or property
		/// </summary>
		/// <param name='name'>
		/// Name of the field or property
		/// </param>
		/// <param name='value'>
		/// The value (a primitive type value, a RawValue instance or a RawValueArray instance)
		/// </param>
		public void SetMemberValue (string name, object value)
		{
			source.SetMemberValue (name, value, options);
		}
	}
	
	/// <summary>
	/// Represents an array of objects in the process being debugged
	/// </summary>
	[Serializable]
	public class RawValueArray
	{
		IRawValueArray source;
		int[] dimensions;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Mono.Debugging.Client.RawValueArray"/> class.
		/// </summary>
		/// <param name='source'>
		/// Value source.
		/// </param>
		public RawValueArray (IRawValueArray source)
		{
			this.source = source;
		}
		
		internal IRawValueArray Source {
			get { return this.source; }
		}
		
		/// <summary>
		/// Full type name of the array items
		/// </summary>
		public string ElementTypeName { get; set; }

		/// <summary>
		/// Gets or sets the item at the specified index.
		/// </summary>
		/// <param name='index'>
		/// The index
		/// </param>
		/// <remarks>
		/// The item value can be a primitive type value, a RawValue instance or a RawValueArray instance.
		/// </remarks>
		public object this [int index] {
			get {
				return source.GetValue (new int[] { index });
			}
			set {
				source.SetValue (new int[] { index }, value);
			}
		}
		
		/// <summary>
		/// Returns an array with all items of the RawValueArray
		/// </summary>
		/// <remarks>
		/// This method is useful to avoid unnecessary debugger-debuggee roundtrips
		/// when processing all items of an array. For example, if a RawValueArray
		/// represents an image encoded in a byte[], getting the values one by one
		/// using the indexer is very slow. The ToArray() will return the whole byte[]
		/// in a single call.
		/// </remarks>
		public Array ToArray ()
		{
			return source.ToArray ();
		}

		/// <summary>
		/// Gets the length of the array
		/// </summary>
		public int Length {
			get {
				if (dimensions == null)
					dimensions = source.Dimensions;
				return dimensions[0];
			}
		}
	}
	
	/// <summary>
	/// Represents a string object in the process being debugged
	/// </summary>
	[Serializable]
	public class RawValueString
	{
		IRawValueString source;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Mono.Debugging.Client.RawValueString"/> class.
		/// </summary>
		/// <param name='source'>
		/// Value source.
		/// </param>
		public RawValueString (IRawValueString source)
		{
			this.source = source;
		}
		
		internal IRawValueString Source {
			get { return this.source; }
		}

		/// <summary>
		/// Gets the length of the string
		/// </summary>
		public int Length {
			get { return source.Length; }
		}
		
		/// <summary>
		/// Gets a substring of the string
		/// </summary>
		/// <param name='index'>
		/// The starting index of the requested substring.
		/// </param>
		/// <param name='length'>
		/// The length of the requested substring.
		/// </param>
		public string Substring (int index, int length)
		{
			return source.Substring (index, length);
		}
		
		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <value>
		/// The value.
		/// </value>
		public string Value {
			get { return source.Value; }
		}
	}
}

