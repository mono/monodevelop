﻿//
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
using MonoDevelop.Core;
using System.Reflection;
using Microsoft.Build.Utilities;
using MonoDevelop.Projects.MSBuild.Conditions;
using System.Globalization;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using MonoDevelop.Projects.Extensions;
using System.Collections;

namespace MonoDevelop.Projects.MSBuild
{
	sealed class MSBuildEvaluationContext: IExpressionContext
	{
		Dictionary<string,string> properties = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		readonly IDictionary envVars;
		readonly HashSet<string> propertiesWithTransforms;
		readonly List<string> propertiesWithTransformsSorted;
		List<ImportSearchPathExtensionNode> searchPaths;
		HashSet<string> nestedImportFiles;

		public Dictionary<string, bool> ExistsEvaluationCache { get; }

		bool allResolved;
		MSBuildProject project;
		MSBuildEvaluationContext parentContext;
		IMSBuildPropertyGroupEvaluated itemMetadata;
		string directoryName;

		string itemInclude;
		string itemFile;
		string recursiveDir;

		public MSBuildEngineLogger Log { get; set; }

		public MSBuildEvaluationContext ()
		{
			ExistsEvaluationCache = new Dictionary<string, bool> ();
			propertiesWithTransforms = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
			propertiesWithTransformsSorted = new List<string> ();
			envVars = Environment.GetEnvironmentVariables ();
			nestedImportFiles = new HashSet<string> ();
		}

		public MSBuildEvaluationContext (MSBuildEvaluationContext parentContext)
		{
			this.parentContext = parentContext;
			this.project = parentContext.project;
			this.Log = parentContext.Log;

			this.ExistsEvaluationCache = parentContext.ExistsEvaluationCache;
			this.propertiesWithTransforms = parentContext.propertiesWithTransforms;
			this.propertiesWithTransformsSorted = parentContext.propertiesWithTransformsSorted;
			this.envVars = parentContext.envVars;
			this.nestedImportFiles = parentContext.nestedImportFiles;
		}

		internal void InitEvaluation (MSBuildProject project)
		{
			this.project = project;

			// Project file properties

			properties.Add ("MSBuildThisFile", Path.GetFileName (project.FileName));
			properties.Add ("MSBuildThisFileName", project.FileName.FileNameWithoutExtension);
			properties.Add ("MSBuildThisFileExtension", Path.GetExtension (project.FileName));
			properties.Add ("MSBuildThisFileFullPath", MSBuildProjectService.ToMSBuildPath (null, project.FileName.FullPath));

			string dir = Path.GetDirectoryName (project.FileName) + Path.DirectorySeparatorChar;
			properties.Add ("MSBuildThisFileDirectory", MSBuildProjectService.ToMSBuildPath (null, dir));
			properties.Add ("MSBuildThisFileDirectoryNoRoot", MSBuildProjectService.ToMSBuildPath (null, dir.Substring (Path.GetPathRoot (dir).Length)));

			// Properties only set for the root project, not for imported projects

			if (parentContext == null) {
				properties.Add ("VisualStudioReferenceAssemblyVersion", project.ToolsVersion + ".0.0");
				properties.Add ("MSBuildProjectDefaultTargets", project.DefaultTargets);
				properties.Add ("MSBuildProjectExtension", Path.GetExtension (project.FileName));
				properties.Add ("MSBuildProjectFile", project.FileName.FileName);
				properties.Add ("MSBuildProjectFullPath", MSBuildProjectService.ToMSBuildPath (null, project.FileName.FullPath.ToString ()));
				properties.Add ("MSBuildProjectName", project.FileName.FileNameWithoutExtension);

				dir = project.BaseDirectory.IsNullOrEmpty ? Environment.CurrentDirectory : project.BaseDirectory.ToString ();
				properties.Add ("MSBuildProjectDirectory", MSBuildProjectService.ToMSBuildPath (null, dir));
				properties.Add ("MSBuildProjectDirectoryNoRoot", MSBuildProjectService.ToMSBuildPath (null, dir.Substring (Path.GetPathRoot (dir).Length)));

				InitEngineProperties (project.TargetRuntime ?? Runtime.SystemAssemblyService.DefaultRuntime, properties, out searchPaths);
			}
		}

