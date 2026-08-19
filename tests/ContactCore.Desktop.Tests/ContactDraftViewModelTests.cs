using ContactCore.Desktop;
using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Desktop.Tests;

[TestClass]
public sealed class ContactDraftViewModelTests
{
    [TestMethod]
    public void Draft_round_trip_preserves_identity_archive_and_favorite_flags()
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
        Assert.IsTrue(draft.IsPersisted);
    }

    [TestMethod]
    public void New_draft_can_be_marked_unsaved_even_with_generated_contact_id()
    {
        var draft = new ContactDraftViewModel();
        draft.Load(new Contact(), isPersisted: false);

        Assert.AreNotEqual(Guid.Empty, draft.Id);
        Assert.IsFalse(draft.IsPersisted);
    }

    [TestMethod]
    public void Draft_rejects_non_iso_birthday()
    {
        var draft = new ContactDraftViewModel { BirthdayText = "19/08/2026" };

        Assert.Throws<FormatException>(() => draft.ToContact());
    }

    [TestMethod]
    public void Rich_editor_round_trip_preserves_ids_and_applies_all_supported_changes()
    {
        var phone = new ContactPhone(Guid.NewGuid(), "Mobile", "+44 20 1000 0001", ContactFieldKind.Mobile);
        var email = new ContactEmail(Guid.NewGuid(), "Personal", "first@example.test", ContactFieldKind.Home);
        var address = new ContactAddress(Guid.NewGuid(), "Home", "1 Fictional Street", "London", "London", "N1 1AA", "UK");
        var organization = new ContactOrganization(Guid.NewGuid(), "Example Org", "Engineer", "Research");
        var group = new ContactGroup(Guid.NewGuid(), "Friends");
        var tag = new ContactTag(Guid.NewGuid(), "Important");

        var source = new Contact { GivenName = "Original", FamilyName = "Person" };
        source.Phones.Add(phone);
        source.Emails.Add(email);
        source.Addresses.Add(address);
        source.Organizations.Add(organization);
        source.Groups.Add(group);
        source.Tags.Add(tag);

        var draft = new ContactDraftViewModel();
        draft.Load(source);
        draft.GivenName = "Edited";
        draft.Phones[0].Number = "  +44 20 9999 0001  ";
        draft.Phones[0].Label = " Personal ";
        draft.Emails[0].Address = "  edited@example.test  ";
        draft.Addresses[0].City = "Manchester";
        draft.Organizations[0].Title = "Principal Engineer";
        draft.Groups.Add(new GroupDraftViewModel { Name = "Project Team" });
        draft.Groups.Add(new GroupDraftViewModel { Name = "friends" });
        draft.Tags.Add(new TagDraftViewModel { Name = "Client" });

        var roundTrip = draft.ToContact();

        Assert.AreEqual("Edited", roundTrip.GivenName);
        Assert.AreEqual(phone.Id, roundTrip.Phones.Single().Id);
        Assert.AreEqual("Personal", roundTrip.Phones.Single().Label);
        Assert.AreEqual("+44 20 9999 0001", roundTrip.Phones.Single().Number);
        Assert.AreEqual(email.Id, roundTrip.Emails.Single().Id);
        Assert.AreEqual("edited@example.test", roundTrip.Emails.Single().Address);
        Assert.AreEqual(address.Id, roundTrip.Addresses.Single().Id);
        Assert.AreEqual("Manchester", roundTrip.Addresses.Single().City);
        Assert.AreEqual(organization.Id, roundTrip.Organizations.Single().Id);
        Assert.AreEqual("Principal Engineer", roundTrip.Organizations.Single().Title);
        Assert.AreEqual(2, roundTrip.Groups.Count);
        Assert.AreEqual(group.Id, roundTrip.Groups.Single(x => x.Name == "Friends").Id);
        Assert.AreEqual(2, roundTrip.Tags.Count);
        Assert.AreEqual(tag.Id, roundTrip.Tags.Single(x => x.Name == "Important").Id);

        Assert.AreEqual(phone.Number, source.Phones[0].Number, "Editing the draft must not mutate the source aggregate.");
        Assert.AreEqual(address.City, source.Addresses[0].City, "Editing the draft must not mutate address records in the source aggregate.");
    }

    [TestMethod]
    public void Renaming_existing_group_and_tag_assigns_new_shared_dictionary_identities()
    {
        var group = new ContactGroup(Guid.NewGuid(), "Friends");
        var tag = new ContactTag(Guid.NewGuid(), "Important");
        var source = new Contact { GivenName = "Rename" };
        source.Groups.Add(group);
        source.Tags.Add(tag);

        var draft = new ContactDraftViewModel();
        draft.Load(source);
        draft.Groups[0].Name = "Family";
        draft.Tags[0].Name = "Client";

        var roundTrip = draft.ToContact();

        var renamedGroup = roundTrip.Groups.Single();
        var renamedTag = roundTrip.Tags.Single();
        Assert.AreEqual("Family", renamedGroup.Name);
        Assert.AreNotEqual(group.Id, renamedGroup.Id, "A per-contact group rename must not reuse a shared dictionary ID that still belongs to the old name.");
        Assert.AreEqual("Client", renamedTag.Name);
        Assert.AreNotEqual(tag.Id, renamedTag.Id, "A per-contact tag rename must not reuse a shared dictionary ID that still belongs to the old name.");
    }

    [TestMethod]
    public void Case_only_group_and_tag_edits_preserve_existing_dictionary_identity_and_canonical_name()
    {
        var group = new ContactGroup(Guid.NewGuid(), "Friends");
        var tag = new ContactTag(Guid.NewGuid(), "Important");
        var source = new Contact { GivenName = "Case" };
        source.Groups.Add(group);
        source.Tags.Add(tag);

        var draft = new ContactDraftViewModel();
        draft.Load(source);
        draft.Groups[0].Name = "friends";
        draft.Tags[0].Name = "IMPORTANT";

        var roundTrip = draft.ToContact();

        Assert.AreEqual(group, roundTrip.Groups.Single());
        Assert.AreEqual(tag, roundTrip.Tags.Single());
    }

    [TestMethod]
    public void Group_and_tag_names_with_delimiters_round_trip_exactly()
    {
        var group = new ContactGroup(Guid.NewGuid(), "Research, Team; East");
        var tag = new ContactTag(Guid.NewGuid(), "Priority; A, B");
        var source = new Contact { GivenName = "Delimiter" };
        source.Groups.Add(group);
        source.Tags.Add(tag);

        var draft = new ContactDraftViewModel();
        draft.Load(source);
        var roundTrip = draft.ToContact();

        Assert.AreEqual(1, roundTrip.Groups.Count);
        Assert.AreEqual(group, roundTrip.Groups.Single());
        Assert.AreEqual(1, roundTrip.Tags.Count);
        Assert.AreEqual(tag, roundTrip.Tags.Single());
    }

    [TestMethod]
    public void Removing_rich_rows_removes_only_the_selected_values()
    {
        var firstPhone = new ContactPhone(Guid.NewGuid(), "Mobile", "1111111", ContactFieldKind.Mobile);
        var secondPhone = new ContactPhone(Guid.NewGuid(), "Work", "2222222", ContactFieldKind.Work);
        var firstEmail = new ContactEmail(Guid.NewGuid(), "Personal", "first@example.test", ContactFieldKind.Home);
        var secondEmail = new ContactEmail(Guid.NewGuid(), "Work", "second@example.test", ContactFieldKind.Work);
        var firstGroup = new ContactGroup(Guid.NewGuid(), "One");
        var secondGroup = new ContactGroup(Guid.NewGuid(), "Two");
        var source = new Contact { GivenName = "Multiple" };
        source.Phones.Add(firstPhone);
        source.Phones.Add(secondPhone);
        source.Emails.Add(firstEmail);
        source.Emails.Add(secondEmail);
        source.Groups.Add(firstGroup);
        source.Groups.Add(secondGroup);

        var draft = new ContactDraftViewModel();
        draft.Load(source);
        draft.Phones.RemoveAt(0);
        draft.Emails.RemoveAt(0);
        draft.Groups.RemoveAt(0);

        var roundTrip = draft.ToContact();

        Assert.AreEqual(1, roundTrip.Phones.Count);
        Assert.AreEqual(secondPhone, roundTrip.Phones.Single());
        Assert.AreEqual(1, roundTrip.Emails.Count);
        Assert.AreEqual(secondEmail, roundTrip.Emails.Single());
        Assert.AreEqual(1, roundTrip.Groups.Count);
        Assert.AreEqual(secondGroup, roundTrip.Groups.Single());
    }

    [TestMethod]
    public void Label_only_existing_address_is_preserved()
    {
        var address = new ContactAddress(Guid.NewGuid(), "Legacy label", "", "", "", "", "");
        var source = new Contact { GivenName = "Legacy" };
        source.Addresses.Add(address);

        var draft = new ContactDraftViewModel();
        draft.Load(source);
        var roundTrip = draft.ToContact();

        Assert.AreEqual(address, roundTrip.Addresses.Single());
    }

    [TestMethod]
    public void Blank_new_rich_rows_are_ignored_on_save_conversion()
    {
        var draft = new ContactDraftViewModel();
        draft.Load(new Contact(), isPersisted: false);
        draft.Phones.Add(new PhoneDraftViewModel());
        draft.Emails.Add(new EmailDraftViewModel());
        draft.Addresses.Add(new AddressDraftViewModel());
        draft.Organizations.Add(new OrganizationDraftViewModel());
        draft.Groups.Add(new GroupDraftViewModel());
        draft.Tags.Add(new TagDraftViewModel());

        var contact = draft.ToContact();

        Assert.AreEqual(0, contact.Phones.Count);
        Assert.AreEqual(0, contact.Emails.Count);
        Assert.AreEqual(0, contact.Addresses.Count);
        Assert.AreEqual(0, contact.Organizations.Count);
        Assert.AreEqual(0, contact.Groups.Count);
        Assert.AreEqual(0, contact.Tags.Count);
    }
}
