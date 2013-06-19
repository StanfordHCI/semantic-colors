using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;

namespace Engine
{

    public class ColorMood
    {
        public double Activity;
        public double Weight;
        public double Heat;

        public ColorMood()
        {
            Activity = 0;
            Weight = 0;
            Heat = 0;
        }

        public ColorMood(double a, double w, double h)
        {
            Activity = a;
            Weight = w;
            Heat = h;
        }

        public double SqDist(ColorMood cm)
        {
            double adiff = this.Activity - cm.Activity;
            double wdiff = this.Weight - cm.Weight;
            double hdiff = this.Heat - cm.Heat;
            return Math.Pow(adiff, 2.0) + Math.Pow(wdiff, 2.0) + Math.Pow(hdiff, 2.0);
        }

        public static ColorMood operator +(ColorMood a, ColorMood b)
        {
            return new ColorMood(a.Activity + b.Activity, a.Weight + b.Weight, a.Heat + b.Heat);
        }

        public static ColorMood operator *(ColorMood a, ColorMood b)
        {
            return new ColorMood(a.Activity * b.Activity, a.Weight * b.Weight, a.Heat * b.Heat);
        }
        public static ColorMood operator -(ColorMood a, ColorMood b)
        {
            return new ColorMood(a.Activity - b.Activity, a.Weight - b.Weight, a.Heat - b.Heat);
        }

        public static ColorMood operator /(ColorMood a, double s)
        {
            return new ColorMood(a.Activity / s, a.Weight / s, a.Heat / s);
        }

        public static ColorMood operator *(ColorMood a, double s)
        {
            return new ColorMood(a.Activity * s, a.Weight * s, a.Heat * s);
        }


    }



    public class HSV
    {
        public double H;
        public double S;
        public double V;

        public HSV()
        {
            H = 0;
            S = 0;
            V = 0;
        }

        public HSV(double h, double s, double v)
        {
            H = h;
            S = s;
            V = v;
        }

        public static HSV operator +(HSV a, HSV b)
        {
            return new HSV(a.H + b.H, a.S + b.S, a.V + b.V);
        }

        public static HSV operator *(HSV a, HSV b)
        {
            return new HSV(a.H * b.H, a.S * b.S, a.V * b.V);
        }

        public static HSV operator /(HSV a, HSV b)
        {
            return new HSV(a.H / b.H, a.S / b.S, a.V / b.V);
        }

        public static HSV operator -(HSV a, HSV b)
        {
            return new HSV(a.H - b.H, a.S - b.S, a.V - b.V);
        }

        public static HSV operator /(HSV a, double s)
        {
            return new HSV(a.H / s, a.S / s, a.V / s);
        }

        public static HSV operator *(HSV a, double s)
        {
            return new HSV(a.H * s, a.S * s, a.V * s);
        }

    }

    public class CIELAB : IEquatable<CIELAB>
    {
        public double L;
        public double A;
        public double B;

        public CIELAB()
        {
            L = 0;
            A = 0;
            B = 0;
        }

        public CIELAB(double l, double a, double b)
        {
            L = l;
            A = a;
            B = b;
        }

        public double SqDist(CIELAB lab)
        {
            double ldiff = this.L - lab.L;
            double adiff = this.A - lab.A;
            double bdiff = this.B - lab.B;
            return Math.Pow(ldiff, 2.0) + Math.Pow(adiff, 2.0) + Math.Pow(bdiff, 2.0);
        }

