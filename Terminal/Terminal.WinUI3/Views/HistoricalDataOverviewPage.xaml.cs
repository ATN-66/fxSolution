﻿/*+------------------------------------------------------------------+
  |                                             Terminal.WinUI3.Views|
  |                                    HistoricalDataOverviewPage.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class HistoricalDataOverviewPage
{
    public HistoricalDataOverviewPage()
    {
        ViewModel = App.GetService<HistoricalDataOverviewViewModel>();
        InitializeComponent();
    }

    public HistoricalDataOverviewViewModel ViewModel
    {
        get;
    }
}