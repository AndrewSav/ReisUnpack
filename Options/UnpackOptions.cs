using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace ReisUnpack.Options
{
    [Verb("unpack",HelpText = "Unpacks Renown Explorers resource archive")]
    class UnpackOptions
    {
        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Unpack Renowned Explores resources file (content.tim)", new UnpackOptions { InputFile = "content.time", OutputFolder = "outputFolder" });
            }
        }

        [Value(0, Required = true, HelpText = "Path to content.tim file", MetaName = "content.tim")]
        public string InputFile { get; set; }
        [Value(1, HelpText = "Path to output folder", MetaName = "ouputFolder", Default = ".")]
        public string OutputFolder { get; set; }
    }
}