        //ported from https://github.com/StanfordHCI/c3/blob/master/lib/d3/d3.color.js
        public double CIEDE2000Dist(CIELAB lab)
        {
            // adapted from Sharma et al's MATLAB implementation at
            //  http://www.ece.rochester.edu/~gsharma/ciede2000/

            CIELAB x = this;
            CIELAB y = lab;

            // parametric factors, use defaults
            double kl = 1, kc = 1, kh = 1;

            // compute terms
            double pi = Math.PI,
                L1 = x.L, a1 = x.A, b1 = x.B, Cab1 = Math.Sqrt(a1 * a1 + b1 * b1),
                L2 = y.L, a2 = y.A, b2 = y.B, Cab2 = Math.Sqrt(a2 * a2 + b2 * b2),
                Cab = 0.5 * (Cab1 + Cab2),
                G = 0.5 * (1 - Math.Sqrt(Math.Pow(Cab, 7) / (Math.Pow(Cab, 7) + Math.Pow(25, 7)))),
                ap1 = (1 + G) * a1,
                ap2 = (1 + G) * a2,
                Cp1 = Math.Sqrt(ap1 * ap1 + b1 * b1),
                Cp2 = Math.Sqrt(ap2 * ap2 + b2 * b2),
                Cpp = Cp1 * Cp2;

            // ensure hue is between 0 and 2pi
            double hp1 = Math.Atan2(b1, ap1); if (hp1 < 0) hp1 += 2 * pi;
            double hp2 = Math.Atan2(b2, ap2); if (hp2 < 0) hp2 += 2 * pi;

            double dL = L2 - L1,
                dC = Cp2 - Cp1,
                dhp = hp2 - hp1;

            if (dhp > +pi) dhp -= 2 * pi;
            if (dhp < -pi) dhp += 2 * pi;
            if (Cpp == 0) dhp = 0;

            // Note that the defining equations actually need
            // signed Hue and chroma differences which is different
            // from prior color difference formulae
            double dH = 2 * Math.Sqrt(Cpp) * Math.Sin(dhp / 2);

            // Weighting functions
            double Lp = 0.5 * (L1 + L2),
                Cp = 0.5 * (Cp1 + Cp2);

            // Average Hue Computation
            // This is equivalent to that in the paper but simpler programmatically.
            // Average hue is computed in radians and converted to degrees where needed
            double hp = 0.5 * (hp1 + hp2);
            // Identify positions for which abs hue diff exceeds 180 degrees 
            if (Math.Abs(hp1 - hp2) > pi) hp -= pi;
            if (hp < 0) hp += 2 * pi;

            // Check if one of the chroma values is zero, in which case set 
            // mean hue to the sum which is equivalent to other value
            if (Cpp == 0) hp = hp1 + hp2;

            double Lpm502 = (Lp - 50) * (Lp - 50),
                Sl = 1 + 0.015 * Lpm502 / Math.Sqrt(20 + Lpm502),
                Sc = 1 + 0.045 * Cp,
                T = 1 - 0.17 * Math.Cos(hp - pi / 6)
                      + 0.24 * Math.Cos(2 * hp)
                      + 0.32 * Math.Cos(3 * hp + pi / 30)
                      - 0.20 * Math.Cos(4 * hp - 63 * pi / 180),
                Sh = 1 + 0.015 * Cp * T,
                ex = (180 / pi * hp - 275) / 25,
                delthetarad = (30 * pi / 180) * Math.Exp(-1 * (ex * ex)),
                Rc = 2 * Math.Sqrt(Math.Pow(Cp, 7) / (Math.Pow(Cp, 7) + Math.Pow(25, 7))),
                RT = -1 * Math.Sin(2 * delthetarad) * Rc;

            dL = dL / (kl * Sl);
            dC = dC / (kc * Sc);
            dH = dH / (kh * Sh);

            // The CIE 00 color difference
            return Math.Sqrt(dL * dL + dC * dC + dH * dH + RT * dC * dH);

        }


        public bool Equals(CIELAB other)
        {
            return L == other.L && A == other.A && B == other.B;
        }


        public override int GetHashCode()
        {
            //convert to RGB and see
            //CIELAB lab = new CIELAB(L, A, B);
            //Color c = Util.LABtoRGB(lab);
            int hCode = (int)Math.Round(L) ^ (int)Math.Round(A) ^ (int)Math.Round(B);
            //int hCode = c.R ^ c.G ^ c.B;
            return hCode.GetHashCode();
        }

        public static CIELAB operator +(CIELAB a, CIELAB b)
        {
            return new CIELAB(a.L + b.L, a.A + b.A, a.B + b.B);
        }

        public static CIELAB operator *(CIELAB a, CIELAB b)
        {
            return new CIELAB(a.L * b.L, a.A * b.A, a.B * b.B);
        }

        public static CIELAB operator /(CIELAB a, CIELAB b)
        {
            return new CIELAB(a.L / b.L, a.A / b.A, a.B / b.B);
        }

        public static CIELAB operator -(CIELAB a, CIELAB b)
        {
            return new CIELAB(a.L - b.L, a.A - b.A, a.B - b.B);
        }

        public static CIELAB operator /(CIELAB a, double s)
        {
            return new CIELAB(a.L / s, a.A / s, a.B / s);
        }

        public static CIELAB operator *(CIELAB a, double s)
        {
            return new CIELAB(a.L * s, a.A * s, a.B * s);
        }

        public override string ToString()
        {
            //return base.ToString();
            return "("+ L + ", " + A + ", " + B + ")";
        }
    }

