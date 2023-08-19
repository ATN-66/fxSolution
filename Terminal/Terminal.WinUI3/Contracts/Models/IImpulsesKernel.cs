using Terminal.WinUI3.Models.Entities;

namespace Terminal.WinUI3.Contracts.Models;

public interface IImpulsesKernel : IKernel
{
    void OnInitialization(ThresholdBar firstTBar, ThresholdBar secondTBar);
    bool Add(Transition transition);
    void SaveTransitions();
}