Imports Windows.ApplicationModel.Core
Imports Windows.UI.Core

Module Modules
    Public Sub WriteToDebug(caller As String, content As String)
#If DEBUG Then
        Debug.WriteLine(String.Format("Thread : {0,3} : {1,15} : {2,60} : {3}", Environment.CurrentManagedThreadId, Date.Now.TimeOfDay.ToString, caller, content))
#End If
    End Sub

    ''' <summary>
    ''' CONVERTOR THAT CONVERTS A DATE TO A UNIX EPOCH INTEGER
    ''' </summary>
    ''' <param name="parDate"></param>
    ''' <returns>INTEGER</returns>
    ''' <remarks></remarks>
    Public Function TimeToUnixSeconds(ByVal parDate As Date) As Long
        If parDate.IsDaylightSavingTime = True Then
            'parDate.AddHours(-1)
        End If
        Dim unixDate As New Date(1970, 1, 1)
        Dim intSecondsDifference = (parDate - unixDate).TotalSeconds
        Return Math.Round(intSecondsDifference)
    End Function


    Public Async Function RunOnUIThread(p As DispatchedHandler) As Task
        Await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, p)
    End Function

    <Extension()>
    Public Function ToObservableCollection(Of T)(collection As IEnumerable(Of T)) As ObservableCollection(Of T)
        Dim observableCollection As New ObservableCollection(Of T)()
        For Each item As T In collection
            observableCollection.Add(item)
        Next

        Return observableCollection
    End Function

End Module
