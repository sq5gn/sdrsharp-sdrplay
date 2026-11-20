# Dziennik zmian

[English](CHANGELOG.md) | **Polski**

Najważniejsze zmiany we wtyczce. Format wg [Keep a Changelog](https://keepachangelog.com/pl/1.1.0/),
wersje wg [Semantic Versioning](https://semver.org/lang/pl/).

- [1.1.0 - 2026-11-20](#110---2026-11-20)
- [1.0.0 - 2026-11-20](#100---2026-11-20)

## 1.1.0 - 2026-11-20

Odbiór diversity oboma tunerami RSPduo (issue #36).

### Dodano

- Ustawienie **Mode**: **Single Tuner** albo **Diversity** (tryb Dual Tuner RSPduo, 2 MS/s na tuner,
  porty 50 Ω). Tryb działa od następnego połączenia źródła SDR#.
- Wzmocnienie osobno dla każdego tunera w Diversity: **LNA State** i **IF Gain Reduction** dla Tunera 1
  i Tunera 2 oraz przycisk **Tuner 2 gain = Tuner 1 gain**. W tym trybie AGC jest wyłączone.
- Okno **Diversity** (trzecie okno dokowane): wyjście **Tuner 1** / **Tuner 2** / **Combined**
  (Tuner 1 + w · Tuner 2), koło fazowe, suwak wzmocnienia, **Null** (wygasza najsilniejszy sygnał w paśmie),
  **Max** (dodaje tunery w fazie), **Auto after retune** oraz pomiary: poziomy, koherencja, limit głębokości
  zerowania, próbki obcięte, pakiety niesparowane.
- Okno **RSPduo** pokazuje tryb, a w Diversity wartości obu tunerów (tłumienie, wzmocnienie systemowe, poziom
  sygnału, przesterowanie).
- Nowe ustawienia w `SDRSharp.config`: tryb, wyjście, wzmocnienie Tunera 2, Auto after retune, faza
  i wzmocnienie wagi.
- Instrukcja i README po angielsku i po polsku opisują Diversity i jego ograniczenia.

### Zmieniono

- Okno wtyczki ma trzeci przycisk z ikoną, dla okna Diversity.
- Znane ograniczenia: wiersz „tylko tryb jednego tunera” zastąpiły ograniczenia Diversity (2 MS/s, porty
  50 Ω, AGC wyłączone, losowa faza po przestrojeniu, jedno wygaszane źródło naraz).
- README: polecenie `git clone` ma konkretny adres repozytorium.

### Znane granice

- Null i Max używają całego pasma 2 MHz. Pasmo odniesienia (bez sygnału pożądanego) dla estymaty jest
  planowane.

## 1.0.0 - 2026-11-20

Pierwsze wydanie: RSPduo w SDR# przez SDRplay API 3.15, jako most SpyServer na `127.0.0.1:5555` dla źródła
SDR# **AIRSPY Server Network**.

### Dodano

- Szybkości próbkowania 2, 6, 8 i 10 MS/s z odpowiednimi filtrami IF; węższe pasma przez decymację w SDR#.
- Sterowanie wzmocnieniem w oknie wtyczki: stan LNA (dB z tabel SDRplay dla pasma i portu), AGC
  (Off / Fast / Medium / Slow), punkt pracy AGC, ręczne tłumienie IF.
- Wybór anteny w trakcie odbioru: Tuner 1 50 Ω, Tuner 1 Hi-Z, Tuner 2 50 Ω.
- Bias-T (Tuner 2), filtry notch MW/FM, DAB i AM, korekcja częstotliwości w ppm.
- Dwa dokowane okna stanu (odbiornik, klient SpyServer); jasny i ciemny motyw zgodny z SDR#.
- Ustawienia zapisywane w `SDRSharp.config`.
- Instrukcja i README po angielsku i po polsku; licencja MIT.
