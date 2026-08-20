using System.Diagnostics.CodeAnalysis;
using ContactCore.Domain;

namespace ContactCore.Application;

public sealed record DuplicateCandidate(Contact Left, Contact Right, double Score, IReadOnlyList<string> Reasons);

public sealed class DuplicateDetector
{
    public IReadOnlyList<DuplicateCandidate> Find(IReadOnlyList<Contact> contacts, double minimumScore = 0.55)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        minimumScore = Math.Clamp(minimumScore, 0, 1);

        var result = new List<DuplicateCandidate>();
        for (var i = 0; i < contacts.Count; i++)
        {
            for (var j = i + 1; j < contacts.Count; j++)
            {
                var candidate = Compare(contacts[i], contacts[j]);
                if (candidate.Score >= minimumScore) result.Add(candidate);
            }
        }
        return result.OrderByDescending(x => x.Score).ToArray();
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is an instance service API used by application composition and tests.")]
    public DuplicateCandidate Compare(Contact left, Contact right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var reasons = new List<string>();
        var score = 0d;
        var leftName = TextNormalizer.SearchKey(left.DisplayName);
        var rightName = TextNormalizer.SearchKey(right.DisplayName);
        if (leftName.Length > 0 && leftName == rightName) { score += 0.45; reasons.Add("Same normalized name"); }

        var leftEmails = left.Emails.Select(x => TextNormalizer.SearchKey(x.Address)).Where(x => x.Length > 0).ToHashSet();
        if (right.Emails.Any(x => leftEmails.Contains(TextNormalizer.SearchKey(x.Address)))) { score += 0.40; reasons.Add("Shared email address"); }

        var leftPhones = left.Phones.Select(x => TextNormalizer.PhoneKey(x.Number)).Where(x => x.Length >= 5).ToHashSet();
        if (right.Phones.Any(x => leftPhones.Contains(TextNormalizer.PhoneKey(x.Number)))) { score += 0.40; reasons.Add("Shared phone number"); }

        if (left.Birthday is not null && left.Birthday == right.Birthday) { score += 0.10; reasons.Add("Same birthday"); }
        return new(left, right, Math.Min(1, score), reasons);
    }
}

public sealed class ContactMerger
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is an instance service API used by application composition and tests.")]
    public Contact Merge(Contact primary, Contact secondary)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(secondary);
        if (primary.Id == secondary.Id) throw new ArgumentException("A contact cannot be merged with itself.", nameof(secondary));

        var merged = primary.DeepCopy();
        if (string.IsNullOrWhiteSpace(merged.GivenName)) merged.GivenName = secondary.GivenName;
        if (string.IsNullOrWhiteSpace(merged.FamilyName)) merged.FamilyName = secondary.FamilyName;
        if (string.IsNullOrWhiteSpace(merged.Nickname)) merged.Nickname = secondary.Nickname;
        if (merged.Birthday is null) merged.Birthday = secondary.Birthday;
        if (string.IsNullOrWhiteSpace(merged.Notes)) merged.Notes = secondary.Notes;
        else if (!string.IsNullOrWhiteSpace(secondary.Notes) && !merged.Notes.Contains(secondary.Notes, StringComparison.Ordinal))
            merged.Notes += Environment.NewLine + Environment.NewLine + secondary.Notes;
        merged.IsFavorite |= secondary.IsFavorite;

        foreach (var phone in secondary.Phones.Where(x => !merged.Phones.Any(y => TextNormalizer.PhoneKey(y.Number) == TextNormalizer.PhoneKey(x.Number))))
            merged.Phones.Add(phone with { Id = Guid.NewGuid() });
        foreach (var email in secondary.Emails.Where(x => !merged.Emails.Any(y => TextNormalizer.SearchKey(y.Address) == TextNormalizer.SearchKey(x.Address))))
            merged.Emails.Add(email with { Id = Guid.NewGuid() });
        foreach (var address in secondary.Addresses.Where(x => !merged.Addresses.Any(y => SameAddress(y, x))))
            merged.Addresses.Add(address with { Id = Guid.NewGuid() });
        foreach (var organization in secondary.Organizations.Where(x => !merged.Organizations.Any(y => SameOrganization(y, x))))
            merged.Organizations.Add(organization with { Id = Guid.NewGuid() });

        merged.Groups.AddRange(secondary.Groups.Where(x => !merged.Groups.Any(y => y.Id == x.Id || TextNormalizer.SearchKey(y.Name) == TextNormalizer.SearchKey(x.Name))));
        merged.Tags.AddRange(secondary.Tags.Where(x => !merged.Tags.Any(y => y.Id == x.Id || TextNormalizer.SearchKey(y.Name) == TextNormalizer.SearchKey(x.Name))));
        merged.UpdatedAt = DateTimeOffset.UtcNow;
        return merged;
    }

    private static bool SameAddress(ContactAddress left, ContactAddress right) =>
        TextNormalizer.SearchKey(left.Label) == TextNormalizer.SearchKey(right.Label) &&
        TextNormalizer.SearchKey(left.Street) == TextNormalizer.SearchKey(right.Street) &&
        TextNormalizer.SearchKey(left.City) == TextNormalizer.SearchKey(right.City) &&
        TextNormalizer.SearchKey(left.Region) == TextNormalizer.SearchKey(right.Region) &&
        TextNormalizer.SearchKey(left.PostalCode) == TextNormalizer.SearchKey(right.PostalCode) &&
        TextNormalizer.SearchKey(left.Country) == TextNormalizer.SearchKey(right.Country);

    private static bool SameOrganization(ContactOrganization left, ContactOrganization right) =>
        TextNormalizer.SearchKey(left.Name) == TextNormalizer.SearchKey(right.Name) &&
        TextNormalizer.SearchKey(left.Title ?? string.Empty) == TextNormalizer.SearchKey(right.Title ?? string.Empty) &&
        TextNormalizer.SearchKey(left.Department ?? string.Empty) == TextNormalizer.SearchKey(right.Department ?? string.Empty);
}
