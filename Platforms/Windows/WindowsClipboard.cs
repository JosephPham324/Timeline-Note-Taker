using System.Runtime.InteropServices;

namespace Timeline_Note_Taker.Platforms.Windows;

public static class WindowsClipboard
{
    // Clipboard formats
    private const uint CF_DIB = 8;
    private const uint CF_DIBV5 = 17;
    
    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    
    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint uFormat);
    
    [DllImport("user32.dll")]
    private static extern bool IsClipboardFormatAvailable(uint format);
    
    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);
    
    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);
    
    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalSize(IntPtr hMem);
    
    public static bool HasImage()
    {
        return IsClipboardFormatAvailable(CF_DIB) || IsClipboardFormatAvailable(CF_DIBV5);
    }
    
    public static async Task<string?> SaveClipboardImageAsync(string directory)
    {
        System.Diagnostics.Debug.WriteLine($"[WindowsClipboard] SaveClipboardImageAsync called, hasImage={HasImage()}");
        
        if (!HasImage())
            return null;
            
        if (!OpenClipboard(IntPtr.Zero))
        {
            System.Diagnostics.Debug.WriteLine("[WindowsClipboard] Failed to open clipboard");
            return null;
        }
            
        try
        {
            IntPtr hDib = GetClipboardData(CF_DIB);
            if (hDib == IntPtr.Zero)
            {
                hDib = GetClipboardData(CF_DIBV5);
            }
            
            if (hDib == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("[WindowsClipboard] No DIB data found");
                return null;
            }
            
            IntPtr pDib = GlobalLock(hDib);
            if (pDib == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("[WindowsClipboard] Failed to lock global memory");
                return null;
            }
            
            try
            {
                // Create a bitmap from the DIB data
                var bitmap = DIBToBitmap(pDib);
                if (bitmap != null)
                {
                    var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    Directory.CreateDirectory(directory);
                    var filePath = Path.Combine(directory, fileName);
                    
                    System.Diagnostics.Debug.WriteLine($"[WindowsClipboard] Saving to: {filePath}");
                    bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                    bitmap.Dispose();
                    
                    System.Diagnostics.Debug.WriteLine($"[WindowsClipboard] Image saved successfully!");
                    return filePath;
                }
            }
            finally
            {
                GlobalUnlock(hDib);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowsClipboard] Error: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            CloseClipboard();
        }
        
        return null;
    }
    
    private static System.Drawing.Bitmap? DIBToBitmap(IntPtr pDib)
    {
        try
        {
            // Read BITMAPINFOHEADER
            var bmiHeader = Marshal.PtrToStructure<BITMAPINFOHEADER>(pDib);
            
            System.Diagnostics.Debug.WriteLine($"[WindowsClipboard] DIB Info: {bmiHeader.biWidth}x{bmiHeader.biHeight}, {bmiHeader.biBitCount} bits, Compression: {bmiHeader.biCompression}");
            
            // We only support standard RGB/BGR formats for now
            if (bmiHeader.biCompression != 0 && bmiHeader.biCompression != 3) // BI_RGB or BI_BITFIELDS
            {
                System.Diagnostics.Debug.WriteLine($"[WindowsClipboard] Unsupported compression: {bmiHeader.biCompression}");
                return null;
            }

            int headerSize = (int)bmiHeader.biSize;
            
            // Calculate pixel data offset
            // For standard headers, it's just the header size. 
            // If bitfields are used (BI_BITFIELDS), there are 3 DWORD masks after the header.
            // If there's a color table (<= 8 bpp), we need to skip it.
            
            int offsetToPixels = headerSize;
            
            if (bmiHeader.biBitCount <= 8)
            {
                int colors = (int)(bmiHeader.biClrUsed > 0 ? bmiHeader.biClrUsed : (1u << bmiHeader.biBitCount));
                offsetToPixels += colors * 4;
            }
            else if (bmiHeader.biCompression == 3) // BI_BITFIELDS
            {
                offsetToPixels += 12; // 3 DWORD masks
            }
            
            IntPtr pPixels = IntPtr.Add(pDib, offsetToPixels);
            
            // Calculate stride (must be multiple of 4 bytes)
            int stride = ((bmiHeader.biWidth * bmiHeader.biBitCount + 31) / 32) * 4;
            
            // Determine PixelFormat
            System.Drawing.Imaging.PixelFormat format;
            switch (bmiHeader.biBitCount)
            {
                case 32: format = System.Drawing.Imaging.PixelFormat.Format32bppRgb; break;
                case 24: format = System.Drawing.Imaging.PixelFormat.Format24bppRgb; break;
                case 8: format = System.Drawing.Imaging.PixelFormat.Format8bppIndexed; break;
                default: 
                    System.Diagnostics.Debug.WriteLine($"[WindowsClipboard] Unsupported bit count: {bmiHeader.biBitCount}");
                    return null;
            }
            
            System.Diagnostics.Debug.WriteLine($"[WindowsClipboard] Creating Bitmap: {bmiHeader.biWidth}x{bmiHeader.biHeight}, Stride: {stride}, Offset: {offsetToPixels}");
            
            // CREATE BITMAP FROM POINTER
            // Note: GDI+ Bitmaps are top-down, but DIBs are usually bottom-up (height > 0).
            // We'll create it and then flip if necessary.
            var bitmap = new System.Drawing.Bitmap(bmiHeader.biWidth, Math.Abs(bmiHeader.biHeight), stride, format, pPixels);
            
            // If height is positive in DIB, it's bottom-up, so we need to flip Y
            if (bmiHeader.biHeight > 0)
            {
                bitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
            }
            
            // Clone to detach from the memory pointer (which will be unlocked shortly)
            var clonedBitmap = new System.Drawing.Bitmap(bitmap);
            bitmap.Dispose();
            
            return clonedBitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowsClipboard] DIBToBitmap error: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }
}
