using System;
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using Newtonsoft.Json;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// ConnectedService implementation that stores state in a .Json file in a subdirectory of the project folder.
	/// </summary>
	public abstract class JsonFileConnectedService : ConnectedService
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.ConnectedServices.JsonFileConnectedService"/> class.
		/// </summary>
		protected JsonFileConnectedService (DotNetProject project) : base (project)
		{
		}

		/// <summary>
		/// Determines if the service has been added to the project.
		/// </summary>
		protected override bool GetIsAddedToProject ()
		{
			return HasConnectedServiceJsonFile (this.Project, this.Id);
		}

		/// <summary>
		/// Creates a new object for storing state about the service in.
		/// </summary>
		protected virtual ConnectedServiceState CreateStateObject ()
		{
			return new ConnectedServiceState ();
		}

		/// <summary>
		/// Stores some state that the service has been added to the project.
		/// </summary>
		protected override Task StoreAddedState ()
		{
			var state = this.CreateStateObject ();
			this.OnStoreAddedState (state);
			WriteConnectedServiceJsonFile (this.Project, this.Id, state);
			return Task.FromResult (true);
		}

		/// <summary>
		/// Stores some state that the service has been added to the project.
		/// </summary>
		protected override Task RemoveAddedState ()
		{
			RemoveConnectedServiceJsonFile (this.Project, this.Id);
			return Task.FromResult (true);
		}

		/// <summary>
		/// Modifies the state object to set any service specific values.
		/// </summary>
		/// <remarks>
		/// The api for this might change, we're introducing a "ProvideId" that appears to have little to do with the Id of 
		/// the service as well as a Version and GettingStartedUrl that may or may not be relevant at this point. 
		/// Override this method to store what you need into the state object.
		/// </remarks>
		protected virtual void OnStoreAddedState (ConnectedServiceState state)
		{
			state.ProviderId = this.Id;
		}

		/// <summary>
		/// Writes a ConnectedServices.json file in the connected services folder of the project for the given service, overwriting the file if it exists already
		/// </summary>
		static void WriteConnectedServiceJsonFile (DotNetProject project, string id, ConnectedServiceState state)
		{
			var jsonFilePath = GetConnectedServiceJsonFilePath (project, id, true);
			var json = JsonConvert.SerializeObject (state, Formatting.Indented);

			if (File.Exists (jsonFilePath)) {
				File.Delete (jsonFilePath);
			}

			File.WriteAllText (jsonFilePath, json);
		}

		/// <summary>
		/// Removes the ConnectedServices.json file in the connected services folder of the project for the given service
		/// </summary>
		static void RemoveConnectedServiceJsonFile (DotNetProject project, string id)
		{
			var jsonFilePath = GetConnectedServiceJsonFilePath (project, id, false);
			var jsonFileDir = Path.GetDirectoryName (jsonFilePath);

			if (Directory.Exists (jsonFileDir)) {
				if (File.Exists (jsonFilePath)) {
					File.Delete (jsonFilePath);
				}

				Directory.Delete (jsonFileDir);
			}
		}

		/// <summary>
		/// Gets whether or not a ConnectedService.json file exists for the given projec and service id.
		/// </summary>
		static bool HasConnectedServiceJsonFile (DotNetProject project, string id)
		{
			var jsonFilePath = GetConnectedServiceJsonFilePath (project, id, false);
			return File.Exists (jsonFilePath);
		}

		/// <summary>
		/// Gets the file path for the ConnectedService.json file for the given project and optionally ensures that the folder
		/// that the file should reside in exists.
		/// </summary>
		internal static string GetConnectedServiceJsonFilePath (DotNetProject project, string id, bool ensureFolderExists)
		{
			var dir = project.BaseDirectory.Combine (ConnectedServices.ProjectStateFolderName).Combine (id);
			if (ensureFolderExists) {
				Directory.CreateDirectory (dir);
			}

			return dir.Combine (ConnectedServices.ConnectedServicesJsonFileName);
		}
	}
}