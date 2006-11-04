
using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Projects.Serialization;

namespace VersionControl.Service
{
	public class VersionControlService
	{
		static List<VersionControlSystem> handlers = new List<VersionControlSystem> ();
		static VersionControlConfiguration configuration;
		static DataContext dataContext = new DataContext ();
		
		static VersionControlService ()
		{
			Runtime.AddInService.RegisterExtensionItemListener ("/MonoDevelop/VersionControl/VersionControlSystems", OnExtensionChanged);
		}

		static void OnExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add) {
				VersionControlSystem vcs = (VersionControlSystem) item;
				handlers.Add (vcs);
				// Include the repository type in the serialization context, so repositories
				// of this type can be deserialized from the configuration file.
				dataContext.IncludeType (vcs.CreateRepositoryInstance ().GetType ());
			}
		}
		
		static string ConfigFile {
			get {
				return Path.Combine (Runtime.Properties.ConfigDirectory, "VersionControl.config");
			}
		}
		
		static public IEnumerable<VersionControlSystem> GetVersionControlSystems ()
		{
			return handlers;
		}
		
		public static void AddRepository (Repository repo)
		{
			GetConfiguration ().Repositories.Add (repo);
		}
		
		public static void RemoveRepository (Repository repo)
		{
			GetConfiguration ().Repositories.Remove (repo);
		}
		
		static public IEnumerable<Repository> GetRepositories ()
		{
			return GetConfiguration ().Repositories;
		}
		
		static VersionControlConfiguration GetConfiguration ()
		{
			if (configuration == null) {
				if (File.Exists (ConfigFile)) {
					XmlDataSerializer ser = new XmlDataSerializer (dataContext);
					XmlTextReader reader = new XmlTextReader (new StreamReader (ConfigFile));
					try {
						configuration = (VersionControlConfiguration) ser.Deserialize (reader, typeof(VersionControlConfiguration));
					} finally {
						reader.Close ();
					}
				}
				if (configuration == null)
					configuration = new VersionControlConfiguration ();
			}
			return configuration;
		}
		
		public static void SaveConfiguration ()
		{
			if (configuration != null) {
				XmlDataSerializer ser = new XmlDataSerializer (dataContext);
				XmlTextWriter tw = new XmlTextWriter (new StreamWriter (ConfigFile));
				try {
					ser.Serialize (tw, configuration, typeof(VersionControlConfiguration));
				} finally {
					tw.Close ();
				}
			}
		}
		
		public static void ResetConfiguration ()
		{
			configuration = null;
		}
		
		public static Repository GetRepositoryReference (string path, string id)
		{
			foreach (VersionControlSystem vcs in handlers) {
				Repository repo = vcs.GetRepositoryReference (path, id);
				if (repo != null)
					return repo;
			}
			return null;
		}
		
		public static void StoreRepositoryReference (Repository repo, string path, string id)
		{
			repo.VersionControlSystem.StoreRepositoryReference (repo, path, id);
		}
		
		internal static Repository InternalGetRepositoryReference (string path, string id)
		{
			string file = Path.Combine (path, id) + ".mdvcs";
			if (!File.Exists (file))
				return null;
			
			XmlDataSerializer ser = new XmlDataSerializer (dataContext);
			XmlTextReader reader = new XmlTextReader (new StreamReader (file));
			try {
				return (Repository) ser.Deserialize (reader, typeof(Repository));
			} finally {
				reader.Close ();
			}
		}
		
		internal static void InternalStoreRepositoryReference (Repository repo, string path, string id)
		{
			string file = Path.Combine (path, id) + ".mdvcs";
			
			XmlDataSerializer ser = new XmlDataSerializer (dataContext);
			XmlTextWriter tw = new XmlTextWriter (new StreamWriter (file));
			try {
				ser.Serialize (tw, repo, typeof(Repository));
			} finally {
				tw.Close ();
			}
		}
	}
}