    public class Util
    {
        public static Color LABtoRGB(CIELAB lab)
        {
            double gamma = 2.2;
            double e = 216 / 24389.0;
            double k = 24389 / 27.0;

            double XR = 0.95047;
            double YR = 1.00000;
            double ZR = 1.08883;

            double fy = (lab.L + 16) / 116.0;
            double fx = lab.A / 500.0 + fy;
            double fz = fy - lab.B / 200.0;

            double[,] xyzTorgbMatrix = new double[3, 3] {{3.2404542, -1.5371385, -0.4985314},
                                                        {-0.9692660,  1.8760108,  0.0415560},
                                                        {0.0556434, -0.2040259,  1.0572252}};
            double xR = Math.Pow(fx, 3.0);
            double zR = Math.Pow(fz, 3.0);

            xR = (xR > e) ? xR : (116 * fx - 16) / k;
            double yR = (lab.L > k * e) ? Math.Pow((lab.L + 16) / 116.0, 3.0) : lab.L / k;
            zR = (zR > e) ? zR : (116 * fz - 16) / k;

            double x = xR * XR;
            double y = yR * YR;
            double z = zR * ZR;

            //xyz to rgb
            double r = xyzTorgbMatrix[0, 0] * x + xyzTorgbMatrix[0, 1] * y + xyzTorgbMatrix[0, 2] * z;
            double g = xyzTorgbMatrix[1, 0] * x + xyzTorgbMatrix[1, 1] * y + xyzTorgbMatrix[1, 2] * z;
            double b = xyzTorgbMatrix[2, 0] * x + xyzTorgbMatrix[2, 1] * y + xyzTorgbMatrix[2, 2] * z;

            int red = (int)Math.Round(255 * (Math.Pow(clamp(r), 1.0 / gamma)));
            int green = (int)Math.Round(255 * (Math.Pow(clamp(g), 1.0 / gamma)));
            int blue = (int)Math.Round(255 * (Math.Pow(clamp(b), 1.0 / gamma)));

            return Color.FromArgb(red, green, blue);
        }

        private static double clamp(double value)
        {
            return Math.Min(Math.Max(value, 0.0), 1.0);
        }


        public static CIELAB RGBtoLAB(Color rgb)
        {
            double gamma = 2.2;
            double red = Math.Pow(rgb.R / 255.0, gamma); //range from 0 to 1.0
            double green = Math.Pow(rgb.G / 255.0, gamma);
            double blue = Math.Pow(rgb.B / 255.0, gamma);


            //assume rgb is already linear
            //sRGB to xyz
            //http://www.brucelindbloom.com/
            double[,] rgbToxyzMatrix = new double[3, 3]{
                                            {0.4124564,  0.3575761,  0.1804375},
                                            {0.2126729,  0.7151522,  0.0721750},
                                            {0.0193339,  0.1191920,  0.9503041}};

            double x = rgbToxyzMatrix[0, 0] * red + rgbToxyzMatrix[0, 1] * green + rgbToxyzMatrix[0, 2] * blue;
            double y = rgbToxyzMatrix[1, 0] * red + rgbToxyzMatrix[1, 1] * green + rgbToxyzMatrix[1, 2] * blue;
            double z = rgbToxyzMatrix[2, 0] * red + rgbToxyzMatrix[2, 1] * green + rgbToxyzMatrix[2, 2] * blue;

            double XR = 0.95047;
            double YR = 1.00000;
            double ZR = 1.08883;

            double e = 216 / 24389.0;
            double k = 24389 / 27.0;

            double xR = x / XR;
            double yR = y / YR;
            double zR = z / ZR;

            double fx = (xR > e) ? Math.Pow(xR, 1.0 / 3.0) : (k * xR + 16) / 116.0;
            double fy = (yR > e) ? Math.Pow(yR, 1.0 / 3.0) : (k * yR + 16) / 116.0;
            double fz = (zR > e) ? Math.Pow(zR, 1.0 / 3.0) : (k * zR + 16) / 116.0;

            double cieL = 116 * fy - 16;
            double cieA = 500 * (fx - fy);
            double cieB = 200 * (fy - fz);

            return new CIELAB(cieL, cieA, cieB);

        }

        //Resize the bitmap using nearest neighbors
        public static Bitmap ResizeBitmapNearest(Bitmap b, int nWidth, int nHeight)
        {
            Bitmap result = new Bitmap(nWidth, nHeight);
            using (Graphics g = Graphics.FromImage((Image)result))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.SmoothingMode = SmoothingMode.None;
                g.DrawImage(b, 0, 0, nWidth, nHeight);
            }
            return result;
        }

        public static String ConvertFileName(String basename, String label, String toExt = null)
        {
            FileInfo info = new FileInfo(basename);
            String extension = info.Extension;
            String result = (toExt == null) ? basename.Replace(extension, label + extension) : basename.Replace(extension, label + toExt);
            return result;
        }

