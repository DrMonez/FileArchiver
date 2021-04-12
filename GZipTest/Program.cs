using GZipTest.Helpers;
using GZipTest.Intetfaces;
using System;
using System.IO;

namespace GZipTest
{
    public class Program
    {
        private static IFileArchiver _fileArchiver = new GZipFileArchiver();
        private static string path = @"C:\Users\iosta\Downloads\GZipTest\";

        public static int Main(string[] args)
        {
            try
            {
                args = new string[3];
                args[0] = "compress";
                //args[1] = $"{path}5gb.txt";
                //args[2] = $"{path}5gb.gz";
                args[1] = $"{path}32gb.txt";
                args[2] = $"{path}32gb.gz";
                //args[1] = $"{path}pict14mb.jpg";
                //args[2] = $"{path}pict14mb.gz";
                //args[0] = "decompress";
                //args[1] = $"{path}5gb.gz";
                //args[2] = $"{path}5gb_dec.txt";
                //args[1] = $"{path}32gb.gz";
                //args[2] = $"{path}32gb_dec.txt";
                //args[1] = $"{path}pict14mb.gz";
                //args[2] = $"{path}pict14mb_dec.jpg";

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