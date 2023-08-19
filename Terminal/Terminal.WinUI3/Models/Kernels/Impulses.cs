using System.Diagnostics;
using Common.Entities;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Entities;

namespace Terminal.WinUI3.Models.Kernels;

public class Impulses : IImpulsesKernel
{
    private readonly Symbol _symbol;
    private int _impID;
    
    private readonly IFileService _fileService;
    private const string FolderPath = @"D:\forex.Terminal.WinUI3.Logs\";//todo: move to config

    private readonly List<Transition> _transitions = new();
    private readonly List<Impulse> _impulses = new();

    //private bool _stop;

    public Impulses(Symbol symbol, IFileService fileService)
    {
        _symbol = symbol;
        _fileService = fileService;
    }

    public void OnInitialization(ThresholdBar firstTBar, ThresholdBar secondTBar)
    {
        return;

        //if (firstTBar.Length >= secondTBar.Length)
        //{
        //    _impulses.Add(new Impulse(++_impID, firstTBar.Open, firstTBar.Close, firstTBar.Start, firstTBar.End, firstTBar.Direction) { IsLeader = true});
        //    _impulses.Add(new Impulse(++_impID, secondTBar.Open, secondTBar.Close, secondTBar.Start, secondTBar.End, secondTBar.Direction) { IsLeader = false });
        //}
        //else
        //{
        //    throw new NotImplementedException("Impulses:OnInitialization");
        //}
    }

    public bool Add(Transition transition)
    {
        return false;

        //if (_symbol != Symbol.EURUSD || _stop) return true;

        //_transitions.Add(transition);
        //var impulse = _impulses[^2];
        //var oppositeImpulse = _impulses[^1];

        ////switch (transition.Type)
        ////{
        ////    default: throw new ArgumentOutOfRangeException();
        ////}

        //return false;
    }

    public void SaveTransitions()
    {
        _fileService.Save(FolderPath, $"{_symbol}_tbars_transitions.json", _transitions);
    }
}