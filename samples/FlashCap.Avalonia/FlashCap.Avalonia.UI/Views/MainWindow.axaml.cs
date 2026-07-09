////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlashCap.Avalonia.ViewModels;
using System;

namespace FlashCap.Avalonia.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.Opened += this.OnOpened;
    }

    private void InitializeComponent() =>
        AvaloniaXamlLoader.Load(this);

    private void OnOpened(object? sender, EventArgs e)
    {
        if (this.DataContext is MainWindowViewModel { Opened: { } opened } &&
            opened.CanExecute(null))
        {
            opened.Execute(null);
        }
    }
}
