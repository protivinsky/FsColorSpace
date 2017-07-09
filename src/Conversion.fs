namespace FsColorSpace

open System
open System.Drawing
open System.Text.RegularExpressions

module Conversion = 

    (*

    CIE LCH color space (L = luminance, C = chroma, H = hue) is the best for getting a variety of palettes 
    for data visualisation, as you can for instance keep luminance and chroma constant and pick several hues
    across the whole spectrum.
    
    The transformation from CIE LCH to .NET Color consists of several steps:

    1. CIE LCH <-> CIE LUV (LCH is just a polar representation of LUV color space)
    2. CIE LUV <-> XYZ 
    3. XYZ <-> linear RGB
    4. linear RGB <-> sRGB (gamma correction)
    5. sRGB <-> .NET Color

    Common R packages for data visualisation (colorspace, ggplot2) uses the same transformation,
    therefore this procedure will provide almost identical color palettes.

    The inverse transformations are not needed for creating color palettes, it is provided just for the sake
    of completeness. All steps contain references for relevant sources describing the process.

    *)

    // ==================================
    // ===   1. CIE LCH <-> CIE LUV   ===
    // ==================================
    // LCH is just a cylindrical representation of LUV, hence the transformation is a simple conversion
    // from polar coordinates.
    // https://en.wikipedia.org/wiki/CIELUV

    let private degToRad x = x * Math.PI / 180.
    let private radToDeg x = x * 180. / Math.PI

    /// CIE LCH --> CIE LUV. 
    /// L is in [0.; 100.], C in [0.; 100.] and h is angle in degrees [0.; 360.], although it can be also
    /// outside of this range.
    let lchToLuv (L, C, h) =
        let u = C * cos (degToRad h)
        let v = C * sin (degToRad h)
        (L, u, v)

    /// CIE LUV --> CIE LCH. 
    let luvToLch (L, u, v) =
        let C = sqrt (u * u + v * v)
        let h = atan2 v u |> radToDeg
        (L, C, (h + 360.) % 360.)

    
    // ==============================
    // ===   2. CIE LUV <-> XYZ   ===
    // ==============================
    // This conversion needs a white point definition, let's use the standard D65 illuminant
    // (roughly corresponds to average midday light in Western / Northern Europe).
    // https://en.wikipedia.org/wiki/Illuminant_D65
    // https://en.wikipedia.org/wiki/CIELUV

    // Illuminant D65 using the standard 2° observer (normalized to Y = 100.)
    let private Xn = 95.047
    let private Yn = 100.000
    let private Zn = 108.883

    // XYZ <-> u'v', helper function
    let private xyzToUv (X, Y, Z) =
        let u' = 4. * X / (X + 15. * Y + 3. * Z)
        let v' = 9. * Y / (X + 15. * Y + 3. * Z)
        (u', v')

    // white point expressed as (un', vn')
    let private un', vn' = xyzToUv (Xn, Yn, Zn)

    /// CIE LUV --> XYZ with standard illuminant D65.
    let luvToXyz (L, u, v) =
        let u' = u / (13. * L) + un'
        let v' = v / (13. * L) + vn'
        let Y = if L <= 8. then Yn * L * (3. / 29.) ** 3. else Yn * ((L + 16.) / 116.) ** 3.
        let X = Y * 9. * u' / (4. * v')
        let Z = Y * (12. - 3. * u' - 20. * v') / (4. * v')
        (X, Y, Z)
    
    /// XYZ --> CIE LUV with standard illuminant D65.
    let xyzToLuv (X, Y, Z) =
        let Yr = Y / Yn
        let L = if Yr <= (6. / 29.) ** 3. then Yr * (29. / 3.) ** 3. else 116. * Yr ** (1. / 3.) - 16.
        let u', v' = xyzToUv (X, Y, Z)
        let u = 13. * L * (u' - un')
        let v = 13. * L * (v' - vn')
        (L, u, v)

    
    // =================================
    // ===   3. XYZ <-> linear RGB   ===
    // =================================
    // Linear transformation.
    // https://en.wikipedia.org/wiki/SRGB
    // https://www.w3.org/Graphics/Color/sRGB.html
    // Both sources have slightly different values, I am using those from Wikipedia.

    /// XYZ --> Linear RGB
    let xyzToLinearRgb (X, Y, Z) =
        let x, y, z = X / Yn, Y / Yn, Z / Yn
        let Rl =  3.2406 * x - 1.5372 * y - 0.4986 * z
        let Gl = -0.9689 * x + 1.8758 * y + 0.0415 * z
        let Bl =  0.0557 * x - 0.2040 * y + 1.0570 * z
        (Rl, Gl, Bl)

    /// Linear RGB --> XYZ
    let linearRgbToXyz (Rl, Gl, Bl) =
        let x = 0.4124 * Rl + 0.3576 * Gl + 0.1805 * Bl
        let y = 0.2126 * Rl + 0.7152 * Gl + 0.0722 * Bl
        let z = 0.0193 * Rl + 0.1192 * Gl + 0.9505 * Bl
        (x * Yn, y * Yn, z * Yn)


    // =====================================================
    // ===   4. linear RGB <-> sRGB (gamma correction)   ===
    // =====================================================
    // Based on gamma = 2.2 (default on Windows).

    // linear component --> sRGB
    let private gammaCorr c = if c <= 0.0031308 then 12.92 * c else 1.055 * c ** (1. / 2.4) - 0.055
    // sRGB component --> linear
    let private invGammaCorr c = if c <= 0.04045 then c / 12.92 else ((c + 0.055) / 1.055) ** 2.4

    /// Linear RGB --> sRGB (gamma correction)
    let linearRgbToSRgb (Rl, Gl, Bl) = (gammaCorr Rl, gammaCorr Gl, gammaCorr Bl)

    /// sRGB --> Linear RGB (inverse gamma correction)
    let sRgbToLinearRgb (R, G, B) = (invGammaCorr R, invGammaCorr G, invGammaCorr B)


    // ==================================
    // ===   5. sRGB <-> .NET Color   ===
    // ==================================
    // Rounding to integer loses some information, inversion is not precise.

    /// sRGB --> .NET Color
    /// Transformation of [0.; 1.] to [0; 255].
    let sRgbToColor (R, G, B) = 
        let toColor (f : float) = f |> (*) 256. |> truncate |> int |> max 0 |> min 255
        Color.FromArgb(toColor R, toColor G, toColor B)    
    /// .NET Color --> sRGB
    /// Transformation of [0; 255] to [0.; 1.].
    let colorToSRgb (c : Color) = float c.R / 255., float c.G / 255., float c.B / 255.


    // ==================================
    // ===   CIE LCH <-> .NET Color   ===
    // ==================================
    // All at once, for easy use in palettes generation.

    /// CIE LCH --> .NET Color
    let lchToColor (L, C, h) = 
        (L, C, h) |> lchToLuv |> luvToXyz |> xyzToLinearRgb |> linearRgbToSRgb |> sRgbToColor

    /// .NET Color --> CIE LCH
    /// Round trip is not exact due to integer rounding.
    let colorToLch (c : Color) =
        c |> colorToSRgb |> sRgbToLinearRgb |> linearRgbToXyz |> xyzToLuv |> luvToLch


    // ===========================================
    // ===   .NET Color <-> hexadecimal code   ===
    // ===========================================

    /// .NET Color --> hexadecimal notation #RRGGBB
    let colorToHex (c : Color) =
        [| int c.R / 16; int c.R % 16; int c.G / 16; int c.G % 16; int c.B / 16; int c.B % 16 |]
        |> Array.map (fun i -> if i < 10 then char (i + 48) else char (i + 55))
        |> Array.append [|'#'|]
        |> String.Concat

    /// Hexadecimal notation #RRGGBB --> .NET Color 
    let hexToColor (s : string) =
        let mtch = Regex.Match(s.ToUpper(), @"^#([A-Z0-9]{6})$")
        if not mtch.Success then failwithf "%s is not a valid color code, expected #RRGGBB." s
        else
            let digs = 
                mtch.Groups.[1].Value.ToCharArray() 
                |> Array.map (int >> fun i -> if i < 65 then i - 48 else i - 55)
            Color.FromArgb(16 * digs.[0] + digs.[1], 16 * digs.[2] + digs.[3], 16 * digs.[4] + digs.[5])



