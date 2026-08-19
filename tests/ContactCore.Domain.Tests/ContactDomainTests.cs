using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Domain.Tests;

[TestClass]
public sealed class ContactDomainTests
{
    [TestMethod]
    public void DisplayName_UsesFullNameThenNickname()
    {
        var named = new Contact { GivenName = "Ada", FamilyName = "Lovelace", Nickname = "Enchantress" };
        var nicknamed = new Contact { Nickname = "Only Nick" };

        Assert.AreEqual("Ada Lovelace", named.DisplayName);
        Assert.AreEqual("Only Nick", nicknamed.DisplayName);
    }

    [TestMethod]
    public void Validation_RejectsMalformedEmailAndPhone()
    {
        var contact = new Contact { GivenName = "Test" };
        contact.Emails.Add(new(Guid.NewGuid(), "Home", "not-an-email"));
        contact.Phones.Add(new(Guid.NewGuid(), "Mobile", "abc"));

        var issues = ContactValidation.Validate(contact);

        Assert.AreEqual(2, issues.Count);
        CollectionAssert.Contains(issues.Select(issue => issue.Field).ToArray(), "Email");
        CollectionAssert.Contains(issues.Select(issue => issue.Field).ToArray(), "Phone");
    }

    [TestMethod]
    public void SearchKey_IsCaseAndDiacriticInsensitive()
    {
        Assert.AreEqual("jose alvarez", TextNormalizer.SearchKey("  José ÁLVAREZ  "));
    }

    [TestMethod]
    public void PhoneKey_RetainsOnlyDigits()
    {
        Assert.AreEqual("919876543210", TextNormalizer.PhoneKey("+91 (98765) 43210"));
    }

    [TestMethod]
    public void DeepCopy_DoesNotShareMutableCollections()
    {
        var original = new Contact { GivenName = "A" };
        original.Emails.Add(new(Guid.NewGuid(), "Home", "a@example.test"));

        var copy = original.DeepCopy();
        copy.Emails.Clear();
        copy.GivenName = "B";

        Assert.AreEqual("A", original.GivenName);
        Assert.AreEqual(1, original.Emails.Count);
        Assert.AreEqual(0, copy.Emails.Count);
    }
}
