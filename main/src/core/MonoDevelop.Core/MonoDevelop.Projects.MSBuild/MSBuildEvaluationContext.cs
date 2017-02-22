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
using System.Reflection;
using Microsoft.Build.Utilities;
using MonoDevelop.Projects.MSBuild.Conditions;
using System.Globalization;
using Microsoft.Build.Evaluation;
using System.Web.UI.WebControls;

namespace MonoDevelop.Projects.MSBuild
{
	class MSBuildEvaluationContext: IExpressionContext
	{
		Dictionary<string,string> properties = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		static Dictionary<string, string> envVars = new Dictionary<string, string> ();
		HashSet<string> propertiesWithTransforms = new HashSet<string> ();
		List<string> propertiesWithTransformsSorted = new List<string> ();
		public Dictionary<string, bool> ExistsEvaluationCache { get; } = new Dictionary<string, bool> ();

		bool allResolved;
		MSBuildProject project;
		MSBuildEvaluationContext parentContext;
		IMSBuildPropertyGroupEvaluated itemMetadata;
		string directoryName;

		string itemFile;
		string recursiveDir;

		public MSBuildEvaluationContext ()
		{
			propertiesWithTransforms = new HashSet<string> ();
			propertiesWithTransformsSorted = new List<string> ();
		}

		public MSBuildEvaluationContext (MSBuildEvaluationContext parentContext)
		{
			this.parentContext = parentContext;
			this.project = parentContext.project;
			this.propertiesWithTransforms = parentContext.propertiesWithTransforms;
			this.propertiesWithTransformsSorted = parentContext.propertiesWithTransformsSorted;
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
				properties.Add ("MSBuildProjectFullPath", MSBuildProjectService.ToMSBuildPath (null, project.FileName.FullPath.ToString()));
				properties.Add ("MSBuildProjectName", project.FileName.FileNameWithoutExtension);

				dir = project.BaseDirectory.IsNullOrEmpty ? Environment.CurrentDirectory : project.BaseDirectory.ToString();
				properties.Add ("MSBuildProjectDirectory", MSBuildProjectService.ToMSBuildPath (null, dir));
				properties.Add ("MSBuildProjectDirectoryNoRoot", MSBuildProjectService.ToMSBuildPath (null, dir.Substring (Path.GetPathRoot (dir).Length)));

				// This MSBuild loader is v15.0
				string toolsVersion = "15.0";
				properties.Add ("MSBuildAssemblyVersion", "15.0");

				var toolsPath = Runtime.SystemAssemblyService.DefaultRuntime.GetMSBuildToolsPath (toolsVersion);

				var frameworkToolsPath = ToolLocationHelper.GetPathToDotNetFramework (TargetDotNetFrameworkVersion.VersionLatest);

				properties.Add ("MSBuildBinPath", MSBuildProjectService.ToMSBuildPath (null, toolsPath));
				properties.Add ("MSBuildToolsPath", MSBuildProjectService.ToMSBuildPath (null, toolsPath));
				properties.Add ("MSBuildToolsRoot", MSBuildProjectService.ToMSBuildPath (null, Path.GetDirectoryName (toolsPath)));
				properties.Add ("MSBuildToolsVersion", toolsVersion);
				properties.Add ("OS", Platform.IsWindows ? "Windows_NT" : "Unix");

				properties.Add ("MSBuildBinPath32", MSBuildProjectService.ToMSBuildPath (null, toolsPath));

				properties.Add ("MSBuildFrameworkToolsPath", MSBuildProjectService.ToMSBuildPath (null, frameworkToolsPath));
				properties.Add ("MSBuildFrameworkToolsPath32", MSBuildProjectService.ToMSBuildPath (null, frameworkToolsPath));

				if (Platform.IsWindows) {
					// Taken from MSBuild source:
					var programFiles = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles);
					var programFiles32 = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86);
					if (string.IsNullOrEmpty(programFiles32))
						programFiles32 = programFiles; // 32 bit box
					
					string programFiles64;
					if (programFiles == programFiles32) {
						// either we're in a 32-bit window, or we're on a 32-bit machine.  
						// if we're on a 32-bit machine, ProgramW6432 won't exist
						// if we're on a 64-bit machine, ProgramW6432 will point to the correct Program Files. 
						programFiles64 = Environment.GetEnvironmentVariable("ProgramW6432");
					}
					else {
						// 64-bit window on a 64-bit machine; %ProgramFiles% points to the 64-bit 
						// Program Files already. 
						programFiles64 = programFiles;
					}

