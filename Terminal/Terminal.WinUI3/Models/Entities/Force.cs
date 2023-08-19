/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Models.Entities |
  |                                                         Force.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Entities;

[Flags]
public enum Force
{
    NaN = 0,

    Extension       = 1 << 0, 
    Retracement     = 1 << 1, 
    Recovery        = 1 << 2,
    NegativeSideWay = 1 << 3,
    PositiveSideWay = 1 << 4, 

    Nothing                 = 1 << 5,
    Initiation              = 1 << 6,
    OppositeRetracement     = 1 << 7,
    OppositeRecovery        = 1 << 8, 
    OppositeNegativeSideWay = 1 << 9,
    OppositePositiveSideWay = 1 << 10,

    Up   = 1 << 11, 
    Down = 1 << 12
}