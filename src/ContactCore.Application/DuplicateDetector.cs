using ContactCore.Domain;

namespace ContactCore.Application;

public sealed record DuplicateCandidate(
    Contact Left,
    Contact Right,
    double Score,
    IReadOnlyList<string> Reasons);

public sealed class DuplicateDetector
{
    public IReadOnlyList<DuplicateCandidate> Find(
        IReadOnlyList<Contact> contacts,
        double minimumScore = 0.55)
    {
        if (minimumScore < 0 || minimumScore > 1)
            throw new ArgumentOutOfRangeException(nameof(minimumScore), "Score must be between 0 and 1.");

        if (contacts.Count < 2) return [];

        // A score of zero intentionally means every pair is a candidate, so preserve that
        // uncommon diagnostic use case with the straightforward algorithm. Normal duplicate
        // scans use blocking indexes below and avoid O(n²) work for unrelated contacts.
        if (minimumScore == 0)
        {
            var allPairs = new List<DuplicateCandidate>();
            for (var i = 0; i < contacts.Count; i++)
            for (var j = i + 1; j < contacts.Count; j++)
                allPairs.Add(Compare(contacts[i], contacts[j]));
            return allPairs.OrderByDescending(x => x.Score).ToArray();
        }

        var candidates = new HashSet<(Guid Left, Guid Right)>();
        AddBucketPairs(
            contacts,
            c => Single(TextNormalizer.SearchKey(c.DisplayName)),
            candidates);
        AddBucketPairs(
            contacts,
            c => c.Emails.Select(x => TextNormalizer.SearchKey(x.Address)).Where(x => x.Length > 0).Distinct(),
            candidates);
        AddBucketPairs(
            contacts,
            c => c.Phones.Select(x => TextNormalizer.PhoneKey(x.Number)).Where(x => x.Length >= 5).Distinct(),
            candidates);
        AddBucketPairs(
            contacts,
            c => c.Birthday is { } birthday ? Single(birthday.ToString("yyyy-MM-dd")) : [],
            candidates);

        var byId = contacts.ToDictionary(x => x.Id);
        return candidates
            .Select(pair => Compare(byId[pair.Left], byId[pair.Right]))
            .Where(x => x.Score >= minimumScore)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Left.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Right.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public DuplicateCandidate Compare(Contact left, Contact right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var reasons = new List<string>();
        var score = 0d;
        var leftName = TextNormalizer.SearchKey(left.DisplayName);
        var rightName = TextNormalizer.SearchKey(right.DisplayName);
        if (leftName.Length > 0 && leftName == rightName)
        {
            score += 0.45;
            reasons.Add("Same normalized name");
        }

        var leftEmails = left.Emails
            .Select(x => TextNormalizer.SearchKey(x.Address))
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        if (right.Emails.Any(x => leftEmails.Contains(TextNormalizer.SearchKey(x.Address))))
        {
            score += 0.40;
            reasons.Add("Shared email address");
        }

        var leftPhones = left.Phones
            .Select(x => TextNormalizer.PhoneKey(x.Number))
            .Where(x => x.Length >= 5)
            .ToHashSet(StringComparer.Ordinal);
        if (right.Phones.Any(x => leftPhones.Contains(TextNormalizer.PhoneKey(x.Number))))
        {
            score += 0.40;
            reasons.Add("Shared phone number");
        }

        if (left.Birthday is not null && left.Birthday == right.Birthday)
        {
            score += 0.10;
            reasons.Add("Same birthday");
        }

        return new(left, right, Math.Min(1, score), reasons);
    }

    private static IEnumerable<string> Single(string value) =>
        value.Length == 0 ? [] : [value];

    private static void AddBucketPairs(
        IReadOnlyList<Contact> contacts,
        Func<Contact, IEnumerable<string>> keys,
        ISet<(Guid Left, Guid Right)> output)
    {
        var buckets = new Dictionary<string, List<Guid>>(StringComparer.Ordinal);
        foreach (var contact in contacts)
        {
            foreach (var key in keys(contact).Distinct(StringComparer.Ordinal))
            {
                if (key.Length == 0) continue;
                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = [];
                    buckets.Add(key, bucket);
                }
                bucket.Add(contact.Id);
            }
        }

        foreach (var bucket in buckets.Values.Where(x => x.Count > 1))
        {
            for (var i = 0; i < bucket.Count; i++)
            for (var j = i + 1; j < bucket.Count; j++)
            {
                var a = bucket[i];
                var b = bucket[j];
                output.Add(a.CompareTo(b) < 0 ? (a, b) : (b, a));
            }
        }
    }
}

