//
// DataSerializer.cs
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
using System.Collections;

namespace MonoDevelop.Internal.Serialization
{
	public class DataSerializer
	{
		SerializationContext serializationContext;
		DataContext dataContext;
		
		public DataSerializer (DataContext ctx)
		{
			dataContext = ctx;
			serializationContext = ctx.CreateSerializationContext ();
		}
		
		public DataSerializer (DataContext ctx, string baseFile)
		{
			dataContext = ctx;
			serializationContext = ctx.CreateSerializationContext ();
			serializationContext.BaseFile = baseFile;
		}
		
		public SerializationContext SerializationContext {
			get { return serializationContext; }
		}
		
		public DataNode Serialize (object obj)
		{
			return dataContext.SaveConfigurationData (serializationContext, obj, null);
		}
		
		public DataNode Serialize (object obj, Type type)
		{
			return dataContext.SaveConfigurationData (serializationContext, obj, type);
		}
		
		public object Deserialize (Type type, DataNode data)
		{
			return dataContext.LoadConfigurationData (serializationContext, type, data);
		}
		
		public void Deserialize (object obj, DataItem data)
		{
			dataContext.SetConfigurationItemData (serializationContext, obj, data);
		}
	}
}
