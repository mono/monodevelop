//
// CompoundType.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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

namespace MonoDevelop.Projects.Dom
{
	public class CompoundType : DomType
	{
		public CompoundType()
		{
		}
		
		public void AddPart (IType part)
		{
			this.parts.Add (part);
			Update ();
		}
		
		public IType RemoveFile (string fileName)
		{
			for (int i = 0; i < this.parts.Count; i++) {
				if (parts [i].CompilationUnit != null && parts [i].CompilationUnit.FileName == fileName) {
					parts.RemoveAt (i);
					i--;
					Update ();
					continue;
				}
			}
			if (parts.Count == 1)
				return parts[0];
			return this;
		}
		
		void Update ()
		{
			if (parts.Count == 0)
				return;
			this.classType = parts[0].ClassType;
			this.Name      = parts[0].Name;
			this.Namespace = parts[0].Namespace;
			this.typeParameters.Clear ();
			this.typeParameters.AddRange (parts[0].TypeParameters);
			
			this.implementedInterfaces.Clear ();
			this.attributes.Clear ();
			Modifiers modifier = Modifiers.None;
			foreach (IType part in parts) {
				modifier |= part.Modifiers;
				this.implementedInterfaces.AddRange (part.ImplementedInterfaces);
				this.attributes.AddRange (part.Attributes);
			}
			this.modifiers = modifier;
		}
		
		public static IType Merge (IType type1, IType type2)
		{
			if (type1 is CompoundType) {
				((CompoundType)type1).AddPart (type2);
				return type1;
			}
			CompoundType result = new CompoundType ();
			result.AddPart (type1);
			result.AddPart (type2);
			return result;
		}
		
		public static IType RemoveFile (IType type, string fileName)
		{
			if (type is CompoundType) 
				return ((CompoundType)type).RemoveFile (fileName);
			
			return type;
		}
		
		
		
	}
}
