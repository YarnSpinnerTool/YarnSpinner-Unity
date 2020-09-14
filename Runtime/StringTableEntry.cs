using System.Collections.Generic;
using CsvHelper;
using CsvHelper.Configuration;

namespace Yarn.Unity
{
    public struct StringTableEntry
    {
        [Optional] // added in v2.0; will not be present in files generated in earlier versions
        public string Language;

        public string ID;

        public string Text;

        public string File;

        public string Node;

        public string LineNumber;

        /// <summary>
        /// A string used as part of a mechanism for checking if translated
        /// versions of this string are out of date.
        /// </summary>
        /// <remarks>
        /// This field contains the first 8 characters of the SHA-256 hash
        /// of the line's text as it appeared in the base localization CSV
        /// file.
        ///
        /// When a new StringTableEntry is created in a localized CSV file
        /// for a .yarn file, the Lock value is copied over from the base
        /// CSV file, and used for the translated entry. 
        ///
        /// Because the base localization CSV is regenerated every time the
        /// .yarn file is imported, the base localization Lock value will
        /// change if a line's text changes. This means that if the base
        /// lock and translated lock differ, the translated line is out of
        /// date, and needs to be updated.
        /// </remarks>
        [Optional]
        public string Lock;

        /// <summary>
        /// A comment used to describe this line to translators.
        /// </summary>
        [Optional]
        public string Comment;

        public StringTableEntry(StringTableEntry s)
        {
            ID = s.ID;
            Text = s.Text;
            File = s.File;
            Node = s.Node;
            LineNumber = s.LineNumber;
            Lock = s.Lock;
            Comment = s.Comment;
            Language = s.Language;
        }

        private static CsvHelper.Configuration.Configuration CsvConfiguration;

        private static CsvHelper.Configuration.Configuration GetConfiguration()
        {
            if (CsvConfiguration == null)
            {
                CsvConfiguration = new CsvHelper.Configuration.Configuration(System.Globalization.CultureInfo.InvariantCulture)
                {
                    MemberTypes = CsvHelper.Configuration.MemberTypes.Fields,
                };
            }
            return CsvConfiguration;
        }

        /// <summary>
        /// Reads comma-separated value ata from <paramref
        /// name="sourceText"/>, and produces a collection of <see
        /// cref="StringTableEntry"/> structs.
        /// </summary>
        /// <param name="sourceText">A string containing CSV-formatted
        /// data.</param>
        /// <returns>The parsed collection of <see
        /// cref="StringTableEntry"/> structs.</returns>
        /// <throws cref="CsvHelperException">Thrown when an error occurs
        /// when parsing the string.</throws>
        public static IEnumerable<StringTableEntry> ParseFromCSV(string sourceText)
        {
            using (var stringReader = new System.IO.StringReader(sourceText))
            using (var csv = new CsvReader(stringReader, GetConfiguration()))
            {
                /*
                Do the below instead of GetRecords<T> due to incompatibility with IL2CPP
                See more: https://github.com/YarnSpinnerTool/YarnSpinner-Unity/issues/36#issuecomment-691489913
                */
                var records = new List<StringTableEntry>();
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    var record = new StringTableEntry
                    {
                        Language = csv.GetField<string>("language"),
                        ID = csv.GetField<string>("id"),
                        Text = csv.GetField<string>("text"),
                        File = csv.GetField<string>("file"),
                        Node = csv.GetField<string>("node"),
                        LineNumber = csv.GetField<string>("lineNumber"),
                        Lock = csv.GetField<string>("lock"),
                        Comment = csv.GetField<string>("comment"),
                    };
                    records.Add(record);
                }

                return records;
            }
        }

        public static string CreateCSV(IEnumerable<StringTableEntry> entries)
        {
            using (var textWriter = new System.IO.StringWriter())
            {
                // Generate the localised .csv file

                // Use the invariant culture when writing the CSV
                var csv = new CsvHelper.CsvWriter(
                    textWriter, // write into this stream
                    GetConfiguration() // use this configuration
                    );

                csv.WriteRecords(entries);

                return textWriter.ToString();
            }
        }

        public override string ToString()
        {
            return $"StringTableEntry: lang={Language} id={ID} text=\"{Text}\" file={File} node={Node} line={LineNumber} lock={Lock} comment={Comment}";
        }

        public override bool Equals(object obj)
        {
            return obj is StringTableEntry entry &&
                   Language == entry.Language &&
                   ID == entry.ID &&
                   Text == entry.Text &&
                   File == entry.File &&
                   Node == entry.Node &&
                   LineNumber == entry.LineNumber &&
                   Lock == entry.Lock &&
                   Comment == entry.Comment;
        }
    }
}

