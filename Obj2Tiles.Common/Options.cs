using System.Text.Json.Serialization;
using CommandLine;

namespace Obj2Tiles.Common;

public sealed class Options
{
    [Value(0, MetaName = "Input", Required = true, HelpText = "Input OBJ file.")]
    public string Input { get; set; }

    [Value(1, MetaName = "Output", Required = true, HelpText = "Output folder.")]
    public string Output { get; set; }

    [Option('s', "stage", Required = false, HelpText = "Stage to stop at (Decimation, Splitting)", Default = Stage.Splitting)]
    public Stage StopAt { get; set; }

    [Option('d', "limitLength", Required = false, HelpText = "Limited size", Default = 10)]
    public double LimitLength { get; set; }
    
    [Option('z', "zsplit", Required = false, HelpText = "Splits along z-axis too", Default = true)]
    public bool ZSplit { get; set; }    
    
    [Option('l', "lods", Required = false, HelpText = "How many levels of details", Default = 3)]
    public int LODs { get; set; }

    [Option('k', "keeptextures", Required = false, HelpText = "Keeps original textures", Default = false)]
    public bool KeepOriginalTextures { get; set; }
    
    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }
    
    public double Altitude { get; set; }

    [Option("scale", Required = false, HelpText = "Scale for data if using units other than meters ( 1200.0/3937.0 for survey ft)", Default = 1.0)]
    public double Scale { get; set; }

    [Option('e',"error", Required = false, HelpText = "Base error for root node", Default = 100.0)]
    public double BaseError { get; set; }
    
    [Option("use-system-temp", Required = false, HelpText = "Uses the system temp folder", Default = false)]
    public bool UseSystemTempFolder { get; set; }
    
    [Option("keep-intermediate", Required = false, HelpText = "Keeps the intermediate files (do not cleanup)", Default = false)]
    public bool KeepIntermediateFiles { get; set; }

    [Option('t', "y-up-to-z-up", Required = false, HelpText = "Convert the upward Y-axis to the upward Z-axis, which is used in some situations where the upward axis may be the Y-axis or the Z-axis after the obj is exported.", Default = false)]
    public bool YUpToZUp { get; set; }
}

public enum Stage
{
    Decimation,
    Splitting,
    Tiling
}