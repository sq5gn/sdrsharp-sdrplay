# SDRplay RSPduo for SDR#

**English** | [Polski](README.pl.md)

An SDR# plugin that lets SDR# receive with an SDRplay RSPduo through the SDRplay API 3.

SDR# 1922 and later (the .NET 10 build) load processing plugins but not third-party IQ sources. This plugin
bridges that gap: it hosts a local SpyServer-compatible server inside SDR#, drives the RSPduo through the
official SDRplay API, and feeds the samples to SDR#'s built-in **AIRSPY Server Network** source.

- [Features](#features)
- [How it works](#how-it-works)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Known limitations](#known-limitations)
- [Building from source](#building-from-source)
- [Acknowledgements](#acknowledgements)
- [Licence](#licence)

## Features

- Sample rates of 2, 6, 8 and 10 MS/s with the matching IF filters; narrower bandwidths via SDR# decimation.
- Full RSPduo gain control from the plugin window: LNA state (dB values from the SDRplay tables per band
  and port), AGC (Off / Fast / Medium / Slow), AGC set-point and manual IF gain reduction.
- Antenna selection while receiving: Tuner 1 50 Ω, Tuner 1 Hi-Z, Tuner 2 50 Ω.
- **Diversity** (RSPduo Dual Tuner mode): both tuners receive at once, 2 MS/s each, and SDR# gets one stream:
  Tuner 1, Tuner 2 or the sum Tuner 1 + w · Tuner 2. The weight w is set on a phase wheel and a gain slider, or
  found automatically: **Null** cancels the strongest interference or a signal marked with the SDR# filter
  (for example a neighbouring station), **Max** adds the tuners in phase.
  Gain is set per tuner. See the [user guide](docs/USER-GUIDE.md#6-diversity).
- Bias-T (Tuner 2), MW/FM, DAB and AM notch filters, frequency correction in ppm.
- Three dockable windows: receiver state, SpyServer client state and Diversity.
- Light and dark theme following SDR#.
- All settings are stored in `SDRSharp.config`.
- Local only: the bridge listens on `127.0.0.1`.

## How it works

```
RSPduo --(SDRplay API 3)--> plugin (SpyServer bridge, 127.0.0.1:5555) --> SDR# source "AIRSPY Server Network"
```

The receiver is controlled from the plugin window, as its maker designed it; the SDR# gain slider is hidden.
SDR# itself sets the centre frequency and the bandwidth (decimation).

## Requirements

- Windows x64.
- SDR# 1922 or later (the .NET 10 build).
- SDRplay API 3.15 installed, with the SDRplay service running.
- An SDRplay RSPduo that is not open in another program.

## Installation

You do not need the source code to use the plugin. Download the ready-made archive
`SDRplay-RSPduo-plugin-vX.Y.Z.zip` from the [Releases](../../releases) page, then:

1. Close SDR#.
2. Unpack the folder `SDRSharp.SDRplay` from the archive into the `Plugins` folder of SDR#, so that you have
   `Plugins\SDRSharp.SDRplay\SDRSharp.SDRplay.dll`.
3. Start SDR#. The plugin appears as **SDRplay RSPduo** in the plugin list.

## Quick start

1. In the **SDRplay RSPduo** window tick **Enabled**. The state label shows `Listening`.
2. In SDR# choose the source **AIRSPY Server Network** and enter `sdr://127.0.0.1:5555/`.
3. In the source settings choose the stream format **PCM 16** and tick **Use full IQ**.
4. Click play. The state label shows `Streaming`.

SDR# clears **Use full IQ** at every reconnection; tick it again each time.

You can find the full instructions [here](docs/USER-GUIDE.md). The changes between versions are in the [changelog](CHANGELOG.md).

## Known limitations

- One client at a time; a second client is refused while one is connected.
- **Use full IQ** must be ticked again after each reconnection (SDR# clears it).
- After a reconnection SDR# puts the VFO back on the centre frequency: SDR# sends the bridge only its centre
  frequency, so the VFO offset stays inside SDR#.
- A new sample rate or a new mode (Single Tuner / Diversity) applies at the next connection of the SDR# source
  (Stop, then Play).
- Diversity runs at 2 MS/s on the 50 Ω ports only; the sample rate and antenna are fixed and the AGC is off
  in this mode.
- The phase between the tuners is random after every retune: repeat Null / Max (or tick **Auto after retune**).
- Two antennas cancel one interference source at a time; the depth is limited by its coherence between them.
- Null on a marked band and Max are not verified with the SDR# frequency shift (upconverter).
- The bridge sends 16-bit IQ only (PCM 16) and no FFT.

## Building from source

The plugin was created with the SDR# Plugin Development Kit (VDK) published by Airspy. The repository does not
contain the VDK or SDR# themselves, so you have to put them next to the sources before building.

**Prerequisites**

- Windows x64 and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
  (check with `dotnet --version`).
- Git.
- The VDK (`vdk.zip`) and SDR# 1922 or later (x64 build for .NET 10), both from the
  [Airspy download page](https://airspy.com/download/).

**Folder layout**

The project file references the VDK libraries by relative paths (`..\lib\ref\*.dll` and
`..\lib\SDRSharp.PanView.dll`), so the folders must be arranged like this:

```
<workspace>\
  lib\                      <- from the VDK (contains ref\ and SDRSharp.PanView.dll)
  sdrsharp-sdrplay\         <- this repository
    sdrsharp-x64\           <- a copy of SDR# (contains SDRSharp.exe)
```

**Steps**

1. Unpack `vdk.zip` and place its `lib` folder in `<workspace>`.
2. Clone the repository into `<workspace>`:

   ```powershell
   git clone https://github.com/sq5gn/sdrsharp-sdrplay.git
   ```
3. Unpack SDR# into `sdrsharp-sdrplay\sdrsharp-x64\`, so that `sdrsharp-x64\SDRSharp.exe` exists. The
   folder is ignored by git. The build stops with an error when `SDRSharp.exe` is missing there.
4. Close SDR# if it is running (it locks the plugin files), then build:

   ```powershell
   cd sdrsharp-sdrplay
   dotnet build -c Release
   ```

5. The compiled plugin appears in `bin\Release\net10.0-windows\`. A post-build step copies
   `SDRSharp.SDRplay.dll` and `SDRSharp.SDRplay.deps.json` to `sdrsharp-x64\Plugins\SDRSharp.SDRplay\`.
6. Start `sdrsharp-x64\SDRSharp.exe`; the plugin is listed as **SDRplay RSPduo**.

The SDRplay API is loaded at run time from the folder recorded by its installer
(`HKLM\SOFTWARE\SDRplay\Service\API`, `Install_Dir`). No SDRplay header and no SDR# library is part of this
repository.

## Acknowledgements

Huge thanks to Youssef Touil (prog) of Airspy for publishing the VDK, which made this plugin possible.

Thanks also to the authors of [SDR++](https://github.com/AlexandreRouma/SDRPlusPlus) (Alexandre Rouma) and of
the [F6FLT SDR# plugin for SDRplay](https://github.com/F6FLT/SDRSharp_v17xx-Plugin-for-SDRPlay), and to the
authors of [SoapySDRPlay3](https://github.com/pothosware/SoapySDRPlay3). I read their open source projects to
understand how the SDRplay API is used, how an SDR# source is built and how the RSPduo antenna ports are
handled. No code was copied from them.

Thanks to SDRplay for the API and its documentation.

## Licence

MIT, see [LICENSE](LICENSE). SDR# and the SDRplay API are separate products with their own licences.
SDRplay and RSPduo are trademarks of SDRplay Limited; this project is independent and not affiliated with or
endorsed by SDRplay Limited or Airspy.
