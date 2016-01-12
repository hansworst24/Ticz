Module Modules
    Public Sub WriteToDebug(caller As String, content As String)
        Debug.WriteLine(String.Format("Thread : {0,3} : {1,15} : {2,60} : {3}", Environment.CurrentManagedThreadId, Date.Now.TimeOfDay.ToString, caller, content))
    End Sub


    Public Function ConstructDeviceGroups(devices As IEnumerable(Of Device)) As List(Of Devices)
        Dim scenes = (From d In devices Where d.Type = "Scene" Or d.Type = "Group" Select d).ToList.ToObservableCollection()
        Dim switches = (From d In devices Where d.Type = "Lighting 2" Select d).ToList.ToObservableCollection()
        Dim temps = (From d In devices Where d.Type = "Temp" Select d).ToList.ToObservableCollection()
        Dim utils = (From d In devices Where d.Type = "General" Select d).ToList.ToObservableCollection()
        Dim dglist As New List(Of Devices)
        If Not scenes.Count = 0 Then dglist.Add(New Devices With {.title = "Scenes / Groups", .result = scenes})
        If Not switches.Count = 0 Then dglist.Add(New Devices With {.title = "Lights / Switches", .result = switches})
        If Not temps.Count = 0 Then dglist.Add(New Devices With {.title = "Temp. Sensors", .result = temps})
        If Not utils.Count = 0 Then dglist.Add(New Devices With {.title = "Utility Sensors", .result = utils})
        Return dglist
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