		static void InitEngineProperties (Core.Assemblies.TargetRuntime runtime, Dictionary<string, string> properties, out List<ImportSearchPathExtensionNode> searchPaths)
		{
			string toolsVersion = "Current";
			string visualStudioVersion = "16.0";

			var toolsPath = runtime.GetMSBuildToolsPath (toolsVersion);
			if (toolsPath == null) {
				toolsVersion = "15.0";
				visualStudioVersion = toolsVersion;
				toolsPath = runtime.GetMSBuildToolsPath (toolsVersion);
			}

			properties.Add ("MSBuildAssemblyVersion", toolsVersion);
			//VisualStudioVersion is a property set by MSBuild itself
			properties.Add ("VisualStudioVersion", visualStudioVersion);

			var msBuildBinPath = toolsPath;
			var msBuildBinPathEscaped = MSBuildProjectService.ToMSBuildPath (null, msBuildBinPath);
			properties.Add ("MSBuildBinPath", msBuildBinPathEscaped);
			properties.Add ("MSBuildToolsPath", msBuildBinPathEscaped);
			properties.Add ("MSBuildBinPath32", msBuildBinPathEscaped);
			properties.Add ("MSBuildRuntimeVersion", "4.0.30319");

			properties.Add ("MSBuildToolsRoot", MSBuildProjectService.ToMSBuildPath (null, Path.GetDirectoryName (toolsPath)));
			properties.Add ("MSBuildToolsVersion", toolsVersion);
			properties.Add ("OS", Platform.IsWindows ? "Windows_NT" : "Unix");

			var frameworkToolsPath = ToolLocationHelper.GetPathToDotNetFramework (TargetDotNetFrameworkVersion.VersionLatest);
			var frameworkToolsPathEscaped = MSBuildProjectService.ToMSBuildPath (null, frameworkToolsPath);
			properties.Add ("MSBuildFrameworkToolsPath", frameworkToolsPathEscaped);
			properties.Add ("MSBuildFrameworkToolsPath32", frameworkToolsPathEscaped);

			searchPaths = MSBuildProjectService.GetProjectImportSearchPaths (runtime, true).ToList ();

			if (Platform.IsWindows) {
				//first use extensions path relative to bindir (MSBuild/15.0/Bin). this works for dev15 isolated install.
				var msBuildExtensionsPath = Path.GetFullPath (Path.Combine (msBuildBinPath, "..", ".."));
				var msBuildExtensionsPathEscaped = MSBuildProjectService.ToMSBuildPath (null, msBuildExtensionsPath);
				properties.Add ("MSBuildExtensionsPath", msBuildExtensionsPathEscaped);
				properties.Add ("MSBuildExtensionsPath32", msBuildExtensionsPathEscaped);
				properties.Add ("MSBuildExtensionsPath64", msBuildExtensionsPathEscaped);

				var vsToolsPathEscaped = MSBuildProjectService.ToMSBuildPath (null, Path.Combine (msBuildExtensionsPath, "Microsoft", "VisualStudio", "v" + visualStudioVersion));
				properties.Add ("VSToolsPath", vsToolsPathEscaped);

				//like the dev15 toolset, add fallbacks to the old global paths

				// Taken from MSBuild source:
				var programFiles = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles);
				var programFiles32 = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86);
				if (string.IsNullOrEmpty (programFiles32))
					programFiles32 = programFiles; // 32 bit box

				string programFiles64;
				if (programFiles == programFiles32) {
					// either we're in a 32-bit window, or we're on a 32-bit machine.  
					// if we're on a 32-bit machine, ProgramW6432 won't exist
					// if we're on a 64-bit machine, ProgramW6432 will point to the correct Program Files. 
					programFiles64 = Environment.GetEnvironmentVariable ("ProgramW6432");
				}
				else {
					// 64-bit window on a 64-bit machine; %ProgramFiles% points to the 64-bit 
					// Program Files already. 
					programFiles64 = programFiles;
				}

				var extensionsPath32Escaped = MSBuildProjectService.ToMSBuildPath (null, Path.Combine (programFiles32, "MSBuild"));
				searchPaths.Insert (0, new ImportSearchPathExtensionNode { Property = "MSBuildExtensionsPath32", Path = extensionsPath32Escaped });

				if (programFiles64 != null) {
					var extensionsPath64Escaped = MSBuildProjectService.ToMSBuildPath (null, Path.Combine (programFiles64, "MSBuild"));
					searchPaths.Insert (0, new ImportSearchPathExtensionNode { Property = "MSBuildExtensionsPath64", Path = extensionsPath64Escaped });
				}

				//yes, dev15's toolset has the 64-bit path fall back to the 32-bit one
				searchPaths.Insert (0, new ImportSearchPathExtensionNode { Property = "MSBuildExtensionsPath64", Path = extensionsPath32Escaped });

				// MSBuildExtensionsPath:  The way this used to work is that it would point to "Program Files\MSBuild" on both 
				// 32-bit and 64-bit machines.  We have a switch to continue using that behavior; however the default is now for
				// MSBuildExtensionsPath to always point to the same location as MSBuildExtensionsPath32. 

				bool useLegacyMSBuildExtensionsPathBehavior = !string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("MSBUILDLEGACYEXTENSIONSPATH"));

