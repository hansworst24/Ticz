Imports System.Reflection
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Command
Imports Newtonsoft.Json
Imports Windows.UI
Imports Windows.Web.Http

Public Class DeviceViewModel
    Inherits ViewModelBase
    Implements IDisposable

    Protected Friend _Device As DeviceModel
    Private _Configuration As TiczStorage.DeviceConfiguration
    'Private _cDialog As ContentDialog 'Used to present a password prompt to the user for Protected Devices

#Region "Constructor"
    Public Sub New(d As DeviceModel, r As String, c As TiczStorage.DeviceConfiguration)
        _Device = d
        RoomView = r
        _Configuration = c
    End Sub
#End Region
#Region "Properties"
    Public ReadOnly Property BatteryLevel As Integer
        Get
            Return _Device.BatteryLevel
        End Get
    End Property
    Public ReadOnly Property BatteryLevelVisibility As String
        Get
            If BatteryLevel <= 100 Then Return Constants.VISIBLE Else Return Constants.VISIBLE
        End Get
    End Property
    Public ReadOnly Property BatteryLevelString As String
        Get
            Return String.Format("{0} %", BatteryLevel)
        End Get
    End Property
    Public Property Barometer As String
        Get
            Return _Device.Barometer
        End Get
        Set(value As String)
            _Device.Barometer = value
            RaisePropertyChanged("Barometer")
        End Set
    End Property
    Public ReadOnly Property ButtonColumnSpan As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON : Return 2
                Case Constants.DEVICEVIEWS.WIDE : Return 1
                Case Constants.DEVICEVIEWS.LARGE : Return 2
                Case Else : Return 1
            End Select
        End Get
    End Property
    Public ReadOnly Property ButtonRowSpan As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON : Return 2
                Case Constants.DEVICEVIEWS.WIDE : Return 1
                Case Constants.DEVICEVIEWS.LARGE : Return 1
                Case Else : Return 2
            End Select
        End Get
    End Property
    Public ReadOnly Property ButtonSize As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON : Return 48
                Case Constants.DEVICEVIEWS.WIDE : Return 48
                Case Constants.DEVICEVIEWS.LARGE : Return 96
                Case Else
                    Return 48
            End Select
        End Get
    End Property
    Public ReadOnly Property CanBeSwitched As Boolean
        Get
            Select Case Type
                Case Constants.DEVICE.TYPE.LIGHTING_LIMITLESS, Constants.DEVICE.TYPE.LIGHTING_1, Constants.DEVICE.TYPE.LIGHTING_2,
                     Constants.DEVICE.TYPE.SCENE, Constants.DEVICE.TYPE.GROUP, Constants.DEVICE.TYPE.LIGHT_SWITCH
                    Return True
                Case Else
                    Return False
            End Select
        End Get
    End Property
    Public Property Chill As String
        Get
            Return _Device.Chill
        End Get
        Set(value As String)
            _Device.Chill = value
            RaisePropertyChanged("Chill")
        End Set
    End Property
    Public ReadOnly Property ContentRow As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON : Return 0
                Case Constants.DEVICEVIEWS.WIDE : Return 0
                Case Constants.DEVICEVIEWS.LARGE : Return 1
                Case Else
                    Return 0
            End Select
        End Get
    End Property
    Public ReadOnly Property ContentColumn As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON : Return 1
                Case Constants.DEVICEVIEWS.WIDE : Return 1
                Case Constants.DEVICEVIEWS.LARGE : Return 0
                Case Else
                    Return 0
            End Select
        End Get
    End Property
    Public ReadOnly Property ContentColumnSpan As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON : Return 1
                Case Constants.DEVICEVIEWS.WIDE : Return 1
                Case Constants.DEVICEVIEWS.LARGE : Return 2
                Case Else
                    Return 0
            End Select
        End Get
    End Property
    Public Property Counter As String
        Get
            Return _Device.Counter
        End Get
        Set(value As String)
            _Device.Counter = value
            RaisePropertyChanged("Counter")
            RaisePropertyChanged("EnergyUsage")
            RaisePropertyChanged("CounterToday")
        End Set
    End Property
    Public Property CounterDeliv As String
        Get
            Return _Device.CounterDeliv
        End Get
        Set(value As String)
            _Device.CounterDeliv = value
            RaisePropertyChanged("CounterDeliv")
        End Set
    End Property
    Public Property CounterDelivToday As String
        Get
            Return _Device.CounterDelivToday
        End Get
        Set(value As String)
            _Device.CounterDelivToday = value
            RaisePropertyChanged("CounterDelivToday")
        End Set
    End Property
    Public Property CounterToday As String
        Get
            Return _Device.CounterToday
        End Get
        Set(value As String)
            _Device.CounterToday = value
            RaisePropertyChanged("CounterToday")
            RaisePropertyChanged("EnergyUsage")
        End Set
    End Property
    Public Property Data As String
        Get
            Return _Device.Data
        End Get
        Set(value As String)
            _Device.Data = value
            RaisePropertyChanged("Data")
            RaisePropertyChanged("FooterText")
        End Set
    End Property
    Public ReadOnly Property Description As String
        Get
            Return _Device.Description
        End Get
    End Property
    Public ReadOnly Property DeviceModel As DeviceModel
        Get
            Return _Device
        End Get
    End Property
    Public ReadOnly Property DeviceIconTemplate As DataTemplate
        Get
            Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
            Return If(vm.TiczSettings.UseDomoticzIcons,
                CType(Application.Current.Resources("DomoIconDataTemplate"), DataTemplate),
                CType(Application.Current.Resources("TiczIconDataTemplate"), DataTemplate))
        End Get
    End Property
    Public ReadOnly Property DeviceColumnSpan As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON
                    Return 1
                Case Constants.DEVICEVIEWS.WIDE
                    Return 2
                Case Constants.DEVICEVIEWS.LARGE
                    Return 2
                Case Else
                    Return 1
            End Select
        End Get
    End Property
    Public ReadOnly Property DeviceContentTemplate As DataTemplate
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON
                    Return CType(Application.Current.Resources("DeviceIconView"), DataTemplate)
                Case Constants.DEVICEVIEWS.WIDE, Constants.DEVICEVIEWS.LARGE
                    Select Case Type
                        Case Constants.DEVICE.TYPE.THERMOSTAT : Return CType(Application.Current.Resources("DeviceWideSetPointView"), DataTemplate)
                        Case Constants.DEVICE.TYPE.WIND : Return CType(Application.Current.Resources("DeviceWideWindView"), DataTemplate)
                        Case Constants.DEVICE.TYPE.RAIN : Return CType(Application.Current.Resources("DeviceWideRainView"), DataTemplate)
                        Case Constants.DEVICE.TYPE.GROUP : Return CType(Application.Current.Resources("DeviceWideGroupView"), DataTemplate)
                        Case Constants.DEVICE.TYPE.SCENE : Return CType(Application.Current.Resources("DeviceWideSceneView"), DataTemplate)
                        Case Constants.DEVICE.TYPE.LIGHTING_2
                            Select Case SwitchType
                                Case Constants.DEVICE.SWITCHTYPE.DIMMER : Return CType(Application.Current.Resources("DeviceWideSliderView"), DataTemplate)
                                Case Constants.DEVICE.SWITCHTYPE.BLINDS : Return CType(Application.Current.Resources("DeviceWideBlindsView"), DataTemplate)
                                Case Constants.DEVICE.SWITCHTYPE.BLINDS_INVERTED : Return CType(Application.Current.Resources("DeviceWideBlindsView"), DataTemplate)
                                Case Constants.DEVICE.SWITCHTYPE.MEDIA_PLAYER
                                    Select Case HardwareType
                                        Case Constants.DEVICE.HARDWARETYPE.KODIMEDIASERVER : Return CType(Application.Current.Resources("DeviceWideKODIPlayerView"), DataTemplate)
                                        Case Constants.DEVICE.HARDWARETYPE.LOGITECHMEDIASERVER : Return CType(Application.Current.Resources("DeviceWideLMSPlayerView"), DataTemplate)
                                    End Select
                                    Return CType(Application.Current.Resources("DeviceWideMediaPlayerView"), DataTemplate)
                            End Select
                        Case Constants.DEVICE.TYPE.LIGHTING_LIMITLESS
                            Select Case SubType
                                Case Constants.DEVICE.SUBTYPE.RGB, Constants.DEVICE.SUBTYPE.RGBW : Return CType(Application.Current.Resources("DeviceWideRGBDimmerView"), DataTemplate)
                                Case Else : Return CType(Application.Current.Resources("DeviceWideSliderView"), DataTemplate)
                            End Select
                        Case Constants.DEVICE.TYPE.LIGHT_SWITCH
                            Select Case SwitchType
                                Case Constants.DEVICE.SWITCHTYPE.SELECTOR : Return CType(Application.Current.Resources("DeviceWideSelectorView"), DataTemplate)
                                Case Constants.DEVICE.SWITCHTYPE.DIMMER : Return CType(Application.Current.Resources("DeviceWideSliderView"), DataTemplate)
                            End Select
                        Case Constants.DEVICE.TYPE.P1_SMART_METER
                            Select Case SubType
                                Case Constants.DEVICE.SUBTYPE.P1_ELECTRIC : Return CType(Application.Current.Resources("DeviceWideP1ElectricityView"), DataTemplate)
                                Case Constants.DEVICE.SUBTYPE.P1_GAS : Return CType(Application.Current.Resources("DeviceWideP1GasView"), DataTemplate)
                            End Select
                    End Select
                    Return CType(Application.Current.Resources("DeviceWideView"), DataTemplate)
                Case Else : Return CType(Application.Current.Resources("DeviceIconView"), DataTemplate)
            End Select
        End Get
    End Property
    Public ReadOnly Property DeviceRowSpan As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON
                    Return 1
                Case Constants.DEVICEVIEWS.WIDE
                    Return 1
                Case Constants.DEVICEVIEWS.LARGE
                    Return 2
                Case Else
                    Return 1
            End Select
        End Get
    End Property
    Public Property DeviceRepresentation As String
        Get
            Select Case RoomView
                Case Constants.ROOMVIEW.RESIZEVIEW, Constants.ROOMVIEW.DASHVIEW : Return _Configuration.DeviceRepresentation
                Case Constants.ROOMVIEW.GRIDVIEW : Return Constants.DEVICEVIEWS.WIDE
                Case Constants.ROOMVIEW.ICONVIEW : Return Constants.DEVICEVIEWS.ICON
                Case Constants.ROOMVIEW.LISTVIEW : Return Constants.DEVICEVIEWS.WIDE
                Case Else : Return Constants.DEVICEVIEWS.ICON
            End Select

        End Get
        Set(value As String)
            If RoomView = Constants.ROOMVIEW.RESIZEVIEW Or RoomView = Constants.ROOMVIEW.DASHVIEW Then
                _Configuration.DeviceRepresentation = value
                RaisePropertyChanged("DeviceRepresentation")
            End If
        End Set
    End Property
    Public Property DeviceOrder As Integer
        Get
            Return _Configuration.DeviceOrder
        End Get
        Set(value As Integer)
            _Configuration.DeviceOrder = value
            RaisePropertyChanged("DeviceOrder")
        End Set
    End Property
    Public ReadOnly Property DeviceProperties As List(Of DeviceProperty)
        Get
            Dim dType As Type = _Device.GetType()
            Dim returnprops As New List(Of DeviceProperty)
            For Each prop In dType.GetProperties()
                Dim v As String
                If prop.PropertyType Is GetType(Integer) Then
                    v = CType(prop.GetValue(DeviceModel), Integer)
                ElseIf prop.PropertyType Is GetType(Double) Then
                    v = CType(prop.GetValue(DeviceModel), Double)
                Else
                    v = TryCast(prop.GetValue(DeviceModel, Nothing), String)
                End If
                returnprops.Add(New DeviceProperty With {.Key = prop.Name, .Value = v})
            Next

            Return returnprops
        End Get
    End Property
    Public Property DewPoint As String
        Get
            Return _Device.DewPoint
        End Get
        Set(value As String)
            _Device.DewPoint = value
            RaisePropertyChanged("DewPoint")
        End Set
    End Property
    Public Property Direction As String
        Get
            Return _Device.Direction
        End Get
        Set(value As String)
            _Device.Direction = value
            RaisePropertyChanged("Direction")
        End Set
    End Property
    Public Property DirectionStr As String
        Get
            Return _Device.DirectionStr
        End Get
        Set(value As String)
            _Device.DirectionStr = value
            RaisePropertyChanged("DirectionStr")
        End Set
    End Property
    Public ReadOnly Property EnergyReturn As String
        Get
            Return String.Format("Return: {0} | Today: {1} ", _Device.CounterDeliv, _Device.CounterDelivToday)
        End Get
    End Property
    Public ReadOnly Property EnergyUsage As String
        Get
            Return String.Format("Usage: {0} | Today: {1}", _Device.Counter, _Device.CounterToday)
        End Get
    End Property
    Public ReadOnly Property FooterFontSize As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON : Return 12
                Case Constants.DEVICEVIEWS.WIDE : Return 16
                Case Constants.DEVICEVIEWS.LARGE : Return 20
                Case Else
                    Return 12
            End Select
        End Get
    End Property
    Public ReadOnly Property FooterText As String
        Get
            Dim app As Application = CType(Application.Current, Application)
            Select Case Type
                Case Constants.DEVICE.TYPE.RAIN : Return RainRainRate
                Case Constants.DEVICE.TYPE.WIND : Return String.Format("{0}{1} | {2} {3}", Direction, DirectionStr, Speed, WindSign)
                Case Constants.DEVICE.TYPE.THERMOSTAT : Return String.Format("{0:0.0}", CType(Data, Double))
                Case Constants.DEVICE.TYPE.TEMP
                    Dim a As Application = CType(Windows.UI.Xaml.Application.Current, Application)
                    Return String.Format("{0}{1}", Temp, a.myViewModel.DomoConfig.TempSign)
                Case Constants.DEVICE.TYPE.TEMP_HUMI
                    Dim a As Application = CType(Windows.UI.Xaml.Application.Current, Application)
                    Return String.Format("{0}{1} | {2}{3}", Temp, a.myViewModel.DomoConfig.TempSign, Humidity, "%")
                Case Constants.DEVICE.TYPE.TEMP_HUMI_BARO
                    Dim a As Application = CType(Windows.UI.Xaml.Application.Current, Application)
                    Return String.Format("{0}{1} | {2}{3} | {4}{5}", Temp, a.myViewModel.DomoConfig.TempSign, Humidity, "%", Barometer, "hPa")
                Case Constants.DEVICE.TYPE.WIND
                    Return String.Format("{0} {1}", Direction, DirectionStr)
            End Select
            Select Case SubType
                Case Constants.DEVICE.SUBTYPE.P1_ELECTRIC : Return Usage
                Case Constants.DEVICE.SUBTYPE.P1_GAS : Return CounterToday
                Case Constants.DEVICE.SUBTYPE.SELECTOR_SWITCH : Return LevelNamesList(LevelNameIndex)
            End Select
            If Status = "" Then Return Data Else Return Status
        End Get
    End Property
    Public ReadOnly Property GasUsage As String
        Get
            Return String.Format("Usage: {0} | Today: {1}", _Device.Counter, _Device.CounterToday)
        End Get
    End Property
    Public ReadOnly Property GraphsMenuItemVisibility As String
        Get
            Select Case Type
                Case Constants.DEVICE.TYPE.P1_SMART_METER : Return Constants.VISIBLE
                Case Else : Return Constants.COLLAPSED
            End Select
        End Get
    End Property
    Public Property Gust As String
        Get
            Return _Device.Gust
        End Get
        Set(value As String)
            _Device.Gust = value
            RaisePropertyChanged("Gust")
        End Set
    End Property
    Public ReadOnly Property HeaderFontSize As Integer
        Get
            Select Case DeviceRepresentation
                Case Constants.DEVICEVIEWS.ICON : Return 12
                Case Constants.DEVICEVIEWS.WIDE : Return 16
                Case Constants.DEVICEVIEWS.LARGE : Return 20
                Case Else
                    Return 12
            End Select
        End Get
    End Property
    Public Property Humidity As String
        Get
            Return _Device.Humidity

        End Get
        Set(value As String)
            _Device.Humidity = value
            RaisePropertyChanged("Humidity")
        End Set
    End Property
    Public ReadOnly Property IconForegroundColor As Brush
        Get
            If isOn Then
                Return Windows.UI.Xaml.Application.Current.Resources("SystemControlHighlightAccentBrush")
            Else
                Dim myBrush As New SolidColorBrush
                myBrush.Color = CType(Windows.UI.Color.FromArgb(128, 128, 128, 128), Color)
                Return myBrush
            End If
        End Get
    End Property
    Public ReadOnly Property BitmapIconURI As String
        Get
            Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel

            If _Device.CustomImage = 0 Then
                Dim FileName As String
                Select Case _Device.TypeImg
                    Case "Alert" : FileName = "Alert48_0.png" 'TODO : Change Icon based on Alert Level
                    Case "air" : FileName = "air48.png"
                    Case "blinds" : FileName = If(isOn, "blinds48.png", "blindsopen48.png")
                    Case "counter" : FileName = "Counter48.png"
                    Case "contact" : FileName = If(isOn, "contact48_open.png", "contact48.png")
                    Case "current" : FileName = "current48.png"
                    Case "gauge" : FileName = "gauge48.png"
                    Case "dimmer" : FileName = If("Dimmer48_On.png", "Dimmer48_Off.png")
                    Case "door" : FileName = If(isOn, "door48open.png", "door48.png")
                    Case "doorbell" : FileName = "doorbell48.png"
                    Case "group" : FileName = If(isOn, "pushoff48.png", "push48.png")
                    Case "hardware" : FileName = "Percentage48.png"
                    Case "leaf" : FileName = "leaf48.png"
                    Case "Light" : FileName = If(isOn, "Light48_On.png", "Light48_Off.png")
                    Case "lightbulb" : FileName = If(isOn, "Light48_On.png", "Light48_Off.png")
                    Case "LogitechMediaServer" : FileName = If(isOn, "LogitechMediaServer48_On.png", "LogitechMediaServer48_Off.png")
                    Case "lux" : FileName = "lux48.png"
                    Case "Media" : If isOn Then FileName = "Media48_On.png" Else FileName = "Media48_Off.png"
                    Case "moisture" : FileName = "moisture48.png"
                    Case "override_mini" : FileName = "override.png"
                    Case "push" : FileName = "push48.png"
                    Case "pushoff" : FileName = "pushoff48.png"
                    Case "rain" : FileName = "rain48.png"
                    Case "scene" : FileName = "push48.png"
                    Case "security" : FileName = "security48.png"
                    Case "siren" : FileName = If(isOn, "siren-on.png", "siren-off.png")
                    Case "smoke" : FileName = If(isOn, "smoke48on.png", "smoke48off.png")
                    Case "Speaker" : FileName = If(isOn, "Speaker48_On.png", "Speaker48_Off.png")
                    Case "temperature" : FileName = "temp48.png"
                    Case "text" : FileName = "text48.png"
                    Case "uv" : FileName = "uv48.png"
                    Case "visibility" : FileName = "visibility48.png"
                    Case "wind" : FileName = "wind48.png"
                End Select
                If FileName = "" Then
                    FileName = "logo.png"
                End If
                Return String.Format("{0}/images/{1}", vm.TiczSettings.GetFullURL, FileName)
            Else
                If isOn Then Return String.Format("{0}/images/{1}{2}", vm.TiczSettings.GetFullURL, _Device.Image, "48_On.png")
                If Not isOn Then Return String.Format("{0}/images/{1}{2}", vm.TiczSettings.GetFullURL, _Device.Image, "48_Off.png")
            End If

        End Get
    End Property
    Public ReadOnly Property HardwareType
        Get
            Return _Device.HardwareType
        End Get
    End Property
    Public ReadOnly Property IconPathGeometry As String
        Get
            If _Device.CustomImage = 0 Then
                'Specific Icons for KODI and LMS based on HardwareType
                Select Case _Device.HardwareType
                    Case Constants.DEVICE.HARDWARETYPE.KODIMEDIASERVER : Return Constants.ICONPATH.KODI
                    Case Constants.DEVICE.HARDWARETYPE.LOGITECHMEDIASERVER : Return Constants.ICONPATH.LMS_PLAYER
                End Select
                Select Case _Device.TypeImg
                    Case Constants.DEVICE.TYPEIMG.ALERT : Return Constants.ICONPATH.ALERT
                    Case Constants.DEVICE.TYPEIMG.AIR : Return Constants.ICONPATH.AIR
                    Case Constants.DEVICE.TYPEIMG.BLINDS : Return Constants.ICONPATH.BLINDS
                    Case Constants.DEVICE.TYPEIMG.CONTACT : Return Constants.ICONPATH.CONTACT
                    Case Constants.DEVICE.TYPEIMG.COUNTER : Return Constants.ICONPATH.COUNTER
                    Case Constants.DEVICE.TYPEIMG.CURRENT : Return Constants.ICONPATH.CURRENT
                    Case Constants.DEVICE.TYPEIMG.DIMMER : Return Constants.ICONPATH.DIMMER
                    Case Constants.DEVICE.TYPEIMG.DOOR : Return Constants.ICONPATH.DOOR
                    Case Constants.DEVICE.TYPEIMG.DOORBELL : Return Constants.ICONPATH.DOORBELL
                    Case Constants.DEVICE.TYPEIMG.ERROR : Return Constants.ICONPATH.ERROR
                    Case Constants.DEVICE.TYPEIMG.GAUGE : Return Constants.ICONPATH.GAUGE
                    Case Constants.DEVICE.TYPEIMG.GROUP : Return Constants.ICONPATH.GROUP
                    Case Constants.DEVICE.TYPEIMG.HARDWARE : Return Constants.ICONPATH.HARDWARE
                    Case Constants.DEVICE.TYPEIMG.INFO : Return Constants.ICONPATH.INFO
                    Case Constants.DEVICE.TYPEIMG.LEAF : Return Constants.ICONPATH.LEAF
                    Case Constants.DEVICE.TYPEIMG.LIGHT : Return Constants.ICONPATH.LIGHTBULB
                    Case Constants.DEVICE.TYPEIMG.LIGHTBULB : Return Constants.ICONPATH.LIGHTBULB
                    Case Constants.DEVICE.TYPEIMG.LUX : Return Constants.ICONPATH.LUX
                    Case Constants.DEVICE.TYPEIMG.MEDIA : Return Constants.ICONPATH.MEDIA
                    Case Constants.DEVICE.TYPEIMG.MOISTURE : Return Constants.ICONPATH.MOISTURE
                    Case Constants.DEVICE.TYPEIMG.OVERRIDE_MINI : Return Constants.ICONPATH.OVERRIDE_MINI
                    Case Constants.DEVICE.TYPEIMG.PUSH : Return Constants.ICONPATH.PUSH
                    Case Constants.DEVICE.TYPEIMG.PUSHOFF : Return Constants.ICONPATH.PUSHOFF
                    Case Constants.DEVICE.TYPEIMG.RAIN : Return Constants.ICONPATH.RAIN
                    Case Constants.DEVICE.TYPEIMG.SCALE : Return Constants.ICONPATH.SCALE
                    Case Constants.DEVICE.TYPEIMG.SCENE : Return Constants.ICONPATH.SCENE
                    Case Constants.DEVICE.TYPEIMG.SECURITY : Return Constants.ICONPATH.SECURITY
                    Case Constants.DEVICE.TYPEIMG.SMOKE : Return Constants.ICONPATH.SMOKE
                    Case Constants.DEVICE.TYPEIMG.SPEAKER : Return Constants.ICONPATH.SPEAKER
                    Case Constants.DEVICE.TYPEIMG.TEMPERATURE : Return Constants.ICONPATH.TEMPERATURE
                    Case Constants.DEVICE.TYPEIMG.TEXT : Return Constants.ICONPATH.TEXT
                    Case Constants.DEVICE.TYPEIMG.UV : Return Constants.ICONPATH.UV
                    Case Constants.DEVICE.TYPEIMG.VISIBILITY : Return Constants.ICONPATH.VISIBILITY
                    Case Constants.DEVICE.TYPEIMG.WIND : Return Constants.ICONPATH.WIND
                    Case Else : Return Constants.ICONPATH.UNKNOWN
                End Select
            Else
                Select Case _Device.Image
                    Case Constants.DEVICE.IMAGE.ALARM : Return Constants.ICONPATH.ALARM
                    Case Constants.DEVICE.IMAGE.AMPLIFIER : Return Constants.ICONPATH.AMPLIFIER
                    Case Constants.DEVICE.IMAGE.CHRISTMASTREE : Return Constants.ICONPATH.CHRISTMASTREE
                    Case Constants.DEVICE.IMAGE.COOLING : Return Constants.ICONPATH.COOLING
                    Case Constants.DEVICE.IMAGE.DESKTOP : Return Constants.ICONPATH.DESKTOP
                    Case Constants.DEVICE.IMAGE.FAN : Return Constants.ICONPATH.FAN
                    Case Constants.DEVICE.IMAGE.FIREPLACE : Return Constants.ICONPATH.FIREPLACE
                    Case Constants.DEVICE.IMAGE.GENERIC : Return Constants.ICONPATH.GENERIC
                    Case Constants.DEVICE.IMAGE.HARDDISK : Return Constants.ICONPATH.HARDDISK
                    Case Constants.DEVICE.IMAGE.HEATING : Return Constants.ICONPATH.TEMPERATURE
                    Case Constants.DEVICE.IMAGE.MEDIA : Return Constants.ICONPATH.MEDIA
                    Case Constants.DEVICE.IMAGE.MOTION : Return Constants.ICONPATH.MOTION
                    Case Constants.DEVICE.IMAGE.LAPTOP : Return Constants.ICONPATH.LAPTOP
                    Case Constants.DEVICE.IMAGE.PHONE : Return Constants.ICONPATH.PHONE
                    Case Constants.DEVICE.IMAGE.PRINTER : Return Constants.ICONPATH.PRINTER
                    Case Constants.DEVICE.IMAGE.SPEAKER : Return Constants.ICONPATH.SPEAKER
                    Case Constants.DEVICE.IMAGE.TELEVISION : Return Constants.ICONPATH.TELEVISION
                    Case Constants.DEVICE.IMAGE.WALLSOCKET : Return Constants.ICONPATH.WALLSOCKET
                    Case Constants.DEVICE.IMAGE.WATER : Return Constants.ICONPATH.MOISTURE
                    Case Else : Return Constants.ICONPATH.UNKNOWN
                End Select
            End If
        End Get
    End Property
    Public Property isMixed As Boolean
        Get
            Return _isMixed
        End Get
        Set(value As Boolean)
            _isMixed = value
            RaisePropertyChanged("isMixed")
        End Set
    End Property
    Private Property _isMixed As Boolean
    Public ReadOnly Property isOn As Boolean
        Get
            Select Case SwitchType
                Case Constants.DEVICE.SWITCHTYPE.ON_OFF
                    If Status = Constants.DEVICE.STATUS.ON Then Return True Else Return False
                Case Constants.DEVICE.SWITCHTYPE.DOOR_LOCK
                    If Status = Constants.DEVICE.STATUS.OPEN Then Return True Else Return False
                Case Constants.DEVICE.SWITCHTYPE.CONTACT
                    If Status = Constants.DEVICE.STATUS.OPEN Then Return True Else Return False
                Case Constants.DEVICE.SWITCHTYPE.BLINDS
                    If Status = Constants.DEVICE.STATUS.OPEN Then Return False Else Return True
                Case Constants.DEVICE.SWITCHTYPE.BLINDS_INVERTED
                    If Status = Constants.DEVICE.STATUS.OPEN Then Return True Else Return False
                Case Constants.DEVICE.SWITCHTYPE.DIMMER
                    If Status = Constants.DEVICE.STATUS.OFF Then Return False Else Return True
                Case Constants.DEVICE.SWITCHTYPE.MEDIA_PLAYER
                    If Status = Constants.DEVICE.STATUS.OFF Then Return False Else Return True
                Case Constants.DEVICE.SWITCHTYPE.SELECTOR
                    If Status = Constants.DEVICE.STATUS.OFF Then Return False Else Return True
                Case Constants.DEVICE.SWITCHTYPE.X10_SIREN
                    If Status = "All On" Then Return True Else Return False
                Case Constants.DEVICE.SWITCHTYPE.SMOKE_DETECTOR
                    If Status = Constants.DEVICE.STATUS.ON Then Return True Else Return False
                Case Else
                    Select Case Type
                        Case Constants.DEVICE.TYPE.SECURITY
                            If Status = Constants.SECPANEL.SEC_ARMAWAY_STATUS Or Status = Constants.SECPANEL.SEC_ARMHOME_STATUS Then Return True Else Return False
                        Case Constants.DEVICE.TYPE.GROUP
                            If Status = Constants.DEVICE.STATUS.OFF Then Return False Else Return True
                        Case Constants.DEVICE.TYPE.SCENE
                            If Status = Constants.DEVICE.STATUS.OFF Then Return False Else Return True
                        Case Else
                    End Select
            End Select
            Return False
        End Get
    End Property
    Public Property LastUpdate As String
        Get
            Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
            If vm.TiczSettings.ShowLastSeen Then Return _Device.LastUpdate Else Return ""
        End Get
        Set(value As String)
            _Device.LastUpdate = value
            RaisePropertyChanged("LastUpdate")
        End Set
    End Property
    Public Property Level As Integer
        Get
            Return _Device.Level
        End Get
        Set(value As Integer)
            _Device.Level = value
            RaisePropertyChanged("Level")
        End Set
    End Property
    Public Property LevelInt As Integer
        Get
            Return _Device.LevelInt
        End Get
        Set(value As Integer)
            _Device.LevelInt = value
            RaisePropertyChanged("LevelInt")
        End Set
    End Property
    Public ReadOnly Property LevelNames As String
        Get
            Return _Device.LevelNames
        End Get
    End Property
    Public Property LevelNameIndex As Integer
        Get
            If Not LevelNamesList.Count = 0 Then
                If LevelInt Mod 10 > 0 Then
                    'Dimmer Level not set to a 10-value, therefore illegal
                    Return 0
                Else
                    Return LevelInt / 10
                End If
            End If
            Return _LevelNameIndex
        End Get
        Set(value As Integer)
            _LevelNameIndex = value
            RaisePropertyChanged("LevelNameIndex")
        End Set
    End Property
    Private Property _LevelNameIndex As Integer
    Public ReadOnly Property LevelNamesList As List(Of String)
        Get
            If Not _Device.LevelNames = "" Then
                Return _Device.LevelNames.Split("|").ToList()
            Else
                Return New List(Of String)
            End If
        End Get
    End Property
    Public ReadOnly Property MaxDimLevel As Integer
        Get
            Return _Device.MaxDimLevel
        End Get
    End Property

    Public ReadOnly Property Name As String
        Get
            Return _Device.Name
        End Get
    End Property
    Public Property PassCode As String
        Get
            Return _PassCode
        End Get
        Set(value As String)
            _PassCode = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _PassCode As String
    Public ReadOnly Property PlanIDs As List(Of Integer)
        Get
            Return _Device.PlanIDs
        End Get
    End Property
    Public ReadOnly Property [Protected] As Boolean
        Get
            Return _Device.Protected
        End Get
    End Property
    Public Property Rain As String
        Get
            Return _Device.Rain
        End Get
        Set(value As String)
            _Device.Rain = value
            RaisePropertyChanged("Rain")
        End Set
    End Property
    Public ReadOnly Property RainRainRate As String
        Get
            Return String.Format("{0} mm | {1} mm/h", _Device.Rain, _Device.RainRate)
        End Get
    End Property
    Public Property RainRate As String
        Get
            Return _Device.RainRate
        End Get
        Set(value As String)
            _Device.RainRate = value
            RaisePropertyChanged("RainRate")
        End Set
    End Property
    Public Property RoomView As String
    Public Property Speed As String
        Get
            Return _Device.Speed
        End Get
        Set(value As String)
            _Device.Speed = value
            RaisePropertyChanged("Speed")
        End Set
    End Property
    Public ReadOnly Property SpeedGust As String
        Get
            Return String.Format("{0} | {1}", _Device.Speed, _Device.Gust)
        End Get
    End Property
    Public Property Status As String
        Get
            Return _Device.Status
        End Get
        Set(value As String)
            If value <> _Device.Status Then
                _Device.Status = value
                Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
                If vm.TiczSettings.UseDomoticzIcons Then
                    RaisePropertyChanged("BitmapIconURI")
                Else
                    RaisePropertyChanged("IconForegroundColor")
                End If
                RaisePropertyChanged("Status")
                RaisePropertyChanged("FooterText")
            End If
        End Set
    End Property
    Public ReadOnly Property SubType As String
        Get
            Return _Device.SubType
        End Get
    End Property
    Public Property SwitchingToState As String
    Public ReadOnly Property SwitchType As String
        Get
            Return _Device.SwitchType
        End Get
    End Property
    Public ReadOnly Property SwitchTypeVal As Integer
        Get
            Return _Device.SwitchTypeVal
        End Get
    End Property
    Public Property Temp As String
        Get
            Return _Device.Temp
        End Get
        Set(value As String)
            _Device.Temp = value
            RaisePropertyChanged("Temp")
        End Set
    End Property
    Public ReadOnly Property TempChill As String
        Get
            Return String.Format("{0} | {1} ", _Device.Temp, _Device.Chill)
        End Get
    End Property
    Public ReadOnly Property Type As String
        Get
            Return _Device.Type
        End Get
    End Property
    Public Property Usage As String
        Get
            Return _Device.Usage
        End Get
        Set(value As String)
            _Device.Usage = value
            RaisePropertyChanged("Usage")
        End Set
    End Property
    Public Property UsageDeliv As String
        Get
            Return _Device.UsageDeliv
        End Get
        Set(value As String)
            _Device.UsageDeliv = value
            RaisePropertyChanged("UsageDeliv")
        End Set
    End Property
    Public ReadOnly Property WindSign As String
        Get
            Dim app As Application = CType(Application.Current, Application)
            Return app.myViewModel.DomoConfig.WindSign
        End Get
    End Property
    Public ReadOnly Property idx As String
        Get
            Return _Device.idx
        End Get
    End Property

    Public Property MarqueeLength As Double?
        Get
            Return _MarqueeLength
        End Get
        Set(value As Double?)
            _MarqueeLength = value
            RaisePropertyChanged("MarqueeLength")
        End Set
    End Property
    Private Property _MarqueeLength As Double?
    Public Property MarqueeStart As Boolean
        Get
            Return _MarqueeStart
        End Get
        Set(value As Boolean)
            _MarqueeStart = value
            RaisePropertyChanged()
        End Set
    End Property
    Private Property _MarqueeStart As Boolean
