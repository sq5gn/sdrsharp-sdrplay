# Changelog

**English** | [Polski](CHANGELOG.pl.md)

All notable changes of the plugin. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
the versions follow [Semantic Versioning](https://semver.org/).

- [1.2.0 - 2026-11-21](#120---2026-11-21)
- [1.1.0 - 2026-11-20](#110---2026-11-20)
- [1.0.0 - 2026-11-20](#100---2026-11-20)

## 1.2.0 - 2026-11-21

Diversity: Null on a signal marked with the SDR# filter (issue #36).

### Added

- Diversity: **Mark** takes the SDR# filter passband as the reference band and **Null** cancels the signal in
  it (for example a neighbouring station or a spur), measured for 1 s; **Clear** goes back to the whole band.
  The reference band is drawn on the SDR# spectrum and stored in `SDRSharp.config`.
- Diversity window: the reference band and **Last Auto** (the band measured and the coherence in it).

### Changed

- **Max** measures the SDR# filter passband (1 s) instead of the whole band.
- The Diversity measurements (combined level, coherence, null depth limit, saturated samples, unpaired
  packets) moved to the **RSPduo** window; the phase wheel follows the size of the Diversity window.

### Known issues

- Null on a marked band and Max are not verified with the SDR# frequency shift (upconverter).

## 1.1.0 - 2026-11-20

Diversity reception with both RSPduo tuners (issue #36).

### Added

- **Mode** setting: **Single Tuner** or **Diversity** (RSPduo Dual Tuner mode, 2 MS/s per tuner, 50 Ω ports).
  The mode applies at the next connection of the SDR# source.
- Gain for each tuner in Diversity: **LNA State** and **IF Gain Reduction** for Tuner 1 and Tuner 2, and the
  button **Tuner 2 gain = Tuner 1 gain**. The AGC is off in this mode.
- **Diversity** window (third dockable window): output **Tuner 1** / **Tuner 2** / **Combined**
  (Tuner 1 + w · Tuner 2), phase wheel, gain slider, **Null** (cancels the strongest signal in the band),
  **Max** (adds the tuners in phase), **Auto after retune**, and the measurements: levels, coherence, null
  depth limit, saturated samples, unpaired packets.
- The **RSPduo** window shows the mode and, in Diversity, the values of both tuners (gain reduction, system
  gain, signal level, overload).
- New settings in `SDRSharp.config`: mode, output, gain of Tuner 2, Auto after retune, phase and gain of the
  weight.
- User guide and README in English and Polish describe Diversity and its limits.

### Changed

- The plugin window has a third icon button, for the Diversity window.
- Known limitations: the line "single tuner mode only" is replaced by the limits of Diversity (2 MS/s, 50 Ω
  ports, AGC off, random phase after a retune, one cancelled source at a time).
- README: the `git clone` command has the concrete repository address.

### Known limits

- Null and Max use the whole 2 MHz band. A reference band (without the wanted signal) for the estimate is
  planned.

## 1.0.0 - 2026-11-20

First release: the RSPduo in SDR# through the SDRplay API 3.15, as a SpyServer bridge on `127.0.0.1:5555`
for the SDR# source **AIRSPY Server Network**.

### Added

- Sample rates 2, 6, 8 and 10 MS/s with the matching IF filters; narrower bandwidths by SDR# decimation.
- Gain control in the plugin window: LNA state (dB from the SDRplay tables per band and port), AGC
  (Off / Fast / Medium / Slow), AGC set-point, manual IF gain reduction.
- Antenna selection while receiving: Tuner 1 50 Ω, Tuner 1 Hi-Z, Tuner 2 50 Ω.
- Bias-T (Tuner 2), MW/FM, DAB and AM notch filters, frequency correction in ppm.
- Two dockable status windows (receiver, SpyServer client); light and dark theme following SDR#.
- Settings stored in `SDRSharp.config`.
- User guide and README in English and Polish; MIT licence.
