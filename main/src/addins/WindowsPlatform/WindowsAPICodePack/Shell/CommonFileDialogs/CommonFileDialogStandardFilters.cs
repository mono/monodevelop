//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Shell.Resources;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Defines the class of commonly used file filters.
    /// </summary>
    public static class CommonFileDialogStandardFilters
    {
        private static CommonFileDialogFilter textFilesFilter;
        /// <summary>
        /// Gets a value that specifies the filter for *.txt files.
        /// </summary>
        public static CommonFileDialogFilter TextFiles
        {
            get
            {
                if (textFilesFilter == null)
                {
                    textFilesFilter = new CommonFileDialogFilter(LocalizedMessages.CommonFiltersText, "*.txt");
                }
                return textFilesFilter;
            }
        }

        private static CommonFileDialogFilter pictureFilesFilter;
        /// <summary>
        /// Gets a value that specifies the filter for picture files.
        /// </summary>
        public static CommonFileDialogFilter PictureFiles
        {
            get
            {
                if (pictureFilesFilter == null)
                {
                    pictureFilesFilter = new CommonFileDialogFilter(LocalizedMessages.CommonFiltersPicture,
                        "*.bmp, *.jpg, *.jpeg, *.png, *.ico");
                }
                return pictureFilesFilter;
            }

        }
        private static CommonFileDialogFilter officeFilesFilter;
        /// <summary>
        /// Gets a value that specifies the filter for Microsoft Office files.
        /// </summary>
        public static CommonFileDialogFilter OfficeFiles
        {
            get
            {
                if (officeFilesFilter == null)
                {
                    officeFilesFilter = new CommonFileDialogFilter(LocalizedMessages.CommonFiltersOffice,
                        "*.doc, *.docx, *.xls, *.xlsx, *.ppt, *.pptx");
                }
                return officeFilesFilter;
            }
        }
    }
}
