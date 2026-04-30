using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ImageMove
{
    internal sealed class FolderPickerDialog : IDisposable
    {
        private const uint CancelledHResult = 0x800704C7;

        private IFileOpenDialog dialog;

        public string Title { get; set; }

        public string InitialFolder { get; set; }

        public string SelectedPath { get; private set; }

        public bool ShowDialog(IntPtr ownerHandle)
        {
            dialog = (IFileOpenDialog)new FileOpenDialogRCW();

            dialog.GetOptions(out FOS options);
            dialog.SetOptions(options | FOS.PICKFOLDERS | FOS.FORCEFILESYSTEM | FOS.PATHMUSTEXIST);

            if (!string.IsNullOrWhiteSpace(Title))
            {
                dialog.SetTitle(Title);
                dialog.SetOkButtonLabel("選択");
            }

            IShellItem initialFolderItem = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(InitialFolder) && Directory.Exists(InitialFolder))
                {
                    initialFolderItem = CreateShellItem(InitialFolder);
                    dialog.SetFolder(initialFolderItem);
                }

                uint result = dialog.Show(ownerHandle);
                if (result == CancelledHResult)
                {
                    return false;
                }

                if (result != 0)
                {
                    Marshal.ThrowExceptionForHR(unchecked((int)result));
                }

                dialog.GetResult(out IShellItem selectedItem);
                try
                {
                    SelectedPath = GetDisplayName(selectedItem);
                }
                finally
                {
                    ReleaseComObject(selectedItem);
                }

                return !string.IsNullOrWhiteSpace(SelectedPath);
            }
            finally
            {
                ReleaseComObject(initialFolderItem);
            }
        }

        public void Dispose()
        {
            ReleaseComObject(dialog);
            dialog = null;
        }

        private static IShellItem CreateShellItem(string path)
        {
            Guid shellItemGuid = typeof(IShellItem).GUID;
            SHCreateItemFromParsingName(path, IntPtr.Zero, shellItemGuid, out IShellItem shellItem);
            return shellItem;
        }

        private static string GetDisplayName(IShellItem shellItem)
        {
            shellItem.GetDisplayName(SIGDN.FILESYSPATH, out IntPtr displayNamePointer);

            try
            {
                return Marshal.PtrToStringUni(displayNamePointer);
            }
            finally
            {
                Marshal.FreeCoTaskMem(displayNamePointer);
            }
        }

        private static void ReleaseComObject(object comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
            {
                Marshal.ReleaseComObject(comObject);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            string path,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IShellItem shellItem);

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRCW
        {
        }

        [ComImport]
        [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            uint Show(IntPtr parent);
            void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IFileDialogEvents pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(FOS fos);
            void GetOptions(out FOS pfos);
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
            void AddPlace(IShellItem psi, FDAP fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter([MarshalAs(UnmanagedType.Interface)] object pFilter);
            void GetResults([MarshalAs(UnmanagedType.Interface)] out object ppenum);
            void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out object ppsai);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [ComImport]
        [Guid("973510DB-7D7F-452B-8975-74A85828D354")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialogEvents
        {
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct COMDLG_FILTERSPEC
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszSpec;
        }

        private enum FDAP
        {
            BOTTOM = 0,
            TOP = 1
        }

        [Flags]
        private enum FOS : uint
        {
            PICKFOLDERS = 0x00000020,
            FORCEFILESYSTEM = 0x00000040,
            PATHMUSTEXIST = 0x00000800
        }

        private enum SIGDN : uint
        {
            FILESYSPATH = 0x80058000
        }
    }
}
