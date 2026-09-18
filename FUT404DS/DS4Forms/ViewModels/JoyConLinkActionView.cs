using FUT404DS;
using FUT404DS.Switch2;

namespace FUT404DSWPF.DS4Forms.ViewModels;

public sealed record JoyConLinkActionView
{
    internal NintendoJoyConCandidate Candidate { get; init; }
    internal NintendoJoyConJoined Joined { get; init; }
    internal InputControllerSlotToken JoinedToken => Joined.Switch2;
    public bool Visible { get; init; }
    public bool Enabled { get; init; }
    public bool IsArmed { get; init; }
    public string Text { get; init; } = "Link";
    public string ToolTip { get; init; }
}
