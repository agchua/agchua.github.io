// Generates og.png (link-preview image) and the PNG icons from the same phase-field model as the site.
// Run from the repo root in Windows PowerShell:
//   Add-Type -TypeDefinition (Get-Content -Raw tools\og-image.cs) -ReferencedAssemblies System.Drawing
//   [OG]::Make("$PWD\og.png"); [OG]::Icon("$PWD\favicon-32.png", 32, $false); [OG]::Icon("$PWD\apple-touch-icon.png", 180, $true)

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

// Same Kobayashi phase-field model and colouring as the site, rendered offline for the link-preview image.
public static class OG
{
    const int NG = 176;
    const double DX = 0.03, DT = 1e-4, TAU = 3e-4, EB = 0.01, DELTA = 0.04, K = 1.6;
    const double ALPHA = 0.9, GAMMA = 10, TEQ = 1, NOISE = 0.03, RING = 0.02;
    static Random rng = new Random(11);

    static void Grow(double th, float[] p, float[] at)
    {
        int N = NG * NG;
        float[] T = new float[N], Tn = new float[N], ax = new float[N], ay = new float[N], e2 = new float[N], dp = new float[N];
        for (int i = 0; i < N; i++) { p[i] = 0; at[i] = -1; }
        double co = Math.Cos(6 * th), so = Math.Sin(6 * th);
        int m = NG / 2;
        for (int y = m - 3; y <= m + 3; y++) for (int x = m - 3; x <= m + 3; x++)
            if ((x - m) * (x - m) + (y - m) * (y - m) <= 9) { p[y * NG + x] = 1; at[y * NG + x] = 0; }
        double i2dx = 1 / (2 * DX), idx2 = 1 / (DX * DX), time = 0;
        for (int step = 0; step < 12000; step++)
        {
            for (int y = 1; y < NG - 1; y++) for (int x = 1; x < NG - 1; x++)
            {
                int i = y * NG + x;
                double gx = (p[i + 1] - p[i - 1]) * i2dx, gy = (p[i + NG] - p[i - NG]) * i2dx, r2 = gx * gx + gy * gy;
                if (r2 < 1e-10) { ax[i] = 0; ay[i] = 0; e2[i] = (float)(EB * EB); continue; }
                double ar = gx * gx - gy * gy, ai = 2 * gx * gy, br = ar * gx - ai * gy, bi = ar * gy + ai * gx;
                double r6 = r2 * r2 * r2, cc = (br * br - bi * bi) / r6, ss = (2 * br * bi) / r6;
                double c6 = cc * co + ss * so, s6 = ss * co - cc * so;
                double eps = EB * (1 + DELTA * c6), epsd = -EB * 6 * DELTA * s6;
                ax[i] = (float)(eps * epsd * gx); ay[i] = (float)(-eps * epsd * gy); e2[i] = (float)(eps * eps);
            }
            for (int y = 2; y < NG - 2; y++) for (int x = 2; x < NG - 2; x++)
            {
                int i = y * NG + x; double pi = p[i], ti = T[i];
                double lapP = (p[i - 1] + p[i + 1] + p[i - NG] + p[i + NG] - 4 * pi) * idx2;
                double lapT = (T[i - 1] + T[i + 1] + T[i - NG] + T[i + NG] - 4 * ti) * idx2;
                double mm = ALPHA / Math.PI * Math.Atan(GAMMA * (TEQ - ti));
                double reac = pi * (1 - pi) * (pi - 0.5 + mm + NOISE * (rng.NextDouble() - 0.5));
                double v = ((ay[i + 1] - ay[i - 1]) * i2dx + (ax[i + NG] - ax[i - NG]) * i2dx + e2[i] * lapP + reac) / TAU;
                dp[i] = (float)v;
                Tn[i] = (float)(ti + DT * lapT + K * DT * v);
            }
            time += DT;
            bool done = false;
            for (int y = 2; y < NG - 2; y++) for (int x = 2; x < NG - 2; x++)
            {
                int i = y * NG + x; float v = (float)(p[i] + DT * dp[i]); p[i] = v;
                if (v > 0.5f)
                {
                    if (at[i] < 0) at[i] = (float)time;
                    if (x <= 6 || y <= 6 || x >= NG - 7 || y >= NG - 7) done = true;
                }
            }
            float[] tmp = T; T = Tn; Tn = tmp;
            if (done) break;
        }
    }

    static float Lerp(float[] f, int a, int b, int ra, int rb, double wx, double wy)
    {
        double t = f[ra + a] + (f[ra + b] - f[ra + a]) * wx, u = f[rb + a] + (f[rb + b] - f[rb + a]) * wx;
        return (float)(t + (u - t) * wy);
    }

