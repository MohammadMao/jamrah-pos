using System.Runtime.InteropServices;

namespace JamrahPOS.Helpers
{
    /// <summary>
    /// Sends raw bytes (e.g. ESC/POS commands) directly to a Windows printer
    /// without any driver-level processing.
    /// </summary>
    internal static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DOCINFOW
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr hPrinter, IntPtr pDefault);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int Level, ref DOCINFOW pDocInfo);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        /// <summary>
        /// Sends a raw byte array to the named Windows printer.
        /// The printer must have a "Generic / Text Only" or similar RAW-capable driver.
        /// For ESC/POS printers, use the XP-K200L driver or Generic/Text Only.
        /// </summary>
        public static bool SendBytesToPrinter(string printerName, byte[] bytes)
        {
            if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
                return false;

            var docInfo = new DOCINFOW
            {
                pDocName = "ESC/POS Print Job",
                pOutputFile = null,
                pDataType = "RAW"
            };

            try
            {
                if (!StartDocPrinter(hPrinter, 1, ref docInfo))
                    return false;

                try
                {
                    if (!StartPagePrinter(hPrinter))
                        return false;

                    try
                    {
                        IntPtr pBytes = Marshal.AllocCoTaskMem(bytes.Length);
                        try
                        {
                            Marshal.Copy(bytes, 0, pBytes, bytes.Length);
                            return WritePrinter(hPrinter, pBytes, bytes.Length, out _);
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pBytes);
                        }
                    }
                    finally
                    {
                        EndPagePrinter(hPrinter);
                    }
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
