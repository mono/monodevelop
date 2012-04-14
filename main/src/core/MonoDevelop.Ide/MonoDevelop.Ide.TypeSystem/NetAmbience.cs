//
// NetAmbience.cs
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
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;

namespace MonoDevelop.Ide.TypeSystem
{
	public class NetAmbience : Ambience
	{
		public NetAmbience () : base ("NET")
		{
			classTypes [TypeKind.Class] = "Class";
			classTypes [TypeKind.Enum] = "Enumeration";
			classTypes [TypeKind.Interface] = "Interface";
			classTypes [TypeKind.Struct] = "Structure";
			classTypes [TypeKind.Delegate] = "Delegate";
		}
		
		public override string SingleLineComment (string text)
		{
			return "// " + text;
		}
		
		#region Type system output
		public override string GetIntrinsicTypeName (string reflectionName)
		{
			return reflectionName;
		}
		
		protected override string GetTypeReferenceString (IType reference, OutputSettings settings)
		{
			return reference.ToString ();
		}
		
		protected override string GetTypeString (IType t, OutputSettings settings)
		{
			ITypeDefinition type = t.GetDefinition ();
			var result = new StringBuilder ();
			if (settings.IncludeModifiers)
				AppendModifiers (result, settings, type);
			if (settings.IncludeKeywords)
				result.Append (settings.EmitKeyword (GetString (type.Kind)));
			
			result.Append (settings.EmitName (type, settings.UseFullName ? type.FullName : type.Name));
			
			int parameterCount = type.TypeParameters.Count;
			
			if (settings.IncludeGenerics && parameterCount > 0) {
				result.Append (settings.Markup ("<"));
				if (!settings.HideGenericParameterNames) {
					for (int i = 0; i < parameterCount; i++) {
						if (i > 0)
							result.Append (settings.Markup (", "));
						result.Append (type.TypeParameters [i].Name);
					}
				}
				result.Append (settings.Markup (">"));
			}
			
			if (settings.IncludeBaseTypes && type.DirectBaseTypes.Any ()) {
				result.Append (settings.Markup (" : "));
				bool first = true;
				foreach (var baseType in type.DirectBaseTypes) {
					if (baseType.Equals (type.Compilation.FindType (KnownTypeCode.Object)))
						continue;
					if (!first)
						result.Append (settings.Markup (", "));
					first = false;
					result.Append (GetTypeReferenceString (baseType, settings));
				}
				
			}
			return result.ToString ();
		}
		
