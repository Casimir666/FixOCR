using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixOCR
{
    enum BnpFields
    {
        Date,
        Operation,
        ValueDate,
        Credit,
        Debit
    }

    class BNPManager
    {
        private bool _isInit;
        private int[] _indexes = new int[5];

        private List<string> _dates = new List<string>();
        private List<string> _operations = new List<string>();
        private List<string> _valueDates = new List<string>();
        private List<string> _credits = new List<string>();
        private List<string> _debits = new List<string>();

        public BNPManager()
        {
        }

        public int CurrentYear { get; set; }

        private bool TryParseDate(string value, out string result)
        {
            var cleanDate = value.Replace(" ", "").Replace(".", "");
            if (cleanDate.Length != 4)
            {
                result = string.Empty;
                return false;
            }

            var day = int.Parse(cleanDate.Substring(0, 2));
            var month = int.Parse(cleanDate.Substring(2, 2));
            result = $"{day}/{month}/{CurrentYear}";
            return true;

        }

        private bool TryParseDecimal(string value, out string result)
        {
            var sb = new StringBuilder();
            var withoutSpace = value.Replace(" ", "");
            for (int i = 0; i < withoutSpace.Length; i++)
            {
                if (char.IsDigit(withoutSpace[i]))
                {
                    sb.Append(withoutSpace[i]);
                }
                else if (withoutSpace[i] == ',' || withoutSpace[i] == '.')
                {
                    if (i >= withoutSpace.Length - 3)
                    {
                        sb.Append('.');
                    }
                }
            }

            var cleanDecimal = sb.ToString();
            if (decimal.TryParse(cleanDecimal, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal resDec))
            {
                result = cleanDecimal;
                return true;
            }

            result = string.Empty;
            return false;
        }

        public void Init(string[] fields)
        {
            //if (!_isInit)
            {
                int fieldIndex = 0;
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!string.IsNullOrEmpty(fields[i]))
                        _indexes[fieldIndex++] = i;
                }

                _isInit = true;
            }
        }

        public void Process(string[] fields)
        {
            int found = 0;

            // Fix : remove bnp message...
            if (_dates.Count == 0 && _operations.Count == 0 && _valueDates.Count == 0 && _credits.Count == 0 && fields.Count(f => !string.IsNullOrEmpty(f)) == 1)
                return;

            foreach (var field in ParseFields(fields, BnpFields.Date))
            {
                if (TryParseDate(field, out string result))
                    _dates.Add(result);
                else
                    throw new FormatException("Invalid date");
                found++;
            }

            foreach (var field in ParseFields(fields, BnpFields.ValueDate))
            {
                if (TryParseDate(field, out string result))
                    _valueDates.Add(result);
                else
                    throw new FormatException("Invalid date");
                found++;
            }

            foreach (var field in ParseFields(fields, BnpFields.Credit))
            {
                if (TryParseDecimal(field, out string result))
                {
                    _debits.Add(result);
                    _credits.Add("");
                }
                else
                    throw new FormatException("Invalid amount");
            }

            foreach (var field in ParseFields(fields, BnpFields.Debit))
            {
                if (TryParseDecimal(field, out string result))
                {
                    _credits.Add(result);
                    _debits.Add("");
                }
                else
                    throw new FormatException("Invalid amount");
            }

            foreach (var field in ParseFields(fields, BnpFields.Operation))
            {
                if (found > 0)
                    _operations.Add(field);
                else
                    _operations[_operations.Count - 1] = _operations[_operations.Count - 1] + " " + field;
            }
        }

        IEnumerable<string> ParseFields(string[] fields, BnpFields type)
        {
            var field = fields[_indexes[(int)type]];
            if (!string.IsNullOrEmpty(field))
            {
                if (field.StartsWith("\""))
                {
                    foreach (var subField in field.Trim('"').Split(new[]{'\n'}))
                    {
                        yield return subField;
                    }
                }
                else
                {
                    yield return field;
                }
            }
        }

        public void Dump(StreamWriter output)
        {
            if (_dates.Count != _operations.Count ||
                _valueDates.Count != _operations.Count ||
                _credits.Count != _operations.Count ||
                _debits.Count != _operations.Count)
            {
                throw new InvalidOperationException("Some data are missing");
            }

            for (int i = 0; i < _operations.Count; i++)
            {
                output.WriteLine($"{_dates[i]};\"{_operations[i]}\";{_valueDates[i]};{_debits[i]};{_credits[i]}");
            }

            output.WriteLine(";;;;");

            _dates.Clear();
            _operations.Clear();
            _valueDates.Clear();
            _credits.Clear();
            _debits.Clear();
        }
    }
}
