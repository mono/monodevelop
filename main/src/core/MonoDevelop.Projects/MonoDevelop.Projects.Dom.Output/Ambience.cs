//
// Ambience.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom.Output
{
	public abstract class Ambience
	{
		string name;
		
		public string Name {
			get {
				return name;
			}
		}
		
		protected Dictionary<Modifiers, string> modifiers = new Dictionary<Modifiers, string> ();
		protected Dictionary<ClassType, string> classTypes = new Dictionary<ClassType, string> ();
		protected Dictionary<ParameterModifiers, string> parameterModifiers = new Dictionary<ParameterModifiers, string> ();
		protected Dictionary<string, string> constructs = new Dictionary<string, string> ();
		protected string nullString = "null";
		
		protected abstract IDomVisitor OutputVisitor {
			get;
		}
		
		public Ambience (string name)
		{
			this.name = name;
		}
		
		protected string GetString (Modifiers m)
		{
			if ((m & Modifiers.ProtectedAndInternal) == Modifiers.ProtectedAndInternal)
				return GetString (m & ~Modifiers.ProtectedAndInternal) + " " + modifiers[Modifiers.ProtectedAndInternal];
			if ((m & Modifiers.ProtectedOrInternal) == Modifiers.ProtectedOrInternal)
				return GetString (m & ~Modifiers.ProtectedOrInternal) + " " + modifiers[Modifiers.ProtectedOrInternal];
			
			StringBuilder result = new StringBuilder ();
			foreach (Modifiers singleModifier in Enum.GetValues (typeof(Modifiers))) {
				if ((m & singleModifier) == singleModifier && modifiers.ContainsKey (singleModifier)) {
					if (result.Length > 0)
						result.Append (' ');
					result.Append (modifiers[singleModifier]);
				}
			}
			return result.ToString ();
		}
		
		protected string GetString (ClassType classType)
		{
			return classTypes.ContainsKey(classType) ? classTypes[classType] : "";
		}
		
		protected string GetString (ParameterModifiers m)
		{
			StringBuilder result = new StringBuilder ();
			foreach (ParameterModifiers singleModifier in Enum.GetValues (typeof(ParameterModifiers))) {
				if ((m & singleModifier) == singleModifier && parameterModifiers.ContainsKey (singleModifier)) {
					if (result.Length > 0)
						result.Append (' ');
					result.Append (parameterModifiers[singleModifier]);
				}
			}
			return result.ToString ();
		}
		#region FlagShortcuts
		protected static bool UseFullName (OutputFlags outputFlags)
		{
			return (outputFlags & OutputFlags.UseFullName) == OutputFlags.UseFullName;
		}
		protected static bool IncludeParameters (OutputFlags outputFlags)
		{
			return (outputFlags & OutputFlags.IncludeParameters) == OutputFlags.IncludeParameters;
		}
		protected static bool IncludeReturnType (OutputFlags outputFlags)
		{
			return (outputFlags & OutputFlags.IncludeReturnType) == OutputFlags.IncludeReturnType;
		}
		protected static bool IncludeParameterName (OutputFlags outputFlags)
		{
			return (outputFlags & OutputFlags.IncludeParameterName) == OutputFlags.IncludeParameterName;
		}
		protected static bool EmitMarkup (OutputFlags outputFlags)
		{
			return (outputFlags & OutputFlags.EmitMarkup) == OutputFlags.EmitMarkup;
		}
		protected static bool EmitKeywords (OutputFlags outputFlags)
		{
			return (outputFlags & OutputFlags.EmitKeywords) == OutputFlags.EmitKeywords;
		}
		protected static bool IncludeModifiers (OutputFlags outputFlags)
		{
			return (outputFlags & OutputFlags.IncludeModifiers) == OutputFlags.IncludeModifiers;
		}
		protected static bool IncludeBaseTypes (OutputFlags outputFlags)
		{
			return (outputFlags & OutputFlags.IncludeBaseTypes) == OutputFlags.IncludeBaseTypes;
		}
		#endregion			
		
		public static string Format (string str)
		{
			if (String.IsNullOrEmpty (str))
				return "";
			str = str.Replace ("&", "&amp;");
			str = str.Replace ("<", "&lt;");
			return str.Replace (">", "&gt;"); 
		}
		
		public abstract string SingleLineComment (string text);
		public abstract string GetString (string nameSpace, OutputFlags flags);
		
		public string GetString (IDomVisitable domVisitable, OutputFlags flags)
		{
			if (domVisitable == null)
				return nullString;
			return (string)domVisitable.AcceptVisitior (OutputVisitor, flags);
		}
	}
}
