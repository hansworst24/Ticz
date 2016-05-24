' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Windows.UI.Core

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class SplitView
    Inherits Page

    Private app As Application = CType(Windows.UI.Xaml.Application.Current, Application)

    Public ReadOnly Property vm As TiczViewModel
        Get
            Return app.myViewModel
        End Get
    End Property

    'Public ReadOnly Property vm As TiczViewModel
    '    Get
    '        Return CType(Application.Current, Application).myViewModel
    '    End Get
    'End Property

    Public Sub New()
        InitializeComponent()
        'AddHandler DataContextChanged, Sub(s, e)
        '                                   vm = CType(DataContext, TiczViewModel)
        '                               End Sub
    End Sub

    Protected Overrides Async Sub OnNavigatedTo(e As NavigationEventArgs)
        '        RemoveHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf app.App_BackRequested
        If e.NavigationMode = NavigationMode.New Then
            AddHandler SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf BackButtonPressed
        End If
        Await vm.Load()
        Me.DataContext = app.myViewModel
    End Sub


    Public Sub BackButtonPressed(sender As Object, e As Windows.UI.Core.BackRequestedEventArgs)
        WriteToDebug("App.BackButtonPressed", "executed")
        If app.myViewModel.TiczMenu.IsMenuOpen Then
            app.myViewModel.TiczMenu.MenuGoBack()
            e.Handled = True
            Exit Sub
        End If
    End Sub


    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        WriteToDebug("App.OnNavigatedFrom", "executed")
        app.myViewModel.StopRefresh()
        'RemoveHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf app.App_BackRequested
    End Sub

End Class
