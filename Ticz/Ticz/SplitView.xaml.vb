' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Windows.UI.Core

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class SplitView
    Inherits Page

    Private app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
    'Dim vm As TiczViewModel = app.myViewModel

    Public Sub New()
        InitializeComponent()
    End Sub

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        '        RemoveHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf app.App_BackRequested
        If e.NavigationMode = NavigationMode.New Then
            AddHandler SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf BackButtonPressed
        End If
        '        Dim rootFrame As Frame = CType(Window.Current.Content, Frame)
        'If rootFrame.CanGoBack Then
        'SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
        'Else
        'SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
        'End If

        Me.DataContext = app.myViewModel
    End Sub


    Public Sub BackButtonPressed(sender As Object, e As Windows.UI.Core.BackRequestedEventArgs)

        'Dim a = app.myViewModel.GoBackCommand
        'a.Execute(e)
        'e.Handled = True
        WriteToDebug("App.BackButtonPressed", "executed")
        If app.myViewModel.ShowDeviceGraph Then
            e.Handled = True
            app.myViewModel.GraphList.Dispose()
            app.myViewModel.ShowDeviceGraph = False
            Exit Sub
        End If
        If app.myViewModel.TiczMenu.ShowSecurityPanel Then e.Handled = True : app.myViewModel.TiczMenu.ShowSecurityPanel = False : Exit Sub
        If app.myViewModel.TiczMenu.ShowAbout Then e.Handled = True : app.myViewModel.TiczMenu.ShowAbout = False : Exit Sub
        If app.myViewModel.TiczMenu.IsMenuOpen Then e.Handled = True : Dim cmd = app.myViewModel.TiczMenu.SettingsMenuGoBack : cmd.Execute(Nothing) : Exit Sub
        If Not app.myViewModel.TiczMenu.IsMenuOpen Then SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
    End Sub


    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        WriteToDebug("App.OnNavigatedFrom", "executed")
        app.myViewModel.StopRefresh()
        'RemoveHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf app.App_BackRequested
    End Sub

    Private Sub GridView_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        WriteToDebug("MainPage.GridView_SizeChanged()", "executed")
        Dim gv As GridView = CType(sender, GridView)
        Dim Panel = CType(gv.ItemsPanelRoot, ItemsWrapGrid)
        Dim amountOfColumns = Math.Ceiling(gv.ActualWidth / 400)
        If amountOfColumns < app.myViewModel.TiczSettings.MinimumNumberOfColumns Then amountOfColumns = app.myViewModel.TiczSettings.MinimumNumberOfColumns
        Panel.ItemWidth = e.NewSize.Width / amountOfColumns
        WriteToDebug("Panel Width = ", Panel.ItemWidth)
    End Sub

End Class
