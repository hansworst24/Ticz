Imports GalaSoft.MvvmLight.Command
Imports Windows.Web.Http

Public Class LogitechMediaServerDeviceViewModel
    Inherits DeviceViewModel

    Public Overloads ReadOnly Property DeviceContentTemplate As DataTemplate
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON : Return CType(Application.Current.Resources("DeviceIconView"), DataTemplate)
                Case Constants.DEVICEVIEWS.WIDE : Return CType(Application.Current.Resources("DeviceWideLMSPlayerView"), DataTemplate)
                Case Constants.DEVICEVIEWS.LARGE : Return CType(Application.Current.Resources("DeviceWideLMSPlayerView"), DataTemplate)
                Case Else : Return CType(Application.Current.Resources("DeviceIconView"), DataTemplate)
            End Select
        End Get
    End Property

    Public Sub New(d As DeviceModel, r As String, c As TiczStorage.DeviceConfiguration)
        MyBase.New(d, r, c)
    End Sub

    Public ReadOnly Property OpenLMSRemote As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Dim cDialog As New ContentDialog
                                        'Because we use a customized ContentDialog Style, the ESC key handler didn't work anymore. Therefore we add our own. 
                                        Dim escapekeyhandler = New KeyEventHandler(Sub(s, e)
                                                                                       If e.Key = Windows.System.VirtualKey.Escape Then
                                                                                           cDialog.Hide()
                                                                                       End If
                                                                                   End Sub)
                                        cDialog.AddHandler(UIElement.KeyDownEvent, escapekeyhandler, True)
                                        cDialog.Title = Me.Name
                                        cDialog.Style = CType(Application.Current.Resources("FullScreenContentDialog"), Style)
                                        cDialog.HorizontalAlignment = HorizontalAlignment.Stretch
                                        cDialog.VerticalAlignment = VerticalAlignment.Stretch
                                        cDialog.HorizontalContentAlignment = HorizontalAlignment.Stretch
                                        cDialog.VerticalContentAlignment = VerticalAlignment.Stretch
                                        cDialog.FullSizeDesired = True
                                        Dim remote As New ucLMSRemote
                                        remote.DataContext = Me
                                        cDialog.Content = remote
                                        Await cDialog.ShowAsync()
                                    End Sub)

        End Get
    End Property


    'Public Async Sub OpenLMSRemote()
    '    Dim cDialog As New ContentDialog
    '    'Because we use a customized ContentDialog Style, the ESC key handler didn't work anymore. Therefore we add our own. 
    '    Dim escapekeyhandler = New KeyEventHandler(Sub(s, e)
    '                                                   If e.Key = Windows.System.VirtualKey.Escape Then
    '                                                       cDialog.Hide()
    '                                                   End If
    '                                               End Sub)
    '    cDialog.AddHandler(UIElement.KeyDownEvent, escapekeyhandler, True)
    '    cDialog.Title = Me.Name
    '    cDialog.Style = CType(Application.Current.Resources("FullScreenContentDialog"), Style)
    '    cDialog.HorizontalAlignment = HorizontalAlignment.Stretch
    '    cDialog.VerticalAlignment = VerticalAlignment.Stretch
    '    cDialog.HorizontalContentAlignment = HorizontalAlignment.Stretch
    '    cDialog.VerticalContentAlignment = VerticalAlignment.Stretch
    '    cDialog.FullSizeDesired = True
    '    Dim remote As New ucLMSRemote
    '    remote.DataContext = Me
    '    cDialog.Content = remote
    '    Await cDialog.ShowAsync()
    'End Sub




    Public Async Function PausePlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).PauseURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function StopPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).StopURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function PlayPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).PlayURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function VolumeUpPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).VolumeUpURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function VolumeDownPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).VolumeDownURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function VolumeMutePlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).MuteURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function LeftPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).LeftURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function RightPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).RightURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function UpPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).UpURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function DownPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).DownURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function FavoritesPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).FavoritesURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function NowPlayingPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).NowPlayingURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function BrowsePlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).BrowseURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function RewindPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).RewindURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function ForwardPlayer() As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi.LMSPlayer).ForwardURL(Me.idx)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function

    Public Async Function SleepPlayer() As Task

    End Function

    Public Async Function PowerPlayer() As Task
    End Function

    Public Async Function AddPlayer() As Task
    End Function

    Public Async Function HomePlayer() As Task
    End Function





End Class
