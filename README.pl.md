# SDRplay RSPduo dla SDR#

[English](README.md) | **Polski**

Wtyczka SDR#, dzięki której SDR# odbiera sygnał z SDRplay RSPduo przez SDRplay API 3.

SDR# 1922 i nowszy (wersja na .NET 10) ładuje wtyczki przetwarzające, ale nie ładuje zewnętrznych źródeł IQ.
Wtyczka wypełnia tę lukę: uruchamia wewnątrz SDR# lokalny serwer zgodny z SpyServer, steruje RSPduo przez
oficjalne SDRplay API i przekazuje próbki do wbudowanego źródła SDR# **AIRSPY Server Network**.

- [Funkcje](#funkcje)
- [Jak to działa](#jak-to-działa)
- [Wymagania](#wymagania)
- [Instalacja](#instalacja)
- [Szybki start](#szybki-start)
- [Znane ograniczenia](#znane-ograniczenia)
- [Budowanie ze źródeł](#budowanie-ze-źródeł)
- [Podziękowania](#podziękowania)
- [Licencja](#licencja)

## Funkcje

- Szybkości próbkowania 2, 6, 8 i 10 MS/s z odpowiednimi filtrami IF oraz Low IF: 0,5 MS/s (filtr IF 200 lub 300 kHz, przeciw przesterowaniu od pobliskich silnych stacji) albo 2 MS/s.
- Pełne sterowanie wzmocnieniem RSPduo z okna wtyczki: stan LNA (wartości dB z tabel SDRplay dla danego
  pasma i portu), AGC (Off / Fast / Medium / Slow), punkt pracy AGC i ręczne tłumienie IF.
- Wybór anteny w trakcie odbioru: Tuner 1 50 Ω, Tuner 1 Hi-Z, Tuner 2 50 Ω.
- Wskaźnik przesterowania ADC: czerwony znacznik **OVL** na widmie SDR#.
- **Diversity** (tryb Dual Tuner RSPduo): oba tunery odbierają jednocześnie, po 2 MS/s, a SDR# dostaje jeden
  strumień: Tuner 1, Tuner 2 albo sumę Tuner 1 + w · Tuner 2. Wagę w ustawia się kołem fazowym i suwakiem
  wzmocnienia albo automatycznie: **Null** wygasza najsilniejsze zakłócenie albo sygnał zaznaczony filtrem
  SDR# (np. sąsiednią stację), **Max** dodaje tunery w fazie.
  Wzmocnienie ustawia się osobno dla każdego tunera. Zobacz [instrukcję](docs/INSTRUKCJA.md#6-diversity).
- Bias-T (Tuner 2), filtry notch MW/FM, DAB i AM, korekcja częstotliwości w ppm.
- Trzy dokowane okna: stanu odbiornika, klienta SpyServer i Diversity.
- Jasny i ciemny motyw zgodny z SDR#.
- Wszystkie ustawienia zapisywane w `SDRSharp.config`.
- Tylko lokalnie: most nasłuchuje na `127.0.0.1`.

## Jak to działa

```
RSPduo --(SDRplay API 3)--> wtyczka (most SpyServer, 127.0.0.1:5555) --> źródło SDR# „AIRSPY Server Network”
```

Odbiornikiem steruje się z okna wtyczki, tak jak zaprojektował to producent; suwak wzmocnienia SDR# jest
ukryty. Częstotliwość środkową i pasmo (decymację) ustawia sam SDR#.

## Wymagania

- Windows x64.
- SDR# 1922 lub nowszy (wersja na .NET 10).
- Zainstalowane SDRplay API 3.15, z uruchomioną usługą SDRplay.
- RSPduo, który nie jest otwarty w innym programie.

## Instalacja

Do korzystania z wtyczki nie potrzeba kodu źródłowego. Pobierz gotowe archiwum
`SDRplay-RSPduo-plugin-vX.Y.Z.zip` ze strony [Releases](../../releases), a następnie:

1. Zamknij SDR#.
2. Rozpakuj folder `SDRSharp.SDRplay` z archiwum do folderu `Plugins` w SDR#, tak aby powstała ścieżka
   `Plugins\SDRSharp.SDRplay\SDRSharp.SDRplay.dll`.
3. Uruchom SDR#. Wtyczka pojawi się na liście jako **SDRplay RSPduo**.

## Szybki start

1. W oknie **SDRplay RSPduo** zaznacz **Enabled**. Etykieta stanu pokaże `Listening`.
2. W SDR# wybierz źródło **AIRSPY Server Network** i wpisz `sdr://127.0.0.1:5555/`.
3. W ustawieniach źródła wybierz format strumienia **PCM 16** i zaznacz **Use full IQ**.
4. Kliknij Play. Etykieta stanu pokaże `Streaming`.

SDR# czyści **Use full IQ** przy każdym ponownym połączeniu; zaznacz je za każdym razem od nowa.

Pełną instrukcję znajdziesz [tutaj](docs/INSTRUKCJA.md). Zmiany między wersjami opisuje [dziennik zmian](CHANGELOG.pl.md).

## Znane ograniczenia

- Jeden klient naraz; drugi klient jest odrzucany, dopóki pierwszy jest połączony.
- **Use full IQ** trzeba zaznaczyć ponownie po każdym połączeniu (SDR# je czyści).
- Po ponownym połączeniu SDR# ustawia VFO z powrotem na częstotliwości środkowej: SDR# wysyła do mostu
  tylko częstotliwość środkową, przesunięcie VFO zostaje wewnątrz SDR#.
- Nowa szybkość próbkowania albo nowy tryb (Single Tuner / Diversity) działa od następnego połączenia źródła
  SDR# (Stop, potem Play).
- Diversity pracuje przy 2 MS/s z Tunerem 2 na porcie 50 Ω (Tuner 1 na 50 Ω albo Hi-Z); szybkość próbkowania jest w nim stała, a AGC
  wyłączone.
- Faza między tunerami jest losowa po każdym przestrojeniu: powtórz Null / Max (albo zaznacz **Auto after retune**).
- Dwie anteny wygaszają jedno źródło zakłóceń naraz; głębokość ogranicza jego koherencja między antenami.
- Null na zaznaczonym paśmie i Max nie są sprawdzone z przesunięciem częstotliwości SDR# (upkonwerter).
- Most wysyła wyłącznie 16-bitowe IQ (PCM 16) i nie wysyła FFT.

## Budowanie ze źródeł

Wtyczka powstała z użyciem pakietu SDR# Plugin Development Kit (VDK) wydanego przez Airspy. Repozytorium nie
zawiera ani VDK, ani samego SDR#, więc przed budowaniem trzeba je umieścić obok źródeł.

**Wymagania**

- Windows x64 i [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
  (sprawdź poleceniem `dotnet --version`).
- Git.
- VDK (`vdk.zip`) oraz SDR# 1922 lub nowszy (wersja x64 na .NET 10), oba ze
  [strony pobierania Airspy](https://airspy.com/download/).

**Układ folderów**

Plik projektu odwołuje się do bibliotek VDK ścieżkami względnymi (`..\lib\ref\*.dll` oraz
`..\lib\SDRSharp.PanView.dll`), więc foldery muszą być ułożone tak:

```
<katalog-roboczy>\
  lib\                      <- z VDK (zawiera ref\ i SDRSharp.PanView.dll)
  sdrsharp-sdrplay\         <- to repozytorium
    sdrsharp-x64\           <- kopia SDR# (zawiera SDRSharp.exe)
```

**Kroki**

1. Rozpakuj `vdk.zip` i umieść jego folder `lib` w `<katalog-roboczy>`.
2. Sklonuj repozytorium do `<katalog-roboczy>`:

   ```powershell
   git clone https://github.com/sq5gn/sdrsharp-sdrplay.git
   ```
3. Rozpakuj SDR# do `sdrsharp-sdrplay\sdrsharp-x64\`, tak aby istniał plik `sdrsharp-x64\SDRSharp.exe`.
   Folder jest ignorowany przez git. Gdy brakuje tam `SDRSharp.exe`, kompilacja kończy się błędem.
4. Zamknij SDR#, jeśli działa (blokuje pliki wtyczki), i zbuduj projekt:

   ```powershell
   cd sdrsharp-sdrplay
   dotnet build -c Release
   ```

5. Skompilowana wtyczka powstaje w `bin\Release\net10.0-windows\`. Krok po kompilacji kopiuje
   `SDRSharp.SDRplay.dll` i `SDRSharp.SDRplay.deps.json` do `sdrsharp-x64\Plugins\SDRSharp.SDRplay\`.
6. Uruchom `sdrsharp-x64\SDRSharp.exe`; wtyczka pojawi się jako **SDRplay RSPduo**.

SDRplay API jest ładowane w czasie działania z folderu zapisanego przez jego instalator
(`HKLM\SOFTWARE\SDRplay\Service\API`, `Install_Dir`). W repozytorium nie ma żadnego nagłówka SDRplay ani
biblioteki SDR#.

## Podziękowania

Ogromne podziękowania dla Youssefa Touila (prog) z Airspy za opublikowanie VDK, co umożliwiło powstanie tej
wtyczki.

Dziękuję też autorom [SDR++](https://github.com/AlexandreRouma/SDRPlusPlus) (Alexandre Rouma), [wtyczki F6FLT
do SDR# dla SDRplay](https://github.com/F6FLT/SDRSharp_v17xx-Plugin-for-SDRPlay) oraz
[SoapySDRPlay3](https://github.com/pothosware/SoapySDRPlay3). Czytałem ich otwarte projekty, żeby zrozumieć,
jak używa się SDRplay API, jak zbudowane jest źródło SDR# i jak obsłużyć porty antenowe RSPduo. Żaden kod
nie został z nich skopiowany.

Dziękuję SDRplay za API i jego dokumentację.

## Licencja

MIT, zobacz [LICENSE](LICENSE). SDR# i SDRplay API to osobne produkty z własnymi licencjami.
SDRplay i RSPduo są znakami towarowymi SDRplay Limited; ten projekt jest niezależny i nie jest powiązany
z SDRplay Limited ani Airspy, ani przez nie popierany.
