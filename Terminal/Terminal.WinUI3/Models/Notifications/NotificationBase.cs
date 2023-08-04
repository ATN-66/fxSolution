/*+------------------------------------------------------------------+
  |                              Terminal.WinUI3.Models.Notifications|
  |                                              NotificationBase.cs |
  +------------------------------------------------------------------+*/

using System.Numerics;
using Common.Entities;

namespace Terminal.WinUI3.Models.Notifications;

public abstract class NotificationBase
{
    public Symbol Symbol;
    public NotificationType Type;
    public required string Description;
    public bool IsSelected;
    public Vector2 StartPoint;
    public Vector2 EndPoint;
}