﻿//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.WindowsAPICodePack.Shell.Resources;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// The FolderTypes values represent a view template applied to a folder, 
    /// usually based on its intended use and contents.
    /// </summary>
    internal static class FolderTypes
    {
        /// <summary>
        /// No particular content type has been detected or specified. This value is not supported in Windows 7 and later systems.
        /// </summary>
        internal static Guid NotSpecified = new Guid(
            0x5c4f28b5, 0xf869, 0x4e84, 0x8e, 0x60, 0xf1, 0x1d, 0xb9, 0x7c, 0x5c, 0xc7);

        /// <summary>
        /// The folder is invalid. There are several things that can cause this judgement: hard disk errors, file system errors, and compression errors among them.
        /// </summary>
        internal static Guid Invalid = new Guid(
            0x57807898, 0x8c4f, 0x4462, 0xbb, 0x63, 0x71, 0x04, 0x23, 0x80, 0xb1, 0x09);

        /// <summary>
        /// The folder contains document files. These can be of mixed format.doc, .txt, and others.
        /// </summary>
        internal static Guid Documents = new Guid(
            0x7d49d726, 0x3c21, 0x4f05, 0x99, 0xaa, 0xfd, 0xc2, 0xc9, 0x47, 0x46, 0x56);

        /// <summary>
        /// Image files, such as .jpg, .tif, or .png files.
        /// </summary>
        internal static Guid Pictures = new Guid(
            0xb3690e58, 0xe961, 0x423b, 0xb6, 0x87, 0x38, 0x6e, 0xbf, 0xd8, 0x32, 0x39);

        /// <summary>
        /// Windows 7 and later. The folder contains audio files, such as .mp3 and .wma files.
        /// </summary>
        internal static Guid Music = new Guid(
            0xaf9c03d6, 0x7db9, 0x4a15, 0x94, 0x64, 0x13, 0xbf, 0x9f, 0xb6, 0x9a, 0x2a);

        /// <summary>
        /// A list of music files displayed in Icons view. This value is not supported in Windows 7 and later systems.
        /// </summary>
        internal static Guid MusicIcons = new Guid(
            0x0b7467fb, 0x84ba, 0x4aae, 0xa0, 0x9b, 0x15, 0xb7, 0x10, 0x97, 0xaf, 0x9e);

        /// <summary>
        /// The folder is the Games folder found in the Start menu.
        /// </summary>
        internal static Guid Games = new Guid(
            0xb689b0d0, 0x76d3, 0x4cbb, 0x87, 0xf7, 0x58, 0x5d, 0x0e, 0x0c, 0xe0, 0x70);

        /// <summary>
        /// The Control Panel in category view. This is a virtual folder.
        /// </summary>
        internal static Guid ControlPanelCategory = new Guid(
            0xde4f0660, 0xfa10, 0x4b8f, 0xa4, 0x94, 0x06, 0x8b, 0x20, 0xb2, 0x23, 0x07);

        /// <summary>
        /// The Control Panel in classic view. This is a virtual folder.
        /// </summary>
        internal static Guid ControlPanelClassic = new Guid(
            0x0c3794f3, 0xb545, 0x43aa, 0xa3, 0x29, 0xc3, 0x74, 0x30, 0xc5, 0x8d, 0x2a);

        /// <summary>
        /// Printers that have been added to the system. This is a virtual folder.
        /// </summary>
        internal static Guid Printers = new Guid(
            0x2c7bbec6, 0xc844, 0x4a0a, 0x91, 0xfa, 0xce, 0xf6, 0xf5, 0x9c, 0xfd, 0xa1);

        /// <summary>
        /// The Recycle Bin. This is a virtual folder.
        /// </summary>
        internal static Guid RecycleBin = new Guid(
            0xd6d9e004, 0xcd87, 0x442b, 0x9d, 0x57, 0x5e, 0x0a, 0xeb, 0x4f, 0x6f, 0x72);

        /// <summary>
        /// The software explorer window used by the Add or Remove Programs control panel icon.
        /// </summary>
        internal static Guid SoftwareExplorer = new Guid(
            0xd674391b, 0x52d9, 0x4e07, 0x83, 0x4e, 0x67, 0xc9, 0x86, 0x10, 0xf3, 0x9d);

        /// <summary>
        /// The folder is a compressed archive, such as a compressed file with a .zip file name extension.
        /// </summary>
        internal static Guid CompressedFolder = new Guid(
             0x80213e82, 0xbcfd, 0x4c4f, 0x88, 0x17, 0xbb, 0x27, 0x60, 0x12, 0x67, 0xa9);

        /// <summary>
        /// An e-mail-related folder that contains contact information.
        /// </summary>
        internal static Guid Contacts = new Guid(
             0xde2b70ec, 0x9bf7, 0x4a93, 0xbd, 0x3d, 0x24, 0x3f, 0x78, 0x81, 0xd4, 0x92);

        /// <summary>
        /// A default library view without a more specific template. This value is not supported in Windows 7 and later systems.
        /// </summary>
        internal static Guid Library = new Guid(
             0x4badfc68, 0xc4ac, 0x4716, 0xa0, 0xa0, 0x4d, 0x5d, 0xaa, 0x6b, 0x0f, 0x3e);

        /// <summary>
        /// The Network Explorer folder.
        /// </summary>
        internal static Guid NetworkExplorer = new Guid(
             0x25cc242b, 0x9a7c, 0x4f51, 0x80, 0xe0, 0x7a, 0x29, 0x28, 0xfe, 0xbe, 0x42);

        /// <summary>
        /// The folder is the FOLDERID_UsersFiles folder.
        /// </summary>
        internal static Guid UserFiles = new Guid(
             0xcd0fc69b, 0x71e2, 0x46e5, 0x96, 0x90, 0x5b, 0xcd, 0x9f, 0x57, 0xaa, 0xb3);

        /// <summary>
        /// Windows 7 and later. The folder contains search results, but they are of mixed or no specific type.
        /// </summary>
        internal static Guid GenericSearchResults = new Guid(
            0x7fde1a1e, 0x8b31, 0x49a5, 0x93, 0xb8, 0x6b, 0xe1, 0x4c, 0xfa, 0x49, 0x43);

        /// <summary>
        /// Windows 7 and later. The folder is a library, but of no specified type.
        /// </summary>
        internal static Guid GenericLibrary = new Guid(
            0x5f4eab9a, 0x6833, 0x4f61, 0x89, 0x9d, 0x31, 0xcf, 0x46, 0x97, 0x9d, 0x49);

        /// <summary>
        /// Windows 7 and later. The folder contains video files. These can be of mixed format.wmv, .mov, and others.
        /// </summary>
        internal static Guid Videos = new Guid(
            0x5fa96407, 0x7e77, 0x483c, 0xac, 0x93, 0x69, 0x1d, 0x05, 0x85, 0x0d, 0xe8);

        /// <summary>
        /// Windows 7 and later. The view shown when the user clicks the Windows Explorer button on the taskbar.
        /// </summary>
        internal static Guid UsersLibraries = new Guid(
            0xc4d98f09, 0x6124, 0x4fe0, 0x99, 0x42, 0x82, 0x64, 0x16, 0x8, 0x2d, 0xa9);

        /// <summary>
        /// Windows 7 and later. The homegroup view.
        /// </summary>
        internal static Guid OtherUsers = new Guid(
            0xb337fd00, 0x9dd5, 0x4635, 0xa6, 0xd4, 0xda, 0x33, 0xfd, 0x10, 0x2b, 0x7a);

        /// <summary>
        /// Windows 7 and later. A folder that contains communication-related files such as e-mails, calendar information, and contact information.
        /// </summary>
        internal static Guid Communications = new Guid(
            0x91475fe5, 0x586b, 0x4eba, 0x8d, 0x75, 0xd1, 0x74, 0x34, 0xb8, 0xcd, 0xf6);

        /// <summary>
        /// Windows 7 and later. The folder contains recorded television broadcasts.
        /// </summary>
        internal static Guid RecordedTV = new Guid(
            0x5557a28f, 0x5da6, 0x4f83, 0x88, 0x09, 0xc2, 0xc9, 0x8a, 0x11, 0xa6, 0xfa);

        /// <summary>
        /// Windows 7 and later. The folder contains saved game states.
        /// </summary>
        internal static Guid SavedGames = new Guid(
            0xd0363307, 0x28cb, 0x4106, 0x9f, 0x23, 0x29, 0x56, 0xe3, 0xe5, 0xe0, 0xe7);

        /// <summary>
        /// Windows 7 and later. The folder contains federated search OpenSearch results.
        /// </summary>
        internal static Guid OpenSearch = new Guid(
            0x8faf9629, 0x1980, 0x46ff, 0x80, 0x23, 0x9d, 0xce, 0xab, 0x9c, 0x3e, 0xe3);

        /// <summary>
        /// Windows 7 and later. Before you search.
        /// </summary>
        internal static Guid SearchConnector = new Guid(
            0x982725ee, 0x6f47, 0x479e, 0xb4, 0x47, 0x81, 0x2b, 0xfa, 0x7d, 0x2e, 0x8f);

        /// <summary>
        /// Windows 7 and later. A user's Searches folder, normally found at C:\Users\username\Searches.
        /// </summary>
        internal static Guid Searches = new Guid(
            0x0b0ba2e3, 0x405f, 0x415e, 0xa6, 0xee, 0xca, 0xd6, 0x25, 0x20, 0x78, 0x53);


        static Dictionary<Guid, string> types;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static FolderTypes()
        {
            types = new Dictionary<Guid, string>();
            // Review: These Localized messages could probably be a reflected value of the field's name.
            types.Add(NotSpecified, LocalizedMessages.FolderTypeNotSpecified);
            types.Add(Invalid, LocalizedMessages.FolderTypeInvalid);
            types.Add(Communications, LocalizedMessages.FolderTypeCommunications);
            types.Add(CompressedFolder, LocalizedMessages.FolderTypeCompressedFolder);
            types.Add(Contacts, LocalizedMessages.FolderTypeContacts);
            types.Add(ControlPanelCategory, LocalizedMessages.FolderTypeCategory);
            types.Add(ControlPanelClassic, LocalizedMessages.FolderTypeClassic);
            types.Add(Documents, LocalizedMessages.FolderTypeDocuments);
            types.Add(Games, LocalizedMessages.FolderTypeGames);
            types.Add(GenericSearchResults, LocalizedMessages.FolderTypeSearchResults);
            types.Add(GenericLibrary, LocalizedMessages.FolderTypeGenericLibrary);
            types.Add(Library, LocalizedMessages.FolderTypeLibrary);
            types.Add(Music, LocalizedMessages.FolderTypeMusic);
            types.Add(MusicIcons, LocalizedMessages.FolderTypeMusicIcons);
            types.Add(NetworkExplorer, LocalizedMessages.FolderTypeNetworkExplorer);
            types.Add(OtherUsers, LocalizedMessages.FolderTypeOtherUsers);
            types.Add(OpenSearch, LocalizedMessages.FolderTypeOpenSearch);
            types.Add(Pictures, LocalizedMessages.FolderTypePictures);
            types.Add(Printers, LocalizedMessages.FolderTypePrinters);
            types.Add(RecycleBin, LocalizedMessages.FolderTypeRecycleBin);
            types.Add(RecordedTV, LocalizedMessages.FolderTypeRecordedTV);
            types.Add(SoftwareExplorer, LocalizedMessages.FolderTypeSoftwareExplorer);
            types.Add(SavedGames, LocalizedMessages.FolderTypeSavedGames);
            types.Add(SearchConnector, LocalizedMessages.FolderTypeSearchConnector);
            types.Add(Searches, LocalizedMessages.FolderTypeSearches);
            types.Add(UsersLibraries, LocalizedMessages.FolderTypeUserLibraries);
            types.Add(UserFiles, LocalizedMessages.FolderTypeUserFiles);
            types.Add(Videos, LocalizedMessages.FolderTypeVideos);
        }

        internal static string GetFolderType(Guid typeId)
        {
            string type;
            return types.TryGetValue(typeId, out type) ? type : string.Empty;
        }
    }
}
