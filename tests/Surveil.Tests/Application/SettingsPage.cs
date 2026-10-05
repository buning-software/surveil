using System;
using System.Linq;
using WinUia.Core;

namespace Surveil.UiTests.Application;

public sealed class SettingsPage(Element window) : Layout(window)
{
    private static readonly TimeSpan AbsentTimeout = TimeSpan.FromSeconds(1);

    public Element Title => Window.Find(e => e.ControlType == ControlType.Text && e.Name == "Settings");

    public Element LaunchOnStartupToggle => Window.FindByAutomationId("LaunchOnStartupToggle");

    public Element ProviderComboBox => Window.FindByAutomationId("ProviderComboBox");

    public Element BaseUrlTextBox => Window.FindByAutomationId("BaseUrlTextBox");

    public Element ApiKeyTextBox => Window.FindByAutomationId("ApiKeyTextBox");

    public Element SnapshotPathTextBox => Window.FindByAutomationId("SnapshotPathTextBox");

    public bool IsUnifiConnectionShown => Window.TryFindByAutomationId("BaseUrlTextBox", AbsentTimeout) is not null;

    public string? ErrorTitle => ErrorInfoBar?.FindByAutomationId("Title").Name;

    public string? ErrorMessage => ErrorInfoBar?.FindByAutomationId("Message").Name;

    private Element? ErrorInfoBar => Window.TryFindByAutomationId("SettingsErrorInfoBar", AbsentTimeout);

    public Element SectionHeader(string text) => Window.Find(e => e.ControlType == ControlType.Text && e.Name == text);

    public void SelectProvider(string displayName)
    {
        ProviderComboBox.Expand();
        ProviderItem(displayName).Select();
    }

    public string ReadSelectedProvider()
    {
        ProviderComboBox.Expand();
        ProviderComboBox.Find(e => e.ControlType == ControlType.ListItem);

        var selected = ProviderComboBox
            .FindAll(e => e.ControlType == ControlType.ListItem)
            .Single(item => item.SelectionItemPattern.IsSelected)
            .Name;

        ProviderComboBox.Collapse();
        return selected;
    }

    public void EnterBaseUrl(string baseUrl) => EnterAndCommit(BaseUrlTextBox, baseUrl);

    public void EnterApiKey(string apiKey) => EnterAndCommit(ApiKeyTextBox, apiKey);

    public void EnterUnifiConnection(string baseUrl, string apiKey)
    {
        EnterBaseUrl(baseUrl);
        EnterApiKey(apiKey);
    }

    private void EnterAndCommit(Element textBox, string value)
    {
        textBox.Focus();
        textBox.SetValue(value);
        SnapshotPathTextBox.Focus();
    }

    private Element ProviderItem(string displayName) =>
        ProviderComboBox.Find(e => e.ControlType == ControlType.ListItem && e.Name == displayName);
}
