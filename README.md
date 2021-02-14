FeedReader
==

![Master CI](https://github.com/feedreaderorg/feedreader/workflows/Master%20CI/badge.svg)
![Release CI](https://github.com/feedreaderorg/feedreader/workflows/Release%20CI/badge.svg)

Build an online web feed reader with blazor web assembly.

## Screen Shots

Website: https://feedreader.org

![](screenshots/screen-shot-1.png)
![](screenshots/screen-shot-2.png)
![](screenshots/screen-shot-3.png)
![](screenshots/screen-shot-4.png)

## Get Started

1. Install [VS 2019](https://visualstudio.microsoft.com/vs/). Community version is good enough.
   Install `ASP.NET and web development` workload. Make sure `Cloud tools for web development` is also selected.

1. Install [DotNet 5.0](https://dotnet.microsoft.com/download/dotnet/5.0).

1. Use the following command to clone the source code:

        git clone https://github.com/feedreaderorg/feedreader.git

1. Start `Microsfot Azure Storage Emulator` from start menu. Or you can run the following command directly:

        C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator>AzureStorageEmulator.exe start

1. Launch `FeedReader.Server` project. Open browser and navigate to `https://localhost`.
