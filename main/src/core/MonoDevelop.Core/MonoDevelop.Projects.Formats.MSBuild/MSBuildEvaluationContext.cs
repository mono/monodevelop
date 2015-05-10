//
// MSBuildEvaluationContext.cs
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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using Microsoft.Build.BuildEngine;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.MSBuild
{

	public class MSBuildEvaluationContext: IExpressionContext
	{
		Dictionary<string,string> properties = new Dictionary<string, string> ();
		bool allResolved;
		MSBuildProject project;

		string itemFile;
		string recursiveDir;

		public MSBuildEvaluationContext ()
		{
		}

		internal void InitEvaluation (MSBuildProject project)
		{
			this.project = project;
			SetPropertyValue ("MSBuildThisFile", Path.GetFileName (project.FileName));
			SetPropertyValue ("MSBuildThisFileName", Path.GetFileNameWithoutExtension (project.FileName));
			SetPropertyValue ("MSBuildThisFileDirectory", Path.GetDirectoryName (project.FileName) + Path.DirectorySeparatorChar);
			SetPropertyValue ("MSBuildThisFileExtension", Path.GetExtension (project.FileName));
			SetPropertyValue ("MSBuildThisFileFullPath", Path.GetFullPath (project.FileName));
			SetPropertyValue ("VisualStudioReferenceAssemblyVersion", project.ToolsVersion + ".0.0");
		}

		internal void SetItemContext (string itemFile, string recursiveDir)
		{
			this.itemFile = itemFile;
			this.recursiveDir = recursiveDir;
		}

		internal void ClearItemContext ()
		{
			this.itemFile = null;
			this.recursiveDir = null;
		}

		public string GetPropertyValue (string name)
		{
			string val;
			if (properties.TryGetValue (name, out val))
				return val;
			else
				return Environment.GetEnvironmentVariable (name);
		}

		public string GetMetadataValue (string name)
		{
			if (itemFile == null)
				return "";

			switch (name.ToLower ()) {
			case "fullpath": return ToMSBuildPath (Path.GetFullPath (itemFile));
			case "rootdir": return ToMSBuildDir (Path.GetPathRoot (itemFile));
			case "filename": return Path.GetFileNameWithoutExtension (itemFile);
			case "extension": return Path.GetExtension (itemFile);
			case "relativedir": return ToMSBuildDir (new FilePath (itemFile).ToRelative (project.BaseDirectory).ParentDirectory);
			case "directory": {
					var root = Path.GetPathRoot (itemFile);
					if (!string.IsNullOrEmpty (root))
						return ToMSBuildDir (Path.GetFullPath (itemFile).Substring (root.Length));
					return ToMSBuildDir (Path.GetFullPath (itemFile));
				}
			case "recursivedir": return ToMSBuildDir (recursiveDir);
			case "identity": return ToMSBuildPath (itemFile);
			case "modifiedtime": {
					try {
						return File.GetLastWriteTime (itemFile).ToString ("yyyy-MM-dd hh:mm:ss");
					} catch {
						return "";
					}
				}
			case "createdtime": {
				try {
					return File.GetCreationTime (itemFile).ToString ("yyyy-MM-dd hh:mm:ss");
				} catch {
					return "";
				}
			}
			case "accessedtime": {
					try {
						return File.GetLastAccessTime (itemFile).ToString ("yyyy-MM-dd hh:mm:ss");
					} catch {
						return "";
					}
				}
			}
			return "";
		}

		string ToMSBuildPath (string path)
		{
			return path.Replace ('/','\\');
		}

		string ToMSBuildDir (string path)
		{
			path = path.Replace ('/','\\');
			if (!path.EndsWith ("\\", StringComparison.Ordinal))
				path = path + '\\';
			return path;
		}

		public void SetPropertyValue (string name, string value)
		{
			properties [name] = value;
		}

		public void ClearPropertyValue (string name)
		{
			properties.Remove (name);
		}

		public bool Evaluate (XmlElement source, out XmlElement result)
		{
			allResolved = true;
			result = (XmlElement) EvaluateNode (source);
			return allResolved;
		}

		XmlNode EvaluateNode (XmlNode source)
		{
			var elemSource = source as XmlElement;
			if (elemSource != null) {
				var elem = source.OwnerDocument.CreateElement (elemSource.Prefix, elemSource.LocalName, elemSource.NamespaceURI);
				foreach (XmlAttribute attr in elemSource.Attributes)
					elem.Attributes.Append ((XmlAttribute)EvaluateNode (attr));
				foreach (XmlNode child in elemSource.ChildNodes)
					elem.AppendChild (EvaluateNode (child));
				return elem;
			}

			var attSource = source as XmlAttribute;
			if (attSource != null) {
				bool oldResolved = allResolved;
				var att = source.OwnerDocument.CreateAttribute (attSource.Prefix, attSource.LocalName, attSource.NamespaceURI);
				att.Value = Evaluate (attSource.Value);

				// Condition attributes don't change the resolution status. Conditions are handled in the property and item objects
				if (attSource.Name == "Condition")
					allResolved = oldResolved;

				return att;
			}
			var textSource = source as XmlText;
			if (textSource != null) {
				return source.OwnerDocument.CreateTextNode (Evaluate (textSource.InnerText));
			}
			return source.Clone ();
		}

		public bool Evaluate (string str, out string result)
		{
			allResolved = true;
			result = Evaluate (str);
			return allResolved;
		}

		static char[] tagStart = new [] {'$','%'};

		string Evaluate (string str)
		{
			int i = FindNextTag (str, 0);
			if (i == -1)
				return str;

			int last = 0;

			StringBuilder sb = new StringBuilder ();
			do {
				var tag = str[i];
				sb.Append (str, last, i - last);
				i += 2;
				int j = str.IndexOf (")", i);
				if (j == -1) {
					allResolved = false;
					return str;
				}

				string prop = str.Substring (i, j - i);
				string val = tag == '$' ? GetPropertyValue (prop) : GetMetadataValue (prop);
				if (val == null) {
					allResolved = false;
					val = string.Empty;
				}

				sb.Append (val);
				last = j + 1;

				i = FindNextTag (str, last);
			}
			while (i != -1);

			sb.Append (str, last, str.Length - last);
			return sb.ToString ();
		}

		int FindNextTag (string str, int i)
		{
			do {
				i = str.IndexOfAny (tagStart, i);
				if (i == -1 || i == str.Length - 1)
					break;
				if (str[i + 1] == '(')
					return i;
				i++;
			} while (i < str.Length);

			return -1;
		}

		#region IExpressionContext implementation

		public string EvaluateString (string value)
		{
			if (value.StartsWith ("$(") && value.EndsWith (")"))
				return GetPropertyValue (value.Substring (2, value.Length - 3)) ?? value;
			else
				return value;
		}

		public string FullFileName {
			get {
				return project.FileName;
			}
		}

		#endregion
	}
}