#End Region
#Region "Relay Commands"
    Public ReadOnly Property DataFieldChanged As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Sub(x)
                                                   Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
                                                   If Me.SwitchType <> Constants.DEVICE.SWITCHTYPE.MEDIA_PLAYER Then Exit Sub
                                                   If Not app.myViewModel.TiczSettings.ShowMarquee Then MarqueeStart = False : Exit Sub
                                                   Dim CanvasLength, TextLength As Integer
                                                   Dim MarqueeCanvas As Canvas = TryCast(x, Canvas)
                                                   If Not MarqueeCanvas Is Nothing Then
                                                       Dim MarqueeTextBlock = TryCast(MarqueeCanvas.Children(0), TextBlock)
                                                       If Not MarqueeTextBlock Is Nothing Then
                                                           TextLength = MarqueeTextBlock.ActualWidth
                                                           CanvasLength = MarqueeCanvas.ActualWidth
                                                           If TextLength > CanvasLength Then
                                                               MarqueeLength = CanvasLength - TextLength
                                                               MarqueeStart = True
                                                           Else
                                                               MarqueeLength = 0
                                                               MarqueeStart = False
                                                           End If
                                                       End If
                                                   End If
                                                   WriteToDebug("TiczViewModel.DataFieldChanged()", String.Format("Canvas Width:{0} / TextLength:{1} / MarqueeStart:{2}", CanvasLength, TextLength, MarqueeStart))

                                               End Sub)
        End Get
    End Property


    Public ReadOnly Property GroupSwitchOn As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await SwitchGroup(Constants.DEVICE.STATUS.ON)
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property GroupSwitchOff As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await SwitchGroup(Constants.DEVICE.STATUS.OFF)
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property SelectorSelectionChanged As RelayCommand(Of Object)
        Get
            Return New RelayCommand(Of Object)(Async Sub(x)

                                                   Dim combobox As ComboBox = TryCast(x, ComboBox)
                                                   If Not combobox Is Nothing Then
                                                       If combobox.SelectedIndex = -1 Then Exit Sub
                                                       Dim s As String = combobox.SelectedItem
                                                       WriteToDebug("Device.SelectorSelectionChanged()", String.Format("Selected Item : {0} / Selected Index {1}", s, combobox.SelectedIndex))
                                                       Dim SwitchToState As String = (combobox.SelectedIndex * 10).ToString
                                                       If [Protected] Then
                                                           'SwitchingToState = SwitchToState
                                                           'Dim vm As TiczViewModel = CType(Windows.UI.Xaml.Application.Current, Application).myViewModel
                                                           'vm.selectedDevice = Me
                                                           ''TODO  : FIX PASSWORD
                                                           Await ShowPasswordPrompt()
                                                           If PassCode = "" Then Exit Sub
                                                       End If
                                                       Dim ret As retvalue = Await SwitchDevice(SwitchToState)
                                                       Else
                                                       WriteToDebug("Device.SelectorSelectionChanged()", "ignoring...")
                                                   End If
                                               End Sub)
        End Get
    End Property

    Public ReadOnly Property SetPointUpCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        'For the moment we assume that a SetPoint only handles temperature changes. We allow the temp to be set in half degrees increments
                                        Data = CType(Data, Double) + 0.5
                                    End Sub)
        End Get
    End Property
    Public ReadOnly Property SetPointDownCommand As RelayCommand
        Get
            Return New RelayCommand(Sub()
                                        'For the moment we assume that a SetPoint only handles temperature changes. We allow the temp to be set in half degrees increments
                                        Data = CType(Data, Double) - 0.5
                                    End Sub)
        End Get
    End Property

    Public ReadOnly Property SetSetPointCommand As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        Await SwitchDevice(Data)
                                    End Sub)
        End Get
    End Property


    Public ReadOnly Property SelectRGBValues As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        WriteToDebug("DeviceViewModel.SelectRGBValues()", "executed")

                                        Dim cDialog As New ContentDialog
                                        Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
                                        vm.IdleTimer.StopCounter()
                                        'Because we use a customized ContentDialog Style, the ESC key handler didn't work anymore. Therefore we add our own. 
                                        Dim escapekeyhandler = New KeyEventHandler(Sub(s, e)
                                                                                       If e.Key = Windows.System.VirtualKey.Escape Then
                                                                                           cDialog.Hide()
                                                                                       End If
                                                                                   End Sub)
                                        cDialog.AddHandler(UIElement.KeyDownEvent, escapekeyhandler, True)
                                        cDialog.Title = "Select a color"
                                        cDialog.Style = CType(Application.Current.Resources("FullScreenContentDialog"), Style)
                                        cDialog.HorizontalAlignment = HorizontalAlignment.Stretch
                                        cDialog.VerticalAlignment = VerticalAlignment.Stretch
                                        cDialog.HorizontalContentAlignment = HorizontalAlignment.Stretch
                                        cDialog.VerticalContentAlignment = VerticalAlignment.Stretch
                                        cDialog.Content = New ucRGBColorPicker
                                        cDialog.DataContext = New ColorPickerViewModel(Me)
                                        Await cDialog.ShowAsync()
                                        vm.IdleTimer.StartCounter()
                                    End Sub)

        End Get
    End Property


    Public ReadOnly Property SliderValueChanged As RelayCommand
        Get
            Return New RelayCommand(Async Sub()
                                        If Me.SwitchType = Constants.DEVICE.SWITCHTYPE.DIMMER Then
                                            'Identify what kind of range the Device handles, either 1-15 or 1-100. Based on this, calculate the value to be sent
                                            Dim ValueToSend As Integer = Math.Round((MaxDimLevel / 100) * LevelInt)
                                            WriteToDebug("Device.SliderValueChanged()", String.Format("executed : value {0}", ValueToSend))
                                            Dim SwitchToState As String = (ValueToSend).ToString
                                            If [Protected] Then
                                                'SwitchingToState = SwitchToState
                                                'Dim vm As TiczViewModel = CType(Windows.UI.Xaml.Application.Current, Application).myViewModel
                                                'vm.selectedDevice = Me
                                                Await ShowPasswordPrompt()
                                                If PassCode = "" Then Exit Sub
                                            End If
                                            Dim ret As retvalue = Await SwitchDevice(SwitchToState)
                                        End If
                                    End Sub)

        End Get
    End Property
