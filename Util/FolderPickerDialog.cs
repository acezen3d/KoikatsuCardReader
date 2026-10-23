using System;
using System.IO;
using System.Runtime.InteropServices;

namespace KoikatsuCardReader.Util
{
    /// <summary>
    /// Modern Windows folder picker (IFileOpenDialog with FOS_PICKFOLDERS).
    /// Same dialog as File Explorer / Save As, available on Windows Vista and later.
    /// Adapted from a community COM wrapper; Unity-specific code and the broken filter marshaling were removed.
    /// </summary>
    public static class FolderPickerDialog
    {
        /// <summary>
        /// Shows the folder picker.
        /// Returns the selected folder path, or null if the user cancelled or an error occurred.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="initialDir">Starting folder. Ignored if it does not exist.</param>
        /// <param name="owner">Owner window handle. Defaults to the active window.</param>
        public static string Show(string title, string initialDir, IntPtr owner = default)
        {
            var dialog = (IFileOpenDialog)new FileOpenDialogCoClass();

            try
            {
                dialog.GetOptions(out var options);
                dialog.SetOptions(options | FosFlags.FOS_PICKFOLDERS | FosFlags.FOS_FORCEFILESYSTEM | FosFlags.FOS_NOCHANGEDIR);

                if (!string.IsNullOrEmpty(title))
                    dialog.SetTitle(title);

                if (!string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir))
                {
                    var riid = IidShellItem;
                    if (NativeMethods.SHCreateItemFromParsingName(initialDir, IntPtr.Zero, ref riid, out var item) == 0)
                    {
                        dialog.SetFolder(item);
                        Marshal.ReleaseComObject(item);
                    }
                }

                if (owner == default)
                    owner = NativeMethods.GetActiveWindow();

                var hr = dialog.Show(owner);
                if (hr != 0)
                    return null; // cancelled (HRESULT_FROM_WIN32(ERROR_CANCELLED)) or failed

                dialog.GetResult(out var result);
                try
                {
                    result.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                    return path;
                }
                finally
                {
                    Marshal.ReleaseComObject(result);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        }

        /// <summary>IID of IShellItem.</summary>
        private static readonly Guid IidShellItem = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");

        [ComImport]
        [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        [ClassInterface(ClassInterfaceType.None)]
        private class FileOpenDialogCoClass
        {
        }

        // Method order must match the IFileOpenDialog vtable exactly.
        [ComImport]
        [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);
            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(FosFlags fos);
            void GetOptions(out FosFlags pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int alignment);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
            void GetResults(out IntPtr ppenum);
            void GetSelectedItems(out IntPtr ppsai);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        private enum SIGDN : uint
        {
            SIGDN_FILESYSPATH = 0x80058000
        }

        [Flags]
        private enum FosFlags : uint
        {
            FOS_NOCHANGEDIR = 0x00000008,
            FOS_PICKFOLDERS = 0x00000020,
            FOS_FORCEFILESYSTEM = 0x00000040,
        }

        private static class NativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern int SHCreateItemFromParsingName(
                [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                IntPtr pbc,
                ref Guid riid,
                [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

            [DllImport("user32.dll")]
            public static extern IntPtr GetActiveWindow();
        }
    }
}
