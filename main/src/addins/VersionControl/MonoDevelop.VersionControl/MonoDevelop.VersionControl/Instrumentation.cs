//
// Counters.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2019 (c) Microsoft Corporation
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
using System;
using MonoDevelop.Core.Instrumentation;
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	static class Instrumentation
	{
		const string Category = "Version Control";
		const string Identifier = "VersionControl";

		public static readonly Counter<RepositoryMetadata> Repositories = InstrumentationService.CreateCounter<RepositoryMetadata> ("VersionControl.RepositoryOpened", Category, id: $"{Identifier}.RepositoryOpened");
		public static readonly TimerCounter<MultipathOperationMetadata> CheckoutCounter = InstrumentationService.CreateTimerCounter<MultipathOperationMetadata> ("Checkout", Category, id: $"{Identifier}.Checkout");
		public static readonly TimerCounter<MultipathOperationMetadata> CommitCounter = InstrumentationService.CreateTimerCounter<MultipathOperationMetadata> ("Commit", Category, id: $"{Identifier}.Commit");
		public static readonly TimerCounter<RevertMetadata> RevertCounter = InstrumentationService.CreateTimerCounter<RevertMetadata> ("Revert", Category, id: $"{Identifier}.Revert");
		public static readonly TimerCounter<PublishMetadata> PublishCounter = InstrumentationService.CreateTimerCounter<PublishMetadata> ("Publish", Category, id: $"{Identifier}.Publish");
		public static readonly TimerCounter<MultipathOperationMetadata> UpdateCounter = InstrumentationService.CreateTimerCounter<MultipathOperationMetadata> ("Update", Category, id: $"{Identifier}.Update");
		public static readonly TimerCounter<MultipathOperationMetadata> AddCounter = InstrumentationService.CreateTimerCounter<MultipathOperationMetadata> ("Add", Category, id: $"{Identifier}.Add");
		public static readonly TimerCounter<MoveMetadata> MoveCounter = InstrumentationService.CreateTimerCounter<MoveMetadata> ("Move", Category, id: $"{Identifier}.Move");
		public static readonly TimerCounter<DeleteMetadata> DeleteCounter = InstrumentationService.CreateTimerCounter<DeleteMetadata> ("Delete", Category, id: $"{Identifier}.Delete");
		public static readonly TimerCounter<MultipathOperationMetadata> LockCounter = InstrumentationService.CreateTimerCounter<MultipathOperationMetadata> ("Lock", Category, id: $"{Identifier}.Lock");
		public static readonly TimerCounter<MultipathOperationMetadata> UnlockCounter = InstrumentationService.CreateTimerCounter<MultipathOperationMetadata> ("Unlock", Category, id: $"{Identifier}.Unlock");
		public static readonly TimerCounter<MultipathOperationMetadata> IgnoreCounter = InstrumentationService.CreateTimerCounter<MultipathOperationMetadata> ("Ignore", Category, id: $"{Identifier}.Ignore");
		public static readonly TimerCounter<MultipathOperationMetadata> UnignoreCounter = InstrumentationService.CreateTimerCounter<MultipathOperationMetadata> ("Unignore", Category, id: $"{Identifier}.Unignore");

		public static readonly TimerCounter<RepositoryMetadata> GetRevisionChangesCounter = InstrumentationService.CreateTimerCounter<RepositoryMetadata> ("Get Revision Changes", Category, id: $"{Identifier}.GetRevisionChanges");
		public static readonly TimerCounter<RepositoryMetadata> GetHistoryCounter = InstrumentationService.CreateTimerCounter<RepositoryMetadata> ("Get History", Category, id: $"{Identifier}.GetHistory");
	}

	class MultipathOperationMetadata : RepositoryMetadata
	{
		public MultipathOperationMetadata ()
		{
		}

		public MultipathOperationMetadata (VersionControlSystem versionControl) : base (versionControl)
		{
		}

		public int PathsCount {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}

		public bool Recursive {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}
	}

	class RevertMetadata : MultipathOperationMetadata
	{
		public enum RevertType
		{
			LocalChanges,
			ToRevision,
			SpecificRevision
		};

		public RevertMetadata ()
		{
		}

		public RevertMetadata (VersionControlSystem versionControl) : base (versionControl)
		{
		}

		public RevertType OperationType {
			get => GetProperty<RevertType> ();
			set => SetProperty (value);
		}
	}

	class PublishMetadata : MultipathOperationMetadata
	{
		public PublishMetadata ()
		{
		}

		public PublishMetadata (VersionControlSystem versionControl) : base (versionControl)
		{
		}

		public bool SubFolder {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}
	}

	class DeleteMetadata : MultipathOperationMetadata
	{
		public DeleteMetadata ()
		{
		}

		public DeleteMetadata (VersionControlSystem versionControl) : base (versionControl)
		{
		}

		public bool Force {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}

		public bool KeepLocal {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}
	}

	class MoveMetadata : RepositoryMetadata
	{
		public enum MoveType
		{
			File,
			Directory
		}

		public MoveMetadata ()
		{
		}

		public MoveMetadata (VersionControlSystem versionControl) : base (versionControl)
		{
		}

		public bool Force {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}

		public MoveType OperationType {
			get => GetProperty<MoveType> ();
			set => SetProperty (value);
		}
	}
}
