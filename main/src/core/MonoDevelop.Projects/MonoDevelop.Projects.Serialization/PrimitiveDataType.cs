//
// PrimitiveDataType.cs
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
using System.Xml;

namespace MonoDevelop.Projects.Serialization
{
	public class PrimitiveDataType: DataType
	{
		public PrimitiveDataType (Type propType): base (propType)
		{
		}
		
		public override bool IsSimpleType { get { return true; } }
		public override bool CanCreateInstance { get { return true; } }
		public override bool CanReuseInstance { get { return false; } }
		
		internal protected override DataNode OnSerialize (SerializationContext serCtx, object mapData, object value)
		{
			return new DataValue (Name, value.ToString ());
		}
		
		internal protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			return Convert.ChangeType (((DataValue)data).Value, ValueType);
		}
	}
	
	internal class DateTimeDataType: PrimitiveDataType
	{
		public DateTimeDataType (): base (typeof(DateTime))
		{
		}
		
		internal protected override DataNode OnSerialize (SerializationContext serCtx, object mapData, object value)
		{
			return new DataValue (Name, XmlConvert.ToString ((DateTime)value, XmlDateTimeSerializationMode.Local));
		}
		
		internal protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			return XmlConvert.ToDateTime (((DataValue)data).Value, XmlDateTimeSerializationMode.Local);
		}
	}
}
