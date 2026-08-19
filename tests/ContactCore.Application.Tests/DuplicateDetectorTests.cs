using ContactCore.Application;
using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Application.Tests;

[TestClass]
public sealed class DuplicateDetectorTests
{
    [TestMethod]
    public void Shared_email_and_name_is_high_confidence()
    {
        var a = new Contact { GivenName = "Grace", FamilyName = "Hopper" };
        a.Emails.Add(new(Guid.NewGuid(), "Work", "grace@example.test"));
        var b = new Contact { GivenName = "grace", FamilyName = "hopper" };
        b.Emails.Add(new(Guid.NewGuid(), "Other", "GRACE@example.test"));

        var result = new DuplicateDetector().Compare(a, b);

        Assert.IsTrue(result.Score >= .8);
        CollectionAssert.Contains(result.Reasons.ToList(), "Shared email address");
    }

    [TestMethod]
    public void Find_uses_shared_identity_keys_without_emitting_unrelated_contacts()
    {
        var contacts = Enumerable.Range(0, 2_000)
            .Select(i => new Contact { GivenName = "Person", FamilyName = $"{i:D4}" })
            .ToList();
        contacts[100].Emails.Add(new(Guid.NewGuid(), "Work", "same@example.test"));
        contacts[1700].Emails.Add(new(Guid.NewGuid(), "Home", "SAME@example.test"));

        var results = new DuplicateDetector().Find(contacts, .40);

        Assert.AreEqual(1, results.Count);
        CollectionAssert.AreEquivalent(
            new[] { contacts[100].Id, contacts[1700].Id },
            new[] { results[0].Left.Id, results[0].Right.Id });
    }

    [TestMethod]
    public void Birthday_only_candidate_can_be_found_at_low_threshold()
    {
        var birthday = new DateOnly(1999, 12, 31);
        var a = new Contact { GivenName = "Alpha", Birthday = birthday };
        var b = new Contact { GivenName = "Beta", Birthday = birthday };

        var result = new DuplicateDetector().Find([a, b], .10).Single();

        Assert.AreEqual(.10, result.Score, .0001);
    }

    [TestMethod]
    public void Invalid_threshold_is_rejected()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new DuplicateDetector().Find([], 1.01));
    }

    [TestMethod]
    public void Merger_deduplicates_phone_numbers()
    {
        var a = new Contact { GivenName = "A" };
        a.Phones.Add(new(Guid.NewGuid(), "Mobile", "+91 98765 43210"));
        var b = new Contact { GivenName = "A" };
        b.Phones.Add(new(Guid.NewGuid(), "Other", "9876543210"));

        Assert.AreEqual(1, new ContactMerger().Merge(a, b).Phones.Count);
    }
}
