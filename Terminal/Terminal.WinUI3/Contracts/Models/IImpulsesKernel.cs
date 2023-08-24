using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Entities;

namespace Terminal.WinUI3.Contracts.Models;

public interface IImpulsesKernel : IKernel
{
    void OnInitialization(ThresholdBar firstTBar, ThresholdBar secondTBar);
    bool Add(Transition transition);
    void Save((DateTime first, DateTime second) dateRange);
    List<Impulse> GetAllImpulses(ViewPort viewPort);
}