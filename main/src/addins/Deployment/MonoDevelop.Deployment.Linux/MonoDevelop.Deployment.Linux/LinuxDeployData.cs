
using System;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using System.Xml;
using System.IO;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Deployment.Linux
{
	[DataItem ("Deployment.LinuxDeployData")]
	public class LinuxDeployData
	{
		[ItemProperty (DefaultValue=true)]
		bool generateScript = true;
		
		[ItemProperty]
		string scriptName;
		
		[ItemProperty]
		string packageName;
		
		[ItemProperty (DefaultValue=false)]
		bool generateDesktopEntry;
		
		[ItemProperty (DefaultValue=true)]
		bool generatePcFile = true;
		
		Project entry;

		internal LinuxDeployData (Project entry)
		{
			this.entry = entry;
		}
		
		internal LinuxDeployData ()
		{
		}
		
		public static LinuxDeployData GetLinuxDeployData (Project entry)
		{
			LinuxDeployData data = (LinuxDeployData) entry.ExtendedProperties ["Deployment.LinuxDeployData"];
			if (data != null)
				return data;
			
			var elem = entry.MSBuildProject.GetMonoDevelopProjectExtension ("Deployment.LinuxDeployData");
			if (elem != null) {
				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				data = (LinuxDeployData) ser.Deserialize (new XmlNodeReader (elem), typeof(LinuxDeployData));
			} else {
				data = CreateDefault (entry);
			}
			data.entry = entry;
			entry.ExtendedProperties ["Deployment.LinuxDeployData"] = data;
			return data;
		}
		
		internal static LinuxDeployData CreateDefault (Project entry)
		{
			return new LinuxDeployData (entry);
		}
		
		void UpdateEntry ()
		{
			var ser = new XmlDataSerializer (new DataContext ());
			ser.Namespace = MSBuildProject.Schema;
			var sw = new StringWriter ();
			var w = new XmlTextWriter (sw);
			ser.Serialize (w, this);

			var elem = (XmlElement) entry.MSBuildProject.Document.ReadNode (new XmlTextReader (new StringReader (sw.ToString ())));

			entry.MSBuildProject.SetMonoDevelopProjectExtension ("Deployment.LinuxDeployData", elem);
		}
		
		public string PackageName {
			get {
				if (packageName != null)
					return packageName;
				if (scriptName != null)
					return scriptName;
				return entry.Name.ToLower ();
			}
			set {
				if (packageName != value) {
					packageName = value;
					UpdateEntry ();
				}
			}
		}
		
		public bool GenerateScript {
			get { return generateScript; }
			set {
				if (generateScript != value) {
					generateScript = value; 
					UpdateEntry ();
				}
			}
		}
		
		public string ScriptName {
			get { return scriptName != null ? scriptName : PackageName; }
			set {
				if (value != ScriptName) {
					scriptName = value; 
					UpdateEntry ();
				}
			}
		}
					    
				
		
		public bool GenerateDesktopEntry {
			get { return generateDesktopEntry; }
			set {
				if (generateDesktopEntry != value) {
					generateDesktopEntry = value; 
					UpdateEntry ();
				}
			}
		}
		
		public bool GeneratePcFile {
			get { return generatePcFile; }
			set {
				if (generatePcFile != value) {
					generatePcFile = value; 
					UpdateEntry ();
				}
			}
		}
	}
}
