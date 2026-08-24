using ContactCore.Domain;

namespace ContactCore.Application.Tests;

[TestClass]
public sealed class ImportExportHardeningTests
{
    [TestMethod]
    public void Csv_with_unrecognized_header_does_not_create_unnamed_contacts()
    {
        var result = ContactCsvCodec.Import("Unrelated,Columns\nvalue,other\n");

        Assert.AreEqual(0, result.Contacts.Count);
        Assert.IsTrue(result.Warnings.Any(x => x.Contains("supported columns", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Csv_duplicate_headers_use_first_column_without_throwing()
    {
        var result = ContactCsvCodec.Import("GivenName,GivenName,Notes\nFirst,Second,Example\n");

        Assert.AreEqual(1, result.Contacts.Count);
        Assert.AreEqual("First", result.Contacts.Single().GivenName);
        Assert.IsTrue(result.Warnings.Any(x => x.Contains("more than once", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Csv_formula_like_text_is_preserved_but_warned_about()
    {
        var result = ContactCsvCodec.Import("GivenName,Notes\n\"=Example\",\"@Text\"\n");

        Assert.AreEqual("=Example", result.Contacts.Single().GivenName);
        Assert.IsTrue(result.Warnings.Any(x => x.Contains("spreadsheet", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Vcard_round_trip_preserves_escaped_name_delimiters_backslashes_and_newlines()
    {
        var source = new Contact
        {
            GivenName = "Given;Name",
            FamilyName = @"Family\\Name",
            Notes = "Line one\nLine two; with comma, and slash \\"
        };

        var decoded = VCardCodec.Import(VCardCodec.Export([source])).Contacts.Single();

        Assert.AreEqual(source.GivenName, decoded.GivenName);
        Assert.AreEqual(source.FamilyName, decoded.FamilyName);
        Assert.AreEqual(source.Notes, decoded.Notes);
    }

    [TestMethod]
    public void Vcard_import_maps_type_parameters_to_contact_field_kind()
    {
        const string card = "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:Example Person\r\nTEL;TYPE=work:+1 555 0100\r\nEMAIL;TYPE=other:example@example.test\r\nEND:VCARD\r\n";

        var decoded = VCardCodec.Import(card).Contacts.Single();

        Assert.AreEqual(ContactFieldKind.Work, decoded.Phones.Single().Kind);
        Assert.AreEqual(ContactFieldKind.Other, decoded.Emails.Single().Kind);
    }

    [TestMethod]
    public void Vcard_invalid_birthday_warning_does_not_echo_imported_value()
    {
        const string secretLikeValue = "not-a-real-private-date-token";
        var card = $"BEGIN:VCARD\r\nVERSION:4.0\r\nFN:Example\r\nBDAY:{secretLikeValue}\r\nEND:VCARD\r\n";

        var result = VCardCodec.Import(card);

        Assert.AreEqual(1, result.Warnings.Count);
        Assert.IsFalse(result.Warnings[0].Contains(secretLikeValue, StringComparison.Ordinal));
    }
}
