using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ImageMove
{
    internal static class TopMostMessageBox
    {
        private const uint MB_SETFOREGROUND = 0x00010000;
        private const uint MB_TOPMOST = 0x00040000;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW", SetLastError = true)]
        private static extern int NativeMessageBox(IntPtr ownerHandle, string text, string caption, uint type);

        internal static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            IntPtr ownerHandle = GetOwnerHandle(owner);
            uint type = (uint)buttons | (uint)icon | MB_SETFOREGROUND | MB_TOPMOST;
            int result = NativeMessageBox(ownerHandle, text ?? string.Empty, caption ?? string.Empty, type);
            return Enum.IsDefined(typeof(DialogResult), result) ? (DialogResult)result : DialogResult.None;
        }

        private static IntPtr GetOwnerHandle(IWin32Window owner)
        {
            if (owner != null)
            {
                return owner.Handle;
            }

            Form activeForm = Form.ActiveForm;
            if (activeForm != null && !activeForm.IsDisposed)
            {
                return activeForm.Handle;
            }

            return IntPtr.Zero;
        }
    }
}
