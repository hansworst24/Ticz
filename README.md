# Ticz

Universal Windows App, written in VB.net, for controlling Domoticz (www.domoticz.com)

Updated for : Ticz v1.4.0

Initial goal of this app is to provide a Domoticz App in the Windows Store for use on Windows 10 Mobile devices. Secondary goal is to allow the app to run on Windows 10 IoT, in conjunction with a Pi2 and Touchscreen in order to allow the app to be used as a (wallmounted) controller for Domoticz. IoT is still Beta, and support for touchscreens for Windows 10 IoT is very limited at this time (the only one MS has on their hardware compatibility list seems sold out and provided by a very small company in Malaysia

Support for Windows 10 (tablet/pc) is basically automatically implemented.

The App will only interface with Domoticz for primary tasks, as reading out devices and switching devices on off. The App's intention is NOT to be able to create/modify/delete devices from within Domoticz. The webGUI of Domoticz is a much better place for doing this.

The Master release will contain a WIP version of the app, with all latest commits. Releases that make it to the Windows Store will be marked as seperate versions.

## Requirements / Before you begin

- Domoticz needs to be configured with Basic-Auth authentication for Authentication to work in Ticz. If configured with Login Page, it'll show 'Unauthorized'
- Domoticz needs to be configured with Roomplans, in order for Devices to show up. 

## Room Configuration
- Within the Settings menu you will find Room Configuration. Here you can define 
  - which Room you want to load at Ticz startup
  - if you want Ticz to add a 'All Devices' room
  - for each room, if it's visible in Ticz and what layout you want it to have

### Room Layouts
Because Ticz can be used on both mobile/tablet/PC screens, it would be nice if we can choose the way the Devices are shown. This is handled through the Room Layout option.

- Icon View : The most basic view for your Devices. No additional information is shown apart from basic info in the Footer. The icon handles On/Off switching or in case of non-switchable devices, asks Domoticz for an update for the device. All devices are grouped as within Domoticz itself, and the order of the devices cannot be changed, but should reflect the order within Domoticz. Devices are made slightly wider or smaller in order to fit nicely on the screen.
- List View : Provides Wide-View Devices in a List format, using the complete screen width.
- Grid View : Provides a 'Icon View' type of layout, but instead shows the devices in Wide View. Here devices are also made slightly wider or smaller in order to fit the whole screen. Within the general settings, there's an option for Grid View to provide a minimum amount of Colums that the Grid View will use. It comes in handy to tune how wide devices will become, depending on your screen width
- Resize View* : Is able to provide devices in both Icon/Wide and Large View. Right-clicking or long-tapping a device (preferrably on the header or footer to avoid switching), will allow to choose the View for the Device. When changed, the device will be re-added to the view on the same place. This view doesn't support reordering
- Dashboard View* : As might be guessed, this view is ment to provide a Dashboard like view which contains your key Devices in the way you want them to have. No grouping is used, to allow for maximum display area. Devices can be resized just like the Resize View, but can also be re-ordered by selecting "Move Up / Move Down" (see it as a vertical list) :). Ticz stores the order op the devices independantly in a XML file for later use.

* NOTE : Resize View and Dashboard View are relatively slow in loading compared to Icon View/List View and Grid View. This is a technical limitation as these views don't support Virtualization, because of the varying Device size. On Phones and slow tablets I would recommend to use only the Icon/List and Grid Views. On desktops it probably doesn't matter that much which view you'll use :). The Dashboard View can also be used on a slow device (like Windows IoT on a Pi2), but stay statically and use the Background Refresh to update the Devices every one and a while.

## Refreshing
Ticz supports 3 'methods' for refreshing your devices :)
- The background Refresh options, found in the settings menu. When set to a greater value than 0, it will ask Domoticz for all updated devices since the last update. It parses that information and updated any Devices that have been updated. 
- The Refresh button in the Bottom Buttonbar on the Right side. This button will ask Domoticz for ALL devices in the room, no matter if they have been updated or not. 
- Clicking the icon of a device ! When it's a switchable device, it will switch the device and immediately ask for the latest status. But when it is a non-switchable device, like the default Motherboard sensors (CPU% etc), clicking the icon will get an updated status from Domoticz as well.

## Known Bugs
- I'm trying to record any known bugs/issues within Githubs own Issues option. Therefore please have a look there if you want to know which bugs/issues are known.


## Additional Information

- For both testing purposes as well as troubleshooting, the App contains a simple mechanism, to selectively load devices. When you create a "Ticz" room on your Domoticz server, Ticz will only load devices within this Room, and ignore any other Devices or Rooms. This might come in handy when issues occur with Ticz, which might be caused by a specific Device Type.
- Ticz was written and tested with the following configuration, therefore devices that weren't part of this setup might not work properly as they need specific configuration :
  - Domoticz v2.4015
  - Fibaro Door Sensors / Wallplugs / Relay Switches
  - GreenWave Powernode 1 and 6
  - Weather Underground Plugion
  - Logitech Media Server plugin


