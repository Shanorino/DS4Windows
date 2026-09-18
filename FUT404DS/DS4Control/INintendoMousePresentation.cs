using FUT404DS.Switch2;

namespace FUT404DS;

// Capability of the canonical mapper's output owner, not a transport/model test.
internal interface INintendoMousePresentation
{
    bool TrySetHighRateMouseSource(Switch2ContinuousMouseSource source, bool active,
        double velocityX, double velocityY, long profileRevision);
    bool TrySetHighRateMappingMouseSources(bool stickAssistActive, double stickAssistVelocityX,
        double stickAssistVelocityY, bool irActive, double irVelocityX, double irVelocityY,
        bool mappedStickActive, double mappedStickVelocityX, double mappedStickVelocityY, long profileRevision);
}
