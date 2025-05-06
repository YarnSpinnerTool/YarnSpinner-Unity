/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Yarn.Unity
{
    /// <summary>
    /// Provides access to all <see cref="Culture"/>s supported by Yarn Spinner.
    /// </summary>
    public static class Cultures
    {
        private static Lazy<IEnumerable<Culture>> _allCultures = new Lazy<IEnumerable<Culture>>(() => MakeCultureList());

        private static Lazy<Dictionary<string, Culture>> _allCulturesTable = new Lazy<Dictionary<string, Culture>>(() =>
        {
            var dict = new Dictionary<string, Culture>();
            foreach (var entry in _allCultures.Value)
            {
                dict[entry.Name] = entry;
            }
            return dict;
        });

        /// <summary>
        /// Get all <see cref="Culture"/>s supported by Yarn Spinner.
        /// </summary>
        private static IEnumerable<Culture> MakeCultureList() => CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Where(c => c.Name != "")
            .Select(c => new Culture
            {
                Name = c.Name,
                DisplayName = c.DisplayName,
                NativeName = c.NativeName,
                IsNeutralCulture = c.IsNeutralCulture,
            })
            .Append(new Culture { Name = "mi", DisplayName = "Maori", NativeName = "Māori", IsNeutralCulture = true })
            .OrderBy(c => c.DisplayName);

        public static IEnumerable<Culture> GetCultures() => _allCultures.Value;

        /// <summary>
        /// Returns the <see cref="Culture"/> represented by the language code
        /// in <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="Culture"/> to
        /// retrieve.</param>
        /// <returns>The <see cref="Culture"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when no <see
        /// cref="Culture"/> with the given language ID can be
        /// found.</exception>
        [Obsolete("Use " + nameof(TryGetCulture) + ", which does not throw if the culture can't be found.")]
        public static Culture GetCulture(string name)
        {
            var exists = _allCulturesTable.Value.TryGetValue(name, out var result);

            if (exists)
            {
                return result;
            }
            else
            {
                throw new ArgumentException($"Culture {name} not found", name);
            }
        }

        /// <summary>
        /// Gets the <see cref="Culture"/> represented by the language code in
        /// <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="Culture"/> to
        /// retrieve.</param>
        /// <param name="culture">On return, the <see cref="Culture"/> if one
        /// was found, or a default <see cref="Culture"/> if otherwise.</param>
        /// <returns><see langword="true"/> if a Culture was found; <see
        /// langword="false"/> otherwise.</returns>
        public static bool TryGetCulture(string name, out Culture culture)
        {
            return _allCulturesTable.Value.TryGetValue(name, out culture);
        }

        /// <summary>
        /// Returns a boolean value indicating whether <paramref name="name"/>
        /// is a valid identifier for retrieving a <see cref="Culture"/> from
        /// <see cref="GetCulture"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns><see langword="true"/> if name is a valid <see cref="Culture"/> name; <see langword="false"/> otherwise.</returns>
        public static bool HasCulture(string name)
        {
            return _allCulturesTable.Value.ContainsKey(name);
        }

        public static Culture CurrentNeutralCulture
        {
            get
            {
                var current = System.Globalization.CultureInfo.CurrentCulture;
                if (current.IsNeutralCulture == false)
                {
                    current = current.Parent;
                }

                TryGetCulture(current.Name, out var culture);
                return culture;
            }
        }
    }
}
