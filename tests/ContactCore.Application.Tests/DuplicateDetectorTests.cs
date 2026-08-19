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
        var a = new Contact { GivenName = "Grace", FamilyName = "Hopper" }; a.Emails.Add(new(Guid.NewGuid(), "Work", "grace@example.test"));
        var b = new Contact { GivenName = "grace", FamilyName = "hopper" }; b.Emails.Add(new(Guid.NewGuid(), "Other", "GRACE@example.test"));
        var result = new DuplicateDetector().Compare(a, b);
        Assert.IsTrue(result.Score >= .8);
        CollectionAssert.Contains(result.Reasons.ToList(), "Shared email address");
    }

    [TestMethod]
    public void Merger_deduplicates_phone_numbers()
    {
        var a = new Contact { GivenName = "A" }; a.Phones.Add(new(Guid.NewGuid(), "Mobile", "+91 98765 43210"));
        var b = new Contact { GivenName = "A" }; b.Phones.Add(new(Guid.NewGuid(), "Other", "9876543210"));
        Assert.AreEqual(1, new ContactMerger().Merge(a, b).Phones.Count);
    }
}
