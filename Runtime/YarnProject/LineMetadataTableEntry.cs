/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using CsvHelper;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// Struct holding information about a line and its associated metadata.
    /// Only used internally as an intermediary before persisting information
    /// in either a `YarnProject` or a CSV file.
    /// </summary>
    internal struct LineMetadataTableEntry
    {
        /// <summary>
        /// The line ID for this line.
        /// </summary>
        public string ID;

        /// <summary>
        /// The name of the Yarn script in which this line was originally
        /// found.
        /// </summary>
        public string File;

        /// <summary>
        /// The name of the node in which this line was originally found.
        /// </summary>
        /// <remarks>
        /// This node can be found in the file indicated by <see
        /// cref="File"/>.
        /// </remarks>
        public string Node;

        /// <summary>
        /// The line number in the file indicated by <see cref="File"/> at
        /// which the original version of this line can be found.
        /// </summary>
        public string LineNumber;

        /// <summary>
        /// Additional metadata included in this line.
        /// </summary>
        public string[] Metadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineMetadataTableEntry"/>
        /// struct, copying values from an existing instance.
        /// </summary>
        /// <param name="s">The instance to copy values from.</param>
        public LineMetadataTableEntry(LineMetadataTableEntry s)
        {
            ID = s.ID;
            File = s.File;
            Node = s.Node;
            LineNumber = s.LineNumber;
            Metadata = s.Metadata;
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
        /// and produces a collection of <see cref="LineMetadataTableEntry"/> structs.
        /// </summary>
        /// <param name="sourceText">A string containing CSV-formatted
        /// data.</param>
        /// <returns>The parsed collection of <see cref="LineMetadataTableEntry"/>
        /// structs.</returns>
        /// <exception cref="ArgumentException">Thrown when an error occurs when
        /// parsing the string.</exception>
        internal static IEnumerable<LineMetadataTableEntry> ParseFromCSV(string sourceText)
        {
            try
            {
                using (var stringReader = new System.IO.StringReader(sourceText))
                using (var csv = new CsvReader(stringReader, GetConfiguration()))
                {
                    /*
                    Do the below instead of GetRecords<T> due to
                    incompatibility with IL2CPP See more:
                    https://github.com/YarnSpinnerTool/YarnSpinner-Unity/issues/36#issuecomment-691489913
                    */
                    var records = new List<LineMetadataTableEntry>();
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        // Fetch values; if they can't be found, they'll be
                        // defaults.
                        csv.TryGetField<string>("id", out var id);
                        csv.TryGetField<string>("file", out var file);
                        csv.TryGetField<string>("node", out var node);
                        csv.TryGetField<string>("lineNumber", out var lineNumber);
                        csv.TryGetField<string>("metadata", out var metadata);

                        var record = new LineMetadataTableEntry
                        {
                            ID = id ?? string.Empty,
                            File = file ?? string.Empty,
                            Node = node ?? string.Empty,
                            LineNumber = lineNumber ?? string.Empty,
                            Metadata = metadata?.Split(' ') ?? new string[] { },
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
        /// <param name="entries">The <see cref="LineMetadataTableEntry"/> values to
        /// generate the spreadsheet from.</param>
        /// <returns>A string containing CSV-formatted data.</returns>
        public static string CreateCSV(IEnumerable<LineMetadataTableEntry> entries)
        {
            using (var textWriter = new System.IO.StringWriter())
            {
                // Use the invariant culture when writing the CSV
                var csv = new CsvWriter(
                    textWriter, // write into this stream
                    GetConfiguration() // use this configuration
                    );

                var fieldNames = new[] {
                    "id",
                    "file",
                    "node",
                    "lineNumber",
                    "metadata",
                };

                foreach (var field in fieldNames)
                {
                    csv.WriteField(field);
                }
                csv.NextRecord();

                foreach (var entry in entries)
                {
                    var values = new[] {
                        entry.ID,
                        entry.File,
                        entry.Node,
                        entry.LineNumber,
                        string.Join(" ", entry.Metadata),
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
            return $"LineMetadataTableEntry: id={ID} file={File} node={Node} line={LineNumber} metadata={string.Join(" ", Metadata)}";
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is LineMetadataTableEntry entry &&
                   ID == entry.ID &&
                   File == entry.File &&
                   Node == entry.Node &&
                   LineNumber == entry.LineNumber &&
                   Enumerable.SequenceEqual(Metadata, entry.Metadata);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var result =
                ID.GetHashCode() ^
                File.GetHashCode() ^
                Node.GetHashCode() ^
                LineNumber.GetHashCode();

            foreach (var piece in Metadata)
            {
                result ^= piece.GetHashCode();
            }

            return result;
        }
    }
}

