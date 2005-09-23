// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;
using ICSharpCode.AssemblyAnalyser.Rules;

namespace ICSharpCode.AssemblyAnalyser
{
	/// <summary>
	/// Description of AssemblyAnalyser.	
	/// </summary>
	public class AssemblyAnalyser : System.MarshalByRefObject
	{
		ArrayList assemblyRules    = new ArrayList();
		ArrayList moduleRules      = new ArrayList();
		ArrayList typeRules        = new ArrayList();
		ArrayList namespaceRules   = new ArrayList();
		ArrayList memberRules      = new ArrayList();
		ArrayList methodBaseRules  = new ArrayList();
		ArrayList constructorRules = new ArrayList();
		ArrayList eventRules       = new ArrayList();
		ArrayList fieldRules       = new ArrayList();
		ArrayList methodRules      = new ArrayList();
		ArrayList parameterRules   = new ArrayList();
		ArrayList propertyRules    = new ArrayList();
		
		ArrayList resolutions      = new ArrayList();
		
		Hashtable namespaces       = new Hashtable();
		
		public ArrayList Resolutions {
			get {
				return resolutions;
			}
		}
		
		public AssemblyAnalyser()
		{
			
			Type[] types = typeof(AssemblyAnalyser).Assembly.GetTypes();
			foreach (Type type in types) {
				if (!type.IsAbstract && type.IsClass) {
					if (type.GetInterface(typeof(IAssemblyRule).FullName) != null) {
 						assemblyRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(IConstructorRule).FullName) != null) {
						constructorRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(IEventRule).FullName) != null) {
						eventRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(IFieldRule).FullName) != null) {
						fieldRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(IMemberRule).FullName) != null) {
						memberRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(IMethodBaseRule).FullName) != null) {
						methodBaseRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(IMethodRule).FullName) != null) {
						methodRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(IModuleRule).FullName) != null) {
						moduleRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(INamespaceRule).FullName) != null) {
						namespaceRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(IParameterRule).FullName) != null) {
						parameterRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(IPropertyRule).FullName) != null) {
						propertyRules.Add(type.Assembly.CreateInstance(type.FullName));
					} else if (type.GetInterface(typeof(ITypeRule).FullName) != null) {
						typeRules.Add(type.Assembly.CreateInstance(type.FullName));
					}
				}
			}
		}
		
		void AddResolutions(Resolution resolution)
		{
			if (resolution != null) {
				resolutions.Add(resolution);
			}
		}
		
		public void Analyse(string fileName)
		{
			Assembly assembly = null;
			try {
				assembly = Assembly.LoadFrom(fileName);
			} catch (Exception e) {
				// FIXME: I18N
				resolutions.Add (new Resolution (new CustomRule ("Assembly cannot be loaded", "When an assembly cannot be loaded it indicates that the format is corrupt. This means that the .dll or .exe might not be in a managed .NET format or that the file was altered (in this case recompiling might help).", PriorityLevel.CriticalError, 100), String.Format ("Recompile {0}. Exception was: {1}", fileName, e.Message), fileName));
				return;
			}
			Analyse(assembly);
		}
		
		public void Analyse(Module module, Type type)
		{
			if (type.IsSpecialName || !(Char.IsLetter(type.Name[0]) || type.Name[0] == '_')) {
				return;
			}
			string ns = type.Namespace == null ? "" : type.Namespace;
			if (namespaces[ns] == null) {
				namespaces[ns] = new ArrayList();
			}
			((ArrayList)namespaces[ns]).Add(type);
			
			foreach (ITypeRule typeRule in typeRules) {
				AddResolutions(typeRule.Check(type));
			}
			
			foreach (Type nestedType in type.GetNestedTypes()) {
				Analyse(module, nestedType);
			}
			BindingFlags bf = BindingFlags.DeclaredOnly |
			                  BindingFlags.Public |
			                  BindingFlags.NonPublic |
			                  BindingFlags.Static |
			                  BindingFlags.Instance;
			
			
			foreach (MemberInfo memberInfo in type.GetMembers(bf)) {
				foreach (IMemberRule memberRule in memberRules) {
					AddResolutions(memberRule.Check(module, memberInfo));
				}
			}
			
			foreach (ConstructorInfo constructorInfo in type.GetConstructors(bf)) {
				if (!constructorInfo.IsSpecialName) {
					// search parameters
					foreach (ParameterInfo parameter in constructorInfo.GetParameters()) {
						foreach (IParameterRule parameterRule in parameterRules) {
							AddResolutions(parameterRule.Check(module, parameter));
						}
					}
					
					foreach (IConstructorRule constructorRule in constructorRules) {
						AddResolutions(constructorRule.Check(constructorInfo));
					}
				}
			}
			
			foreach (EventInfo eventInfo in type.GetEvents(bf)) {
				if (!eventInfo.IsSpecialName) {
					foreach (IEventRule eventRule in eventRules) {
						AddResolutions(eventRule.Check(eventInfo));
					}
				}
			}
			
			foreach (FieldInfo fieldInfo in type.GetFields(bf)) {
				if (!fieldInfo.IsSpecialName) {
					foreach (IFieldRule fieldRule in fieldRules) {
						AddResolutions(fieldRule.Check(module, fieldInfo));
					}
				}
			}
			
			// TODO: IMethodBaseRule
			foreach (MethodInfo methodInfo in type.GetMethods(bf)) {
				if (!methodInfo.IsSpecialName) {
					//Console.WriteLine(methodInfo.Attributes);
				
					// search parameters
					foreach (ParameterInfo parameter in methodInfo.GetParameters()) {
						foreach (IParameterRule parameterRule in parameterRules) {
							AddResolutions(parameterRule.Check(module, parameter));
						}
					}
					
					foreach (IMethodRule methodRule in methodRules) {
						AddResolutions(methodRule.Check(module, methodInfo));
					}
				}
			}
			
			foreach (PropertyInfo propertyInfo in type.GetProperties(bf)) {
				if (!propertyInfo.IsSpecialName) {
					foreach (IPropertyRule propertyRule in propertyRules) {
						AddResolutions(propertyRule.Check(propertyInfo));
					}
				}
			}
		}
		public void Analyse(Assembly assembly)
		{
			namespaces = new Hashtable();
			resolutions = new ArrayList();
			foreach (IAssemblyRule assemblyRule in assemblyRules) {
				AddResolutions(assemblyRule.Check(assembly));
			}
			
			foreach (Module module in assembly.GetModules()) {
				foreach (IModuleRule moduleRule in moduleRules) {
					AddResolutions(moduleRule.Check(module));
				}
				foreach (Type type in module.GetTypes()) {
					Analyse(module, type);
				}
			}
			
			foreach (DictionaryEntry namespaceEntry in namespaces) {
				foreach (INamespaceRule namespaceRule in namespaceRules) {
					AddResolutions(namespaceRule.Check(namespaceEntry.Key.ToString(), (ArrayList)namespaceEntry.Value));
				}
			}
		}
	}
}
