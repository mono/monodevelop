// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.PackageManagement.UI
{
	/// <summary>
	/// List of possible statuses of items loading operation (search).
	/// Utilized by item loader and UI for progress tracking.
	/// </summary>
	public enum LoadingStatus
	{
		Unknown, // not initialized
		Cancelled, // loading cancelled
		ErrorOccurred, // error occurred
		Loading, // loading is running in background
		NoItemsFound, // loading complete, no items found
		NoMoreItems, // loading complete, no more items discovered beyond current page
		Ready // loading of current page is done, next page is available
	}
}
