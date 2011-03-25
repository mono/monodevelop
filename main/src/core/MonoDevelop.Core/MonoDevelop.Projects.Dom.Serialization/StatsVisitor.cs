// 
// StatsVisitor.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom
{
	class StatsVisitor: IDomVisitor<string,INode>
	{
		ProjectDomStats stats;
		public IReturnType[] SharedTypes;
		
		public int ReturnTypeCount;
		public List<string> Failures = new List<string> ();
		
		public StatsVisitor (ProjectDomStats stats)
		{
			this.stats = stats;
		}
		
		public void Reset ()
		{
			Failures.Clear ();
			ReturnTypeCount = 0;
		}
		
		public virtual INode Visit (ICompilationUnit unit, string data)
		{
			foreach (IUsing u in unit.Usings)
				u.AcceptVisitor (this, data + "Usings/");
			foreach (IAttribute a in unit.Attributes)
				a.AcceptVisitor (this, data + "Attributes/");
			foreach (IType t in unit.Types)
				t.AcceptVisitor (this, data + "Types/");
			return null;
		}
		
		public virtual INode Visit (IAttribute attribute, string data)
		{
			data += attribute.Name + "/";
			stats.Attributes++;
			if (attribute.AttributeType != null)
				attribute.AttributeType.AcceptVisitor (this, data + "AttributeType/");
			return null;
		}
		
		protected void VisitMember (IMember source, string data)
		{
			if (source.ReturnType != null)
				source.ReturnType.AcceptVisitor (this, data + "RT/");
			foreach (IReturnType rt in source.ExplicitInterfaces)
				rt.AcceptVisitor (this, data + "Interfaces/");
			foreach (IAttribute attr in source.Attributes)
				attr.AcceptVisitor (this, data + "Attributes");
		}
		
		public virtual INode Visit (IType type, string data)
		{
			data += type.FullName + "/";
			VisitMember (type, data);

			foreach (ITypeParameter param in type.TypeParameters)
				Visit (param, data + "TypeParameters/");

			if (type.BaseType != null)
				type.BaseType.AcceptVisitor (this, data + "BaseType/");

			foreach (IReturnType iface in type.ImplementedInterfaces)
				iface.AcceptVisitor (this, data + "ImplementedInterfaces/");

			foreach (IMember member in type.Members)
				member.AcceptVisitor (this, data + "Members/");
			
			return null;
		}
		
		public virtual INode Visit (IField field, string data)
		{
			data += field.Name + "/";
			stats.Fields++;
			VisitMember (field, data);
			return null;
		}
		
		public virtual INode Visit (IMethod source, string data)
		{
			data += source.Name + "()/";
			stats.Methods++;
			VisitMember (source, data);
			
			foreach (ITypeParameter tp in source.TypeParameters)
				Visit (tp, data + "TypeParameters/");
			
			foreach (IParameter parameter in source.Parameters)
				parameter.AcceptVisitor (this, data + "Parameters/");
			
			return null;
		}
		
		public virtual INode Visit (IProperty source, string data)
		{
			data += source.Name + "/";
			stats.Properties++;
			if (source.ReturnType != null)
				source.ReturnType.AcceptVisitor (this, data + "RT/");
			foreach (IReturnType rt in source.ExplicitInterfaces)
				rt.AcceptVisitor (this, data + "ExplicitInterfaces/");
			foreach (IAttribute attr in source.Attributes)
				attr.AcceptVisitor (this, data + "Attributes/");
			
			foreach (IParameter parameter in source.Parameters)
				parameter.AcceptVisitor (this, data + "Parameters/");
			return null;
		}
		
		public virtual INode Visit (IEvent source, string data)
		{
			data += source.Name + "/";
			stats.Events++;
			VisitMember (source, data);
			if (source.AddMethod != null)
				source.AddMethod.AcceptVisitor (this, data + "AddMethod/");
			if (source.RemoveMethod != null)
				source.RemoveMethod.AcceptVisitor (this, data + "RemoveMethod/");
			if (source.RaiseMethod != null)
				source.RaiseMethod.AcceptVisitor (this, data + "RaiseMethod/");
			return null;
		}
		
		protected virtual IReturnTypePart Visit (IReturnTypePart returnTypePart, string data)
		{
			data += returnTypePart.Name + "/";
			stats.ReturnTypeParts++;
			foreach (IReturnType ga in returnTypePart.GenericArguments)
				ga.AcceptVisitor (this, data + "GenericArguments/");
			return null;
		}
		
		public virtual INode Visit (IReturnType type, string data)
		{
			ReturnTypeCount++;
			stats.ReturnTypes++;
			
			foreach (IReturnTypePart p in type.Parts)
				Visit (p, data + "Part/");
			
			foreach (IReturnType rt in SharedTypes) {
				if (object.ReferenceEquals (rt, type))
					return null;
			}

			var sysRt = DomReturnType.GetSharedReturnType (type, true);
			if (sysRt != null) {
				if (object.ReferenceEquals (sysRt, type))
					return null;
			}
			
			Failures.Add (data + type.ToInvariantString ());
			return null;
		}
		
		public virtual INode Visit (IParameter source, string data)
		{
			data += source.Name + "/";
			stats.Parameters++;
			if (source.ReturnType != null)
				source.ReturnType.AcceptVisitor (this, data + "RT/");
			foreach (IAttribute attr in source.Attributes)
				attr.AcceptVisitor (this, data + "Attributes/");
			return null;
		}
		
		public virtual INode Visit (IUsing u, string data)
		{
			data += u.ToString () + "/";
			foreach (KeyValuePair<string, IReturnType> val in u.Aliases)
				val.Value.AcceptVisitor (this, data + "Aliases/");
			return null;
		}
		
		public virtual INode Visit (Namespace namesp, string data)
		{
			data += namesp.Name + "/";
			VisitMember (namesp, data);
			return null;
		}
		
		public virtual INode Visit (LocalVariable var, string data)
		{
			data += var.Name + "/";
			var.ReturnType.AcceptVisitor (this, data);
			return null;
		}
		
		protected virtual ITypeParameter Visit (ITypeParameter type, string data)
		{
			data += type.Name;
			foreach (IAttribute attr in type.Attributes)
				attr.AcceptVisitor (this, data + "Attributes/");
			foreach (IReturnType rt in type.Constraints)
				rt.AcceptVisitor (this, data + "Constraints/");
			return null;
		}	
	}
}
