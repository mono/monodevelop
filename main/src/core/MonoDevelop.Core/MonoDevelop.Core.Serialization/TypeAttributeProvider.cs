// ReflectionSerializationMap.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Reflection;

namespace MonoDevelop.Core.Serialization
{
	class TypeAttributeProvider: ISerializationAttributeProvider
	{
		public static ISerializationAttributeProvider Instance = new TypeAttributeProvider ();
		
		public object GetCustomAttribute (object ob, Type type, bool inherit)
		{
			if (ob is MemberInfo)
				return Attribute.GetCustomAttribute ((MemberInfo)ob, type, inherit);
			else
				return null;
		}

		public object[] GetCustomAttributes (object ob, Type type, bool inherit)
		{
			if (ob is MemberInfo)
				return Attribute.GetCustomAttributes ((MemberInfo)ob, type, inherit);
			else
				return null;
		}

		public bool IsDefined (object ob, Type type, bool inherit)
		{
			MemberInfo mi = ob as MemberInfo;
			if (mi != null)
				return Attribute.IsDefined (mi, type, inherit);
			else
				return false;
		}
		
		public ICustomDataItem GetCustomDataItem (object ob)
		{
			return ob as ICustomDataItem;
		}

		public ItemMember[] GetItemMembers (Type type)
		{
			return new ItemMember [0];
		}
	}
}
