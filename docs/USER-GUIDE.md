# SDRplay RSPduo for SDR# — User guide

**English** | [Polski](INSTRUKCJA.md) | [Back to README](../README.md)

- [1. Overview](#1-overview)
- [2. Installation](#2-installation)
- [3. Connecting SDR# to the receiver](#3-connecting-sdr-to-the-receiver)
- [4. Plugin window reference](#4-plugin-window-reference)
- [5. Status windows](#5-status-windows)
- [6. Working tips](#6-working-tips)
- [7. Troubleshooting](#7-troubleshooting)
- [8. Limitations](#8-limitations)

## 1. Overview

SDR# 1922 and later load processing plugins but no third-party IQ sources. The plugin runs a small
SpyServer-compatible server inside SDR# (on `127.0.0.1`, default port 5555), opens the RSPduo through the
SDRplay API 3 and streams its samples to the built-in SDR# source **AIRSPY Server Network**.

```
RSPduo --(SDRplay API 3)--> plugin (SpyServer bridge, 127.0.0.1:5555) --> SDR# source "AIRSPY Server Network"
```

Division of work:

| Controlled in the plugin window | Controlled in SDR# |
|---|---|
| Gain (LNA, AGC, IF), sample rate, antenna, Bias-T, notches, ppm | Centre frequency, bandwidth (decimation), demodulation |

The SDR# gain slider is hidden because gain belongs to the plugin window.

## 2. Installation

Requirements:

- Windows x64.
- SDR# 1922 or later (the .NET 10 build).
- SDRplay API 3.15 installed; the SDRplay service must be running.
- An SDRplay RSPduo that is not in use by another program.

Steps:

1. Download `SDRplay-RSPduo-plugin-vX.Y.Z.zip` from the repository's Releases page.
2. Close SDR#.
3. Unpack the folder `SDRSharp.SDRplay` from the archive into the `Plugins` folder of SDR#. It holds
   `SDRSharp.SDRplay.dll` and `SDRSharp.SDRplay.deps.json`.
4. Start SDR#. **SDRplay RSPduo** appears in the plugin list and as a window.

The SDRplay API is found automatically through the registry entry written by its installer.

## 3. Connecting SDR# to the receiver

1. In the **SDRplay RSPduo** window tick **Enabled**. The state label shows `Listening`.
2. In SDR# open the source list and choose **AIRSPY Server Network**.
3. Enter the address `sdr://127.0.0.1:5555/` (the port is the plugin's **TCP Port**).
4. Open the source settings, choose **PCM 16** and tick **Use full IQ**.
5. Click play. The state label shows `Streaming`.

Notes:

- SDR# clears **Use full IQ** at every reconnection. The plugin window then says
  `Tick "Use full IQ" in the source`.
- A new **Sample Rate** applies when the SDR# source connects the next time (stop and play again).
- Narrower bandwidths are chosen in SDR# (decimation 2 to 32) and do not need any change in the plugin.
- All plugin settings are saved in `SDRSharp.config` (keys `plugin.sdrplay.*`) when SDR# closes.

## 4. Plugin window reference

| Setting | Description |
|---|---|
| **Enabled** | Starts and stops the bridge. Stopping the receiver takes about 1.5 s. |
| **TCP Port** | Port of the bridge on `127.0.0.1` (default 5555). Must match the port in the SDR# source address. |
| **LNA State** | RF gain-reduction step. The dB value comes from the SDRplay table for the tuned band and the selected port, and is clamped when the band or port changes. |
| **AGC** | **Off**, **Fast**, **Medium** or **Slow**: the AGC loop bandwidth of 100, 50 or 5 Hz (higher is faster). Default **Medium**. |
| **AGC Setpoint (dBFS)** | Target signal level of the AGC. |
| **IF Gain Reduction** | Manual IF gain reduction, 20–59 dB. Used while the AGC is **Off**. |
| **Sample Rate** | 2, 6, 8 or 10 MS/s, each with the matching IF filter. |
| **Antenna** | **Tuner 1 50 Ω**, **Tuner 1 Hi-Z** (high impedance, intended for frequencies below 60 MHz) or **Tuner 2 50 Ω**. Can be changed while receiving. |
| **Bias-T** | DC supply on the Tuner 2 port. Available only with Tuner 2 selected. Off by default. |
| **MW/FM Notch** | Broadcast MW/FM band-stop filter. |
| **DAB Notch** | DAB band-stop filter. |
| **AM Notch** | AM band-stop filter of Tuner 1; not available with Tuner 2. |
| **Frequency Correction (ppm)** | Correction of the reference oscillator, −100 to +100 ppm. Press Enter or leave the field to apply a typed value. |

**Caution — Bias-T** powers whatever is connected to the Tuner 2 port (for example an active antenna or LNA).
Enable it only when the connected device expects DC; otherwise leave it off.

## 5. Status windows

Two icon buttons at the bottom of the plugin window show and hide two windows docked in SDR#.

**RSPduo** (antenna icon)

| Row | Meaning |
|---|---|
| Device | Model, serial number and the selected antenna. |
| Frequency | Current centre frequency. |
| Sample rate | Configured sample rate. |
| Decimation | Decimation requested by SDR#. |
| Gain reduction | Total gain reduction in dB (LNA plus IF). |
| System gain | System gain reported by the receiver. |
| ADC overload | Warning when the ADC is overloaded. Raise the LNA state or the IF gain reduction. |
| Measured rate | Actual sample rate delivered by the receiver. |
| Overruns | Count of sample blocks dropped because the client was too slow. |
| Device status | State of the device; API errors are shown here in full. |

**SpyServer Client** (monitor icon)

| Row | Meaning |
|---|---|
| Client | Connected client name. |
| Protocol | Protocol version of the client. |
| IQ format | Format of the stream (PCM 16). |
| Mode | Streaming mode requested by the client. |
| Streaming | Whether samples are currently sent. |
| Digital gain | Digital gain requested by the client. |
| Messages sent | Number of messages sent to the client. |
| Last command | Last command received from the client. |

Both windows scroll when their content does not fit.

## 6. Working tips

- **Overload.** If the ADC overload indicator shows, increase the LNA state (more attenuation) before anything
  else; strong broadcast stations are the usual cause. The notch filters help near the MW/FM or DAB bands.
- **Low frequencies.** Use **Tuner 1 Hi-Z** below 60 MHz for long-wire and loop antennas; use the 50 Ω ports
  for ordinary antennas.
- **Switching antennas.** The change is applied immediately; gain settings are re-sent automatically.
- **Wide band.** 10 MS/s gives the widest view; reduce the sample rate if the computer cannot keep up
  (watch the **Overruns** counter).
- **Frequency accuracy.** Measure a known carrier and enter the offset in **Frequency Correction (ppm)**.
- After reconnecting, SDR# returns the VFO to the centre frequency; retune after the reconnection.

## 7. Troubleshooting

| Symptom | Cause and remedy |
|---|---|
| SDR# reports "couldn't connect" | **Enabled** is not ticked in the plugin window, or the port in the source address differs from **TCP Port**. |
| Message `Tick "Use full IQ" in the source` | SDR# cleared the option on reconnection. Tick **Use full IQ** in the source settings. |
| "RSPduo error (see Status)" | Open the RSPduo window and read **Device status**. Usually the RSPduo is used by another program, is not connected, or the SDRplay API or its service is missing. |
| SDR# shows "format … not supported" | A stream format other than **PCM 16** is selected. Choose **PCM 16**. |
| Second connection is refused | Only one client at a time is supported. Disconnect the first client. |
| Stream stutters, **Overruns** grows | The computer cannot process the data rate. Lower the sample rate or close other programs. |
| VFO jumps to the centre frequency after reconnecting | Expected; see [Limitations](#8-limitations). |
| Plugin is not listed | Check the path `Plugins\SDRSharp.SDRplay\SDRSharp.SDRplay.dll` and that SDR# is version 1922 or later (.NET 10). |

## 8. Limitations

- One client at a time.
- **Use full IQ** must be ticked again after each reconnection.
- After a reconnection SDR# resets the VFO to the centre frequency (SDR# sends the bridge only its centre
  frequency).
- A new sample rate applies at the next connection of the source.
- Single tuner mode only; no dual tuner or diversity reception.
- PCM 16 only; no FFT is sent by the bridge.