public sealed class ContactMerger
{
    public Contact Merge(Contact primary, Contact secondary)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(secondary);

        var merged = primary.DeepCopy();
        if (string.IsNullOrWhiteSpace(merged.GivenName)) merged.GivenName = secondary.GivenName;
        if (string.IsNullOrWhiteSpace(merged.FamilyName)) merged.FamilyName = secondary.FamilyName;
        if (string.IsNullOrWhiteSpace(merged.Nickname)) merged.Nickname = secondary.Nickname;
        if (merged.Birthday is null) merged.Birthday = secondary.Birthday;

        if (string.IsNullOrWhiteSpace(merged.Notes)) merged.Notes = secondary.Notes;
        else if (!string.IsNullOrWhiteSpace(secondary.Notes) &&
                 !merged.Notes.Contains(secondary.Notes, StringComparison.Ordinal))
            merged.Notes += Environment.NewLine + Environment.NewLine + secondary.Notes;

        merged.IsFavorite |= secondary.IsFavorite;
        merged.Phones.AddRange(secondary.Phones.Where(x =>
            !merged.Phones.Any(y => TextNormalizer.PhoneKey(y.Number) == TextNormalizer.PhoneKey(x.Number))));
        merged.Emails.AddRange(secondary.Emails.Where(x =>
            !merged.Emails.Any(y => TextNormalizer.SearchKey(y.Address) == TextNormalizer.SearchKey(x.Address))));
        merged.Addresses.AddRange(secondary.Addresses.Where(x =>
            !merged.Addresses.Any(y =>
                TextNormalizer.SearchKey(y.Label) == TextNormalizer.SearchKey(x.Label) &&
                TextNormalizer.SearchKey(y.Street) == TextNormalizer.SearchKey(x.Street) &&
                TextNormalizer.SearchKey(y.City) == TextNormalizer.SearchKey(x.City) &&
                TextNormalizer.SearchKey(y.Country) == TextNormalizer.SearchKey(x.Country))));
        merged.Organizations.AddRange(secondary.Organizations.Where(x =>
            !merged.Organizations.Any(y => TextNormalizer.SearchKey(y.Name) == TextNormalizer.SearchKey(x.Name))));
        merged.Dates.AddRange(secondary.Dates.Where(x =>
            !merged.Dates.Any(y =>
                y.Date == x.Date &&
                TextNormalizer.SearchKey(y.Label) == TextNormalizer.SearchKey(x.Label))));
        merged.NoteEntries.AddRange(secondary.NoteEntries.Where(x =>
            !merged.NoteEntries.Any(y =>
                TextNormalizer.SearchKey(y.Label) == TextNormalizer.SearchKey(x.Label) &&
                TextNormalizer.SearchKey(y.Content) == TextNormalizer.SearchKey(x.Content))));
        merged.Groups.AddRange(secondary.Groups.Where(x =>
            !merged.Groups.Any(y => y.Id == x.Id || TextNormalizer.SearchKey(y.Name) == TextNormalizer.SearchKey(x.Name))));
        merged.Tags.AddRange(secondary.Tags.Where(x =>
            !merged.Tags.Any(y => y.Id == x.Id || TextNormalizer.SearchKey(y.Name) == TextNormalizer.SearchKey(x.Name))));
        return merged;
    }
}
