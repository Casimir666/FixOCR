using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixOCR
{
    class Program
    {
        static void Main(string[] args)
        {
            var cic = new FixScan();

            /*
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WebSynchro\Perso\Documents Martine\Relevé compte CIC depuis 2012 split par année");
            foreach (var file in Directory.GetFiles(path, "*.txt"))
            {
                cic.RunCIC(file);
            }
            */

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"WebSynchro\Perso\Documents Martine\20170505112248_ilovepdf_split_range");
            foreach (var file in Directory.GetFiles(path, "*.csv"))
            {
                cic.RunBNP(file);
            }
        }
    }
}
