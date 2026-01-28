# 09 – Streaming & Pipelines

Tento dokument definuje **streamovací model AJIS** – jak data plynou parserem, serializérem a navazujícími komponentami bez nutnosti načítat celý obsah do paměti.

AJIS je od počátku navržen jako **průchozí (stream-first) formát**.

---

## 9.1 Základní principy

* žádná povinnost držet celý dokument v paměti
* čtení a zápis probíhá **po segmentech**
* každý segment může být:

  * validován
  * transformován
  * emitován dál
* pipeline je **komponovatelná**

AJIS pipeline lze chápat jako:

```
Input → Reader → Parser → (Events) → Consumer / Writer
```

---

## 9.2 Pull vs Push model

### Pull (Reader-driven)

* konzument si vyžádá další token / blok
* vhodné pro:

  * synchronní zpracování
  * jednoduché nástroje

### Push (Producer-driven)

* parser aktivně posílá události
* vhodné pro:

  * dlouhé běhy
  * UI / progress
  * síťový streaming

AJIS **podporuje oba modely**.

---

## 9.3 Segmentace dat

Parser nikdy neposílá „celý dokument“.

Typické segmenty:

* ObjectStart / ObjectEnd
* PropertyName
* Value (primitive / string / number)
* ArrayItem
* BinaryReference (v ATP režimu)

Segment je **atomický a samostatný**.

---

## 9.4 Zero-copy strategie

Kde to prostředí dovolí:

* používá se `ReadOnlySpan<byte>` / `ReadOnlyMemory<byte>`
* stringy se dekódují **až na vyžádání**
* čísla mohou zůstat v textové podobě

Cíl:

* minimum alokací
* minimum GC tlaku

---

## 9.5 File-based pipelines

Typický scénář:

```
File → AjisReader → Filter → Serializer → File
```

Možné operace bez plného načtení:

* vyhledání klíče
* selektivní serializace
* transformace hodnot
* validace struktury

---

## 9.6 Network & IO integrace

AJIS pipeline je navržena pro:

* HttpClient
* HttpServer (ASP.NET)
* streamované uploady / downloady

Parser může pracovat nad:

* `Stream`
* `PipeReader`
* custom transportem (ATP)

---

## 9.7 Progress & Observabilita

Každý pipeline krok **může emitovat eventy**:

* bytes processed
* objects processed
* percent estimate

Pipeline **nikdy nečeká na posluchače**.

---

## 9.8 Chybové chování

* chyba v segmentu neznamená nutně konec streamu
* dle konfigurace:

  * stop
  * skip
  * continue with diagnostics

---

## 9.9 Shrnutí

Streaming & Pipelines jsou základní stavební kámen AJIS:

* umožňují práci s obřími soubory
* oddělují IO, parsing a logiku
* tvoří most k ATP a binární části

AJIS je navržen jako **datový tok, ne strom v paměti**.
