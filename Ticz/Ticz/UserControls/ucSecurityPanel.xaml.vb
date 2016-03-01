' The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

Public NotInheritable Class ucSecurityPanel
    Inherits UserControl

    Private app As Application = CType(Windows.UI.Xaml.Application.Current, Application)
    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        AddHandler app.myViewModel.DomoSecPanel.PlayDigitSoundRequested, AddressOf PlayDigitSound
        AddHandler app.myViewModel.DomoSecPanel.PlayArmRequested, AddressOf PlayArm
        AddHandler app.myViewModel.DomoSecPanel.PlayDisArmRequested, AddressOf PlayDisarm
        AddHandler app.myViewModel.DomoSecPanel.PlayWrongCodeRequested, AddressOf PlayWrongCode
        ' Add any initialization after the InitializeComponent() call.

    End Sub


    Public Sub PlayDigitSound()
        DigitSound.Play()
    End Sub

    Public Sub PlayArm()
        ArmSound.Play()
    End Sub
    Public Sub PlayDisarm()
        DisArmSound.Play()
    End Sub

    Public Sub PlayWrongCode()
        WrongCodeSound.Play()
    End Sub
End Class
