﻿//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell.Resources;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// An ennumerable list of ShellObjects
    /// </summary>
    public class ShellObjectCollection : IEnumerable, IDisposable, IList<ShellObject>
    {
        private List<ShellObject> content = new List<ShellObject>();

        bool readOnly;
        bool isDisposed;

        #region construction/disposal/finialization
        /// <summary>
        /// Creates a ShellObject collection from an IShellItemArray
        /// </summary>
        /// <param name="iArray">IShellItemArray pointer</param>
        /// <param name="readOnly">Indicates whether the collection shouldbe read-only or not</param>
        internal ShellObjectCollection(IShellItemArray iArray, bool readOnly)
        {
            this.readOnly = readOnly;

            if (iArray != null)
            {
                try
                {
                    uint itemCount = 0;
                    iArray.GetCount(out itemCount);
                    content.Capacity = (int)itemCount;
                    for (uint index = 0; index < itemCount; index++)
                    {
                        IShellItem iShellItem = null;
                        iArray.GetItemAt(index, out iShellItem);
                        content.Add(ShellObjectFactory.Create(iShellItem));
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(iArray);
                }
            }
        }

        /// <summary>
        /// Creates a ShellObjectCollection from an IDataObject passed during Drop operation.
        /// </summary>
        /// <param name="dataObject">An object that implements the IDataObject COM interface.</param>
        /// <returns>ShellObjectCollection created from the given IDataObject</returns>
        public static ShellObjectCollection FromDataObject(System.Runtime.InteropServices.ComTypes.IDataObject dataObject)
        {
            IShellItemArray shellItemArray;
            Guid iid = new Guid(ShellIIDGuid.IShellItemArray);
            ShellNativeMethods.SHCreateShellItemArrayFromDataObject(dataObject, ref iid, out shellItemArray);
            return new ShellObjectCollection(shellItemArray, true);
        }

        /// <summary>
        /// Constructs an empty ShellObjectCollection
        /// </summary>
        public ShellObjectCollection()
        {
            // Left empty
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~ShellObjectCollection()
        {
            Dispose(false);
        }

        /// <summary>
        /// Standard Dispose pattern
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Standard Dispose patterns 
        /// </summary>
        /// <param name="disposing">Indicates that this is being called from Dispose(), rather than the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed == false)
            {
                if (disposing)
                {
                    foreach (ShellObject shellObject in content)
                    {
                        shellObject.Dispose();
                    }

                    content.Clear();
                }

                isDisposed = true;
            }
        }
        #endregion

        #region implementation

        /// <summary>
        /// Item count
        /// </summary>
        public int Count { get { return content.Count; } }

        /// <summary>
        /// Collection enumeration
        /// </summary>
        /// <returns></returns>
        public System.Collections.IEnumerator GetEnumerator()
        {
            foreach (ShellObject obj in content)
            {
                yield return obj;
            }
        }

        /// <summary>
        /// Builds the data for the CFSTR_SHELLIDLIST Drag and Clipboard data format from the 
        /// ShellObjects in the collection.
        /// </summary>
        /// <returns>A memory stream containing the drag/drop data.</returns>
        public MemoryStream BuildShellIDList()
        {
            if (content.Count == 0)
            {
                throw new InvalidOperationException(LocalizedMessages.ShellObjectCollectionEmptyCollection);
            }


            MemoryStream mstream = new MemoryStream();
            try
            {
                BinaryWriter bwriter = new BinaryWriter(mstream);


                // number of IDLs to be written (shell objects + parent folder)
                uint itemCount = (uint)(content.Count + 1);

                // grab the object IDLs            
                IntPtr[] idls = new IntPtr[itemCount];

                for (int index = 0; index < itemCount; index++)
                {
                    if (index == 0)
                    {
                        // Because the ShellObjects passed in may be from anywhere, the 
                        // parent folder reference must be the desktop.
                        idls[index] = ((ShellObject)KnownFolders.Desktop).PIDL;
                    }
                    else
                    {
                        idls[index] = content[index - 1].PIDL;
                    }
                }

                // calculate offset array (folder IDL + item IDLs)
                uint[] offsets = new uint[itemCount + 1];
                for (int index = 0; index < itemCount; index++)
                {
                    if (index == 0)
                    {
                        // first offset equals size of CIDA header data
                        offsets[0] = (uint)(sizeof(uint) * (offsets.Length + 1));
                    }
                    else
                    {
                        offsets[index] = offsets[index - 1] + ShellNativeMethods.ILGetSize(idls[index - 1]);
                    }
                }

                // Fill out the CIDA header
                //
                //    typedef struct _IDA {
                //    UINT cidl;          // number of relative IDList
                //    UINT aoffset[1];    // [0]: folder IDList, [1]-[cidl]: item IDList
                //    } CIDA, * LPIDA;
                //
                bwriter.Write(content.Count);
                foreach (uint offset in offsets)
                {
                    bwriter.Write(offset);
                }

                // copy idls
                foreach (IntPtr idl in idls)
                {
                    byte[] data = new byte[ShellNativeMethods.ILGetSize(idl)];
                    Marshal.Copy(idl, data, 0, data.Length);
                    bwriter.Write(data, 0, data.Length);
                }
            }
            catch
            {
                mstream.Dispose();
                throw;
            }
            // return CIDA stream 
            return mstream;
        }
        #endregion

        #region IList<ShellObject> Members

        /// <summary>
        /// Returns the index of a particualr shell object in the collection
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns>The index of the item found, or -1 if not found.</returns>
        public int IndexOf(ShellObject item)
        {
            return content.IndexOf(item);
        }

        /// <summary>
        /// Inserts a new shell object into the collection.
        /// </summary>
        /// <param name="index">The index at which to insert.</param>
        /// <param name="item">The item to insert.</param>
        public void Insert(int index, ShellObject item)
        {
            if (readOnly)
            {
                throw new InvalidOperationException(LocalizedMessages.ShellObjectCollectionInsertReadOnly);
            }

            content.Insert(index, item);
        }

        /// <summary>
        /// Removes the specified ShellObject from the collection
        /// </summary>
        /// <param name="index">The index to remove at.</param>
        public void RemoveAt(int index)
        {
            if (readOnly)
            {
                throw new InvalidOperationException(LocalizedMessages.ShellObjectCollectionRemoveReadOnly);
            }

            content.RemoveAt(index);
        }

        /// <summary>
        /// The collection indexer
        /// </summary>
        /// <param name="index">The index of the item to retrieve.</param>
        /// <returns>The ShellObject at the specified index</returns>
        public ShellObject this[int index]
        {
            get
            {
                return content[index];
            }
            set
            {
                if (readOnly)
                {
                    throw new InvalidOperationException(LocalizedMessages.ShellObjectCollectionInsertReadOnly);
                }

                content[index] = value;
            }
        }

        #endregion

        #region ICollection<ShellObject> Members

        /// <summary>
        /// Adds a ShellObject to the collection,
        /// </summary>
        /// <param name="item">The ShellObject to add.</param>
        public void Add(ShellObject item)
        {
            if (readOnly)
            {
                throw new InvalidOperationException(LocalizedMessages.ShellObjectCollectionInsertReadOnly);
            }

            content.Add(item);
        }

        /// <summary>
        /// Clears the collection of ShellObjects.
        /// </summary>
        public void Clear()
        {
            if (readOnly)
            {
                throw new InvalidOperationException(LocalizedMessages.ShellObjectCollectionRemoveReadOnly);
            }

            content.Clear();
        }

        /// <summary>
        /// Determines if the collection contains a particular ShellObject.
        /// </summary>
        /// <param name="item">The ShellObject.</param>
        /// <returns>true, if the ShellObject is in the list, false otherwise.</returns>
        public bool Contains(ShellObject item)
        {
            return content.Contains(item);
        }

        /// <summary>
        /// Copies the ShellObjects in the collection to a ShellObject array.
        /// </summary>
        /// <param name="array">The destination to copy to.</param>
        /// <param name="arrayIndex">The index into the array at which copying will commence.</param>
        public void CopyTo(ShellObject[] array, int arrayIndex)
        {
            if (array == null) { throw new ArgumentNullException("array"); }
            if (array.Length < arrayIndex + content.Count)
            {
                throw new ArgumentException(LocalizedMessages.ShellObjectCollectionArrayTooSmall, "array");
            }

            for (int index = 0; index < content.Count; index++)
            {
                array[index + arrayIndex] = content[index];
            }
        }

        /// <summary>
        /// Retrieves the number of ShellObjects in the collection
        /// </summary>
        int ICollection<ShellObject>.Count
        {
            get
            {
                return content.Count;
            }
        }

        /// <summary>
        /// If true, the contents of the collection are immutable.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return readOnly;
            }
        }

        /// <summary>
        /// Removes a particular ShellObject from the list.
        /// </summary>
        /// <param name="item">The ShellObject to remove.</param>
        /// <returns>True if the item could be removed, false otherwise.</returns>
        public bool Remove(ShellObject item)
        {
            if (readOnly)
            {
                throw new InvalidOperationException(LocalizedMessages.ShellObjectCollectionRemoveReadOnly);
            }

            return content.Remove(item);
        }

        #endregion

        #region IEnumerable<ShellObject> Members

        /// <summary>
        /// Allows for enumeration through the list of ShellObjects in the collection.
        /// </summary>
        /// <returns>The IEnumerator interface to use for enumeration.</returns>
        IEnumerator<ShellObject> IEnumerable<ShellObject>.GetEnumerator()
        {
            foreach (ShellObject obj in content)
            {
                yield return obj;
            }
        }

        #endregion
    }
}
