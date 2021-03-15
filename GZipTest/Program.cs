using GZipTest.Helpers;
using GZipTest.Intetfaces;
using System;
using System.IO;

namespace GZipTest
{
    public class Program
    {
        private static IFileArchiver _fileArchiver = new FileArchiver();

        public static int Main(string[] args)
        {
            try
            {
                ConsoleHelper.WriteProcessMessage("Validation arguments...");
                ValidationHelper.Validate(args, _fileArchiver.DestinationFileExtension);

                var initialFile = new FileInfo(args[1]);
                var destinationFile = new FileInfo(args[2]);

                switch(args[0])
                {
                    case "compress":
                        _fileArchiver.Compress(initialFile, destinationFile);
                        break;
                    case "decompress":
                        _fileArchiver.Decompress(initialFile, destinationFile);
                        break;
                }

                ConsoleHelper.WriteProcessMessage("Process completed.");
                return 0;
            }
            catch(Exception ex)
            {
                ConsoleHelper.WriteErrorMessage(ex.Message);
                return 1;
            }
        }
    }
}