// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Reflection;
using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core.Services
{ 
	/// <summary>
	/// This class handles the Global Properties for the IDE, all what can be configured should be
	/// loaded/saved by this class. It is a bit like a Singleton with static delegation instead
	/// of returning a static reference to a <code>IProperties</code> object.
	/// </summary>
	public class PropertyService : DefaultProperties, IService
	{
		
		readonly static string propertyFileName    = "MonoDevelopProperties.xml";
		readonly static string propertyFileVersion = "1.1";
		
		readonly static string propertyXmlRootNodeName  = "SharpDevelopProperties";
		
		static string dataDirectory;
		
		static PropertyService()
		{
			string confDataDirectory = System.Configuration.ConfigurationSettings.AppSettings["DataDirectory"];
			
			if (confDataDirectory != null) {
				dataDirectory = confDataDirectory;
			} else {
				dataDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + 
				                                       Path.DirectorySeparatorChar + ".." +
				                                       Path.DirectorySeparatorChar + "data";
			}

			configDirectory = Environment.GetEnvironmentVariable ("XDG_CONFIG_HOME");
			if (configDirectory == null || configDirectory == "")
				configDirectory = System.IO.Path.Combine (Environment.GetEnvironmentVariable ("HOME"), ".config");

			configDirectory = System.IO.Path.Combine (configDirectory, "MonoDevelop");
			configDirectory += System.IO.Path.DirectorySeparatorChar;
		}

		static string configDirectory;
		/// <summary>
		/// returns the path of the default application configuration directory
		/// </summary>
		public string ConfigDirectory {
			get {
				return configDirectory;
			}
		}
		
		public string DataDirectory {
			get {
				return dataDirectory;
			}
		}
		
		public PropertyService()
		{
			try {
				LoadProperties();
			} catch (PropertyFileLoadException) {
				//System.Windows.Forms.MessageBox.Show("Can't load property file", "Warning"); // don't use message service --> cyclic dependency
			}
		}
		
		void WritePropertiesToFile(string fileName)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version=\"1.0\"?>\n<" + propertyXmlRootNodeName + " fileversion = \"" + propertyFileVersion + "\" />");
			
			doc.DocumentElement.AppendChild(ToXmlElement(doc));
			
			FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.GetService(typeof(FileUtilityService));
			fileUtilityService.ObservedSave(new NamedFileOperationDelegate(doc.Save), fileName, FileErrorPolicy.ProvideAlternative);
		}
		
		bool LoadPropertiesFromStream(string filename)
		{
			try {
				XmlDocument doc = new XmlDocument();
				doc.Load(filename);
				
				if (doc.DocumentElement.Attributes["fileversion"].InnerText != propertyFileVersion) {
					return false;
				}
				SetValueFromXmlElement(doc.DocumentElement["Properties"]);
			} catch (Exception e) {
				//Console.WriteLine("Exception while load properties from stream :\n " + e.ToString());
				return false;
			}
			return true;
		}
		
		/// <summary>
		/// Loads the global properties from the current users application data folder, or
		/// if it doesn't exists or couldn't read them it reads the default properties out
		/// of the application folder.
		/// </summary>
		/// <exception cref="PropertyFileLoadException">
		/// Is thrown when no property file could be loaded.
		/// </exception>
		void LoadProperties()
		{
			if (!Directory.Exists(configDirectory)) {
				Directory.CreateDirectory(configDirectory);
			}
			
			if (!LoadPropertiesFromStream(configDirectory + propertyFileName)) {
				if (!LoadPropertiesFromStream(DataDirectory + Path.DirectorySeparatorChar + "options" + Path.DirectorySeparatorChar + propertyFileName)) {
					throw new PropertyFileLoadException();
				}
			}
		}
		
		/// <summary>
		/// Saves the current global property state to a file in the users application data folder.
		/// </summary>
		public void SaveProperties()
		{
			WritePropertiesToFile(configDirectory + propertyFileName);
		}
		
		// IService implementation:
		public virtual void InitializeService()
		{
			OnInitialize(EventArgs.Empty);
		}
		
		public virtual void UnloadService()
		{
			// save properties on exit
			SaveProperties();
			OnUnload(EventArgs.Empty);
		}
		
		protected virtual void OnInitialize(EventArgs e)
		{
			if (Initialize != null) {
				Initialize(this, e);
			}
		}
		
		protected virtual void OnUnload(EventArgs e)
		{
			if (Unload != null) {
				Unload(this, e);
			}
		}
		
		public event EventHandler Initialize;
		public event EventHandler Unload;			
	}
}
