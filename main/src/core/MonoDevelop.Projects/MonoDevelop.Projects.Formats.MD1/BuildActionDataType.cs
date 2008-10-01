// 
// BuildActionDataType.cs
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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.MD1
{
	
	/// <summary>
	/// Maps some build action names between the MD1 format and the standard MSBuild equivalents
	/// </summary>
	class BuildActionDataType : PrimitiveDataType
	{
		string EmbedAsResource = "EmbedAsResource";
		string Nothing = "Nothing";
		string Exclude = "Exclude";
		
		public BuildActionDataType (Type type): base (type)
		{
		}

		protected override DataNode OnSerialize (SerializationContext serCtx, object mapData, object value)
		{
			string str = value as string;
			switch (str) {
			case "None":
				str = Nothing;
				break;
			case "EmbeddedResource":
				str = EmbedAsResource;
				break;
			case "Content":
				str = Exclude;
				break;
			}
			return new DataValue (Name, str);
		}
		
		protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			string str = ((DataValue)data).Value;
			switch (str) {
			case "Nothing":
				str = BuildAction.None;
				break;
			case "EmbedAsResource":
				str = BuildAction.EmbeddedResource;
				break;
			case "FileCopy":
			case "Exclude":
				str = BuildAction.Content;
				break;
			}
			return str;
		}
	}	
}
