using System;
using ICSharpCode.PackageManagement;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class TestablePackageViewModel : PackageViewModel
	{
		public FakePackageManagementSolution FakeSolution;
		public PackageManagementEvents PackageManagementEvents;
		public FakePackage FakePackage;
		public FakeLogger FakeLogger;

		public TestablePackageViewModel (
			IPackageViewModelParent parent,
			FakePackageManagementSolution solution)
			: this (
				parent,
				new FakePackage ("Test"),
				new PackageManagementSelectedProjects (solution),
				new PackageManagementEvents (),
				new FakeLogger ())
		{
			this.FakeSolution = solution;
		}

		public TestablePackageViewModel (
			IPackageViewModelParent parent,
			FakePackage package,
			PackageManagementSelectedProjects selectedProjects,
			PackageManagementEvents packageManagementEvents,
			FakeLogger logger)
			: base (
				parent,
				package,
				selectedProjects,
				packageManagementEvents,
				null,
				logger)
		{
			this.FakePackage = package;
			this.PackageManagementEvents = packageManagementEvents;
			this.FakeLogger = logger;
		}

		protected override PackageViewModelOperationLogger CreateLogger (ILogger logger)
		{
			PackageViewModelOperationLogger operationLogger = base.CreateLogger (logger);
			operationLogger.AddingPackageMessageFormat = "Installing...{0}";
			operationLogger.RemovingPackageMessageFormat = "Uninstalling...{0}";
			operationLogger.ManagingPackageMessageFormat = "Managing...{0}";
			OperationLoggerCreated = operationLogger;
			return operationLogger;
		}

		public PackageViewModelOperationLogger OperationLoggerCreated;

		public PackageOperation AddOneFakeInstallPackageOperationForViewModelPackage ()
		{
			var operation = new FakePackageOperation (FakePackage, PackageAction.Install);

			FakeSolution
				.FakeProjectToReturnFromGetProject
				.FakeInstallOperations
				.Add (operation);

			return operation;
		}

		public PackageOperation AddOneFakeUninstallPackageOperation ()
		{
			var package = new FakePackage ("PackageToUninstall");
			var operation = new FakePackageOperation (package, PackageAction.Uninstall);
			FakeSolution.FakeProjectToReturnFromGetProject.FakeInstallOperations.Add (operation);
			return operation;
		}
	}
}

