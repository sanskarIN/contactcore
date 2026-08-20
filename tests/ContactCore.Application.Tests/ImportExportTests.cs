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
    public void Csv_default_export_preserves_formula_like_text_exactly()
    {
        var c = new Contact { GivenName = "=1+1", FamilyName = "+SUM(A1:A2)", Nickname = "@name", Notes = "-42" };
        var decoded = ContactCsvCodec.Import(ContactCsvCodec.Export([c])).Contacts.Single();

        Assert.AreEqual("=1+1", decoded.GivenName);
        Assert.AreEqual("+SUM(A1:A2)", decoded.FamilyName);
        Assert.AreEqual("@name", decoded.Nickname);
        Assert.AreEqual("-42", decoded.Notes);
    }

    [TestMethod]
    public void Csv_spreadsheet_export_neutralizes_formula_prefixes()
    {
        var dangerousValues = new[]
        {
            "=1+1", "+1+1", "-1+1", "@SUM(A1:A2)", "\t=1+1", "\r=1+1", "\n=1+1",
            "\uFF1D1+1", "\uFF0B1+1", "\uFF0D1+1", "\uFF20SUM(A1:A2)"
        };

        foreach (var value in dangerousValues)
        {
            var csv = ContactCsvCodec.ExportForSpreadsheet([new Contact { GivenName = value }]);
            Assert.IsTrue(csv.Contains($"\"'{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"", StringComparison.Ordinal), $"Expected spreadsheet-safe prefix for {Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(value))}.");
        }
    }

    [TestMethod]
    public void Csv_spreadsheet_export_does_not_modify_normal_text()
    {
        var c = new Contact { GivenName = "Ada", FamilyName = "Lovelace", Notes = "Normal text" };
        Assert.AreEqual(ContactCsvCodec.Export([c]), ContactCsvCodec.ExportForSpreadsheet([c]));
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
    public void Vcard_import_unfolds_continuation_lines_and_unescapes_text()
    {
        const string text = "BEGIN:VCARD\r\nVERSION:4.0\r\nN:Chen;Lin;;;\r\nNOTE:First\\nSecond\\, value\\; detail\r\n continued\r\nEND:VCARD\r\n";
        var decoded = VCardCodec.Import(text).Contacts.Single();

        Assert.AreEqual("Lin", decoded.GivenName);
        Assert.AreEqual("Chen", decoded.FamilyName);
        Assert.AreEqual("First\nSecond, value; detailcontinued", decoded.Notes);
    }

    [TestMethod]
    public void Vcard_import_warns_when_end_marker_is_missing()
    {
        var result = VCardCodec.Import("BEGIN:VCARD\nVERSION:4.0\nFN:Incomplete\n");
        Assert.AreEqual(0, result.Contacts.Count);
        Assert.AreEqual(1, result.Warnings.Count);
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

    [TestMethod]
    public void Vcard_parser_survives_random_unicode_without_throwing()
    {
        var random = new Random(4321);
        for (var n = 0; n < 250; n++)
        {
            var chars = Enumerable.Range(0, random.Next(0, 120)).Select(_ => (char)random.Next(32, 0x3000)).ToArray();
            _ = VCardCodec.Import(new string(chars));
        }
    }
}