        //from: http://stackoverflow.com/questions/1335426/is-there-a-built-in-c-net-system-api-for-hsv-to-rgb
        public static HSV RGBtoHSV(Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));
            double chroma = max - min;

            double huep = 0;
            if (chroma == 0)
                huep = 0;
            else if (max == color.R)
                huep = (color.G - color.B) / chroma % 6;
            else if (max == color.G)
                huep = (color.B - color.R) / chroma + 2;
            else
                huep = (color.R - color.G) / chroma + 4;

            double hue = 60 * huep;//color.GetHue();

            if (hue < 0)
                hue += 360;

           /* double alpha = 0.5 * (2 * color.R - color.G - color.B);
            double beta = Math.Sqrt(3) / 2.0 * (color.G - color.B);
            double hue = Math.Atan2(beta, alpha) * 180 / Math.PI;
            if (hue < 0)
                hue += 360;*/

            //double hue = color.GetHue();

            double saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            double value = max / 255d;


            return new HSV(hue, saturation, value);
        }

        public static Color HSVtoRGB(HSV hsv)
        {
            double hue = hsv.H;
            double value = hsv.V;
            double saturation = hsv.S;

            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        public static ColorMood LABtoColorMood(CIELAB lab)
        {
            //hue angle
            double h = Math.Atan2(lab.B, lab.A);// +Math.PI;
            double radians1 = 100 * (Math.PI / 180.0);
            double radians2 = 50 * (Math.PI / 180.0);
            double chroma = Math.Sqrt(lab.A * lab.A + lab.B * lab.B);

            double activity = -2.1 + 0.06 * Math.Sqrt(Math.Pow(lab.L - 50, 2) + Math.Pow(lab.A - 3, 2) + Math.Pow((lab.B - 17) / 1.4, 2));
            double weight = -1.8 + 0.04 * (100 - lab.L) + 0.45 * Math.Cos(h - radians1);
            double heat = -0.5 + 0.02 * Math.Pow(chroma, 1.07) * Math.Cos(h - radians2);

            return new ColorMood(activity, weight, heat);
        }

        public static Color[,] BitmapToArray(Bitmap image)
        {
            /*Color[,] result = new Color[image.Width, image.Height];
            for (int j = 0; j < image.Height; j++)
            {
                for (int i = 0; i < image.Width; i++)
                {
                    result[i,j] = image.GetPixel(i, j);
                }
            }
            return result;*/

            BitmapData lockData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Int32[] imageData = new Int32[image.Width * image.Height];

            System.Runtime.InteropServices.Marshal.Copy(lockData.Scan0, imageData, 0, imageData.Length);
            image.UnlockBits(lockData);

            Color[,] result = new Color[image.Width, image.Height];
            for (int i = 0; i < image.Width; i++)
                for (int j = 0; j < image.Height; j++)
                    result[i, j] = Color.FromArgb(imageData[j * image.Width + i]);

            return result;

        }

        public static Bitmap ArrayToBitmap(Color[,] image)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);
            Bitmap result = new Bitmap(width, height);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    result.SetPixel(i, j, image[i, j]);
                }
            }

            return result;
        }

        //2D array map function
        public static TDest[,] Map<TSource, TDest>(TSource[,] source, Func<TSource, TDest> func)
        {
            int width = source.GetLength(0);
            int height = source.GetLength(1);

            TDest[,] result = new TDest[width, height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    result[i, j] = func(source[i, j]);
                }
            }

            return result;
        }

        //Shuffle function
        public static void Shuffle<T>(List<T> shuffled, Random random=null)
        {
            if (random == null)
                random = new Random();

            for (int j = shuffled.Count() - 1; j >= 0; j--)
            {
                int idx = random.Next(j + 1);
                T temp = shuffled[j];
                shuffled[j] = shuffled[idx];
                shuffled[idx] = temp;
            }
        }


        public static bool InBounds(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        /*
         * A function to get a Bitmap object directly from a web resource
         * Author: Danny Battison
         * Contact: gabehabe@googlemail.com
         * http://www.dreamincode.net/code/snippet2555.htm
         */

        /// <summary>
        /// Get a bitmap directly from the web
        /// </summary>
        /// <param name="URL">The URL of the image</param>
        /// <returns>A bitmap of the image requested</returns>
        public static Bitmap BitmapFromWeb(string URL)
        {
            try
            {
                // create a web request to the url of the image
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(URL);
                // set the method to GET to get the image
                myRequest.Method = "GET";
                // get the response from the webpage
                HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
                // create a bitmap from the stream of the response
                Bitmap bmp = new Bitmap(myResponse.GetResponseStream());
                // close off the stream and the response
                myResponse.Close();
                // return the Bitmap of the image
                return bmp;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not download image!");
                return null; // if for some reason we couldn't get to image, we return null
            }
        }



    }
}