					var extensionsPath32 = MSBuildProjectService.ToMSBuildPath (null, Path.Combine (programFiles32, "MSBuild"));
					properties.Add ("MSBuildExtensionsPath32", extensionsPath32);

					if (programFiles64 != null)
						properties.Add ("MSBuildExtensionsPath64", MSBuildProjectService.ToMSBuildPath (null, Path.Combine(programFiles64, "MSBuild")));
					
					// MSBuildExtensionsPath:  The way this used to work is that it would point to "Program Files\MSBuild" on both 
					// 32-bit and 64-bit machines.  We have a switch to continue using that behavior; however the default is now for
					// MSBuildExtensionsPath to always point to the same location as MSBuildExtensionsPath32. 

					bool useLegacyMSBuildExtensionsPathBehavior = !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("MSBUILDLEGACYEXTENSIONSPATH"));

					string extensionsPath;
					if (useLegacyMSBuildExtensionsPathBehavior)
						extensionsPath = Path.Combine (programFiles, "MSBuild");
					else
						extensionsPath = extensionsPath32;
					properties.Add ("MSBuildExtensionsPath", extensionsPath);
				}
				else if (!String.IsNullOrEmpty (DefaultExtensionsPath)) {
					var ep = MSBuildProjectService.ToMSBuildPath (null, extensionsPath);
					properties.Add ("MSBuildExtensionsPath", ep);
					properties.Add ("MSBuildExtensionsPath32", ep);
					properties.Add ("MSBuildExtensionsPath64", ep);
				}

				// Environment

				properties.Add ("MSBuildProgramFiles32", MSBuildProjectService.ToMSBuildPath (null, Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86)));
			}
		}

		public MSBuildProject Project {
			get { return project; }
		}

		static string extensionsPath;

		internal static string DefaultExtensionsPath {
			get {
				if (extensionsPath == null)
					extensionsPath = Environment.GetEnvironmentVariable ("MSBuildExtensionsPath");

				if (extensionsPath == null) {
					// NOTE: code from mcs/tools/gacutil/driver.cs
					PropertyInfo gac = typeof (System.Environment).GetProperty (
						"GacPath", BindingFlags.Static | BindingFlags.NonPublic);

					if (gac != null) {
						MethodInfo get_gac = gac.GetGetMethod (true);
						string gac_path = (string) get_gac.Invoke (null, null);
						extensionsPath = Path.GetFullPath (Path.Combine (
							gac_path, Path.Combine ("..", "xbuild")));
					}
				}
				return extensionsPath;
			}
		}

		static string DotConfigExtensionsPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), Path.Combine ("xbuild", "tasks"));
		const string MacOSXExternalXBuildDir = "/Library/Frameworks/Mono.framework/External/xbuild";

		internal static IEnumerable<string> GetApplicableExtensionsPaths ()
		{
			// On windows there is a single extension path, which is already properly defined in the engine
			if (Platform.IsWindows)
				yield return null;
			if (Platform.IsMac)
				yield return MacOSXExternalXBuildDir;
			yield return DotConfigExtensionsPath;
			yield return DefaultExtensionsPath;
		}

		internal void SetItemContext (string itemFile, string recursiveDir, IMSBuildPropertyGroupEvaluated metadata = null)
		{
			this.itemFile = itemFile;
			this.recursiveDir = recursiveDir;
			this.itemMetadata = metadata;
		}

		internal void ClearItemContext ()
		{
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

			lock (envVars) {
				if (!envVars.TryGetValue (name, out val))
					envVars[name] = val = Environment.GetEnvironmentVariable (name);

				return val;
			}
		}

		public string GetMetadataValue (string name)
		{
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
				if (itemMetadata != null)
					return itemMetadata.GetValue (name, "");
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
					if (!EvaluateReference (str, evaluatedItemsCollection, ref j, out val, out nie))
						allResolved = false;
					needsItemEvaluation |= nie;
					sb.Append (ValueToString (val));
					last = j;

					i = FindNextTag (str, last);
				}
				while (i != -1);

				sb.Append (str, last, str.Length - last);
				return sb.ToString ();
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

		bool EvaluateReference (string str, List<MSBuildItemEvaluated> evaluatedItemsCollection, ref int i, out object val, out bool needsItemEvaluation)
		{
			needsItemEvaluation = false;

			val = null;
			var tag = str[i];
			int start = i;

			i += 2;
			int j = FindClosingChar (str, i, ')');
			if (j == -1) {
				val = str.Substring (start);
				i = str.Length;
				return false;
			}

			string prop = str.Substring (i, j - i).Trim ();
			i = j + 1;

			bool res = false;
			if (prop.Length > 0) {
				switch (tag) {
					case '$': {
						bool nie;
						res = EvaluateProperty (prop, evaluatedItemsCollection != null, out val, out nie);
						needsItemEvaluation |= nie;
						break;
					}
				case '%': res = EvaluateMetadata (prop, out val); break;
				case '@':
					if (evaluatedItemsCollection != null)
						res = EvaluateList (prop, evaluatedItemsCollection, out val);
					else {
						res = false;
						needsItemEvaluation = true;
					}
					break;
				}
			}
			if (!res)
				val = str.Substring (start, j - start + 1);
			return res;
		}

		string ValueToString (object ob)
		{
			return ob != null ? Convert.ToString (ob, CultureInfo.InvariantCulture) : string.Empty;
		}

		bool EvaluateProperty (string prop, bool ignorePropsWithTransforms, out object val, out bool needsItemEvaluation)
		{
			needsItemEvaluation = false;
			val = null;
			if (prop [0] == '[') {
				int i = prop.IndexOf (']');
				if (i == -1 || (prop.Length - i) < 3 || prop [i + 1] != ':' || prop [i + 2] != ':')
					return false;
				var typeName = prop.Substring (1, i - 1).Trim ();
				if (typeName.Length == 0)
					return false;
				var type = ResolveType (typeName);
				if (type == null)
					return false;
				i += 3;
				return EvaluateMember (type, null, prop, i, out val);
			}

			int n = prop.IndexOf ('[');
			if (n > 0) {
				return EvaluateStringAtIndex (prop, n, out val);
			}

			n = prop.IndexOf ('.');
			if (n == -1) {
				needsItemEvaluation |= (!ignorePropsWithTransforms && propertiesWithTransforms.Contains (prop));
				val = GetPropertyValue (prop) ?? string.Empty;
				return true;
			} else {
				var pn = prop.Substring (0, n);
				val = GetPropertyValue (pn) ?? string.Empty;
				return EvaluateMember (typeof(string), val, prop, n + 1, out val);
			}
		}

		internal bool EvaluateMember (Type type, object instance, string str, int i, out object val)
		{
			val = null;

			// Find the delimiter of the member
			int j = str.IndexOfAny (new [] { '.', ')', '(' }, i);
			if (j == -1)
				j = str.Length;

			var memberName = str.Substring (i, j - i).Trim ();
			if (memberName.Length == 0)
				return false;
			
			var member = ResolveMember (type, memberName, instance == null);
			if (member == null || member.Length == 0)
				return false;

			if (j < str.Length && str[j] == '(') {
				// It is a method invocation
				object [] parameterValues;
				j++;
				if (!EvaluateParameters (str, ref j, out parameterValues))
					return false;

				if (!EvaluateMethod (str, member, instance, parameterValues, out val))
					return false;
				
				// Skip the closing parens
				j++;

			} else {
				// It has to be a property or field
				try {
					if (member[0] is PropertyInfo)
						val = ((PropertyInfo)member[0]).GetValue (instance);
					else if (member[0] is FieldInfo)
						val = ((FieldInfo)member[0]).GetValue (instance);
					else
						return false;
				} catch (Exception ex) {
					LoggingService.LogError ("MSBuild property evaluation failed: " + str, ex);
					return false;
				}
			}
			if (j < str.Length && str[j] == '.') {
				// Chained member invocation
				if (val == null)
					return false;
				return EvaluateMember (val.GetType (), val, str, j + 1, out val);
			}
			return true;
		}

		internal bool EvaluateMember (string str, Type type, string memberName, object instance, object [] parameterValues, out object val)
		{
			val = null;
			var member = ResolveMember (type, memberName, instance == null);
			if (member == null || member.Length == 0)
				return false;
			return EvaluateMethod (str, member, instance, parameterValues, out val);
		}

		bool EvaluateMethod (string str, MemberInfo[] member, object instance, object [] parameterValues, out object val)
		{
			val = null;

			// Find a method with a matching number of parameters
			var method = FindBestOverload (member.OfType<MethodBase> (), parameterValues);
			if (method == null)
				return false;

			try {
				// Convert the given parameters to the types specified in the method signature
				var methodParams = method.GetParameters ();

				var convertedArgs = (methodParams.Length == parameterValues.Length) ? parameterValues : new object [methodParams.Length];

				int numArgs = methodParams.Length;
				Type paramsArgType = null;
				if (methodParams.Length > 0 && methodParams [methodParams.Length - 1].ParameterType.IsArray && methodParams [methodParams.Length - 1].IsDefined (typeof (ParamArrayAttribute))) {
					paramsArgType = methodParams [methodParams.Length - 1].ParameterType.GetElementType ();
					numArgs--;
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
					var argsArray = new object [parameterValues.Length - numArgs];
					for (int m = 0; m < argsArray.Length; m++)
						argsArray [m] = ConvertArg (method, n, parameterValues [n++], paramsArgType);
					convertedArgs [convertedArgs.Length - 1] = argsArray;
				}

				// Invoke the method
				val = method.Invoke (instance, convertedArgs);
			} catch (Exception ex) {
				LoggingService.LogError ("MSBuild property evaluation failed: " + str, ex);
				return false;
			}
			return true;
		}

		internal bool EvaluateParameters (string str, ref int i, out object[] parameters)
		{
			parameters = null;
			var list = new List<object> ();

			while (i < str.Length) {
				var j = FindClosingChar (str, i, new [] { ',', ')' });
				if (j == -1)
					return false;
				
				var arg = str.Substring (i, j - i).Trim ();

				if (arg.Length == 0 && str [j] == ')' && list.Count == 0) {
					// Empty parameters list
					parameters = new object [0];
					i = j;
					return true;
				}

				// Trim enclosing quotation marks
				if (arg.Length > 1 && IsQuote(arg [0]) && arg[arg.Length - 1] == arg [0])
					arg = arg.Substring (1, arg.Length - 2);

				list.Add (Evaluate (arg));

				if (str [j] == ')') {
					// End of parameters list
					parameters = list.ToArray ();
					i = j;
					return true;
				}
				i = j + 1;
			}
			return false;
		}

		MethodBase FindBestOverload (IEnumerable<MethodBase> methods, object [] args)
		{
			MethodBase methodWithParams = null;

			foreach (var m in methods) {
				var argInfo = m.GetParameters ();

				// Exclude methods which take a complex object as argument
				if (argInfo.Any (a => a.ParameterType != typeof(object) && Type.GetTypeCode (a.ParameterType) == TypeCode.Object && !IsParamsArg(a)))
					continue;

				if (args.Length >= argInfo.Length - 1 && argInfo.Length > 0 && IsParamsArg (argInfo [argInfo.Length - 1])) {
					methodWithParams = m;
					continue;
				}
				if (args.Length != argInfo.Length)
					continue;

				bool isValid = true;
				for (int n = 0; n < args.Length; n++) {
					if (!CanConvertArg (m, n, args [n], argInfo [n].ParameterType)) {
						isValid = false;
						break;
					}
				}
				if (isValid)
					return m;
			}
			return methodWithParams;
		}

		bool IsParamsArg (ParameterInfo pi)
		{
			return pi.ParameterType.IsArray && pi.IsDefined (typeof (ParamArrayAttribute));
		}

		bool CanConvertArg (MethodBase method, int argNum, object value, Type parameterType)
		{
			var sval = value as string;
			if (sval == "null" || value == null)
				return !parameterType.IsValueType || typeof(Nullable).IsInstanceOfType (parameterType);

			if (sval != null && parameterType == typeof (char []))
				return true;

			if (parameterType == typeof (char) && sval != null && sval.Length != 1)
				return false;

			return true;
		}

		object ConvertArg (MethodBase method, int argNum, object value, Type parameterType)
		{
			var sval = value as string;
			if (sval == "null" || value == null)
				return null;

			if (sval != null && parameterType == typeof (char[]))
				return sval.ToCharArray ();

			if (sval != null && Path.DirectorySeparatorChar != '\\')
				value = sval.Replace ('\\', Path.DirectorySeparatorChar);
			
			var res = Convert.ChangeType (value, parameterType, CultureInfo.InvariantCulture);
			bool convertPath = false;

			if ((method.DeclaringType == typeof (System.IO.File) || method.DeclaringType == typeof (System.IO.Directory)) && argNum == 0) {
				convertPath = true;
			} else if (method.DeclaringType == typeof (IntrinsicFunctions)) {
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

		bool EvaluateList (string prop, List<MSBuildItemEvaluated> evaluatedItemsCollection, out object val)
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
			else {
				var t = supportedTypeMembers.FirstOrDefault (st => st.Item1.FullName == typeName);
				if (t == null)
					return null;
				return t.Item1;
			}
		}

		MemberInfo[] ResolveMember (Type type, string memberName, bool isStatic)
		{
			if (type == typeof (string) && memberName == "new")
				memberName = "Copy";
			if (type.IsArray)
				type = typeof (Array);
			var flags = isStatic ? BindingFlags.Static : BindingFlags.Instance;
			if (type != typeof (Microsoft.Build.Evaluation.IntrinsicFunctions)) {
				var t = supportedTypeMembers.FirstOrDefault (st => st.Item1 == type);
				if (t == null)
					return null;
				if (t.Item2 != null && !t.Item2.Contains (memberName))
					return null;
			} else
				flags |= BindingFlags.NonPublic;
			
			return type.GetMember (memberName, flags | BindingFlags.Public | BindingFlags.IgnoreCase);
		}

		bool EvaluateStringAtIndex (string prop, int i, out object val)
		{
			val = null;

			int j = prop.IndexOf (']');
			if (j == -1)
				return false;

			if (j < prop.Length - 1 || j - i < 2)
				return false;

			string indexText = prop.Substring (i + 1, j - (i + 1));
			int index = -1;
			if (!int.TryParse (indexText, out index))
				return false;

			prop = prop.Substring (0, i);
			string propertyValue = GetPropertyValue (prop) ?? string.Empty;
			if (propertyValue.Length <= index)
				return false;

			val = propertyValue.Substring (index, 1);
			return true;
		}

		static Tuple<Type, string []> [] supportedTypeMembers = {
			Tuple.Create (typeof(System.Array), (string[]) null),
			Tuple.Create (typeof(System.Byte), (string[]) null),
			Tuple.Create (typeof(System.Char), (string[]) null),
			Tuple.Create (typeof(System.Convert), (string[]) null),
			Tuple.Create (typeof(System.DateTime), (string[]) null),
			Tuple.Create (typeof(System.Decimal), (string[]) null),
			Tuple.Create (typeof(System.Double), (string[]) null),
			Tuple.Create (typeof(System.Enum), (string[]) null),
			Tuple.Create (typeof(System.Guid), (string[]) null),
			Tuple.Create (typeof(System.Int16), (string[]) null),
			Tuple.Create (typeof(System.Int32), (string[]) null),
			Tuple.Create (typeof(System.Int64), (string[]) null),
			Tuple.Create (typeof(System.IO.Path), (string[]) null),
			Tuple.Create (typeof(System.Math), (string[]) null),
			Tuple.Create (typeof(System.UInt16), (string[]) null),
			Tuple.Create (typeof(System.UInt32), (string[]) null),
			Tuple.Create (typeof(System.UInt64), (string[]) null),
			Tuple.Create (typeof(System.SByte), (string[]) null),
			Tuple.Create (typeof(System.Single), (string[]) null),
			Tuple.Create (typeof(System.String), (string[]) null),
			Tuple.Create (typeof(System.StringComparer), (string[]) null),
			Tuple.Create (typeof(System.TimeSpan), (string[]) null),
			Tuple.Create (typeof(System.Text.RegularExpressions.Regex), (string[]) null),
			Tuple.Create (typeof(Microsoft.Build.Utilities.ToolLocationHelper), (string[]) null),
			Tuple.Create (typeof(System.Environment), new string [] {
				"CommandLine", "ExpandEnvironmentVariables", "GetEnvironmentVariable", "GetEnvironmentVariables", "GetFolderPath", "GetLogicalDrives"
			}),
			Tuple.Create (typeof(System.IO.Directory), new string [] {
				"GetDirectories", "GetFiles", "GetLastAccessTime", "GetLastWriteTime", "GetParent"
			}),
			Tuple.Create (typeof(System.IO.File), new string [] {
				"Exists", "GetCreationTime", "GetAttributes", "GetLastAccessTime", "GetLastWriteTime", "ReadAllText"
			}),
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

		int FindClosingChar (string str, int i, char closeChar)
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
					i = str.IndexOf (c, i + 1);
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

		static int FindClosingChar (string str, int i, char[] closeChar)
		{
			int pc = 0;
			while (i < str.Length) {
				var c = str [i];
				if (pc == 0 && closeChar.Contains (c))
					return i;
				if (c == '(' || c == '[')
					pc++;
				else if (c == ')' || c == ']')
					pc--;
				else if (IsQuote (c)) {
					i = str.IndexOf (c, i + 1);
					if (i == -1)
						return -1;
				}
				i++;
			}
			return -1;
		}

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
				if (FullFileName == String.Empty)
					return null;
				if (directoryName == null)
					directoryName = Path.GetDirectoryName (FullFileName);

				return directoryName;
			}
		}

		#endregion
	}
}
