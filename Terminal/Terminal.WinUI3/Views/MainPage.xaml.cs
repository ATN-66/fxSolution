﻿using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }

    public MainViewModel ViewModel
    {
        get;
    }
}