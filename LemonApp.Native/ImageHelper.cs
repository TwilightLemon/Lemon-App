using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace LemonApp.Native;

public static class ImageHelper
{
    #region 处理模糊图像
    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipBitmapApplyEffect(IntPtr bitmap, IntPtr effect, ref Rectangle rectOfInterest, bool useAuxData, IntPtr auxData, int auxDataSize);
    /// <summary>
    /// 获取对象的私有字段的值
    /// </summary>
    /// <typeparam name="TResult">字段的类型</typeparam>
    /// <param name="obj">要从其中获取字段值的对象</param>
    /// <param name="fieldName">字段的名称.</param>
    /// <returns>字段的值</returns>
    /// <exception cref="System.InvalidOperationException">无法找到该字段.</exception>
    /// 
    internal static TResult GetPrivateField<TResult>(this object obj, string fieldName)
    {
        if (obj == null) return default(TResult);
        Type ltType = obj.GetType();
        FieldInfo lfiFieldInfo = ltType.GetField(fieldName, BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
        if (lfiFieldInfo != null)
            return (TResult)lfiFieldInfo.GetValue(obj);
        else
            throw new InvalidOperationException(string.Format("Instance field '{0}' could not be located in object of type '{1}'.", fieldName, obj.GetType().FullName));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BlurParameters
    {
        internal float Radius;
        internal bool ExpandEdges;
    }
    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipCreateEffect(Guid guid, out IntPtr effect);
    private static Guid BlurEffectGuid = new Guid("{633C80A4-1843-482B-9EF2-BE2834C5FDD4}");
    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipSetEffectParameters(IntPtr effect, IntPtr parameters, uint size);
    public static IntPtr NativeHandle(this Bitmap Bmp)
    {
        // 通过反射获取Bitmap的私有字段nativeImage的值，该值为GDI+的内部图像句柄
        //新版Drawing的Nuget包中字段由 nativeImage变更为_nativeImage
        return Bmp.GetPrivateField<IntPtr>("_nativeImage");
    }
    [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GdipDeleteEffect(IntPtr effect);
    public static void GaussianBlur(this Bitmap Bmp, ref Rectangle Rect, float Radius = 10, bool ExpandEdge = false)
    {
        int Result;
        IntPtr BlurEffect;
        BlurParameters BlurPara;
        if ((Radius < 0) || (Radius > 255))
        {
            throw new ArgumentOutOfRangeException("半径必须在[0,255]范围内");
        }
        BlurPara.Radius = Radius;
        BlurPara.ExpandEdges = ExpandEdge;
        Result = GdipCreateEffect(BlurEffectGuid, out BlurEffect);
        if (Result == 0)
        {
            IntPtr Handle = Marshal.AllocHGlobal(Marshal.SizeOf(BlurPara));
            Marshal.StructureToPtr(BlurPara, Handle, true);
            GdipSetEffectParameters(BlurEffect, Handle, (uint)Marshal.SizeOf(BlurPara));
            GdipBitmapApplyEffect(Bmp.NativeHandle(), BlurEffect, ref Rect, false, IntPtr.Zero, 0);
            // 使用GdipBitmapCreateApplyEffect函数可以不改变原始的图像，而把模糊的结果写入到一个新的图像中
            GdipDeleteEffect(BlurEffect);
            Marshal.FreeHGlobal(Handle);
        }
        else
        {
            throw new ExternalException("不支持的GDI+版本，必须为GDI+1.1及以上版本，且操作系统要求为Win Vista及之后版本.");
        }
    }
    #endregion

    public static BitmapImage ToBitmapImage(this Bitmap Bmp)
    {
        BitmapImage BmpImage = new();
        using (MemoryStream lmemStream = new())
        {
            Bmp.Save(lmemStream, ImageFormat.Png);
            BmpImage.BeginInit();
            BmpImage.StreamSource = new MemoryStream(lmemStream.ToArray());
            BmpImage.EndInit();
        }
        return BmpImage;
    }

    public static Bitmap ToBitmap(this BitmapImage img){
        using (MemoryStream outStream = new())
        {
            BitmapEncoder enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(img));
            enc.Save(outStream);
            return new Bitmap(outStream);
        }
    }

    public static void AddMask(this Bitmap bitmap,bool darkmode)
    {
        var color1 = darkmode ? Color.FromArgb(210, 0, 0, 0) : Color.FromArgb(230, 255, 255, 255);
        var color2 = darkmode ? Color.FromArgb(180, 0, 0, 0) : Color.FromArgb(200, 255, 255, 255);
        using Graphics g = Graphics.FromImage(bitmap);
        using LinearGradientBrush brush = new(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            color1,
            color2,
            LinearGradientMode.Vertical);
        g.FillRectangle(brush, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
    }
    public static void AdjustContrast(this Bitmap bitmap, float contrast)
    {
        contrast = (100.0f + contrast) / 100.0f;
        contrast *= contrast;

        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadWrite, bitmap.PixelFormat);

        int width = bitmap.Width;
        int height = bitmap.Height;

        unsafe
        {
            for (int y = 0; y < height; y++)
            {
                byte* row = (byte*)data.Scan0 + (y * data.Stride);
                for (int x = 0; x < width; x++)
                {
                    int idx = x * 4;

                    float blue = row[idx] / 255.0f;
                    float green = row[idx + 1] / 255.0f;
                    float red = row[idx + 2] / 255.0f;

                    // 转换为HSL
                    RgbToHsl(red, green, blue, out float h, out float s, out float l);

                    // 调整亮度以增加对比度
                    l = (((l - 0.5f) * contrast) + 0.5f);

                    // 转换回RGB
                    HslToRgb(h, s, l, out red, out green, out blue);

                    row[idx] = (byte)Math.Max(0, Math.Min(255, blue * 255.0f));
                    row[idx + 1] = (byte)Math.Max(0, Math.Min(255, green * 255.0f));
                    row[idx + 2] = (byte)Math.Max(0, Math.Min(255, red * 255.0f));
                }
            }
        }

        bitmap.UnlockBits(data);
    }

    private static void RgbToHsl(float r, float g, float b, out float h, out float s, out float l)
    {
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        h = s = l = (max + min) / 2.0f;

        if (max == min)
        {
            h = s = 0.0f; // achromatic
        }
        else
        {
            float d = max - min;
            s = l > 0.5f ? d / (2.0f - max - min) : d / (max + min);
            if (max == r)
            {
                h = (g - b) / d + (g < b ? 6.0f : 0.0f);
            }
            else if (max == g)
            {
                h = (b - r) / d + 2.0f;
            }
            else
            {
                h = (r - g) / d + 4.0f;
            }
            h /= 6.0f;
        }
    }

    private static void HslToRgb(float h, float s, float l, out float r, out float g, out float b)
    {
        if (s == 0.0f)
        {
            r = g = b = l; // achromatic
        }
        else
        {
            Func<float, float, float, float> hue2rgb = (p, q, t) =>
            {
                if (t < 0.0f) t += 1.0f;
                if (t > 1.0f) t -= 1.0f;
                if (t < 1.0f / 6.0f) return p + (q - p) * 6.0f * t;
                if (t < 1.0f / 3.0f) return q;
                if (t < 1.0f / 2.0f) return p + (q - p) * (2.0f / 3.0f - t) * 6.0f;
                return p;
            };

            float q = l < 0.5f ? l * (1.0f + s) : l + s - l * s;
            float p = 2.0f * l - q;
            r = hue2rgb(p, q, h + 1.0f / 3.0f);
            g = hue2rgb(p, q, h);
            b = hue2rgb(p, q, h - 1.0f / 3.0f);
        }
    }

    public static void ApplyMicaEffect(this Bitmap bitmap,bool isDarkmode)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        bitmap.AdjustContrast(isDarkmode?5:-20);
        bitmap.AddMask(isDarkmode);
        bitmap.GaussianBlur(ref rect, 160f, false);
    }
}
