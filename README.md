# Interceptor Message Flow App
This `im-flow` .NET Core console application summarizes the message flow between Genesys, the Interceptor, and the SSC.  

![Screenshot](assets/screen-01.png)

For this screenshot, the output shows messages as follows:
* Interceptor sent `PlaceOutboundCallResponseMessage` to SSC
* Genesys sent four `EventUserEvent`s to Interceptor
* Genesys sent `EventAttachedDataChanged` to Interceptor
* Genesys sent `EventEstablished` to Interceptor
* Genesys sent `EventUserEvent` to Interceptor
* SSC sent `EmployeePresenceChangingMessage` to Interceptor
* Interceptor sent `RequestQueryAddress` to Genesys
* and so on...

## Example Usage
By default, this app writes its output to the console.  The app can be run for a given Interceptor log file using either:

```
.\im-flow.exe c:\some-path\interceptor.log
```

...or:

```
.\im-flow.exe -i c:\some-path\interceptor.log
```

Note that the application, by default, will modify the width of the console in which it is run to fit the output.  To disable that, use the `-x` command-line parameter:

```
.\im-flow.exe -x -i c:\some-path\interceptor.log
```

## Write Output to File
The application can also be write its output to a file using the `-o` command-line parameter:

```
.\im-flow.exe -i c:\some-path\interceptor.log -o c:\some-path\results.txt
```

## Opening Results in an Editor
The application can also automatically open the results in whatever application is registered with Windows to open files using the `-e` command-line parameter.

For instance, if you specify to write the results to a file named `results.log` and you have `.log` files registered with Windows to open using [Visual Studio Code](https://code.visualstudio.com/):

```
.\im-flow.exe -e -i c:\some-path\interceptor.log c:\some-path\results.log
```

...then the application will save the results to that file and then open the file in Visual Studio Code.

Alternately, you can leave off the name of the output file (while using the `-e` command-line parameter)—in which case, the application will save the results to a temp file ending in `.txt` and then open it in whatever application is registered in Windows to open `.txt` files.

```
.\im-flow.exe -e -i c:\some-path\interceptor.log
```
