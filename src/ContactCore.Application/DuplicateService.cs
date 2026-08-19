using ContactCore.Domain;

namespace ContactCore.Application;

public sealed record DuplicateCandidate(Contact Contact, double Score, IReadOnlyList<string> Reasons);

public sealed class DuplicateService(IContactRepository repository)
{
    private readonly IContactRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IReadOnlyList<DuplicateCandidate>> FindPotentialDuplicatesAsync(
        Contact candidate,
        double threshold = 0.55,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        threshold = Math.Clamp(threshold, 0, 1);
        var contacts = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return contacts
            .Where(existing => existing.Id != candidate.Id)
            .Select(existing => Score(candidate, existing))
            .Where(result => result.Score >= threshold)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Contact.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static DuplicateCandidate Score(Contact left, Contact right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var score = 0d;
        var reasons = new List<string>();
        var leftName = TextNormalizer.SearchKey(left.DisplayName);
        var rightName = TextNormalizer.SearchKey(right.DisplayName);
        if (leftName.Length > 0 && leftName == rightName)
        {
            score += 0.30;
            reasons.Add("same normalized name");
        }

        var leftEmails = left.Emails.Select(e => e.Address.Trim().ToLowerInvariant()).Where(v => v.Length > 0).ToHashSet(StringComparer.Ordinal);
        if (right.Emails.Any(e => leftEmails.Contains(e.Address.Trim().ToLowerInvariant())))
        {
            score += 0.35;
            reasons.Add("shared email address");
        }

        var leftPhones = left.Phones.Select(p => TextNormalizer.PhoneKey(p.Number)).Where(v => v.Length > 0).ToHashSet(StringComparer.Ordinal);
        if (right.Phones.Any(p => leftPhones.Contains(TextNormalizer.PhoneKey(p.Number))))
        {
            score += 0.35;
            reasons.Add("shared phone number");
        }

        return new DuplicateCandidate(right, Math.Min(1, score), reasons);
    }

    public static Contact Merge(Contact primary, Contact secondary)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(secondary);
        var merged = primary.DeepCopy();
        merged.GivenName = Prefer(merged.GivenName, secondary.GivenName);
        merged.FamilyName = Prefer(merged.FamilyName, secondary.FamilyName);
        merged.Nickname = Prefer(merged.Nickname, secondary.Nickname);
        merged.Notes = MergeNotes(merged.Notes, secondary.Notes);
        merged.Birthday ??= secondary.Birthday;
        merged.IsFavorite |= secondary.IsFavorite;

        AddUnique(merged.Phones, secondary.Phones, p => TextNormalizer.PhoneKey(p.Number));
        AddUnique(merged.Emails, secondary.Emails, e => e.Address.Trim().ToLowerInvariant());
        AddUnique(merged.Addresses, secondary.Addresses, a => TextNormalizer.SearchKey($"{a.Street}|{a.City}|{a.PostalCode}|{a.Country}"));
        AddUnique(merged.Organizations, secondary.Organizations, o => TextNormalizer.SearchKey($"{o.Name}|{o.Title}|{o.Department}"));
        AddUnique(merged.Groups, secondary.Groups, g => TextNormalizer.SearchKey(g.Name));
        AddUnique(merged.Tags, secondary.Tags, t => TextNormalizer.SearchKey(t.Name));
        merged.UpdatedAt = DateTimeOffset.UtcNow;
        return merged;
    }

    private static void AddUnique<T>(List<T> target, IEnumerable<T> source, Func<T, string> keySelector)
    {
        var keys = target.Select(keySelector).ToHashSet(StringComparer.Ordinal);
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (key.Length > 0 && keys.Add(key)) target.Add(item);
        }
    }

    private static string Prefer(string current, string alternative) =>
        string.IsNullOrWhiteSpace(current) ? alternative : current;

    private static string MergeNotes(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left)) return right;
        if (string.IsNullOrWhiteSpace(right) || left.Contains(right, StringComparison.Ordinal)) return left;
        return $"{left.Trim()}\n\n{right.Trim()}";
    }
}
