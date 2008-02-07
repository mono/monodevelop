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
using System.Text;

namespace MonoDevelop.Ide.Dom.Output
{
	public class NetAmbience : Ambience
	{
		const string nullString = "Null";
		public NetAmbience () : base ("NET")
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
		
		public override string GetString (string nameSpace, OutputFlags flags)
		{
			StringBuilder result = new StringBuilder ();
			if (EmitKeywords (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append ("Namespace ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			result.Append (nameSpace);
			return result.ToString ();
		}
		
		public override string GetString (IProperty property, OutputFlags flags)
		{
			if (property == null)
				return nullString;
			StringBuilder result = new StringBuilder ();
			if (IncludeModifiers (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (base.GetString (property.Modifiers));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			if (EmitKeywords (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append ("Property ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (UseFullName (flags)) {
				result.Append (property.FullName);
			} else {
				result.Append (property.Name);
			}
			if (IncludeReturnType (flags)) {
				result.Append (" : ");
				result.Append (GetString (property.ReturnType, flags));
			}
			return result.ToString ();
		}
		
		public override string GetString (IField field, OutputFlags flags)
		{
			if (field == null)
				return nullString;
			StringBuilder result = new StringBuilder ();
			
			if (IncludeModifiers (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (base.GetString (field.Modifiers));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (EmitKeywords (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append ("Field ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (UseFullName (flags)) {
				result.Append (field.FullName);
			} else {
				result.Append (field.Name);
			}
			if (IncludeReturnType (flags)) {
				result.Append (" : ");
				result.Append (GetString (field.ReturnType, flags));
			}
			return result.ToString ();
		}
		
		public override string GetString (IReturnType returnType, OutputFlags flags)
		{
			if (returnType == null)
				return nullString;
			StringBuilder result = new StringBuilder ();
			if (UseFullName (flags)) {
				result.Append (returnType.FullName);
			} else {
				result.Append (returnType.Name);
			}
			return result.ToString ();
		}
		
		public override string GetString (IMethod method, OutputFlags flags)
		{
			if (method == null)
				return nullString;
			StringBuilder result = new StringBuilder ();
			
			if (IncludeModifiers (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (base.GetString (method.Modifiers));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (EmitKeywords (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append ("Method ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (UseFullName (flags)) {
				result.Append (method.FullName);
			} else {
				result.Append (method.Name);
			}
			
			if (IncludeParameters (flags)) {
				result.Append ("(");
				bool first = true;
				foreach (IParameter parameter in method.Parameters) {
					if (!first)
						result.Append (", ");
					result.Append (GetString (parameter, flags));
					first = false;
				}
				result.Append (")");
			}
				
			if (IncludeReturnType (flags)) {
				result.Append (" : ");
				result.Append (GetString (method.ReturnType, flags));
			}
			
			return result.ToString ();
		}
		
		public override string GetString (IParameter parameter, OutputFlags flags)
		{
			if (parameter == null)
				return nullString;
			StringBuilder result = new StringBuilder ();
			if (IncludeParameterName (flags)) {
				result.Append (parameter.Name);
				if (IncludeReturnType (flags)) {
					result.Append (" : ");
					result.Append (GetString (parameter.ReturnType, flags));
				}				
			} else {
				result.Append (GetString (parameter.ReturnType, flags));
			}
			return result.ToString ();
		}
		
		public override string GetString (IType type, OutputFlags flags)
		{
			if (type == null)
				return nullString;
			StringBuilder result = new StringBuilder ();
			if (IncludeModifiers (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (base.GetString (type.Modifiers));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (EmitKeywords (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (GetString (type.ClassType));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
						
			result.Append (type.Name);
			
			if (type.BaseType != null) {
				result.Append (" : ");
				result.Append (type.BaseType.Name);
			}
			return result.ToString ();
		}
		
		public override string GetString (IEvent evt, OutputFlags flags)
		{
			if (evt == null)
				return nullString;
			StringBuilder result = new StringBuilder ();
			if (IncludeModifiers (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append (base.GetString (evt.Modifiers));
				result.Append (" ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
			
			if (EmitKeywords (flags)) {
				if (EmitMarkup (flags))
					result.Append ("<b>");
				result.Append ("Event ");
				if (EmitMarkup (flags))
					result.Append ("</b>");
			}
						
			result.Append (evt.Name);
			
			return result.ToString ();
		}
		
	}
}
