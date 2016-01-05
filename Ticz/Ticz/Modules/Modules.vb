Module Modules
    Public Function WriteToDebug(caller As String, content As String)
        Debug.WriteLine(String.Format("Thread : {0,3} : {1,15} : {2,60} : {3}", Environment.CurrentManagedThreadId, Date.Now.TimeOfDay.ToString, caller, content))
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
