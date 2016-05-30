Imports Newtonsoft.Json
Imports Windows.Web.Http

Public Class CameraListViewModel
    Inherits ObservableCollection(Of CameraViewModel)

    Public Async Function Load() As Task(Of retvalue)
        Me.Clear()
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
            Await app.myViewModel.Notify.Update(True, String.Format("Error loading cameras ({0})", ret.err), 2, False, 2)
        End If
        Return ret
    End Function


End Class
