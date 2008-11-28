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

namespace MonoDevelop.Projects.Dom.Output
{
	public class NetAmbience : Ambience, IDomVisitor
	{
		protected override IDomVisitor OutputVisitor {
			get {
				return this;
			}
		}
		
		public NetAmbience () : base ("NET", "")
		{
			classTypes[ClassType.Class]     = "Class";
			classTypes[ClassType.Enum]      = "Enumeration";
			classTypes[ClassType.Interface] = "Interface";
			classTypes[ClassType.Struct]    = "Structure";
			classTypes[ClassType.Delegate]  = "Delegate";
			
			parameterModifiers[ParameterModifiers.In]       = "In";
			parameterModifiers[ParameterModifiers.Out]      = "Out";
			parameterModifiers[ParameterModifiers.Ref]      = "Ref";
			parameterModifiers[ParameterModifiers.Params]   = "Params";
			parameterModifiers[ParameterModifiers.Optional] = "Optional";
			
			modifiers[Modifiers.Private]              = "Private";
			modifiers[Modifiers.Internal]             = "Internal";
			modifiers[Modifiers.Protected]            = "Protected";
			modifiers[Modifiers.Public]               = "Public";
			modifiers[Modifiers.Abstract]             = "Abstract";
			modifiers[Modifiers.Virtual]              = "Virtual";
			modifiers[Modifiers.Sealed]               = "Sealed";
			modifiers[Modifiers.Static]               = "Static";
			modifiers[Modifiers.Override]             = "Override";
			modifiers[Modifiers.Readonly]             = "Readonly";
			modifiers[Modifiers.Const]                = "Const";
			modifiers[Modifiers.Partial]              = "Partial";
			modifiers[Modifiers.Extern]               = "Extern";
			modifiers[Modifiers.Volatile]             = "Volatile";
			modifiers[Modifiers.Unsafe]               = "Unsafe";
			modifiers[Modifiers.Overloads]            = "Overloads";
			modifiers[Modifiers.WithEvents]           = "WithEvents";
			modifiers[Modifiers.Default]              = "Default";
			modifiers[Modifiers.Fixed]                = "Fixed";
			modifiers[Modifiers.ProtectedAndInternal] = "Protected Internal";
			modifiers[Modifiers.ProtectedOrInternal]  = "Internal Protected";
		}
		
		public override string SingleLineComment (string text)
		{
			return "// " + text;
		}

		public override string GetString (string nameSpace, OutputSettings settings)
		{
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitKeyword ("Namespace"));
			result.Append (Format (nameSpace));
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (ICompilationUnit unit, object data)
		{
			return "TODO";
		}
		
		object IDomVisitor.Visit (IUsing u, object data)
		{
			return "TODO";
		}
		
		object IDomVisitor.Visit (IProperty property, object data)
		{
			StringBuilder result = new StringBuilder ();
			OutputSettings settings = GetSettings (data);
			result.Append (settings.EmitModifiers (base.GetString (property.Modifiers)));
			result.Append (settings.EmitKeyword ("Property"));
			
			if (UseFullName (data)) {
				result.Append (Format (property.FullName));
			} else {
				result.Append (Format (property.Name));
			}
			
