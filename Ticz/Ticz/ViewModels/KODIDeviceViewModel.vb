Imports GalaSoft.MvvmLight.Command
Imports Windows.Web.Http

Public Class KODIDeviceViewModel
    Inherits DeviceViewModel

    Public Sub New(d As DeviceModel, r As String, c As TiczStorage.DeviceConfiguration)
        MyBase.New(d, r, c)
    End Sub

    Public ReadOnly Property OpenKODIRemote As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Dim cDialog As New TiczContentDialog
                                        Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
                                        vm.IdleTimer.StopCounter()
                                        cDialog.Title = Me.Name
                                        Dim remote As New ucKODIRemote
                                        remote.DataContext = Me
                                        cDialog.Content = remote
                                        Await cDialog.ShowAsync()
                                        vm.IdleTimer.StartCounter()
                                    End Sub)
        End Get
    End Property


    Public Async Function PlayPausePressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).PlayPauseURL(Me.idx))
    End Function

    Public Async Function StopPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).StopURL(Me.idx))
    End Function


    Public Async Function VolumeUpPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).VolumeUpURL(Me.idx))
    End Function

    Public Async Function VolumeDownPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).VolumeDownURL(Me.idx))
    End Function

    Public Async Function VolumeMutePressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).MuteURL(Me.idx))
    End Function

    Public Async Function LeftPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).LeftURL(Me.idx))
    End Function

    Public Async Function RightPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).RightURL(Me.idx))
    End Function

    Public Async Function UpPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).UpURL(Me.idx))
    End Function

    Public Async Function DownPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).DownURL(Me.idx))
    End Function

    Public Async Function FastForwardPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).FastForwardURL(Me.idx))
    End Function

    Public Async Function RewindPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).RewindURL(Me.idx))
    End Function

    Public Async Function BigStepBackPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).BigStepBackURL(Me.idx))
    End Function

    Public Async Function BigStepForwardPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).BigStepForwardURL(Me.idx))
    End Function

    Public Async Function OKPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).OKURL(Me.idx))
    End Function

    Public Async Function MenuPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).MenuURL(Me.idx))
    End Function

    Public Async Function BackPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).BackURL(Me.idx))
    End Function

    Public Async Function InfoPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).InfoURL(Me.idx))
    End Function

    Public Async Function SubtitlesPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).ShowSubtitlesURL(Me.idx))
    End Function

    Public Async Function FullScreenPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).FullScreenURL(Me.idx))
    End Function

    Public Async Function HomePressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).HomeURL(Me.idx))
    End Function

    Public Async Function ChannelsPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).ChannelsURL(Me.idx))
    End Function

    Public Async Function ChannelUpPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).ChannelUpURL(Me.idx))
    End Function

    Public Async Function ChannelDownPressed() As Task
        Await ButtonPressed((New DomoApi.KODIPlayer).ChannelDownURL(Me.idx))
    End Function

    Public Async Function ButtonPressed(jsonCommand As String) As Task
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(jsonCommand))
        If Not response.IsSuccessStatusCode Then
            Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
        End If
    End Function
End Class
