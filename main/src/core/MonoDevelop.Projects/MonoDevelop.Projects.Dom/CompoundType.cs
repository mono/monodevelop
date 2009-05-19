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
using System.Linq;
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom
{
	public class CompoundType : DomType
	{
		public CompoundType()
		{
		}
		
		public override string ToString ()
		{
			StringBuilder partsString = new StringBuilder ();
			foreach (IType part in parts) {
				if (partsString.Length > 0)
					partsString.Append (", ");
				partsString.Append (part.ToString ());
			}
			return String.Format ("[CompoundType: FullName={0}, #Parts={1}, Parts={2}]", this.FullName, this.parts.Count, partsString);
		}
		
		List<IType> parts = new List<IType> ();
		public override IEnumerable<IType> Parts { 
			get {
				return parts;
			}
		}
		public override MonoDevelop.Projects.Dom.Parser.ProjectDom SourceProjectDom {
			get {
				return parts.Count > 0 ? parts[0].SourceProjectDom : base.SourceProjectDom;
			}
			set {
				if (parts.Count > 0) {
					parts.ForEach (x => x.SourceProjectDom = value);
			 	} else {
					base.SourceProjectDom = value;
				}
			}
		}
		// get the unit & compilation of the suggested main part.
		public override ICompilationUnit CompilationUnit {
			get { return parts.Count > 0 ? parts[0].CompilationUnit : null; }
			set { if (parts.Count > 0) parts[0].CompilationUnit = value; }
		}
		
		public override DomLocation Location {
			get { return parts.Count > 0 ? parts[0].Location : DomLocation.Empty; }
		}
		
		public override DomRegion BodyRegion {
			get { return parts.Count > 0 ? parts[0].BodyRegion : DomRegion.Empty; }
		}
		
		public override bool HasParts {
			get {
				return parts.Count > 0;
			}
		}
		
		public int PartsCount {
			get {
				return parts.Count;
			}
		}
		
		public override IEnumerable<IMember> Members {
			get {
				foreach (IType part in Parts) {
					foreach (IMember member in part.Members) {
						yield return member;
					}
				}
			}
		}
		
		public void SetMainPart (string fileName, DomLocation location) 
		{
			SetMainPart (fileName, location.Line, location.Column);
		}

		public void SetMainPart (FilePath fileName, int line, int column) 
		{
			int idx = parts.FindIndex (1, x => x.CompilationUnit.FileName == fileName && x.Location.Line == line);
			if (idx > 0) {
				IType tmp = parts[0];
				parts[0] = parts[idx];
				parts[idx] = tmp;
			}
		}
		
		public void AddPart (IType part)
		{
			parts.Add (part);
			Update ();
		}

		public IType RemoveFile (FilePath fileName)
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
			else if (parts.Count == 0)
				return null;
			else
				return this;
		}
		
		void Update ()
		{
			if (parts.Count == 0)
				return;
			this.classType = parts[0].ClassType;
			this.Name      = parts[0].Name;
			this.Namespace = parts[0].Namespace;
			if (parts[0] is DomType)
				this.Resolved = ((DomType)parts[0]).Resolved;
			this.ClearTypeParameter ();
			this.AddTypeParameter (parts[0].TypeParameters);
			
			fieldCount = 0;
			methodCount = 0;
			constructorCount = 0;
			indexerCount = 0;
			propertyCount = 0;
			eventCount = 0;
			innerTypeCount = 0;
			
			this.ClearInterfaceImplementations ();
			this.ClearAttributes ();
			Modifiers modifier = Modifiers.None;
			BaseType = null;
			
			foreach (IType part in parts) {
				fieldCount += part.FieldCount;
				methodCount += part.MethodCount;
				constructorCount += part.ConstructorCount;
				indexerCount += part.IndexerCount;
				propertyCount += part.PropertyCount;
				eventCount += part.EventCount;
				innerTypeCount += part.InnerTypeCount;
				modifier |= part.Modifiers;
				this.AddInterfaceImplementations (part.ImplementedInterfaces);
				this.AddRange (part.Attributes);
				if (part.BaseType != null && (BaseType == null || BaseType.FullName == "System.Object"))
					BaseType = part.BaseType;
			}
			
			this.Modifiers = modifier;
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

		public static IType RemoveFile (IType type, FilePath fileName)
		{
			if (type is CompoundType) 
				return ((CompoundType)type).RemoveFile (fileName);

			if (type.CompilationUnit == null || type.CompilationUnit.FileName == fileName)
				return null;
			
			// Class belongs to another file
			return type;
		}
	}
}
