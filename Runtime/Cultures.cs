using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Yarn.Unity
{
    /// <summary>
    /// All <see cref="Culture"/>s supported by YarnSpinner.
    /// </summary>
    public static class Cultures
    {
        private static Lazy<IEnumerable<Culture>> _allCultures = new Lazy<IEnumerable<Culture>>(() => MakeCultureList());

        private static Lazy<Dictionary<string, Culture>> _allCulturesTable = new Lazy<Dictionary<string, Culture>>( () => {
            var dict = new Dictionary<string, Culture>();
            foreach (var entry in _allCultures.Value) {
                dict[entry.Name] = entry;
            }
            return dict;
        });

        /// <summary>
        /// Get all <see cref="Culture"/>s supported by YarnSpinner.
        /// </summary>
        private static IEnumerable<Culture> MakeCultureList() => CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Where(c => c.Name != "")
            .Select(c => new Culture
            {
                Name = c.Name,
                DisplayName = c.DisplayName,
                NativeName = c.NativeName,                
            })
            .Append(new Culture { Name = "mi", DisplayName = "Maori", NativeName = "Māori" })
            .OrderBy(c => c.DisplayName);

        public static IEnumerable<Culture> GetCultures() => _allCultures.Value;

        public static Culture GetCulture(string name) {
            var exists = _allCulturesTable.Value.TryGetValue(name, out var result);

            if (exists) {
                return result;
            } else {
                throw new ArgumentException($"Culture {name} not found", name);
            }
        }

        public static bool HasCulture(string name) {
            return _allCulturesTable.Value.ContainsKey(name);
        }
    }
}
