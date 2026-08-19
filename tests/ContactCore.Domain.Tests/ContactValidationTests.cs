using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Domain.Tests;

[TestClass]
public sealed class ContactValidationTests
{
    [TestMethod]
    public void Valid_contact_has_no_issues()
    {
        var c = new Contact { GivenName = "Ada", FamilyName = "Lovelace" };
        c.Emails.Add(new(Guid.NewGuid(), "Work", "ada@example.test"));
        c.Phones.Add(new(Guid.NewGuid(), "Mobile", "+44 20 1234 5678"));
        Assert.AreEqual(0, ContactValidation.Validate(c).Count);
    }

    [TestMethod]
    public void Invalid_email_is_reported()
    {
        var c = new Contact();
        c.Emails.Add(new(Guid.NewGuid(), "Email", "not-an-email"));
        Assert.IsTrue(ContactValidation.Validate(c).Any(x => x.Field == "Email"));
    }

    [TestMethod]
    public void Validation_messages_do_not_echo_contact_values()
    {
        const string email = "private-invalid-email";
        const string phone = "private-invalid-phone";
        var c = new Contact();
        c.Emails.Add(new(Guid.NewGuid(), "Email", email));
        c.Phones.Add(new(Guid.NewGuid(), "Mobile", phone));

        var issues = ContactValidation.Validate(c);
        var combined = string.Join(" ", issues.Select(issue => issue.Message));

        Assert.IsFalse(combined.Contains(email, StringComparison.Ordinal));
        Assert.IsFalse(combined.Contains(phone, StringComparison.Ordinal));
    }

    [TestMethod]
    public void Name_and_notes_length_boundaries_are_enforced_exactly()
    {
        var atLimit = new Contact
        {
            GivenName = new string('A', 120),
            FamilyName = new string('B', 120),
            Nickname = new string('C', 120),
            Notes = new string('N', 20_000)
        };
        Assert.AreEqual(0, ContactValidation.Validate(atLimit).Count);

        var overLimit = new Contact
        {
            GivenName = new string('A', 121),
            FamilyName = new string('B', 121),
            Nickname = new string('C', 121),
            Notes = new string('N', 20_001)
        };
        var fields = ContactValidation.Validate(overLimit).Select(x => x.Field).ToArray();
        CollectionAssert.AreEquivalent(new[] { "GivenName", "FamilyName", "Nickname", "Notes" }, fields);
    }

    [TestMethod]
    public void Deep_copy_keeps_identity_values_but_owns_independent_child_lists()
    {
        var source = new Contact { GivenName = "Source" };
        var phone = new ContactPhone(Guid.NewGuid(), "Mobile", "1234567", ContactFieldKind.Mobile);
        var address = new ContactAddress(Guid.NewGuid(), "Home", "1 Example Road", "Example City", "Region", "100001", "Exampleland");
        source.Phones.Add(phone);
        source.Addresses.Add(address);

        var copy = source.DeepCopy();
        copy.GivenName = "Copy";
        copy.Phones.Clear();
        copy.Addresses.Clear();

        Assert.AreEqual(source.Id, copy.Id);
        Assert.AreEqual(source.CreatedAt, copy.CreatedAt);
        Assert.AreEqual("Source", source.GivenName);
        Assert.AreEqual(1, source.Phones.Count);
        Assert.AreEqual(phone, source.Phones.Single());
        Assert.AreEqual(1, source.Addresses.Count);
        Assert.AreEqual(address, source.Addresses.Single());
    }

    [TestMethod]
    public void Display_name_prefers_full_name_then_nickname_then_fallback()
    {
        Assert.AreEqual("Ada Lovelace", new Contact { GivenName = "Ada", FamilyName = "Lovelace", Nickname = "Countess" }.DisplayName);
        Assert.AreEqual("Countess", new Contact { Nickname = "Countess" }.DisplayName);
        Assert.AreEqual("Unnamed contact", new Contact().DisplayName);
    }

    [DataTestMethod]
    [DataRow("Élodie", "elodie")]
    [DataRow("  HELLO  ", "hello")]
    public void Search_key_is_stable(string input, string expected) => Assert.AreEqual(expected, TextNormalizer.SearchKey(input));

    [DataTestMethod]
    [DataRow("+91 (999) 123-4567", "919991234567")]
    [DataRow(" 0044.20.1234 5678 ", "00442012345678")]
    public void Phone_key_keeps_digits_only(string input, string expected) => Assert.AreEqual(expected, TextNormalizer.PhoneKey(input));
}
