Imports Newtonsoft.Json
Imports Windows.Web.Http

Public Class CameraListViewModel
    Inherits ObservableCollection(Of CameraViewModel)
    Public Property message As New ToastMessageViewModel

    Public Async Function Load() As Task(Of retvalue)
        Me.Clear()
        message.Clear()
        Dim ret As New retvalue
        Dim url As String = (New DomoApi).getCameras()
        WriteToDebug("CameraListViewModel.Load()", url)
        Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim cams As Domoticz.Cameras
            Try
                cams = JsonConvert.DeserializeObject(Of Domoticz.Cameras)(body)
                For Each c In cams.result
                    Me.Add(New CameraViewModel(c.idx, c.Name))
                Next
                ret.issuccess = True
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing Domoticz cameras"
            End Try
        Else
            ret.issuccess = False : ret.err = response.ReasonPhrase
            Dim app As Application = CType(Application.Current, Application)
            Await app.myViewModel.Notify.Update(True, String.Format("Error loading cameras / {0} / {1})", response.StatusCode.ToString, ret.err), 2, False, 2)
        End If

        'Register to listen for visible bound changes, in order to resize the Camera View accordingly
        'Dim app As Application = CType(Application.Current, Application)
        AddHandler ApplicationView.GetForCurrentView.VisibleBoundsChanged, AddressOf ResizeCameras
        Return ret
    End Function

    Public Async Sub ResizeCameras(sender As ApplicationView, args As Object)
        WriteToDebug("CameraListViewModel.ResizeCameras()", "executed")
        For Each c In Me
            c.MaxItemHeight = If(ApplicationView.GetForCurrentView.Orientation = ApplicationViewOrientation.Portrait,
                               ApplicationView.GetForCurrentView.VisibleBounds.Height - 40,
                               ApplicationView.GetForCurrentView.VisibleBounds.Height)
        Next
    End Sub

End Class
