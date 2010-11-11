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

using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>Constants describing various file modes recognized by GIT.</summary>
	/// <remarks>
	/// Constants describing various file modes recognized by GIT.
	/// <p>
	/// GIT uses a subset of the available UNIX file permission bits. The
	/// <code>FileMode</code> class provides access to constants defining the modes
	/// actually used by GIT.
	/// </p>
	/// </remarks>
	public abstract class FileMode
	{
		/// <summary>Mask to apply to a file mode to obtain its type bits.</summary>
		/// <remarks>Mask to apply to a file mode to obtain its type bits.</remarks>
		/// <seealso cref="TYPE_TREE">TYPE_TREE</seealso>
		/// <seealso cref="TYPE_SYMLINK">TYPE_SYMLINK</seealso>
		/// <seealso cref="TYPE_FILE">TYPE_FILE</seealso>
		/// <seealso cref="TYPE_GITLINK">TYPE_GITLINK</seealso>
		/// <seealso cref="TYPE_MISSING">TYPE_MISSING</seealso>
		public const int TYPE_MASK = 0xf000;

		/// <summary>
		/// Bit pattern for
		/// <see cref="TYPE_MASK">TYPE_MASK</see>
		/// matching
		/// <see cref="TREE">TREE</see>
		/// .
		/// </summary>
		public const int TYPE_TREE = 0x4000;

		/// <summary>
		/// Bit pattern for
		/// <see cref="TYPE_MASK">TYPE_MASK</see>
		/// matching
		/// <see cref="SYMLINK">SYMLINK</see>
		/// .
		/// </summary>
		public const int TYPE_SYMLINK = 0xa000;

		/// <summary>
		/// Bit pattern for
		/// <see cref="TYPE_MASK">TYPE_MASK</see>
		/// matching
		/// <see cref="REGULAR_FILE">REGULAR_FILE</see>
		/// .
		/// </summary>
		public const int TYPE_FILE = 0x8000;

		/// <summary>
		/// Bit pattern for
		/// <see cref="TYPE_MASK">TYPE_MASK</see>
		/// matching
		/// <see cref="GITLINK">GITLINK</see>
		/// .
		/// </summary>
		public const int TYPE_GITLINK = 0xe000;

		/// <summary>
		/// Bit pattern for
		/// <see cref="TYPE_MASK">TYPE_MASK</see>
		/// matching
		/// <see cref="MISSING">MISSING</see>
		/// .
		/// </summary>
		public const int TYPE_MISSING = 0000000;

		private sealed class _FileMode_88 : NGit.FileMode
		{
			public _FileMode_88(int baseArg1, int baseArg2) : base(baseArg1, baseArg2)
			{
			}

			public override bool Equals(int modeBits)
			{
				return (modeBits & NGit.FileMode.TYPE_MASK) == NGit.FileMode.TYPE_TREE;
			}
		}

		/// <summary>Mode indicating an entry is a tree (aka directory).</summary>
		/// <remarks>Mode indicating an entry is a tree (aka directory).</remarks>
		public static readonly NGit.FileMode TREE = new _FileMode_88(TYPE_TREE, Constants
			.OBJ_TREE);

		private sealed class _FileMode_97 : NGit.FileMode
		{
			public _FileMode_97(int baseArg1, int baseArg2) : base(baseArg1, baseArg2)
			{
			}

			public override bool Equals(int modeBits)
			{
				return (modeBits & NGit.FileMode.TYPE_MASK) == NGit.FileMode.TYPE_SYMLINK;
			}
		}

		/// <summary>Mode indicating an entry is a symbolic link.</summary>
		/// <remarks>Mode indicating an entry is a symbolic link.</remarks>
		public static readonly NGit.FileMode SYMLINK = new _FileMode_97(TYPE_SYMLINK, Constants
			.OBJ_BLOB);

		private sealed class _FileMode_106 : NGit.FileMode
		{
			public _FileMode_106(int baseArg1, int baseArg2) : base(baseArg1, baseArg2)
			{
			}

			public override bool Equals(int modeBits)
			{
				return (modeBits & NGit.FileMode.TYPE_MASK) == NGit.FileMode.TYPE_FILE && (modeBits
					 & 0x49) == 0;
			}
		}

		/// <summary>Mode indicating an entry is a non-executable file.</summary>
		/// <remarks>Mode indicating an entry is a non-executable file.</remarks>
		public static readonly NGit.FileMode REGULAR_FILE = new _FileMode_106(0x81a4, Constants
			.OBJ_BLOB);

		private sealed class _FileMode_115 : NGit.FileMode
		{
			public _FileMode_115(int baseArg1, int baseArg2) : base(baseArg1, baseArg2)
			{
			}

			public override bool Equals(int modeBits)
			{
				return (modeBits & NGit.FileMode.TYPE_MASK) == NGit.FileMode.TYPE_FILE && (modeBits
					 & 0x49) != 0;
			}
		}

		/// <summary>Mode indicating an entry is an executable file.</summary>
		/// <remarks>Mode indicating an entry is an executable file.</remarks>
		public static readonly NGit.FileMode EXECUTABLE_FILE = new _FileMode_115(0x81ed, 
			Constants.OBJ_BLOB);

		private sealed class _FileMode_124 : NGit.FileMode
		{
			public _FileMode_124(int baseArg1, int baseArg2) : base(baseArg1, baseArg2)
			{
			}

			public override bool Equals(int modeBits)
			{
				return (modeBits & NGit.FileMode.TYPE_MASK) == NGit.FileMode.TYPE_GITLINK;
			}
		}

		/// <summary>Mode indicating an entry is a submodule commit in another repository.</summary>
		/// <remarks>Mode indicating an entry is a submodule commit in another repository.</remarks>
		public static readonly NGit.FileMode GITLINK = new _FileMode_124(TYPE_GITLINK, Constants
			.OBJ_COMMIT);

		private sealed class _FileMode_133 : NGit.FileMode
		{
			public _FileMode_133(int baseArg1, int baseArg2) : base(baseArg1, baseArg2)
			{
			}

			public override bool Equals(int modeBits)
			{
				return modeBits == 0;
			}
		}

		/// <summary>Mode indicating an entry is missing during parallel walks.</summary>
		/// <remarks>Mode indicating an entry is missing during parallel walks.</remarks>
		public static readonly NGit.FileMode MISSING = new _FileMode_133(TYPE_MISSING, Constants
			.OBJ_BAD);

		/// <summary>Convert a set of mode bits into a FileMode enumerated value.</summary>
		/// <remarks>Convert a set of mode bits into a FileMode enumerated value.</remarks>
		/// <param name="bits">the mode bits the caller has somehow obtained.</param>
		/// <returns>the FileMode instance that represents the given bits.</returns>
		public static NGit.FileMode FromBits(int bits)
		{
			switch (bits & TYPE_MASK)
			{
				case TYPE_MISSING:
				{
					if (bits == 0)
					{
						return MISSING;
					}
					break;
				}

				case TYPE_TREE:
				{
					return TREE;
				}

				case TYPE_FILE:
				{
					if ((bits & 0x49) != 0)
					{
						return EXECUTABLE_FILE;
					}
					return REGULAR_FILE;
				}

				case TYPE_SYMLINK:
				{
					return SYMLINK;
				}

				case TYPE_GITLINK:
				{
					return GITLINK;
				}
			}
			return new _FileMode_164(bits, bits, Constants.OBJ_BAD);
		}

		private sealed class _FileMode_164 : NGit.FileMode
		{
			public _FileMode_164(int bits, int baseArg1, int baseArg2) : base(baseArg1, baseArg2
				)
			{
				this.bits = bits;
			}

			public override bool Equals(int a)
			{
				return bits == a;
			}

			private readonly int bits;
		}

		private readonly byte[] octalBytes;

		private readonly int modeBits;

		private readonly int objectType;

		private FileMode(int mode, int expType)
		{
			modeBits = mode;
			objectType = expType;
			if (mode != 0)
			{
				byte[] tmp = new byte[10];
				int p = tmp.Length;
				while (mode != 0)
				{
					tmp[--p] = unchecked((byte)((byte)('0') + (mode & 0x7)));
					mode >>= 3;
				}
				octalBytes = new byte[tmp.Length - p];
				for (int k = 0; k < octalBytes.Length; k++)
				{
					octalBytes[k] = tmp[p + k];
				}
			}
			else
			{
				octalBytes = new byte[] { (byte)('0') };
			}
		}

		/// <summary>
		/// Test a file mode for equality with this
		/// <see cref="FileMode">FileMode</see>
		/// object.
		/// </summary>
		/// <param name="modebits"></param>
		/// <returns>true if the mode bits represent the same mode as this object</returns>
		public abstract bool Equals(int modebits);

		/// <summary>Copy this mode as a sequence of octal US-ASCII bytes.</summary>
		/// <remarks>
		/// Copy this mode as a sequence of octal US-ASCII bytes.
		/// <p>
		/// The mode is copied as a sequence of octal digits using the US-ASCII
		/// character encoding. The sequence does not use a leading '0' prefix to
		/// indicate octal notation. This method is suitable for generation of a mode
		/// string within a GIT tree object.
		/// </p>
		/// </remarks>
		/// <param name="os">stream to copy the mode to.</param>
		/// <exception cref="System.IO.IOException">the stream encountered an error during the copy.
		/// 	</exception>
		public virtual void CopyTo(OutputStream os)
		{
			os.Write(octalBytes);
		}

		/// <summary>Copy this mode as a sequence of octal US-ASCII bytes.</summary>
		/// <remarks>
		/// Copy this mode as a sequence of octal US-ASCII bytes.
		/// The mode is copied as a sequence of octal digits using the US-ASCII
		/// character encoding. The sequence does not use a leading '0' prefix to
		/// indicate octal notation. This method is suitable for generation of a mode
		/// string within a GIT tree object.
		/// </remarks>
		/// <param name="buf">buffer to copy the mode to.</param>
		/// <param name="ptr">
		/// position within
		/// <code>buf</code>
		/// for first digit.
		/// </param>
		public virtual void CopyTo(byte[] buf, int ptr)
		{
			System.Array.Copy(octalBytes, 0, buf, ptr, octalBytes.Length);
		}

		/// <returns>
		/// the number of bytes written by
		/// <see cref="CopyTo(Sharpen.OutputStream)">CopyTo(Sharpen.OutputStream)</see>
		/// .
		/// </returns>
		public virtual int CopyToLength()
		{
			return octalBytes.Length;
		}

		/// <summary>Get the object type that should appear for this type of mode.</summary>
		/// <remarks>
		/// Get the object type that should appear for this type of mode.
		/// <p>
		/// See the object type constants in
		/// <see cref="Constants">Constants</see>
		/// .
		/// </remarks>
		/// <returns>one of the well known object type constants.</returns>
		public virtual int GetObjectType()
		{
			return objectType;
		}

		/// <summary>Format this mode as an octal string (for debugging only).</summary>
		/// <remarks>Format this mode as an octal string (for debugging only).</remarks>
		public override string ToString()
		{
			return Sharpen.Extensions.ToOctalString(modeBits);
		}

		/// <returns>The mode bits as an integer.</returns>
		public virtual int GetBits()
		{
			return modeBits;
		}
	}
}
