//
// IProject.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

using MonoDevelop.Core;
using MonoDevelop.Ide.Projects.Item;

namespace MonoDevelop.Ide.Projects
{
	public class MSBuildProject : IProject
	{
		string            fileName;
		string            language;
		List<string>      imports = new List<string> ();
		List<ProjectItem> items   = new List<ProjectItem> ();
		List<PropertyGroup> propertyGroups = new List<PropertyGroup> ();
		
		public string FileName {
			get {
				return fileName;
			}
			set {
				fileName = value;
			}
		}
		
		public string Language {
			get {
				return language;
			}
		}
		
		
		public virtual string Name {
			get {
				return Path.GetFileNameWithoutExtension (FileName);
			}
		}
		
		public string BasePath {
			get {
				return Path.GetDirectoryName (FileName);
			}
		}
		
		public string OutputAssemblyFileName {
			get {
				return Path.Combine (this.OutputPath, this.AssemblyName + GetExtension (this.OutputType));
			}
		}
		
		public ReadOnlyCollection<ProjectItem> Items {
			get {
				return items.AsReadOnly ();
			}
		}
		
		public List<PropertyGroup> PropertyGroups {
			get {
				return this.propertyGroups;
			}
		}
		
		public MSBuildProject (string language)
		{
			this.language = language;
			propertyGroups.Add(new PropertyGroup (null));
			propertyGroups.Add(new PropertyGroup (" '$(Configuration)' == 'Debug' "));
			propertyGroups.Add(new PropertyGroup (" '$(Configuration)' == 'Release' "));
			SetProperty ("ProjectGuid", "{" + System.Guid.NewGuid ().ToString ().ToUpper () + "}", null, null);
			SetProperty ("Configuration", "Debug", null, null);
			SetProperty ("Platform", "AnyCPU", null, null);
			
			SetProperty ("OutputPath", @"bin\Debug\", "Debug", null);
			SetProperty ("DebugSymbols", @"True", "Debug", null);
			SetProperty ("DebugType", @"Full", "Debug", null);
			SetProperty ("CheckForOverflowUnderflow", @"True", "Debug", null);
			SetProperty ("DefineConstants", @"DEBUG;TRACE", "Debug", null);
			
			SetProperty ("OutputPath", @"bin\Release\", "Release", null);
			SetProperty ("DebugSymbols", @"False", "Release", null);
			SetProperty ("DebugType", @"None", "Release", null);
			SetProperty ("CheckForOverflowUnderflow", @"False", "Release", null);
			SetProperty ("DefineConstants", @"TRACE", "Release", null);
		}
		
		public MSBuildProject (string language, string fileName)
		{
			this.language = language;
			this.fileName = fileName;
		}
		
		public static string GetExtension (string outputType)
		{
			if (outputType.ToLower () == "exe" || outputType.ToLower () == "winexe") {
				return ".exe";
			}
			return ".dll";
		}

		
		public void Start (IProgressMonitor monitor, ExecutionContext context)
		{
			string path;
			if (!String.IsNullOrEmpty (this.OutputPath)) 
			    path = Path.Combine (BasePath, SolutionProject.NormalizePath (this.OutputPath));
			else 
				path = BasePath;
			Runtime.ProcessService.StartConsoleProcess ( Path.Combine(path, this.AssemblyName + ".exe"),
			                                            "",
			                                            BasePath,
			                                            context.ConsoleFactory.CreateConsole (true),
			                                            delegate {});
		}
		
#region Global Properties
		public string Guid {
			get {
				return GetProperty ("ProjectGuid", null, null);
			}
		}
		
		
		public string Configuration {
			get {
				return GetProperty ("Configuration", null, null);
			}
			set {
				SetProperty ("Configuration", value, null, null);
			}
		}
		
		public string Platform {
			get {
				return GetProperty ("Platform", null, null);
			}
			set {
				SetProperty ("Platform", value, null, null);
			}
		}
		
		public string AssemblyName {
			get {
				return GetProperty ("AssemblyName", null, null);
			}
			set {
				SetProperty ("AssemblyName", value, null, null);
			}
		}
		
		public string RootNamespace {
			get {
				return GetProperty ("RootNamespace", null, null);
			}
			set {
				SetProperty ("RootNamespace", value, null, null);
			}
		}
		
		public string OutputType {
			get {
				return GetProperty ("OutputType", null, null);
			}
			set {
				SetProperty ("OutputType", value, null, null);
			}
		}
#endregion
		public string OutputPath {
			get {
				return GetProperty ("OutputPath");
			}
			set {
				SetProperty ("OutputPath", value);
			}
		}
		public string DefineConstants {
			get {
				return GetProperty ("DefineConstants");
			}
			set {
				SetProperty ("DefineConstants", value);
			}
		}
		
		public T GetProperty<T> (string name, T defaultValue)
		{
			string value = GetProperty (name);
			if (String.IsNullOrEmpty (value))
				return defaultValue;
			TypeConverter converter = TypeDescriptor.GetConverter (typeof(T));
			return (T)converter.ConvertFromInvariantString (value);
		}
		public string GetProperty (string name)
		{
			return GetProperty (name, Configuration, Platform);
		}
		public T GetProperty<T> (string name, string configuration, string cpu, T defaultValue)
		{
			string value = GetProperty (name, configuration, cpu);
			if (String.IsNullOrEmpty (value))
				return defaultValue;
			TypeConverter converter = TypeDescriptor.GetConverter (typeof(T));
			return (T)converter.ConvertFromInvariantString (value);
		}
		
		public string GetProperty (string name, string configuration, string cpu)
		{
			foreach (PropertyGroup group in this.PropertyGroups) {
				if (!group.IsValid (configuration, cpu)) 
					continue;
				Property property = group.GetProperty (name);
				if (property != null) {
					return property.Value;
				}
			}
			return null;
		}
		
		public void SetProperty (string name, string value)
		{
			SetProperty (name, value, Configuration, Platform);
		}
		public void SetProperty (string name, string value, string configuration, string platform)
		{
			foreach (PropertyGroup group in this.PropertyGroups) {
				if (!group.IsValid (configuration, platform)) 
					continue;
				if (!String.IsNullOrEmpty (configuration) && String.IsNullOrEmpty (group.Configuration))
					continue;
				if (!String.IsNullOrEmpty (platform) && String.IsNullOrEmpty (group.Platform))
					continue;
				Property property = group.GetProperty (name);
				if (property != null) {
					if (String.IsNullOrEmpty (value)) {
						group.RemoveProperty (property);
						if (group.IsEmpty)
							this.PropertyGroups.Remove (group);
					} else
						property.Value = value;
					return;
				} else {
					group.Properties.Add (new Property (name, value));
					return;
				}
			}
			PropertyGroup newGroup = new PropertyGroup (configuration, platform);
			newGroup.Properties.Add (new Property (name, value));
			this.PropertyGroups.Add (newGroup);
		}
		
#region Debug options
		public string StartProgram {
			get {
				return GetProperty ("StartProgram");
			}
			set {
				SetProperty ("StartProgram", value);
			}
		}
		
		public string StartUrl {
			get {
				return GetProperty ("StartURL");
			}
			set {
				SetProperty ("StartURL", value);
			}
		}
		
		public StartAction StartAction {
			get {
				return GetProperty<StartAction> ("StartAction", StartAction.Program);
			}
			set {
				SetProperty ("StartAction", value.ToString());
			}
		}
		
		public string StartArguments {
			get {
				return GetProperty ("StartArguments");
			}
			set {
				SetProperty ("StartArguments", value);
			}
		}
		
		public string StartWorkingDirectory {
			get {
				return GetProperty ("StartWorkingDirectory");
			}
			set {
				SetProperty ("StartWorkingDirectory", value);
			}
		}
#endregion
			
#region I/O
		class Property
		{
			string name;
			string value;
			Dictionary<string, string> attributes = new Dictionary<string, string> ();
			
			public string Name {
				get { return name; }
				set { name = value; }
			}
			public string Value {
				get { return this.value; }
				set { this.value = value; }
			}
			public Dictionary<string, string> Attributes {
				get { return this.attributes; }
			}
			
			public Property ()
			{
			}
			
			public Property (string name, string value)
			{
				this.Name  = name;
				this.Value = value;
			}
			
			public void Write (XmlWriter writer)
			{
				writer.WriteStartElement (this.Name);
				foreach (KeyValuePair<string, string> attribute in this.Attributes) 
					writer.WriteAttributeString (attribute.Key, attribute.Value);
				writer.WriteString (this.Value);
				writer.WriteEndElement ();
			}
			
			public static Property Read (XmlReader reader)
			{
				Property result = new Property ();
				result.Name = reader.LocalName;
				for (int i = 0; i < reader.AttributeCount; ++i) {
					reader.MoveToAttribute (i);
					result.Attributes[reader.LocalName] = reader.Value;
				}
				reader.MoveToElement();
				result.Value = reader.ReadString();
				return result;
			}
			
			public override string ToString ()
			{
				return String.Format ("[Property: Name={0}, Value={1}, #Attributes={2}]", Name, Value, Attributes.Count);
			}
		}
		
		class PropertyGroup
		{
			string configuration;
			string platform;
			List<Property> properties = new List<Property> ();
			
			public string Configuration {
				get { return configuration; }
				set { configuration = value; }
			}
			
			public string Platform {
				get { return platform; }
				set { platform = value; }
			}
			
			public List<Property> Properties {
				get { return properties; }
			}
				
			public bool IsEmpty {
				get {
					return this.properties.Count == 0;
				}
			}
			
			static Regex configPattern         = new Regex (@"\s*'\$\(Configuration\)'\s*==\s*'(?<Configuration>.*)'\s*", RegexOptions.Compiled);
			static Regex platformPattern       = new Regex (@"\s*'\$\(Platform\)'\s*==\s*'(?<Platform>.*)'\s*", RegexOptions.Compiled);
			static Regex configplatformPattern = new Regex (@"\s*'\$\(Configuration\)\|\$\(Platform\)'\s*==\s*'(?<Configuration>.*)\|(?<Platform>.*)'\s*", RegexOptions.Compiled);
			
			public PropertyGroup (string configuration, string platform)
			{
				this.Configuration = configuration;
				this.Platform      = platform;
			}
			
			public PropertyGroup (string condition)
			{
				if (!String.IsNullOrEmpty (condition)) {
					Match match = configplatformPattern.Match (condition);
					if (!match.Success) {
						match = configPattern.Match (condition);
						if (!match.Success) {
							match = platformPattern.Match (condition);
							if (match.Success) {
								Platform = match.Result ("${Platform}");
							}
						} else {
							Configuration = match.Result ("${Configuration}");
						}
					} else {
						Configuration = match.Result ("${Configuration}");
						Platform = match.Result ("${Platform}");
					}
				}
			}
			
			public Property GetProperty (string name)
			{
				foreach (Property property in this.Properties) {
					if (property.Name == name) {
						return property;
					}
				}
				return null;
			}
			
			public void RemoveProperty (Property property)
			{
				this.Properties.Remove (property);
			}
			
			public bool IsValid (string configuration, string platform)
			{
				return (this.Configuration == configuration || String.IsNullOrEmpty (this.Configuration)) && 
					   (this.Platform == platform || String.IsNullOrEmpty (this.Platform));
			}
			
			string GetMSBuildCondition ()
			{
				if (String.IsNullOrEmpty (this.Configuration) && String.IsNullOrEmpty (this.Platform)) 
					return null;
				if (String.IsNullOrEmpty (this.Configuration)) 
					return String.Format(@"'$(Platform)' == '{0}'", this.Platform);
				if (String.IsNullOrEmpty (this.Platform)) 
					return String.Format(@"'$(Configuration)' == '{0}'", this.Configuration);
				return String.Format(@"'$(Configuration)|$(Platform)' == '{0}|{1}'", this.Configuration, this.Platform);
			}
			
			public void Write (XmlWriter writer)
			{
				writer.WriteStartElement ("PropertyGroup");
				if (!String.IsNullOrEmpty (GetMSBuildCondition ()))
				    writer.WriteAttributeString ("Condition", GetMSBuildCondition ());
				foreach (Property property in this.Properties) 
					property.Write (writer);
				writer.WriteEndElement ();
			}
			
			public static PropertyGroup Read (XmlReader reader)
			{
				PropertyGroup result = new PropertyGroup (reader.GetAttribute("Condition"));
				ProjectReadHelper.ReadList (reader, "PropertyGroup", delegate() {
					result.Properties.Add (Property.Read (reader));
					return true;
				});
				return result;
			}
		}
		
		void ReadItemGroup (XmlReader reader)
		{
			ProjectReadHelper.ReadList (reader, "ItemGroup", delegate() {
				ProjectItem item = ProjectItemFactory.Create (reader.LocalName);
				if (item != null) {
					item.Include = reader.GetAttribute ("Include");
					ProjectReadHelper.ReadList (reader, reader.LocalName, delegate() {
						item.SetMetadata (reader.LocalName, reader.ReadString ());
						return true;
					});
					this.Add (item);
					return true;
				}
				return false;
			});
		}
		
		public static IProject Load (string language, string fileName)
		{
			MSBuildProject result = new MSBuildProject (language, fileName);
			using (XmlReader reader = XmlTextReader.Create (fileName)) {
				ProjectReadHelper.ReadList (reader, "Project", delegate() {
					switch (reader.LocalName) {
					case "Project":
						// Root node
						return true;
					case "PropertyGroup":
						result.PropertyGroups.Add(PropertyGroup.Read (reader));
						return true;
					case "ItemGroup":
						result.ReadItemGroup (reader);
						return true;
					case "Import":
						if (!String.IsNullOrEmpty (reader.GetAttribute ("Project"))) {
							result.imports.Add (reader.GetAttribute ("Project"));
						}
						return true;
					}
					return false;
				});
			}
			return result;
		}
		
		public void Save ()
		{
			using (XmlTextWriter writer = new XmlTextWriter (FileName, System.Text.Encoding.UTF8)) {
				writer.Formatting = Formatting.Indented;
				writer.WriteStartElement ("Project");
				writer.WriteAttributeString ("DefaultTargets", "Build");
				writer.WriteAttributeString ("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");
				foreach (PropertyGroup propertyGroup in this.propertyGroups) {
					propertyGroup.Write (writer);
				}
				writer.WriteStartElement ("ItemGroup");
				foreach (ProjectItem item in this.Items) {
					if (item is ReferenceProjectItem) {
						item.Write (writer);
					}
				}
				writer.WriteEndElement (); // ItemGroup
				
				writer.WriteStartElement ("ItemGroup");
				foreach (ProjectItem item in this.Items) {
					if (!(item is ReferenceProjectItem)) {
						item.Write (writer);
					}
				}
				writer.WriteEndElement (); // ItemGroup
				
				foreach (string import in this.imports) {
					writer.WriteStartElement ("Import");
					writer.WriteAttributeString ("Project", import);
					writer.WriteEndElement (); //  Import
				}
				
				writer.WriteEndElement (); // Project
			}
			
		}
#endregion
		
		public string GetDefaultNamespace (string fileName)
		{
			return this.RootNamespace;
		}
		
		public bool IsFileInProject (string fileName)
		{
			return GetFile (fileName) != null;
		}
		public ProjectFile GetFile (string fileName)
		{
			string exactFileName = Path.GetFullPath (fileName);
			foreach (ProjectItem item in this.Items) {
				if (item is ProjectFile)
					if (Path.GetFullPath (Path.Combine (this.BasePath, item.Include)) == exactFileName)
						return item as ProjectFile;
			}
			return null;
		}
		
		
		public void Add (ProjectItem item)
		{
			if (item.Project == this) 
				return;
			item.Project = this;
			this.items.Add (item);
			OnItemAdded (new ProjectItemEventArgs (item));
		}
		
		public void Remove (ProjectItem item)
		{
			if (item.Project != this) 
				return;
			item.Project = null;
			this.items.Remove (item);
			OnItemRemoved (new ProjectItemEventArgs (item));
		}
		
		public event EventHandler<RenameEventArgs> NameChanged;
		protected virtual void OnNameChanged(RenameEventArgs e)
		{
			if (NameChanged != null)
				NameChanged (this, e);
		}
		
		public event EventHandler<ProjectItemEventArgs> ItemAdded;
		protected virtual void OnItemAdded(ProjectItemEventArgs e)
		{
			if (ItemAdded != null)
				ItemAdded (this, e);
		}
		
		public event EventHandler<ProjectItemEventArgs> ItemRemoved;
		protected virtual void OnItemRemoved(ProjectItemEventArgs e)
		{
			if (ItemRemoved != null)
				ItemRemoved (this, e);
		}
	}
}
