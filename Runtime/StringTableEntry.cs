/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using CsvHelper;
using System.Collections.Generic;

#nullable enable

namespace Yarn.Unity
{
    public struct StringTableEntry
    {
        /// <summary>
        /// The language that the line is written in.
        /// </summary>
        public string Language;

        /// <summary>
        /// The line ID for this line. This value will be the same across all
        /// localizations.
        /// </summary>
        public string ID;

        /// <summary>
        /// The text of this line, in the language specified by <see
        /// cref="Language"/>.
        /// </summary>
        public string? Text;

        /// <summary>
        /// The name of the Yarn script in which this line was originally found.
        /// </summary>
        public string File;

        /// <summary>
        /// The name of the node in which this line was originally found.
        /// </summary>
        /// <remarks>
        /// This node can be found in the file indicated by <see cref="File"/>.
        /// </remarks>
        public string Node;

        /// <summary>
        /// The line number in the file indicated by <see cref="File"/> at which
        /// the original version of this line can be found.
        /// </summary>
        public string LineNumber;

        /// <summary>
        /// A string used as part of a mechanism for checking if translated
        /// versions of this string are out of date.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This field contains the first 8 characters of the SHA-256 hash of
        /// the line's text as it appeared in the base localization CSV file.
        /// </para>
        /// <para>
        /// When a new StringTableEntry is created in a localized CSV file for a
        /// .yarn file, the Lock value is copied over from the base CSV file,
        /// and used for the translated entry. 
        /// </para>
        /// <para>
        /// Because the base localization CSV is regenerated every time the
        /// .yarn file is imported, the base localization Lock value will change
        /// if a line's text changes. This means that if the base lock and
        /// translated lock differ, the translated line is out of date, and
        /// needs to be updated.
        /// </para>
        /// </remarks>
        public string Lock;

        /// <summary>
        /// A comment used to describe this line to translators.
        /// </summary>
        public string Comment;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringTableEntry"/>
        /// struct, copying values from an existing instance.
        /// </summary>
        /// <param name="s">The instance to copy values from.</param>
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

        private static CsvHelper.Configuration.Configuration? CsvConfiguration;

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
        /// Reads comma-separated value data from <paramref name="sourceText"/>,
        /// and produces a collection of <see cref="StringTableEntry"/> structs.
        /// </summary>
        /// <param name="sourceText">A string containing CSV-formatted
        /// data.</param>
        /// <returns>The parsed collection of <see cref="StringTableEntry"/>
        /// structs.</returns>
        /// <exception cref="ArgumentException">Thrown when an error occurs when
        /// parsing the string.</exception>
        public static IEnumerable<StringTableEntry> ParseFromCSV(string sourceText)
        {
            try
            {
                using (var stringReader = new System.IO.StringReader(sourceText))
                using (var csv = new CsvReader(stringReader, GetConfiguration()))
                {
                    /*
                    Do the below instead of GetRecords<T> due to incompatibility
                    with IL2CPP See more:
                    https://github.com/YarnSpinnerTool/YarnSpinner-Unity/issues/36#issuecomment-691489913
                    */
                    var records = new List<StringTableEntry>();
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        // Fetch values; if they can't be found, they'll be
                        // defaults.
                        csv.TryGetField<string>("language", out var language);
                        csv.TryGetField<string>("lock", out var lockString);
                        csv.TryGetField<string>("comment", out var comment);
                        csv.TryGetField<string>("id", out var id);
                        csv.TryGetField<string>("text", out var text);
                        csv.TryGetField<string>("file", out var file);
                        csv.TryGetField<string>("node", out var node);
                        csv.TryGetField<string>("lineNumber", out var lineNumber);

                        var record = new StringTableEntry
                        {
                            Language = language ?? string.Empty,
                            ID = id ?? string.Empty,
                            Text = text ?? string.Empty,
                            File = file ?? string.Empty,
                            Node = node ?? string.Empty,
                            LineNumber = lineNumber ?? string.Empty,
                            Lock = lockString ?? string.Empty,
                            Comment = comment ?? string.Empty,
                        };

                        records.Add(record);
                    }

                    return records;
                }
            }
            catch (CsvHelperException e)
            {
                throw new System.ArgumentException($"Error reading CSV file: {e}");
            }
        }

        /// <summary>
        /// Creates a CSV-formatted string containing data from <paramref
        /// name="entries"/>.
        /// </summary>
        /// <param name="entries">The <see cref="StringTableEntry"/> values to
        /// generate the spreadsheet from.</param>
        /// <returns>A string containing CSV-formatted data.</returns>
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

                var fieldNames = new[] {
                    "language",
                    "id",
                    "text",
                    "file",
                    "node",
                    "lineNumber",
                    "lock",
                    "comment",
                };

                foreach (var field in fieldNames)
                {
                    csv.WriteField(field);
                }
                csv.NextRecord();

                foreach (var entry in entries)
                {
                    var values = new[] {
                        entry.Language,
                        entry.ID,
                        entry.Text,
                        entry.File,
                        entry.Node,
                        entry.LineNumber,
                        entry.Lock,
                        entry.Comment,
                    };
                    foreach (var value in values)
                    {
                        csv.WriteField(value);
                    }
                    csv.NextRecord();
                }

                return textWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"StringTableEntry: lang={Language} id={ID} text=\"{Text}\" file={File} node={Node} line={LineNumber} lock={Lock} comment={Comment}";
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return
                Language.GetHashCode() ^
                ID.GetHashCode() ^
                (Text?.GetHashCode() ?? 1) ^
                File.GetHashCode() ^
                Node.GetHashCode() ^
                LineNumber.GetHashCode() ^
                Lock.GetHashCode() ^
                Comment.GetHashCode();
        }
    }
}

