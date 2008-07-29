//  CompoundClass.cs
//
//  This file was derived from a file from #Develop 2.0 
//
//  Copyright (C) Daniel Grunwald <daniel@danielgrunwald.de>
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 

using System;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Parser
{
	/// <summary>
	/// A class made up of multiple partial classes.
	/// </summary>
	sealed class CompoundClass : DefaultClass
	{
		List<IClass> parts = new List<IClass>();
		
		/// <summary>
		/// Gets the parts this class is based on.
		/// </summary>
		public override IClass[] Parts {
			get {
				return parts.ToArray ();
			}
		}
		
		/// <summary>
		/// Creates a new CompoundClass with the specified class as first part.
		/// </summary>
		public CompoundClass ()
		{
		}
		
		public CompoundClass (IClass firstPart, IClass secondPart)
		{
			parts.Add (firstPart);
			parts.Add (secondPart);
			UpdateInformationFromParts();
		}
		
		public override ICompilationUnit CompilationUnit {
			get {
				IClass cls = GetMainClass ();
				return cls.CompilationUnit;
			}
		}
		
		IClass GetMainClass ()
		{
			// Try to guess the main class of this compound class.
			// If there is one class declared in a file with the same name, that's the class

			foreach (IClass cls in parts) {
				if (cls.Name == System.IO.Path.GetFileNameWithoutExtension (cls.Region.FileName))
					return cls;
			}
			
			// If there isn't such class, return the one that has constructors
			foreach (IClass cls in parts) {
				foreach (IMethod met in cls.Methods) {
					if (met.IsConstructor)
						return cls;
				}
			}
			
			// Just return the first one
			return parts [0];
		}
		
		public static IClass MergeClass (IClass current, IClass cls)
		{
			if (current is CompoundClass && cls.Region != null) {
				// It's already a compound class. Add the new one.
				CompoundClass ccls = (CompoundClass) current;
				ccls.AddClass (cls);
				ccls.UpdateInformationFromParts ();
				return ccls;
			}
			else if (current.Region != null && cls.Region != null && current.Region.FileName != cls.Region.FileName) {
				// Classes from different files. Merge them.
				CompoundClass cc = new CompoundClass (current, cls);
				cc.SourceProject = current.SourceProject;
				return cc;
			}
			else
				return cls;
		}
		
		public static IClass RemoveFile (IClass cls, string fileName)
		{
			CompoundClass ccls = cls as CompoundClass;
			if (ccls != null)
				return ccls.RemoveFile (fileName);
			
			if (cls.Region == null || cls.Region.FileName == fileName)
				return null;

			// Class belongs to another file
			return cls;
		}

		// UpdateInformationFromParts must be called after adding a class
		public void AddClass (IClass cls)
		{
			for (int n=0; n<parts.Count; n++) {
				if (parts [n].Region.FileName == cls.Region.FileName) {
					parts [n] = cls;
					return;
				}
			}
			parts.Add (cls);
		}
		
		public IClass RemoveFile (string fileName)
		{
			for (int n=0; n<parts.Count; n++) {
				if (parts [n].Region.FileName == fileName) {
					if (parts.Count == 2) {
						// Not a compound class anymore
						if (n == 0) return parts[1];
						else return parts [0];
					}
					parts.RemoveAt (n);
					UpdateInformationFromParts ();
					return this;
				}
			}
			return this;
		}
		
		/// <summary>
		/// Re-calculate information from class parts (Modifier, Base classes, Type parameters etc.)
		/// </summary>
		public void UpdateInformationFromParts()
		{
			// Common for all parts:
			this.classType = parts[0].ClassType;
			this.region = GetMainClass ().Region;

			Name = parts[0].Name;
			Namespace = parts[0].Namespace;
			
			ModifierEnum modifier = ModifierEnum.None;
			const ModifierEnum defaultClassVisibility = ModifierEnum.Internal;
			BaseTypes.Clear();
			genericParamters = null;
			Attributes.Clear();
			
			bool nonRootBaseClassFound = false;
			IReturnType rootType = null;
			
			foreach (IClass part in parts) {
				
				if ((part.Modifiers & ModifierEnum.VisibilityMask) != defaultClassVisibility) {
					modifier |= part.Modifiers;
				} else {
					modifier |= part.Modifiers &~ ModifierEnum.VisibilityMask;
				}
				
				bool rootClassFound = false;
				foreach (IReturnType rt in part.BaseTypes) {
					if (rt.IsRootType) {
						rootClassFound = true;
						rootType = rt;
						continue;
					}
					if (!BaseTypes.Contains (rt)) {
						this.BaseTypes.Add(rt);
					}
				}
				if (!rootClassFound)
					nonRootBaseClassFound = true;
				
				if (part.GenericParameters != null) {
					if (genericParamters == null)
						genericParamters = new GenericParameterList ();
					foreach (GenericParameter typeParam in part.GenericParameters) {
						genericParamters.Add(typeParam);
					}
				}
				foreach (IAttributeSection attribute in part.Attributes) {
					this.Attributes.Add(attribute);
				}
			}
			
			if (!nonRootBaseClassFound && rootType != null) {
				BaseTypes.Add (rootType);
			}
			
			if ((modifier & ModifierEnum.VisibilityMask) == ModifierEnum.None) {
				modifier |= defaultClassVisibility;
			}
			this.modifiers = modifier;
		}
		
		public override ClassCollection InnerClasses {
			get {
				ClassCollection l = new ClassCollection();
				foreach (IClass part in parts) {
					l.AddRange(part.InnerClasses);
				}
				return l;
			}
		}
		
		public override FieldCollection Fields {
			get {
				FieldCollection l = new FieldCollection (this);
				foreach (IClass part in parts) {
					l.AddRange(part.Fields);
				}
				return l;
			}
		}
		
		public override PropertyCollection Properties {
			get {
				PropertyCollection l = new PropertyCollection (this);
				foreach (IClass part in parts) {
					l.AddRange(part.Properties);
				}
				return l;
			}
		}
		
		public override MethodCollection Methods {
			get {
				MethodCollection l = new MethodCollection (this);
				foreach (IClass part in parts) {
					l.AddRange(part.Methods);
				}
				return l;
			}
		}
		
		public override EventCollection Events {
			get {
				EventCollection l = new EventCollection (this);
				foreach (IClass part in parts) {
					l.AddRange(part.Events);
				}
				return l;
			}
		}
	}
}
