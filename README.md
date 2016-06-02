# __Ticz__
## Universal Windows App, written in VB.net, for controlling Domoticz (www.domoticz.com)
#### Updated for : Ticz v1.9.2
---
## What and why is Ticz ?
Ticz is being developed primarily as a Windows 10 Mobile app, to control your devices/lights from Domoticz. Although Domoticz implements a powerful WebGUI, I had trouble using this on Windows 10 Mobile in combination with the Edge browser. My credentials weren't stored properly, so when I was outside my LAN, I always had to retype my credentials on the Login page. Long story short ; It took me too long before I was able to switch a light :)

As I had gained a little experience in programming for Windows Store with TV Head (control your TVHeadend server), I decided it might be best to write an App, instead of using the WebGUI. With the advantage of Universal Apps, and the support for Windows IoT on Raspberry Pi2's I also saw it as a nice goal to see if the same App could be run as a (wall-mounted) "Control Panel". The support for Windows 10 on Desktops/Tablets is automatically implemented.

### Goal of Ticz
Ticz will only implement the basic primary tasks of controlling Domoticz devices. It should remain as light-weight as possible, but allow you to do all primary tasks when 'on the road'. Most features to control Domoticz are implemented now.

### Current Status
Version v1.9.2.0 supports all main features of Domoticz, including ; Devices / Camera's / Charts / Security Panel / Variables

---
# __Requirements__

## Windows 10 Mobile/Tablet/PC
Although primarily targeting Windows 10 Mobile, the app works perfectly fine (and much faster :) ) on Windows 10 Desktop/Tablet.

## Windows 10 IoT
Version v1.9.2.0 of Ticz works fine on a RPi2 running Windows IoT, although some operations obviously require more time (i.e. rendering). I currently run a RPi2 with a Chalk-Elec 7" 1280x800 IPS Touch panel, and it supports 'true' touch (not mouse emulation). Windows IoT doesn't support a soft-keyboard yet, so when setting up the app you'll need a keyboard too.

### Before you start using Ticz

- Domoticz needs to be configured with Basic-Auth authentication for Authentication to work in Ticz. If configured with Login Page, it'll show 'Unauthorized' even though your credentials match.
- Ticz follows the thought of having your devices grouped in Rooms, so you will not have to search for your device in a big long list. Therefore Domoticz needs to be configured with Rooms, in which your devices sit.

### Device Support
All standard devices are as far as I know supported and working properly. There might be specific devices (think about thermostats or other stuff) that I was not able to test in any way, not even with virtual devices. If you have such devices, and they don't work properly in Ticz, let me know and with your help in sending over some device information I can add them !

### Room Configuration
- Within the Settings menu you will find Room Configuration. Here you can define 
  - which Room you want to load at Ticz startup
  - if you want Ticz to add a 'All Devices' room
  - for each room, if it's visible in Ticz and what layout you want it to have

### Room Layouts
Because Ticz can be used on both mobile/tablet/PC screens, it would be nice if we can choose the way the Devices are shown. This is handled through the Room Layout option.

- __Icon View__ : The most basic view for your Devices. No additional information is shown apart from basic info in the Footer. The icon handles On/Off switching or in case of non-switchable devices, asks Domoticz for an update for the device. All devices are grouped as within Domoticz itself, and the order of the devices cannot be changed, but should reflect the order within Domoticz. Devices are made slightly wider or smaller in order to fit nicely on the screen.
- __List View__ : Provides Wide-View Devices in a List format, using the complete screen width.
- __Grid View__ : Provides a 'Icon View' type of layout, but instead shows the devices in Wide View. Here devices are also made slightly wider or smaller in order to fit the whole screen. Within the general settings, there's an option for Grid View to provide a minimum amount of Colums that the Grid View will use. It comes in handy to tune how wide devices will become, depending on your screen width
- __Resize View*__ : Is able to provide devices in both Icon/Wide and Large View. Right-clicking or long-tapping a device (preferrably on the header or footer to avoid switching), will allow to choose the View for the Device. When changed, the device will be re-added to the view on the same place. This view doesn't support reordering
- __Dashboard View*__ : As might be guessed, this view is ment to provide a Dashboard like view which contains your key Devices in the way you want them to have. No grouping is used, to allow for maximum display area. Devices can be resized just like the Resize View, but can also be re-ordered by selecting "Move Up / Move Down" (see it as a vertical list) :). Ticz stores the order of the devices independently in a XML file for later use.

