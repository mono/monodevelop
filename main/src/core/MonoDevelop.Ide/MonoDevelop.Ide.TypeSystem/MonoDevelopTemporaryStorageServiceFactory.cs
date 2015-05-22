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

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceServiceFactory(typeof(ITemporaryStorageService), ServiceLayer.Host), Shared]
	sealed class MonoDevelopTemporaryStorageServiceFactory : IWorkspaceServiceFactory
	{
		public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
		{
			return new TemporaryStorageService();
		}

		class TemporaryStorageService : ITemporaryStorageService
		{
			public ITemporaryStreamStorage CreateTemporaryStreamStorage (CancellationToken cancellationToken = default(CancellationToken))
			{
				return new StreamStorage(Path.GetTempFileName ());
			}

			public ITemporaryTextStorage CreateTemporaryTextStorage (CancellationToken cancellationToken = default(CancellationToken))
			{
				return new TemporaryTextStorage(Path.GetTempFileName ());
			}
		}

		class TemporaryTextStorage : ITemporaryTextStorage
		{
			string fileName;

			public TemporaryTextStorage (string fileName)
			{
				this.fileName = fileName;
			}

			public void Dispose()
			{
				try {
					Console.WriteLine ("remove : "+ fileName);
					File.Delete (fileName);
				} catch (Exception) {}
			}

			public SourceText ReadText(CancellationToken cancellationToken = default(CancellationToken))
			{
				var src = StringTextSource.ReadFrom (fileName);
				Console.WriteLine (fileName +" read text : "+ src.Length);
				return SourceText.From(src.Text, src.Encoding);
			}

			public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default(CancellationToken))
			{
				return Task.Run(delegate { return ReadText (cancellationToken); });
			}

			public void WriteText(SourceText text, CancellationToken cancellationToken = default(CancellationToken))
			{
				Console.WriteLine ("write text :" + text.Length);
				using (var writer = new StreamWriter(fileName, false, text.Encoding))
					text.Write (writer, cancellationToken);
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
			Stream stream;

			public StreamStorage (string fileName)
			{
				this.fileName = fileName;
			}

			public void Dispose()
			{
				stream.Close ();
				try {
					File.Delete (fileName);
				} catch (Exception) {}

			}

			public Stream ReadStream(CancellationToken cancellationToken = default(CancellationToken))
			{
				return File.Open (fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			}

			public Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
			{
				return Task.FromResult((Stream)File.Open (fileName, FileMode.Open, FileAccess.Read, FileShare.Read));
			}

			public void WriteStream(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				using (var newStream = File.Open (fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Write)) {
					stream.CopyTo(newStream);
				}
			}

			public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				using (var newStream = File.Open (fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Write)) {
					await stream.CopyToAsync(newStream).ConfigureAwait(false);
				}
			}
		}
	}
}

