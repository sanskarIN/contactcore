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

    [TestMethod]
    public void Draft_edit_preserves_unexposed_rich_fields_and_additional_values()
    {
        var firstPhone = new ContactPhone(Guid.NewGuid(), "Mobile", "+44 20 1000 0001", ContactFieldKind.Mobile);
        var secondPhone = new ContactPhone(Guid.NewGuid(), "Work", "+44 20 1000 0002", ContactFieldKind.Work);
        var firstEmail = new ContactEmail(Guid.NewGuid(), "Personal", "first@example.test", ContactFieldKind.Home);
        var secondEmail = new ContactEmail(Guid.NewGuid(), "Work", "second@example.test", ContactFieldKind.Work);
        var address = new ContactAddress(Guid.NewGuid(), "Home", "1 Fictional Street", "London", "London", "N1 1AA", "UK");
        var organization = new ContactOrganization(Guid.NewGuid(), "Example Org", "Engineer", "Research");
        var group = new ContactGroup(Guid.NewGuid(), "Friends");
        var tag = new ContactTag(Guid.NewGuid(), "Important");

        var source = new Contact { GivenName = "Original", FamilyName = "Person" };
        source.Phones.Add(firstPhone);
        source.Phones.Add(secondPhone);
        source.Emails.Add(firstEmail);
        source.Emails.Add(secondEmail);
        source.Addresses.Add(address);
        source.Organizations.Add(organization);
        source.Groups.Add(group);
        source.Tags.Add(tag);

        var draft = new ContactDraftViewModel();
        draft.Load(source);
        draft.GivenName = "Edited";
        draft.Phone = "  +44 20 9999 0001  ";
        draft.Email = "  edited@example.test  ";

        var roundTrip = draft.ToContact();

        Assert.AreEqual("Edited", roundTrip.GivenName);
        Assert.AreEqual(2, roundTrip.Phones.Count);
        Assert.AreEqual(firstPhone.Id, roundTrip.Phones[0].Id);
        Assert.AreEqual(firstPhone.Label, roundTrip.Phones[0].Label);
        Assert.AreEqual(firstPhone.Kind, roundTrip.Phones[0].Kind);
        Assert.AreEqual("+44 20 9999 0001", roundTrip.Phones[0].Number);
        Assert.AreEqual(secondPhone, roundTrip.Phones[1]);

        Assert.AreEqual(2, roundTrip.Emails.Count);
        Assert.AreEqual(firstEmail.Id, roundTrip.Emails[0].Id);
        Assert.AreEqual(firstEmail.Label, roundTrip.Emails[0].Label);
        Assert.AreEqual(firstEmail.Kind, roundTrip.Emails[0].Kind);
        Assert.AreEqual("edited@example.test", roundTrip.Emails[0].Address);
        Assert.AreEqual(secondEmail, roundTrip.Emails[1]);

        Assert.AreEqual(address, roundTrip.Addresses.Single());
        Assert.AreEqual(organization, roundTrip.Organizations.Single());
        Assert.AreEqual(group, roundTrip.Groups.Single());
        Assert.AreEqual(tag, roundTrip.Tags.Single());

        Assert.AreEqual(firstPhone.Number, source.Phones[0].Number, "Editing the draft must not mutate the loaded source aggregate.");
        Assert.AreEqual(firstEmail.Address, source.Emails[0].Address, "Editing the draft must not mutate the loaded source aggregate.");
    }

    [TestMethod]
    public void Clearing_primary_phone_and_email_preserves_additional_values()
    {
        var firstPhone = new ContactPhone(Guid.NewGuid(), "Mobile", "1111111", ContactFieldKind.Mobile);
        var secondPhone = new ContactPhone(Guid.NewGuid(), "Work", "2222222", ContactFieldKind.Work);
        var firstEmail = new ContactEmail(Guid.NewGuid(), "Personal", "first@example.test", ContactFieldKind.Home);
        var secondEmail = new ContactEmail(Guid.NewGuid(), "Work", "second@example.test", ContactFieldKind.Work);
        var source = new Contact { GivenName = "Multiple" };
        source.Phones.Add(firstPhone);
        source.Phones.Add(secondPhone);
        source.Emails.Add(firstEmail);
        source.Emails.Add(secondEmail);

        var draft = new ContactDraftViewModel();
        draft.Load(source);
        draft.Phone = "";
        draft.Email = "";

        var roundTrip = draft.ToContact();

        Assert.AreEqual(1, roundTrip.Phones.Count);
        Assert.AreEqual(secondPhone, roundTrip.Phones.Single());
        Assert.AreEqual(1, roundTrip.Emails.Count);
        Assert.AreEqual(secondEmail, roundTrip.Emails.Single());
    }
}
