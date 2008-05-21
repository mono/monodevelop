
using System;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Deployment.Linux
{
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
		
		SolutionItem entry;
		bool connected;
		
		internal LinuxDeployData (SolutionItem entry)
		{
			this.entry = entry;
		}
		
		internal LinuxDeployData ()
		{
		}
		
		public static LinuxDeployData GetLinuxDeployData (SolutionItem entry)
		{
			LinuxDeployData data = (LinuxDeployData) entry.ExtendedProperties ["Deployment.LinuxDeployData"];
			if (data != null) {
				if (data.entry == null) {
					data.Bind (entry);
					data.connected = true;
				}
				return data;
			}
			
			data = (LinuxDeployData) entry.ExtendedProperties ["Temp.Deployment.LinuxDeployData"];
			if (data != null)
				return data;
			
			data = CreateDefault (entry);
			entry.ExtendedProperties ["Temp.Deployment.LinuxDeployData"] = data;
			data.Bind (entry);
			return data;
		}
		
		internal static LinuxDeployData CreateDefault (SolutionItem entry)
		{
			return new LinuxDeployData (entry);
		}
		
		void Bind (SolutionItem entry)
		{
			this.entry = entry;
		}
		
		void UpdateEntry ()
		{
			if (connected)
				return;
			entry.ExtendedProperties ["Deployment.LinuxDeployData"] = this;
			entry.ExtendedProperties.Remove ("Temp.Deployment.LinuxDeployData");
			connected = true;
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
