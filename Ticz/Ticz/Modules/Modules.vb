Module Modules
    Public Sub WriteToDebug(caller As String, content As String)
        Debug.WriteLine(String.Format("Thread : {0,3} : {1,15} : {2,60} : {3}", Environment.CurrentManagedThreadId, Date.Now.TimeOfDay.ToString, caller, content))
    End Sub


    Public Function ConstructDeviceGroups(devices As IEnumerable(Of Device)) As ObservableCollection(Of Group(Of Device))
        '#If DEBUG Then
        '        Dim list = devices.ToList
        '        For Each d In list
        '            WriteToDebug("Modules.ConstructDeviceGroups()", String.Format("{0} : {1}", d.Name, d.Type))
        '        Next
        '#End If
        'Go through each device, and map it to a seperate subcollection
        Dim scenes, switches, weather, temps, utils, other As New ObservableCollection(Of Device)
        For Each d In devices.ToList()
            Select Case d.Type
                Case "Scene"
                    scenes.Add(d)
                Case "Group"
                    scenes.Add(d)
                Case "Lighting Limitless/Applamp"
                    switches.Add(d)
                Case "Light/Switch"
                    switches.Add(d)
                Case "Lighting 2"
                    switches.Add(d)
                Case "Temp + Humidity + Baro"
                    weather.Add(d)
                Case "Wind"
                    weather.Add(d)
                Case "UV"
                    weather.Add(d)
                Case "Rain"
                    weather.Add(d)
                Case "Temp"
                    temps.Add(d)
                Case "Thermostat"
                    temps.Add(d)
                Case "General"
                    utils.Add(d)
                Case "Usage"
                    utils.Add(d)
                Case "P1 Smart Meter"
                    utils.Add(d)
                Case Else
                    other.Add(d)
                    WriteToDebug("Modules.ConstructDeviceGroups()", String.Format("{0} : {1}", d.Name, d.Type))
            End Select
        Next
        Dim dglist2 As New ObservableCollection(Of Group(Of Device))

        If Not scenes.Count = 0 Then dglist2.Add(New [Group](Of Device)("Scenes / Groups", scenes))
        If Not switches.Count = 0 Then dglist2.Add(New [Group](Of Device)("Lights / Switches", switches))
        If Not temps.Count = 0 Then dglist2.Add(New [Group](Of Device)("Temperature Sensors", temps))
        If Not weather.Count = 0 Then dglist2.Add(New [Group](Of Device)("Weather Sensors", weather))
        If Not utils.Count = 0 Then dglist2.Add(New [Group](Of Device)("Utility Sensors", utils))
        If Not other.Count = 0 Then dglist2.Add(New [Group](Of Device)("Other Devices", other))

        'Dim dglist As New List(Of Devices)
        'If Not scenes.Count = 0 Then dglist.Add(New Devices With {.title = "Scenes / Groups", .result = scenes})
        'If Not switches.Count = 0 Then dglist.Add(New Devices With {.title = "Lights / Switches", .result = switches})
        'If Not temps.Count = 0 Then dglist.Add(New Devices With {.title = "Temperature Sensors", .result = temps})
        'If Not weather.Count = 0 Then dglist.Add(New Devices With {.title = "Weather Sensors", .result = weather})
        'If Not utils.Count = 0 Then dglist.Add(New Devices With {.title = "Utility Sensors", .result = utils})
        'If Not other.Count = 0 Then dglist.Add(New Devices With {.title = "Other Devices", .result = other})
        Return dglist2
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
