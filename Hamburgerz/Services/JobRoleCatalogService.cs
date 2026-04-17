using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Hamburgerz.Services
{
    // Local controlled vocabulary seeded for app autocomplete from official occupation catalogs:
    // O*NET Occupation Data + Alternate Titles, with app-specific friendly aliases added for UX.
    public sealed class JobRoleCatalogService
    {
        private readonly IReadOnlyList<JobRoleCatalogEntry> _entries;
        private readonly IReadOnlyList<JobRoleCatalogEntry> _featuredEntries;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<JobRoleCatalogEntry>> _exactLookup;

        public JobRoleCatalogService(IWebHostEnvironment environment)
        {
            var catalogPath = Path.Combine(
                environment.ContentRootPath,
                "Data",
                "Reference",
                "job-role-catalog.json");

            if (!File.Exists(catalogPath))
            {
                throw new FileNotFoundException("Job role catalog file was not found.", catalogPath);
            }

            using var stream = File.OpenRead(catalogPath);
            var fileEntries = JsonSerializer.Deserialize<List<JobRoleCatalogFileEntry>>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? [];

            _entries = fileEntries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.CanonicalTitle))
                .Select(BuildCatalogEntry)
                .ToList();

            if (_entries.Count == 0)
            {
                throw new InvalidOperationException("Job role catalog is empty.");
            }

            _featuredEntries = _entries
                .Where(entry => entry.Featured)
                .ToList();

            var exactLookup = new Dictionary<string, List<JobRoleCatalogEntry>>(StringComparer.Ordinal);

            foreach (var entry in _entries)
            {
                foreach (var searchValue in entry.SearchValues)
                {
                    if (!exactLookup.TryGetValue(searchValue.NormalizedValue, out var matches))
                    {
                        matches = [];
                        exactLookup[searchValue.NormalizedValue] = matches;
                    }

                    if (!matches.Any(current =>
                            current.CanonicalTitle.Equals(entry.CanonicalTitle, StringComparison.OrdinalIgnoreCase)))
                    {
                        matches.Add(entry);
                    }
                }
            }

            _exactLookup = exactLookup.ToDictionary(
                item => item.Key,
                item => (IReadOnlyList<JobRoleCatalogEntry>)item.Value,
                StringComparer.Ordinal);
        }

        public string? TryResolveCanonicalTitle(string? rawValue)
        {
            var normalizedValue = Normalize(rawValue);
            if (string.IsNullOrEmpty(normalizedValue))
            {
                return null;
            }

            return _exactLookup.TryGetValue(normalizedValue, out var matches) && matches.Count == 1
                ? matches[0].CanonicalTitle
                : null;
        }

        public IReadOnlyList<JobRoleSuggestion> Search(string? query, int maxResults = 8)
        {
            var take = Math.Clamp(maxResults, 1, 12);
            var normalizedQuery = Normalize(query);

            if (string.IsNullOrEmpty(normalizedQuery))
            {
                return _featuredEntries
                    .Take(take)
                    .Select(entry => ToSuggestion(entry))
                    .ToList();
            }

            return _entries
                .Select(entry => EvaluateSearch(entry, normalizedQuery))
                .Where(match => match != null)
                .OrderBy(match => match!.Score)
                .ThenBy(match => match!.Entry.CanonicalTitle, StringComparer.OrdinalIgnoreCase)
                .Take(take)
                .Select(match => ToSuggestion(match!.Entry))
                .ToList();
        }

        private static JobRoleCatalogEntry BuildCatalogEntry(JobRoleCatalogFileEntry fileEntry)
        {
            var canonicalTitle = (fileEntry.CanonicalTitle ?? string.Empty).Trim();
            var friendlyValues = (fileEntry.FriendlyValues ?? [])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Where(value => !value.Equals(canonicalTitle, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var searchValues = new List<JobRoleSearchValue>
            {
                new(canonicalTitle, Normalize(canonicalTitle), IsCanonical: true)
            };

            foreach (var friendlyValue in friendlyValues)
            {
                var normalizedValue = Normalize(friendlyValue);
                if (string.IsNullOrEmpty(normalizedValue))
                {
                    continue;
                }

                if (searchValues.Any(current => current.NormalizedValue == normalizedValue))
                {
                    continue;
                }

                searchValues.Add(new JobRoleSearchValue(friendlyValue, normalizedValue, IsCanonical: false));
            }

            return new JobRoleCatalogEntry(canonicalTitle, friendlyValues, fileEntry.Featured, searchValues);
        }

        private static JobRoleSuggestion ToSuggestion(JobRoleCatalogEntry entry) => new(entry.CanonicalTitle);

        private static JobRoleSearchMatch? EvaluateSearch(JobRoleCatalogEntry entry, string normalizedQuery)
        {
            JobRoleSearchMatch? bestMatch = null;

            foreach (var searchValue in entry.SearchValues)
            {
                var score = GetSearchScore(searchValue.NormalizedValue, normalizedQuery, searchValue.IsCanonical);
                if (!score.HasValue)
                {
                    continue;
                }

                if (bestMatch == null || score.Value < bestMatch.Score)
                {
                    bestMatch = new JobRoleSearchMatch(entry, searchValue.DisplayValue, score.Value);
                }
            }

            return bestMatch;
        }

        private static int? GetSearchScore(string candidate, string query, bool isCanonical)
        {
            var basePenalty = isCanonical ? 0 : 1;

            if (candidate.Equals(query, StringComparison.Ordinal))
            {
                return basePenalty;
            }

            if (candidate.StartsWith(query, StringComparison.Ordinal))
            {
                return 10 + basePenalty;
            }

            if (candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(word => word.StartsWith(query, StringComparison.Ordinal)))
            {
                return 20 + basePenalty;
            }

            if (candidate.Contains(query, StringComparison.Ordinal))
            {
                return 30 + basePenalty;
            }

            return null;
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var previousWasSpace = false;

            foreach (var character in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
                if (unicodeCategory == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                    previousWasSpace = false;
                    continue;
                }

                if (character is '-' or '/' or '&' or '+' || char.IsWhiteSpace(character))
                {
                    if (!previousWasSpace && builder.Length > 0)
                    {
                        builder.Append(' ');
                        previousWasSpace = true;
                    }
                }
            }

            return builder.ToString().Trim();
        }

        public sealed record JobRoleSuggestion(string CanonicalTitle);

        private sealed record JobRoleSearchMatch(JobRoleCatalogEntry Entry, string MatchedDisplayValue, int Score);

        private sealed record JobRoleSearchValue(string DisplayValue, string NormalizedValue, bool IsCanonical);

        private sealed class JobRoleCatalogEntry
        {
            public JobRoleCatalogEntry(
                string canonicalTitle,
                IReadOnlyList<string> friendlyValues,
                bool featured,
                IReadOnlyList<JobRoleSearchValue> searchValues)
            {
                CanonicalTitle = canonicalTitle;
                FriendlyValues = friendlyValues;
                Featured = featured;
                SearchValues = searchValues;
            }

            public string CanonicalTitle { get; }

            public IReadOnlyList<string> FriendlyValues { get; }

            public bool Featured { get; }

            public IReadOnlyList<JobRoleSearchValue> SearchValues { get; }
        }

        private sealed class JobRoleCatalogFileEntry
        {
            public string CanonicalTitle { get; set; } = string.Empty;

            public bool Featured { get; set; }

            public List<string> FriendlyValues { get; set; } = [];
        }
    }
}
