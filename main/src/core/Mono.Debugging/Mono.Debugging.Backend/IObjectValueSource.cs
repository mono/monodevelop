// IObjectValueSource.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
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
//
//

using System;
using Mono.Debugging.Client;

namespace Mono.Debugging.Backend
{
	public interface IObjectValueSource
	{
		ObjectValue[] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options);
		EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options);
		ObjectValue GetValue (ObjectPath path, EvaluationOptions options);
		
		object GetRawValue (ObjectPath path, EvaluationOptions options);
		void SetRawValue (ObjectPath path, object value, EvaluationOptions options);
	}
	
	public interface IRawValue
	{
		object CallMethod (string name, object[] parameters, EvaluationOptions options);
		object GetMemberValue (string name, EvaluationOptions options);
		void SetMemberValue (string name, object value, EvaluationOptions options);
	}
	
	public interface IRawValueArray
	{
		object GetValue (int[] index);
		void SetValue (int[] index, object value);
		int[] Dimensions { get; }
		Array ToArray ();
	}
	
	public interface IRawValueString
	{
		string Substring (int index, int length);
		string Value { get; }
		int Length { get; }
	}
	
	[Serializable]
	public class EvaluationResult
	{
		public EvaluationResult (string value)
		{
			Value = value;
		}
		
		public EvaluationResult (string value, string displayValue)
		{
			Value = value;
			DisplayValue = displayValue;
		}
		
		public string Value { get; private set; }
		public string DisplayValue { get; private set; }
		
		public override string ToString ()
		{
			return Value;
		}
	}
}
