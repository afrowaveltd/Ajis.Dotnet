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

Poznámka: Lax (Lex) režim v současném StreamWalk používá stejný engine jako AJIS a zatím
nemění validační pravidla.

Poznámka: Pro ne-univerzální profily se lexer-based parsing použije vždy, když jsou
vyžadované AJIS rozšíření (direktivy, komentáře, identifikátory, base prefixy, trailing commas).

---

## 9.2.1 Processing profile hint

Streamovací parser může respektovat **processing profile** z nastavení:

* **Universal** – vyvážený výchozí režim
* **LowMemory** – preferuje nízkou paměťovou stopu
* **HighThroughput** – preferuje vyšší propustnost

Aktuální výběr modulu pro segment parsing:

* **Universal** → lexer-based parsing (span i stream)
* **LowMemory** → memory-mapped stream cesta
* **HighThroughput** → lexer-based parsing pro span, memory-mapped stream cesta

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

## 9.6.1 Aktuální stav implementace (M3)

* `AjisParse.ParseSegments` používá StreamWalk M1 a mapuje události na segmenty.
* `ParseSegmentsAsync` používá zero-copy pro `MemoryStream`, memory‑mapped čtení pro `FileStream`, a pro ostatní streamy používá dočasný soubor s memory‑mapped čtením (bez plného bufferu).
* Prah pro chunked čtení lze nastavit v `AjisSettings.StreamChunkThreshold` (např. `1k`, `512M`, `2G`). Bez suffixu je hodnota v MB.
* Chunked cesta aktuálně používá `Utf8JsonReader`, takže stringy jsou vraceny dekódované (bez původních escape sekvencí).
* Chunked memory‑mapped cesta podporuje i soubory >2GB, ale není zatím pokrytá testovacím fixture.

## 9.6.2 Testování profilů na větších datech

* V testech `AjisParseLargeDataTests` se ověřuje Universal/HighThroughput/LowMemory na generovaném JSON payloadu.
* Testuje se i `StreamChunkThreshold` (suffixy `k/M/g` a default v MB) a chování při neplatné hodnotě.
* Chunked cesta je pokrytá testem s větším payloadem a escape sekvencí (dekódovaný string).

## 9.7 M2 Reader foundation

* Implementován `IAjisReader` se span/stream implementacemi.
* Základní testy parity a refill chování v `AjisReaderTests`.

## 9.8 M2 Lexer foundation

* `AjisLexer` tokenizuje JSON subset nad `IAjisReader`.
* Testy tokenizace v `AjisLexerTests` (escapes, unicode, čísla, stream reader) + pozice.
* `AjisParse` nyní používá lexer-based parser pro span vstupy.
* Validace čísel odpovídá JSON (bez leading plus, povinné číslice po tečce/exponentu).
* Lexer podporuje AJIS prefixy (`0b`, `0o`, `0x`) a volitelné separátory podle `AjisNumberOptions`.
* Validace separátorů respektuje skupiny (decimal/octal 3, binary 4, hex 2/4 bez mixu).
* Stringy respektují `AjisTextMode`: JSON striktní bez newline, AJIS dovoluje multiline, Lex je tolerantní k invalidním escapes.
* AJIS režim může vypnout escapes (`EnableEscapes=false`), což zachová backslash jako literal.
* AJIS může povolit single-quote stringy přes `AllowSingleQuotes` (JSON je stále odmítá).
* Unquoted property names jsou povoleny v AJIS/Lex (`AllowUnquotedPropertyNames`), JSON je odmítá.
* Lex režim vrací neuzavřené stringy/escape sekvence jako best-effort tokeny.
* Komentáře (`//`, `/* */`) jsou v AJIS/Lex ignorovány jako whitespace, JSON je odmítá.
* Trailing commas jsou povoleny při `AllowTrailingCommas=true` nebo v Lex režimu.
* Direktivy jsou řádky začínající `#` na začátku řádku; v AJIS/Lex se přeskočí jako whitespace (binding na další prvek dle pravidel).
* Stream varianta v profilu `Universal` používá lexer-based parser nad `AjisStreamReader`.

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
