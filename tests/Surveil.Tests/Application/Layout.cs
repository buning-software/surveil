using WinUia.Core;

namespace Surveil.UiTests.Application;

public class Layout(Element window)
{
    private const string CaptionControlsClassName = "ReunionWindowingCaptionControls";

    protected Element Window { get; } = window;

    public Element NavigationView => Window.FindByAutomationId("NavView");

    public Element CamerasItem => Window.FindByAutomationId("CamerasGroup");

    public Element SettingsItem => Window.FindByAutomationId("SettingsItem");

    public Element CloseButton => Window
        .Find(e => e.ClassName == CaptionControlsClassName, TreeScope.Children)
        .FindByAutomationId("Close");

    public SettingsPage OpenSettings()
    {
        SettingsItem.Click();
        return new SettingsPage(Window);
    }
}
