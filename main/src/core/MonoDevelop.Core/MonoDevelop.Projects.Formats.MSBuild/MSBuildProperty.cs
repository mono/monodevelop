//
// MSBuildProperty.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Xml;
using Microsoft.Build.BuildEngine;
using System.Xml.Linq;
using MonoDevelop.Core;
using System.Globalization;


namespace MonoDevelop.Projects.Formats.MSBuild
{
	
	public class MSBuildProperty: MSBuildPropertyCore, IMetadataProperty, IMSBuildPropertyEvaluated
	{
		bool preserverCase;
		MSBuildValueType valueType = MSBuildValueType.Default;

		internal MSBuildProperty (MSBuildProject project, XmlElement elem): base (project, elem)
		{
			NotifyChanges = true;
		}

		internal override string GetName ()
		{
			return Element.Name;
		}

		public bool IsImported {
			get;
			set;
		}

		public bool MergeToMainGroup { get; set; }

		internal bool Overwritten { get; set; }

		internal MSBuildPropertyGroup Owner { get; set; }

		internal bool HasDefaultValue { get; set; }

		internal bool NotifyChanges { get; set; }

		internal MSBuildValueType ValueType {
			get { return valueType; }
		}

		internal MergedProperty CreateMergedProperty ()
		{
			return new MergedProperty (Name, preserverCase, HasDefaultValue);
		}

