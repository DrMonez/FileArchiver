using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GZipTest.Helpers
{
    internal static class ValidationHelper
    {
        public static void Validate(string[] args, string archiverExtension)
        {
            if (args.Length != 3)
            {
                throw new Exception("Please enter arguments up to the following pattern:\n compress/decompress [Source file] [Destination file].");
            }

            var stringComparer = StringComparer.InvariantCultureIgnoreCase;
            if (stringComparer.Compare(args[0], "compress") != 0 && stringComparer.Compare(args[0], "decompress") != 0)
            {
                throw new Exception("First argument has to be \"compress\" or \"decompress\".");
            }

            if (!File.Exists(args[1]))
            {
                throw new Exception("No source file was found.");
            }

            if (string.Equals(args[1], args[2]))
            {
                throw new Exception("Source and destination files have to be different.");
            }

            FileInfo sourceFile = new FileInfo(args[1]);
            FileInfo destinationFile = new FileInfo(args[2]);

            if (destinationFile.Exists)
            {
                throw new Exception("Destination file already exists. Please indiciate the different file name.");
            }

            if (stringComparer.Compare(args[0], "compress") == 0)
            {
                if (string.Equals(sourceFile.Extension, archiverExtension))
                {
                    throw new Exception("File has already been compressed.");
                }

                if (!string.Equals(destinationFile.Extension, archiverExtension))
                {
                    throw new Exception($"Destination file has to have a {archiverExtension} extension.");
                }
            }
            else
            {
                if (!string.Equals(sourceFile.Extension, archiverExtension))
                {
                    throw new Exception($"File to be decompressed has to have {archiverExtension} extension.");
                }

                if (string.Equals(destinationFile.Extension, archiverExtension))
                {
                    throw new Exception($"Destination file hasn't to have a {archiverExtension} extension.");
                }
            }
        }
    }
}
