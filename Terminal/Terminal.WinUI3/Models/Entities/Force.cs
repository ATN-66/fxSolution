namespace Terminal.WinUI3.Models.Entities;

public enum Force
{
    Nothing,
    Initiation, // This is the first wave of the impulse sequence and represents the beginning of a new trend.
    Retracement, // This is a pullback or correction of the trend.
    Recovery, // from the end of the retracement to the start of the extension
    Extension, // This is the resumption of the trend.
    NegativeSideWay, // This is a sideways movement that is negative in nature.
    PositiveSideWay // This is a sideways movement that is positive in nature.
}