using System;
using System.Threading.Tasks;
using Xwt;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using System.Threading;
using MonoDevelop.Core.Serialization;
using System.Collections.Immutable;

namespace MonoDevelop.ConnectedServices.DebugService
{
	sealed class TestDebugServiceProvider : IConnectedServiceProvider
	{
		public TestDebugServiceProvider ()
		{
		}

		public IConnectedService GetConnectedService (DotNetProject project)
		{
			#if DEBUG
			if (project.LanguageName == "C#") { 
				return new TestDebugService (project);
			}
			#endif
			return null;
		}
	}

	sealed class TestDebugServiceProjectExtension : DotNetProjectExtension
	{
		#if DEBUG
		[ItemProperty]
		public string TestDebugServiceProperty { get; set; }
		#endif
	}

	#if DEBUG
	#if USE_CONNECTED_SERVICES_JSON_FILE
	sealed class TestDebugService : JsonFileConnectedService
	{
		public TestDebugService (DotNetProject project) : base (project)
		{
			this.Id = "MonoDevelop.ConnectedServices.DebugService.TestDebugService";
			this.DisplayName = "Test Service";
			this.Description = "This is a simple service example to show how you might construct your own service implementation.";

			this.Dependencies = new IConnectedServiceDependency [] {
				new PackageDependency (this, "Newtonsoft.Json", "Newtonsoft.Json", "6.0.8"),
			};

			this.Sections = new IConfigurationSection [] {
				new TestDebugConfigurationSection(this),
			};
		}

		protected override void OnStoreAddedState(ConnectedServiceState state)
		{
			base.OnStoreAddedState (state);
			state.ProviderId = "MonoDevelop.TestDebugService";
			state.GettingStartedDocument = "https://www.google.com/webhp?q=how+do+i+get+started";
			state.Version = "1.1";
		}
	}
	#else

	sealed class TestDebugService : ConnectedService
	{
		public TestDebugService (DotNetProject project) : base(project)
		{
			this.Id = "MonoDevelop.ConnectedServices.DebugService.TestDebugService";
			this.DisplayName = "Test Service";
			this.Description = "This is a simple service example to show how you might construct your own service implementation.";
			this.DetailsDescription = "This is a simple service example to show how you might construct your own service implementation.\nSome more text.";

			this.Dependencies = ImmutableArray.Create<IConnectedServiceDependency>(
				new PackageDependency (this, "Newtonsoft.Json", "Newtonsoft.Json", "6.0.8")
			);

			this.Sections = ImmutableArray.Create < IConfigurationSection > (
				new TestDebugConfigurationSection(this)
			);
		}

		protected override bool GetIsAddedToProject ()
		{
			var ext = this.Project.GetService<TestDebugServiceProjectExtension> ();
			return !string.IsNullOrEmpty (ext.TestDebugServiceProperty);
		}

		protected override async Task OnAddToProject ()
		{
			var ext = this.Project.GetService<TestDebugServiceProjectExtension> ();
			ext.TestDebugServiceProperty = "Installed";

			await base.OnAddToProject ().ConfigureAwait (false);
		}

		protected override async Task OnRemoveFromProject ()
		{
			var ext = this.Project.GetService<TestDebugServiceProjectExtension> ();
			ext.TestDebugServiceProperty = null;

			await base.OnRemoveFromProject ().ConfigureAwait (false);
		}
	}
	#endif

	sealed class TestDebugConfigurationSection : ConfigurationSection
	{
		bool added;
		
		public override bool IsAdded {
			get { return added; }
		}
		
		public TestDebugConfigurationSection (IConnectedService service) : base (service, "Configure a setting")
		{
		}

		public override Control GetSectionWidget ()
		{
			return new TestConfigurationWidget ();
		}

		protected override async Task<bool> OnAddToProject (CancellationToken token)
		{
			await Task.Delay (1000).ConfigureAwait (false);
			added = true;
			return true;
		}
	}

	sealed class TestConfigurationWidget : AbstractXwtControl
	{
		readonly Widget content;
		
		public TestConfigurationWidget ()
		{
			var vbox = new VBox ();

			var label = new Label { Text = "this configures a setting" };
			vbox.PackStart (label);

			content = vbox;
		}

		public override Widget Widget {
			get {
				return content;
			}
		}
	}
#endif
}
