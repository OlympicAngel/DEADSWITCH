# Native widgets (doc 08 s5)

Home-screen / lock-screen widget showing the threat level, the hottest faction's heat and the next forecast timer.
The game side is live already (`Runtime/Notifications/WidgetBridge.cs` writes a snapshot whenever the app goes to the
background). These native parts sit in this `~` folder so Unity does not import them until you install them: they
need a device build to verify.

## Android
1. Copy `android/DeadswitchWidget.androidlib` to `unity/Assets/Plugins/Android/`.
2. Build with the one-click Android build. Long-press the home screen > Widgets > DEADSWITCH.
The provider reads SharedPreferences `deadswitch_widget` (keys `threat`, `heat`, `next`); the game calls
`DeadswitchWidget.refresh` after writing. Without the plugin the refresh call is skipped.

## iOS (iOS 17+ for `containerBackground`; lock screen via `.accessoryRectangular`)
1. Copy `ios/Plugins/DsWidget.mm` to `unity/Assets/Plugins/iOS/` and add `DS_IOS_WIDGET` to the iOS scripting define
   symbols (Player Settings).
2. In the generated Xcode project add a Widget Extension target, replace its Swift file with
   `ios/DeadswitchWidget/DeadswitchWidget.swift`.
3. Give both the app and the extension the App Group `group.com.deadswitch.shared` (Signing & Capabilities).
