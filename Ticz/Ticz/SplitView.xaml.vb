' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Windows.UI.Core

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class SplitView
    Inherits Page

    Dim app As App = CType(Application.Current, App)
    'Dim vm As TiczViewModel = app.myViewModel

    Public Sub New()
        InitializeComponent()
    End Sub

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        RemoveHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf app.App_BackRequested
        AddHandler SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf BackButtonPressed

        Dim rootFrame As Frame = CType(Window.Current.Content, Frame)
        'If rootFrame.CanGoBack Then
        'SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
        'Else
        'SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
        'End If

        Me.DataContext = app.myViewModel
    End Sub


    Public Sub BackButtonPressed(sender As Object, e As Windows.UI.Core.BackRequestedEventArgs)
        WriteToDebug("App.BackButtonPressed", "executed")
        If TiczViewModel.TiczMenu.ShowAbout = True Then
            e.Handled = True
            TiczViewModel.TiczMenu.ShowAbout = False
            Exit Sub
        End If
        If TiczViewModel.TiczMenu.IsMenuOpen Then
            e.Handled = True
            Dim cmd = TiczViewModel.TiczMenu.SettingsMenuGoBack
            cmd.Execute(Nothing)
            If Not TiczViewModel.TiczMenu.IsMenuOpen Then SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
        End If

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
        If amountOfColumns < TiczViewModel.TiczSettings.MinimumNumberOfColumns Then amountOfColumns = TiczViewModel.TiczSettings.MinimumNumberOfColumns
        Panel.ItemWidth = e.NewSize.Width / amountOfColumns
        WriteToDebug("Panel Width = ", Panel.ItemWidth)
    End Sub

End Class
