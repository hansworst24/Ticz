Imports Newtonsoft.Json
Imports Windows.Web.Http

Public Class VariableListViewModel
    Inherits List(Of VariableViewModel)

    Public Property message As New ToastMessageViewModel

    Public Async Function Load() As Task(Of retvalue)
        Me.Clear()
        Dim ret As New retvalue
        Dim url As String = (New DomoApi).getVariables()
        WriteToDebug("VariableListViewModel.Load()", url)
        Dim response As HttpResponseMessage = Await (New Domoticz).DownloadJSON(url)
        If response.IsSuccessStatusCode Then
            Dim body As String = Await response.Content.ReadAsStringAsync()
            Dim variables As Domoticz.Variables
            Try
                variables = JsonConvert.DeserializeObject(Of Domoticz.Variables)(body)
                For Each v In variables.result
                    Me.Add(New VariableViewModel(v))
                Next
                ret.issuccess = True
            Catch ex As Exception
                ret.issuccess = False : ret.err = "Error parsing Domoticz variables"
            End Try
        Else
            ret.issuccess = False : ret.err = response.ReasonPhrase
            Dim app As Application = CType(Application.Current, Application)
            Await app.myViewModel.Notify.Update(True, String.Format("Error loading Variables ({0})", ret.err), 2, False, 2)
        End If
        Return ret
    End Function


End Class
