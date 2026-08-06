using MudBlazor;

namespace ChatBot.Web.Theming;

/// <summary>
/// Dark "AI workspace" palette for MudBlazor's own components (drawer, app bar, selects,
/// dialogs, chips) so they match the hand-styled message bubbles/composer in app.css instead
/// of MudBlazor's stock theme.
/// </summary>
public static class AppTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#4f46e5",
            Secondary = "#2563eb",
            AppbarBackground = "#0b1120",
            Background = "#020617",
            Surface = "#111827",
            DrawerBackground = "#0b1120",
            DrawerText = "#e2e8f0",
            TextPrimary = "#f8fafc",
            TextSecondary = "#94a3b8",
            ActionDefault = "#94a3b8",
            LinesDefault = "rgba(255,255,255,0.08)",
            TableLines = "rgba(255,255,255,0.08)",
            Divider = "rgba(255,255,255,0.08)",
            Success = "#22c55e",
            Warning = "#f59e0b",
            Error = "#ef4444",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
        },
    };
}