    // composite one grain (same outline / fill / ring colouring as the site) onto an RGB float buffer
    static void Paint(float[] buf, int W, int H, float[] p, float[] at, double cx, double cy, double S)
    {
        double[] BLUE = { 72, 112, 190 }, FILL = { 250, 251, 253 };
        double half = NG * S / 2, left = cx - half, top = cy - half;
        int x0 = Math.Max(0, (int)left), x1 = Math.Min(W - 1, (int)(left + 2 * half));
        int y0 = Math.Max(0, (int)top), y1 = Math.Min(H - 1, (int)(top + 2 * half));
        for (int py = y0; py <= y1; py++)
        {
            double sy = (py + 0.5 - top) / S - 0.5; int ya = (int)Math.Floor(sy);
            if (ya < 0 || ya >= NG - 1) continue;
            double wy = sy - ya; int ra = ya * NG, rb = (ya + 1) * NG;
            for (int px = x0; px <= x1; px++)
            {
                double sx = (px + 0.5 - left) / S - 0.5; int a = (int)Math.Floor(sx);
                if (a < 0 || a >= NG - 1) continue;
                double wx = sx - a; int b = a + 1;
                double ph = Math.Min(1, Math.Max(0, Lerp(p, a, b, ra, rb, wx, wy)));
                if (ph < 0.02) continue;
                double o = 4 * ph * (1 - ph), o2 = o * o, o4 = o2 * o2, edge = o4 * o4, ring = 0;
                if (ph > 0.6 && at[ra + a] >= 0 && at[ra + b] >= 0 && at[rb + a] >= 0 && at[rb + b] >= 0)
                {
                    double tv = Lerp(at, a, b, ra, rb, wx, wy);
                    if (tv > RING * 0.5) { double f = (tv / RING) % 1, dd = Math.Min(f, 1 - f); ring = Math.Exp(-(dd * dd) / 0.0018); }
                }
                double mix = Math.Min(1, 0.6 * edge + 0.16 * ring);
                double alpha = Math.Min(1, Math.Max(edge, ph * 2));
                int k = (py * W + px) * 3;
                for (int ch = 0; ch < 3; ch++)
                {
                    double col = FILL[ch] + (BLUE[ch] - FILL[ch]) * mix;
                    buf[k + ch] = (float)(buf[k + ch] * (1 - alpha) + col * alpha);
                }
            }
        }
    }

    public static void Make(string ogPath)
    {
        int W = 1200, H = 630;
        float[] buf = new float[W * H * 3];
        for (int i = 0; i < buf.Length; i++) buf[i] = 255;
        // grains around the edges, layered like the site (later ones on top)
        double[][] spots = {
            new double[] { 150, 130, 2.5 }, new double[] { 1060, 120, 2.4 }, new double[] { 600, 20, 2.2 },
            new double[] { 170, 560, 2.6 }, new double[] { 1040, 540, 2.5 }, new double[] { 610, 640, 2.3 },
            new double[] { 360, 330, 2.1 }, new double[] { 860, 300, 2.2 }
        };
        float[] p = new float[NG * NG], at = new float[NG * NG];
        foreach (double[] s in spots)
        {
            Grow(rng.NextDouble() * Math.PI / 3, p, at);
            Paint(buf, W, H, p, at, s[0], s[1], s[2]);
        }
        using (Bitmap bmp = new Bitmap(W, H, PixelFormat.Format24bppRgb))
        {
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            byte[] row = new byte[bd.Stride];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int k = (y * W + x) * 3;
                    row[x * 3] = (byte)Math.Round(buf[k + 2]); row[x * 3 + 1] = (byte)Math.Round(buf[k + 1]); row[x * 3 + 2] = (byte)Math.Round(buf[k]);
                }
                Marshal.Copy(row, 0, bd.Scan0 + y * bd.Stride, bd.Stride);
            }
            bmp.UnlockBits(bd);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                Rectangle box = new Rectangle(300, 200, 600, 230);
                g.FillRectangle(Brushes.White, box);
                using (Pen border = new Pen(Color.FromArgb(212, 212, 212), 2)) g.DrawRectangle(border, box);
                using (Font name = new Font("Consolas", 34, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Font body = new Font("Consolas", 21, FontStyle.Regular, GraphicsUnit.Pixel))
                using (SolidBrush ink = new SolidBrush(Color.FromArgb(17, 17, 17)))
                using (SolidBrush mute = new SolidBrush(Color.FromArgb(107, 107, 107)))
                using (Pen rule = new Pen(Color.FromArgb(212, 212, 212), 2))
                {
                    g.DrawString("Aaron Gabriel Chua", name, ink, 336, 236);
                    g.DrawLine(rule, 336, 292, 864, 292);
                    g.DrawString("Materials science + OIDD, Penn M&T", body, ink, 336, 314);
                    g.DrawString("agchua.github.io", body, mute, 336, 368);
                }
            }
            bmp.Save(ogPath, ImageFormat.Png);
        }
    }

    // six-armed snowflake icon, matching favicon.svg
    public static void Icon(string path, int size, bool background)
    {
        using (Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(background ? Color.White : Color.Transparent);
            double c = size / 2.0, u = size / 64.0 * (background ? 0.8 : 1.0);
            using (Pen pen = new Pen(Color.FromArgb(72, 112, 190), (float)(4 * u)))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                for (int k = 0; k < 6; k++)
                {
                    double a = k * Math.PI / 3;
                    Func<double, double, PointF> P = (x, y) => new PointF(
                        (float)(c + (x * Math.Cos(a) - y * Math.Sin(a)) * u),
                        (float)(c + (x * Math.Sin(a) + y * Math.Cos(a)) * u));
                    g.DrawLine(pen, P(0, 0), P(0, -27));
                    g.DrawLine(pen, P(0, -14), P(-8, -21));
                    g.DrawLine(pen, P(0, -14), P(8, -21));
                }
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
