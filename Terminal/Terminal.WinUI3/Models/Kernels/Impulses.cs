/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.Models.Kernels|
  |                                                      Impulses.cs |
  +------------------------------------------------------------------+*/

using System.Diagnostics;
using Common.Entities;
using Microsoft.UI.Xaml.Media.Animation;
using Terminal.WinUI3.Contracts.Models;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Chart;
using Terminal.WinUI3.Models.Entities;
using Transition = Terminal.WinUI3.Models.Entities.Transition;

namespace Terminal.WinUI3.Models.Kernels;

public class Impulses : IImpulsesKernel
{
    private readonly Symbol _symbol;
    private int _impID;
    
    private readonly IFileService _fileService;
    private const string FolderPath = @"D:\forex.Terminal.WinUI3.Logs\";//todo: move to config

    private readonly List<Transition> _transitions = new();
    private readonly List<Impulse> _impulses = new();

    public Impulses(Symbol symbol, IFileService fileService)
    {
        _symbol = symbol;
        _fileService = fileService;
    }

    public void OnInitialization(ThresholdBar firstTBar, ThresholdBar secondTBar)
    {
        Debug.Assert(firstTBar.Close.Equals(secondTBar.Open));

        Debug.Assert(firstTBar.Start < firstTBar.End);
        Debug.Assert(secondTBar.Start < secondTBar.End);
        Debug.Assert(firstTBar.End.Equals(secondTBar.Start));

        if (firstTBar.Length >= secondTBar.Length)
        {
            _impulses.Add(new Impulse(++_impID, firstTBar.Open, firstTBar.Close, firstTBar.Start, firstTBar.End, firstTBar.Direction) { IsLeader = true });
            _impulses.Add(new Impulse(++_impID, secondTBar.Open, secondTBar.Close, firstTBar.End, secondTBar.End, secondTBar.Direction) { IsLeader = false });
        }
        else
        {
            throw new NotImplementedException("Impulses:OnInitialization");
        }
    }

    public bool Add(Transition transition)
    {
        _transitions.Add(transition);
        var impulse = _impulses[^2];
        var oppositeImpulse = _impulses[^1];

        switch (impulse.Direction)
        {
            case Direction.Up:
                switch (transition.Stage)
                {
                    case Stage.RetracementStart | Stage.Up:
                    case Stage.RetracementContinue | Stage.Up:
                    case Stage.RetracementDone | Stage.Up:
                        oppositeImpulse.Close = transition.Close;
                        oppositeImpulse.End = transition.End;
                        break;

                    case Stage.NegativeSideWayStart | Stage.Up: 
                    case Stage.NegativeSideWayContinue | Stage.Up: break;
                    case Stage.NegativeSideWayDone | Stage.Up:
                        if (transition.Close.Equals(oppositeImpulse.Close))
                        {
                            oppositeImpulse.End = transition.End;
                        }
                        break;

                    case Stage.PositiveSideWayStart | Stage.Up:
                    case Stage.PositiveSideWayContinue | Stage.Up:
                    case Stage.PositiveSideWayDone | Stage.Up: break;

                    case Stage.RecoveryStart | Stage.Up: 
                    case Stage.RecoveryContinue | Stage.Up:
                    case Stage.RecoveryDone | Stage.Up: break;

                    case Stage.ExtensionStart | Stage.Up:
                    case Stage.ExtensionContinue | Stage.Up:
                        if (transition.Close >= impulse.Close)
                        {
                            oppositeImpulse.Open = oppositeImpulse.Close = impulse.Close = transition.Close;
                            oppositeImpulse.Start = oppositeImpulse.End = impulse.End = transition.End;
                        }
                        break;
                    case Stage.ExtensionStart | Stage.Down:
                        impulse.IsLeader = false;
                        oppositeImpulse.IsLeader = true;
                        oppositeImpulse.Close = transition.Close;
                        oppositeImpulse.End = transition.End;
                        _impulses.Add(new Impulse(++_impID, transition.Close, transition.Close, transition.End, transition.End, oppositeImpulse.OppositeDirection) { IsLeader = false });
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
                break;
            case Direction.Down:
                switch (transition.Stage)
                {
                    case Stage.RetracementStart | Stage.Down:
                    case Stage.RetracementContinue | Stage.Down:
                    case Stage.RetracementDone | Stage.Down:
                        oppositeImpulse.Close = transition.Close;
                        oppositeImpulse.End = transition.End;
                        break;

                    case Stage.NegativeSideWayStart | Stage.Down:
                    case Stage.NegativeSideWayContinue | Stage.Down: break;
                    case Stage.NegativeSideWayDone | Stage.Down:
                        if (transition.Close.Equals(oppositeImpulse.Close))
                        {
                            oppositeImpulse.End = transition.End;
                        }
                        break;

                    case Stage.PositiveSideWayStart | Stage.Down:
                    case Stage.PositiveSideWayContinue | Stage.Down:
                    case Stage.PositiveSideWayDone | Stage.Down: break;

                    case Stage.RecoveryStart | Stage.Down: 
                    case Stage.RecoveryContinue | Stage.Down: 
                    case Stage.RecoveryDone | Stage.Down: break;

                    case Stage.ExtensionStart | Stage.Down:
                    case Stage.ExtensionContinue | Stage.Down:
                        if (transition.Close <= impulse.Close)
                        {
                            oppositeImpulse.Open = oppositeImpulse.Close = impulse.Close = transition.Close;
                            oppositeImpulse.Start = oppositeImpulse.End = impulse.End = transition.End;
                        }
                        break;
                    case Stage.ExtensionStart | Stage.Up:
                        impulse.IsLeader = false;
                        oppositeImpulse.IsLeader = true;
                        oppositeImpulse.Close = transition.Close;
                        oppositeImpulse.End = transition.End;
                        _impulses.Add(new Impulse(++_impID, transition.Close, transition.Close, transition.End, transition.End, oppositeImpulse.OppositeDirection) { IsLeader = false });
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
                break;
            case Direction.NaN:
        default: throw new ArgumentOutOfRangeException();
        }

        return false;
    }

    public List<Impulse> GetAllImpulses(ViewPort viewPort)
    {
        var impulses = _impulses.Where(impulse => impulse.Start >= viewPort.Start && impulse.End <= viewPort.End).ToList();
        return impulses;
    }

    public void Save((DateTime first, DateTime second) dateRange)
    {
        var selectedTransitions = _transitions.Where(t => t.End >= dateRange.first && t.End <= dateRange.second);
        _fileService.Save(FolderPath, $"{_symbol}_transitions.json", selectedTransitions);

        var selectedImpulses = _impulses.Where(t => t.Start >= dateRange.first && t.End <= dateRange.second);
        _fileService.Save(FolderPath, $"{_symbol}_impulses.json", selectedImpulses);
    }
}