				string extensionsPath;
				if (useLegacyMSBuildExtensionsPathBehavior)
					extensionsPath = Path.Combine (programFiles, "MSBuild");
				else
					extensionsPath = extensionsPath32Escaped;
				searchPaths.Insert (0, new ImportSearchPathExtensionNode { Property = "MSBuildExtensionsPath", Path = extensionsPath });
			}
			else {
				var msBuildExtensionsPath = runtime.GetMSBuildExtensionsPath ();
				var msBuildExtensionsPathEscaped = MSBuildProjectService.ToMSBuildPath (null, msBuildExtensionsPath);
				properties.Add ("MSBuildExtensionsPath", msBuildExtensionsPathEscaped);
				properties.Add ("MSBuildExtensionsPath32", msBuildExtensionsPathEscaped);
				properties.Add ("MSBuildExtensionsPath64", msBuildExtensionsPathEscaped);

				var vsToolsPathEscaped = MSBuildProjectService.ToMSBuildPath (null, Path.Combine (msBuildExtensionsPath, "Microsoft", "VisualStudio", "v" + visualStudioVersion));
				properties.Add ("VSToolsPath", vsToolsPathEscaped);
			}

			// Environment

			properties.Add ("MSBuildProgramFiles32", MSBuildProjectService.ToMSBuildPath (null, Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86)));

			// Custom override of MSBuildExtensionsPath using an env var

			var customExtensionsPath = Environment.GetEnvironmentVariable ("MSBuildExtensionsPath");
			if (!string.IsNullOrEmpty (customExtensionsPath)) {
				if (IsExternalMSBuildExtensionsPath (customExtensionsPath))
					// This is actually an override of the mono extensions path. Don't replace the default MSBuildExtensionsPath value since
					// core targets still need to be loaded from there.
					searchPaths.Insert (0, new ImportSearchPathExtensionNode { Property = "MSBuildExtensionsPath", Path = customExtensionsPath });
				else
					properties["MSBuildExtensionsPath"] = MSBuildProjectService.ToMSBuildPath (null, customExtensionsPath);
			}
		}

		public MSBuildProject Project {
			get { return project; }
		}

		public IReadOnlyList<ImportSearchPathExtensionNode> GetProjectImportSearchPaths ()
		{
			if (parentContext != null)
				return parentContext.GetProjectImportSearchPaths ();
			return searchPaths;
		}

		static bool IsExternalMSBuildExtensionsPath (string path)
		{
			// Mono has a hack to allow loading msbuild targets from different locations. Besides the default location
			// inside Mono, targets are also loaded from /Library/Frameworks/Mono.framework/External/xbuild.
			// When evaluating an msbuild project, MSBuildExtensionsPath is replaced by those locations.

			// This check is a workaround for a special corner case that happens, for example, when building the Xamarin SDKs.
			// In that case, the MSBuildExtensionsPath env var is specified to point to a local version of the msbuild targets
			// that are usually installed in /Library/Frameworks/Mono.framework/External/xbuild.
			// However, by setting this variable, the new value replaces the default build target location, and msbuild
			// can't finde Microsoft.Common.props.

			// This check is done to avoid overwriting the default msbuild builds target location when what is being
			// specified in MSBuildExtensionsPath is actually a path that intends to replace the external build
			// targets path.

			return Platform.IsMac && path.Contains ("Mono.framework/External/xbuild");
		}

		internal void SetItemContext (string itemInclude, string itemFile, string recursiveDir, IMSBuildPropertyGroupEvaluated metadata = null)
		{
			this.itemInclude = itemInclude;
			this.itemFile = itemFile;
			this.recursiveDir = recursiveDir;
			this.itemMetadata = metadata;
		}

		internal void ClearItemContext ()
		{
			this.itemInclude = null;
			this.itemFile = null;
			this.recursiveDir = null;
			this.itemMetadata = null;
		}

		string GetPropertyValue (string name)
		{
			string val;
			if (properties.TryGetValue (name, out val))
				return val;
			if (parentContext != null)
				return parentContext.GetPropertyValue (name);

			return (string)envVars [name];
		}

		public string GetMetadataValue (string name)
		{
			// First of all check if the metadata is explicitly set
			if (itemMetadata != null && itemMetadata.HasProperty (name))
				return itemMetadata.GetValue (name, "");

			// Now check for file metadata. We avoid a FromMSBuildPath call by checking after item metadata
			if (itemFile == null && itemInclude != null)
				itemFile = MSBuildProjectService.FromMSBuildPath (project.BaseDirectory, itemInclude);
			
			if (itemFile == null)
				return "";

			try {
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
				case "recursivedir": return recursiveDir != null ? ToMSBuildDir (recursiveDir) : "";
				case "identity": return ToMSBuildPath (itemFile);
				case "modifiedtime": {
						if (!File.Exists (itemFile))
							return "";
						return File.GetLastWriteTime (itemFile).ToString ("yyyy-MM-dd hh:mm:ss");
					}
				case "createdtime": {
						if (!File.Exists (itemFile))
							return "";
						return File.GetCreationTime (itemFile).ToString ("yyyy-MM-dd hh:mm:ss");
					}
				case "accessedtime": {
						if (!File.Exists (itemFile))
							return "";
						return File.GetLastAccessTime (itemFile).ToString ("yyyy-MM-dd hh:mm:ss");
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Failure in MSBuild file", ex);
				return "";
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
			if (parentContext != null)
				parentContext.SetPropertyValue (name, value);
			else
				properties [name] = value;
			if (Log != null)
				LogPropertySet (name, value);
		}

		public void SetContextualPropertyValue (string name, string value)
		{
			// Sets a properly value only for the scope of this context, not for the scope of the global evaluation operation
			properties [name] = value;
		}

		public void ClearPropertyValue (string name)
		{
			properties.Remove (name);
			if (parentContext != null)
				parentContext.ClearPropertyValue (name);
		}

		public void SetPropertyNeedsTransformEvaluation (string name)
		{
			if (!propertiesWithTransforms.Add (name))
				propertiesWithTransformsSorted.Remove (name);
			propertiesWithTransformsSorted.Add (name);
		}

		public IEnumerable<String> GetPropertiesNeedingTransformEvaluation ()
		{
			return propertiesWithTransformsSorted;
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

		readonly static char[] tagStart = new [] {'$','%','@'};

		Queue<StringBuilder> evaluationSbs = new Queue<StringBuilder> ();
		StringBuilder GetEvaluationSb ()
		{
			if (evaluationSbs.Count == 0)
				return new StringBuilder ();
			return evaluationSbs.Dequeue ().Clear ();
		}

		string Evaluate (string str, StringBuilder sb, List<MSBuildItemEvaluated> evaluatedItemsCollection, out bool needsItemEvaluation)
		{
			needsItemEvaluation = false;

			if (str == null)
				return null;
			int i = FindNextTag (str, 0);
			if (i == -1)
				return str;

			int last = 0;

			if (sb == null)
				sb = GetEvaluationSb ();

			try {
				do {
					sb.Append (str, last, i - last);
					int j = i;
					object val;
					bool nie;
					if (!EvaluateReference (str.AsSpan (), evaluatedItemsCollection, ref j, out val, out nie))
						allResolved = false;
					needsItemEvaluation |= nie;
					sb.Append (ValueToString (val));
					last = j;

					i = FindNextTag (str, last);
				}
				while (i != -1);

				sb.Append (str, last, str.Length - last);
				return StringInternPool.AddShared (sb);
			} finally {
				evaluationSbs.Enqueue (sb);
			}
		}

		public string Evaluate (string str, StringBuilder sb)
		{
			bool needsItemEvaluation;
			return Evaluate (str, sb, null, out needsItemEvaluation);
		}

		public string EvaluateWithItems (string str, List<MSBuildItemEvaluated> evaluatedItemsCollection)
		{
			bool needsItemEvaluation;
			return Evaluate (str, null, evaluatedItemsCollection, out needsItemEvaluation);
		}

		public string Evaluate (string str)
		{
			bool needsItemEvaluation;
			return Evaluate (str, null, null, out needsItemEvaluation);
		}

		public string Evaluate (string str, out bool needsItemEvaluation)
		{
			return Evaluate (str, null, null, out needsItemEvaluation);
		}

		bool EvaluateReference (ReadOnlySpan<char> str, List<MSBuildItemEvaluated> evaluatedItemsCollection, ref int i, out object val, out bool needsItemEvaluation)
		{
			needsItemEvaluation = false;

			val = null;
			var tag = str[i];
			int start = i;

			i += 2;
			int j = FindClosingChar (str, i, ')');
			if (j == -1) {
				i = str.Length;
				str = str.Slice (start);
				val = StringInternPool.AddShared (str.ToString ());
				return false;
			}

			var propSpan = str.Slice (i, j - i).Trim ();
			i = j + 1;

			bool res = false;
			if (propSpan.Length > 0) {
				switch (tag) {
					case '$': {
						bool nie;
						res = EvaluateProperty (propSpan, evaluatedItemsCollection != null, out val, out nie);
						needsItemEvaluation |= nie;
						break;
					}
				case '%':
					string prop = StringInternPool.AddShared (propSpan.ToString ());
					res = EvaluateMetadata (prop, out val);
					break;
				case '@':
					if (evaluatedItemsCollection != null)
						res = EvaluateList (propSpan, evaluatedItemsCollection, out val);
					else {
						res = false;
						needsItemEvaluation = true;
					}
					break;
				}
			}
			if (!res)
				val = StringInternPool.AddShared (str.Slice (start, j - start + 1).ToString ());

			return res;
		}

		string ValueToString (object ob)
		{
			return ob != null ? Convert.ToString (ob, CultureInfo.InvariantCulture) : string.Empty;
		}

		static char [] dotOrBracket = { '.', '[' };

		bool EvaluateProperty (ReadOnlySpan<char> prop, bool ignorePropsWithTransforms, out object val, out bool needsItemEvaluation)
		{
			needsItemEvaluation = false;
			val = null;
			if (prop [0] == '[') {
				int i = prop.IndexOf (']');
				if (i == -1 || (prop.Length - i) < 3 || prop [i + 1] != ':' || prop [i + 2] != ':')
					return false;
				var typeName = prop.Slice (1, i - 1).Trim ();
				if (typeName.Length == 0)
					return false;
				var type = ResolveType (typeName.ToString ());
				if (type == null)
					return false;
				i += 3;
				return EvaluateMember (type, null, prop, i, out val);
			}

			int n = prop.IndexOfAny (dotOrBracket);

			if (n == -1) {
				var propString = prop.ToString ();
				needsItemEvaluation |= (!ignorePropsWithTransforms && propertiesWithTransforms.Contains (propString));
				val = GetPropertyValue (propString) ?? string.Empty;
				return true;
			} else {
				var pn = prop.Slice (0, n).ToString ();
				val = GetPropertyValue (pn) ?? string.Empty;
				return EvaluateMemberOrIndexer (typeof (string), val, prop, n, out val);
			}
		}

		bool EvaluateMemberOrIndexer (Type type, object instance, ReadOnlySpan<char> str, int i, out object val)
		{
			// Position in string is either a '.' or a '['.

			val = null;
			if (i >= str.Length)
				return false;
			if (str [i] == '.') {
				return EvaluateMember (type, instance, str, i + 1, out val);
			} else if (str [i] == '[') {
				return EvaluateIndexer (type, instance, str, i, out val);
			}
			return false;
		}

		static readonly char[] MemberDelimiter = new[] { '.', ')', '(' };
		internal bool EvaluateMember (Type type, object instance, ReadOnlySpan<char> str, int i, out object val)
		{
			val = null;

			// Find the delimiter of the member
			int j = str.IndexOfAny (MemberDelimiter, i);
			if (j == -1)
				j = str.Length;

			var memberName = str.Slice (i, j - i).Trim ();
			if (memberName.Length == 0)
				return false;

			if (j < str.Length && str[j] == '(') {
				// It is a method invocation
				object [] parameterValues;
				j++;
				if (!EvaluateParameters (str, ref j, out parameterValues))
					return false;

				var member = ResolveMember (type, memberName.ToString (), instance == null, MemberTypes.Method);
				if (member == null || member.Length == 0)
					return false;

				if (!EvaluateMethod (str, member, instance, parameterValues, out val))
					return false;
				
				// Skip the closing parens
				j++;

			} else {
				// It has to be a property or field
				try {
					var member = ResolveMember (type, memberName.ToString (), instance == null, MemberTypes.Property | MemberTypes.Field);
					if (member == null || member.Length == 0)
						return false;

					if (member[0] is PropertyInfo)
						val = ((PropertyInfo)member[0]).GetValue (instance);
					else if (member[0] is FieldInfo)
						val = ((FieldInfo)member[0]).GetValue (instance);
					else
						return false;
				} catch (Exception ex) {
					LoggingService.LogError ("MSBuild property evaluation failed: " + str.ToString (), ex);
					return false;
				}
			}
			if (j < str.Length) {
				// Chained member invocation
				if (val == null)
					return false;
				return EvaluateMemberOrIndexer (val.GetType (), val, str, j, out val);
			}
			return true;
		}

		bool EvaluateIndexer (Type type, object instance, ReadOnlySpan<char> str, int i, out object val)
		{
			val = null;
			object [] parameters;
			i++;
			if (!EvaluateParameters (str, ref i, out parameters))
				return false;
			if (parameters.Length != 1)
				return false;
			
			var index = Convert.ToInt32 (parameters [0]);
			if (instance is string) {
				val = ((string)instance) [index];
			}
			else if (instance is IList array) {
				val = array[index];
			} else
				return false;

			if (++i < str.Length) {
				// Chained member invocation
				return EvaluateMemberOrIndexer (val.GetType (), val, str, i, out val);
			}
			return true;
		}

		internal bool EvaluateMember (ReadOnlySpan<char> str, Type type, string memberName, object instance, object [] parameterValues, out object val)
		{
			val = null;
			var member = ResolveMember (type, memberName, instance == null, MemberTypes.Method);
			if (member == null || member.Length == 0)
				return false;
			return EvaluateMethod (str, member, instance, parameterValues, out val);
		}

		bool EvaluateMethod (ReadOnlySpan<char> str, MemberInfo[] member, object instance, object [] parameterValues, out object val)
		{
			val = null;

			// Find a method with a matching number of parameters
			var (method, methodParams) = FindBestOverload (member, parameterValues, out var paramsArgType);
			if (method == null)
				return false;

			try {
				// Convert the given parameters to the types specified in the method signature
				var convertedArgs = (methodParams.Length == parameterValues.Length) ? parameterValues : new object [methodParams.Length];

				int numArgs = methodParams.Length;
				if (paramsArgType != null)
					numArgs--;

				if (method.DeclaringType == typeof (IntrinsicFunctions) && method.Name == nameof (IntrinsicFunctions.GetPathOfFileAbove) && parameterValues.Length == methodParams.Length - 1) {
					string startingDirectory = String.IsNullOrWhiteSpace (FullFileName) ? String.Empty : Path.GetDirectoryName (FullFileName);
					var last = convertedArgs.Length - 1;
					convertedArgs [last] = ConvertArg (method, last, startingDirectory, methodParams [last].ParameterType);
					numArgs = 1;
				}

				int n;
				for (n = 0; n < numArgs; n++)
					convertedArgs [n] = ConvertArg (method, n, parameterValues [n], methodParams [n].ParameterType);

				if (methodParams.Length == parameterValues.Length && paramsArgType != null) {
					// Invoking an method with a params argument, but the number of arguments provided is the same as the
					// number of arguments of the method, so the last argument can be either one of the values of the
					// params array, or it can be the whole params array. 
					try {
						var last = convertedArgs.Length - 1;
						convertedArgs [last] = ConvertArg (method, last, parameterValues [last], methodParams [last].ParameterType);

						// Conversion worked. Ignore the params argument.
						paramsArgType = null;
					} catch (InvalidCastException) {
						// Conversion of the last argument failed, so it probably needs to be handled as a single value
						// for the params argument.
					}
				}

				if (paramsArgType != null) {
					var argsArray = Array.CreateInstance (paramsArgType, parameterValues.Length - numArgs);
					for (int m = 0; m < argsArray.Length; m++)
						argsArray.SetValue (ConvertArg (method, n, parameterValues [n++], paramsArgType), m);
					convertedArgs [convertedArgs.Length - 1] = argsArray;
				}

				// Invoke the method
				val = method.Invoke (instance, convertedArgs);
			} catch (Exception ex) {
				LoggingService.LogError ("MSBuild property evaluation failed: " + str.ToString (), ex);
				return false;
			}
			return true;
		}

		static char[] parameterCloseChars = new[] { ',', ')', ']' };
		internal bool EvaluateParameters (ReadOnlySpan<char> str, ref int i, out object[] parameters)
		{
			parameters = null;
			var list = new List<object> ();

			while (i < str.Length) {
				var j = FindClosingChar (str, i, parameterCloseChars);
				if (j == -1)
					return false;

				var foundListEnd = str [j] == ')' || str [j] == ']';
				var arg = str.Slice (i, j - i).Trim ();

				if (arg.Length == 0 && foundListEnd && list.Count == 0) {
					// Empty parameters list
					parameters = new object [0];
					i = j;
					return true;
				}

				// Trim enclosing quotation marks
				if (arg.Length > 1 && IsQuote(arg [0]) && arg[arg.Length - 1] == arg [0])
					arg = arg.Slice (1, arg.Length - 2);

				list.Add (Evaluate (arg.ToString ()));

				if (foundListEnd) {
					// End of parameters list
					parameters = list.ToArray ();
					i = j;
					return true;
				}
				i = j + 1;
			}
			return false;
		}

		(MethodBase method, ParameterInfo[] parameters) FindBestOverload (IEnumerable<MemberInfo> members, object [] args, out Type paramsArgType)
		{
			(MethodBase, ParameterInfo[]) methodWithParams = default;
			(MethodBase, ParameterInfo[]) validMatch = default;

			paramsArgType = null;

			foreach (var member in members) {
				if (!(member is MethodBase m))
					continue;

				var argInfo = m.GetParameters ();

				if (args.Length == argInfo.Length - 1) {
					if (m.DeclaringType == typeof (IntrinsicFunctions) && m.Name == nameof (IntrinsicFunctions.GetPathOfFileAbove)) {
						validMatch = (m, argInfo);
						continue;
					}
				}

				// Unable to match in this case.
				if (args.Length < argInfo.Length - 1)
					continue;

				var kind = MatchArgs (args, argInfo);
				if (kind == MatchKind.Exact)
					return (m, argInfo);

				if (kind == MatchKind.CanConvert)
					validMatch = (m, argInfo);
				else if (kind == MatchKind.Params) {
					methodWithParams = (m, argInfo);
					paramsArgType = argInfo [argInfo.Length - 1].ParameterType.GetElementType ();
				}
			}

			return validMatch != default ? validMatch : methodWithParams;
		}

		enum MatchKind
		{
			None,
			Params,
			CanConvert,
			Exact,
		}

		static MatchKind MatchArgs (object[] args, ParameterInfo[] parameters)
		{
			bool isParams = parameters.Length > 0 && IsParamsArg (parameters [parameters.Length - 1]);

			int last = parameters.Length;
			if (isParams)
				last--;
			else if (args.Length != parameters.Length)
				return MatchKind.None;

			var kind = MatchKind.Exact;
			for (int n = 0; n < last; n++) {
				var parameterType = parameters [n].ParameterType;

				var other = Match (parameterType, args [n]);
				if (other == MatchKind.None)
					return MatchKind.None;

				if (other == MatchKind.CanConvert)
					kind = MatchKind.CanConvert;
			}

			if (!isParams)
				return kind;

			// Check implicit argument
			if (args.Length == last)
				return MatchKind.Params;

			var elementType = parameters[last].ParameterType.GetElementType ();
			if (IsComplexType (elementType))
				return MatchKind.None;

			int argsRemaining = args.Length - last;
			if (argsRemaining == 1 && elementType == typeof(char)) {
				if (Match (parameters [last].ParameterType, args [last], checkComplexType: false) != MatchKind.None)
					return MatchKind.Params;
			}

			for (int n_arg = last; n_arg < args.Length; ++n_arg) {
				if (Match (elementType, args [n_arg]) == MatchKind.None)
					return MatchKind.None;
			}

			return MatchKind.Params;

			static bool IsComplexType (Type type)
			{
				return Type.GetTypeCode (type) == TypeCode.Object && type != typeof (object);
			}

			static MatchKind Match (Type parameterType, object argument, bool checkComplexType = true)
			{
				if (parameterType.IsInstanceOfType (argument))
					return MatchKind.Exact;

				if (checkComplexType && IsComplexType (parameterType))
					return MatchKind.None;

				if (CanConvertArg (argument, parameterType))
					return MatchKind.CanConvert;


				return MatchKind.None;
			}
		}

		static bool IsParamsArg (ParameterInfo pi)
		{
			return pi.ParameterType.IsArray && pi.IsDefined (typeof (ParamArrayAttribute));
		}

		static bool CanConvertArg (object value, Type parameterType)
		{
			var sval = value as string;
			if (sval == "null" || value == null)
				return !parameterType.IsValueType || Nullable.GetUnderlyingType (parameterType) != null;

			if (sval != null && parameterType == typeof (char []))
				return true;

			if (sval != null && sval.Length != 1 && parameterType == typeof (char)) {
				return false;
			}

			if (sval != null && parameterType.IsEnum) {
				// Enum.Parse expects comma separated values.
				var enumValue = sval.Replace ('|', ',')
					.Replace (parameterType.FullName + ".", "")
					.Replace (parameterType.Name + ".", "");

				try {
					_ = Enum.Parse (parameterType, enumValue, ignoreCase: true);
					return true;
				} catch {
					return false;
				}
			}

			try {
				_ = Convert.ChangeType (value, parameterType, CultureInfo.InvariantCulture);
				return true;
			} catch {
				return false;
			}
		}

		object ConvertArg (MethodBase method, int argNum, object value, Type parameterType)
		{
			var sval = value as string;
			if (sval == "null" || value == null)
				return null;

			if (sval != null && parameterType == typeof (char[]))
				return sval.ToCharArray ();

			if (sval != null && parameterType.IsEnum) {
				// Enum.Parse expects comma separated values.
				var enumValue = sval.Replace ('|', ',')
					.Replace (parameterType.FullName + ".", "")
					.Replace (parameterType.Name + ".", "");

				return Enum.Parse(parameterType, enumValue, ignoreCase: true);
			}

			if (sval != null && Path.DirectorySeparatorChar != '\\')
				value = sval.Replace ('\\', Path.DirectorySeparatorChar);
			
			var res = Convert.ChangeType (value, parameterType, CultureInfo.InvariantCulture);
			bool convertPath = false;

			if ((method.DeclaringType == typeof (System.IO.File) || method.DeclaringType == typeof (System.IO.Directory)) && argNum == 0)
				convertPath = true;
			else if (method.DeclaringType == typeof (System.IO.Path))
				// The windows path is already converted to a native path, but it may contain escape sequences
				res = MSBuildProjectService.UnescapePath ((string)res);
			else if (method.DeclaringType == typeof (IntrinsicFunctions)) {
				if (method.Name == "MakeRelative")
					convertPath = true;
				else if (method.Name == "GetDirectoryNameOfFileAbove" && argNum == 0)
					convertPath = true;
			}

			// The argument is a path. Convert to native path and make absolute
			if (convertPath)
				res = MSBuildProjectService.FromMSBuildPath (project.BaseDirectory, (string)res);
			
			return res;
		}

		bool EvaluateMetadata (string prop, out object val)
		{
			val = GetMetadataValue (prop);
			return val != null;
		}

		bool EvaluateList (ReadOnlySpan<char> prop, List<MSBuildItemEvaluated> evaluatedItemsCollection, out object val)
		{
			string items;
			var res = DefaultMSBuildEngine.ExecuteStringTransform (evaluatedItemsCollection, this, prop, out items);
			val = items;
			return res;
		}

		Type ResolveType (string typeName)
		{
			if (typeName == "MSBuild")
				return typeof (Microsoft.Build.Evaluation.IntrinsicFunctions);

			foreach (var kvp in supportedTypeMembers) {
				if (kvp.Key.FullName == typeName)
					return kvp.Key;
			}
			return null;
		}

		static readonly Dictionary<string, MethodInfo[]> cachedIntrinsicFunctions = typeof (IntrinsicFunctions)
			.GetMethods (BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Static)
			.ToLookup (x => x.Name)
			.ToDictionary(x => x.Key, x => x.ToArray (), StringComparer.OrdinalIgnoreCase);

		MemberInfo[] ResolveMember (Type type, string memberName, bool isStatic, MemberTypes memberTypes)
		{
			if (type == typeof (string)) {
				if (memberName == "new" || memberName == "Copy") {
					type = typeof (IntrinsicFunctions);
					memberName = "Copy";
				}
			} else {
				if (type.IsArray)
					type = typeof (Array);
			}

			if (type == typeof(IntrinsicFunctions)) {
				return cachedIntrinsicFunctions.TryGetValue (memberName, out var result) ? result : null;
			}

			var flags = isStatic ? BindingFlags.Static : BindingFlags.Instance;
			if (!supportedTypeMembers.TryGetValue (type, out var list))
				return null;

			if (list != null && !list.Contains (memberName))
				return null;

			return type.GetMember (memberName, memberTypes, flags | BindingFlags.Public | BindingFlags.IgnoreCase);
		}

		sealed class TypeEqualityComparer : IEqualityComparer<Type>
		{
			public bool Equals (Type x, Type y) => x == y;

			public int GetHashCode (Type obj) => obj?.GetHashCode () ?? 0;
		}

		static readonly Dictionary<Type, string []> supportedTypeMembers = new Dictionary<Type, string []> (new TypeEqualityComparer()) {
			{ typeof(System.Array), null },
			{ typeof(System.Byte), null },
			{ typeof(System.Char), null },
			{ typeof(System.Convert), null },
			{ typeof(System.DateTime), null },
			{ typeof(System.Decimal), null },
			{ typeof(System.Double), null },
			{ typeof(System.Enum), null },
			{ typeof(System.Guid), null },
			{ typeof(System.Int16), null },
			{ typeof(System.Int32), null },
			{ typeof(System.Int64), null },
			{ typeof(System.IO.Path), null },
			{ typeof(System.Math), null },
			{ typeof(System.UInt16), null },
			{ typeof(System.UInt32), null },
			{ typeof(System.UInt64), null },
			{ typeof(System.SByte), null },
			{ typeof(System.Single), null },
			{ typeof(System.String), null },
			{ typeof(System.StringComparer), null },
			{ typeof(System.TimeSpan), null },
			{ typeof(System.Text.RegularExpressions.Regex), null },
			{ typeof(Microsoft.Build.Utilities.ToolLocationHelper), null },
			{ typeof(System.Globalization.CultureInfo), null },
			{
				typeof (System.Environment),
				new string [] {
					"CommandLine", "ExpandEnvironmentVariables", "GetEnvironmentVariable", "GetEnvironmentVariables", "GetFolderPath", "GetLogicalDrives"
				}
			},
			{
				typeof (System.IO.Directory),
				new string [] {
					"GetDirectories", "GetFiles", "GetLastAccessTime", "GetLastWriteTime", "GetParent"
				}
			},
			{
				typeof (System.IO.File),
				new string [] {
					"Exists", "GetCreationTime", "GetAttributes", "GetLastAccessTime", "GetLastWriteTime", "ReadAllText"
				}
			},
		};

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

		int FindClosingChar (ReadOnlySpan<char> str, int i, char closeChar)
		{
			int pc = 0;
			while (i < str.Length) {
				var c = str [i];
				if (pc == 0 && c == closeChar)
					return i;
				if (c == '(' || c == '[')
					pc++;
				else if (c == ')' || c == ']')
					pc--;
				else if (IsQuote (c)) {
					int start = i + 1;
					i = str.IndexOf (c, start);
					if (i == -1)
						return -1;
				}
				i++;
			}
			return -1;
		}

		static bool IsQuote (char c)
		{
			return c == '"' || c == '\'' || c == '`';
		}

		static int FindClosingChar (ReadOnlySpan<char> str, int i, char[] closeChar)
		{
			int pc = 0;
			while (i < str.Length) {
				var c = str [i];
				if (pc == 0 && Array.IndexOf (closeChar, c) != -1)
					return i;
				if (c == '(' || c == '[')
					pc++;
				else if (c == ')' || c == ']')
					pc--;
				else if (IsQuote (c)) {
					int start = i + 1;
					i = str.IndexOf (c, i + 1);
					if (i == -1)
						return -1;
				}
				i++;
			}
			return -1;
		}

		public string CustomFullDirectoryName { get; set; }

		#region IExpressionContext implementation

		public string EvaluateString (string value)
		{
			return Evaluate (value);
		}

		public string FullFileName {
			get {
				return project.FileName;
			}
		}

		public string FullDirectoryName {
			get {
				if (CustomFullDirectoryName != null)
					return CustomFullDirectoryName;
				if (FullFileName == String.Empty)
					return null;
				if (directoryName == null)
					directoryName = Path.GetDirectoryName (FullFileName);

				return directoryName;
			}
		}

		#endregion

		void LogPropertySet (string key, string value)
		{
			if (Log.Flags.HasFlag (MSBuildLogFlags.Properties))
				Log.LogMessage ($"Set Property: {key} = {value}");
		}

		public void Dump ()
		{
			var allProps = new HashSet<string> ();

			MSBuildEvaluationContext ctx = this;
			while (ctx != null) {
				allProps.UnionWith (ctx.properties.Select (p => p.Key));
				ctx = ctx.parentContext;
			}
			foreach (var v in allProps.OrderBy (s => s))
				Log.LogMessage (string.Format ($"{v,-30} = {GetPropertyValue (v)}"));
		}

		/// <summary>
		/// Registers imports to check for circular dependencies. Once the import has been
		/// evaluated it should be removed using RemoveImport.
		/// </summary>
		public void AddImport (string file)
		{
			if (!nestedImportFiles.Add (file))
				throw new InvalidProjectFileException (GettextCatalog.GetString ("Importing the file \"{0}\" into the file \"{1}\" results in a circular dependency.", file, FullFileName));
		}

		public void RemoveImport (string file)
		{
			nestedImportFiles.Remove (file);
		}
	}
}