			if (IncludeReturnType (data)) {
				result.Append (settings.Markup (" : "));
				result.Append (GetString (property.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IField field, object data)
		{
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			
			result.Append (settings.EmitModifiers (base.GetString (field.Modifiers)));
			result.Append (settings.EmitKeyword ("Field"));
			
			if (UseFullName (settings)) {
				result.Append (Format (field.FullName));
			} else {
				result.Append (Format (field.Name));
			}
			
			if (IncludeReturnType (settings) && !field.IsLiteral) {
				result.Append (settings.Markup (" : "));
				result.Append (GetString (field.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IReturnType returnType, object data)
		{
			return Format (UseFullName (data) ? returnType.FullName : returnType.Name);
		}
		
		object IDomVisitor.Visit (IMethod method, object data)
		{
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			
			result.Append (settings.EmitModifiers (base.GetString (method.Modifiers)));
			result.Append (settings.EmitKeyword (method.IsConstructor ? "Constructor" : "Method"));
			
			result.Append (Format (UseFullName (settings) ? method.FullName : method.Name));
			
			if (IncludeParameters (settings)) {
				result.Append (settings.Markup ("("));
				bool first = true;
				if (method.Parameters != null) {
					foreach (IParameter parameter in method.Parameters) {
						if (!first)
							result.Append (settings.Markup (", "));
						result.Append (GetString (parameter, settings));
						first = false;
					}
				}
				result.Append (settings.Markup (")"));
			}
				
			if (IncludeReturnType (settings) && !method.IsConstructor) {
				result.Append (settings.Markup (" : "));
				result.Append (GetString (method.ReturnType, settings));
			}
			
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IParameter parameter, object data)
		{
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			if (IncludeParameterName (settings)) {
				result.Append (Format (parameter.Name));
				if (IncludeReturnType (settings)) {
					result.Append (settings.Markup (" : "));
					result.Append (GetString (parameter.ReturnType, settings));
				}				
			} else {
				result.Append (GetString (parameter.ReturnType, settings));
			}
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IType type, object data)
		{
			InstantiatedType instantiatedType = type as InstantiatedType;
			OutputSettings settings = GetSettings (data);
			OutputFlags flags = GetFlags (data);
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (type.Modifiers)));
			result.Append (settings.EmitKeyword (GetString (type.ClassType)));
			
			result.Append (Format (type.Name));
			
			int parameterCount = type.TypeParameters.Count;
			if (instantiatedType != null)
				parameterCount = instantiatedType.GenericParameters.Count;
			if (IncludeGenerics (flags) && parameterCount > 0) {
				result.Append (settings.Markup ("<"));
				for (int i = 0; i < parameterCount; i++) {
					if (i > 0)
						result.Append (settings.Markup (", "));
					if (instantiatedType != null) {
						result.Append (instantiatedType.GenericParameters[i].AcceptVisitor (this, data));
					} else {
						result.Append (type.TypeParameters[i].Name);
					}
				}
				result.Append (settings.Markup (">"));
			
			}
			if (IncludeBaseTypes (flags) && type.BaseTypes.Any ()) {
				result.Append (settings.Markup (" : "));
				bool first = true;
				foreach (IReturnType baseType in type.BaseTypes) {
					if (baseType.FullName == "System.Object")
						continue;
					if (!first)
						result.Append (settings.Markup (", "));
					first = false;
					result.Append (baseType.AcceptVisitor (this, data));	
				}
				
			}
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (IAttribute attribute, object data)
		{
			OutputFlags flags = GetFlags (data);
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			result.Append (settings.Markup ("["));
			result.Append (GetString (attribute.AttributeType, settings));
			result.Append (settings.Markup ("("));
			bool first = true;
			if (attribute.PositionalArguments != null) {
				foreach (object o in attribute.PositionalArguments) {
					if (!first)
						result.Append (settings.Markup (", "));
					first = false;
					if (o is string) {
						result.Append (settings.Markup ("\""));
						result.Append (o);
						result.Append (settings.Markup ("\""));
					} else if (o is char) {
						result.Append (settings.Markup ("\""));
						result.Append (o);
						result.Append (settings.Markup ("\""));
					} else
						result.Append (o);
				}
			}
			result.Append (settings.Markup (")]"));
			return result.ToString ();
		}
		
		object IDomVisitor.Visit (Namespace ns, object data)
		{
			OutputSettings settings = GetSettings (data);
			return settings.EmitKeyword ("Namespace") + ns.Name;
		}
		
		object IDomVisitor.Visit (LocalVariable var, object data)
		{
			return var.Name;
		}
		
		object IDomVisitor.Visit (IEvent evt, object data)
		{
			OutputFlags flags = GetFlags (data);
			OutputSettings settings = GetSettings (data);
			StringBuilder result = new StringBuilder ();
			result.Append (settings.EmitModifiers (base.GetString (evt.Modifiers)));
			result.Append (settings.EmitKeyword ("Event"));
			result.Append (Format (evt.Name));
			return result.ToString ();
		}
	}
}
