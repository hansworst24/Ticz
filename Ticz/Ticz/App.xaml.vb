Imports GalaSoft.MvvmLight.Threading
''' <summary>
''' Provides application-specific behavior to supplement the default Application class.
''' </summary>
NotInheritable Class App
    Inherits Application

    Public myViewModel As New TiczViewModel

    ''' <summary>
    ''' Initializes a new instance of the App class.
    ''' </summary>
    Public Sub New()
        Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
            Microsoft.ApplicationInsights.WindowsCollectors.Metadata Or
            Microsoft.ApplicationInsights.WindowsCollectors.Session)
        InitializeComponent()
    End Sub

    ''' <summary>
    ''' Invoked when the application is launched normally by the end user.  Other entry points
    ''' will be used when the application is launched to open a specific file, to display
    ''' search results, and so forth.
    ''' </summary>
    ''' <param name="e">Details about the launch request and process.</param>
    Protected Overrides Sub OnLaunched(e As Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)
#If DEBUG Then
        ' Show graphics profiling information while debugging.
        If System.Diagnostics.Debugger.IsAttached Then
            ' Display the current frame rate counters
            Me.DebugSettings.EnableFrameRateCounter = True
        End If
#End If
        'Add BackKeyHandler for HardwareButtons
        AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf App_BackRequested

        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)


        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If rootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = New Frame()
            If myViewModel.TiczSettings.UseDarkTheme Then rootFrame.RequestedTheme = ElementTheme.Dark Else rootFrame.RequestedTheme = ElementTheme.Light
            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed

            If e.PreviousExecutionState = ApplicationExecutionState.Terminated Then
                ' TODO: Load state from previously suspended application
            End If
            ' Place the frame in the current Window
            Window.Current.Content = rootFrame
        End If
        If rootFrame.Content Is Nothing Then
            ' When the navigation stack isn't restored navigate to the first page,
            ' configuring the new page by passing required information as a navigation
            ' parameter
            rootFrame.Navigate(GetType(SplitView), e.Arguments)
        End If
        ApplicationView.GetForCurrentView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow)
        ApplicationView.PreferredLaunchViewSize = New Size(800, 480)
        ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize
        If (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar")) Then
            Dim sBar As StatusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView()
            If Not sBar Is Nothing Then
                sBar.HideAsync()
            End If
        End If
        'If (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons")) Then
        '    TiczViewModel.ShowBackButtonBar = False
        'Else
        '    TiczViewModel.ShowBackButtonBar = True
        'End If

        '        var service = FirstFloor.XamlSpy.Services.XamlSpyService.Current;
        'service.Connect("[address]", [port], "[password]";

        'AddHandler ApplicationView.GetForCurrentView.VisibleBoundsChanged, AddressOf VisibleBoundsChanged
        ' Ensure the current window is active
        DispatcherHelper.Initialize()
        Window.Current.Activate()
    End Sub

    Public Sub VisibleBoundsChanged(sender As ApplicationView, args As Object)
        WriteToDebug("App.VisibleBoundsChanged", "executed")
    End Sub


    Public Sub App_BackRequested(sender As Object, e As Windows.UI.Core.BackRequestedEventArgs)
        If myViewModel.TiczMenu.IsMenuOpen Then myViewModel.TiczMenu.IsMenuOpen = False
        WriteToDebug("App.App_BackRequested", "executed")
        Dim rootFrame As Frame = CType(Window.Current.Content, Frame)
        If rootFrame Is Nothing Then Exit Sub
        If rootFrame.CanGoBack AndAlso e.Handled = False Then
            e.Handled = True
            rootFrame.GoBack()

        End If

    End Sub

    ''' <summary>
    ''' Invoked when Navigation to a certain page fails
    ''' </summary>
    ''' <param name="sender">The Frame which failed navigation</param>
    ''' <param name="e">Details about the navigation failure</param>
    Private Sub OnNavigationFailed(sender As Object, e As NavigationFailedEventArgs)
        Throw New Exception("Failed to load Page " + e.SourcePageType.FullName)
    End Sub

    ''' <summary>
    ''' Invoked when application execution is being suspended.  Application state is saved
    ''' without knowing whether the application will be terminated or resumed with the contents
    ''' of memory still intact.
    ''' </summary>
    ''' <param name="sender">The source of the suspend request.</param>
    ''' <param name="e">Details about the suspend request.</param>
    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs) Handles Me.Suspending
        Dim deferral As SuspendingDeferral = e.SuspendingOperation.GetDeferral()
        ' TODO: Save application state and stop any background activity
        deferral.Complete()
    End Sub

End Class
