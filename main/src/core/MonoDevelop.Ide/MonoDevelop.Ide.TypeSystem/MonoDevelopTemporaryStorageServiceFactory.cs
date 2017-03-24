// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core.Text;
using System.Reflection;
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceServiceFactory (typeof (ITemporaryStorageService), MonoDevelopWorkspace.ServiceLayer), Shared]
	sealed class MonoDevelopTemporaryStorageServiceFactory : IWorkspaceServiceFactory
	{
		static IWorkspaceServiceFactory microsoftFactory;

		static MonoDevelopTemporaryStorageServiceFactory ()
		{
			// On Mac, the mmap TemporaryStorageServiceFactory crashes the IDE by burning all available file handles
			if (Core.Platform.IsMac) {
				return;
			}

			try {
				var asm = Assembly.Load ("Microsoft.CodeAnalysis.Workspaces.Desktop");
				if (asm != null) {
					var type = asm.GetType ("Microsoft.CodeAnalysis.Host.TemporaryStorageServiceFactory");
					if (type != null)
						microsoftFactory = Activator.CreateInstance (type) as IWorkspaceServiceFactory;
				}
			} catch (Exception e) {
				LoggingService.LogWarning ("Can't load TemporaryStorageServiceFactory, falling back to default.", e);
			}
		}

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			if (microsoftFactory != null) {
				return microsoftFactory.CreateService (workspaceServices);
			}
			return new TemporaryStorageService ();
		}

		class TemporaryStorageService : ITemporaryStorageService
		{
			List<IDisposable> serviceList = new List<IDisposable> ();

			void DisposeServices ()
			{
				foreach (var service in serviceList)
					service.Dispose ();
				serviceList.Clear ();
			}

			public TemporaryStorageService ()
			{
				IdeApp.Workspace.LastWorkspaceItemClosed += delegate {
					DisposeServices ();
				};
			}

			public ITemporaryStreamStorage CreateTemporaryStreamStorage (CancellationToken cancellationToken = default(CancellationToken))
			{
				var result = new StreamStorage ();
				serviceList.Add (result);
				return result;
			}

			public ITemporaryTextStorage CreateTemporaryTextStorage (CancellationToken cancellationToken = default(CancellationToken))
			{
				var result = new TemporaryTextStorage ();
				serviceList.Add (result);
				return result;
			}
		}

		class TemporaryTextStorage : ITemporaryTextStorage
		{
			string fileName;
			Encoding encoding;
			WeakReference<SourceText> sourceText;

			public void Dispose()
			{
				if (fileName == null)
					return;
				try {
					File.Delete (fileName);
					fileName = null;
				} catch (Exception) {}
			}

			public SourceText ReadText(CancellationToken cancellationToken = default(CancellationToken))
			{
				SourceText result;
				if (sourceText == null || !sourceText.TryGetTarget (out result)) {
					var text = File.ReadAllText (fileName, encoding);
					result = SourceText.From (text, encoding);
					sourceText = new WeakReference<SourceText>(result);
				}
				return result;
			}

			public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default(CancellationToken))
			{
				return Task.Run(delegate { return ReadText (cancellationToken); });
			}

			object writeTextLocker = new object ();

			public void WriteText(SourceText text, CancellationToken cancellationToken = default(CancellationToken))
			{
				lock (writeTextLocker) {
					if (fileName == null)
						this.fileName = Path.GetTempFileName ();
					string tmpPath = Path.Combine (Path.GetDirectoryName (fileName), ".#" + Path.GetFileName (fileName));
					encoding = text.Encoding ?? Encoding.Default;
					using (var writer = new StreamWriter (tmpPath, false, encoding))
						text.Write (writer, cancellationToken);
					sourceText = new WeakReference<SourceText>(text);
					FileService.SystemRename (tmpPath, fileName);
				}
			}

			Task ITemporaryTextStorage.WriteTextAsync(SourceText text, CancellationToken cancellationToken)
			{
				return Task.Run (delegate {
					WriteText (text, cancellationToken);
				});
			}
		}

		class StreamStorage : ITemporaryStreamStorage
		{
			string fileName;

			public StreamStorage ()
			{
			}

			public void Dispose()
			{
				if (fileName == null)
					return;
				try {
					File.Delete (fileName);
					fileName = null;
				} catch (Exception) {}
			}

			public Stream ReadStream(CancellationToken cancellationToken = default(CancellationToken))
			{
				return File.Open (fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			}

			public Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
			{
				return Task.FromResult(ReadStream(cancellationToken));
			}

			public void WriteStream(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				if (fileName == null)
					this.fileName = Path.GetTempFileName ();
				using (var newStream = File.Open (fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Write)) {
					stream.CopyTo(newStream);
				}
			}

			public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				if (fileName == null)
					this.fileName = Path.GetTempFileName ();
				using (var newStream = File.Open (fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Write)) {
					await stream.CopyToAsync(newStream).ConfigureAwait(false);
				}
			}
		}
	}
}

