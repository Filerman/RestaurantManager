# RestaurantManager

Aplikacja webowa do zarządzania restauracją stworzona w oparciu o ASP.NET Core MVC.

## 1. Wymagania wstępne

Aby uruchomić projekt, na komputerze muszą być zainstalowane:

* **SDK .NET 8.0** (lub nowszy) - [Pobierz tutaj](https://dotnet.microsoft.com/download)
* **Microsoft SQL Server** (np. wersja Express lub Developer)
* **SQL Server Management Studio (SSMS)** - do podglądu bazy danych
* **Visual Studio 2022** (zalecane) lub Visual Studio Code

## 2. Konfiguracja Bazy Danych

Aplikacja wykorzystuje **Entity Framework Core** (podejście Code-First). Przed uruchomieniem należy skonfigurować połączenie z lokalną instancją SQL Server.

### Krok 1: Sprawdzenie nazwy serwera
1. Otwórz **SSMS**.
2. W oknie logowania skopiuj nazwę pola **Server name** (np. `localhost\SQLEXPRESS`, `(localdb)\MSSQLLocalDB` lub `DESKTOP-XXXX\SQLSERVER`).

### Krok 2: Edycja AppSettings
1. W folderze projektu otwórz plik `appsettings.json`.
2. Znajdź sekcję `ConnectionStrings` i klucz `DefaultConnection`.
3. Podmień fragment `Server=...` na nazwę Twojej instancji SQL Server.

Przykładowy `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS; Database=RestaurantDB; Trusted_Connection=True; TrustServerCertificate=True;"
}