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
    public static System.Windows.Media.Color GetMajorColor(this Bitmap bitmap)
    {
        int threshold = 36;
        //色调的总和
        var sum_hue = 0d;
        //色差的阈值
        //计算色调总和
        for (int h = 0; h < bitmap.Height; h++)
        {
            for (int w = 0; w < bitmap.Width; w++)
            {
                var hue = bitmap.GetPixel(w, h).GetHue();
                sum_hue += hue;
            }
        }
        var avg_hue = sum_hue / (bitmap.Width * bitmap.Height);
        if (avg_hue < 190) threshold = 5;
        //色差大于阈值的颜色值
        var rgbs = new List<Color>();
        for (int h = 0; h < bitmap.Height; h++)
        {
            for (int w = 0; w < bitmap.Width; w++)
            {
                var color = bitmap.GetPixel(w, h);
                var hue = color.GetHue();
                //如果色差大于阈值，则加入列表
                if (Math.Abs(hue - avg_hue) > threshold)
                {
                    rgbs.Add(color);
                }
            }
        }
        if (rgbs.Count == 0)
            return System.Windows.Media.Colors.Black;
        //计算列表中的颜色均值，结果即为该图片的主色调
        int sum_r = 0, sum_g = 0, sum_b = 0;
        foreach (var rgb in rgbs)
        {
            sum_r += rgb.R;
            sum_g += rgb.G;
            sum_b += rgb.B;
        }
        return System.Windows.Media.Color.FromRgb((byte)(sum_r / rgbs.Count),
            (byte)(sum_g / rgbs.Count),
            (byte)(sum_b / rgbs.Count));
    }
    public static System.Windows.Media.Color AdjustColor(this System.Windows.Media.Color col)
    {
        int high = 230;
        int dark = 120;
        if (col.R >= high && col.G >= high && col.B >= high)
        {
            col.R = (byte)(col.R * 0.6);
            col.G = (byte)(col.G * 0.6);
            col.B = (byte)(col.B * 0.6);
        }
        else if ((col.R + col.G + col.B) / 3 < dark)
        {
            col.R = (byte)(col.R * 1.8);
            col.G = (byte)(col.G * 1.8);
            col.B = (byte)(col.B * 1.8);
        }
        if (col.R >= high && col.G >= high && col.B >= high)
        {
            col.R -= 90; col.G -= 90; col.B -= 90;
        }
        else if ((col.R + col.G + col.B) / 3 < dark)
        {
            col.R += 80; col.G += 80; col.B += 80;
        }
        return col;
    }
    public static System.Windows.Media.Color ApplyColorMode(this System.Windows.Media.Color color,bool isDarkMode)
    {
        RgbToHsl(color.R/255f,color.G/255f, color.B/255f,out float h, out float s, out float l);
        if (isDarkMode)
            l = Math.Max(0.05f, l - 0.1f);
        else
            l = Math.Min(0.95f, l + 0.1f);

        HslToRgb(h, s, l, out float r, out float g, out float b);
        static void adjust(ref float a) => a = (a > 1) ? 1 : a;
        adjust(ref r);adjust(ref g);adjust(ref b);
        return System.Windows.Media.Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

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
        var color1 = darkmode ? Color.FromArgb(150, 0, 0, 0) : Color.FromArgb(160, 255, 255, 255);
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
                    int idx = x * 3;

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

    public static void ScaleImage(this Bitmap bitmap, double scale)
    {
        // 计算新的尺寸
        int newWidth = (int)(bitmap.Width * scale);
        int newHeight = (int)(bitmap.Height * scale);

        // 创建目标位图
        Bitmap newBitmap = new Bitmap(newWidth, newHeight, bitmap.PixelFormat);

        // 设置高质量绘图参数
        using (Graphics graphics = Graphics.FromImage(newBitmap))
        {
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 绘制缩放后的图像
            graphics.DrawImage(bitmap,
                new Rectangle(0, 0, newWidth, newHeight),
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                GraphicsUnit.Pixel);
        }
        bitmap = newBitmap;
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
        bitmap.AdjustContrast(isDarkmode?-1:-20);
        bitmap.AddMask(isDarkmode);
        bitmap.ScaleImage(2);
        bitmap.GaussianBlur(ref rect, 80f, false);
    }
}
