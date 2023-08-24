/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Models.Entities |
  |                                                         Stage.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Entities;

[Flags]
public enum Stage
{
    NaN = 0,

    ExtensionStart          = 1 << 0, // NothingStart
    ExtensionContinue       = 1 << 1, // NothingContinue

    RetracementStart        = 1 << 2, // InitiationStart
    RetracementContinue     = 1 << 3, // InitiationContinue
    RetracementDone         = 1 << 4, // InitiationDone

    RecoveryStart           = 1 << 5, // OppositeRetracementStart
    RecoveryContinue        = 1 << 6, // OppositeRetracementContinue
    RecoveryDone            = 1 << 7, // OppositeRetracementDone

    NegativeSideWayStart    = 1 << 8, // OppositeRecoveryStart 
    NegativeSideWayContinue = 1 << 9, // OppositeRecoveryContinue
    NegativeSideWayDone     = 1 << 10, // OppositeRecoveryDone

    PositiveSideWayStart    = 1 << 11, // OppositeNegativeSideWayStart
    PositiveSideWayContinue = 1 << 12, // OppositeNegativeSideWayContinue
    PositiveSideWayDone     = 1 << 13, // OppositeNegativeSideWayDone

    Up   = 1 << 14,
    Down = 1 << 15
}