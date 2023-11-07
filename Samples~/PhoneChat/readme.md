# Phone Chat

This sample demonstrate a text message style phone conversation template using custom views and commands.
This is a complete overhaul of the built in systems and uses custom code and layout groups to create a dynamic UI.

This sample highlights:

- custom dialogue views
- custom commands
- highly customised UI

To start the sample play the scene.
Dialogue will start and advance automatically by itself.
To select an option click on it in the UI.

## PhoneChatDialogueHelper.cs

This class is the dialogue view subclass and manages all the custom UI including moving the dialogue up the phone screen as new lines come in.
This also registers two commands `<<Me>>` and `<<Them>>` which it uses to change which type of lines get shown on the phone.
