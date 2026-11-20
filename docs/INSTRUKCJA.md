# SDRplay RSPduo dla SDR# — instrukcja użytkownika

[English](USER-GUIDE.md) | **Polski** | [Powrót do README](../README.pl.md)

- [1. Przegląd](#1-przegląd)
- [2. Instalacja](#2-instalacja)
- [3. Połączenie SDR# z odbiornikiem](#3-połączenie-sdr-z-odbiornikiem)
- [4. Okno wtyczki — opis ustawień](#4-okno-wtyczki--opis-ustawień)
- [5. Okna stanu](#5-okna-stanu)
- [6. Diversity](#6-diversity)
- [7. Wskazówki praktyczne](#7-wskazówki-praktyczne)
- [8. Rozwiązywanie problemów](#8-rozwiązywanie-problemów)
- [9. Ograniczenia](#9-ograniczenia)

## 1. Przegląd

SDR# 1922 i nowszy ładuje wtyczki przetwarzające, ale nie ładuje zewnętrznych źródeł IQ. Wtyczka uruchamia
wewnątrz SDR# mały serwer zgodny z SpyServer (na `127.0.0.1`, domyślnie port 5555), otwiera RSPduo przez
SDRplay API 3 i przekazuje próbki do wbudowanego źródła SDR# **AIRSPY Server Network**.

```
RSPduo --(SDRplay API 3)--> wtyczka (most SpyServer, 127.0.0.1:5555) --> źródło SDR# „AIRSPY Server Network”
```

Podział zadań:

| Ustawiane w oknie wtyczki | Ustawiane w SDR# |
|---|---|
| Tryb (Single Tuner / Diversity), wzmocnienie (LNA, AGC, IF), szybkość próbkowania, antena, Bias-T, filtry notch, ppm | Częstotliwość środkowa, pasmo (decymacja), demodulacja |

Suwak wzmocnienia SDR# jest ukryty, bo wzmocnieniem zarządza okno wtyczki.

## 2. Instalacja

Wymagania:

- Windows x64.
- SDR# 1922 lub nowszy (wersja na .NET 10).
- Zainstalowane SDRplay API 3.15; usługa SDRplay musi działać.
- RSPduo nieużywany przez inny program.

Kroki:

1. Pobierz `SDRplay-RSPduo-plugin-vX.Y.Z.zip` ze strony Releases repozytorium.
2. Zamknij SDR#.
3. Rozpakuj folder `SDRSharp.SDRplay` z archiwum do folderu `Plugins` w SDR#. Zawiera on
   `SDRSharp.SDRplay.dll` i `SDRSharp.SDRplay.deps.json`.
4. Uruchom SDR#. **SDRplay RSPduo** pojawi się na liście wtyczek i jako okno.

SDRplay API jest znajdowane automatycznie przez wpis w rejestrze zapisany przez jego instalator.

## 3. Połączenie SDR# z odbiornikiem

1. W oknie **SDRplay RSPduo** zaznacz **Enabled**. Etykieta stanu pokaże `Listening`.
2. W SDR# otwórz listę źródeł i wybierz **AIRSPY Server Network**.
3. Wpisz adres `sdr://127.0.0.1:5555/` (port to **TCP Port** wtyczki).
4. W ustawieniach źródła wybierz **PCM 16** i zaznacz **Use full IQ**.
5. Kliknij Play. Etykieta stanu pokaże `Streaming`.

Uwagi:

- SDR# czyści **Use full IQ** przy każdym ponownym połączeniu. Okno wtyczki pokazuje wtedy komunikat
  `Tick "Use full IQ" in the source`.
- Nowa wartość **Sample Rate** działa przy następnym połączeniu źródła SDR# (Stop, potem Play).
- Węższe pasma wybiera się w SDR# (decymacja od 2 do 32); nie wymaga to zmian we wtyczce.
- Wszystkie ustawienia wtyczki są zapisywane w `SDRSharp.config` (klucze `plugin.sdrplay.*`) przy zamykaniu SDR#.

## 4. Okno wtyczki — opis ustawień

| Ustawienie | Opis |
|---|---|
| **Enabled** | Włącza i wyłącza most. Zatrzymanie odbiornika trwa około 1,5 s. |
| **TCP Port** | Port mostu na `127.0.0.1` (domyślnie 5555). Musi zgadzać się z portem w adresie źródła SDR#. |
| **Mode** | **Single Tuner** (domyślnie) albo **Diversity** (oba tunery, zobacz [rozdział 6](#6-diversity)). Działa od następnego połączenia źródła SDR#. |
| **LNA State** | Stopień tłumienia we wzmacniaczu RF. Wartość w dB pochodzi z tabeli SDRplay dla ustawionego pasma i wybranego portu; przy zmianie pasma lub portu jest ograniczana do dozwolonego zakresu. |
| **AGC** | **Off**, **Fast**, **Medium** lub **Slow**: szerokość pętli AGC 100, 50 lub 5 Hz (więcej oznacza szybciej). Domyślnie **Medium**. |
| **AGC Setpoint (dBFS)** | Docelowy poziom sygnału dla AGC. |
| **IF Gain Reduction** | Ręczne tłumienie IF, 20–59 dB. Działa przy AGC **Off**. |
| **Sample Rate** | 2, 6, 8 lub 10 MS/s, każda z odpowiednim filtrem IF. |
| **Antenna** | **Tuner 1 50 Ω**, **Tuner 1 Hi-Z** (wysoka impedancja, przeznaczona dla częstotliwości poniżej 60 MHz) lub **Tuner 2 50 Ω**. Można zmieniać w trakcie odbioru. |
| **Bias-T** | Zasilanie DC na porcie Tunera 2. Dostępne tylko po wybraniu Tunera 2. Domyślnie wyłączone. |
| **MW/FM Notch** | Filtr zaporowy pasm radiofonicznych MW/FM. |
| **DAB Notch** | Filtr zaporowy pasma DAB. |
| **AM Notch** | Filtr zaporowy AM Tunera 1; niedostępny przy Tunerze 2. |
| **Frequency Correction (ppm)** | Korekcja oscylatora wzorcowego, od −100 do +100 ppm. Wpisaną wartość zatwierdź Enterem lub opuszczając pole. |

**Uwaga — Bias-T** zasila urządzenie podłączone do portu Tunera 2 (np. antenę aktywną lub wzmacniacz).
Włączaj go tylko wtedy, gdy podłączone urządzenie oczekuje napięcia DC; w pozostałych przypadkach zostaw wyłączony.

## 5. Okna stanu

Trzy przyciski z ikonami na dole okna wtyczki pokazują i ukrywają trzy okna dokowane w SDR#. Okno
**Diversity** (koło ze wskazówką) opisuje [rozdział 6](#6-diversity).

**RSPduo** (ikona anteny)

| Wiersz | Znaczenie |
|---|---|
| Device | Model, numer seryjny i wybrana antena. |
| Mode | Tryb działającego połączenia: Single Tuner albo Diversity. |
| Frequency | Bieżąca częstotliwość środkowa. |
| Sample rate | Ustawiona szybkość próbkowania. |
| Decimation | Decymacja żądana przez SDR#. |
| Gain reduction | Całkowite tłumienie w dB (LNA plus IF). |
| System gain | Wzmocnienie systemowe raportowane przez odbiornik. |
| Signal level | Poziom każdego tunera w dBFS (tylko w Diversity). |
| ADC overload | Ostrzeżenie o przesterowaniu przetwornika ADC. Zwiększ stan LNA lub tłumienie IF. |
| Measured rate | Rzeczywista szybkość próbkowania dostarczana przez odbiornik. |
| Overruns | Liczba pominiętych bloków próbek, gdy klient nie nadążał. |
| Device status | Stan urządzenia; pełne komunikaty błędów API pojawiają się tutaj. |

**SpyServer Client** (ikona monitora)

| Wiersz | Znaczenie |
|---|---|
| Client | Nazwa podłączonego klienta. |
| Protocol | Wersja protokołu klienta. |
| IQ format | Format strumienia (PCM 16). |
| Mode | Tryb strumieniowania żądany przez klienta. |
| Streaming | Czy próbki są aktualnie wysyłane. |
| Digital gain | Wzmocnienie cyfrowe żądane przez klienta. |
| Messages sent | Liczba komunikatów wysłanych do klienta. |
| Last command | Ostatnie polecenie otrzymane od klienta. |

W Diversity wiersze tłumienia, wzmocnienia systemowego, poziomu i przesterowania pokazują oba tunery jako
`T1 … | T2 …`. Wszystkie trzy okna mają przewijanie, gdy dane nie mieszczą się w oknie.

## 6. Diversity

Diversity odbiera jednocześnie oboma tunerami RSPduo. Podłącz antenę do każdego portu tunera; wtyczka łączy
oba sygnały w jeden strumień dla SDR#. Służy do osłabiania lokalnych zakłóceń (EMI, QRM), które dwie anteny
odbierają inaczej niż sygnał pożądany.

**Włączanie**

1. W oknie wtyczki ustaw **Mode** na **Diversity**.
2. Zatrzymaj i uruchom źródło SDR# (tryb działa od następnego połączenia; do tego czasu etykieta stanu
   pokazuje `Reconnect the source for the new mode`).
3. Pokaż okno **Diversity** (trzeci przycisk z ikoną) i ustaw **Output** na **Combined**.

W Diversity okno wtyczki się zmienia:

- Szybkość próbkowania jest stała, 2 MS/s na tuner, i używane są tylko porty 50 Ω; **Sample Rate** oraz
  **Antenna** są nieaktywne.
- AGC jest wyłączone. **LNA State** i **IF Gain Reduction** są osobno dla każdego tunera (Tuner 1, Tuner 2);
  przycisk **Tuner 2 gain = Tuner 1 gain** kopiuje ustawienia Tunera 1 do Tunera 2.
- Bias-T działa na Tunerze 2, a filtr AM Notch na Tunerze 1, tak jak w trybie Single Tuner.
- Powrót: ustaw **Mode** na **Single Tuner** i zatrzymaj oraz uruchom źródło; szybkość próbkowania wybrana
  w oknie wtyczki wraca.

**Okno Diversity**

Sygnał połączony to `Tuner 1 + w · Tuner 2`, gdzie waga w ma fazę φ i wzmocnienie g.

| Element | Opis |
|---|---|
| **Output** | Co trafia do SDR#: **Tuner 1**, **Tuner 2** albo **Combined**. |
| **Phase (Tuner 2)** | Faza φ Tunera 2 w sumie. Przeciągnij wskazówkę koła albo kręć kółkiem myszy nad kołem (1°, z Shiftem 10°). 0° jest u góry, zgodnie z ruchem wskazówek zegara. |
| **Gain (Tuner 2)** | Poziom g Tunera 2 względem Tunera 1 w sumie, od −30 do +30 dB co 0,1 dB. |
| **Null** | Mierzy przez 0,1 s i ustawia φ oraz g tak, by moc na wyjściu była minimalna: wygasza najsilniejszy sygnał w paśmie. |
| **Max** | Mierzy przez 0,1 s i ustawia φ oraz g tak, by tunery dodawały się w fazie (łączenie z maksymalnym stosunkiem); przy dwóch jednakowych antenach daje do około 3 dB w stosunku sygnału do szumu. |
| **Auto after retune** | Powtarza ostatni Null lub Max po każdej zmianie częstotliwości. |
| Status | `Not streaming`, `Mode is Single Tuner`, `Measuring...` albo `Running`. |
| Level Tuner 1 / Tuner 2 / Combined | Poziomy sygnałów w dBFS. |
| Coherence | Korelacja obu tunerów, od 0 do 1. |
| Null Depth Limit | Najgłębsze zerowanie, na jakie pozwala bieżąca koherencja, 10·log10(1 − c²). |
| Saturated Samples | Próbki sumy obcięte (przesterowane), na 100 ms. |
| Unpaired Packets | Pakiety jednego tunera, które dotarły bez pakietu drugiego. |

Koło i suwak podążają za wynikiem Null i Max, więc można od niego dostroić ręcznie. Wartości są zapisywane
razem z pozostałymi ustawieniami.

**Praca z Null**

1. Ustaw takie samo wzmocnienie na obu tunerach i sprawdź, czy **Saturated Samples** zostaje na 0.
2. Dostrój do miejsca, w którym zakłócenie jest wyraźnie najsilniejszym sygnałem, ustaw **Output** na
   **Combined** i kliknij **Null**. Poziom zakłócenia w SDR# spada; dostrój kołem i suwakiem.
3. Przestrojenie: po każdej zmianie częstotliwości faza między tunerami jest znowu losowa. Kliknij **Null**
   ponownie albo zaznacz **Auto after retune**.

Null wygasza to, co jest najsilniejsze w paśmie. Jeśli stacja pożądana jest silniejsza niż zakłócenie, Null
wygasi stację: użyj wtedy **Max** albo ustaw koło ręcznie.

**Ograniczenia**

- Dwie anteny wygaszają jedno źródło naraz. Głębokość zerowania nie przekroczy **Null Depth Limit**.
- Anteny muszą odbierać zakłócenie inaczej niż sygnał pożądany. Dwie jednakowe anteny tuż obok siebie
  odbierają prawie to samo; inna polaryzacja (np. jedna antena pionowo, druga poziomo) albo odstęp dają
  materiał do pracy.
- Null i Max używają całego pasma 2 MHz; wybór pasma odniesienia jest planowany (issue #36).

## 7. Wskazówki praktyczne

- **Przesterowanie.** Gdy pojawi się wskaźnik przesterowania ADC, najpierw zwiększ stan LNA (większe
  tłumienie); zwykle winne są silne stacje radiofoniczne. Przy pasmach MW/FM i DAB pomagają filtry notch.
- **Niskie częstotliwości.** Poniżej 60 MHz używaj **Tuner 1 Hi-Z** dla anten długoprzewodowych i pętlowych;
  dla zwykłych anten używaj portów 50 Ω.
- **Przełączanie anten.** Zmiana działa natychmiast; ustawienia wzmocnienia są wysyłane ponownie automatycznie.
- **Szerokie pasmo.** 10 MS/s daje najszerszy podgląd; gdy komputer nie nadąża, zmniejsz szybkość próbkowania
  (obserwuj licznik **Overruns**).
- **Dokładność częstotliwości.** Zmierz znaną nośną i wpisz odchyłkę w **Frequency Correction (ppm)**.
- Po ponownym połączeniu SDR# ustawia VFO na częstotliwość środkową; dostrój ponownie.

## 8. Rozwiązywanie problemów

| Objaw | Przyczyna i rozwiązanie |
|---|---|
| SDR# zgłasza „couldn't connect” | W oknie wtyczki nie zaznaczono **Enabled** albo port w adresie źródła różni się od **TCP Port**. |
| Komunikat `Tick "Use full IQ" in the source` | SDR# wyczyścił opcję przy ponownym połączeniu. Zaznacz **Use full IQ** w ustawieniach źródła. |
| „RSPduo error (see Status)” | Otwórz okno RSPduo i przeczytaj **Device status**. Zwykle RSPduo jest zajęty przez inny program, odłączony albo brakuje SDRplay API lub jego usługi. |
| SDR# pokazuje „format … not supported” | Wybrano format strumienia inny niż **PCM 16**. Wybierz **PCM 16**. |
| Drugie połączenie jest odrzucane | Obsługiwany jest jeden klient naraz. Rozłącz pierwszego klienta. |
| Dźwięk lub widmo „szarpie”, rośnie **Overruns** | Komputer nie nadąża z przepływem danych. Zmniejsz szybkość próbkowania lub zamknij inne programy. |
| VFO wraca na częstotliwość środkową po ponownym połączeniu | Zachowanie oczekiwane; zobacz [Ograniczenia](#9-ograniczenia). |
| Okno Diversity pokazuje `Mode is Single Tuner` | Źródło pracuje w trybie Single Tuner. Wybierz **Mode** = **Diversity**, potem zatrzymaj i uruchom źródło SDR#. |
| Po zmianie **Mode** wtyczka pokazuje `Reconnect the source for the new mode` | Tryb działa od następnego połączenia. W SDR# kliknij Stop, potem Play. |
| W Diversity rośnie **Unpaired Packets** albo **Saturated Samples** nie jest równe 0 | Pakiety obu tunerów nie dotarły razem albo suma jest obcinana. Zmniejsz wzmocnienie tunerów lub Tunera 2 (suwak Gain) i zamknij inne programy. |
| Zerowanie jest płytkie, **Null Depth Limit** jest niski | Zakłócenie jest odbierane przez obie anteny inaczej, więc nie da się go wygasić całkowicie. Zmień położenie albo polaryzację jednej anteny. |
| Wtyczki nie ma na liście | Sprawdź ścieżkę `Plugins\SDRSharp.SDRplay\SDRSharp.SDRplay.dll` i wersję SDR# (1922 lub nowsza, .NET 10). |

## 9. Ograniczenia

- Jeden klient naraz.
- **Use full IQ** trzeba zaznaczyć ponownie po każdym połączeniu.
- Po ponownym połączeniu SDR# ustawia VFO na częstotliwość środkową (SDR# wysyła do mostu tylko
  częstotliwość środkową).
- Nowa szybkość próbkowania albo nowy tryb działa od następnego połączenia źródła.
- Diversity: tylko 2 MS/s, tylko porty 50 Ω (bez Hi-Z), AGC wyłączone; Sample Rate i Antenna są stałe.
- Diversity: faza między tunerami jest losowa po każdym przestrojeniu; powtórz Null / Max albo użyj
  **Auto after retune**.
- Diversity: Null i Max używają całego pasma 2 MHz. Dwie anteny wygaszają jedno źródło zakłóceń naraz.
- Tylko PCM 16; most nie wysyła FFT.
