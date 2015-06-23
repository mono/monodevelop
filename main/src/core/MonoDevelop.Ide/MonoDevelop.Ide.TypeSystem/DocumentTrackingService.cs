// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis;
using System.Composition;

namespace MonoDevelop.Ide.TypeSystem
{
	//MD is added to name to make it clear this is not IDocumentTrackingService from Microsoft.CodeAnalyis.Features.dll
	//But it does mimic it's behavior
	interface IMDDocumentTrackingService : IWorkspaceService
	{
		event EventHandler<DocumentId> ActiveDocumentChanged;

		DocumentId GetActiveDocument ();
	}

//	[ExportWorkspaceServiceFactory (typeof(IMDDocumentTrackingService), ServiceLayer.Host), Shared]
	class MonoDevelopDocumentTrackingServiceFactory : IWorkspaceServiceFactory
	{
		private IMDDocumentTrackingService _singleton;

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return _singleton ?? (_singleton = new MonoDevelopDocumentTrackingService ());
		}

		public class MonoDevelopDocumentTrackingService : IMDDocumentTrackingService
		{
			public MonoDevelopDocumentTrackingService ()
			{
				if (IdeApp.IsInitialized)
					IdeApp.Workbench.ActiveDocumentChanged += MonoDevelop_Ide_IdeApp_Workbench_ActiveDocumentChanged;
			}

			#region IDocumentTrackingService implementation

			public event EventHandler<DocumentId> ActiveDocumentChanged;

			public DocumentId GetActiveDocument ()
			{
				var document = IdeApp.Workbench?.ActiveDocument;
				if (document == null)
					return null;
				return TypeSystemService.GetDocumentId (document.Project, document.FileName);
			}

			#endregion

			void MonoDevelop_Ide_IdeApp_Workbench_ActiveDocumentChanged (object sender, EventArgs e)
			{
				ActiveDocumentChanged?.Invoke (null, GetActiveDocument ());
			}
		}
	}
}

