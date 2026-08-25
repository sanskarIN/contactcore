using ContactCore.Desktop;

namespace ContactCore.Desktop.Tests;

[TestClass]
public sealed class ContactDraftViewModelReorderTests
{
    [TestMethod]
    public void Reorder_commands_cover_all_desktop_repeated_fields()
    {
        var draft = new ContactDraftViewModel();
        var phone1 = new PhoneDraftViewModel { Number = "1111111111" };
        var phone2 = new PhoneDraftViewModel { Number = "2222222222" };
        var email1 = new EmailDraftViewModel { Address = "first@example.test" };
        var email2 = new EmailDraftViewModel { Address = "second@example.test" };
        var address1 = new AddressDraftViewModel { City = "First" };
        var address2 = new AddressDraftViewModel { City = "Second" };
        var organization1 = new OrganizationDraftViewModel { Name = "First" };
        var organization2 = new OrganizationDraftViewModel { Name = "Second" };
        var group1 = new GroupDraftViewModel { Name = "First group" };
        var group2 = new GroupDraftViewModel { Name = "Second group" };
        var tag1 = new TagDraftViewModel { Name = "First tag" };
        var tag2 = new TagDraftViewModel { Name = "Second tag" };

        draft.Phones.Add(phone1);
        draft.Phones.Add(phone2);
        draft.Emails.Add(email1);
        draft.Emails.Add(email2);
        draft.Addresses.Add(address1);
        draft.Addresses.Add(address2);
        draft.Organizations.Add(organization1);
        draft.Organizations.Add(organization2);
        draft.Groups.Add(group1);
        draft.Groups.Add(group2);
        draft.Tags.Add(tag1);
        draft.Tags.Add(tag2);

        draft.MovePhoneDownCommand.Execute(phone1);
        draft.MoveEmailUpCommand.Execute(email2);
        draft.MoveAddressDownCommand.Execute(address1);
        draft.MoveOrganizationUpCommand.Execute(organization2);
        draft.MoveGroupDownCommand.Execute(group1);
        draft.MoveTagUpCommand.Execute(tag2);

        CollectionAssert.AreEqual(new[] { phone2, phone1 }, draft.Phones.ToArray());
        CollectionAssert.AreEqual(new[] { email2, email1 }, draft.Emails.ToArray());
        CollectionAssert.AreEqual(new[] { address2, address1 }, draft.Addresses.ToArray());
        CollectionAssert.AreEqual(new[] { organization2, organization1 }, draft.Organizations.ToArray());
        CollectionAssert.AreEqual(new[] { group2, group1 }, draft.Groups.ToArray());
        CollectionAssert.AreEqual(new[] { tag2, tag1 }, draft.Tags.ToArray());
    }

    [TestMethod]
    public void Desktop_reordering_preserves_boundaries_identity_and_saved_order()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var draft = new ContactDraftViewModel();
        var first = new PhoneDraftViewModel { Id = firstId, Number = "1111111111" };
        var second = new PhoneDraftViewModel { Id = secondId, Number = "2222222222" };
        draft.Phones.Add(first);
        draft.Phones.Add(second);

        draft.MovePhoneUpCommand.Execute(first);
        draft.MovePhoneDownCommand.Execute(second);
        CollectionAssert.AreEqual(new[] { first, second }, draft.Phones.ToArray());

        draft.MovePhoneDownCommand.Execute(first);
        var saved = draft.ToContact();

        Assert.AreEqual(secondId, saved.Phones[0].Id);
        Assert.AreEqual(firstId, saved.Phones[1].Id);
        CollectionAssert.AreEqual(new[] { "2222222222", "1111111111" }, saved.Phones.Select(x => x.Number).ToArray());
    }
}