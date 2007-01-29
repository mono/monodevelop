// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using MonoDevelop.Core;

using System;
using System.Collections;
using System.Xml;
using System.Collections.Specialized;
using Mono.Cecil;
using MDGenericParameter = MonoDevelop.Projects.Parser.GenericParameter;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class ReflectionClass : DefaultClass
	{
		public ReflectionClass (): base (null)
		{
		}
		
		public static bool IsDelegate (TypeReference type)
		{
			if (type.FullName == "System.MulticastDelegate" || type.FullName == "System.Delegate")
				return false;

			while (type != null) {
				if (type.FullName == "System.Delegate")
					return true;
				TypeDefinition td = type as TypeDefinition;
				if (td != null)
					type = td.BaseType;
				else
					break;
			}
			return false;
		}
		
		public ReflectionClass (TypeDefinition type): base (null)
		{
			string fqname = type.FullName.Replace ('/','+');
			Name = type.Name.Replace ('/','.');
			Name = Name.Replace ('+', '.');
			Namespace = type.Namespace;

			XmlDocument docs = Services.DocumentationService != null ? Services.DocumentationService.GetHelpXml (fqname) : null;
			if (docs != null) {
				XmlNode node = docs.SelectSingleNode ("/Type/Docs/summary");
				if (node != null) {
					Documentation = node.InnerXml;
				}
			}
			
			// set classtype
			if (IsDelegate(type)) {
				classType = ClassType.Delegate;
				MethodDefinition invoke = type.Methods.GetMethod ("Invoke")[0];
				ReflectionMethod newMethod = new ReflectionMethod (invoke, null);
				methods.Add(newMethod);
			} else if (type.IsInterface) {
				classType = ClassType.Interface;
			} else if (type.IsEnum) {
				classType = ClassType.Enum;
			} else if (type.IsValueType) {
				classType = ClassType.Struct;
			} else {
				classType = ClassType.Class;
			}
			
			modifiers = ModifierEnum.None;
			
			if (type.IsSealed) {
				modifiers |= ModifierEnum.Sealed;
			}
			if (type.IsAbstract) {
				modifiers |= ModifierEnum.Abstract;
			}
			
			modifiers |= GetModifiers (type.Attributes);
			
			// Add generic parameters to the type
			if (type.GenericParameters != null && type.GenericParameters.Count > 0) {
				this.GenericParameters = new GenericParameterList();
				
				foreach (Mono.Cecil.GenericParameter par in type.GenericParameters) {
					// Fill out the type constraints for generic parameters 
					ReturnTypeList rtl = null;
					if (par.Constraints != null && par.Constraints.Count > 0) {
						rtl = new ReturnTypeList();
						foreach (Mono.Cecil.TypeReference typeRef in par.Constraints) {
							rtl.Add(new ReflectionReturnType(typeRef));
						}
					}
					
					// Add the parameter to the generic parameter list
					this.GenericParameters.Add(new MDGenericParameter(par.Name, rtl, (System.Reflection.GenericParameterAttributes)par.Attributes));
				}
			}
			
			// set base classes
			if (type.BaseType != null) { // it's null for System.Object ONLY !!!
				baseTypes.Add(new ReflectionReturnType(type.BaseType));
			}
			
			if (classType != ClassType.Delegate) {
				// add members
				foreach (TypeReference iface in type.Interfaces) {
					baseTypes.Add(new ReflectionReturnType(iface));
				}
				
				foreach (TypeDefinition nestedType in type.NestedTypes) {
					TypeAttributes vis = nestedType.Attributes & TypeAttributes.VisibilityMask;
					if (vis == TypeAttributes.Public || vis == TypeAttributes.NestedPublic)
						innerClasses.Add (new ReflectionClass(nestedType));
				}
				
				foreach (FieldDefinition field in type.Fields) {
//					if (!field.IsSpecialName) {
					IField newField = new ReflectionField (field, docs);
					if (!newField.IsInternal) {
						fields.Add(newField);
					}
//					}
				}
				
				foreach (PropertyDefinition propertyInfo in type.Properties) {
//					if (!propertyInfo.IsSpecialName) {
					ParameterDefinitionCollection p = propertyInfo.Parameters;
					
					if (p == null || p.Count == 0) {
						IProperty newProperty = new ReflectionProperty (propertyInfo, docs);
						if (!newProperty.IsInternal) {
							properties.Add(newProperty);
						}
					} else {
						IIndexer newIndexer = new ReflectionIndexer (propertyInfo, docs);
						if (!newIndexer.IsInternal) {
							indexer.Add(newIndexer);
						}
					}
//					}
				}
				
				foreach (MethodDefinition methodInfo in type.Methods) {
					if (!methodInfo.IsSpecialName) {
						IMethod newMethod = new ReflectionMethod (methodInfo, docs);
						
						if (!newMethod.IsInternal) {
							methods.Add(newMethod);
						}
					}
				}
				
				foreach (MethodDefinition constructorInfo in type.Constructors) {
					IMethod newMethod = new ReflectionMethod (constructorInfo, docs);
					if (!newMethod.IsInternal) {
						methods.Add(newMethod);
					}
				}
				
				foreach (EventDefinition eventInfo in type.Events) {
//					if (!eventInfo.IsSpecialName) {
					IEvent newEvent = new ReflectionEvent (eventInfo, docs);
					
					if (!newEvent.IsInternal) {
						events.Add(newEvent);
					}
//					}
				}
			}
		}
		
		public static ModifierEnum GetModifiers (TypeAttributes attributes)
		{
			TypeAttributes visibility = attributes & TypeAttributes.VisibilityMask;
			
			if (visibility == TypeAttributes.NestedPrivate) { // I assume that private is used most and public last (at least should be)
				return ModifierEnum.Private;
			} else if (visibility == TypeAttributes.NestedFamily) {
				return ModifierEnum.Protected;
			} else if (visibility == TypeAttributes.NestedPublic || visibility == TypeAttributes.Public) {
				return ModifierEnum.Public;
			} else if (visibility == TypeAttributes.NestedAssembly) {
				return ModifierEnum.Internal;
			} else if (visibility == TypeAttributes.NotPublic) {
				return ModifierEnum.Internal;
			} else if (visibility == TypeAttributes.NestedFamORAssem) {
				return ModifierEnum.ProtectedOrInternal;
			} else if (visibility == TypeAttributes.NestedFamANDAssem) {
				return ModifierEnum.Protected | ModifierEnum.Internal;
			}
			return ModifierEnum.None;
		}
	}
}
