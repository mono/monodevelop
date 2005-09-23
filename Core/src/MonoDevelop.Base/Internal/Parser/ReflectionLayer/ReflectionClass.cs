// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using MonoDevelop.Services;
using MonoDevelop.Core.Services;

using System;
using System.Collections;
using System.Xml;
using System.Reflection;
using System.Collections.Specialized;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public class ReflectionClass : AbstractClass
	{
		BindingFlags flags = BindingFlags.Instance  | 
		                     BindingFlags.Static    | 
//		                     BindingFlags.DeclaredOnly |
		                     BindingFlags.NonPublic |
		                     BindingFlags.Public;
		
		/// <value>
		/// A reflection class doesn't have a compilation unit (because
		/// it is not parsed the information is gathered using reflection)
		/// </value>
		public override ICompilationUnit CompilationUnit {
			get {
				return null;
			}
		}
		
		public static bool IsDelegate(Type type)
		{
			return type.IsSubclassOf(typeof(Delegate)) && type != typeof(MulticastDelegate);
		}
		
		public ReflectionClass(Type type)
		{

			if (type == null)
				type = Type.GetType ("System.Object");
			

			FullyQualifiedName = type.FullName;

			XmlDocument docs = Runtime.Documentation != null ? Runtime.Documentation.GetHelpXml (FullyQualifiedName) : null;
			if (docs != null) {
				XmlNode node = docs.SelectSingleNode ("/Type/Docs/summary");
				if (node != null) {
					Documentation = node.InnerXml;
				}
			}
			
			FullyQualifiedName = FullyQualifiedName.Replace("+", ".");
			
			// set classtype
			if (IsDelegate(type)) {
				classType = ClassType.Delegate;
				MethodInfo invoke          = type.GetMethod("Invoke");
				ReflectionMethod newMethod = new ReflectionMethod(invoke, null);
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
			
			if (type.IsNestedAssembly) {
				modifiers |= ModifierEnum.Internal;
			}
			
			if (type.IsSealed) {
				modifiers |= ModifierEnum.Sealed;
			}
			if (type.IsAbstract) {
				modifiers |= ModifierEnum.Abstract;
			}
			
			if (type.IsNestedPrivate ) { // I assume that private is used most and public last (at least should be)
				modifiers |= ModifierEnum.Private;
			} else if (type.IsNestedFamily ) {
				modifiers |= ModifierEnum.Protected;
			} else if (type.IsNestedPublic || type.IsPublic) {
				modifiers |= ModifierEnum.Public;
			} else if (type.IsNotPublic) {
				modifiers |= ModifierEnum.Internal;
			} else if (type.IsNestedFamORAssem) {
				modifiers |= ModifierEnum.ProtectedOrInternal;
			} else if (type.IsNestedFamANDAssem) {
				modifiers |= ModifierEnum.Protected;
				modifiers |= ModifierEnum.Internal;
			}
			
			// set base classes
			if (type.BaseType != null) { // it's null for System.Object ONLY !!!
				baseTypes.Add(type.BaseType.FullName);
			}
			
			if (classType != ClassType.Delegate) {
				// add members
				foreach (Type iface in type.GetInterfaces()) {
					baseTypes.Add(iface.FullName);
				}
				
				foreach (Type nestedType in type.GetNestedTypes(flags)) {
					innerClasses.Add(new ReflectionClass(nestedType));
				}
				
				foreach (FieldInfo field in type.GetFields(flags)) {
//					if (!field.IsSpecialName) {
					IField newField = new ReflectionField(field, docs);
					if (!newField.IsInternal) {
						fields.Add(newField);
					}
//					}
				}
				
				foreach (PropertyInfo propertyInfo in type.GetProperties(flags)) {
//					if (!propertyInfo.IsSpecialName) {
					ParameterInfo[] p = null;
					
					// we may not get the permission to access the index parameters
					try {
						p = propertyInfo.GetIndexParameters();
					} catch (Exception) {}
					if (p == null || p.Length == 0) {
						IProperty newProperty = new ReflectionProperty(propertyInfo, docs);
						if (!newProperty.IsInternal) {
							properties.Add(newProperty);
						}
					} else {
						IIndexer newIndexer = new ReflectionIndexer(propertyInfo, docs);
						if (!newIndexer.IsInternal) {
							indexer.Add(newIndexer);
						}
					}
//					}
				}
				
				foreach (MethodInfo methodInfo in type.GetMethods(flags)) {
					if (!methodInfo.IsSpecialName) {
						IMethod newMethod = new ReflectionMethod(methodInfo, docs);
						
						if (!newMethod.IsInternal) {
							methods.Add(newMethod);
						}
					}
				}
				
				foreach (ConstructorInfo constructorInfo in type.GetConstructors(flags)) {
					IMethod newMethod = new ReflectionMethod(constructorInfo, docs);
					if (!newMethod.IsInternal) {
						methods.Add(newMethod);
					}
				}
				
				foreach (EventInfo eventInfo in type.GetEvents(flags)) {
//					if (!eventInfo.IsSpecialName) {
					IEvent newEvent = new ReflectionEvent(eventInfo, docs);
					
					if (!newEvent.IsInternal) {
						events.Add(newEvent);
					}
//					}
				}
			}
		}
	}
}
