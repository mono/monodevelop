using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;

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
			return new TestDebugService (project);
			#else
			return null;
			#endif
		}
	}

	#if DEBUG
	sealed class TestDebugService : ConnectedService
	{
		public TestDebugService (DotNetProject project) : base(project)
		{
			this.Id = "MonoDevelop.ConnectedServices.DebugService.TestDebugService";
			this.DisplayName = "Test Service";
			this.Description = "This is a simple service example to show how you might construct your own service implementation.";
		}

		public override bool IsConfigured {
			get {
				return false;
			}
		}

		public override object GetConfigurationWidget ()
		{
			throw new NotImplementedException ();
		}

		public override object GetGettingStartedWidget ()
		{
			throw new NotImplementedException ();
		}

		protected override void OnAddToProject ()
		{
		}

		protected override void OnStoreAddedState(ConnectedServiceState state)
		{
			base.OnStoreAddedState (state);
			state.ProviderId = "MonoDevelop.TestDebugService";
			state.GettingStartedDocument = "https://www.google.com/webhp?q=how+do+i+get+started";
			state.Version = "1.1";
		}

	}
	#endif
}