* ___NOTE___ : Resize View and Dashboard View are relatively slow in loading compared to Icon View/List View and Grid View. This is a technical limitation as these views don't support Virtualization, because of the varying Device size. On Phones and slow tablets I would recommend to limit the amount of devices in such a view or only use the Icon/List and Grid Views. On desktops it probably doesn't matter that much which view you'll use :). The Dashboard View can also be used on a slow device (like Windows IoT on a Pi2), but stay statically and use the Background Refresh to update the Devices every one and a while.

### Refreshing
Ticz supports 3 'methods' for refreshing your devices :)
- The background Refresh options, found in the settings menu. When set to a greater value than 0, it will ask Domoticz for all updated devices since the last update. It parses that information and updated any Devices that have been updated. 
- The Refresh button in the Bottom Buttonbar on the Right side. This button will ask Domoticz for ALL devices in the room, no matter if they have been updated or not. 
- Clicking the icon of a device ! When it's a switchable device, it will switch the device and immediately ask for the latest status. But when it is a non-switchable device, like the default Motherboard sensors (CPU% etc), clicking the icon will get an updated status from Domoticz as well.

### Charts/Graphs
Since v1.6.0 Ticz supports the rendering of Graphs for most standard devices. To render the graphs I've found a Community Edition of SyncFusion's WinRT Toolkit with tons of custom controls. The graphs within this free toolkit are much more customizable than the WinRT Toolkit versions that were ported from Silverlight. They also render much faster, although the speed on phones could still be improved a little. If you're waiting for your graphs to load, it's 99% render time, which is CPU.
- For most devices, like temperature/energy consumption, lux etc, Domoticz keeps and builds daily/weekly/monthly/yearly data. Nevertheless for ON/OFF devices, Domoticz keeps ___all___ records, without any consolidation. Therefore, for ON/OFF devices any graphs are limited to the latest 200 datapoints, in order to ensure that rendering the graph will not kill your device.

### Icons
Apart from creating a functional App, I also wanted this App to look good. Taste is subjective, but I like a minimalistic overview, with few colors. Therefore I created for all standard devices a vector image which, when the device is turned on, will change in the accent color of your device.

For those who want the 'Domoticz experience', the app also supports loading the Domoticz icons, through Settings-->General-->Use Domoticz Icons. The advantage of using Domoticz icons is that any strong customization you've done for Domoticz icons (editing the /www/switch_icons.txt file), will also show up in Ticz. When using Ticz's 'internal' vector icons, these customized devices might show up with a ? icon representation
### Known Bugs
- I'm trying to record any known bugs/issues within Githubs own Issues option. Therefore please have a look there if you want to know which bugs/issues are known.


### Debugging

- For both testing purposes as well as troubleshooting, the App contains a simple mechanism, to selectively load devices. When you create a "Ticz" room on your Domoticz server, Ticz will only load devices within this Room, and ignore any other Devices or Rooms. It will also not load/save any Room customizations you've done to your room views. This might come in handy when issues occur with Ticz, which might be caused by a specific Device Type.

### My environment
- Ticz was written and tested with the following configuration, therefore devices that weren't part of this setup might not work properly as they need specific configuration :
  - Domoticz v3.5114
  - Fibaro Door Sensors / Wallplugs / Relay Switches
  - GreenWave Powernode 1 and 6
  - Weather Underground Plugion
  - Logitech Media Server plugin
  - Lots of virtual switches for testing :)
