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

using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>Caches when a file was last read, making it possible to detect future edits.
	/// 	</summary>
	/// <remarks>
	/// Caches when a file was last read, making it possible to detect future edits.
	/// <p>
	/// This object tracks the last modified time of a file. Later during an
	/// invocation of
	/// <see cref="IsModified(Sharpen.FilePath)">IsModified(Sharpen.FilePath)</see>
	/// the object will return true if the
	/// file may have been modified and should be re-read from disk.
	/// <p>
	/// A snapshot does not "live update" when the underlying filesystem changes.
	/// Callers must poll for updates by periodically invoking
	/// <see cref="IsModified(Sharpen.FilePath)">IsModified(Sharpen.FilePath)</see>
	/// .
	/// <p>
	/// To work around the "racy git" problem (where a file may be modified multiple
	/// times within the granularity of the filesystem modification clock) this class
	/// may return true from isModified(File) if the last modification time of the
	/// file is less than 3 seconds ago.
	/// </remarks>
	public class FileSnapshot
	{
		/// <summary>A FileSnapshot that is considered to always be modified.</summary>
		/// <remarks>
		/// A FileSnapshot that is considered to always be modified.
		/// <p>
		/// This instance is useful for application code that wants to lazily read a
		/// file, but only after
		/// <see cref="IsModified(Sharpen.FilePath)">IsModified(Sharpen.FilePath)</see>
		/// gets invoked. The returned
		/// snapshot contains only invalid status information.
		/// </remarks>
		public static readonly NGit.Storage.File.FileSnapshot DIRTY = new NGit.Storage.File.FileSnapshot
			(-1, -1);

		/// <summary>Record a snapshot for a specific file path.</summary>
		/// <remarks>
		/// Record a snapshot for a specific file path.
		/// <p>
		/// This method should be invoked before the file is accessed.
		/// </remarks>
		/// <param name="path">
		/// the path to later remember. The path's current status
		/// information is saved.
		/// </param>
		/// <returns>the snapshot.</returns>
		public static NGit.Storage.File.FileSnapshot Save(FilePath path)
		{
			long read = Runtime.CurrentTimeMillis();
			long modified = path.LastModified();
			return new NGit.Storage.File.FileSnapshot(read, modified);
		}

		/// <summary>Last observed modification time of the path.</summary>
		/// <remarks>Last observed modification time of the path.</remarks>
		private readonly long lastModified;

		/// <summary>Last wall-clock time the path was read.</summary>
		/// <remarks>Last wall-clock time the path was read.</remarks>
		private long lastRead;

		/// <summary>
		/// True once
		/// <see cref="lastRead">lastRead</see>
		/// is far later than
		/// <see cref="lastModified">lastModified</see>
		/// .
		/// </summary>
		private bool cannotBeRacilyClean;

		private FileSnapshot(long read, long modified)
		{
			this.lastRead = read;
			this.lastModified = modified;
			this.cannotBeRacilyClean = NotRacyClean(read);
		}

		internal virtual long LastModified()
		{
			return lastModified;
		}

		/// <summary>Check if the path may have been modified since the snapshot was saved.</summary>
		/// <remarks>Check if the path may have been modified since the snapshot was saved.</remarks>
		/// <param name="path">the path the snapshot describes.</param>
		/// <returns>true if the path needs to be read again.</returns>
		public virtual bool IsModified(FilePath path)
		{
			return IsModified(path.LastModified());
		}

		/// <summary>Update this snapshot when the content hasn't changed.</summary>
		/// <remarks>
		/// Update this snapshot when the content hasn't changed.
		/// <p>
		/// If the caller gets true from
		/// <see cref="IsModified(Sharpen.FilePath)">IsModified(Sharpen.FilePath)</see>
		/// , re-reads the
		/// content, discovers the content is identical, and
		/// <see cref="Equals(FileSnapshot)">Equals(FileSnapshot)</see>
		/// is true, it can use
		/// <see cref="SetClean(FileSnapshot)">SetClean(FileSnapshot)</see>
		/// to make a future
		/// <see cref="IsModified(Sharpen.FilePath)">IsModified(Sharpen.FilePath)</see>
		/// return false. The logic goes something like
		/// this:
		/// <pre>
		/// if (snapshot.isModified(path)) {
		/// FileSnapshot other = FileSnapshot.save(path);
		/// Content newContent = ...;
		/// if (oldContent.equals(newContent) &amp;&amp; snapshot.equals(other))
		/// snapshot.setClean(other);
		/// }
		/// </pre>
		/// </remarks>
		/// <param name="other">the other snapshot.</param>
		public virtual void SetClean(NGit.Storage.File.FileSnapshot other)
		{
			long now = other.lastRead;
			if (NotRacyClean(now))
			{
				cannotBeRacilyClean = true;
			}
			lastRead = now;
		}

		/// <summary>Compare two snapshots to see if they cache the same information.</summary>
		/// <remarks>Compare two snapshots to see if they cache the same information.</remarks>
		/// <param name="other">the other snapshot.</param>
		/// <returns>true if the two snapshots share the same information.</returns>
		public virtual bool Equals(NGit.Storage.File.FileSnapshot other)
		{
			return lastModified == other.lastModified;
		}

		public override bool Equals(object other)
		{
			if (other is NGit.Storage.File.FileSnapshot)
			{
				return Equals((NGit.Storage.File.FileSnapshot)other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			// This is pretty pointless, but override hashCode to ensure that
			// x.hashCode() == y.hashCode() when x.equals(y) is true.
			//
			return (int)lastModified;
		}

		private bool NotRacyClean(long read)
		{
			// The last modified time granularity of FAT filesystems is 2 seconds.
			// Using 2.5 seconds here provides a reasonably high assurance that
			// a modification was not missed.
			//
			return read - lastModified > 2500;
		}

		private bool IsModified(long currLastModified)
		{
			// Any difference indicates the path was modified.
			//
			if (lastModified != currLastModified)
			{
				return true;
			}
			// We have already determined the last read was far enough
			// after the last modification that any new modifications
			// are certain to change the last modified time.
			//
			if (cannotBeRacilyClean)
			{
				return false;
			}
			if (NotRacyClean(lastRead))
			{
				// Our last read should have marked cannotBeRacilyClean,
				// but this thread may not have seen the change. The read
				// of the volatile field lastRead should have fixed that.
				//
				return false;
			}
			// Our lastRead flag may be old, refresh and retry
			lastRead = Runtime.CurrentTimeMillis();
			if (NotRacyClean(lastRead))
			{
				return false;
			}
			// We last read this path too close to its last observed
			// modification time. We may have missed a modification.
			// Scan again, to ensure we still see the same state.
			//
			return true;
		}
	}
}
