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
		
		Dictionary<Modifiers, string> cachedModifiers = new Dictionary<Modifiers, string> ();
		Dictionary<ParameterModifiers, string> cachedParameterModifiers = new Dictionary<ParameterModifiers, string> ();
		
		protected Dictionary<Modifiers, string> modifiers = new Dictionary<Modifiers, string> ();
		protected Dictionary<ClassType, string> classTypes = new Dictionary<ClassType, string> ();
		protected Dictionary<ParameterModifiers, string> parameterModifiers = new Dictionary<ParameterModifiers, string> ();
		protected Dictionary<string, string> constructs = new Dictionary<string, string> ();
		protected const string nullString = "null";
		
		protected abstract IDomVisitor<OutputSettings, string> OutputVisitor {
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
			string res;
			if (cachedModifiers.TryGetValue (m, out res))
				return res;
			
			if ((m & Modifiers.ProtectedAndInternal) == Modifiers.ProtectedAndInternal)
				res = (GetString (m & ~Modifiers.ProtectedAndInternal) + " " + modifiers[Modifiers.ProtectedAndInternal]).Trim ();
			else if ((m & Modifiers.ProtectedOrInternal) == Modifiers.ProtectedOrInternal)
				res = (GetString (m & ~Modifiers.ProtectedOrInternal) + " " + modifiers[Modifiers.ProtectedOrInternal]).Trim ();
			else {
				StringBuilder result = new StringBuilder ();
				foreach (Modifiers singleModifier in Enum.GetValues (typeof(Modifiers))) {
					if ((m & singleModifier) == singleModifier && modifiers.ContainsKey (singleModifier)) {
						if (modifiers[singleModifier].Length > 0 && result.Length > 0) // don't add spaces for empty modifiers
							result.Append (' ');
						result.Append (modifiers[singleModifier]);
					}
				}
				res = result.ToString ();
			}
			return cachedModifiers [m] = res;
		}
		
		protected string GetString (ClassType classType)
		{
			string res;
			if (classTypes.TryGetValue (classType, out res))
				return res;
			else
				return string.Empty;
		}
		
		protected string GetString (ParameterModifiers m)
		{
			string res;
			if (cachedParameterModifiers.TryGetValue (m, out res))
				return res;
			
			StringBuilder result = new StringBuilder ();
			foreach (ParameterModifiers singleModifier in Enum.GetValues (typeof(ParameterModifiers))) {
				if ((m & singleModifier) == singleModifier && parameterModifiers.ContainsKey (singleModifier)) {
					if (result.Length > 0)
						result.Append (' ');
					result.Append (parameterModifiers[singleModifier]);
				}
			}
			return cachedParameterModifiers [m] = result.ToString ();
		}
		
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
		
		public string GetString (string nameSpace, OutputFlags flags)
		{
			return GetString (nameSpace, new OutputSettings (flags));
		}
		
		public string GetString (IDomVisitable domVisitable, OutputSettings settings)
		{
			if (domVisitable == null)
				return nullString;
			string result = (string)domVisitable.AcceptVisitor (OutputVisitor, settings);
			if (settings is OutputSettings) 
				((OutputSettings)settings).PostProcess (domVisitable, ref result);
			return result;
		}
		
		public string GetString (IDomVisitable domVisitable, OutputFlags flags)
		{
			return GetString (domVisitable, new OutputSettings (flags));
		}
	}
}
