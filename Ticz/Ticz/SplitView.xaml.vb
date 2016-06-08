Imports Windows.UI.Core

Public NotInheritable Class SplitView
    Inherits Page

    Private vm As TiczViewModel = CType(Windows.UI.Xaml.Application.Current, Application).myViewModel

    Public Sub New()
        InitializeComponent()
    End Sub

    Protected Overrides Async Sub OnNavigatedTo(e As NavigationEventArgs)
        If e.NavigationMode = NavigationMode.New Then
            AddHandler SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf BackButtonPressed
        End If
        Await vm.Load()
        Me.DataContext = vm
    End Sub


    Public Sub BackButtonPressed(sender As Object, e As Windows.UI.Core.BackRequestedEventArgs)
        WriteToDebug("App.BackButtonPressed", "executed")
        If vm.TiczMenu.IsMenuOpen Then
            vm.TiczMenu.MenuGoBack()
            e.Handled = True
            Exit Sub
        End If
    End Sub


    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        WriteToDebug("App.OnNavigatedFrom", "executed")
        vm.StopRefresh()
    End Sub

    Private Sub mainGrid_Tapped(sender As Object, e As TappedRoutedEventArgs)
        If vm.TiczMenu.IsMenuOpen Then vm.TiczMenu.IsMenuOpen = False
    End Sub
End Class
