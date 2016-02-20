using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;
using LzhamWrapper.Enums;

namespace ReisUnpack.Options
{
    [Verb("repack", HelpText = "Repack Renown Explorers resources into archive")]
    class RepackOptions
    {
        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Repack Renowned Explores resources files into (content.tim)", new RepackOptions { OutputFile = "content.tim", InputFolder = "inputFolder" });
            }
        }

        [Value(0, Required = true, HelpText = "Path to input folder", MetaName = "inputFolder", Default = ".")]
        public string InputFolder { get; set; }
        [Value(1, HelpText = "Path to resulting resources archive", MetaName = "content.tim", Default = "content.tim")]
        public string OutputFile { get; set; }

        [Option('m', Default = CompressionLevel.Uber, HelpText = "Compression level: 0=fastest, 1=faster, 2=default, 3=better, 4=uber. Default is uber (4)", MetaValue = "level")]
        public CompressionLevel Level { get; set; }
    }
}
