// 
// CodeMetricsServices.cs
//  
// Author:
//       nikhil <${AuthorEmail}>
// 
// Copyright (c) 2009 nikhil
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

using System;
using System.Collections.Generic;
using System.Text;
using Gtk;

using MonoDevelop.Core;
 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;

namespace MonoDevelop.CodeMetrics
{
	public class CodeMetricsService
	{
		public static void AddTypes (ProjectProperties projectprop, MetricsContext ctx)
		{
			var dom = TypeSystemService.GetCompilation (projectprop.Project);
			foreach (var ob in dom.MainAssembly.GetAllTypeDefinitions ()) {
				projectprop.AddInstance(ob);
			}
		}
		
		internal static void ProcessInnerTypes(ProjectProperties projprop)
		{
			foreach (var namesp in projprop.Namespaces) {
				namesp.Value.ProcessClasses();
				projprop.CyclometricComplexity += namesp.Value.CyclometricComplexity;
				projprop.LOCReal += namesp.Value.LOCReal;
				projprop.LOCComments += namesp.Value.LOCComments;
			}
			foreach (var cls in projprop.Classes) {
				cls.Value.ProcessInnerClasses();
				projprop.CyclometricComplexity += cls.Value.CyclometricComplexity;
				projprop.LOCReal += cls.Value.LOCReal;
				projprop.LOCComments += cls.Value.LOCComments;
			}	
		}
		
		public static string GenerateAssemblyMetricText()
		{
			// TODO General stuff about the assembly
			return "";
		}
		
		public static string GenerateTypeMetricText(IProperties item)
		{
			if(item is NamespaceProperties){
				
				return GenerateNamespaceMetricText((NamespaceProperties)item).ToString();
				
			} else if (item is MethodProperties) {
				
				return GenerateMethodMetricText((MethodProperties)item).ToString();
				
			} else if (item is ClassProperties) {
				
				return GenerateClassMetricText((ClassProperties)item).ToString();
				
			} else if (item is InterfaceProperties) {
				
				return GenerateInterfaceMetricText((InterfaceProperties)item).ToString();
				
			} else if (item is EnumProperties) {
				
				return GenerateEnumMetricText((EnumProperties)item).ToString();
				
			} else if (item is DelegateProperties) {
				
				return GenerateDelegateMetricText((DelegateProperties)item).ToString();
				
			} else if (item is StructProperties) {
				
				return GenerateStructMetricText((StructProperties)item).ToString();
			
			}
			
			return "NULL";
		}
		
		private static StringBuilder GenerateNamespaceMetricText(NamespaceProperties item)
		{
			StringBuilder results = new StringBuilder();
			results.Append(GettextCatalog.GetString("\nName : ") + item.FullName);
			results.Append(GettextCatalog.GetString("\nTotal number of classes : ") + item.Classes.Count);
			results.Append(GettextCatalog.GetString("\nTotal number of methods : ") + item.MethodCount);
			results.Append(GettextCatalog.GetString("\nTotal number of fields : ") + item.FieldCount);
			results.Append(GettextCatalog.GetString("\nClass Coupling : ") + item.ClassCoupling);
			return results;
		}
		
		private static StringBuilder GenerateClassMetricText(ClassProperties item)
		{
			StringBuilder results = new StringBuilder();
			results.Append(GettextCatalog.GetString("\nName : ") + item.FullName);
			results.Append(GettextCatalog.GetString("\nDepth of inheritance : ") + item.DepthOfInheritance);
			results.Append(GettextCatalog.GetString("\nNumber of children : ") + item.FanOut);
			results.Append(GettextCatalog.GetString("\nAfferent Coupling : ") + item.AfferentCoupling);
			results.Append(GettextCatalog.GetString("\nEfferent Coupling : ") + item.EfferentCoupling);
			results.Append(GettextCatalog.GetString("\nData abstraction coupling : ") + item.DataAbstractionCoupling);
			results.Append(GettextCatalog.GetString("\nConstructors : ") + item.ConstructorCount);
			results.Append(GettextCatalog.GetString("\nDelegates : ") + item.DelegateCount);
			results.Append(GettextCatalog.GetString("\nEvents : ") + item.EventCount);
			results.Append(GettextCatalog.GetString("\nFields : ") + item.FieldCount);
			results.Append(GettextCatalog.GetString("\nInner classes : ") + item.InnerClassCount);
			results.Append(GettextCatalog.GetString("\nStructs : ") + item.StructCount);
			results.Append(GettextCatalog.GetString("\nMethods : ") + item.MethodCount);
			results.Append(GettextCatalog.GetString("\nProperties : ") + item.Class.Properties.Count ());
			results.Append(GettextCatalog.GetString("\nLack of cohesion of methods : ") + item.LCOM);
			results.Append(GettextCatalog.GetString("\nLack of cohesion of methods (Henderson-Sellers) : ") + item.LCOM_HS);
			return results;
		}
		
		private static StringBuilder GenerateMethodMetricText(MethodProperties item)
		{
			StringBuilder results = new StringBuilder();
			results.Append(GettextCatalog.GetString("\nName : " + item.FullName));
			results.Append(GettextCatalog.GetString("\nTotal number of local variables : " + item.NumberOfVariables));
			results.Append(GettextCatalog.GetString("\nTotal number of parameters : " + item.ParameterCount));
			results.Append(GettextCatalog.GetString("\nAfferent Coupling : " + item.AfferentCoupling));
			results.Append(GettextCatalog.GetString("\nEfferent Coupling : " + item.EfferentCoupling));
			
			return results;
		}
		
		private static StringBuilder GenerateStructMetricText (StructProperties item)
		{
			StringBuilder results = new StringBuilder();
			results.Append(GettextCatalog.GetString("\nName : " + item.FullName));
			results.Append(GettextCatalog.GetString("\nTotal number of fields : " + item.Struct.Fields.Count ()));
			results.Append(GettextCatalog.GetString("\nTotal number of properties : " + item.Struct.Properties.Count ()));
			return results;
		}
		
		private static StringBuilder GenerateInterfaceMetricText (InterfaceProperties item)
		{
			StringBuilder results = new StringBuilder();
			results.Append(GettextCatalog.GetString("\nName : " + item.FullName));
			results.Append(GettextCatalog.GetString("\nTotal number of properties : " + item.Interface.Properties.Count ()));
			results.Append(GettextCatalog.GetString("\nTotal number of methods : " + item.Interface.Methods.Count ()));
			return results;
		}
		
		private static StringBuilder GenerateDelegateMetricText (DelegateProperties item)
		{
			StringBuilder results = new StringBuilder();
			results.Append(GettextCatalog.GetString("\nName : " + item.FullName));
			results.Append(GettextCatalog.GetString("\nReturn Type : " + item.Delegate.GetDelegateInvokeMethod ().ReturnType));
			results.Append(GettextCatalog.GetString("\nTotal number of parameters : " + item.Delegate.GetDelegateInvokeMethod ().Parameters.Count));
			return results;
		}
		
		private static StringBuilder GenerateEnumMetricText (EnumProperties item)
		{
			StringBuilder results = new StringBuilder();
			results.Append(GettextCatalog.GetString("\nName : " + item.FullName));
			results.Append(GettextCatalog.GetString("\nTotal number of inner types : " + item.Enum.NestedTypes.Count));
			return results;
		}
	}
}
