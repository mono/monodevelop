//
// GenerateTypeOptionsService.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;
using System.Composition;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.ProjectManagement;

namespace MonoDevelop.Refactoring.GenerateType
{
	[ExportWorkspaceServiceFactory (typeof (IGenerateTypeOptionsService)), Shared]
	class GenerateTypeOptionsServiceFactory2 : IWorkspaceServiceFactory
	{
		static Lazy<IGenerateTypeOptionsService> service = new Lazy<IGenerateTypeOptionsService> (() => new GenerateTypeOptionsService ());

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{

			return service.Value;
		}

		class GenerateTypeOptionsService : IGenerateTypeOptionsService
		{
			GenerateTypeOptionsResult IGenerateTypeOptionsService.GetGenerateTypeOptions (string className, GenerateTypeDialogOptions generateTypeDialogOptions, Document document, INotificationService notificationService, IProjectManagementService projectManagementService, ISyntaxFactsService syntaxFactsService)
			{
				var dialog = new GenerateTypeDialog (className, generateTypeDialogOptions, document, notificationService, projectManagementService, syntaxFactsService);
				try {
					bool performChange = dialog.Run () == Xwt.Command.Ok;
					if (!performChange)
						return GenerateTypeOptionsResult.Cancelled;
					
					return dialog.GenerateTypeOptionsResult;
				} catch (Exception ex) {
					LoggingService.LogError ("Error while signature changing.", ex);
					return GenerateTypeOptionsResult.Cancelled;
				} finally {
					dialog.Dispose ();
				}
			}
		}
	}
}
