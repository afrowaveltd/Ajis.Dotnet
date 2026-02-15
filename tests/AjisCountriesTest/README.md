# AjisCountriesTest

Testovací aplikace demonstrující použití Afrowave.AJIS knihovny s daty zemí.

## Funkce

- Načtení countries*.json souborů z test adresářů
- Sloučení dat do countries.ajis
- CRUD operace pomocí AjisContext
- Vyhledávání podle názvu, hlavního města, regionu

## Sestavení

```bash
dotnet build tests/AjisCountriesTest/AjisCountriesTest.csproj
```

## Spuštění

```bash
dotnet run tests/AjisCountriesTest/AjisCountriesTest.csproj
```
