//
// ExtractInterfaceOptionService.cs
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
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Composition;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.LanguageServices;
using System.Collections.Generic;

namespace MonoDevelop.Refactoring.ExtractInterface
{
	[ExportWorkspaceService (typeof (IExtractInterfaceOptionsService), ServiceLayer.Default), Shared]
	class ExtractInterfaceOptionsService : IExtractInterfaceOptionsService
	{
		public ExtractInterfaceOptionsResult GetExtractInterfaceOptions (ISyntaxFactsService syntaxFactsService, INotificationService notificationService, List<ISymbol> extractableMembers, string defaultInterfaceName, List<string> conflictingTypeNames, string defaultNamespace, string generatedNameTypeParameterSuffix, string languageName)
		{

			var dialog = new ExtractInterfaceDialog ();
			try {
				dialog.Init (syntaxFactsService, notificationService, extractableMembers, defaultInterfaceName, conflictingTypeNames, defaultNamespace, generatedNameTypeParameterSuffix, languageName);
				bool performChange = dialog.Run () == Xwt.Command.Ok;
				if (!performChange)
					return ExtractInterfaceOptionsResult.Cancelled;

				return new ExtractInterfaceOptionsResult (false, dialog.IncludedMembers, dialog.InterfaceName, dialog.FileName);
			} catch (Exception ex) {
				LoggingService.LogError ("Error while signature changing.", ex);
				return ExtractInterfaceOptionsResult.Cancelled;
			} finally {
				dialog.Dispose ();
			}
		}
	}
}
