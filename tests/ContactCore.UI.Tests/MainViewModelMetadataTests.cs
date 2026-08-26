using ContactCore.UI;

namespace ContactCore.UI.Tests;

[TestClass]
public sealed class MainViewModelMetadataTests
{
    [TestMethod]
    public void About_summary_uses_the_built_ui_assembly_version()
    {
        var viewModel = new MainViewModel(UiTestServices.Create(new RecordingContactRepository()));
        var version = typeof(MainViewModel).Assembly.GetName().Version?.ToString(3);

        Assert.IsFalse(string.IsNullOrWhiteSpace(version));
        Assert.IsTrue(
            viewModel.AboutSummary.StartsWith($"ContactCore {version}", StringComparison.Ordinal),
            $"Expected About summary to begin with the built assembly version '{version}', but was '{viewModel.AboutSummary}'.");
    }
}