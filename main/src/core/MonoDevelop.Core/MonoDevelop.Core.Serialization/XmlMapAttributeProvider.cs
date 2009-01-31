// XmlMapAttributeProvider.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Core.Serialization
{
	public class XmlMapAttributeProvider: ISerializationAttributeProvider
	{
		Dictionary<Type, SerializationMap> maps = new Dictionary<Type,SerializationMap> ();
		BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		
		public XmlMapAttributeProvider ()
		{
		}
		
		public void AddMap (RuntimeAddin addin, string xmlMap, string fileId)
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (xmlMap);
			
			foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("DataItem")) {
				string tname = elem.GetAttribute ("class");
				Type type = addin.GetType (tname);
				if (type == null) {
					LoggingService.LogError ("[SerializationMap " + fileId + "] Type not found: '" + tname + "'");
					continue;
				}
				
				string cname = elem.GetAttribute ("name");
				string ftname = elem.GetAttribute ("fallbackType");

				SerializationMap map;
				if (!maps.TryGetValue (type, out map)) {
					map = new SerializationMap (type);
					maps [type] = map;
					map.FileId = fileId;
					if (cname.Length > 0 || ftname.Length > 0) {
						DataItemAttribute iat = new DataItemAttribute ();
						if (cname.Length > 0)
							iat.Name = cname;
						if (ftname.Length > 0)
							iat.FallbackType = addin.GetType (ftname, true);
						map.TypeAttributes.Add (iat);
					}
				} else {
					if (!string.IsNullOrEmpty (cname))
						throw new InvalidOperationException (string.Format ("Type name for type '{0}' in map '{1}' already specified in another serialization map for the same type ({2}).", type, fileId, map.FileId));
					if (!string.IsNullOrEmpty (ftname))
						throw new InvalidOperationException (string.Format ("Fallback type for type '{0}' in map '{1}' already specified in another serialization map for the same type ({2}).", type, fileId, map.FileId));
				}
				
				string customDataItem = elem.GetAttribute ("customDataItem");
				if (customDataItem.Length > 0) {
					ICustomDataItemHandler ch = (ICustomDataItemHandler) addin.CreateInstance (customDataItem, true);
					if (map.CustomHandler != null)
						map.CustomHandler = new CustomDataItemHandlerChain (map.CustomHandler, ch);
					else
						map.CustomHandler = ch;
				}
				
				ItemMember lastMember = null;
				int litc = 0;
				
				foreach (XmlElement att in elem.SelectNodes ("ItemProperty|ExpandedCollection|LiteralProperty|ItemMember"))
				{
					string memberName = null;
					ItemMember prevMember = lastMember;
					lastMember = null;
					
					if (att.Name == "LiteralProperty") {
						ItemMember mem = new ItemMember ();
						memberName = mem.Name = "_literal_" + (++litc);
						mem.Type = typeof(string);
						mem.InitValue = att.GetAttribute ("value");
						mem.DeclaringType = map.Type;
						map.ExtendedMembers.Add (mem);
						ItemPropertyAttribute itemAtt = new ItemPropertyAttribute ();
						itemAtt.Name = att.GetAttribute ("name");
						map.AddMemberAttribute (mem, itemAtt);
						lastMember = mem;
						continue;
					}
					else if (att.Name == "ItemMember") {
						ItemMember mem = new ItemMember ();
						memberName = mem.Name = att.GetAttribute ("name");
						mem.Type = addin.GetType (att.GetAttribute ("type"), true);
						mem.DeclaringType = map.Type;
						map.ExtendedMembers.Add (mem);
						lastMember = mem;
						continue;
					}
					else
					{
						memberName = att.GetAttribute ("member");
						
						Type mt;
						object mi;
						if (!FindMember (map, memberName, out mi, out mt)) {
							LoggingService.LogError ("[SerializationMap " + fileId + "] Member '" + memberName + "' not found in type '" + tname + "'");
							continue;
						}
						
						if (att.Name == "ItemProperty")
						{
							ItemPropertyAttribute itemAtt = new ItemPropertyAttribute ();
							
							string val = att.GetAttribute ("name");
							if (val.Length > 0)
								itemAtt.Name = val;
							
							val = att.GetAttribute ("scope");
							if (val.Length > 0)
								itemAtt.Scope = val;
							
							if (att.Attributes ["defaultValue"] != null) {
								if (mt.IsEnum)
									itemAtt.DefaultValue = Enum.Parse (mt, att.GetAttribute ("defaultValue"));
								else
									itemAtt.DefaultValue = Convert.ChangeType (att.GetAttribute ("defaultValue"), mt);
							}
							
							val = att.GetAttribute ("serializationDataType");
							if (val.Length > 0)
								itemAtt.SerializationDataType = addin.GetType (val, true);
							
							val = att.GetAttribute ("valueType");
							if (val.Length > 0)
								itemAtt.ValueType = addin.GetType (val, true);
							
							val = att.GetAttribute ("readOnly");
							if (val.Length > 0)
								itemAtt.ReadOnly = bool.Parse (val);
							
							val = att.GetAttribute ("writeOnly");
							if (val.Length > 0)
								itemAtt.WriteOnly = bool.Parse (val);
							
							val = att.GetAttribute ("fallbackType");
							if (val.Length > 0)
								itemAtt.FallbackType = addin.GetType (val, true);
							
							val = att.GetAttribute ("isExternal");
							if (val.Length > 0)
								itemAtt.IsExternal = bool.Parse (val);
							
							val = att.GetAttribute ("skipEmpty");
							if (val.Length > 0)
								itemAtt.SkipEmpty = bool.Parse (val);
							
							map.AddMemberAttribute (mi, itemAtt);
						}
						else if (att.Name == "ExpandedCollection")
						{
							ExpandedCollectionAttribute eat = new ExpandedCollectionAttribute ();
							map.AddMemberAttribute (mi, eat);
						}
					}
					if (prevMember != null)
						prevMember.InsertBefore = memberName;
				}
			}
		}
		
		public void RemoveMap (string fileId)
		{
			List<Type> toDelete = new List<Type> ();
			foreach (KeyValuePair<Type,SerializationMap> entry in maps) {
				if (entry.Value.FileId == fileId)
					toDelete.Add (entry.Key);
			}
			foreach (Type type in toDelete)
				maps.Remove (type);
		}
		
		bool FindMember (SerializationMap map, string name, out object member, out Type memberType)
		{
			FieldInfo fi = map.Type.GetField (name, bindingFlags);
			if (fi != null) {
				memberType = fi.FieldType;
				member = fi;
				return true;
			}
			PropertyInfo pi = map.Type.GetProperty (name, bindingFlags);
			if (pi != null) {
				memberType = pi.PropertyType;
				member = pi;
				return true;
			}
			
			foreach (ItemMember mem in map.ExtendedMembers) {
				if (mem.Name == name) {
					member = mem;
					memberType = mem.Type;
					return true;
				}
			}
			member = null;
			memberType = null;
			return false;
		}

		public object GetCustomAttribute (object ob, Type type, bool inherit)
		{
			ArrayList list;
			if (!GetAttributeList (ob, out list))
				return TypeAttributeProvider.Instance.GetCustomAttribute (ob, type, inherit);
			
			if (list == null)
				return null;
			foreach (object att in list)
				if (type.IsInstanceOfType (att))
					return att;
			return null;
		}

		public object[] GetCustomAttributes (object ob, Type type, bool inherit)
		{
			ArrayList list;
			if (!GetAttributeList (ob, out list))
				return TypeAttributeProvider.Instance.GetCustomAttributes (ob, type, inherit);
			
			// Member does not have attributes
			if (list == null)
				return new object [0];
			
			ArrayList res = new ArrayList ();
			foreach (object att in list)
				if (type.IsInstanceOfType (att))
					res.Add (att);
			return res.ToArray ();
		}

		public bool IsDefined (object ob, Type type, bool inherit)
		{
			ArrayList list;
			if (!GetAttributeList (ob, out list))
				return TypeAttributeProvider.Instance.IsDefined (ob, type, inherit);
			
			if (list == null)
				return false;
			foreach (object att in list)
				if (type.IsInstanceOfType (att))
					return true;
			return false;
		}
		
		public ICustomDataItem GetCustomDataItem (object ob)
		{
			foreach (SerializationMap map in maps.Values) {
				if (map.Type.IsInstanceOfType (ob) && map.CustomHandler != null) {
					return new CustomDataItemWrapper (map.CustomHandler, ob);
				}
			}
			return TypeAttributeProvider.Instance.GetCustomDataItem (ob);
		}

		public ItemMember[] GetItemMembers (Type type)
		{
			SerializationMap map;
			if (!maps.TryGetValue (type, out map))
				return new ItemMember [0];
			else
				return map.ExtendedMembers.ToArray ();
		}
		
		bool GetAttributeList (object ob, out ArrayList list)
		{
			list = null;
			if (ob is Type) {
				SerializationMap map;
				if (!maps.TryGetValue ((Type)ob, out map))
					return false;
				list = map.TypeAttributes;
			} else if (ob is MemberInfo) {
				MemberInfo mi = (MemberInfo) ob;
				SerializationMap map;
				if (!maps.TryGetValue (mi.DeclaringType, out map))
					return false;
				if (!map.MemberMap.TryGetValue (mi, out list))
					return true; // Type found, but member not found
			} else if (ob is ItemMember) {
				ItemMember mi = (ItemMember) ob;
				SerializationMap map;
				if (!maps.TryGetValue (mi.DeclaringType, out map))
					return false;
				if (!map.MemberMap.TryGetValue (mi, out list))
					return true; // Type found, but member not found
			}
			return true;
		}
	}
	
	class SerializationMap
	{
		public Type Type;
		public Dictionary<object, ArrayList> MemberMap = new Dictionary<object, ArrayList> ();
		public ArrayList TypeAttributes = new ArrayList ();
		public List<ItemMember> ExtendedMembers = new List<ItemMember> ();
		public ICustomDataItemHandler CustomHandler;
		public string FileId;
		
		public SerializationMap (Type type)
		{
			this.Type = type;
		}
		
		public void AddMemberAttribute (object mi, object attribute)
		{
			ArrayList list;
			if (!MemberMap.TryGetValue (mi, out list)) {
				list = new ArrayList ();
				MemberMap [mi] = list;
			}
			list.Add (attribute);
		}
	}
	
	class CustomDataItemWrapper: ICustomDataItem
	{
		ICustomDataItemHandler itemHandler;
		object ob;
		
		public CustomDataItemWrapper (ICustomDataItemHandler handler, object ob)
		{
			this.itemHandler = handler;
			this.ob = ob;
		}
		
		public DataCollection Serialize (ITypeSerializer handler)
		{
			return itemHandler.Serialize (ob, handler);
		}

		public void Deserialize (ITypeSerializer handler, DataCollection data)
		{
			itemHandler.Deserialize (ob, handler, data);
		}
	}

	class CustomDataItemHandlerChain: ICustomDataItemHandler
	{
		ICustomDataItemHandler mainHandler;
		ICustomDataItemHandler subHandler;
		
		public CustomDataItemHandlerChain (ICustomDataItemHandler mainHandler, ICustomDataItemHandler subHandler)
		{
			this.mainHandler = mainHandler;
			this.subHandler = subHandler;
		}
		
		public DataCollection Serialize (object obj, ITypeSerializer handler)
		{
			return subHandler.Serialize (obj, new ChainedTypeSerializer (mainHandler, handler));
		}
		
		public void Deserialize (object obj, ITypeSerializer handler, DataCollection data)
		{
			subHandler.Deserialize (obj, new ChainedTypeSerializer (mainHandler, handler), data);
		}
	}

	class ChainedTypeSerializer: ITypeSerializer
	{
		ICustomDataItemHandler handler;
		ITypeSerializer serializer;
		
		public ChainedTypeSerializer (ICustomDataItemHandler handler, ITypeSerializer serializer)
		{
			this.handler = handler;
			this.serializer = serializer;
		}
		
		public DataCollection Serialize (object instance)
		{
			return handler.Serialize (instance, serializer);
		}
		
		public void Deserialize (object instance, DataCollection data)
		{
			handler.Deserialize (instance, serializer, data);
		}
		
		public SerializationContext SerializationContext {
			get {
				return serializer.SerializationContext;
			}
		}
	}
}
