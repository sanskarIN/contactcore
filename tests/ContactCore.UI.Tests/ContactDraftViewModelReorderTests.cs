using ContactCore.UI;

namespace ContactCore.UI.Tests;

[TestClass]
public sealed class ContactDraftViewModelReorderTests
{
    [TestMethod]
    public void Phone_commands_reorder_rows_and_preserve_boundary_items()
    {
        var draft = new ContactDraftViewModel();
        var first = new PhoneDraftViewModel { Number = "1111111111" };
        var second = new PhoneDraftViewModel { Number = "2222222222" };
        draft.Phones.Add(first);
        draft.Phones.Add(second);

        draft.MovePhoneUpCommand.Execute(first);
        CollectionAssert.AreEqual(new[] { first, second }, draft.Phones.ToArray());

        draft.MovePhoneDownCommand.Execute(first);
        CollectionAssert.AreEqual(new[] { second, first }, draft.Phones.ToArray());

        draft.MovePhoneDownCommand.Execute(first);
        CollectionAssert.AreEqual(new[] { second, first }, draft.Phones.ToArray());
    }

    [TestMethod]
    public void Email_commands_reorder_rows()
    {
        var draft = new ContactDraftViewModel();
        var first = new EmailDraftViewModel { Address = "first@example.test" };
        var second = new EmailDraftViewModel { Address = "second@example.test" };
        draft.Emails.Add(first);
        draft.Emails.Add(second);

        draft.MoveEmailUpCommand.Execute(second);

        CollectionAssert.AreEqual(new[] { second, first }, draft.Emails.ToArray());
    }

    [TestMethod]
    public void Address_commands_reorder_rows()
    {
        var draft = new ContactDraftViewModel();
        var first = new AddressDraftViewModel { City = "First" };
        var second = new AddressDraftViewModel { City = "Second" };
        draft.Addresses.Add(first);
        draft.Addresses.Add(second);

        draft.MoveAddressDownCommand.Execute(first);

        CollectionAssert.AreEqual(new[] { second, first }, draft.Addresses.ToArray());
    }

    [TestMethod]
    public void Organization_commands_reorder_rows()
    {
        var draft = new ContactDraftViewModel();
        var first = new OrganizationDraftViewModel { Name = "First" };
        var second = new OrganizationDraftViewModel { Name = "Second" };
        draft.Organizations.Add(first);
        draft.Organizations.Add(second);

        draft.MoveOrganizationUpCommand.Execute(second);

        CollectionAssert.AreEqual(new[] { second, first }, draft.Organizations.ToArray());
    }

    [TestMethod]
    public void Group_and_tag_commands_reorder_rows()
    {
        var draft = new ContactDraftViewModel();
        var firstGroup = new GroupDraftViewModel { Name = "First group" };
        var secondGroup = new GroupDraftViewModel { Name = "Second group" };
        var firstTag = new TagDraftViewModel { Name = "First tag" };
        var secondTag = new TagDraftViewModel { Name = "Second tag" };
        draft.Groups.Add(firstGroup);
        draft.Groups.Add(secondGroup);
        draft.Tags.Add(firstTag);
        draft.Tags.Add(secondTag);

        draft.MoveGroupDownCommand.Execute(firstGroup);
        draft.MoveTagUpCommand.Execute(secondTag);

        CollectionAssert.AreEqual(new[] { secondGroup, firstGroup }, draft.Groups.ToArray());
        CollectionAssert.AreEqual(new[] { secondTag, firstTag }, draft.Tags.ToArray());
    }

    [TestMethod]
    public void Reordered_rows_are_emitted_in_the_same_order_when_saved()
    {
        var draft = new ContactDraftViewModel();
        var firstPhone = new PhoneDraftViewModel { Number = "1111111111" };
        var secondPhone = new PhoneDraftViewModel { Number = "2222222222" };
        var firstEmail = new EmailDraftViewModel { Address = "first@example.test" };
        var secondEmail = new EmailDraftViewModel { Address = "second@example.test" };
        draft.Phones.Add(firstPhone);
        draft.Phones.Add(secondPhone);
        draft.Emails.Add(firstEmail);
        draft.Emails.Add(secondEmail);

        draft.MovePhoneDownCommand.Execute(firstPhone);
        draft.MoveEmailUpCommand.Execute(secondEmail);

        var contact = draft.ToContact();

        CollectionAssert.AreEqual(new[] { "2222222222", "1111111111" }, contact.Phones.Select(x => x.Number).ToArray());
        CollectionAssert.AreEqual(new[] { "second@example.test", "first@example.test" }, contact.Emails.Select(x => x.Address).ToArray());
    }
}