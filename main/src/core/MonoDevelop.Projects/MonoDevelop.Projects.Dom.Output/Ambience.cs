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
		
		string mimeTypes;
		public string MimeTypes {
			get {
				return mimeTypes;
			}
		}
		
		protected Dictionary<Modifiers, string> modifiers = new Dictionary<Modifiers, string> ();
		protected Dictionary<ClassType, string> classTypes = new Dictionary<ClassType, string> ();
		protected Dictionary<ParameterModifiers, string> parameterModifiers = new Dictionary<ParameterModifiers, string> ();
		protected Dictionary<string, string> constructs = new Dictionary<string, string> ();
		protected const string nullString = "null";
		
		protected abstract IDomVisitor OutputVisitor {
			get;
		}
		
		public Ambience (string name, string mimeTypes)
		{
			this.name      = name;
			this.mimeTypes = mimeTypes ?? "";
		}
		
		public virtual bool IsValidFor (string fileName)
		{
			return false;
		}
		
		
		protected string GetString (Modifiers m)
		{
			if ((m & Modifiers.ProtectedAndInternal) == Modifiers.ProtectedAndInternal)
				return (GetString (m & ~Modifiers.ProtectedAndInternal) + " " + modifiers[Modifiers.ProtectedAndInternal]).Trim ();
			if ((m & Modifiers.ProtectedOrInternal) == Modifiers.ProtectedOrInternal)
				return (GetString (m & ~Modifiers.ProtectedOrInternal) + " " + modifiers[Modifiers.ProtectedOrInternal]).Trim ();
			
			StringBuilder result = new StringBuilder ();
			foreach (Modifiers singleModifier in Enum.GetValues (typeof(Modifiers))) {
				if ((m & singleModifier) == singleModifier && modifiers.ContainsKey (singleModifier)) {
					if (modifiers[singleModifier].Length > 0 && result.Length > 0) // don't add spaces for empty modifiers
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
		protected static bool UseFullName (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.UseFullName) == OutputFlags.UseFullName;
		}
		protected static bool IncludeParameters (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.IncludeParameters) == OutputFlags.IncludeParameters;
		}
		protected static bool IncludeReturnType (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.IncludeReturnType) == OutputFlags.IncludeReturnType;
		}
		protected static bool IncludeParameterName (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.IncludeParameterName) == OutputFlags.IncludeParameterName;
		}
		protected static bool EmitMarkup (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.EmitMarkup) == OutputFlags.EmitMarkup;
		}
		protected static bool EmitKeywords (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.EmitKeywords) == OutputFlags.EmitKeywords;
		}
		protected static bool IncludeModifiers (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.IncludeModifiers) == OutputFlags.IncludeModifiers;
		}
		protected static bool IncludeBaseTypes (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.IncludeBaseTypes) == OutputFlags.IncludeBaseTypes;
		}
		protected static bool IncludeGenerics (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.IncludeGenerics) == OutputFlags.IncludeGenerics;
		}
		protected static bool HighlightName (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.HighlightName) == OutputFlags.HighlightName;
		}
		protected static bool HideExtensionsParameter (object settings)
		{
			OutputFlags outputFlags = GetFlags (settings);
			return (outputFlags & OutputFlags.HideExtensionsParameter) == OutputFlags.HideExtensionsParameter;
		}
		#endregion
		
		public static string Format (string str)
		{
			if (String.IsNullOrEmpty (str))
				return string.Empty;
			
			StringBuilder sb = new StringBuilder (str.Length);
			MarkupUtilities.AppendEscapedString (sb, str);
			return sb.ToString (); 
		}
		
		protected static OutputFlags GetFlags (object settings)
		{
			if (settings is OutputFlags)
				return (OutputFlags)settings;
			return ((OutputSettings)settings).OutputFlags;
		}
		
		protected static OutputSettings GetSettings (object settings)
		{
			if (settings is OutputFlags)
				return new OutputSettings ((OutputFlags)settings);
			return (OutputSettings)settings;
		}
		
		public abstract string SingleLineComment (string text);
		public abstract string GetString (string nameSpace, OutputSettings settings);
		
		public string GetString (IDomVisitable domVisitable, OutputFlags flags)
		{
			return GetString (domVisitable, new OutputSettings (flags));
		}
		
		public string GetString (IDomVisitable domVisitable, object settings)
		{
			if (domVisitable == null)
				return nullString;
			string result = (string)domVisitable.AcceptVisitor (OutputVisitor, settings);
			if (settings is OutputSettings) 
				((OutputSettings)settings).PostProcess (domVisitable, ref result);
			return result;
		}
		
	}
}
