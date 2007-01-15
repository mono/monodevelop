// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1965 $</version>
// </file>

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
			if (current is CompoundClass) {
				// It's already a compound class. Add the new one.
				CompoundClass ccls = (CompoundClass) current;
				ccls.AddClass (cls);
				ccls.UpdateInformationFromParts ();
				return ccls;
			}
			else if (current.Region != null && cls.Region != null && current.Region.FileName != cls.Region.FileName) {
				// Classes from different files. Merge them.
				return new CompoundClass (current, cls);
			}
			else
				return cls;
		}
		
		public static IClass RemoveFile (IClass cls, string fileName)
		{
			CompoundClass ccls = cls as CompoundClass;
			if (ccls != null)
				return ccls.RemoveFile (fileName);
			
			if (cls.Region.FileName == fileName)
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

			if (FullyQualifiedName == null)
				FullyQualifiedName = parts[0].FullyQualifiedName;
			
			ModifierEnum modifier = ModifierEnum.None;
			const ModifierEnum defaultClassVisibility = ModifierEnum.Internal;
			BaseTypes.Clear();
			genericParamters = null;
			Attributes.Clear();
			
			foreach (IClass part in parts) {
				if ((part.Modifiers & ModifierEnum.VisibilityMask) != defaultClassVisibility) {
					modifier |= part.Modifiers;
				} else {
					modifier |= part.Modifiers &~ ModifierEnum.VisibilityMask;
				}
				foreach (IReturnType rt in part.BaseTypes) {
					if (!BaseTypes.Contains (rt)) {
						this.BaseTypes.Add(rt);
					}
				}
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
				FieldCollection l = new FieldCollection();
				foreach (IClass part in parts) {
					l.AddRange(part.Fields);
				}
				return l;
			}
		}
		
		public override PropertyCollection Properties {
			get {
				PropertyCollection l = new PropertyCollection();
				foreach (IClass part in parts) {
					l.AddRange(part.Properties);
				}
				return l;
			}
		}
		
		public override MethodCollection Methods {
			get {
				MethodCollection l = new MethodCollection();
				foreach (IClass part in parts) {
					l.AddRange(part.Methods);
				}
				return l;
			}
		}
		
		public override EventCollection Events {
			get {
				EventCollection l = new EventCollection();
				foreach (IClass part in parts) {
					l.AddRange(part.Events);
				}
				return l;
			}
		}
	}
}
