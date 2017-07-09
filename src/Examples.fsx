#load "Conversion.fs"
#load "Palettes.fs"
open FsColorSpace
open System.Drawing
open System.Windows.Forms


module Examples = 

    let showPalette (colors : Color []) =
        let width = 800
        let height = 200
        let border = 50
        let n = colors.Length
        let offset = (width - 1) / n + 1
        let bmp = new Bitmap(width + 2 * border, height + 2 * border)
        for i = border to border + width - 1 do
            for j = border to border + height - 1 do
                let k = (i - border) / offset
                bmp.SetPixel(i, j, colors.[k])
        let frm = new Form(Visible = true, ClientSize = Size(width + 2 * border, height + 2 * border))
        let img = new PictureBox(Image = bmp, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Normal)
        frm.Controls.Add(img)


    // QUALITATIVE PALETTES
    // default palette mimicks ggplot2 colors, similar to rainbow_hcl in R colorspace package
    3 |> Palettes.Qualitative.basic |> showPalette
    4 |> Palettes.Qualitative.basic |> showPalette
    8 |> Palettes.Qualitative.basic |> showPalette
    200 |> Palettes.Qualitative.basic |> showPalette
    4 |> Palettes.Qualitative.warmHues |> showPalette
    4 |> Palettes.Qualitative.coldHues |> showPalette

    // SEQUENTIAL PALETTES 
    // mimicks sequential_hcl, heat_hcl, terrain_hcl in R colorspace package
    12 |> Palettes.Sequential.basic |> showPalette
    12 |> Palettes.Sequential.heat |> showPalette
    12 |> Palettes.Sequential.terrain |> showPalette
    12 |> Palettes.Sequential.forHue 0. |> showPalette

    // DIVERGING PALETTES 
    // mimicks diverge_hcl in R colorspace package
    7 |> Palettes.Diverging.basic |> showPalette
    7 |> Palettes.Diverging.forHues (130., 43.) |> showPalette


    // REPRODUCE PALETTES FROM HCL-COLORS ARTICLE
    // https://cran.r-project.org/web/packages/colorspace/vignettes/hcl-colors.pdf

    // qualitative - figure 1 
    4 |> Palettes.Qualitative.full 70. 50. (30., 300.) |> showPalette
    4 |> Palettes.Qualitative.full 70. 50. (60., 240.) |> showPalette
    4 |> Palettes.Qualitative.full 70. 50. (270., 150.) |> showPalette
    4 |> Palettes.Qualitative.full 70. 50. (90., -30.) |> showPalette

    // sequential - figure 2
    12 |> Palettes.Sequential.full (2.2, 2.2) (30., 90.) (0., 0.) (260., 260.) |> showPalette
    12 |> Palettes.Sequential.full (2.2, 2.2) (30., 90.) (80., 0.) (260., 260.) |> showPalette
    12 |> Palettes.Sequential.full (2., 0.2) (30., 90.) (80., 30.) (0., 90.) |> showPalette
    12 |> Palettes.Sequential.full (1.5, 0.5) (45., 90.) (65., 0.) (130., 0.) |> showPalette
    12 |> Palettes.Sequential.full (1., 1.) (75., 40.) (40., 80.) (0., -100.) |> Array.rev |> showPalette

    // diverging - figure 3
    7 |> Palettes.Diverging.basic |> showPalette
    7 |> Palettes.Diverging.full (1., 1.) (50., 90.) 100. (260., 0.) |> showPalette
    7 |> Palettes.Diverging.full (1.5, 1.5) (70., 90.) 100. (130., 43.) |> showPalette
    7 |> Palettes.Diverging.full (1.5, 1.5) (75., 95.) 59. (180., 330.) |> showPalette




