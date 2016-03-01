' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Windows.UI.Core
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class AboutPage
    Inherits Page


    Private app As Application = CType(Windows.UI.Xaml.Application.Current, Application)

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        AddHandler SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf app.App_BackRequested

        Me.DataContext = app.myViewModel
        Dim rootFrame As Frame = CType(Window.Current.Content, Frame)
        If rootFrame.CanGoBack Then
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible
        Else
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed
        End If

    End Sub

    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        RemoveHandler SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf app.App_BackRequested
    End Sub


End Class