		public void SetValue (string value, bool preserveCase = false, bool mergeToMainGroup = false)
		{
			MergeToMainGroup = mergeToMainGroup;
			this.preserverCase = preserveCase;
			valueType = preserveCase ? MSBuildValueType.Default : MSBuildValueType.DefaultPreserveCase;

			if (value == null)
				value = String.Empty;

			if (preserveCase) {
				var current = GetPropertyValue ();
				if (current != null) {
					if (current.Equals (value, preserveCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
						return;
				}
			}
			SetPropertyValue (value);
			if (Project != null && NotifyChanges)
				Project.NotifyChanged ();
		}

		public void SetValue (FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false)
		{
			MergeToMainGroup = mergeToMainGroup;
			this.preserverCase = false;
			valueType = MSBuildValueType.Path;

			string baseDir = null;
			if (relativeToPath != null) {
				baseDir = relativeToPath;
			} else if (relativeToProject) {
				baseDir = Project.BaseDirectory;
			}

			// If the path is normalized in the property, keep the value
			if (!string.IsNullOrEmpty (Value) && new FilePath (MSBuildProjectService.FromMSBuildPath (baseDir, Value)).CanonicalPath == value.CanonicalPath)
				return;

			SetPropertyValue (MSBuildProjectService.ToMSBuildPath (baseDir, value, false));
			if (Project != null && NotifyChanges)
				Project.NotifyChanged ();
		}

		public void SetValue (object value, bool mergeToMainGroup = false)
		{
			if (value is bool) {
				if (Owner != null && Owner.UppercaseBools)
					SetValue ((bool)value ? "True" : "False", preserveCase: true, mergeToMainGroup: mergeToMainGroup);
				else
					SetValue ((bool)value ? "true" : "false", preserveCase: true, mergeToMainGroup: mergeToMainGroup);
				valueType = MSBuildValueType.Boolean;
			}
			else
				SetValue (Convert.ToString (value, CultureInfo.InvariantCulture), false, mergeToMainGroup);
		}		

		internal virtual void SetPropertyValue (string value)
		{
			if (Element.IsEmpty && string.IsNullOrEmpty (value))
				return;

			if (value == null)
				value = string.Empty;

			// This code is from Microsoft.Build.Internal.Utilities

			if (value.IndexOf('<') != -1) {
				// If the value looks like it probably contains XML markup ...
				try {
					// Attempt to store it verbatim as XML.
					Element.InnerXml = value;
					XmlUtil.FormatElement (Project.TextFormat, Element);
					return;
				}
				catch (XmlException) {
					// But that may fail, in the event that "value" is not really well-formed
					// XML.  Eat the exception and fall through below ...
				}
			}

			// The value does not contain valid XML markup.  Store it as text, so it gets 
			// escaped properly.
			Element.InnerText = value;
			if (Project != null && NotifyChanges)
				Project.NotifyChanged ();
		}

		internal override string GetPropertyValue ()
		{
			// This code is from Microsoft.Build.Internal.Utilities

			if (!Element.HasChildNodes)
				return string.Empty;

			if (Element.ChildNodes.Count == 1 && (Element.FirstChild.NodeType == XmlNodeType.Text || Element.FirstChild.NodeType == XmlNodeType.CDATA))
				return Element.InnerText.Trim ();

			string innerXml = Element.InnerXml;

			// If there is no markup under the XML node (detected by the presence
			// of a '<' sign
			int firstLessThan = innerXml.IndexOf('<');
			if (firstLessThan == -1) {
				// return the inner text so it gets properly unescaped
				return Element.InnerText.Trim ();
			}

			bool containsNoTagsOtherThanComments = ContainsNoTagsOtherThanComments (innerXml, firstLessThan);

			// ... or if the only XML is comments,
			if (containsNoTagsOtherThanComments) {
				// return the inner text so the comments are stripped
				// (this is how one might comment out part of a list in a property value)
				return Element.InnerText.Trim ();
			}

			// ...or it looks like the whole thing is a big CDATA tag ...
			bool startsWithCData = (innerXml.IndexOf("<![CDATA[", StringComparison.Ordinal) == 0);

			if (startsWithCData) {
				// return the inner text so it gets properly extracted from the CDATA
				return Element.InnerText.Trim ();
			}

			// otherwise, it looks like genuine XML; return the inner XML so that
			// tags and comments are preserved and any XML escaping is preserved
			return innerXml;
		}

		// This code is from Microsoft.Build.Internal.Utilities

		/// <summary>
		/// Figure out whether there are any XML tags, other than comment tags,
		/// in the string.
		/// </summary>
		/// <remarks>
		/// We know the string coming in is a valid XML fragment. (The project loaded after all.)
		/// So for example we can ignore an open comment tag without a matching closing comment tag.
		/// </remarks>
		static bool ContainsNoTagsOtherThanComments (string innerXml, int firstLessThan)
		{
			bool insideComment = false;
			for (int i = firstLessThan; i < innerXml.Length; i++)
			{
				if (!insideComment)
				{
					// XML comments start with exactly "<!--"
					if (i < innerXml.Length - 3
						&& innerXml[i] == '<'
						&& innerXml[i + 1] == '!'
						&& innerXml[i + 2] == '-'
						&& innerXml[i + 3] == '-')
					{
						// Found the start of a comment
						insideComment = true;
						i = i + 3;
						continue;
					}
				}

				if (!insideComment)
				{
					if (innerXml[i] == '<')
					{
						// Found a tag!
						return false;
					}
				}

				if (insideComment)
				{
					// XML comments end with exactly "-->"
					if (i < innerXml.Length - 2
						&& innerXml[i] == '-'
						&& innerXml[i + 1] == '-'
						&& innerXml[i + 2] == '>')
					{
						// Found the end of a comment
						insideComment = false;
						i = i + 2;
						continue;
					}
				}
			}

			// Didn't find any tags, except possibly comments
			return true;
		}

		public override string UnevaluatedValue {
			get {
				return Value;
			}
		}
	}

	class ItemMetadataProperty: MSBuildProperty
	{
		string value;
		string name;

		public ItemMetadataProperty (MSBuildProject project, string name): base (project, null)
		{
			NotifyChanges = false;
			this.name = name;
		}

		internal override string GetName ()
		{
			return name;
		}

		internal override void SetPropertyValue (string value)
		{
			this.value = value;
		}

		internal override string GetPropertyValue ()
		{
			return value;
		}

		public override string UnevaluatedValue {
			get {
				return value;
			}
		}
	}

	class MSBuildPropertyEvaluated: MSBuildPropertyCore, IMSBuildPropertyEvaluated
	{
		string value;
		string evaluatedValue;
		string name;

		internal MSBuildPropertyEvaluated (MSBuildProject project, string name, string value, string evaluatedValue): base (project, null)
		{
			this.evaluatedValue = evaluatedValue;
			this.value = value;
			this.name = name;
		}

		internal override string GetName ()
		{
			return name;
		}

		public bool IsImported { get; set; }

		public override string UnevaluatedValue {
			get { return value; }
		}

		internal override string GetPropertyValue ()
		{
			return evaluatedValue;
		}
	}

	public abstract class MSBuildPropertyCore: MSBuildObject
	{
		MSBuildProject project;

		internal MSBuildPropertyCore (MSBuildProject project, XmlElement elem): base (elem)
		{
			this.project = project;
		}

		public MSBuildProject Project {
			get { return project; }
			internal set { project = value; }
		}

		public string Name {
			get { return GetName (); }
		}

		public string Value {
			get { return GetPropertyValue (); }
		}

		public T GetValue<T> ()
		{
			return (T)GetValue (typeof(T));
		}

		public object GetValue (Type t)
		{
			var val = GetPropertyValue ();
			if (t == typeof(bool))
				return (object) val.Equals ("true", StringComparison.InvariantCultureIgnoreCase);
			if (t.IsEnum)
				return Enum.Parse (t, val, true);
			if (t.IsGenericType && t.GetGenericTypeDefinition () == typeof(Nullable<>)) {
				var at = t.GetGenericArguments () [0];
				if (string.IsNullOrEmpty (Value))
					return null;
				return Convert.ChangeType (Value, at, CultureInfo.InvariantCulture);
			}
			return Convert.ChangeType (Value, t, CultureInfo.InvariantCulture);
		}

		public FilePath GetPathValue (bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var val = GetPropertyValue ();
			string baseDir = null;

			if (relativeToPath != null) {
				baseDir = relativeToPath;
			} else if (relativeToProject) {
				baseDir = project.BaseDirectory;
			}
			var p = MSBuildProjectService.FromMSBuildPath (baseDir, val);

			// Remove the trailing slash
			if (p.Length > 0 && p[p.Length - 1] == System.IO.Path.DirectorySeparatorChar && p != "." + System.IO.Path.DirectorySeparatorChar)
				return p.TrimEnd (System.IO.Path.DirectorySeparatorChar);
			
			return p;
		}

		public bool TryGetPathValue (out FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var val = GetPropertyValue ();
			string baseDir = null;

			if (relativeToPath != null) {
				baseDir = relativeToPath;
			} else if (relativeToProject) {
				baseDir = project.BaseDirectory;
			}
			string path;
			var res = MSBuildProjectService.FromMSBuildPath (baseDir, val, out path);
			value = path;
			return res;
		}

		public abstract string UnevaluatedValue { get; }

		internal abstract string GetPropertyValue ();

		internal abstract string GetName ();

		public override string ToString ()
		{
			return "[" + Name + " = " + Value + "]";
		}
	}

	public interface IMSBuildPropertyEvaluated
	{
		bool IsImported { get; }

		string Name { get; }

		MSBuildProject Project { get; }

		string Value { get; }

		string UnevaluatedValue { get; }

		T GetValue<T> ();

		object GetValue (Type t);

		FilePath GetPathValue (bool relativeToProject = true, FilePath relativeToPath = default(FilePath));

		bool TryGetPathValue (out FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath));
	}

	public interface IMetadataProperty
	{
		string Name { get; }

		string Value { get; }

		string UnevaluatedValue { get; }

		T GetValue<T> ();

		object GetValue (Type t);

		FilePath GetPathValue (bool relativeToProject = true, FilePath relativeToPath = default(FilePath));

		bool TryGetPathValue (out FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath));

		void SetValue (string value, bool preserveCase = false, bool mergeToMainGroup = false);

		void SetValue (FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false);

		void SetValue (object value, bool mergeToMainGroup = false);
	}

	class MergedProperty
	{
		public readonly string Name;
		public readonly bool IsDefault;
		public readonly bool PreserveExistingCase;

		public MergedProperty (string name, bool preserveExistingCase, bool isDefault)
		{
			this.Name = name;
			IsDefault = isDefault;
			this.PreserveExistingCase = preserveExistingCase;
		}
	}
}
