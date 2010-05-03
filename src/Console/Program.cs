using System;
using NPackage.Core;

namespace NPackage.Console
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                return new InstallCommand().Run(args);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}