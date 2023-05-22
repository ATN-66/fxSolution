﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Core.Models;

namespace Terminal.WinUI3.Views;

public sealed partial class ListDetailsDetailControl : UserControl
{
    public static readonly DependencyProperty ListDetailsMenuItemProperty =
        DependencyProperty.Register("ListDetailsMenuItem", typeof(SampleOrder), typeof(ListDetailsDetailControl),
            new PropertyMetadata(null, OnListDetailsMenuItemPropertyChanged));

    public ListDetailsDetailControl()
    {
        InitializeComponent();
    }

    public SampleOrder? ListDetailsMenuItem
    {
        get => GetValue(ListDetailsMenuItemProperty) as SampleOrder;
        set => SetValue(ListDetailsMenuItemProperty, value);
    }

    private static void OnListDetailsMenuItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListDetailsDetailControl control)
        {
            control.ForegroundElement.ChangeView(0, 0, 1);
        }
    }
}