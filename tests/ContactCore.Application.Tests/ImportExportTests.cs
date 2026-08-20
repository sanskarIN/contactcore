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
        var c = new Contact { GivenName = "Ada, A.", FamilyName = "Lovelace", Notes = "Line 1\n\"Line 2\"" };
        c.Emails.Add(new(Guid.NewGuid(), "Email", "ada@example.test"));
        var decoded = ContactCsvCodec.Import(ContactCsvCodec.Export([c])).Contacts.Single();
        Assert.AreEqual(c.GivenName, decoded.GivenName); Assert.AreEqual(c.Notes, decoded.Notes); Assert.AreEqual("ada@example.test", decoded.Emails.Single().Address);
    }

    [TestMethod]
    public void Csv_export_neutralizes_spreadsheet_formulas_and_round_trips()
    {
        var c = new Contact { GivenName = "=2+2", FamilyName = "  @SUM(A1:A2)", Notes = "-10" };
        c.Phones.Add(new(Guid.NewGuid(), "Mobile", "+91 99999 00000"));

        var csv = ContactCsvCodec.Export([c]);

        Assert.IsTrue(csv.Contains("\"'=2+2\"", StringComparison.Ordinal));
        Assert.IsTrue(csv.Contains("\"'  @SUM(A1:A2)\"", StringComparison.Ordinal));
        Assert.IsTrue(csv.Contains("\"'-10\"", StringComparison.Ordinal));
        Assert.IsTrue(csv.Contains("\"'+91 99999 00000\"", StringComparison.Ordinal));

        var decoded = ContactCsvCodec.Import(csv).Contacts.Single();
        Assert.AreEqual(c.GivenName, decoded.GivenName);
        Assert.AreEqual(c.FamilyName.Trim(), decoded.FamilyName);
        Assert.AreEqual(c.Notes, decoded.Notes);
        Assert.AreEqual("+91 99999 00000", decoded.Phones.Single().Number);
    }

    [TestMethod]
    public void Vcard_round_trip_preserves_primary_fields_and_note()
    {
        var c = new Contact { GivenName = "Lin", FamilyName = "Chen", Birthday = new DateOnly(2000, 1, 2), Notes = "Hello" };
        c.Phones.Add(new(Guid.NewGuid(), "Mobile", "+1 555 0100"));
        var decoded = VCardCodec.Import(VCardCodec.Export([c])).Contacts.Single();
        Assert.AreEqual("Lin", decoded.GivenName); Assert.AreEqual("Chen", decoded.FamilyName); Assert.AreEqual(c.Birthday, decoded.Birthday); Assert.AreEqual("Hello", decoded.Notes); Assert.AreEqual(1, decoded.Phones.Count);
    }

    [TestMethod]
    public void Csv_parser_survives_random_unicode_without_throwing()
    {
        var random = new Random(1234);
        for (var n = 0; n < 250; n++)
        {
            var chars = Enumerable.Range(0, random.Next(0, 100)).Select(_ => (char)random.Next(32, 0x3000)).ToArray();
            _ = ContactCsvCodec.Import(new string(chars));
        }
    }
}