#End Region
#Region "Methods"

    Public Async Function ResizeIcon() As Task
        Await Resize(Constants.DEVICEVIEWS.ICON)
    End Function

    Public Async Function ResizeWide() As Task
        Await Resize(Constants.DEVICEVIEWS.WIDE)
    End Function

    Public Async Function ResizeLarge() As Task
        Await Resize(Constants.DEVICEVIEWS.LARGE)
    End Function

    Public Async Function Resize(deviceSize As String) As Task
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        WriteToDebug(String.Format("Device.Resize() - {0}", deviceSize), "executed")
        'First, remove the Device from the ViewModel, otherwise the device isn't resized properly
        Dim myIndex As Integer
        myIndex = app.myViewModel.currentRoom.Devices.IndexOf(Me)
        app.myViewModel.currentRoom.Devices.Remove(Me)
        'Secondly change the DeviceRepresentation to the one selected
        DeviceRepresentation = deviceSize
        'Save the DeviceRepresentation to storage
        Dim devConfig = (From d In app.myViewModel.currentRoom.RoomConfiguration.DeviceConfigurations Where d.DeviceIDX = Me.idx And d.DeviceName = Me.Name Select d).FirstOrDefault
        If Not devConfig Is Nothing Then
            devConfig.DeviceRepresentation = DeviceRepresentation
        End If
        Await app.myViewModel.TiczRoomConfigs.SaveRoomConfigurations()
        're-insert the device back into the view
        app.myViewModel.currentRoom.Devices.Insert(myIndex, Me)
        RaisePropertyChanged("DeviceRepresentation")
    End Function

    Public Async Sub MoveUp()
        WriteToDebug("Device.MoveUp()", "executed")
        Dim vm As TiczViewModel = CType(Windows.UI.Xaml.Application.Current, Application).myViewModel
        vm.currentRoom.RoomConfiguration.DeviceConfigurations.MoveUp(idx, Name)
        Dim myIndex As Integer = vm.currentRoom.Devices.IndexOf(Me)
        If Not myIndex = 0 Then
            vm.currentRoom.Devices.Remove(Me)
            vm.currentRoom.Devices.Insert(myIndex - 1, Me)
        End If
        Await vm.TiczRoomConfigs.SaveRoomConfigurations()
    End Sub

    Public Async Sub MoveDown()
        WriteToDebug("Device.MoveDown()", "executed")
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        app.myViewModel.currentRoom.RoomConfiguration.DeviceConfigurations.MoveDown(idx, Name)
        Dim myIndex As Integer = app.myViewModel.currentRoom.Devices.IndexOf(Me)
        If Not myIndex = app.myViewModel.currentRoom.Devices.Count - 1 Then
            app.myViewModel.currentRoom.Devices.Remove(Me)
            app.myViewModel.currentRoom.Devices.Insert(myIndex + 1, Me)
        End If
        Await app.myViewModel.TiczRoomConfigs.SaveRoomConfigurations()
    End Sub

    ''' <summary>
    ''' Triggered by Devices that Open/Close
    ''' </summary>
    ''' <returns></returns>
    Public Async Function OpenButton() As Task
        WriteToDebug("Device.OpenButtonCommand()", "executed")
        Dim switchToState As String
        Select Case SwitchType
            Case Constants.DEVICE.SWITCHTYPE.BLINDS
                switchToState = Constants.DEVICE.STATUS.OFF
            Case Constants.DEVICE.SWITCHTYPE.BLINDS_INVERTED
                switchToState = Constants.DEVICE.STATUS.ON
            Case Else
                switchToState = Constants.DEVICE.STATUS.OFF
        End Select
        If [Protected] Then
            'SwitchingToState = switchToState
            'Dim vm As TiczViewModel = CType(Windows.UI.Xaml.Application.Current, Application).myViewModel
            'vm.selectedDevice = Me
            Await ShowPasswordPrompt()
            If PassCode = "" Then Exit Function
        End If
        Dim ret As retvalue = Await SwitchDevice(switchToState)
    End Function
    ''' <summary>
    ''' Triggered by Devices that Open/Close
    ''' </summary>
    ''' <returns></returns>
    Public Async Function CloseButton() As Task
        WriteToDebug("Device.CloseButtonCommand()", "executed")
        Dim switchToState As String
        Select Case SwitchType
            Case Constants.DEVICE.SWITCHTYPE.BLINDS
                switchToState = Constants.DEVICE.STATUS.ON
            Case Constants.DEVICE.SWITCHTYPE.BLINDS_INVERTED
                switchToState = Constants.DEVICE.STATUS.OFF
            Case Else
                switchToState = Constants.DEVICE.STATUS.ON
        End Select
        If [Protected] Then
            'SwitchingToState = switchToState
            'Dim vm As TiczViewModel = CType(Windows.UI.Xaml.Application.Current, Application).myViewModel
            'vm.selectedDevice = Me
            Await ShowPasswordPrompt()
            If PassCode = "" Then Exit Function
        End If
        Dim ret As retvalue = Await SwitchDevice(switchToState)
    End Function


    ''' <summary>
    ''' Opens a custom ContentDialog which shows a list of Key/Value pairs for each property of the device
    ''' </summary>
    Public Async Sub ShowDeviceDetails()
        WriteToDebug("Device.ShowDeviceDetails()", "executed")
        Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
        vm.IdleTimer.StopCounter()
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
        Dim details As New ucDevice_Details()
        details.DataContext = Me
        cDialog.Content = details
        Await cDialog.ShowAsync()
        vm.IdleTimer.StartCounter()
    End Sub

    Public Async Function GetDeviceGraphData() As Task(Of GraphListViewModel)
        Dim app As Application = CType(Application.Current, Application)
        Dim GraphList = New GraphListViewModel
        '        ShowDeviceGraph = True
        Await app.myViewModel.Notify.Update(False, "Loading graphs, please wait...", 0, False, 0)
        GraphList.deviceName = Name
        Dim GraphsToAdd As New List(Of Domoticz.DeviceGraphContainer)

        Select Case Type
            Case Constants.DEVICE.TYPE.RFXMETER
                Select Case SwitchTypeVal
                    Case 0 'ENERGY METER
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "week", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyWeek"), DataTemplate), (New DomoApi).getGraph(idx, "week", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "counter")))
                    Case 1 'GAS METER
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphGasDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "week", TryCast(Xaml.Application.Current.Resources("FastGraphGasWeek"), DataTemplate), (New DomoApi).getGraph(idx, "week", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphGasMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphGasYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "counter")))
                    Case 2 'WATER METER
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("GraphWaterConsumptionDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "week", TryCast(Xaml.Application.Current.Resources("GraphWaterConsumptionWeek"), DataTemplate), (New DomoApi).getGraph(idx, "week", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("GraphWaterConsumptionMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("GraphWaterConsumptionYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "counter")))
                    Case 3 'COUNTER METER
                        'ASSUMPTION THAT COUNTER METER IS LIKE ON/OFF SWITCH
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "", TryCast(Xaml.Application.Current.Resources("FastGraph"), DataTemplate), (New DomoApi).getLightLog(idx)))
                    Case 4 'ENERGY GENERATED METER
                        'ASSUMPTION THAT ENERGY GENERATED IS THE SAME AS ENERGY METER
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "week", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyWeek"), DataTemplate), (New DomoApi).getGraph(idx, "week", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "counter")))
                End Select
            Case Constants.DEVICE.TYPE.LUX
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphLuxDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "counter")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphLuxMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "counter")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphLuxYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "counter")))
            Case Constants.DEVICE.TYPE.WEIGHT
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphWeightDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "counter")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphWeightMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "counter")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphWeightYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "counter")))
            Case Constants.DEVICE.TYPE.WIND
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphWindDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "wind")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphWindMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "wind")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphWindYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "wind")))
            Case Constants.DEVICE.TYPE.RAIN
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphRainDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "rain")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "week", TryCast(Xaml.Application.Current.Resources("FastGraphRainWeek"), DataTemplate), (New DomoApi).getGraph(idx, "week", "rain")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphRainMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "rain")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphRainYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "rain")))

            Case Constants.DEVICE.TYPE.THERMOSTAT
                Select Case SubType
                    Case Constants.DEVICE.SUBTYPE.SETPOINT
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "temp")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "temp")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "temp")))
                End Select
            Case Constants.DEVICE.TYPE.LIGHT_SWITCH
                Select Case SubType
                    Case Constants.DEVICE.SUBTYPE.SELECTOR_SWITCH, Constants.DEVICE.SUBTYPE.SWITCH
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "", TryCast(Xaml.Application.Current.Resources("FastGraph"), DataTemplate), (New DomoApi).getLightLog(idx)))
                End Select
            Case Constants.DEVICE.TYPE.LIGHTING_2, Constants.DEVICE.TYPE.LIGHTING_1, Constants.DEVICE.TYPE.LIGHTING_LIMITLESS
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "", TryCast(Xaml.Application.Current.Resources("FastGraph"), DataTemplate), (New DomoApi).getLightLog(idx)))

            Case Constants.DEVICE.TYPE.TEMP
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "temp")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "temp")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphTemperatureYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "temp")))
            Case Constants.DEVICE.TYPE.TEMP_HUMI, Constants.DEVICE.TYPE.TEMP_HUMI_BARO
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphTempHuDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "temp")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphTempHuMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "temp")))
                GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphTempHuYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "temp")))
            Case Constants.DEVICE.TYPE.GENERAL
                Select Case SubType
                    Case Constants.DEVICE.SUBTYPE.KWH
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("GeneralKWHDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "week", TryCast(Xaml.Application.Current.Resources("GeneralKWHWeek"), DataTemplate), (New DomoApi).getGraph(idx, "week", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("GeneralKWHMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("GeneralKWHYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "counter")))
                End Select
            Case Constants.DEVICE.TYPE.USAGE
                Select Case SubType
                    Case Constants.DEVICE.SUBTYPE.ELECTRIC
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphUsageElectricDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphUsageElectricMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphUsageElectricYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "counter")))
                End Select
            Case Else
                Select Case SubType
                    Case Constants.DEVICE.SUBTYPE.PERCENTAGE
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphPercentageDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "Percentage")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphPercentageMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "Percentage")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphPercentageYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "Percentage")))
                    Case Constants.DEVICE.SUBTYPE.P1_ELECTRIC
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "week", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyWeek"), DataTemplate), (New DomoApi).getGraph(idx, "week", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphEnergyYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "counter")))
                    Case Constants.DEVICE.SUBTYPE.P1_GAS
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "day", TryCast(Xaml.Application.Current.Resources("FastGraphGasDay"), DataTemplate), (New DomoApi).getGraph(idx, "day", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "week", TryCast(Xaml.Application.Current.Resources("FastGraphGasWeek"), DataTemplate), (New DomoApi).getGraph(idx, "week", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "month", TryCast(Xaml.Application.Current.Resources("FastGraphGasMonth"), DataTemplate), (New DomoApi).getGraph(idx, "month", "counter")))
                        GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("FastGraphGasYear"), DataTemplate), (New DomoApi).getGraph(idx, "year", "counter")))
                End Select
        End Select

        'Add an empty graph which tells the user to help getting their data in :)
        If GraphsToAdd.Count = 0 Then
            GraphsToAdd.Add(New Domoticz.DeviceGraphContainer(idx, Type, SubType, Name, "year", TryCast(Xaml.Application.Current.Resources("NoGraphAvailable"), DataTemplate), ""))
        End If

        For Each g In GraphsToAdd
            Await Task.Run(Function() g.Load(Me, g.datafile))
            GraphList.graphDataList.Add(g)
        Next
        app.myViewModel.Notify.Clear(True)
        Return GraphList

    End Function

    Public Async Sub ShowDeviceGraphs()
        WriteToDebug("Device.ShowDeviceDetails()", "executed")
        Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
        vm.IdleTimer.StopCounter()
        Dim GraphList As GraphListViewModel = Await GetDeviceGraphData()
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
        Dim details As New ucDevice_GraphsList()
        details.DataContext = GraphList
        cDialog.Content = details
        Await cDialog.ShowAsync()
        GraphList.Dispose()
        vm.IdleTimer.StartCounter()
    End Sub


    Public Async Function LoadStatus() As Task(Of DeviceModel)
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim response As HttpResponseMessage
        If Type = "Group" Or Type = "Scene" Then
            response = Await Task.Run(Function() (New Domoticz).DownloadJSON((New DomoApi).getAllScenes()))
        Else
            response = Await Task.Run(Function() (New Domoticz).DownloadJSON((New DomoApi).getDeviceStatus(Me.idx)))
        End If

        If response.IsSuccessStatusCode Then
            Dim deserialized = JsonConvert.DeserializeObject(Of DevicesModel)(Await response.Content.ReadAsStringAsync)
            Dim myDevice As DeviceModel = (From dev In deserialized.result Where dev.idx = idx Select dev).FirstOrDefault()
            If Not myDevice Is Nothing Then
                Return myDevice
            Else
                Await app.myViewModel.Notify.Update(True, "couldn't get device's status", 2, False, 2)
                Return Nothing
            End If
        Else
            Await app.myViewModel.Notify.Update(True, "couldn't get device's status", 2, False, 2)
            Return Nothing
        End If
    End Function

    ''' <summary>
    ''' Handles the update of properties for DeviceViewModel, based on a received update of a DeviceModel. If no updated
    ''' DeviceModel has been sent, the DeviceViewModel will request one. If there are certain values of a device that do not
    ''' seem to get updated during a refresh, they are very likely not included here.
    ''' </summary>
    ''' <param name="d">A DeviceModel containing updated values for the device</param>
    ''' <returns></returns>
    Public Async Function Update(Optional d As DeviceModel = Nothing) As Task
        'If we haven't sent an updated device to this function, retrieve the device's latest status from the server
        If d Is Nothing Then
            d = Await LoadStatus()
        End If

        If Not d Is Nothing Then
            Level = d.Level
            If (SwitchType = Constants.DEVICE.SWITCHTYPE.DIMMER Or SwitchType = Constants.DEVICE.SWITCHTYPE.SELECTOR) AndAlso MaxDimLevel <> 0 Then
                LevelInt = Math.Floor((100 / MaxDimLevel) * d.LevelInt)
            End If
            LastUpdate = d.LastUpdate
            Direction = d.Direction
            DirectionStr = d.DirectionStr
            Speed = d.Speed
            Gust = d.Gust
            Data = d.Data
            Status = d.Status
            Counter = d.Counter
            CounterToday = d.CounterToday
            CounterDeliv = d.CounterDeliv
            CounterDelivToday = d.CounterDelivToday
            Usage = d.Usage
            UsageDeliv = d.UsageDeliv
        End If

    End Function

    Public Async Function SwitchGroup(ToStatus As String) As Task
        Await SwitchDevice(ToStatus)
    End Function


    Public Async Sub SwitchDeviceCommand()
        If Me.CanBeSwitched Then
            Dim ret As retvalue = Await SwitchDevice()
        Else
            'Only get the status of the device if it can't be switched
            Await Update()
        End If

    End Sub


    Public Async Function SetRGBValues(hex As String) As Task(Of retvalue)
        WriteToDebug("DeviceViewModel.SetRGBValues()", "executed")
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        Dim url As String = (New DomoApi).setRGBDimmer(Me.idx, hex)
        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
            Return New retvalue With {.err = "Error switching device", .issuccess = 0}
        Else
            If Not response.Content Is Nothing Then
                Dim domoRes As Domoticz.Response
                Try
                    domoRes = JsonConvert.DeserializeObject(Of Domoticz.Response)(Await response.Content.ReadAsStringAsync())
                    If domoRes.status <> "OK" Then
                        Await app.myViewModel.Notify.Update(True, domoRes.message, 2, False, 2)
                        Return New retvalue With {.err = "Error switching device", .issuccess = 0}
                    Else
                        Await app.myViewModel.Notify.Update(False, "Device switched", 1, False, 2)
                    End If
                    Await Me.Update()
                    domoRes = Nothing
                    SwitchingToState = ""
                    Return New retvalue With {.issuccess = 1}
                Catch ex As Exception
                    app.myViewModel.Notify.Update(True, "Server sent empty response", 2, False, 2)
                    Return New retvalue With {.issuccess = 0, .err = "server sent empty response"}
                End Try
            End If
            Return New retvalue With {.issuccess = 0, .err = "server sent empty response"}
        End If
    End Function


    'Triggers closing the Password Prompt Dialog Box
    Public Sub ClosePasswordPrompt()
        Dim cDialog As ContentDialog = CType(Application.Current, Application).myViewModel.CurrentContentDialog
        If Not cDialog Is Nothing Then
            cDialog.Hide()
        End If
    End Sub

    ''' <summary>
    ''' Shows a Password Prompt Dialog with a OK button. By clicking the OK button or ESC/Back button the Dialog dissapears
    ''' </summary>
    ''' <returns></returns>
    Public Async Function ShowPasswordPrompt() As Task
        WriteToDebug("DeviceViewModel.ShowPasswordPrompt()", "executed")
        Dim vm As TiczViewModel = CType(Application.Current, Application).myViewModel
        vm.IdleTimer.StopCounter()
        vm.CurrentContentDialog = New ContentDialog
        'Because we use a customized ContentDialog Style, the ESC key handler didn't work anymore. Therefore we add our own. 
        Dim escapekeyhandler = New KeyEventHandler(Sub(s, e)
                                                       If e.Key = Windows.System.VirtualKey.Escape Then
                                                           PassCode = ""
                                                           vm.CurrentContentDialog.Hide()
                                                       End If
                                                   End Sub)
        vm.CurrentContentDialog.AddHandler(UIElement.KeyDownEvent, escapekeyhandler, True)
        vm.CurrentContentDialog.Title = Me.Name
        vm.CurrentContentDialog.Style = CType(Application.Current.Resources("HalfScreenContentDialog"), Style)
        vm.CurrentContentDialog.MaxHeight = Window.Current.Bounds.Height
        vm.CurrentContentDialog.VerticalAlignment = VerticalAlignment.Stretch
        vm.CurrentContentDialog.VerticalContentAlignment = VerticalAlignment.Stretch
        vm.CurrentContentDialog.IsPrimaryButtonEnabled = True
        vm.CurrentContentDialog.PrimaryButtonText = "OK"
        Dim password As New ucDevice_Password()
        password.DataContext = Me
        vm.CurrentContentDialog.Content = password
        Await vm.CurrentContentDialog.ShowAsync()
        vm.CurrentContentDialog = Nothing
        vm.IdleTimer.StartCounter()
    End Function


    Public Async Function SwitchDevice(Optional forcedSwitchToState As String = "") As Task(Of retvalue)
        Dim app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
        If Not forcedSwitchToState = "" Then SwitchingToState = forcedSwitchToState
        'Check if the device is password protected. If so, show the password prompt
        If [Protected] And PassCode = "" Then
            Await ShowPasswordPrompt()
            'If the PassCode is still empty, don't do anything
            If PassCode = "" Then Exit Function
        End If

        'Identify what kind of device we are and in what state we're in in order to perform the switch
        Dim url As String
        Select Case Type
            Case Constants.DEVICE.TYPE.THERMOSTAT
                url = (New DomoApi).setSetpoint(Me.idx, forcedSwitchToState)
            Case Constants.DEVICE.TYPE.GROUP
                If SwitchingToState = "" Then
                    If Me.Status = Constants.DEVICE.STATUS.OFF Or Me.Status = "Mixed" Then SwitchingToState = Constants.DEVICE.STATUS.ON Else SwitchingToState = Constants.DEVICE.STATUS.OFF
                End If
                url = (New DomoApi).SwitchScene(Me.idx, SwitchingToState, PassCode)
            Case Constants.DEVICE.TYPE.SCENE
                url = (New DomoApi).SwitchScene(Me.idx, Constants.DEVICE.STATUS.ON, PassCode)
            Case Else
                Select Case SwitchType
                    Case Nothing
                        Exit Select
                    Case Constants.DEVICE.SWITCHTYPE.PUSH_ON_BUTTON
                        url = (New DomoApi).SwitchLight(Me.idx, Constants.DEVICE.STATUS.ON, PassCode)
                    Case Constants.DEVICE.SWITCHTYPE.PUSH_OFF_BUTTON
                        url = (New DomoApi).SwitchLight(Me.idx, Constants.DEVICE.STATUS.OFF, PassCode)
                    Case Constants.DEVICE.SWITCHTYPE.DIMMER
                        If SwitchingToState = "" Then
                            If Me.Status = Constants.DEVICE.STATUS.OFF Then SwitchingToState = Constants.DEVICE.STATUS.ON Else SwitchingToState = Constants.DEVICE.STATUS.OFF
                        End If
                        url = (New DomoApi).setDimmer(idx, SwitchingToState, PassCode)
                    Case Constants.DEVICE.SWITCHTYPE.SELECTOR
                        If SwitchingToState = "" Then
                            If Me.Status = Constants.DEVICE.STATUS.OFF Then SwitchingToState = Constants.DEVICE.STATUS.ON Else SwitchingToState = Constants.DEVICE.STATUS.OFF
                        End If
                        url = (New DomoApi).setDimmer(idx, SwitchingToState, PassCode)
                    Case Else
                        If SwitchingToState = "" Then
                            If Me.isOn Then SwitchingToState = Constants.DEVICE.STATUS.OFF Else SwitchingToState = Constants.DEVICE.STATUS.ON
                        End If
                        url = (New DomoApi).SwitchLight(Me.idx, SwitchingToState, PassCode)
                End Select
        End Select

        If url = "" Then
            Await app.myViewModel.Notify.Update(True, "Don't know how to switch :(", 2, False, 2)
            Exit Function
        End If

        Dim response As HttpResponseMessage = Await Task.Run(Function() (New Domoticz).DownloadJSON(url))
        PassCode = ""
        If Not response.IsSuccessStatusCode Then
            Await app.myViewModel.Notify.Update(True, "Error switching device", 2, False, 2)
            Return New retvalue With {.err = "Error switching device", .issuccess = 0}
        Else
            If Not response.Content Is Nothing Then
                Dim domoRes As Domoticz.Response
                Try
                    domoRes = JsonConvert.DeserializeObject(Of Domoticz.Response)(Await response.Content.ReadAsStringAsync())
                    If domoRes.status <> "OK" Then
                        Await app.myViewModel.Notify.Update(True, domoRes.message, 2, False, 2)
                        Return New retvalue With {.err = "Error switching device", .issuccess = 0}
                    Else
                        Await app.myViewModel.Notify.Update(False, "Device switched", 1, False, 2)
                    End If
                    Await Me.Update()
                    domoRes = Nothing
                    SwitchingToState = ""
                    Return New retvalue With {.issuccess = 1}
                Catch ex As Exception
                    app.myViewModel.Notify.Update(True, "Server sent empty response", 2, False, 2)
                    Return New retvalue With {.issuccess = 0, .err = "server sent empty response"}
                End Try
            End If
            Return New retvalue With {.issuccess = 0, .err = "server sent empty response"}
        End If



    End Function


#End Region
#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
                Me._Device = Nothing
                Me._Configuration = Nothing
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
#End Region
End Class