		protected override string GetMethodString (IMethod method, OutputSettings settings)
		{
			var result = new StringBuilder ();
			
			if (settings.IncludeModifiers)
				AppendModifiers (result, settings, method);
			
			result.Append (settings.EmitKeyword ("Method"));
			result.Append (settings.EmitName (method, settings.UseFullName ? method.FullName : method.Name));
			
			if (settings.IncludeParameters) {
				result.Append (settings.Markup ("("));
				bool first = true;
				foreach (var parameter in method.Parameters) {
					if (!first)
						result.Append (settings.Markup (", "));
					result.Append (GetParameterString (method, parameter, settings));
					first = false;
				}
				result.Append (settings.Markup (")"));
			}
				
			if (settings.IncludeReturnType) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (method.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		protected override string GetConstructorString (IMethod method, OutputSettings settings)
		{
			var result = new StringBuilder ();
			
			if (settings.IncludeModifiers)
				AppendModifiers (result, settings, method);
			
			result.Append (settings.EmitKeyword ("Constructor"));
			result.Append (settings.EmitName (method, method.DeclaringType.Name));
			
			if (settings.IncludeParameters) {
				result.Append (settings.Markup ("("));
				bool first = true;
				foreach (var parameter in method.Parameters) {
					if (!first)
						result.Append (settings.Markup (", "));
					result.Append (GetParameterString (method, parameter, settings));
					first = false;
				}
				result.Append (settings.Markup (")"));
			}
			return result.ToString ();
		}
		
		protected override string GetDestructorString (IMethod method, OutputSettings settings)
		{
			var result = new StringBuilder ();
			result.Append (settings.EmitKeyword ("Destructor"));
			result.Append (settings.EmitName (method, method.DeclaringType.Name));
			return result.ToString ();
		}
		
		protected override string GetOperatorString (IMethod method, OutputSettings settings)
		{
			var result = new StringBuilder ();
			
			if (settings.IncludeModifiers)
				AppendModifiers (result, settings, method);
			
			result.Append (settings.EmitKeyword ("Operator"));
			result.Append (settings.EmitName (method, settings.UseFullName ? method.FullName : method.Name));
			
			if (settings.IncludeParameters) {
				result.Append (settings.Markup ("("));
				bool first = true;
				foreach (var parameter in method.Parameters) {
					if (!first)
						result.Append (settings.Markup (", "));
					result.Append (GetParameterString (method, parameter, settings));
					first = false;
				}
				result.Append (settings.Markup (")"));
			}
				
			if (settings.IncludeReturnType) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (method.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		protected override string GetFieldString (IField field, OutputSettings settings)
		{
			var result = new StringBuilder ();
			
			if (settings.IncludeModifiers)
				AppendModifiers (result, settings, field);
			
			result.Append (settings.EmitKeyword ("Field"));
			result.Append (settings.EmitName (field, field.Name));
			
			if (settings.IncludeReturnType) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (field.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		protected override string GetEventString (IEvent evt, OutputSettings settings)
		{
			var result = new StringBuilder ();
			
			if (settings.IncludeModifiers)
				AppendModifiers (result, settings, evt);
			
			result.Append (settings.EmitKeyword ("Event"));
			result.Append (settings.EmitName (evt, evt.Name));
			
			if (settings.IncludeReturnType) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (evt.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		protected override string GetPropertyString (IProperty property, OutputSettings settings)
		{
			var result = new StringBuilder ();
			if (settings.IncludeModifiers)
				AppendModifiers (result, settings, property);
			result.Append (settings.EmitKeyword ("Property"));
			result.Append (settings.EmitName (property, property.Name));
			if (settings.IncludeReturnType) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (property.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		protected override string GetIndexerString (IProperty property, OutputSettings settings)
		{
			var result = new StringBuilder ();
			if (settings.IncludeModifiers)
				AppendModifiers (result, settings, property);
			result.Append (settings.EmitKeyword ("Indexer"));
			result.Append (settings.EmitName (property, property.Name));
			
			if (settings.IncludeParameters && property.Parameters.Count > 0) {
				result.Append (settings.Markup ("("));
				bool first = true;
				foreach (var parameter in property.Parameters) {
					if (!first)
						result.Append (settings.Markup (", "));
					result.Append (GetParameterString (property, parameter, settings));
					first = false;
				}
				result.Append (settings.Markup (")"));
			}
			if (settings.IncludeReturnType) {
				result.Append (settings.Markup (" : "));
				result.Append (GetTypeReferenceString (property.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		protected override string GetParameterString (IParameterizedMember member, IParameter parameter, OutputSettings settings)
		{
			var result = new StringBuilder ();
			if (settings.IncludeParameterName) {
				result.Append (Format (parameter.Name));
				if (settings.IncludeReturnType) {
					result.Append (settings.Markup (" : "));
					result.Append (GetTypeReferenceString (parameter.Type, settings));
				}
			} else {
				result.Append (GetTypeReferenceString (parameter.Type, settings));
			}
			if (parameter.IsRef || parameter.IsOut)
				result.Append (settings.Markup ("&"));
			return result.ToString ();
		}
		#endregion
		
		void AppendModifiers (StringBuilder result, OutputSettings settings, IEntity entity)
		{
			if (entity.IsStatic)
				result.Append (settings.EmitModifiers ("Static"));
			if (entity.IsSealed)
				result.Append (settings.EmitModifiers ("Sealed"));
			if (entity.IsAbstract)
				result.Append (settings.EmitModifiers ("Abstract"));
			if (entity.IsShadowing)
				result.Append (settings.EmitModifiers ("Shadows"));
			if (entity.IsSynthetic)
				result.Append (settings.EmitModifiers ("Synthetic"));
			
			switch (entity.Accessibility) {
			case Accessibility.Internal:
				result.Append (settings.EmitModifiers ("Internal"));
				break;
			case Accessibility.ProtectedAndInternal:
				result.Append (settings.EmitModifiers ("Protected And Internal"));
				break;
			case Accessibility.ProtectedOrInternal:
				result.Append (settings.EmitModifiers ("Protected Or Internal"));
				break;
			case Accessibility.Protected:
				result.Append (settings.EmitModifiers ("Protected"));
				break;
			case Accessibility.Private:
				result.Append (settings.EmitModifiers ("Private"));
				break;
			case Accessibility.Public:
				result.Append (settings.EmitModifiers ("Public"));
				break;
			}
		}
		
		public override string GetString (string nameSpace, OutputSettings settings)
		{
			var result = new StringBuilder ();
			result.Append (settings.EmitKeyword ("Namespace"));
			result.Append (Format (nameSpace));
			return result.ToString ();
		}
		
		Dictionary<TypeKind, string> classTypes = new Dictionary<TypeKind, string> ();
		
		string GetString (TypeKind classType)
		{
			string res;
			if (classTypes.TryGetValue (classType, out res))
				return res;
			return string.Empty;
		}
//		public string Visit (IAttribute attribute, OutputSettings settings)
//		{
//			StringBuilder result = new StringBuilder ();
//			result.Append (settings.Markup ("["));
//			result.Append (GetString (attribute.AttributeType, settings));
//			result.Append (settings.Markup ("("));
//			bool first = true;
//			if (attribute.PositionalArguments != null) {
//				foreach (object o in attribute.PositionalArguments) {
//					if (!first)
//						result.Append (settings.Markup (", "));
//					first = false;
//					if (o is string) {
//						result.Append (settings.Markup ("\""));
//						result.Append (o);
//						result.Append (settings.Markup ("\""));
//					} else if (o is char) {
//						result.Append (settings.Markup ("\""));
//						result.Append (o);
//						result.Append (settings.Markup ("\""));
//					} else
//						result.Append (o);
//				}
//			}
//			result.Append (settings.Markup (")]"));
//			return result.ToString ();
//		}
	}
}
