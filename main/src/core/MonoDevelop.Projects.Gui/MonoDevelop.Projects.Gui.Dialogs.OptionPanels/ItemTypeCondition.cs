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

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	class ItemTypeCondition: ConditionType
	{
		Type objType;
		List<string> typeNames;
		
		public ItemTypeCondition (Type objType)
		{
			this.objType = objType;
		}
		
		public override bool Evaluate (NodeElement conditionNode)
		{
			string type = conditionNode.GetAttribute ("value");
			if (type.IndexOf ('.') == -1)
				type = "MonoDevelop.Projects." + type;
			
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
			foreach (string ss in typeNames)
				Console.WriteLine ("pptn: " + ss);
			Console.WriteLine ("pp ccc: " + type);
			return typeNames.Contains (type);
		}
	}
}
