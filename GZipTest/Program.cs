using GZipTest.Helpers;
using GZipTest.Intetfaces;
using System;
using System.IO;

namespace GZipTest
{
    public class Program
    {
        private static IFileArchiver _fileArchiver = new FileArchiver();
        private static string directoryPath = @"C:\Users\iosta\Downloads\Tests\CurrentTest";

        public static int Main(string[] args)
        {
            try
            {
                args = new string[3];
                args[0] = "compress";
                args[1] = directoryPath + @"\10mb.txt";
                args[2] = directoryPath + @"\10mb.txt" + _fileArchiver.DestinationFileExtension;
                ConsoleHelper.WriteProcessMessage("Validation arguments...");
                // ValidationHelper.Validate(args, _fileArchiver.DestinationFileExtension);

                DirectoryInfo directorySelected = new DirectoryInfo(directoryPath);

                foreach (FileInfo fileToCompress in directorySelected.GetFiles())
                {
                    ConsoleHelper.WriteProcessMessage("Compressing...");
                    _fileArchiver.Compress(fileToCompress);
                }

                foreach (FileInfo fileToDecompress in directorySelected.GetFiles("*.gz"))
                {
                    ConsoleHelper.WriteProcessMessage("Decompressing...");
                    _fileArchiver.Decompress(fileToDecompress);
                }
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