using System.IO;

namespace EazFixer
{
    internal static class Commandline
    {
        public static bool Parse(string[] args, out string inFile, out string outFile)
        {
            inFile = null;
            outFile = null;
            
            if (args.Length == 0)
                return false;

            //arg 1: inFile
            if (args.Length >= 1) {
                if (!File.Exists(inFile = Path.GetFullPath(args[0])))
                    return false;
            }

            //arg 2: outFile (optional)
            if (args.Length >= 2) {
                //if output directory is not specified, use inFile's directory
                string outDir = Path.GetDirectoryName(args[1]);
                if (string.IsNullOrEmpty(outDir)) outDir = Path.GetDirectoryName(inFile);

                outFile = Path.Combine(outDir, Path.GetFileName(args[1]));
            }
            else {
                //use directory from infile, always
                string outDir = Path.GetDirectoryName(inFile);

                //add -eazfix to the name
                outFile = Path.Combine(outDir, Path.GetFileNameWithoutExtension(inFile) + "-eazfix" + Path.GetExtension(inFile));
            }

            return true;
        }
    }
}
