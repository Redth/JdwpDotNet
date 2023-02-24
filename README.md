# JDWP for .NET
JDWP (Java Debug Wire Protocol) Implementation in .NET


1. Launch your app with `-D` in the `am start` command, for example:

```
adb shell am start -a "android.intent.action.MAIN" -c "android.intent.category.LAUNCHER" -n "com.companyname.mauiapp14/crc6448f7deee554f6954.MainActivity" -D
```

2. You should see the app launch with a dialog indicating it is waiting for a debugger connection.

3. Run the sample app and type in the package name (in the example above it's `com.companyname.mauiapp14`).

4. The JDWP debugger should connect now, the waiting dialog should go away, and the app should resume launching!
