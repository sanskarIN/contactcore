using System.Text.Json.Serialization;
using ContactCore.Domain;

namespace ContactCore.Browser;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = false)]
[JsonSerializable(typeof(BrowserContactDocument[]))]
[JsonSerializable(typeof(BrowserPreferencesModel))]
internal partial class BrowserJsonContext : JsonSerializerContext;

internal sealed record BrowserPreferencesModel(
    string Theme,
    bool ReducedMotion,
    bool ConfirmPermanentDelete);

internal sealed class BrowserContactDocument
{
    public Guid Id { get; set; }
    public string GivenName { get; set; } = "";
    public string FamilyName { get; set; } = "";
    public string Nickname { get; set; } = "";
    public DateOnly? Birthday { get; set; }
    public string Notes { get; set; } = "";
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<ContactPhone> Phones { get; set; } = [];
    public List<ContactEmail> Emails { get; set; } = [];
    public List<ContactAddress> Addresses { get; set; } = [];
    public List<ContactOrganization> Organizations { get; set; } = [];
    public List<ContactGroup> Groups { get; set; } = [];
    public List<ContactTag> Tags { get; set; } = [];

    public Contact ToDomain()
    {
        var contact = new Contact
        {
            Id = Id,
            GivenName = GivenName,
            FamilyName = FamilyName,
            Nickname = Nickname,
            Birthday = Birthday,
            Notes = Notes,
            IsFavorite = IsFavorite,
            IsArchived = IsArchived,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
        contact.Phones.AddRange(Phones);
        contact.Emails.AddRange(Emails);
        contact.Addresses.AddRange(Addresses);
        contact.Organizations.AddRange(Organizations);
        contact.Groups.AddRange(Groups);
        contact.Tags.AddRange(Tags);
        return contact;
    }

    public static BrowserContactDocument FromDomain(Contact contact) => new()
    {
        Id = contact.Id,
        GivenName = contact.GivenName,
        FamilyName = contact.FamilyName,
        Nickname = contact.Nickname,
        Birthday = contact.Birthday,
        Notes = contact.Notes,
        IsFavorite = contact.IsFavorite,
        IsArchived = contact.IsArchived,
        CreatedAt = contact.CreatedAt,
        UpdatedAt = contact.UpdatedAt,
        Phones = [.. contact.Phones],
        Emails = [.. contact.Emails],
        Addresses = [.. contact.Addresses],
        Organizations = [.. contact.Organizations],
        Groups = [.. contact.Groups],
        Tags = [.. contact.Tags]
    };
}
