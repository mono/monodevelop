// ISerializationAttributeProvider.cs
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
using System.Xml;

namespace MonoDevelop.Projects.Serialization
{
	internal interface ISerializationAttributeProvider
	{
		object GetCustomAttribute (object ob, Type type, bool inherit);
		object[] GetCustomAttributes (object ob, Type type, bool inherit);
		bool IsDefined (object ob, Type type, bool inherit);
		ICustomDataItem GetCustomDataItem (object ob);
		ItemMember[] GetItemMembers (Type type);
	}
	
	class ItemMember
	{
		string name;
		Type type;
		Type declaringType;
		object initValue;
		string insertBefore;
		
		public ItemMember ()
		{
		}
		
		public ItemMember (Type declaringType, string name)
		{
			this.declaringType = declaringType;
			this.name = name;
			this.type = typeof(string);
		}
		
		public ItemMember (Type declaringType, string name, Type memberType)
		{
			this.declaringType = declaringType;
			this.name = name;
			this.type = memberType;
		}
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public Type Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}

		public Type DeclaringType {
			get {
				return declaringType;
			}
			set {
				declaringType = value;
			}
		}

		public object InitValue {
			get {
				return initValue;
			}
			set {
				initValue = value;
			}
		}

		public string InsertBefore {
			get {
				return insertBefore;
			}
			set {
				insertBefore = value;
			}
		}
	}
}
