using ContactCore.Application;
using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Application.Tests;

[TestClass]
public sealed class ImportExportTests
{
    [TestMethod]
    public void Csv_round_trip_handles_commas_quotes_and_newlines()
    {
        var contact = new Contact
        {
            GivenName = "Ada, A.",
            FamilyName = "Lovelace",
            Notes = "Line 1\n\"Line 2\""
        };
        contact.Emails.Add(new(Guid.NewGuid(), "Email", "ada@example.test"));

        var decoded = ContactCsvCodec.Import(ContactCsvCodec.Export([contact])).Contacts.Single();

        Assert.AreEqual(contact.GivenName, decoded.GivenName);
        Assert.AreEqual(contact.Notes, decoded.Notes);
        Assert.AreEqual("ada@example.test", decoded.Emails.Single().Address);
    }

    [TestMethod]
    public void Csv_round_trip_preserves_repeated_and_extended_fields()
    {
        var contact = new Contact
        {
            GivenName = "Rich",
            FamilyName = "Contact",
            Nickname = "RC",
            Birthday = new DateOnly(1990, 5, 6),
            IsFavorite = true,
            IsArchived = true
        };
        contact.Phones.Add(new(Guid.NewGuid(), "Mobile", "+91 11111 22222", ContactFieldKind.Mobile));
        contact.Phones.Add(new(Guid.NewGuid(), "Work", "+91 33333 44444", ContactFieldKind.Work));
        contact.Emails.Add(new(Guid.NewGuid(), "Home", "rich@example.test", ContactFieldKind.Home));
        contact.Addresses.Add(new(Guid.NewGuid(), "Home", "1 Test Road", "Pune", "MH", "411001", "India"));
        contact.Organizations.Add(new(Guid.NewGuid(), "Example Org", "Engineer", "R&D"));
        contact.Groups.Add(new(Guid.NewGuid(), "Friends"));
        contact.Tags.Add(new(Guid.NewGuid(), "Priority"));

        var result = ContactCsvCodec.Import(ContactCsvCodec.Export([contact]));
        var decoded = result.Contacts.Single();

        Assert.AreEqual(0, result.Warnings.Count);
        Assert.AreEqual(2, decoded.Phones.Count);
        Assert.AreEqual(1, decoded.Emails.Count);
        Assert.AreEqual(1, decoded.Addresses.Count);
        Assert.AreEqual("Pune", decoded.Addresses[0].City);
        Assert.AreEqual("Engineer", decoded.Organizations.Single().Title);
        Assert.AreEqual("Friends", decoded.Groups.Single().Name);
        Assert.AreEqual("Priority", decoded.Tags.Single().Name);
        Assert.IsTrue(decoded.IsFavorite);
        Assert.IsTrue(decoded.IsArchived);
    }

    [DataTestMethod]
    [DataRow("=2+2")]
    [DataRow("+cmd")]
    [DataRow("-1+2")]
    [DataRow("@SUM(A1:A2)")]
    public void Csv_default_export_neutralizes_spreadsheet_formula_prefixes_and_round_trips(string value)
    {
        var contact = new Contact { GivenName = value };

        var exported = ContactCsvCodec.Export([contact]);
        var decoded = ContactCsvCodec.Import(exported).Contacts.Single();

        StringAssert.Contains(exported, "\"'" + value + "\"");
        Assert.AreEqual(value, decoded.GivenName);
    }

    [TestMethod]
    public void Csv_can_export_raw_machine_interchange_when_explicitly_requested()
    {
        var exported = ContactCsvCodec.Export([new Contact { GivenName = "=literal" }], spreadsheetSafe: false);

        StringAssert.Contains(exported, "\"=literal\"");
        Assert.IsFalse(exported.Contains("\"'=literal\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Vcard_round_trip_preserves_repeated_standard_and_contactcore_fields()
    {
        var contact = new Contact
        {
            GivenName = "Lin",
            FamilyName = "Chen",
            Nickname = "LC",
            Birthday = new DateOnly(2000, 1, 2),
            Notes = "Hello; world, again\nNext line",
            IsFavorite = true,
            IsArchived = true
        };
        contact.Phones.Add(new(Guid.NewGuid(), "Mobile", "+1 555 0100", ContactFieldKind.Mobile));
        contact.Phones.Add(new(Guid.NewGuid(), "Work", "+1 555 0101", ContactFieldKind.Work));
        contact.Emails.Add(new(Guid.NewGuid(), "Home", "lin@example.test", ContactFieldKind.Home));
        contact.Addresses.Add(new(Guid.NewGuid(), "Home", "10 Main; Apt 2", "Toronto", "ON", "A1A 1A1", "Canada"));
        contact.Organizations.Add(new(Guid.NewGuid(), "Example; Labs", "Researcher", "AI, Systems"));
        contact.Tags.Add(new(Guid.NewGuid(), "Friend, close"));
        contact.Groups.Add(new(Guid.NewGuid(), "Research; Team"));

        var result = VCardCodec.Import(VCardCodec.Export([contact]));
        var decoded = result.Contacts.Single();

        Assert.AreEqual(0, result.Warnings.Count);
        Assert.AreEqual("Lin", decoded.GivenName);
        Assert.AreEqual("Chen", decoded.FamilyName);
        Assert.AreEqual("LC", decoded.Nickname);
        Assert.AreEqual(contact.Birthday, decoded.Birthday);
        Assert.AreEqual(contact.Notes, decoded.Notes);
        Assert.AreEqual(2, decoded.Phones.Count);
        Assert.AreEqual(ContactFieldKind.Work, decoded.Phones[1].Kind);
        Assert.AreEqual("10 Main; Apt 2", decoded.Addresses.Single().Street);
        Assert.AreEqual("Researcher", decoded.Organizations.Single().Title);
        Assert.AreEqual("AI, Systems", decoded.Organizations.Single().Department);
        Assert.AreEqual("Friend, close", decoded.Tags.Single().Name);
        Assert.AreEqual("Research; Team", decoded.Groups.Single().Name);
        Assert.IsTrue(decoded.IsFavorite);
        Assert.IsTrue(decoded.IsArchived);
    }

    [TestMethod]
    public void Legacy_csv_columns_remain_importable()
    {
        const string csv = "GivenName,FamilyName,Email,Phone,Birthday,Notes\nAda,Lovelace,ada@example.test,+44 123456,1815-12-10,Legacy";

        var decoded = ContactCsvCodec.Import(csv).Contacts.Single();

        Assert.AreEqual("Ada", decoded.GivenName);
        Assert.AreEqual("ada@example.test", decoded.Emails.Single().Address);
        Assert.AreEqual("+44 123456", decoded.Phones.Single().Number);
        Assert.AreEqual(new DateOnly(1815, 12, 10), decoded.Birthday);
    }

    [TestMethod]
    public void Csv_parser_survives_random_unicode_without_throwing()
    {
        var random = new Random(1234);
        for (var n = 0; n < 250; n++)
        {
            var chars = Enumerable.Range(0, random.Next(0, 100))
                .Select(_ => (char)random.Next(32, 0x3000))
                .ToArray();
            _ = ContactCsvCodec.Import(new string(chars));
        }
    }
}
