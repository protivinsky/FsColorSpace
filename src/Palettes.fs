namespace FsColorSpace

module Palettes = 

    (*

    The provided color palettes mimick behavior of functions in R colorspace package:
        - qualitative palettes are similar to rainbow_hcl function
        - sequential palettes corresponds to sequential_hcl, heat_hcl and terrain_hcl
        - diverging palettes corresponds to diverge_hcl

    In many cases it might be needed to use full specification to obtain better results.
    See Examples.fsx for some examples of different specifications.

    *)

    let private range n start end' = 
        if n < 2 then failwith "Cannot generate a range with a single element." 
        let step = (end' - start) / float (n - 1)
        Array.init n (fun i -> start + float i * step)


    module Qualitative =

        /// Full specification of qualitative palette with constant luminance and chrome and equally 
        /// spaced hues from a given range. Similar to rainbow_hcl function from R colorspace package.
        let full lum chr (hStart, hEnd) n =
            (n, hStart, hEnd) |||> range |> Array.map (fun hue -> Conversion.lchToColor (lum, chr, hue))

        // R colorspace defaults are chroma = 50. and luminance = 70. 
        // I used instead the default settings 65. and 100. from ggplot2 as it provides better colors.

        /// Qualitative palette with default luminance = 65. and chroma 100. with equally spaced hues 
        /// from a given range.
        let forHues (hStart, hEnd) n = full 65. 100. (hStart, hEnd) n

        /// Basic qualitative palette with equally spaced hues from the whole wheel.
        let basic n = forHues (15., (375. - (360. / float n))) n

        /// Qualitative palette with equally spaced cold hues.
        let coldHues n = forHues (270., 150.) n

        /// Qualitative palette with equally spaced warm hues.
        let warmHues n = forHues (90., -30.) n


    module Sequential =
        
        /// Full specification of sequential palette, mimicks sequential_hcl function in R colorspace.
        let full (powL, powC) (lStart, lEnd) (cStart, cEnd) (hStart, hEnd) n =
            range n 1. 0.
            |> Array.map (fun x -> 
                let chr = cEnd - (cEnd - cStart) * (x ** powC)
                let lum = lEnd - (lEnd - lStart) * (x ** powL)
                let hue = hEnd - (hEnd - hStart) * x
                Conversion.lchToColor (lum, chr, hue))
        
        /// Sequential heat colors, mimicks heat_hcl function in R colorspace.
        let heat n = full (1., 0.2) (50., 90.) (100., 30.) (0., 90.) n

        /// Sequential terrain colors, mimicks terrain_hcl function in R colorspace.
        let terrain n = full (1., 0.1) (60., 95.) (80., 0.) (130., 0.) n

        /// Sequential palette with a given hue with luminance and chroma the same as sequential_hcl defaults
        /// in R colorspace.
        let forHue hue n = full (1.5, 1.5) (30., 90.) (80., 0.) (hue, hue) n

        /// Basic blue sequential palette (default of sequential_hcl in R colorspace).
        let basic n = forHue 260. n


    module Diverging = 

        /// Full specification of diverging palette, mimicks diverge_hcl in R colorspace.
        let full (powL, powC) (lStart, lEnd) chr (hStart, hEnd) n =
            range n 1. -1.
            |> Array.map (fun x ->
                let chr = chr * (abs x ** powC)
                let lum = lEnd - (lEnd - lStart) * (abs x ** powL)
                let hue = if x > 0. then hStart else hEnd
                Conversion.lchToColor (lum, chr, hue))
        
        /// Diverging palette for given hues with defaults corresponding to diverge_hcl R colorspace package.
        let forHues (hStart, hEnd) n = full (1.5, 1.5) (30., 90.) 80. (hStart, hEnd) n

        /// Basic blue - red diverging palette (default of diverge_hcl in R colorspace).
        let basic n = forHues (260., 0.) n

