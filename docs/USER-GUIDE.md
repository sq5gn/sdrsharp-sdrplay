# SDRplay RSPduo for SDR# — User guide

**English** | [Polski](INSTRUKCJA.md) | [Back to README](../README.md)

- [1. Overview](#1-overview)
- [2. Installation](#2-installation)
- [3. Connecting SDR# to the receiver](#3-connecting-sdr-to-the-receiver)
- [4. Plugin window reference](#4-plugin-window-reference)
- [5. Status windows](#5-status-windows)
- [6. Diversity](#6-diversity)
- [7. Working tips](#7-working-tips)
- [8. Troubleshooting](#8-troubleshooting)
- [9. Limitations](#9-limitations)

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
| Mode (Single Tuner / Diversity), gain (LNA, AGC, IF), sample rate, antenna, Bias-T, notches, ppm | Centre frequency, bandwidth (decimation), demodulation |

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
| **Mode** | **Single Tuner** (default) or **Diversity** (both tuners, see [section 6](#6-diversity)). Applies at the next connection of the SDR# source. |
| **LNA State** | RF gain-reduction step. The dB value comes from the SDRplay table for the tuned band and the selected port, and is clamped when the band or port changes. |
| **AGC** | **Off**, **Fast**, **Medium** or **Slow**: the AGC loop bandwidth of 100, 50 or 5 Hz (higher is faster). Default **Medium**. |
| **AGC Setpoint (dBFS)** | Target signal level of the AGC. |
| **IF Gain Reduction** | Manual IF gain reduction, 20–59 dB. Used while the AGC is **Off**. |
| **Sample Rate** | Zero IF: 2, 6, 8 or 10 MS/s, each with the matching IF filter. Low IF: **0.5 MS/s Low IF** with a 200 or 300 kHz IF filter, or **2 MS/s Low IF** with a 1.536 MHz IF filter. |
| **Antenna** | **Tuner 1 50 Ω**, **Tuner 1 Hi-Z** (high impedance, intended for frequencies below 60 MHz) or **Tuner 2 50 Ω**. Can be changed while receiving. |
| **Bias-T** | DC supply on the Tuner 2 port. Available only with Tuner 2 selected. Off by default. |
| **MW/FM Notch** | Broadcast MW/FM band-stop filter. |
| **DAB Notch** | DAB band-stop filter. |
| **AM Notch** | AM band-stop filter of Tuner 1; not available with Tuner 2. |
| **Frequency Correction (ppm)** | Correction of the reference oscillator, −100 to +100 ppm. Press Enter or leave the field to apply a typed value. |

**Caution — Bias-T** powers whatever is connected to the Tuner 2 port (for example an active antenna or LNA).
Enable it only when the connected device expects DC; otherwise leave it off.

## 5. Status windows

Three icon buttons at the bottom of the plugin window show and hide three windows docked in SDR#. The
**Diversity** window (circle with a pointer) is described in [section 6](#6-diversity).

**RSPduo** (antenna icon)

| Row | Meaning |
|---|---|
| Device | Model, serial number and the selected antenna. |
| Mode | Mode of the running connection: Single Tuner or Diversity. |
| Frequency | Current centre frequency. |
| Sample rate | Configured sample rate. |
| Decimation | Decimation requested by SDR#. |
| Gain reduction | Total gain reduction in dB (LNA plus IF). |
| System gain | System gain reported by the receiver. |
| Signal level | Level of each tuner in dBFS (Diversity only). |
| Combined Level | Level of the sum Tuner 1 + w · Tuner 2 in dBFS (Diversity only). |
| Coherence | Correlation of the two tuners, 0 to 1 (Diversity only). |
| Null Depth Limit | The deepest null the current coherence allows, 10·log10(1 − c²) (Diversity only). |
| Saturated Samples | Samples of the sum that clipped, per 100 ms (Diversity only). |
| Unpaired Packets | Packets of one tuner that arrived without the packet of the other (Diversity only). |
| ADC overload | Warning when the ADC is overloaded. Raise the LNA state or the IF gain reduction. While it lasts, a red **OVL** badge (in Diversity **OVL T1** / **OVL T2**) also shows in the top right corner of the SDR# spectrum. |
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

In Diversity the gain, system gain, signal level and overload rows show both tuners as `T1 … | T2 …`.
All three windows scroll when their content does not fit.

## 6. Diversity

Diversity receives with both RSPduo tuners at once. Connect an antenna to each tuner port; the plugin
combines the two signals into one stream for SDR#. It is meant to reduce local interference (EMI, QRM) that
the two antennas receive differently from the wanted signal.

**Switching on**

1. In the plugin window set **Mode** to **Diversity**.
2. Stop and start the SDR# source (the mode applies at the next connection; until then the state label
   shows `Reconnect the source for the new mode`).
3. Show the **Diversity** window (third icon button) and set **Output** to **Combined**.

In Diversity the plugin window changes:

- The sample rate is fixed at 2 MS/s per tuner; **Sample Rate** is disabled. Tuner 2 uses its 50 Ω port;
  **Antenna** chooses the port of Tuner 1: **Tuner 1 50 Ω + Tuner 2 50 Ω** or **Tuner 1 Hi-Z + Tuner 2 50 Ω**
  (for example a long wire on Hi-Z for listening and a local noise antenna on Tuner 2). It can be changed
  while receiving. The Hi-Z port is intended for 1 kHz – 30 MHz (RSPduo datasheet).
- The AGC is off. **LNA State** and **IF Gain Reduction** exist for each tuner (Tuner 1, Tuner 2); the button
  **Tuner 2 gain = Tuner 1 gain** copies the settings of Tuner 1 to Tuner 2.
- Bias-T acts on Tuner 2 and the AM notch on Tuner 1, as in Single Tuner mode.
- To go back, set **Mode** to **Single Tuner** and stop and start the source; the sample rate chosen in the
  plugin window is restored.

**Diversity window**

The combined signal is `Tuner 1 + w · Tuner 2`, where the weight w has a phase φ and a gain g.

| Control | Description |
|---|---|
| **Output** | What goes to SDR#: **Tuner 1**, **Tuner 2** or **Combined**. |
| **Phase (Tuner 2)** | Phase φ of Tuner 2 in the sum. Drag the pointer of the wheel, or turn the mouse wheel over it (1°, with Shift 10°). 0° is at the top, clockwise. |
| **Gain (Tuner 2)** | Level g of Tuner 2 relative to Tuner 1 in the sum, −30 to +30 dB in 0.1 dB steps. |
| **Null** | Sets φ and g so that the output power in the measured band is minimal: the strongest signal there is cancelled. With a reference band it measures that band for 1 s; without one, the whole band for 0.1 s. |
| **Max** | Measures the SDR# filter passband for 1 s and sets φ and g so that the tuners add in phase there (maximal ratio combining); with two equal antennas it gains up to about 3 dB of signal to noise ratio. |
| **Reference (Null)** | The band Null measures, in kHz, or `Whole band`. It is drawn on the SDR# spectrum as a turquoise band. |
| **Mark** | Takes the current SDR# filter passband as the reference band. |
| **Clear** | Removes the reference band: Null measures the whole band again. |
| **Auto after retune** | Repeats the last Null or Max after every frequency change. |
| Status | `Not streaming`, `Mode is Single Tuner`, `Measuring...` or `Running`. |
| Last Auto | The last Null or Max: the band it measured (`ref` = reference band, `filter`, `whole`; `whole*` = the band was not usable) and the coherence `c` of the tuners in it. |

The wheel and the slider follow the result of Null and Max, so you can fine-tune from it by hand. The values
are saved with the other settings.

**Working with Null**

1. Set the same gain on both tuners and check that **Saturated Samples** (RSPduo window) stays at 0.
2. Tune where the interference is clearly the strongest signal, set **Output** to **Combined** and click
   **Null**. The level of the interference in SDR# drops; fine-tune with the wheel and the slider.
3. Retune: after every change of frequency the phase between the tuners is random again. Click **Null** again,
   or tick **Auto after retune**.

Null cancels whatever is strongest in the band. If the wanted station is stronger than the interference, Null
cancels the station: mark the interference first (below), use **Max**, or set the wheel by hand.

**Null on a marked signal** (a neighbouring station, a spur next to the wanted signal)

1. Move the SDR# filter onto the unwanted signal, where the wanted one is not, and click **Mark**. The band
   appears on the spectrum in turquoise.
2. Move the filter back onto the wanted station and click **Null**. After about 1 s the marked signal drops;
   **Last Auto** shows `Null, ref` and the coherence in that band.

The weight is one for the whole stream, so the marked signal drops wherever it is, also where it overlaps the
wanted station inside the filter: this is what the filter alone cannot do. The wanted station stays only if the
antennas receive it differently from the marked signal.

**Limits**

- Two antennas cancel one source at a time. The depth of the null cannot exceed **Null Depth Limit**.
- The antennas must receive the interference differently from the wanted signal. Two identical antennas side
  by side receive almost the same signals; a different polarisation (for example one antenna vertical, one
  horizontal) or spacing gives something to work with.
- A reference band or a filter passband wider than 25 kHz, or outside the stream, is not used: the measurement
  falls back to the whole band (**Last Auto** says so).

## 7. Working tips

- **Overload.** If the ADC overload indicator shows, increase the LNA state (more attenuation) before anything
  else; strong broadcast stations are the usual cause. The notch filters help near the MW/FM or DAB bands.
- **Narrow band next to strong stations.** To watch up to about 200–300 kHz, for example an amateur band
  segment next to broadcast stations, use **0.5 MS/s Low IF** with the 200 or 300 kHz filter. The IF filter
  attenuates the stations outside it before the ADC, so the gain can stay higher without overload; the SDR#
  decimation narrows only the view and does not protect the ADC. Measured on the FM band: with the 1.536 MHz
  filter the ADC overloaded at an LNA reduction of 18 dB, with the 300 kHz filter not even at LNA state 0.
- **Low frequencies.** Use **Tuner 1 Hi-Z** below 60 MHz for long-wire and loop antennas; use the 50 Ω ports
  for ordinary antennas.
- **Switching antennas.** The change is applied immediately; gain settings are re-sent automatically.
- **Wide band.** 10 MS/s gives the widest view; reduce the sample rate if the computer cannot keep up
  (watch the **Overruns** counter).
- **Frequency accuracy.** Measure a known carrier and enter the offset in **Frequency Correction (ppm)**.
- After reconnecting, SDR# returns the VFO to the centre frequency; retune after the reconnection.

## 8. Troubleshooting

| Symptom | Cause and remedy |
|---|---|
| SDR# reports "couldn't connect" | **Enabled** is not ticked in the plugin window, or the port in the source address differs from **TCP Port**. |
| Message `Tick "Use full IQ" in the source` | SDR# cleared the option on reconnection. Tick **Use full IQ** in the source settings. |
| "RSPduo error (see Status)" | Open the RSPduo window and read **Device status**. Usually the RSPduo is used by another program, is not connected, or the SDRplay API or its service is missing. |
| SDR# shows "format … not supported" | A stream format other than **PCM 16** is selected. Choose **PCM 16**. |
| Second connection is refused | Only one client at a time is supported. Disconnect the first client. |
| Stream stutters, **Overruns** grows | The computer cannot process the data rate. Lower the sample rate or close other programs. |
| VFO jumps to the centre frequency after reconnecting | Expected; see [Limitations](#9-limitations). |
| Diversity window says `Mode is Single Tuner` | The source runs in Single Tuner mode. Choose **Mode** = **Diversity**, then stop and start the SDR# source. |
| After changing **Mode** the plugin says `Reconnect the source for the new mode` | The mode applies at the next connection. Click stop, then play in SDR#. |
| In Diversity **Unpaired Packets** grows or **Saturated Samples** is not 0 | The packets of the two tuners did not arrive together, or the sum clips. Lower the gain of the tuners or of Tuner 2 (Gain slider) and close other programs. |
| The null is shallow, **Null Depth Limit** is low | The interference is received differently by the two antennas, so it cannot be cancelled fully. Change the position or polarisation of one antenna. |
| Plugin is not listed | Check the path `Plugins\SDRSharp.SDRplay\SDRSharp.SDRplay.dll` and that SDR# is version 1922 or later (.NET 10). |

## 9. Limitations

- One client at a time.
- **Use full IQ** must be ticked again after each reconnection.
- After a reconnection SDR# resets the VFO to the centre frequency (SDR# sends the bridge only its centre
  frequency).
- A new sample rate or a new mode applies at the next connection of the source.
- Diversity: 2 MS/s only, Tuner 2 on its 50 Ω port, AGC off; Sample Rate is fixed.
  Tuner 1 on its 50 Ω or Hi-Z port.
- Diversity: the phase between the tuners is random after every retune; repeat Null / Max or use
  **Auto after retune**.
- Diversity: one weight for the whole stream; two antennas cancel one interference source at a time. A measured
  band is at most 25 kHz wide.
- Diversity: Null on a marked band and Max on the filter are not verified with the SDR# frequency shift
  (upconverter); with the shift on they may measure the wrong place.
- PCM 16 only; no FFT is sent by the bridge.
