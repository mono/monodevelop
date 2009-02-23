// ItemTypeCondition.cs
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
using Mono.Addins;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Extensions
{
	public class ItemTypeCondition: ConditionType
	{
		Type objType;
		List<string> typeNames;
		IDictionary<string,string> aliases;
		
		public ItemTypeCondition (Type objType): this (objType, null)
		{
		}
		
		public ItemTypeCondition (Type objType, IDictionary<string,string> typeNameAliases)
		{
			this.objType = objType;
			if (typeNameAliases != null)
				aliases = typeNameAliases;
			else
				aliases = new Dictionary<string, string> ();
		}
		
		public override bool Evaluate (NodeElement conditionNode)
		{
			foreach (string type in conditionNode.GetAttribute ("value").Split ('|')) {
				if (MatchesType (type))
					return true;
			}
			return false;
		}
		
		public void AddTypeAlias (string alias, string fullName)
		{
			aliases [alias] = fullName;
		}
		
		bool MatchesType (string type)
		{
			if (type.IndexOf ('.') == -1) {
				string res;
				if (aliases.TryGetValue (type, out res))
					type = res;
				else
					type = "MonoDevelop.Projects." + type;
			}
			
			// Type checking might be done by loading the provided type
			// and then comparing the provided and the object types by
			// using IsAssignableFrom. However, type comparison is done
			// here by comparing type names. The advantage is that in
			// this way the provided type doesn't need to be loaded,
			// thus delaying the loading of the add-in that is making
			// use of the condition.
			
			if (typeNames == null) {
				typeNames = new List<string> ();
				
				typeNames.Add (objType.FullName);
				typeNames.Add (objType.AssemblyQualifiedName);
				
	 			// base class hierarchy
	
	 			Type baseType = objType.BaseType;
	 			while (baseType != null) {
					typeNames.Add (baseType.FullName);
					typeNames.Add (baseType.AssemblyQualifiedName);
					baseType = baseType.BaseType;
				}
	
	 			// Implemented interfaces
	
	 			Type[] interfaces = objType.GetInterfaces();
	 			foreach (Type itype in interfaces) {
					typeNames.Add (itype.FullName);
					typeNames.Add (itype.AssemblyQualifiedName);
				}
			}
			return typeNames.Contains (type);
		}
	}
}
