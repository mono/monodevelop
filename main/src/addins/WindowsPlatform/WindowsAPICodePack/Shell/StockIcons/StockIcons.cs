//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// Collection of all the standard system stock icons
    /// </summary>
    public class StockIcons
    {
        #region Private Members

        private IDictionary<StockIconIdentifier, StockIcon> stockIconCache;
        private StockIconSize defaultSize = StockIconSize.Large;
        private bool isSelected;
        private bool isLinkOverlay;

        #endregion

        #region Public Constructors

        /// <summary>
        /// Creates a stock icon collection using the default options for 
        /// size, link overlay and selection state.
        /// </summary>
        public StockIcons()
        {
            // Create an empty dictionary. Stock icons will be created when requested
            // or when they are enumerated on this collection
            stockIconCache = new Dictionary<StockIconIdentifier, StockIcon>();

            Array allIdentifiers = Enum.GetValues(typeof(StockIconIdentifier));

            foreach (StockIconIdentifier id in allIdentifiers)
            {
                stockIconCache.Add(id, null);
            }
        }

        /// <summary>
        /// Overloaded constructor that takes in size and Boolean values for 
        /// link overlay and selected icon state. The settings are applied to 
        /// all the stock icons in the collection.
        /// </summary>
        /// <param name="size">StockIcon size for all the icons in the collection.</param>
        /// <param name="linkOverlay">Link Overlay state for all the icons in the collection.</param>
        /// <param name="selected">Selection state for all the icons in the collection.</param>
        public StockIcons(StockIconSize size, bool linkOverlay, bool selected)
        {
            defaultSize = size;
            isLinkOverlay = linkOverlay;
            isSelected = selected;

            // Create an empty dictionary. Stock icons will be created when requested
            // or when they are enumerated on this collection
            stockIconCache = new Dictionary<StockIconIdentifier, StockIcon>();

            Array allIdentifiers = Enum.GetValues(typeof(StockIconIdentifier));

            foreach (StockIconIdentifier id in allIdentifiers)
            {
                stockIconCache.Add(id, null);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the default stock icon size in one of the StockIconSize values.
        /// This size applies to all the stock icons in the collection.
        /// </summary>
        public StockIconSize DefaultSize
        {
            get { return defaultSize; }
        }

        /// <summary>
        /// Gets the default link overlay state for the icon. This property 
        /// applies to all the stock icons in the collection.
        /// </summary>
        public bool DefaultLinkOverlay
        {
            get { return isLinkOverlay; }
        }

        /// <summary>
        /// Gets the default selected state for the icon. This property 
        /// applies to all the stock icons in the collection.
        /// </summary>
        public bool DefaultSelectedState
        {
            get { return isSelected; }
        }

        /// <summary>
        /// Gets a collection of all the system stock icons
        /// </summary>
        public ICollection<StockIcon> AllStockIcons
        {
            get { return GetAllStockIcons(); }
        }

        /// <summary>
        /// Icon for a document (blank page), no associated program.
        /// </summary>
        public StockIcon DocumentNotAssociated { get { return GetStockIcon(StockIconIdentifier.DocumentNotAssociated); } }

        /// <summary>
        /// Icon for a document with an associated program.
        /// </summary>
        public StockIcon DocumentAssociated { get { return GetStockIcon(StockIconIdentifier.DocumentAssociated); } }

        /// <summary>
        ///  Icon for a generic application with no custom icon.
        /// </summary>
        public StockIcon Application { get { return GetStockIcon(StockIconIdentifier.Application); } }

        /// <summary>
        ///  Icon for a closed folder.
        /// </summary>
        public StockIcon Folder { get { return GetStockIcon(StockIconIdentifier.Folder); } }

        /// <summary>
        /// Icon for an open folder. 
        /// </summary>
        public StockIcon FolderOpen { get { return GetStockIcon(StockIconIdentifier.FolderOpen); } }

        /// <summary>
        /// Icon for a 5.25" floppy disk drive.
        /// </summary>
        public StockIcon Drive525 { get { return GetStockIcon(StockIconIdentifier.Drive525); } }

        /// <summary>
        ///  Icon for a 3.5" floppy disk drive. 
        /// </summary>
        public StockIcon Drive35 { get { return GetStockIcon(StockIconIdentifier.Drive35); } }

        /// <summary>
        ///  Icon for a removable drive.
        /// </summary>
        public StockIcon DriveRemove { get { return GetStockIcon(StockIconIdentifier.DriveRemove); } }

        /// <summary>
        ///  Icon for a fixed (hard disk) drive.
        /// </summary>
        public StockIcon DriveFixed { get { return GetStockIcon(StockIconIdentifier.DriveFixed); } }

        /// <summary>
        ///  Icon for a network drive.
        /// </summary>
        public StockIcon DriveNetwork { get { return GetStockIcon(StockIconIdentifier.DriveNetwork); } }

        /// <summary>
        ///  Icon for a disconnected network drive.
        /// </summary>
        public StockIcon DriveNetworkDisabled { get { return GetStockIcon(StockIconIdentifier.DriveNetworkDisabled); } }

        /// <summary>
        ///  Icon for a CD drive.
        /// </summary>
        public StockIcon DriveCD { get { return GetStockIcon(StockIconIdentifier.DriveCD); } }

        /// <summary>
        ///  Icon for a RAM disk drive. 
        /// </summary>
        public StockIcon DriveRam { get { return GetStockIcon(StockIconIdentifier.DriveRam); } }

        /// <summary>
        ///  Icon for an entire network. 
        /// </summary>
        public StockIcon World { get { return GetStockIcon(StockIconIdentifier.World); } }

        /// <summary>
        ///  Icon for a computer on the network.
        /// </summary>
        public StockIcon Server { get { return GetStockIcon(StockIconIdentifier.Server); } }

        /// <summary>
        ///  Icon for a printer. 
        /// </summary>
        public StockIcon Printer { get { return GetStockIcon(StockIconIdentifier.Printer); } }

        /// <summary>
        /// Icon for My Network places.
        /// </summary>
        public StockIcon MyNetwork { get { return GetStockIcon(StockIconIdentifier.MyNetwork); } }

        /// <summary>
        /// Icon for search (magnifying glass).
        /// </summary>
        public StockIcon Find { get { return GetStockIcon(StockIconIdentifier.Find); } }

        /// <summary>
        ///  Icon for help.     
        /// </summary>
        public StockIcon Help { get { return GetStockIcon(StockIconIdentifier.Help); } }

        /// <summary>
        ///  Icon for an overlay indicating shared items.        
        /// </summary>
        public StockIcon Share { get { return GetStockIcon(StockIconIdentifier.Share); } }

        /// <summary>
        ///  Icon for an overlay indicating shortcuts to items.
        /// </summary>
        public StockIcon Link { get { return GetStockIcon(StockIconIdentifier.Link); } }

        /// <summary>
        /// Icon for an overlay for slow items.
        /// </summary>
        public StockIcon SlowFile { get { return GetStockIcon(StockIconIdentifier.SlowFile); } }

        /// <summary>
        ///  Icon for a empty recycle bin.
        /// </summary>
        public StockIcon Recycler { get { return GetStockIcon(StockIconIdentifier.Recycler); } }

        /// <summary>
        ///  Icon for a full recycle bin.
        /// </summary>
        public StockIcon RecyclerFull { get { return GetStockIcon(StockIconIdentifier.RecyclerFull); } }

        /// <summary>
        ///  Icon for audio CD media.
        /// </summary>
        public StockIcon MediaCDAudio { get { return GetStockIcon(StockIconIdentifier.MediaCDAudio); } }

        /// <summary>
        ///  Icon for a security lock.
        /// </summary>
        public StockIcon Lock { get { return GetStockIcon(StockIconIdentifier.Lock); } }

        /// <summary>
        ///  Icon for a auto list.
        /// </summary>
        public StockIcon AutoList { get { return GetStockIcon(StockIconIdentifier.AutoList); } }

        /// <summary>
        /// Icon for a network printer.
        /// </summary>
        public StockIcon PrinterNet { get { return GetStockIcon(StockIconIdentifier.PrinterNet); } }

        /// <summary>
        ///  Icon for a server share.
        /// </summary>
        public StockIcon ServerShare { get { return GetStockIcon(StockIconIdentifier.ServerShare); } }

        /// <summary>
        ///  Icon for a Fax printer.
        /// </summary>
        public StockIcon PrinterFax { get { return GetStockIcon(StockIconIdentifier.PrinterFax); } }

        /// <summary>
        /// Icon for a networked Fax printer.
        /// </summary>
        public StockIcon PrinterFaxNet { get { return GetStockIcon(StockIconIdentifier.PrinterFaxNet); } }

        /// <summary>
        ///  Icon for print to file.
        /// </summary>
        public StockIcon PrinterFile { get { return GetStockIcon(StockIconIdentifier.PrinterFile); } }

        /// <summary>
        /// Icon for a stack.
        /// </summary>
        public StockIcon Stack { get { return GetStockIcon(StockIconIdentifier.Stack); } }

        /// <summary>
        ///  Icon for a SVCD media.
        /// </summary>
        public StockIcon MediaSvcd { get { return GetStockIcon(StockIconIdentifier.MediaSvcd); } }

        /// <summary>
        ///  Icon for a folder containing other items.
        /// </summary>
        public StockIcon StuffedFolder { get { return GetStockIcon(StockIconIdentifier.StuffedFolder); } }

        /// <summary>
        ///  Icon for an unknown drive.
        /// </summary>
        public StockIcon DriveUnknown { get { return GetStockIcon(StockIconIdentifier.DriveUnknown); } }

        /// <summary>
        ///  Icon for a DVD drive. 
        /// </summary>
        public StockIcon DriveDvd { get { return GetStockIcon(StockIconIdentifier.DriveDvd); } }

        /// <summary>
        /// Icon for DVD media.
        /// </summary>
        public StockIcon MediaDvd { get { return GetStockIcon(StockIconIdentifier.MediaDvd); } }

        /// <summary>
        ///  Icon for DVD-RAM media.   
        /// </summary>
        public StockIcon MediaDvdRam { get { return GetStockIcon(StockIconIdentifier.MediaDvdRam); } }

        /// <summary>
        /// Icon for DVD-RW media.
        /// </summary>
        public StockIcon MediaDvdRW { get { return GetStockIcon(StockIconIdentifier.MediaDvdRW); } }

        /// <summary>
        ///  Icon for DVD-R media.
        /// </summary>
        public StockIcon MediaDvdR { get { return GetStockIcon(StockIconIdentifier.MediaDvdR); } }

        /// <summary>
        ///  Icon for a DVD-ROM media.
        /// </summary>
        public StockIcon MediaDvdRom { get { return GetStockIcon(StockIconIdentifier.MediaDvdRom); } }

        /// <summary>
        ///  Icon for CD+ (Enhanced CD) media.
        /// </summary>
        public StockIcon MediaCDAudioPlus { get { return GetStockIcon(StockIconIdentifier.MediaCDAudioPlus); } }

        /// <summary>
        ///  Icon for CD-RW media.
        /// </summary>
        public StockIcon MediaCDRW { get { return GetStockIcon(StockIconIdentifier.MediaCDRW); } }

        /// <summary>
        ///  Icon for a CD-R media.
        /// </summary>
        public StockIcon MediaCDR { get { return GetStockIcon(StockIconIdentifier.MediaCDR); } }

        /// <summary>
        ///  Icon burning a CD.
        /// </summary>
        public StockIcon MediaCDBurn { get { return GetStockIcon(StockIconIdentifier.MediaCDBurn); } }

        /// <summary>
        ///  Icon for blank CD media.
        /// </summary>
        public StockIcon MediaBlankCD { get { return GetStockIcon(StockIconIdentifier.MediaBlankCD); } }

        /// <summary>
        ///  Icon for CD-ROM media.
        /// </summary>
        public StockIcon MediaCDRom { get { return GetStockIcon(StockIconIdentifier.MediaCDRom); } }

        /// <summary>
        ///  Icon for audio files.
        /// </summary>
        public StockIcon AudioFiles { get { return GetStockIcon(StockIconIdentifier.AudioFiles); } }

        /// <summary>
        ///  Icon for image files.
        /// </summary>
        public StockIcon ImageFiles { get { return GetStockIcon(StockIconIdentifier.ImageFiles); } }

        /// <summary>
        ///  Icon for video files.
        /// </summary>
        public StockIcon VideoFiles { get { return GetStockIcon(StockIconIdentifier.VideoFiles); } }

        /// <summary>
        ///  Icon for mixed Files.
        /// </summary>
        public StockIcon MixedFiles { get { return GetStockIcon(StockIconIdentifier.MixedFiles); } }

        /// <summary>
        /// Icon for a folder back.
        /// </summary>
        public StockIcon FolderBack { get { return GetStockIcon(StockIconIdentifier.FolderBack); } }

        /// <summary>
        ///  Icon for a folder front.
        /// </summary>
        public StockIcon FolderFront { get { return GetStockIcon(StockIconIdentifier.FolderFront); } }

        /// <summary>
        ///  Icon for a security shield. Use for UAC prompts only.
        /// </summary>
        public StockIcon Shield { get { return GetStockIcon(StockIconIdentifier.Shield); } }

        /// <summary>
        ///  Icon for a warning.
        /// </summary>
        public StockIcon Warning { get { return GetStockIcon(StockIconIdentifier.Warning); } }

        /// <summary>
        ///  Icon for an informational message.
        /// </summary>
        public StockIcon Info { get { return GetStockIcon(StockIconIdentifier.Info); } }

        /// <summary>
        ///  Icon for an error message.
        /// </summary>
        public StockIcon Error { get { return GetStockIcon(StockIconIdentifier.Error); } }

        /// <summary>
        ///  Icon for a key.
        /// </summary>
        public StockIcon Key { get { return GetStockIcon(StockIconIdentifier.Key); } }

        /// <summary>
        ///  Icon for software.
        /// </summary>
        public StockIcon Software { get { return GetStockIcon(StockIconIdentifier.Software); } }

        /// <summary>
        ///  Icon for a rename.
        /// </summary>
        public StockIcon Rename { get { return GetStockIcon(StockIconIdentifier.Rename); } }

        /// <summary>
        ///  Icon for delete.
        /// </summary>
        public StockIcon Delete { get { return GetStockIcon(StockIconIdentifier.Delete); } }

        /// <summary>
        ///  Icon for audio DVD media.
        /// </summary>
        public StockIcon MediaAudioDvd { get { return GetStockIcon(StockIconIdentifier.MediaAudioDvd); } }

        /// <summary>
        ///  Icon for movie DVD media.
        /// </summary>
        public StockIcon MediaMovieDvd { get { return GetStockIcon(StockIconIdentifier.MediaMovieDvd); } }

        /// <summary>
        ///  Icon for enhanced CD media.
        /// </summary>
        public StockIcon MediaEnhancedCD { get { return GetStockIcon(StockIconIdentifier.MediaEnhancedCD); } }

        /// <summary>
        ///  Icon for enhanced DVD media.
        /// </summary>
        public StockIcon MediaEnhancedDvd { get { return GetStockIcon(StockIconIdentifier.MediaEnhancedDvd); } }

        /// <summary>
        ///  Icon for HD-DVD media.
        /// </summary>
        public StockIcon MediaHDDvd { get { return GetStockIcon(StockIconIdentifier.MediaHDDvd); } }

        /// <summary>
        ///  Icon for BluRay media.
        /// </summary>
        public StockIcon MediaBluRay { get { return GetStockIcon(StockIconIdentifier.MediaBluRay); } }

        /// <summary>
        ///  Icon for VCD media.
        /// </summary>
        public StockIcon MediaVcd { get { return GetStockIcon(StockIconIdentifier.MediaVcd); } }

        /// <summary>
        ///  Icon for DVD+R media.
        /// </summary>
        public StockIcon MediaDvdPlusR { get { return GetStockIcon(StockIconIdentifier.MediaDvdPlusR); } }

        /// <summary>
        ///  Icon for DVD+RW media.
        /// </summary>
        public StockIcon MediaDvdPlusRW { get { return GetStockIcon(StockIconIdentifier.MediaDvdPlusRW); } }

        /// <summary>
        ///  Icon for desktop computer.
        /// </summary>
        public StockIcon DesktopPC { get { return GetStockIcon(StockIconIdentifier.DesktopPC); } }

        /// <summary>
        ///  Icon for mobile computer (laptop/notebook).
        /// </summary>
        public StockIcon MobilePC { get { return GetStockIcon(StockIconIdentifier.MobilePC); } }

        /// <summary>
        ///  Icon for users.
        /// </summary>
        public StockIcon Users { get { return GetStockIcon(StockIconIdentifier.Users); } }

        /// <summary>
        ///  Icon for smart media.
        /// </summary>
        public StockIcon MediaSmartMedia { get { return GetStockIcon(StockIconIdentifier.MediaSmartMedia); } }

        /// <summary>
        ///  Icon for compact flash.
        /// </summary>
        public StockIcon MediaCompactFlash { get { return GetStockIcon(StockIconIdentifier.MediaCompactFlash); } }

        /// <summary>
        ///  Icon for a cell phone.
        /// </summary>
        public StockIcon DeviceCellPhone { get { return GetStockIcon(StockIconIdentifier.DeviceCellPhone); } }

        /// <summary>
        ///  Icon for a camera.
        /// </summary>
        public StockIcon DeviceCamera { get { return GetStockIcon(StockIconIdentifier.DeviceCamera); } }

        /// <summary>
        ///  Icon for video camera.
        /// </summary>
        public StockIcon DeviceVideoCamera { get { return GetStockIcon(StockIconIdentifier.DeviceVideoCamera); } }

        /// <summary>
        ///  Icon for audio player.
        /// </summary>
        public StockIcon DeviceAudioPlayer { get { return GetStockIcon(StockIconIdentifier.DeviceAudioPlayer); } }

        /// <summary>
        ///  Icon for connecting to network.
        /// </summary>
        public StockIcon NetworkConnect { get { return GetStockIcon(StockIconIdentifier.NetworkConnect); } }

        /// <summary>
        ///  Icon for the Internet.
        /// </summary>
        public StockIcon Internet { get { return GetStockIcon(StockIconIdentifier.Internet); } }

        /// <summary>
        ///  Icon for a ZIP file.
        /// </summary>
        public StockIcon ZipFile { get { return GetStockIcon(StockIconIdentifier.ZipFile); } }

        /// <summary>
        /// Icon for settings.
        /// </summary>
        public StockIcon Settings { get { return GetStockIcon(StockIconIdentifier.Settings); } }

        /// <summary>
        /// HDDVD Drive (all types)
        /// </summary>
        public StockIcon DriveHDDVD { get { return GetStockIcon(StockIconIdentifier.DriveHDDVD); } }

        /// <summary>
        /// Icon for BluRay Drive (all types)
        /// </summary>
        public StockIcon DriveBluRay { get { return GetStockIcon(StockIconIdentifier.DriveBluRay); } }

        /// <summary>
        /// Icon for HDDVD-ROM Media
        /// </summary>
        public StockIcon MediaHDDVDROM { get { return GetStockIcon(StockIconIdentifier.MediaHDDVDROM); } }

        /// <summary>
        /// Icon for HDDVD-R Media
        /// </summary>
        public StockIcon MediaHDDVDR { get { return GetStockIcon(StockIconIdentifier.MediaHDDVDR); } }

        /// <summary>
        /// Icon for HDDVD-RAM Media
        /// </summary>
        public StockIcon MediaHDDVDRAM { get { return GetStockIcon(StockIconIdentifier.MediaHDDVDRAM); } }

        /// <summary>
        /// Icon for BluRay ROM Media
        /// </summary>
        public StockIcon MediaBluRayROM { get { return GetStockIcon(StockIconIdentifier.MediaBluRayROM); } }

        /// <summary>
        /// Icon for BluRay R Media
        /// </summary>
        public StockIcon MediaBluRayR { get { return GetStockIcon(StockIconIdentifier.MediaBluRayR); } }

        /// <summary>
        /// Icon for BluRay RE Media (Rewriable and RAM)
        /// </summary>
        public StockIcon MediaBluRayRE { get { return GetStockIcon(StockIconIdentifier.MediaBluRayRE); } }

        /// <summary>
        /// Icon for Clustered disk
        /// </summary>
        public StockIcon ClusteredDisk { get { return GetStockIcon(StockIconIdentifier.ClusteredDisk); } }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the existing stock icon from the internal cache, or creates a new one
        /// based on the current settings if it's not in the cache.
        /// </summary>
        /// <param name="stockIconIdentifier">Unique identifier for the requested stock icon</param>
        /// <returns>Stock Icon based on the identifier given (either from the cache or created new)</returns>
        private StockIcon GetStockIcon(StockIconIdentifier stockIconIdentifier)
        {
            // Check the cache first
            if (stockIconCache[stockIconIdentifier] != null)
                return stockIconCache[stockIconIdentifier];
            else
            {
                // Create a new icon based on our default settings
                StockIcon icon = new StockIcon(stockIconIdentifier, defaultSize, isLinkOverlay, isSelected);

                try
                {
                    // Add it to the cache
                    stockIconCache[stockIconIdentifier] = icon;
                }
                catch
                {
                    icon.Dispose();
                    throw;
                }

                // Return 
                return icon;
            }
        }

        private ICollection<StockIcon> GetAllStockIcons()
        {
            // Create a list of stock Identifiers
            StockIconIdentifier[] ids = new StockIconIdentifier[stockIconCache.Count];
            stockIconCache.Keys.CopyTo(ids, 0);

            // For each identifier, if our cache is null, create a new stock icon
            foreach (StockIconIdentifier id in ids)
            {
                if (stockIconCache[id] == null)
                    GetStockIcon(id);
            }

            // return the list of stock icons
            return stockIconCache.Values;
        }


        #endregion

    }


}
