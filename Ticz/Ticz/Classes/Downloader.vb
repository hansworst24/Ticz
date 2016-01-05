Imports System.Threading
Imports Windows.Web.Http
Imports Windows.Web.Http.Filters

Public Class Downloader

    Dim app As App = CType(Application.Current, App)
    Dim vm As TiczViewModel = app.myViewModel

    Public Async Function DownloadJSON(url As String) As Task(Of HttpResponseMessage)

        Using filter As New HttpBaseProtocolFilter
            If Not vm.TiczSettings.Password = "" AndAlso Not vm.TiczSettings.Username = "" Then
                filter.ServerCredential = New Windows.Security.Credentials.PasswordCredential With {.Password = vm.TiczSettings.Password, .UserName = vm.TiczSettings.Username}
            End If
            filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.Default
            filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache
            filter.AllowUI = False
            filter.UseProxy = False
            Using wc As New HttpClient(filter)
                Dim cts As New CancellationTokenSource(vm.TiczSettings.TimeOut * 1000)
                Try
                    WriteToDebug("Downloader.DownloadJSON", url)
                    Dim response As HttpResponseMessage = Await wc.GetAsync(New Uri(url)).AsTask(cts.Token)
                    Return response
                Catch ex As TaskCanceledException
                    Return New HttpResponseMessage With {.ReasonPhrase = "Connection timed out", .StatusCode = HttpStatusCode.RequestTimeout}
                Catch ex As Exception
                    WriteToDebug("Downloader.DownloadJSON", ex.Message.ToString)
                    Return New HttpResponseMessage With {.ReasonPhrase = ex.Message, .StatusCode = HttpStatusCode.Unauthorized}
                End Try
            End Using
        End Using
    End Function
End Class
