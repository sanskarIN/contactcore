using ContactCore.Desktop;
using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Desktop.Tests;

[TestClass]
public sealed class ContactDraftViewModelTests
{
    [TestMethod]
    public void Draft_round_trip_preserves_archive_and_favorite_flags()
    {
        var source = new Contact
        {
            GivenName = "Archive",
            FamilyName = "Example",
            IsFavorite = true,
            IsArchived = true,
            CreatedAt = DateTimeOffset.Parse("2026-08-19T00:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture)
        };

        var draft = new ContactDraftViewModel();
        draft.Load(source);
        var roundTrip = draft.ToContact();

        Assert.AreEqual(source.Id, roundTrip.Id);
        Assert.AreEqual(source.CreatedAt, roundTrip.CreatedAt);
        Assert.IsTrue(roundTrip.IsFavorite);
        Assert.IsTrue(roundTrip.IsArchived);
    }

    [TestMethod]
    public void Draft_rejects_non_iso_birthday()
    {
        var draft = new ContactDraftViewModel { BirthdayText = "19/08/2026" };

        Assert.Throws<FormatException>(() => draft.ToContact());
    }
}
