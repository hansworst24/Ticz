' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Ticz.AppSettings
Imports Ticz.TiczViewModel
Imports Windows.UI.Core
Imports Windows.Web.Http
Imports WinRTXamlToolkit.Controls
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class SplitView
    Inherits Page

    Dim app As App = CType(Application.Current, App)
    Dim vm As TiczViewModel = app.myViewModel

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)

        Dim rootFrame As Frame = CType(Window.Current.Content, Frame)
        If rootFrame.CanGoBack Then
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
        Else
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
        End If

        Me.DataContext = vm

    End Sub

    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        vm.StopRefresh()
    End Sub

    Private Sub AppBar_SizeChanged(sender As Object, e As SizeChangedEventArgs)

    End Sub

    Private Sub AppBar_Tapped(sender As Object, e As TappedRoutedEventArgs)
        Dim a As AppBar = CType(sender, AppBar)
        'If a.IsOpen Then vm.NotifiCationMargin = 36 Else vm.NotificationMargin = 0
        WriteToDebug("AppBar.AppBar_Tapped()", a.ActualHeight)

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
