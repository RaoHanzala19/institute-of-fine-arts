using SkiaSharp;

namespace InstituteOfFineArts.Services;

/// <summary>
/// Paints small procedural artworks so the demo database ships with real JPEG files to look at.
/// Three styles (landscape, abstract, geometric) are chosen from the seed value.
/// </summary>
public static class PaintingGenerator
{
    private static readonly string[][] Palettes =
    {
        new[] { "#1D2B53", "#7E2553", "#FF004D", "#FFA300", "#FFEC27" },
        new[] { "#0B3C49", "#2E86AB", "#F6F5AE", "#F5B841", "#D1495B" },
        new[] { "#2B2D42", "#8D99AE", "#EDF2F4", "#EF233C", "#D90429" },
        new[] { "#264653", "#2A9D8F", "#E9C46A", "#F4A261", "#E76F51" },
        new[] { "#22223B", "#4A4E69", "#9A8C98", "#C9ADA7", "#F2E9E4" },
        new[] { "#05668D", "#028090", "#00A896", "#02C39A", "#F0F3BD" },
        new[] { "#3D348B", "#7678ED", "#F7B801", "#F18701", "#F35B04" },
        new[] { "#014F86", "#2A6F97", "#61A5C2", "#A9D6E5", "#FFB703" },
    };

    public static SKBitmap Generate(int seed, int width = 1200, int height = 800)
    {
        var rnd = new Random(seed * 7919 + 17);
        var palette = Palettes[Math.Abs(seed) % Palettes.Length].Select(SKColor.Parse).ToArray();
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        switch (Math.Abs(seed) % 3)
        {
            case 0: Landscape(canvas, rnd, palette, width, height); break;
            case 1: Abstract(canvas, rnd, palette, width, height); break;
            default: Geometric(canvas, rnd, palette, width, height); break;
        }

        Grain(canvas, rnd, width, height);
        return bitmap;
    }

    private static void Landscape(SKCanvas c, Random r, SKColor[] p, int w, int h)
    {
        using (var sky = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, h * 0.7f), new[] { p[0], p[2].WithAlpha(255) }, SKShaderTileMode.Clamp) })
            c.DrawRect(0, 0, w, h, sky);

        // sun / moon with a soft halo
        var sunX = w * (0.25f + (float)r.NextDouble() * 0.5f);
        var sunY = h * (0.22f + (float)r.NextDouble() * 0.15f);
        using (var halo = new SKPaint { Color = p[4].WithAlpha(90), IsAntialias = true, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 60) })
            c.DrawCircle(sunX, sunY, 150, halo);
        using (var sun = new SKPaint { Color = p[4], IsAntialias = true })
            c.DrawCircle(sunX, sunY, 70, sun);

        // layered hills
        var layers = 5;
        for (var i = 0; i < layers; i++)
        {
            var baseY = h * (0.45f + i * 0.11f);
            var amp = 40 + r.Next(60);
            var freq = 0.004f + (float)r.NextDouble() * 0.006f;
            var phase = (float)r.NextDouble() * 6f;
            using var path = new SKPath();
            path.MoveTo(0, h);
            for (var x = 0; x <= w; x += 8)
                path.LineTo(x, baseY + (float)Math.Sin(x * freq + phase) * amp + (float)Math.Sin(x * freq * 2.3f + phase) * amp * 0.35f);
            path.LineTo(w, h);
            path.Close();
            var col = Blend(p[1], p[3], i / (float)(layers - 1));
            using var paint = new SKPaint { Color = col.WithAlpha((byte)(190 + i * 12)), IsAntialias = true };
            c.DrawPath(path, paint);
        }
    }

    private static void Abstract(SKCanvas c, Random r, SKColor[] p, int w, int h)
    {
        using (var bg = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(w, h), new[] { p[0], p[1] }, SKShaderTileMode.Clamp) })
            c.DrawRect(0, 0, w, h, bg);

        for (var i = 0; i < 16; i++)
        {
            var col = p[r.Next(p.Length)].WithAlpha((byte)r.Next(70, 200));
            using var paint = new SKPaint { Color = col, IsAntialias = true, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r.Next(2, 26)) };
            c.DrawOval(r.Next(w), r.Next(h), r.Next(50, 260), r.Next(40, 200), paint);
        }

        for (var i = 0; i < 12; i++)
        {
            using var path = new SKPath();
            path.MoveTo(r.Next(w), r.Next(h));
            path.CubicTo(r.Next(w), r.Next(h), r.Next(w), r.Next(h), r.Next(w), r.Next(h));
            using var stroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, IsAntialias = true,
                StrokeWidth = r.Next(6, 38), Color = p[r.Next(p.Length)].WithAlpha((byte)r.Next(120, 230))
            };
            c.DrawPath(path, stroke);
        }
    }

    private static void Geometric(SKCanvas c, Random r, SKColor[] p, int w, int h)
    {
        c.Clear(p[4]);
        var cols = 6;
        var rows = 4;
        var cw = w / (float)cols;
        var ch = h / (float)rows;
        for (var x = 0; x < cols; x++)
        for (var y = 0; y < rows; y++)
        {
            var left = x * cw;
            var top = y * ch;
            using var fill = new SKPaint { Color = p[r.Next(p.Length)], IsAntialias = true };
            switch (r.Next(4))
            {
                case 0: c.DrawRect(left, top, cw, ch, fill); break;
                case 1: c.DrawCircle(left + cw / 2, top + ch / 2, Math.Min(cw, ch) / 2, fill); break;
                case 2:
                    using (var tri = new SKPath())
                    {
                        tri.MoveTo(left, top + ch); tri.LineTo(left + cw, top + ch); tri.LineTo(left + cw / 2, top); tri.Close();
                        c.DrawPath(tri, fill);
                    }
                    break;
                default:
                    c.DrawRect(left, top, cw, ch, fill);
                    using (var inner = new SKPaint { Color = p[r.Next(p.Length)], IsAntialias = true })
                        c.DrawCircle(left + cw / 2, top + ch / 2, Math.Min(cw, ch) / 3.2f, inner);
                    break;
            }
        }
    }

    private static void Grain(SKCanvas c, Random r, int w, int h)
    {
        using var paint = new SKPaint { IsAntialias = false };
        for (var i = 0; i < 6000; i++)
        {
            paint.Color = (r.Next(2) == 0 ? SKColors.White : SKColors.Black).WithAlpha((byte)r.Next(6, 22));
            c.DrawPoint(r.Next(w), r.Next(h), paint);
        }
    }

    private static SKColor Blend(SKColor a, SKColor b, float t) => new(
        (byte)(a.Red + (b.Red - a.Red) * t),
        (byte)(a.Green + (b.Green - a.Green) * t),
        (byte)(a.Blue + (b.Blue - a.Blue) * t));
}
