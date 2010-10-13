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
using NGit.Revwalk;
using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>
	/// Extension of
	/// <see cref="NGit.ObjectReader">NGit.ObjectReader</see>
	/// that supports reusing objects in packs.
	/// <p>
	/// <code>ObjectReader</code>
	/// implementations may also optionally implement this
	/// interface to support
	/// <see cref="PackWriter">PackWriter</see>
	/// with a means of copying an object
	/// that is already in pack encoding format directly into the output stream,
	/// without incurring decompression and recompression overheads.
	/// </summary>
	public interface ObjectReuseAsIs
	{
		/// <summary>
		/// Allocate a new
		/// <code>PackWriter</code>
		/// state structure for an object.
		/// <p>
		/// <see cref="PackWriter">PackWriter</see>
		/// allocates these objects to keep track of the
		/// per-object state, and how to load the objects efficiently into the
		/// generated stream. Implementers may subclass this type with additional
		/// object state, such as to remember what file and offset contains the
		/// object's pack encoded data.
		/// </summary>
		/// <param name="obj">
		/// identity of the object that will be packed. The object's
		/// parsed status is undefined here. Implementers must not rely on
		/// the object being parsed.
		/// </param>
		/// <returns>a new instance for this object.</returns>
		ObjectToPack NewObjectToPack(RevObject obj);

		/// <summary>Select the best object representation for a packer.</summary>
		/// <remarks>
		/// Select the best object representation for a packer.
		/// Implementations should iterate through all available representations of
		/// an object, and pass them in turn to the PackWriter though
		/// <see cref="PackWriter.Select(ObjectToPack, StoredObjectRepresentation)">PackWriter.Select(ObjectToPack, StoredObjectRepresentation)
		/// 	</see>
		/// so
		/// the writer can select the most suitable representation to reuse into the
		/// output stream.
		/// The implementation may choose to consider multiple objects at once on
		/// concurrent threads, but must evaluate all representations of an object
		/// within the same thread.
		/// </remarks>
		/// <param name="packer">the packer that will write the object in the near future.</param>
		/// <param name="monitor">
		/// progress monitor, implementation should update the monitor
		/// once for each item in the iteration when selection is done.
		/// </param>
		/// <param name="objects">the objects that are being packed.</param>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// there is no representation available for the object, as it is
		/// no longer in the repository. Packing will abort.
		/// </exception>
		/// <exception cref="System.IO.IOException">the repository cannot be accessed. Packing will abort.
		/// 	</exception>
		void SelectObjectRepresentation(PackWriter packer, ProgressMonitor monitor, Iterable
			<ObjectToPack> objects);

		/// <summary>Write objects to the pack stream in roughly the order given.</summary>
		/// <remarks>
		/// Write objects to the pack stream in roughly the order given.
		/// <code>PackWriter</code>
		/// invokes this method to write out one or more objects,
		/// in approximately the order specified by the iteration over the list. A
		/// simple implementation of this method would just iterate the list and
		/// output each object:
		/// <pre>
		/// for (ObjectToPack obj : list)
		/// out.writeObject(obj)
		/// </pre>
		/// However more sophisticated implementors may try to perform some (small)
		/// reordering to access objects that are stored close to each other at
		/// roughly the same time. Implementations may choose to write objects out of
		/// order, but this may increase pack file size due to using a larger header
		/// format to reach a delta base that is later in the stream. It may also
		/// reduce data locality for the reader, slowing down data access.
		/// Invoking
		/// <see cref="PackOutputStream.WriteObject(ObjectToPack)">PackOutputStream.WriteObject(ObjectToPack)
		/// 	</see>
		/// will cause
		/// <see cref="CopyObjectAsIs(PackOutputStream, ObjectToPack)">CopyObjectAsIs(PackOutputStream, ObjectToPack)
		/// 	</see>
		/// to be invoked
		/// recursively on
		/// <code>this</code>
		/// if the current object is scheduled for reuse.
		/// </remarks>
		/// <param name="out">the stream to write each object to.</param>
		/// <param name="list">
		/// the list of objects to write. Objects should be written in
		/// approximately this order.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// the stream cannot be written to, or one or more required
		/// objects cannot be accessed from the object database.
		/// </exception>
		void WriteObjects(PackOutputStream @out, Iterable<ObjectToPack> list);

		/// <summary>Output a previously selected representation.</summary>
		/// <remarks>
		/// Output a previously selected representation.
		/// <p>
		/// <code>PackWriter</code>
		/// invokes this method only if a representation
		/// previously given to it by
		/// <code>selectObjectRepresentation</code>
		/// was chosen
		/// for reuse into the output stream. The
		/// <code>otp</code>
		/// argument is an instance
		/// created by this reader's own
		/// <code>newObjectToPack</code>
		/// , and the
		/// representation data saved within it also originated from this reader.
		/// <p>
		/// Implementors must write the object header before copying the raw data to
		/// the output stream. The typical implementation is like:
		/// <pre>
		/// MyToPack mtp = (MyToPack) otp;
		/// byte[] raw = validate(mtp); // throw SORNAE here, if at all
		/// out.writeHeader(mtp, mtp.inflatedSize);
		/// out.write(raw);
		/// </pre>
		/// </remarks>
		/// <param name="out">stream the object should be written to.</param>
		/// <param name="otp">the object's saved representation information.</param>
		/// <exception cref="NGit.Errors.StoredObjectRepresentationNotAvailableException">
		/// the previously selected representation is no longer
		/// available. If thrown before
		/// <code>out.writeHeader</code>
		/// the pack
		/// writer will try to find another representation, and write
		/// that one instead. If throw after
		/// <code>out.writeHeader</code>
		/// ,
		/// packing will abort.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// the stream's write method threw an exception. Packing will
		/// abort.
		/// </exception>
		void CopyObjectAsIs(PackOutputStream @out, ObjectToPack otp);
	}
}
