//
// RegisteredPackageSourcesViewModelTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet.Configuration;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class RegisteredPackageSourcesViewModelTests
	{
		RegisteredPackageSourcesViewModel viewModel;
		List<string> propertiesChanged;
		FakePackageSourceProvider packageSourceProvider;
		List<PackageSource> packageSources;

		void CreateViewModel ()
		{
			packageSourceProvider = new FakePackageSourceProvider ();
			packageSources = packageSourceProvider.PackageSources;
			viewModel = new RegisteredPackageSourcesViewModel (packageSourceProvider, new FolderBrowser (), null);
		}

		void CreateViewModelWithOnePackageSource ()
		{
			CreateViewModel ();
			AddPackageSourceToProvider ("Source 1", "http://url1");
		}

		void CreateViewModelWithTwoPackageSources ()
		{
			CreateViewModel ();
			AddPackageSourceToProvider ("Source 1", "http://url1");
			AddPackageSourceToProvider ("Source 2", "http://url2");
		}

		void AddPackageSourceToProvider (string name, string url)
		{
			var source = new PackageSource (url, name);
			packageSources.Add (source);
		}

		void RecordPropertyChanges ()
		{
			propertiesChanged = new List<string> ();
			viewModel.PropertyChanged += (sender, e) => propertiesChanged.Add (e.PropertyName);
		}

		[Test]
		public void Constructor_InstanceCreated_NoPackageSourceViewModels ()
		{
			CreateViewModel ();

			Assert.AreEqual (0, viewModel.PackageSourceViewModels.Count);
		}

		[Test]
		public void Load_OptionsHasOneRegisteredPackageSource_ViewModelHasOnePackageSourceViewModel ()
		{
			CreateViewModelWithOnePackageSource ();
			viewModel.Load ();

			Assert.AreEqual (1, viewModel.PackageSourceViewModels.Count);
		}

		[Test]
		public void Load_OptionsHasOneRegisteredPackageSource_ViewModelHasOnePackageSourceViewModelWithPackageSourceFromOptions ()
		{
			CreateViewModelWithOnePackageSource ();
			viewModel.Load ();

			var expectedSources = new PackageSource[] {
				packageSources [0]
			};

			PackageSourceCollectionAssert.AreEqual (expectedSources, viewModel.PackageSourceViewModels);
		}

		[Test]
		public void Load_OptionsHasTwoRegisteredPackageSources_ViewModelHasTwoPackageSourceViewModelWithPackageSourcesFromOptions ()
		{
			CreateViewModelWithTwoPackageSources ();
			viewModel.Load ();

			var expectedSources = new PackageSource[] {
				packageSources [0],
				packageSources [1]
			};

			PackageSourceCollectionAssert.AreEqual (expectedSources, viewModel.PackageSourceViewModels);
		}

		[Test]
		public void Load_PackageSourceModifiedAfterLoadAndSaveNotCalled_RegisteredPackageSourcesInOptionsUnchanged ()
		{
			CreateViewModel ();
			AddPackageSourceToProvider ("Test", "http://monodevelop.com");
			viewModel.Load ();

			PackageSourceViewModel packageSourceViewModel = viewModel.PackageSourceViewModels [0];
			packageSourceViewModel.Name = "Changed-Name";
			packageSourceViewModel.Source  = "changed-url";

			var expectedSources = new PackageSource[] {
				new PackageSource ("http://monodevelop.com", "Test")
			};

			PackageSourceCollectionAssert.AreEqual (expectedSources, packageSources);
		}

		[Test]
		public void Save_PackageSourceModifiedAfterLoad_RegisteredPackageSourcesInOptionsUpdated ()
		{
			CreateViewModel ();
			AddPackageSourceToProvider ("Test", "http://monodevelop.com");
			viewModel.Load ();

			PackageSourceViewModel packageSourceViewModel = viewModel.PackageSourceViewModels [0];
			packageSourceViewModel.Name = "Test-updated";
			packageSourceViewModel.Source  = "url-updated";

			viewModel.Save ();

			var expectedSources = new PackageSource[] {
				new PackageSource ("url-updated", "Test-updated")
			};

			PackageSourceCollectionAssert.AreEqual (expectedSources, packageSources);
		}

		[Test]
		public void Save_OnePackageSourceAddedAfterLoadAndBeforeSave_TwoRegisteredPackageSourcesInOptions ()
		{
			CreateViewModel ();
			AddPackageSourceToProvider ("Test", "http://monodevelop.com/1");
			viewModel.Load ();

			var newSource = new PackageSource ("http://monodevelop.com/2", "Test");

			var newPackageSourceViewModel = new PackageSourceViewModel (newSource);
			viewModel.PackageSourceViewModels.Add (newPackageSourceViewModel);

			viewModel.Save ();

			var expectedSource = new PackageSource ("http://monodevelop.com/1", "Test");

			var expectedSources = new PackageSource[] {
				expectedSource,
				newSource
			};

			PackageSourceCollectionAssert.AreEqual (expectedSources, packageSources);
		}

		[Test]
		public void AddPackageSourceCommand_CommandExecuted_AddsPackageSourceToPackageSourceViewModelsCollection ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceName = "Test";
			viewModel.NewPackageSourceUrl  = "http://monodevelop.com";

			viewModel.AddPackageSourceCommand.Execute (null);

			var expectedSources = new PackageSource[] {
				new PackageSource ("http://monodevelop.com", "Test")
			};

			PackageSourceCollectionAssert.AreEqual (expectedSources, viewModel.PackageSourceViewModels);
		}

		[Test]
		public void AddPackageSourceCommand_NewPackageSourceHasNameButNoUrl_CanExecuteReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceName = "Test";
			viewModel.NewPackageSourceUrl  = null;

			bool result = viewModel.AddPackageSourceCommand.CanExecute (null);

			Assert.IsFalse (result);
		}

		[Test]
		public void AddPackageSourceCommand_NewPackageSourceHasNameAndUrl_CanExecuteReturnsTrue ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceName = "Test";
			viewModel.NewPackageSourceUrl  = "http://codeplex.com";

			bool result = viewModel.AddPackageSourceCommand.CanExecute (null);

			Assert.IsTrue (result);
		}

		[Test]
		public void AddPackageSourceCommand_NewPackageSourceHasUrlButNoName_CanExecuteReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceName = null;
			viewModel.NewPackageSourceUrl  = "http://codeplex.com";

			bool result = viewModel.AddPackageSourceCommand.CanExecute (null);

			Assert.IsFalse (result);
		}

		[Test]
		public void AddPackageSource_NoExistingPackageSources_SelectsPackageSourceViewModel ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceUrl  = "http://url";
			viewModel.NewPackageSourceName = "abc";

			viewModel.AddPackageSource ();

			PackageSourceViewModel expectedViewModel = viewModel.PackageSourceViewModels [0];

			Assert.AreEqual (expectedViewModel, viewModel.SelectedPackageSourceViewModel);
		}

		[Test]
		public void NewPackageSourceName_Changed_NewPackageSourceNameUpdated ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceName = "Test";

			Assert.AreEqual ("Test", viewModel.NewPackageSourceName);
		}

		[Test]
		public void NewPackageSourceUrl_Changed_NewPackageSourceUrlUpdated ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceUrl  = "Test";

			Assert.AreEqual ("Test", viewModel.NewPackageSourceUrl);
		}

		[Test]
		public void RemovePackageSourceCommand_TwoPackagesSourcesInListAndOnePackageSourceSelected_PackageSourceIsRemoved ()
		{
			CreateViewModelWithTwoPackageSources ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [0];

			viewModel.RemovePackageSourceCommand.Execute (null);

			var expectedSources = new PackageSource[] {
				packageSources [1]
			};

			PackageSourceCollectionAssert.AreEqual (expectedSources, viewModel.PackageSourceViewModels);
		}

		[Test]
		public void RemovePackageSourceCommand_NoPackageSourceSelected_CanExecuteReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = null;

			bool result = viewModel.RemovePackageSourceCommand.CanExecute (null);

			Assert.IsFalse (result);
		}

		[Test]
		public void RemovePackageSourceCommand_PackageSourceSelected_CanExecuteReturnsTrue ()
		{
			CreateViewModelWithOnePackageSource ();

			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [0];

			bool result = viewModel.RemovePackageSourceCommand.CanExecute (null);

			Assert.IsTrue (result);
		}

		[Test]
		public void SelectedPackageSourceViewModel_Changed_PropertyChangedEventFiredForCanAddPackageSource ()
		{
			CreateViewModelWithOnePackageSource ();
			viewModel.Load ();

			string propertyName = null;
			viewModel.PropertyChanged += (sender, e) => propertyName = e.PropertyName;

			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [0];

			Assert.AreEqual ("CanAddPackageSource", propertyName);
		}

		[Test]
		public void MovePackageSourceUpCommand_TwoPackagesSourcesInListAndLastPackageSourceSelected_PackageSourceIsMovedUp ()
		{
			CreateViewModelWithTwoPackageSources ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [1];

			viewModel.MovePackageSourceUpCommand.Execute (null);

			var expectedSources = new PackageSource[] {
				packageSources [1],
				packageSources [0]
			};

			PackageSourceCollectionAssert.AreEqual (expectedSources, viewModel.PackageSourceViewModels);
		}

		[Test]
		public void MovePackageSourceUpCommand_FirstPackageSourceSelected_CanExecuteReturnsFalse ()
		{
			CreateViewModelWithTwoPackageSources ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [0];

			bool result = viewModel.MovePackageSourceUpCommand.CanExecute (null);

			Assert.IsFalse (result);
		}

		[Test]
		public void MovePackageSourceUpCommand_LastPackageSourceSelected_CanExecuteReturnsTrue ()
		{
			CreateViewModelWithTwoPackageSources ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [1];

			bool result = viewModel.MovePackageSourceUpCommand.CanExecute (null);

			Assert.IsTrue (result);
		}

		[Test]
		public void CanMovePackageSourceUp_NoPackages_ReturnsFalse ()
		{
			CreateViewModel ();
			viewModel.Load ();

			bool result = viewModel.CanMovePackageSourceUp;

			Assert.IsFalse (result);
		}

		[Test]
		public void MovePackageSourceDownCommand_TwoPackagesSourcesAndFirstPackageSourceSelected_PackageSourceIsMovedDown ()
		{
			CreateViewModelWithTwoPackageSources ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [0];

			viewModel.MovePackageSourceDownCommand.Execute (null);

			var expectedSources = new PackageSource[] {
				packageSources [1],
				packageSources [0]
			};

			PackageSourceCollectionAssert.AreEqual (expectedSources, viewModel.PackageSourceViewModels);
		}

		[Test]
		public void MovePackageSourceDownCommand_TwoPackageSourcesAndLastPackageSourceSelected_CanExecuteReturnsFalse ()
		{
			CreateViewModelWithTwoPackageSources ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [1];

			bool result = viewModel.MovePackageSourceDownCommand.CanExecute (null);

			Assert.IsFalse (result);
		}

		[Test]
		public void MovePackageSourceDownCommand_TwoPackageSourcesAndFirstPackageSourceSelected_CanExecuteReturnsTrue ()
		{
			CreateViewModelWithTwoPackageSources ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [0];

			bool result = viewModel.MovePackageSourceDownCommand.CanExecute (null);

			Assert.IsTrue (result);
		}

		[Test]
		public void CanMovePackageSourceDown_NoPackageSources_ReturnFalse ()
		{
			CreateViewModel ();
			viewModel.Load ();

			bool result = viewModel.CanMovePackageSourceDown;

			Assert.IsFalse (result);
		}

		[Test]
		public void CanMovePackageSourceDown_OnePackageSourceAndPackageSourceIsSelected_ReturnsFalse ()
		{
			CreateViewModelWithOnePackageSource ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [0];

			bool result = viewModel.CanMovePackageSourceDown;

			Assert.IsFalse (result);
		}

		[Test]
		public void CanMovePackageSourceDown_OnePackageSourceAndNothingIsSelected_ReturnsFalse ()
		{
			CreateViewModelWithOnePackageSource ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = null;

			bool result = viewModel.CanMovePackageSourceDown;

			Assert.IsFalse (result);
		}

		[Test]
		public void CanMovePackageSourceUp_OnePackageSourceAndNothingIsSelected_ReturnsFalse ()
		{
			CreateViewModelWithOnePackageSource ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = null;

			bool result = viewModel.CanMovePackageSourceUp;

			Assert.IsFalse (result);
		}

		[Test]
		public void CanMovePackageSourceUp_TwoPackageSourcesAndNothingIsSelected_ReturnsFalse ()
		{
			CreateViewModelWithTwoPackageSources ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = null;

			bool result = viewModel.CanMovePackageSourceUp;

			Assert.IsFalse (result);
		}

		[Test]
		public void CanMovePackageSourceDown_TwoPackageSourcesAndNothingIsSelected_ReturnsFalse ()
		{
			CreateViewModelWithTwoPackageSources ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = null;

			bool result = viewModel.CanMovePackageSourceDown;

			Assert.IsFalse (result);
		}

		[Test]
		public void SelectedPackageSourceViewModel_PropertyChanged_FiresPropertyChangedEvent ()
		{
			CreateViewModelWithOnePackageSource ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [0];

			List<string> propertyNames = new List<string> ();
			viewModel.PropertyChanged += (sender, e) => propertyNames.Add (e.PropertyName);
			viewModel.SelectedPackageSourceViewModel = null;

			Assert.IsTrue (propertyNames.Contains ("SelectedPackageSourceViewModel"));
		}

		[Test]
		public void AddPackageSource_OneExistingPackageSources_FiresPropertyChangedEventForSelectedPackageSource ()
		{
			CreateViewModelWithOnePackageSource ();
			viewModel.Load ();
			viewModel.SelectedPackageSourceViewModel = viewModel.PackageSourceViewModels [0];
			viewModel.NewPackageSourceUrl = "http://url";
			viewModel.NewPackageSourceName = "Test";

			List<string> propertyNames = new List<string> ();
			viewModel.PropertyChanged += (sender, e) => propertyNames.Add (e.PropertyName);
			viewModel.AddPackageSource ();

			Assert.IsTrue (propertyNames.Contains ("SelectedPackageSourceViewModel"));
		}

		[Test]
		public void AddPackageSource_NewPackageSourceHasEmptyStringPassword_DoesNotThrowCryptographicExceptionAndNewPackageSourceAddedWithNullPassword ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceUrl  = "http://url";
			viewModel.NewPackageSourceName = "abc";
			viewModel.NewPackageSourcePassword = "";

			Assert.DoesNotThrow (() => viewModel.AddPackageSource ());

			PackageSourceViewModel expectedViewModel = viewModel.PackageSourceViewModels [0];

			Assert.IsNull (expectedViewModel.Password);
			Assert.AreEqual ("abc", expectedViewModel.Name);
			Assert.AreEqual ("http://url", expectedViewModel.Source);
		}

		[Test]
		public void AddPackageSource_NewPackageSourceHasPassword_DoesNotThrowCryptographicExceptionAndNewPackageSourceAddedWithPassword ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceUrl  = "http://url";
			viewModel.NewPackageSourceName = "abc";
			viewModel.NewPackageSourcePassword = "test";

			Assert.DoesNotThrow (() => viewModel.AddPackageSource ());

			PackageSourceViewModel expectedViewModel = viewModel.PackageSourceViewModels [0];

			Assert.AreEqual ("test", expectedViewModel.Password);
			Assert.AreEqual ("abc", expectedViewModel.Name);
			Assert.AreEqual ("http://url", expectedViewModel.Source);
		}

		[Test]
		public void NewPackageSourceName_ChangedWithWhitespaceAtStartAndEnd_NewPackageSourceNameWhitespaceIsTrimmed ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceName = "  Test  ";

			Assert.AreEqual ("Test", viewModel.NewPackageSourceName);
		}

		[Test]
		public void NewPackageSourceUrl_ChangedWithWhitespaceAtStartAndEnd_NewPackageSourceUrlWhitespaceIsTrimmed ()
		{
			CreateViewModel ();
			viewModel.Load ();
			viewModel.NewPackageSourceUrl  = " Test ";

			Assert.AreEqual ("Test", viewModel.NewPackageSourceUrl);
		}
	}
}

