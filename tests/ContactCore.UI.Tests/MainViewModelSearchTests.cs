using ContactCore.Domain;
using ContactCore.UI;

namespace ContactCore.UI.Tests;

[TestClass]
public sealed class MainViewModelSearchTests
{
    [TestMethod]
    public async Task Rapid_search_changes_are_debounced_to_the_latest_query()
    {
        var repository = new RecordingContactRepository();
        repository.Seed(new Contact { GivenName = "Fast", FamilyName = "Result" });
        var viewModel = new MainViewModel(UiTestServices.Create(repository));

        viewModel.SearchText = "f";
        await Task.Delay(25).ConfigureAwait(false);
        viewModel.SearchText = "fa";
        await Task.Delay(25).ConfigureAwait(false);
        viewModel.SearchText = "fast";

        await WaitUntilAsync(
            () => repository.SearchQueries.Any(query => query.Search == "fast") && viewModel.Contacts.Count == 1,
            TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        var searches = repository.SearchQueries.Select(query => query.Search).ToArray();
        CollectionAssert.AreEqual(new[] { "fast" }, searches);
        Assert.AreEqual("Fast Result", viewModel.Contacts.Single().DisplayName);
    }

    [TestMethod]
    public async Task New_search_cancels_in_flight_older_search_before_latest_results_are_applied()
    {
        var repository = new RecordingContactRepository();
        repository.Seed(
            new Contact { GivenName = "Slow", FamilyName = "Result" },
            new Contact { GivenName = "Fast", FamilyName = "Result" });
        var viewModel = new MainViewModel(UiTestServices.Create(repository));

        viewModel.SearchText = "slow";
        await repository.SlowSearchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        viewModel.SearchText = "fast";
        await WaitUntilAsync(
            () => repository.CancelledSearches == 1 &&
                  viewModel.Contacts.Count == 1 &&
                  viewModel.Contacts[0].DisplayName == "Fast Result",
            TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        Assert.AreEqual(1, repository.CancelledSearches);
        CollectionAssert.AreEqual(
            new[] { "slow", "fast" },
            repository.SearchQueries.Select(query => query.Search).ToArray());
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
                Assert.Fail("The expected asynchronous view-model state was not reached before the test deadline.");
            await Task.Delay(20).ConfigureAwait(false);
        }
    }
}
