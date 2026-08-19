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
        var c = new Contact(); c.Emails.Add(new(Guid.NewGuid(), "Email", "not-an-email"));
        Assert.IsTrue(ContactValidation.Validate(c).Any(x => x.Field == "Email"));
    }

    [DataTestMethod]
    [DataRow("Élodie", "elodie")]
    [DataRow("  HELLO  ", "hello")]
    public void Search_key_is_stable(string input, string expected) => Assert.AreEqual(expected, TextNormalizer.SearchKey(input));
}
