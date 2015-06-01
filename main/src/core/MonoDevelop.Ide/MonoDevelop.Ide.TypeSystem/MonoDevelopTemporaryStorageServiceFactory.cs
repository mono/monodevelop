//
// MonoDevelopTemporaryStorageServiceFactory.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceServiceFactory (typeof (ITemporaryStorageService), ServiceLayer.Host), Shared]
	sealed class MonoDevelopTemporaryStorageServiceFactory : IWorkspaceServiceFactory
	{
		static IWorkspaceServiceFactory microsoftFactory;

		static MonoDevelopTemporaryStorageServiceFactory ()
		{
			if (Core.Platform.IsWindows) {
				var asm = Assembly.Load ("Microsoft.CodeAnalysis.Workspaces.Desktop");
				if (asm != null) {
					var type = asm.GetType ("Microsoft.CodeAnalysis.Host.TemporaryStorageServiceFactory");
					if (type != null)
						microsoftFactory = Activator.CreateInstance (type) as IWorkspaceServiceFactory;
				}
			}
		}

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			if (microsoftFactory != null)
				return microsoftFactory.CreateService (workspaceServices);
			return new TemporaryStorageService ();
		}

		class TemporaryStorageService : ITemporaryStorageService
		{
			public ITemporaryStreamStorage CreateTemporaryStreamStorage (CancellationToken cancellationToken = default(CancellationToken))
			{
				return new StreamStorage ();
			}

			public ITemporaryTextStorage CreateTemporaryTextStorage (CancellationToken cancellationToken = default(CancellationToken))
			{
				return new TemporaryTextStorage ();
			}
		}

		sealed class StreamStorage : ITemporaryStreamStorage
		{
			MemoryStream _stream;

			public void Dispose ()
			{
				_stream?.Dispose ();
				_stream = null;
			}

			public Stream ReadStream (CancellationToken cancellationToken = default(CancellationToken))
			{
				if (_stream == null) {
					throw new InvalidOperationException ();
				}

				_stream.Position = 0;
				return _stream;
			}

			public Task<Stream> ReadStreamAsync (CancellationToken cancellationToken = default(CancellationToken))
			{
				if (_stream == null) {
					throw new InvalidOperationException ();
				}

				_stream.Position = 0;
				return Task.FromResult ((Stream)_stream);
			}

			public void WriteStream (Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				var newStream = new MemoryStream ();
				stream.CopyTo (newStream);
				_stream = newStream;
			}

			public async Task WriteStreamAsync (Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				var newStream = new MemoryStream ();
				await stream.CopyToAsync (newStream).ConfigureAwait (false);
				_stream = newStream;
			}
		}

		sealed class TemporaryTextStorage : ITemporaryTextStorage
		{
			SourceText _sourceText;

			public void Dispose ()
			{
				_sourceText = null;
			}

			public SourceText ReadText (CancellationToken cancellationToken = default(CancellationToken))
			{
				return _sourceText;
			}

			public Task<SourceText> ReadTextAsync (CancellationToken cancellationToken = default(CancellationToken))
			{
				return Task.FromResult (ReadText (cancellationToken));
			}

			public void WriteText (SourceText text, CancellationToken cancellationToken = default(CancellationToken))
			{
				// This is a trivial implementation, indeed. Note, however, that we retain a strong
				// reference to the source text, which defeats the intent of RecoverableTextAndVersion, but
				// is appropriate for this trivial implementation.
				_sourceText = text;
			}

			public Task WriteTextAsync (SourceText text, CancellationToken cancellationToken = default(CancellationToken))
			{
				WriteText (text, cancellationToken);
				return Task.FromResult (true);
			}
		}

		//class TemporaryTextStorage : ITemporaryTextStorage
		//{
		//	string fileName;
		//	Encoding encoding;
		//	WeakReference<SourceText> sourceText;

		//	public void Dispose()
		//	{
		//		if (fileName == null)
		//			return;
		//		try {
		//			File.Delete (fileName);
		//		} catch (Exception) {}
		//	}

		//	public SourceText ReadText(CancellationToken cancellationToken = default(CancellationToken))
		//	{
		//		SourceText result;
		//		if (sourceText == null || !sourceText.TryGetTarget (out result)) {
		//			var text = File.ReadAllText (fileName, encoding);
		//			result = SourceText.From (text, encoding);
		//			sourceText = new WeakReference<SourceText>(result);
		//		}
		//		return result;
		//	}

		//	public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default(CancellationToken))
		//	{
		//		return Task.Run(delegate { return ReadText (cancellationToken); });
		//	}

		//	object writeTextLocker = new object ();

		//	public void WriteText(SourceText text, CancellationToken cancellationToken = default(CancellationToken))
		//	{
		//		lock (writeTextLocker) {
		//			if (fileName == null)
		//				this.fileName = Path.GetTempFileName ();
		//			string tmpPath = Path.Combine (Path.GetDirectoryName (fileName), ".#" + Path.GetFileName (fileName));
		//			encoding = text.Encoding ?? Encoding.Default;
		//			using (var writer = new StreamWriter (tmpPath, false, text.Encoding))
		//				text.Write (writer, cancellationToken);
		//			sourceText = new WeakReference<SourceText>(text);
		//			FileService.SystemRename (tmpPath, fileName);
		//		}
		//	}

		//	Task ITemporaryTextStorage.WriteTextAsync(SourceText text, CancellationToken cancellationToken)
		//	{
		//		return Task.Run (delegate {
		//			WriteText (text, cancellationToken);
		//		});
		//	}
		//}

		//class StreamStorage : ITemporaryStreamStorage
		//{
		//	string fileName;

		//	public void Dispose()
		//	{
		//		if (fileName == null)
		//			return;
		//		try {
		//			File.Delete (fileName);
		//		} catch (Exception) {}
		//	}

		//	public Stream ReadStream(CancellationToken cancellationToken = default(CancellationToken))
		//	{
		//		return File.Open (fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
		//	}

		//	public Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
		//	{
		//		return Task.FromResult(ReadStream(cancellationToken));
		//	}

		//	public void WriteStream(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
		//	{
		//		if (fileName == null)
		//			this.fileName = Path.GetTempFileName ();
		//		using (var newStream = File.Open (fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Write)) {
		//			stream.CopyTo(newStream);
		//		}
		//	}

		//	public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
		//	{
		//		if (fileName == null)
		//			this.fileName = Path.GetTempFileName ();
		//		using (var newStream = File.Open (fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Write)) {
		//			await stream.CopyToAsync(newStream).ConfigureAwait(false);
		//		}
		//	}
		//}
	}
}

