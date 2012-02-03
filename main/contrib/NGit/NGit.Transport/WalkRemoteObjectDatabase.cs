/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.Collections.Generic;
using System.IO;
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Transfers object data through a dumb transport.</summary>
	/// <remarks>
	/// Transfers object data through a dumb transport.
	/// <p>
	/// Implementations are responsible for resolving path names relative to the
	/// <code>objects/</code> subdirectory of a single remote Git repository or
	/// naked object database and make the content available as a Java input stream
	/// for reading during fetch. The actual object traversal logic to determine the
	/// names of files to retrieve is handled through the generic, protocol
	/// independent
	/// <see cref="WalkFetchConnection">WalkFetchConnection</see>
	/// .
	/// </remarks>
	internal abstract class WalkRemoteObjectDatabase
	{
		internal static readonly string ROOT_DIR = "../";

		internal static readonly string INFO_PACKS = "info/packs";

		internal static readonly string INFO_ALTERNATES = "info/alternates";

		internal static readonly string INFO_HTTP_ALTERNATES = "info/http-alternates";

		internal static readonly string INFO_REFS = ROOT_DIR + Constants.INFO_REFS;

		internal abstract URIish GetURI();

		/// <summary>Obtain the list of available packs (if any).</summary>
		/// <remarks>
		/// Obtain the list of available packs (if any).
		/// <p>
		/// Pack names should be the file name in the packs directory, that is
		/// <code>pack-035760ab452d6eebd123add421f253ce7682355a.pack</code>. Index
		/// names should not be included in the returned collection.
		/// </remarks>
		/// <returns>list of pack names; null or empty list if none are available.</returns>
		/// <exception cref="System.IO.IOException">
		/// The connection is unable to read the remote repository's list
		/// of available pack files.
		/// </exception>
		internal abstract ICollection<string> GetPackNames();

		/// <summary>Obtain alternate connections to alternate object databases (if any).</summary>
		/// <remarks>
		/// Obtain alternate connections to alternate object databases (if any).
		/// <p>
		/// Alternates are typically read from the file
		/// <see cref="INFO_ALTERNATES">INFO_ALTERNATES</see>
		/// or
		/// <see cref="INFO_HTTP_ALTERNATES">INFO_HTTP_ALTERNATES</see>
		/// . The content of each line must be resolved
		/// by the implementation and a new database reference should be returned to
		/// represent the additional location.
		/// <p>
		/// Alternates may reuse the same network connection handle, however the
		/// fetch connection will
		/// <see cref="Close()">Close()</see>
		/// each created alternate.
		/// </remarks>
		/// <returns>
		/// list of additional object databases the caller could fetch from;
		/// null or empty list if none are configured.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// The connection is unable to read the remote repository's list
		/// of configured alternates.
		/// </exception>
		internal abstract ICollection<WalkRemoteObjectDatabase> GetAlternates();

		/// <summary>Open a single file for reading.</summary>
		/// <remarks>
		/// Open a single file for reading.
		/// <p>
		/// Implementors should make every attempt possible to ensure
		/// <see cref="System.IO.FileNotFoundException">System.IO.FileNotFoundException</see>
		/// is used when the remote object does not
		/// exist. However when fetching over HTTP some misconfigured servers may
		/// generate a 200 OK status message (rather than a 404 Not Found) with an
		/// HTML formatted message explaining the requested resource does not exist.
		/// Callers such as
		/// <see cref="WalkFetchConnection">WalkFetchConnection</see>
		/// are prepared to handle this
		/// by validating the content received, and assuming content that fails to
		/// match its hash is an incorrectly phrased FileNotFoundException.
		/// </remarks>
		/// <param name="path">
		/// location of the file to read, relative to this objects
		/// directory (e.g.
		/// <code>cb/95df6ab7ae9e57571511ef451cf33767c26dd2</code> or
		/// <code>pack/pack-035760ab452d6eebd123add421f253ce7682355a.pack</code>).
		/// </param>
		/// <returns>a stream to read from the file. Never null.</returns>
		/// <exception cref="System.IO.FileNotFoundException">the requested file does not exist at the given location.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">
		/// The connection is unable to read the remote's file, and the
		/// failure occurred prior to being able to determine if the file
		/// exists, or after it was determined to exist but before the
		/// stream could be created.
		/// </exception>
		internal abstract WalkRemoteObjectDatabase.FileStream Open(string path);

		/// <summary>
		/// Create a new connection for a discovered alternate object database
		/// <p>
		/// This method is typically called by
		/// <see cref="ReadAlternates(string)">ReadAlternates(string)</see>
		/// when
		/// subclasses us the generic alternate parsing logic for their
		/// implementation of
		/// <see cref="GetAlternates()">GetAlternates()</see>
		/// .
		/// </summary>
		/// <param name="location">
		/// the location of the new alternate, relative to the current
		/// object database.
		/// </param>
		/// <returns>
		/// a new database connection that can read from the specified
		/// alternate.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// The database connection cannot be established with the
		/// alternate, such as if the alternate location does not
		/// actually exist and the connection's constructor attempts to
		/// verify that.
		/// </exception>
		internal abstract WalkRemoteObjectDatabase OpenAlternate(string location);

		/// <summary>Close any resources used by this connection.</summary>
		/// <remarks>
		/// Close any resources used by this connection.
		/// <p>
		/// If the remote repository is contacted by a network socket this method
		/// must close that network socket, disconnecting the two peers. If the
		/// remote repository is actually local (same system) this method must close
		/// any open file handles used to read the "remote" repository.
		/// </remarks>
		internal abstract void Close();

		/// <summary>Delete a file from the object database.</summary>
		/// <remarks>
		/// Delete a file from the object database.
		/// <p>
		/// Path may start with <code>../</code> to request deletion of a file that
		/// resides in the repository itself.
		/// <p>
		/// When possible empty directories must be removed, up to but not including
		/// the current object database directory itself.
		/// <p>
		/// This method does not support deletion of directories.
		/// </remarks>
		/// <param name="path">
		/// name of the item to be removed, relative to the current object
		/// database.
		/// </param>
		/// <exception cref="System.IO.IOException">deletion is not supported, or deletion failed.
		/// 	</exception>
		internal virtual void DeleteFile(string path)
		{
			throw new IOException(MessageFormat.Format(JGitText.Get().deletingNotSupported, path
				));
		}

		/// <summary>Open a remote file for writing.</summary>
		/// <remarks>
		/// Open a remote file for writing.
		/// <p>
		/// Path may start with <code>../</code> to request writing of a file that
		/// resides in the repository itself.
		/// <p>
		/// The requested path may or may not exist. If the path already exists as a
		/// file the file should be truncated and completely replaced.
		/// <p>
		/// This method creates any missing parent directories, if necessary.
		/// </remarks>
		/// <param name="path">
		/// name of the file to write, relative to the current object
		/// database.
		/// </param>
		/// <returns>
		/// stream to write into this file. Caller must close the stream to
		/// complete the write request. The stream is not buffered and each
		/// write may cause a network request/response so callers should
		/// buffer to smooth out small writes.
		/// </returns>
		/// <param name="monitor">
		/// (optional) progress monitor to post write completion to during
		/// the stream's close method.
		/// </param>
		/// <param name="monitorTask">(optional) task name to display during the close method.
		/// 	</param>
		/// <exception cref="System.IO.IOException">
		/// writing is not supported, or attempting to write the file
		/// failed, possibly due to permissions or remote disk full, etc.
		/// </exception>
		internal virtual OutputStream WriteFile(string path, ProgressMonitor monitor, string
			 monitorTask)
		{
			throw new IOException(MessageFormat.Format(JGitText.Get().writingNotSupported, path
				));
		}

		/// <summary>Atomically write a remote file.</summary>
		/// <remarks>
		/// Atomically write a remote file.
		/// <p>
		/// This method attempts to perform as atomic of an update as it can,
		/// reducing (or eliminating) the time that clients might be able to see
		/// partial file content. This method is not suitable for very large
		/// transfers as the complete content must be passed as an argument.
		/// <p>
		/// Path may start with <code>../</code> to request writing of a file that
		/// resides in the repository itself.
		/// <p>
		/// The requested path may or may not exist. If the path already exists as a
		/// file the file should be truncated and completely replaced.
		/// <p>
		/// This method creates any missing parent directories, if necessary.
		/// </remarks>
		/// <param name="path">
		/// name of the file to write, relative to the current object
		/// database.
		/// </param>
		/// <param name="data">complete new content of the file.</param>
		/// <exception cref="System.IO.IOException">
		/// writing is not supported, or attempting to write the file
		/// failed, possibly due to permissions or remote disk full, etc.
		/// </exception>
		internal virtual void WriteFile(string path, byte[] data)
		{
			OutputStream os = WriteFile(path, null, null);
			try
			{
				os.Write(data);
			}
			finally
			{
				os.Close();
			}
		}

		/// <summary>Delete a loose ref from the remote repository.</summary>
		/// <remarks>Delete a loose ref from the remote repository.</remarks>
		/// <param name="name">
		/// name of the ref within the ref space, for example
		/// <code>refs/heads/pu</code>.
		/// </param>
		/// <exception cref="System.IO.IOException">deletion is not supported, or deletion failed.
		/// 	</exception>
		internal virtual void DeleteRef(string name)
		{
			DeleteFile(ROOT_DIR + name);
		}

		/// <summary>Delete a reflog from the remote repository.</summary>
		/// <remarks>Delete a reflog from the remote repository.</remarks>
		/// <param name="name">
		/// name of the ref within the ref space, for example
		/// <code>refs/heads/pu</code>.
		/// </param>
		/// <exception cref="System.IO.IOException">deletion is not supported, or deletion failed.
		/// 	</exception>
		internal virtual void DeleteRefLog(string name)
		{
			DeleteFile(ROOT_DIR + Constants.LOGS + "/" + name);
		}

		/// <summary>Overwrite (or create) a loose ref in the remote repository.</summary>
		/// <remarks>
		/// Overwrite (or create) a loose ref in the remote repository.
		/// <p>
		/// This method creates any missing parent directories, if necessary.
		/// </remarks>
		/// <param name="name">
		/// name of the ref within the ref space, for example
		/// <code>refs/heads/pu</code>.
		/// </param>
		/// <param name="value">new value to store in this ref. Must not be null.</param>
		/// <exception cref="System.IO.IOException">
		/// writing is not supported, or attempting to write the file
		/// failed, possibly due to permissions or remote disk full, etc.
		/// </exception>
		internal virtual void WriteRef(string name, ObjectId value)
		{
			ByteArrayOutputStream b;
			b = new ByteArrayOutputStream(Constants.OBJECT_ID_STRING_LENGTH + 1);
			value.CopyTo(b);
			b.Write('\n');
			WriteFile(ROOT_DIR + name, b.ToByteArray());
		}

		/// <summary>
		/// Rebuild the
		/// <see cref="INFO_PACKS">INFO_PACKS</see>
		/// for dumb transport clients.
		/// <p>
		/// This method rebuilds the contents of the
		/// <see cref="INFO_PACKS">INFO_PACKS</see>
		/// file to
		/// match the passed list of pack names.
		/// </summary>
		/// <param name="packNames">
		/// names of available pack files, in the order they should appear
		/// in the file. Valid pack name strings are of the form
		/// <code>pack-035760ab452d6eebd123add421f253ce7682355a.pack</code>.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// writing is not supported, or attempting to write the file
		/// failed, possibly due to permissions or remote disk full, etc.
		/// </exception>
		internal virtual void WriteInfoPacks(IEnumerable<string> packNames)
		{
			StringBuilder w = new StringBuilder();
			foreach (string n in packNames)
			{
				w.Append("P ");
				w.Append(n);
				w.Append('\n');
			}
			WriteFile(INFO_PACKS, Constants.EncodeASCII(w.ToString()));
		}

		/// <summary>Open a buffered reader around a file.</summary>
		/// <remarks>
		/// Open a buffered reader around a file.
		/// <p>
		/// This is shorthand for calling
		/// <see cref="Open(string)">Open(string)</see>
		/// and then wrapping it
		/// in a reader suitable for line oriented files like the alternates list.
		/// </remarks>
		/// <returns>a stream to read from the file. Never null.</returns>
		/// <param name="path">
		/// location of the file to read, relative to this objects
		/// directory (e.g. <code>info/packs</code>).
		/// </param>
		/// <exception cref="System.IO.FileNotFoundException">the requested file does not exist at the given location.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">
		/// The connection is unable to read the remote's file, and the
		/// failure occurred prior to being able to determine if the file
		/// exists, or after it was determined to exist but before the
		/// stream could be created.
		/// </exception>
		internal virtual BufferedReader OpenReader(string path)
		{
			InputStream @is = Open(path).@in;
			return new BufferedReader(new InputStreamReader(@is, Constants.CHARSET));
		}

		/// <summary>Read a standard Git alternates file to discover other object databases.</summary>
		/// <remarks>
		/// Read a standard Git alternates file to discover other object databases.
		/// <p>
		/// This method is suitable for reading the standard formats of the
		/// alternates file, such as found in <code>objects/info/alternates</code>
		/// or <code>objects/info/http-alternates</code> within a Git repository.
		/// <p>
		/// Alternates appear one per line, with paths expressed relative to this
		/// object database.
		/// </remarks>
		/// <param name="listPath">
		/// location of the alternate file to read, relative to this
		/// object database (e.g. <code>info/alternates</code>).
		/// </param>
		/// <returns>
		/// the list of discovered alternates. Empty list if the file exists,
		/// but no entries were discovered.
		/// </returns>
		/// <exception cref="System.IO.FileNotFoundException">the requested file does not exist at the given location.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">
		/// The connection is unable to read the remote's file, and the
		/// failure occurred prior to being able to determine if the file
		/// exists, or after it was determined to exist but before the
		/// stream could be created.
		/// </exception>
		internal virtual ICollection<WalkRemoteObjectDatabase> ReadAlternates(string listPath
			)
		{
			BufferedReader br = OpenReader(listPath);
			try
			{
				ICollection<WalkRemoteObjectDatabase> alts = new AList<WalkRemoteObjectDatabase>(
					);
				for (; ; )
				{
					string line = br.ReadLine();
					if (line == null)
					{
						break;
					}
					if (!line.EndsWith("/"))
					{
						line += "/";
					}
					alts.AddItem(OpenAlternate(line));
				}
				return alts;
			}
			finally
			{
				br.Close();
			}
		}

		/// <summary>Read a standard Git packed-refs file to discover known references.</summary>
		/// <remarks>Read a standard Git packed-refs file to discover known references.</remarks>
		/// <param name="avail">
		/// return collection of references. Any existing entries will be
		/// replaced if they are found in the packed-refs file.
		/// </param>
		/// <exception cref="NGit.Errors.TransportException">an error occurred reading from the packed refs file.
		/// 	</exception>
		protected internal virtual void ReadPackedRefs(IDictionary<string, Ref> avail)
		{
			try
			{
				BufferedReader br = OpenReader(ROOT_DIR + Constants.PACKED_REFS);
				try
				{
					ReadPackedRefsImpl(avail, br);
				}
				finally
				{
					br.Close();
				}
			}
			catch (FileNotFoundException)
			{
			}
			catch (IOException e)
			{
				// Perhaps it wasn't worthwhile, or is just an older repository.
				throw new TransportException(GetURI(), JGitText.Get().errorInPackedRefs, e);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadPackedRefsImpl(IDictionary<string, Ref> avail, BufferedReader br
			)
		{
			Ref last = null;
			bool peeled = false;
			for (; ; )
			{
				string line = br.ReadLine();
				if (line == null)
				{
					break;
				}
				if (line[0] == '#')
				{
					if (line.StartsWith(RefDirectory.PACKED_REFS_HEADER))
					{
						line = Sharpen.Runtime.Substring(line, RefDirectory.PACKED_REFS_HEADER.Length);
						peeled = line.Contains(RefDirectory.PACKED_REFS_PEELED);
					}
					continue;
				}
				if (line[0] == '^')
				{
					if (last == null)
					{
						throw new TransportException(JGitText.Get().peeledLineBeforeRef);
					}
					ObjectId id = ObjectId.FromString(Sharpen.Runtime.Substring(line, 1));
					last = new ObjectIdRef.PeeledTag(RefStorage.PACKED, last.GetName(), last.GetObjectId
						(), id);
					avail.Put(last.GetName(), last);
					continue;
				}
				int sp = line.IndexOf(' ');
				if (sp < 0)
				{
					throw new TransportException(MessageFormat.Format(JGitText.Get().unrecognizedRef, 
						line));
				}
				ObjectId id_1 = ObjectId.FromString(Sharpen.Runtime.Substring(line, 0, sp));
				string name = Sharpen.Runtime.Substring(line, sp + 1);
				if (peeled)
				{
					last = new ObjectIdRef.PeeledNonTag(RefStorage.PACKED, name, id_1);
				}
				else
				{
					last = new ObjectIdRef.Unpeeled(RefStorage.PACKED, name, id_1);
				}
				avail.Put(last.GetName(), last);
			}
		}

		internal sealed class FileStream
		{
			internal readonly InputStream @in;

			internal readonly long length;

			/// <summary>Create a new stream of unknown length.</summary>
			/// <remarks>Create a new stream of unknown length.</remarks>
			/// <param name="i">
			/// stream containing the file data. This stream will be
			/// closed by the caller when reading is complete.
			/// </param>
			internal FileStream(InputStream i)
			{
				@in = i;
				length = -1;
			}

			/// <summary>Create a new stream of known length.</summary>
			/// <remarks>Create a new stream of known length.</remarks>
			/// <param name="i">
			/// stream containing the file data. This stream will be
			/// closed by the caller when reading is complete.
			/// </param>
			/// <param name="n">
			/// total number of bytes available for reading through
			/// <code>i</code>.
			/// </param>
			internal FileStream(InputStream i, long n)
			{
				@in = i;
				length = n;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal byte[] ToArray()
			{
				try
				{
					if (length >= 0)
					{
						byte[] r = new byte[(int)length];
						IOUtil.ReadFully(@in, r, 0, r.Length);
						return r;
					}
					ByteArrayOutputStream r_1 = new ByteArrayOutputStream();
					byte[] buf = new byte[2048];
					int n;
					while ((n = @in.Read(buf)) >= 0)
					{
						r_1.Write(buf, 0, n);
					}
					return r_1.ToByteArray();
				}
				finally
				{
					@in.Close();
				}
			}
		}
	}
}
