//
// ProjectPathItemProperty.cs
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
using System.IO;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class ProjectPathItemProperty: ItemPropertyAttribute
	{
		public ProjectPathItemProperty ()
		{
			SerializationDataType = typeof (PathDataType);
		}
		
		public ProjectPathItemProperty (string name): base (name)
		{
			SerializationDataType = typeof (PathDataType);
		}
	}
	
	public class PathDataType: PrimitiveDataType
	{
		public PathDataType (Type type): base (type)
		{
			if (type != typeof (string) && type != typeof (FilePath))
				throw new InvalidOperationException ("ProjectPathItemProperty can only be applied to fields of type string and FilePath");
		}

		protected override DataNode OnSerialize (SerializationContext serCtx, object mapData, object value)
		{
			FilePath path = value is string ? new FilePath ((string) value) : (FilePath) value;
			if (path.IsNullOrEmpty) return null;
			FilePath basePath = Path.GetDirectoryName (serCtx.BaseFile);
			string file = path.ToRelative (basePath);
			if (Path.DirectorySeparatorChar != serCtx.DirectorySeparatorChar)
				file = file.Replace (Path.DirectorySeparatorChar, serCtx.DirectorySeparatorChar);
			return new DataValue (Name, file);
		}
		
		protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			string file = ((DataValue)data).Value;
			if (!string.IsNullOrEmpty (file)) {
				if (Path.DirectorySeparatorChar != serCtx.DirectorySeparatorChar)
					file = file.Replace (serCtx.DirectorySeparatorChar, Path.DirectorySeparatorChar);
				string basePath = Path.GetDirectoryName (serCtx.BaseFile);
				file = FileService.RelativeToAbsolutePath (basePath, file);
			}
			if (ValueType == typeof (string))
				return file;
			else
				return (FilePath) file;
		}
	}
}


