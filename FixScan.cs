using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FixOCR
{
    class FixScan
    {
        private const string outSep = ";";

        public void RunCIC(string path)
        {
            using (var inputFile = File.OpenRead(path))
            using (var input = new StreamReader(inputFile, Encoding.Default))
            using (var outputFile = File.Open(path + ".csv", FileMode.Create, FileAccess.Write))
            using (var output = new StreamWriter(outputFile, Encoding.Default))
            {
                while (!input.EndOfStream)
                {
                    var line = input.ReadLine();
                    var fields = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length >= 6)
                    {
                        var outline = "";
                        for (int i = 0; i < fields.Length; i++)
                        {
                            if (i == 0)
                                outline = FixCICFields(fields[i],i);
                            else 
                                outline = outline + outSep + FixCICFields(fields[i], i);
                        }

                        output.WriteLine(outline);
                    }
                }
            }
        }

        private string FixCICFields(string rawField, int index)
        {
            switch (index)
            {
                case 0:
                case 1:
                case 2:
                    var cleanDate = rawField.Replace(" ", "");
                    if (DateTime.TryParse(cleanDate, out DateTime res))
                    {
                        return rawField;
                    }
                    else if (cleanDate.Length == 10 && (cleanDate[2] == '1' || cleanDate[5] == '1'))
                    {
                        var newDate = cleanDate.Substring(0, 2) + "/" + cleanDate.Substring(3, 2) + "/" + cleanDate.Substring(6, 4);
                        if (DateTime.TryParse(newDate, out DateTime res2))
                            return newDate;
                    }

                    return "# " + rawField;
                case 5: // amount
                    var cleanDecimal = rawField.Replace('•', '-').Replace(" ", "").Replace(',', '.').Trim('"');
                    if (decimal.TryParse(cleanDecimal, out decimal resDec))
                    {
                        return cleanDecimal;
                    }
                    return "# " + cleanDecimal;
                default:
                    return rawField;
            }
        }



        public void RunBNP(string path)
        {
            var outputFilename = Path.Combine(Path.GetDirectoryName(path), "Result", Path.GetFileName(path));
            using (var inputFile = File.OpenRead(path))
            using (var input = new StreamReader(inputFile, Encoding.Default))
            using (var outputFile = File.Open(outputFilename, FileMode.Create, FileAccess.Write))
            using (var output = new StreamWriter(outputFile, Encoding.Default))
            {
                var inside = false;
                var lineCpt = 1;
                var currentYear = 0;
                var currentOperations = new BNPManager();
                var columnNumber = 0;
                while (!input.EndOfStream)
                {
                    var line = "";

                    do
                    {
                        line += (string.IsNullOrEmpty(line) ? "" : "\n") +  input.ReadLine();
                        if (lineCpt == 1)
                            columnNumber = line.Count(c => c == ';');
                        lineCpt++;
                    } while (line.Count(c => c == ';') < columnNumber);

                    if (line.Count(c => c == ';') != columnNumber)
                        throw new FormatException("Invalid format");

                    var trimLine = line.Replace(" ", "").Replace("\t", "").Replace(";","");

                    if (trimLine.IndexOf("TOTALDESMONTANTS", StringComparison.CurrentCultureIgnoreCase) >= 0 || 
                        trimLine.IndexOf("TOTALDESOPERATIONS", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        currentOperations.Dump(output);

                        if (!inside)
                            throw new InvalidOperationException("Invalid end or operations");
                        inside = false;
                    }
                    else if (trimLine.IndexOf("SOLDECREDITEURAU", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                             trimLine.IndexOf("SOLDEDEBITEURAU", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        var regex = new Regex(@"AU(?<Month>\d{1,2}).(?<Day>\d{1,2}).(?<Year>(?:\d{4}|\d{2}))");
                        var m = regex.Match(line.Replace(" ", ""));
                        if (!m.Success)
                        {
                            throw new InvalidDataException("");
                        }

                        currentOperations.CurrentYear = int.Parse(m.Groups[3].Value);
                    }
                    else if (inside)
                    {
                        var cpt = line.Count(f => f == ';');
                        var fields = line.Split(new[] {';'});

                        currentOperations.Process(fields);
                    }

                    if (trimLine.IndexOf("Naturedesopérations", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        if (inside)
                            throw new InvalidOperationException("Invalid start of operations");
                        inside = true;
                        currentOperations.Init(line.Split(new[] { ';' }));
                    }
                }
            }
        }

        private string FixBNPFields(string rawField, int index, int currentYear)
        {
            switch (index)
            {
                case 0:
                case 2:
                    var cleanDate = rawField.Replace(" ", "").Replace(".", "");
                    var day = int.Parse(cleanDate.Substring(0, 2));
                    var month = int.Parse(cleanDate.Substring(2, 2));
                    return $"{day}/{month}/{currentYear}";

                case 3: // amount
                    var cleanDecimal = rawField.Replace('•', '-').Replace(" ", "").Replace(',', '.').Trim('"');
                    if (decimal.TryParse(cleanDecimal, out decimal resDec))
                    {
                        return cleanDecimal;
                    }
                    return "# " + cleanDecimal;
                default:
                    return rawField;
            }
        }

    }
}
