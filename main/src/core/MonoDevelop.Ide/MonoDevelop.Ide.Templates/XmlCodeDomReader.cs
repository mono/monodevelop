//
// XmlCodeDomReader.cs
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
using System.Reflection;
using System.CodeDom;
using System.Collections;
using System.CodeDom.Compiler;

namespace MonoDevelop.Ide.Templates
{
	public class XmlCodeDomReader
	{
		public CodeCompileUnit ReadCompileUnit (XmlElement elem)
		{
			return GenerateElement (elem, null) as CodeCompileUnit;
		}
		
		object GenerateElement (XmlElement elem, Type requiredType)
		{
			Type type = GetElementType (elem.Name);
			object ob;
			
			if (type == typeof(CodeTypeReference))
				ob = new CodeTypeReference (elem.GetAttribute ("BaseType"));
			else
				ob = Activator.CreateInstance (type);
			
			foreach (XmlAttribute att in elem.Attributes) {
				PropertyInfo prop = type.GetProperty (att.Name);
				if (prop == null) {
					if (att.Name.EndsWith ("Type") && type.GetProperty (att.Name.Substring (0, att.Name.Length - 4)) != null)
						continue;
					throw new InvalidOperationException ("Property '" + att.Name + "' not found in type '" + type + "'.");
				}
				
				Type ptype = null;
				string typeatt = elem.GetAttribute (att.Name + "Type");
				if (typeatt != "")
					ptype = Type.GetType (typeatt);
				
				if (ptype == null)
					ptype = prop.PropertyType;
					
				object val = GenerateValue (att.Value, ptype);
				prop.SetValue (ob, val, null);
			}
			
			foreach (XmlNode node in elem.ChildNodes) {
				XmlElement celem = node as XmlElement;
				if (celem == null) continue;
				
				PropertyInfo prop = type.GetProperty (celem.Name);
				if (prop == null)
					throw new InvalidOperationException ("Property '" + celem.Name + "' not found in type '" + type + "'.");

				if (typeof(IEnumerable).IsAssignableFrom (prop.PropertyType)) {
					object col = prop.GetValue (ob, null);
					object val = GenerateCollection (celem, prop.PropertyType, col);
					if (col == null)
						prop.SetValue (ob, val, null);
				} else {
					XmlElement cobElem = celem.SelectSingleNode ("*") as XmlElement;
					if (cobElem != null) {
						object val = GenerateElement (cobElem, prop.PropertyType);
						prop.SetValue (ob, val, null);
					}
				}
			}

			return ob;
		}
		
		object GenerateCollection (XmlElement elem, Type type, object col)
		{
			if (col == null)
				col = Activator.CreateInstance (type);
			
			ArrayList methods = new ArrayList ();
			ArrayList types = new ArrayList ();
			
			MethodInfo[] mets = type.GetMethods ();
			foreach (MethodInfo met in mets) {
				if (met.Name != "Add")
					continue;
				ParameterInfo[] pi = met.GetParameters ();
				if (pi.Length != 1)
					continue;
				types.Add (pi[0].ParameterType);
				methods.Add (met);
			}
			
			if (methods.Count == 0)
				throw new InvalidOperationException ("Add method not found in " + type);
			
			foreach (XmlNode node in elem.ChildNodes) {
				XmlElement celem = node as XmlElement;
				if (celem == null) continue;
				
				object val = GenerateElement (celem, null);
				
				for (int n=0; n < types.Count; n++) {
					Type t = (Type) types [n];
					if (t.IsAssignableFrom (val.GetType ())) {
						MethodInfo met = (MethodInfo) methods [n];
						met.Invoke (col, new object [] { val });
						break;
					}
				}
			}
			return col;
		}
		
		object GenerateValue (string xml, Type type)
		{
			if (type == typeof(CodeTypeReference))
				return new CodeTypeReference (xml);
			if (type.IsEnum)
				return Enum.Parse (type, xml);
			return Convert.ChangeType (xml, type);
		}
		
		public Type GetElementType (string elemName)
		{
			Type type = typeof(CodeObject).Assembly.GetType ("System.CodeDom.Code" + elemName);
			if (type == null)
				throw new InvalidOperationException ("Type not found for element: " + elemName);
			return type;
		}
	}
}
