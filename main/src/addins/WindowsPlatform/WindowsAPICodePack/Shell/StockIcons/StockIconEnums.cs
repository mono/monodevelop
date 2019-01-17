//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Shell
{

    /// <summary>
    /// Specifies options for the size of the stock icon.
    /// </summary>
    public enum StockIconSize
    {
        /// <summary>
        /// Retrieve the small version of the icon, as specified by SM_CXSMICON and SM_CYSMICON system metrics.
        /// </summary>
        Small,

        /// <summary>
        /// Retrieve the large version of the icon, as specified by SM_CXICON and SM_CYICON system metrics.
        /// </summary>
        Large,

        /// <summary>
        /// Retrieve the shell-sized icons (instead of the size specified by the system metrics).
        /// </summary>
        ShellSize,
    }

    /// <summary>
    /// Provides values used to specify which standard icon to retrieve. 
    /// </summary>
    public enum StockIconIdentifier
    {
        /// <summary>
        /// Icon for a document (blank page), no associated program.
        /// </summary>
        DocumentNotAssociated = 0,
        /// <summary>
        /// Icon for a document with an associated program.
        /// </summary>
        DocumentAssociated = 1,
        /// <summary>
        ///  Icon for a generic application with no custom icon.
        /// </summary>
        Application = 2,
        /// <summary>
        ///  Icon for a closed folder.
        /// </summary>
        Folder = 3,
        /// <summary>
        /// Icon for an open folder. 
        /// </summary>
        FolderOpen = 4,
        /// <summary>
        /// Icon for a 5.25" floppy disk drive.
        /// </summary>
        Drive525 = 5,
        /// <summary>
        ///  Icon for a 3.5" floppy disk drive. 
        /// </summary>
        Drive35 = 6,
        /// <summary>
        ///  Icon for a removable drive.
        /// </summary>
        DriveRemove = 7,
        /// <summary>
        ///  Icon for a fixed (hard disk) drive.
        /// </summary>
        DriveFixed = 8,
        /// <summary>
        ///  Icon for a network drive.
        /// </summary>
        DriveNetwork = 9,
        /// <summary>
        ///  Icon for a disconnected network drive.
        /// </summary>
        DriveNetworkDisabled = 10,
        /// <summary>
        ///  Icon for a CD drive.
        /// </summary>
        DriveCD = 11,
        /// <summary>
        ///  Icon for a RAM disk drive. 
        /// </summary>
        DriveRam = 12,
        /// <summary>
        ///  Icon for an entire network. 
        /// </summary>
        World = 13,
        /// <summary>
        ///  Icon for a computer on the network.
        /// </summary>
        Server = 15,
        /// <summary>
        ///  Icon for a printer. 
        /// </summary>
        Printer = 16,
        /// <summary>
        /// Icon for My Network places.
        /// </summary>
        MyNetwork = 17,
        /// <summary>
        /// Icon for search (magnifying glass).
        /// </summary>
        Find = 22,
        /// <summary>
        ///  Icon for help.     
        /// </summary>
        Help = 23,
        /// <summary>
        ///  Icon for an overlay indicating shared items.        
        /// </summary>
        Share = 28,
        /// <summary>
        ///  Icon for an overlay indicating shortcuts to items.
        /// </summary>
        Link = 29,
        /// <summary>
        /// Icon for an overlay for slow items.
        /// </summary>
        SlowFile = 30,
        /// <summary>
        ///  Icon for a empty recycle bin.
        /// </summary>
        Recycler = 31,
        /// <summary>
        ///  Icon for a full recycle bin.
        /// </summary>
        RecyclerFull = 32,
        /// <summary>
        ///  Icon for audio CD media.
        /// </summary>
        MediaCDAudio = 40,
        /// <summary>
        ///  Icon for a security lock.
        /// </summary>
        Lock = 47,
        /// <summary>
        ///  Icon for a auto list.
        /// </summary>
        AutoList = 49,
        /// <summary>
        /// Icon for a network printer.
        /// </summary>
        PrinterNet = 50,
        /// <summary>
        ///  Icon for a server share.
        /// </summary>
        ServerShare = 51,
        /// <summary>
        ///  Icon for a Fax printer.
        /// </summary>
        PrinterFax = 52,
        /// <summary>
        /// Icon for a networked Fax printer.
        /// </summary>
        PrinterFaxNet = 53,
        /// <summary>
        ///  Icon for print to file.
        /// </summary>
        PrinterFile = 54,
        /// <summary>
        /// Icon for a stack.
        /// </summary>
        Stack = 55,
        /// <summary>
        ///  Icon for a SVCD media.
        /// </summary>
        MediaSvcd = 56,
        /// <summary>
        ///  Icon for a folder containing other items.
        /// </summary>
        StuffedFolder = 57,
        /// <summary>
        ///  Icon for an unknown drive.
        /// </summary>
        DriveUnknown = 58,
        /// <summary>
        ///  Icon for a DVD drive. 
        /// </summary>
        DriveDvd = 59,
        /// <summary>
        /// Icon for DVD media.
        /// </summary>
        MediaDvd = 60,
        /// <summary>
        ///  Icon for DVD-RAM media.   
        /// </summary>
        MediaDvdRam = 61,
        /// <summary>
        /// Icon for DVD-RW media.
        /// </summary>
        MediaDvdRW = 62,
        /// <summary>
        ///  Icon for DVD-R media.
        /// </summary>
        MediaDvdR = 63,
        /// <summary>
        ///  Icon for a DVD-ROM media.
        /// </summary>
        MediaDvdRom = 64,
        /// <summary>
        ///  Icon for CD+ (Enhanced CD) media.
        /// </summary>
        MediaCDAudioPlus = 65,
        /// <summary>
        ///  Icon for CD-RW media.
        /// </summary>
        MediaCDRW = 66,
        /// <summary>
        ///  Icon for a CD-R media.
        /// </summary>
        MediaCDR = 67,
        /// <summary>
        ///  Icon burning a CD.
        /// </summary>
        MediaCDBurn = 68,
        /// <summary>
        ///  Icon for blank CD media.
        /// </summary>
        MediaBlankCD = 69,
        /// <summary>
        ///  Icon for CD-ROM media.
        /// </summary>
        MediaCDRom = 70,
        /// <summary>
        ///  Icon for audio files.
        /// </summary>
        AudioFiles = 71,
        /// <summary>
        ///  Icon for image files.
        /// </summary>
        ImageFiles = 72,
        /// <summary>
        ///  Icon for video files.
        /// </summary>
        VideoFiles = 73,
        /// <summary>
        ///  Icon for mixed Files.
        /// </summary>
        MixedFiles = 74,
        /// <summary>
        /// Icon for a folder back.
        /// </summary>
        FolderBack = 75,
        /// <summary>
        ///  Icon for a folder front.
        /// </summary>
        FolderFront = 76,
        /// <summary>
        ///  Icon for a security shield. Use for UAC prompts only.
        /// </summary>
        Shield = 77,
        /// <summary>
        ///  Icon for a warning.
        /// </summary>
        Warning = 78,
        /// <summary>
        ///  Icon for an informational message.
        /// </summary>
        Info = 79,
        /// <summary>
        ///  Icon for an error message.
        /// </summary>
        Error = 80,
        /// <summary>
        ///  Icon for a key.
        /// </summary>
        Key = 81,
        /// <summary>
        ///  Icon for software.
        /// </summary>
        Software = 82,
        /// <summary>
        ///  Icon for a rename.
        /// </summary>
        Rename = 83,
        /// <summary>
        ///  Icon for delete.
        /// </summary>
        Delete = 84,
        /// <summary>
        ///  Icon for audio DVD media.
        /// </summary>
        MediaAudioDvd = 85,
        /// <summary>
        ///  Icon for movie DVD media.
        /// </summary>
        MediaMovieDvd = 86,
        /// <summary>
        ///  Icon for enhanced CD media.
        /// </summary>
        MediaEnhancedCD = 87,
        /// <summary>
        ///  Icon for enhanced DVD media.
        /// </summary>
        MediaEnhancedDvd = 88,
        /// <summary>
        ///  Icon for HD-DVD media.
        /// </summary>
        MediaHDDvd = 89,
        /// <summary>
        ///  Icon for BluRay media.
        /// </summary>
        MediaBluRay = 90,
        /// <summary>
        ///  Icon for VCD media.
        /// </summary>
        MediaVcd = 91,
        /// <summary>
        ///  Icon for DVD+R media.
        /// </summary>
        MediaDvdPlusR = 92,
        /// <summary>
        ///  Icon for DVD+RW media.
        /// </summary>
        MediaDvdPlusRW = 93,
        /// <summary>
        ///  Icon for desktop computer.
        /// </summary>
        DesktopPC = 94,
        /// <summary>
        ///  Icon for mobile computer (laptop/notebook).
        /// </summary>
        MobilePC = 95,
        /// <summary>
        ///  Icon for users.
        /// </summary>
        Users = 96,
        /// <summary>
        ///  Icon for smart media.
        /// </summary>
        MediaSmartMedia = 97,
        /// <summary>
        ///  Icon for compact flash.
        /// </summary>
        MediaCompactFlash = 98,
        /// <summary>
        ///  Icon for a cell phone.
        /// </summary>
        DeviceCellPhone = 99,
        /// <summary>
        ///  Icon for a camera.
        /// </summary>
        DeviceCamera = 100,
        /// <summary>
        ///  Icon for video camera.
        /// </summary>
        DeviceVideoCamera = 101,
        /// <summary>
        ///  Icon for audio player.
        /// </summary>
        DeviceAudioPlayer = 102,
        /// <summary>
        ///  Icon for connecting to network.
        /// </summary>
        NetworkConnect = 103,
        /// <summary>
        ///  Icon for the Internet.
        /// </summary>
        Internet = 104,
        /// <summary>
        ///  Icon for a ZIP file.
        /// </summary>
        ZipFile = 105,
        /// <summary>
        /// Icon for settings.
        /// </summary>
        Settings = 106,

        // 107-131 are internal Vista RTM icons
        // 132-159 for SP1 icons

        /// <summary>
        /// HDDVD Drive (all types)
        /// </summary>
        DriveHDDVD = 132,

        /// <summary>
        /// Icon for BluRay Drive (all types)
        /// </summary>
        DriveBluRay = 133,

        /// <summary>
        /// Icon for HDDVD-ROM Media
        /// </summary>
        MediaHDDVDROM = 134,

        /// <summary>
        /// Icon for HDDVD-R Media
        /// </summary>
        MediaHDDVDR = 135,

        /// <summary>
        /// Icon for HDDVD-RAM Media
        /// </summary>
        MediaHDDVDRAM = 136,

        /// <summary>
        /// Icon for BluRay ROM Media
        /// </summary>
        MediaBluRayROM = 137,

        /// <summary>
        /// Icon for BluRay R Media
        /// </summary>
        MediaBluRayR = 138,

        /// <summary>
        /// Icon for BluRay RE Media (Rewriable and RAM)
        /// </summary>
        MediaBluRayRE = 139,

        /// <summary>
        /// Icon for Clustered disk
        /// </summary>
        ClusteredDisk = 140,

    }

}
