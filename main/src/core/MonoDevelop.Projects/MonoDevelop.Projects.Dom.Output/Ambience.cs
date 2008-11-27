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
		protected static bool IncludeGenerics (OutputFlags outputFlags)
		{
			return (outputFlags & OutputFlags.IncludeGenerics) == OutputFlags.IncludeGenerics;
		}
		protected static bool HighlightName (OutputFlags outputFlags)
		{
			return (outputFlags & OutputFlags.HighlightName) == OutputFlags.HighlightName;
		}
		protected static bool HideExtensionsParameter (OutputFlags outputFlags)
		{
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
		
		public Ambience Clone ()
		{
			return (Ambience)this.MemberwiseClone ();
		}
		
		public abstract string SingleLineComment (string text);
		public abstract string GetString (string nameSpace, OutputFlags flags);
		
		public string GetString (IDomVisitable domVisitable, OutputFlags flags)
		{
			if (domVisitable == null)
				return nullString;
			string result = (string)domVisitable.AcceptVisitor (OutputVisitor, flags);
			if (PostProcess != null)
				PostProcess (domVisitable, flags, ref result);
			return result;
		}
		
		public event PostProcessString PostProcess;

		
		public delegate void PostProcessString (IDomVisitable domVisitable, OutputFlags flags, ref string outString);
	}
}
